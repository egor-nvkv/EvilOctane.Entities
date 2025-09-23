using System;
using System.Runtime.CompilerServices;
using Unity.Assertions;
using Unity.Burst;
using Unity.Burst.CompilerServices;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using EventList = Unity.Collections.LowLevel.Unsafe.UnsafeList<EvilOctane.Entities.EventReceiveBuffer.Element>;
using EventSpan = Unity.Collections.LowLevel.Unsafe.UnsafeSpan<EvilOctane.Entities.EventReceiveBuffer.Element>;
using ListenerList = EvilOctane.Entities.Internal.EventSubscriptionRegistry.Component.ListenerList;

namespace EvilOctane.Entities.Internal
{
    [BurstCompile]
    public unsafe struct EventRouteJob : IJobChunk
    {
        private const int maxListenerQueueLength = int.MaxValue;

        [ReadOnly]
        public EntityTypeHandle EntityTypeHandle;

        [ReadOnly]
        public BufferTypeHandle<EventBuffer.EntityElement> EventEntityBufferTypeHandle;
        [ReadOnly]
        public BufferTypeHandle<EventBuffer.TypeElement> EventTypeBufferTypeHandle;

        public ComponentTypeHandle<EventSubscriptionRegistry.Component> EventSubscriptionRegistryComponentTypeHandle;

        [NativeDisableParallelForRestriction]
        public BufferLookup<EventReceiveBuffer.Element> EventReceiveBufferLookup;
        [NativeDisableParallelForRestriction]
        public ComponentLookup<EventReceiveBuffer.LockComponent> EventReceiveBufferLockComponentLookup;

        public NativeArray<UnsafeHashMap<Entity, EventList>> ListenerEventListMapPerThreadArray;
        public NativeArray<UnsafeList<EventListenerEventListPair>> ListenerEventListsPerThreadArray;

        public AllocatorManager.AllocatorHandle TempAllocator;

        [NativeSetThreadIndex]
        public int ThreadIndex;

        private static void CreateSortedListenerEventLists(
            ref UnsafeHashMap<Entity, EventList> listenerEventListMap,
            ref UnsafeList<EventListenerEventListPair> listenerEventLists)
        {
            listenerEventLists.Clear();
            listenerEventLists.EnsureCapacity(listenerEventListMap.Count);

            // Copy to list

            foreach (KVPair<Entity, EventList> kvPair in listenerEventListMap)
            {
                kvPair.AssumeIndexIsValid();

                ref EventList eventList = ref kvPair.Value;

                if (eventList.IsEmpty)
                {
                    // Previously cleared
                    continue;
                }

                Entity listenerEntity = kvPair.Key;

                listenerEventLists.AddNoResize(new EventListenerEventListPair()
                {
                    ListenerEntity = listenerEntity,
                    EventSpan = eventList.AsSpan()
                });

                // Clear list in map
                eventList.Clear();
            }

            // Sort list
            listenerEventLists.Sort();
        }

        public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
        {
            Assert.IsFalse(useEnabledMask);

            ref UnsafeHashMap<Entity, EventList> listenerEventListMap = ref ListenerEventListMapPerThreadArray.ElementAt(ThreadIndex);

            if (!listenerEventListMap.IsCreated)
            {
                listenerEventListMap = UnsafeHashMapUtility.CreateHashMap<Entity, EventList>(16, 32, TempAllocator);
            }
            else
            {
                listenerEventListMap.Clear();
            }

            ref UnsafeList<EventListenerEventListPair> listenerEventLists = ref ListenerEventListsPerThreadArray.ElementAt(ThreadIndex);

            if (!listenerEventLists.IsCreated)
            {
                listenerEventLists = UnsafeListExtensions2.Create<EventListenerEventListPair>(4, TempAllocator);
            }

            Entity* entityPtr = chunk.GetEntityDataPtrRO(EntityTypeHandle);

            BufferAccessor<EventBuffer.EntityElement> eventEntityBufferAccessor = chunk.GetBufferAccessorRO(ref EventEntityBufferTypeHandle);
            BufferAccessor<EventBuffer.TypeElement> eventTypeBufferAccessor = chunk.GetBufferAccessorRO(ref EventTypeBufferTypeHandle);
            EventSubscriptionRegistry.Component* eventSubscriptionRegistryPtr = chunk.GetComponentDataPtrRW(ref EventSubscriptionRegistryComponentTypeHandle);

            for (int entityIndex = 0; ;)
            {
                // Fill map

                FillListenerEventLists(
                    ref entityIndex,
                    entityPtr,
                    eventEntityBufferAccessor,
                    eventTypeBufferAccessor,
                    eventSubscriptionRegistryPtr,
                    ref listenerEventListMap,
                    out bool chunkIterationFinished);

#if EVIL_OCTANE_SORT_EVENT_LISTENERS
                // Sort Event Lists by Listener
                CreateSortedListenerEventLists(ref listenerEventListMap, ref listenerEventLists);

                // Route Events
                RouteEvents(listenerEventLists.AsSpan());
#else
                // Route Events
                RouteEvents(listenerEventListMap);
#endif

                if (chunkIterationFinished)
                {
                    // Done
                    break;
                }

                // Clear Listener Event map
                listenerEventListMap.Clear();
            }
        }

