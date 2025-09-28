#if EVIL_OCTANE_ENABLE_PARALLEL_EVENT_ROUTING
using System;
using System.Runtime.CompilerServices;
using Unity.Assertions;
using Unity.Burst;
using Unity.Burst.CompilerServices;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Entities.LowLevel.Unsafe;
using EventList = Unity.Collections.LowLevel.Unsafe.UnsafeList<EvilOctane.Entities.EventReceiveBuffer.Element>;
using EventSpan = Unity.Collections.LowLevel.Unsafe.UnsafeSpan<EvilOctane.Entities.EventReceiveBuffer.Element>;
using ListenerList = EvilOctane.Entities.Internal.EventSubscriptionRegistryComponent.ListenerList;

#if EVIL_OCTANE_ENABLE_EVENT_REROUTING
using static System.Runtime.CompilerServices.Unsafe;
#endif

namespace EvilOctane.Entities.Internal
{
    [BurstCompile]
    public unsafe struct EventRouteJobParallel : IJobChunk
    {
        [ReadOnly]
        public EntityTypeHandle EntityTypeHandle;

        public ComponentTypeHandle<EventSubscriptionRegistryComponent> EventSubscriptionRegistryComponentTypeHandle;
        public BufferTypeHandle<EventBuffer.EntityElement> EventEntityBufferTypeHandle;
        public BufferTypeHandle<EventBuffer.TypeElement> EventTypeBufferTypeHandle;

        [NativeDisableParallelForRestriction]
        public BufferLookup<EventReceiveBuffer.Element> EventReceiveBufferLookup;
        [NativeDisableParallelForRestriction]
        public ComponentLookup<EventReceiveBuffer.LockComponent> EventReceiveBufferLockComponentLookup;

        public NativeArray<PerThreadTempContainers> PerThreadTempContainersArray;

        public AllocatorManager.AllocatorHandle TempAllocator;
        public EntityCommandBuffer.ParallelWriter CommandBuffer;

        [NativeSetThreadIndex]
        public int ThreadIndex;

        public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
        {
            Assert.IsFalse(useEnabledMask);

            ref PerThreadTempContainers tempContainers = ref PerThreadTempContainersArray.ElementAt(ThreadIndex);
            tempContainers.CreateOrClear(TempAllocator);

            Entity* entityPtr = chunk.GetEntityDataPtrRO(EntityTypeHandle);

            BufferAccessor<EventBuffer.EntityElement> eventEntityBufferAccessor = chunk.GetBufferAccessorRW(ref EventEntityBufferTypeHandle);
            BufferAccessor<EventBuffer.TypeElement> eventTypeBufferAccessor = chunk.GetBufferAccessorRW(ref EventTypeBufferTypeHandle);
            EventSubscriptionRegistryComponent* eventSubscriptionRegistryPtr = chunk.GetComponentDataPtrRW(ref EventSubscriptionRegistryComponentTypeHandle);

            // Fill map

            FillListenerEventLists(
                entityPtr,
                eventEntityBufferAccessor,
                eventTypeBufferAccessor,
                eventSubscriptionRegistryPtr,
                ref tempContainers.ListenerEventListMap);

            // Route Events

#if EVIL_OCTANE_ENABLE_EVENT_REROUTING
            RouteEventsWithRerouting(ref tempContainers);
#else
            RouteEvents(ref tempContainers);
#endif

            // Clear Event Buffers

            // Get Event Entities
            UnsafeList<Entity> entitiesToDestroyList = EntityDynamicBufferUtility.ExtractEntityList(eventEntityBufferAccessor, TempAllocator, clearBuffers: true);

            if (Hint.Likely(!entitiesToDestroyList.IsEmpty))
            {
                // Destroy Event Entities
                CommandBuffer.DestroyEntity(unfilteredChunkIndex, entitiesToDestroyList.AsSpan());
            }

            // Clear Event Types
            DynamicBufferUtility.ClearAllBuffersInChunk(in chunk, ref EventTypeBufferTypeHandle);
        }

        private void FillListenerEventLists(
            Entity* entityPtr,
            BufferAccessor<EventBuffer.EntityElement> eventEntityBufferAccessor,
            BufferAccessor<EventBuffer.TypeElement> eventTypeBufferAccessor,
            EventSubscriptionRegistryComponent* eventSubscriptionRegistryPtr,
            ref UnsafeHashMap<Entity, EventList> listenerEventListMap)
        {
            for (int entityIndex = 0; entityIndex != eventEntityBufferAccessor.Length; ++entityIndex)
            {
                UnsafeSpan<EventBuffer.EntityElement> eventSpanRO = eventEntityBufferAccessor[entityIndex].AsSpanRO();

                if (eventSpanRO.IsEmpty)
                {
                    // No Events
                    continue;
                }

                ref EventSubscriptionRegistryComponent eventSubscriptionRegistry = ref eventSubscriptionRegistryPtr[entityIndex];

                if (Hint.Unlikely(eventSubscriptionRegistry.IsEmpty))
                {
                    // No subscriptions

#if ENABLE_PROFILER
                    EventSystemProfiler.EventsNotRoutedCounter.Data.Value += eventSpanRO.Length;
#endif
                    continue;
                }

                UnsafeSpan<EventBuffer.TypeElement> eventTypeSpanRO = eventTypeBufferAccessor[entityIndex].AsSpanRO();
                Assert.AreEqual(eventSpanRO.Length, eventTypeSpanRO.Length);

                Entity eventFirerEntity = entityPtr[entityIndex];

                QueueEvents(
                    eventFirerEntity,
                    eventSpanRO,
                    eventTypeSpanRO,
                    ref eventSubscriptionRegistry,
                    ref listenerEventListMap);
            }
        }