        private void FillListenerEventLists(
            ref int entityIndex,
            Entity* entityPtr,
            BufferAccessor<EventBuffer.EntityElement> eventEntityBufferAccessor,
            BufferAccessor<EventBuffer.TypeElement> eventTypeBufferAccessor,
            EventSubscriptionRegistry.Component* eventSubscriptionRegistryPtr,
            ref UnsafeHashMap<Entity, EventList> listenerEventListMap,
            out bool chunkIterationFinished)
        {
            for (; entityIndex != eventEntityBufferAccessor.Length; ++entityIndex)
            {
                UnsafeSpan<EventBuffer.EntityElement> eventSpanRO = eventEntityBufferAccessor[entityIndex].AsSpanRO();

                if (eventSpanRO.IsEmpty)
                {
                    // No Events
                    continue;
                }

                ref EventSubscriptionRegistry.Component eventSubscriptionRegistry = ref eventSubscriptionRegistryPtr[entityIndex];

                if (Hint.Unlikely(eventSubscriptionRegistry.IsEmpty))
                {
                    // No subscriptions
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

                bool listenerMapIsFull = listenerEventListMap.Count >= maxListenerQueueLength;

                if (Hint.Unlikely(listenerMapIsFull))
                {
                    // Too many unique Listeners

                    // Don't forget to increment before returning
                    ++entityIndex;

                    chunkIterationFinished = false;
                    return;
                }
            }

            chunkIterationFinished = true;
        }

        private void QueueEvents(
            Entity eventFirerEntity,
            UnsafeSpan<EventBuffer.EntityElement> eventSpanRO,
            UnsafeSpan<EventBuffer.TypeElement> eventTypeSpanRO,
            ref EventSubscriptionRegistry.Component eventSubscriptionRegistry,
            ref UnsafeHashMap<Entity, EventList> listenerEventListMap)
        {
            HashMapHelperRef<TypeIndex> eventSubscriptionRegistryHelper = eventSubscriptionRegistry.EventTypeIndexListenerListMap.GetHelperRef();
            HashMapHelperRef<Entity> listenerEventListMapHelper = listenerEventListMap.GetHelperRef();

            for (int eventIndex = 0; eventIndex != eventSpanRO.Length; ++eventIndex)
            {
                TypeIndex eventTypeIndex = eventTypeSpanRO[eventIndex].EventTypeIndex;
                int eventTypeIndexInRegistry = eventSubscriptionRegistryHelper.Find(eventTypeIndex);

                if (eventTypeIndexInRegistry < 0)
                {
                    // No subscriptions for this Event Type
                    continue;
                }

                // Listeners to this Event Type
                ListenerList listenerList = eventSubscriptionRegistryHelper.GetValue<ListenerList>(eventTypeIndexInRegistry);

                Entity eventEntity = eventSpanRO[eventIndex].EventEntity;
                bool listenerListChanged = false;

                for (int listerIndex = 0; listerIndex != listenerList.Length;)
                {
                    Entity listenerEntity = listenerList.Ptr[listerIndex];
                    bool listenerExists = EventReceiveBufferLookup.EntityExists(listenerEntity);

                    if (Hint.Unlikely(!listenerExists))
                    {
                        // Listener destroyed

                        listenerList.AsUnsafeList().RemoveAtSwapBack(listerIndex);
                        --listenerList.Length;

                        listenerListChanged = true;
                        continue;
                    }

                    // Queue Event to per-Listener list

                    int listenerIndexInEventListMap = listenerEventListMapHelper.Find(listenerEntity);

                    if (listenerIndexInEventListMap >= 0)
                    {
                        // Listener already has Event list
                        EventList listenerEventList = listenerEventListMapHelper.GetValue<EventList>(listenerIndexInEventListMap);

                        // Queue Event
                        listenerEventList.Add(new EventReceiveBuffer.Element()
                        {
                            EventFirerEntity = eventFirerEntity,
                            EventEntity = eventEntity
                        });

                        // Update Event list
                        listenerEventListMapHelper.SetValue(listenerIndexInEventListMap, listenerEventList);
                    }
                    else
                    {
                        // First time visiting this Listener
                        EventList listenerEventList = UnsafeListExtensions2.Create<EventReceiveBuffer.Element>(4, TempAllocator);

                        // Queue Event
                        listenerEventList.AddNoResize(new EventReceiveBuffer.Element()
                        {
                            EventFirerEntity = eventFirerEntity,
                            EventEntity = eventEntity
                        });

                        // Register Event list
                        listenerEventListMapHelper.AddUnchecked(listenerEntity, listenerEventList);
                    }

                    ++listerIndex;
                }

                if (Hint.Unlikely(listenerListChanged))
                {
                    if (Hint.Likely(listenerList.Length > 0))
                    {
                        // Update Listener list
                        eventSubscriptionRegistryHelper.SetValue(eventTypeIndexInRegistry, listenerList);
                    }
                    else
                    {
                        // All Listeners destroyed

                        listenerList.Dispose();
                        _ = eventSubscriptionRegistryHelper.Remove(eventTypeIndex);
                    }
                }
            }
        }

        private void RouteEvents(UnsafeSpan<EventListenerEventListPair> listenerEventListSpan)
        {
            foreach (EventListenerEventListPair listenerEventListPair in listenerEventListSpan)
            {
                Entity listenerEntity = listenerEventListPair.ListenerEntity;
                EventSpan eventSpan = listenerEventListPair.EventSpan;

                RouteEvents(listenerEntity, eventSpan);
            }
        }

        private void RouteEvents(UnsafeHashMap<Entity, EventList> listenerEventListMap)
        {
            foreach (KVPair<Entity, EventList> kvPair in listenerEventListMap)
            {
                kvPair.AssumeIndexIsValid();

                Entity listenerEntity = kvPair.Key;
                EventList eventList = kvPair.Value;

                RouteEvents(listenerEntity, eventList.AsSpan());
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
    }
}