        private void QueueEvents(
            Entity eventFirerEntity,
            UnsafeSpan<EventBuffer.EntityElement> eventSpanRO,
            UnsafeSpan<EventBuffer.TypeElement> eventTypeSpanRO,
            ref EventSubscriptionRegistryComponent eventSubscriptionRegistry,
            ref UnsafeHashMap<Entity, EventList> listenerEventListMap)
        {
            HashMapHelperRef<TypeIndex> eventSubscriptionRegistryHelper = eventSubscriptionRegistry.EventTypeIndexListenerListMap.GetHelperRef();
            HashMapHelperRef<Entity> listenerEventListMapHelper = listenerEventListMap.GetHelperRef();

            for (int eventIndex = 0; eventIndex != eventSpanRO.Length; ++eventIndex)
            {
                TypeIndex eventTypeIndex = eventTypeSpanRO[eventIndex].EventTypeIndex;

                // Listeners to this Event Type
                ref ListenerList listenerList = ref eventSubscriptionRegistryHelper.TryGetValueRef<ListenerList>(eventTypeIndex, out bool listenerListExists);

                if (!listenerListExists)
                {
                    // No subscriptions for this Event Type

#if ENABLE_PROFILER
                    ++EventSystemProfiler.EventsNotRoutedCounter.Data.Value;
#endif
                    continue;
                }

                Entity eventEntity = eventSpanRO[eventIndex].EventEntity;

#if ENABLE_PROFILER
                bool eventRouted = false;
#endif

                for (int listerIndex = 0; listerIndex != listenerList.Length;)
                {
                    Entity listenerEntity = listenerList.Ptr[listerIndex];
                    bool listenerExists = EventReceiveBufferLookup.EntityExists(listenerEntity);

                    if (Hint.Unlikely(!listenerExists))
                    {
                        // Listener destroyed

                        listenerList.RemoveListener(listerIndex);

#if ENABLE_PROFILER
                        ++EventSystemProfiler.PhantomListenersCounter.Data.Value;
#endif
                        continue;
                    }

                    // Queue Event to per-Listener list

                    ref EventList listenerEventList = ref listenerEventListMapHelper.GetOrAddValue<EventList>(listenerEntity, out bool eventListAdded);

                    if (eventListAdded)
                    {
                        // First time visiting this Listener

                        listenerEventList = UnsafeListExtensions2.Create<EventReceiveBuffer.Element>(4, TempAllocator);

                        // Queue Event
                        listenerEventList.AddNoResize(new EventReceiveBuffer.Element()
                        {
                            EventFirerEntity = eventFirerEntity,
                            EventEntity = eventEntity
                        });
                    }
                    else
                    {
                        // Listener already has Event list

                        // Queue Event
                        listenerEventList.Add(new EventReceiveBuffer.Element()
                        {
                            EventFirerEntity = eventFirerEntity,
                            EventEntity = eventEntity
                        });
                    }

#if ENABLE_PROFILER
                    eventRouted = true;
#endif

                    ++listerIndex;
                }

                if (Hint.Unlikely(listenerList.Length == 0))
                {
                    // All Listeners destroyed

                    listenerList.Dispose();
                    _ = eventSubscriptionRegistryHelper.Remove(eventTypeIndex);
                }

#if ENABLE_PROFILER
                if (Hint.Unlikely(!eventRouted))
                {
                    ++EventSystemProfiler.EventsNotRoutedCounter.Data.Value;
                }
#endif
            }
        }

#if EVIL_OCTANE_ENABLE_EVENT_REROUTING
        private void RouteEventsWithRerouting(ref PerThreadTempContainers tempContainers)
        {
            tempContainers.ListenerEventLists0.EnsureCapacity(tempContainers.ListenerEventListMap.Count);

            foreach (KVPair<Entity, EventList> kvPair in tempContainers.ListenerEventListMap)
            {
                kvPair.AssumeIndexIsValid();

                Entity listenerEntity = kvPair.Key;
                EventSpan eventSpan = kvPair.Value.AsSpan();

                if (!TryRouteEvents(listenerEntity, eventSpan))
                {
                    // Reroute
                    tempContainers.ListenerEventLists0.AddNoResize((listenerEntity, eventSpan));
                }
            }

            if (tempContainers.ListenerEventLists0.IsEmpty)
            {
                // Done
                return;
            }

            tempContainers.ListenerEventLists1.EnsureCapacity(tempContainers.ListenerEventLists0.Length);

            for (int listIndex = 0; ; listIndex = 1 - listIndex)
            {
                if (ReRouteEvents(ref tempContainers, listIndex))
                {
                    // Done
                    break;
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool ReRouteEvents(ref PerThreadTempContainers tempContainers, int listIndex)
        {
            ref UnsafeList<(Entity, EventSpan)> listenerEventListsActive = ref Add(ref tempContainers.ListenerEventLists0, listIndex);
            ref UnsafeList<(Entity, EventSpan)> listenerEventListsNext = ref Add(ref tempContainers.ListenerEventLists0, 1 - listIndex);

            listenerEventListsNext.Clear();

            foreach ((Entity listenerEntity, EventSpan eventSpan) in listenerEventListsActive)
            {
                if (!TryRouteEvents(listenerEntity, eventSpan))
                {
                    // Reroute
                    listenerEventListsNext.AddNoResize((listenerEntity, eventSpan));
                }
            }

            return listenerEventListsNext.IsEmpty;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool TryRouteEvents(Entity listenerEntity, EventSpan eventSpan)
        {
            RefRW<EventReceiveBuffer.LockComponent> eventReceiveBufferLock = EventReceiveBufferLockComponentLookup.GetRefRW(listenerEntity);
            ref Spinner spinner = ref eventReceiveBufferLock.ValueRW.Spinner;

            // Try lock

            if (!spinner.TryAcquire())
            {
                // Busy
                return false;
            }

            DynamicBuffer<EventReceiveBuffer.Element> eventReceiveBuffer = EventReceiveBufferLookup[listenerEntity];

            // Route Events
            eventReceiveBuffer.AddRange(eventSpan);

            // Unlock
            spinner.Release();

            return true;
        }
#else
        private void RouteEvents(ref PerThreadTempContainers tempContainers)
        {
            foreach (KVPair<Entity, EventList> kvPair in tempContainers.ListenerEventListMap)
            {
                kvPair.AssumeIndexIsValid();

                Entity listenerEntity = kvPair.Key;
                EventSpan eventSpan = kvPair.Value.AsSpan();

                RouteEvents(listenerEntity, eventSpan);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void RouteEvents(Entity listenerEntity, EventSpan eventSpan)
        {
            DynamicBuffer<EventReceiveBuffer.Element> eventReceiveBuffer = EventReceiveBufferLookup[listenerEntity];
            RefRW<EventReceiveBuffer.LockComponent> eventReceiveBufferLock = EventReceiveBufferLockComponentLookup.GetRefRW(listenerEntity);

            ref Spinner spinner = ref eventReceiveBufferLock.ValueRW.Spinner;

            // Lock
            spinner.Acquire();

            // Route Events
            eventReceiveBuffer.AddRange(eventSpan);

            // Unlock
            spinner.Release();
        }
#endif

        public struct PerThreadTempContainers
        {
            public UnsafeHashMap<Entity, EventList> ListenerEventListMap;

#if EVIL_OCTANE_ENABLE_EVENT_REROUTING
            public UnsafeList<(Entity, EventSpan)> ListenerEventLists0;
            public UnsafeList<(Entity, EventSpan)> ListenerEventLists1;
#endif

            public readonly bool IsCreated => ListenerEventListMap.IsCreated;

            public void Create(AllocatorManager.AllocatorHandle tempAllocator)
            {
                ListenerEventListMap = UnsafeHashMapUtility.CreateHashMap<Entity, EventList>(16, 32, tempAllocator);

#if EVIL_OCTANE_ENABLE_EVENT_REROUTING
                ListenerEventLists0 = UnsafeListExtensions2.Create<(Entity, EventSpan)>(16, tempAllocator);
                ListenerEventLists1 = UnsafeListExtensions2.Create<(Entity, EventSpan)>(16, tempAllocator);
#endif
            }

            public void Clear()
            {
                ListenerEventListMap.Clear();

#if EVIL_OCTANE_ENABLE_EVENT_REROUTING
                ListenerEventLists0.Clear();
                ListenerEventLists1.Clear();
#endif
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void CreateOrClear(AllocatorManager.AllocatorHandle tempAllocator)
            {
                if (IsCreated)
                {
                    Clear();
                }
                else
                {
                    Create(tempAllocator);
                }
            }
        }
    }
}
#endif
