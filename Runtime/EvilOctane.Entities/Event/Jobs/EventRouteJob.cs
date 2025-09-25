#if !EVIL_OCTANE_ENABLE_PARALLEL_EVENT_ROUTING
using System;
using Unity.Assertions;
using Unity.Burst;
using Unity.Burst.CompilerServices;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Entities.LowLevel.Unsafe;
using ListenerList = EvilOctane.Entities.Internal.EventSubscriptionRegistry.Component.ListenerList;

namespace EvilOctane.Entities.Internal
{
    [BurstCompile]
    public unsafe struct EventRouteJob : IJobChunk
    {
        [ReadOnly]
        public EntityTypeHandle EntityTypeHandle;

        public ComponentTypeHandle<EventSubscriptionRegistry.Component> EventSubscriptionRegistryComponentTypeHandle;
        public BufferTypeHandle<EventBuffer.EntityElement> EventEntityBufferTypeHandle;
        public BufferTypeHandle<EventBuffer.TypeElement> EventTypeBufferTypeHandle;

        public BufferLookup<EventReceiveBuffer.Element> EventReceiveBufferLookup;

        public AllocatorManager.AllocatorHandle TempAllocator;
        public EntityCommandBuffer CommandBuffer;

        public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
        {
            Assert.IsFalse(useEnabledMask);

            Entity* entityPtr = chunk.GetEntityDataPtrRO(EntityTypeHandle);

            BufferAccessor<EventBuffer.EntityElement> eventEntityBufferAccessor = chunk.GetBufferAccessorRW(ref EventEntityBufferTypeHandle);
            BufferAccessor<EventBuffer.TypeElement> eventTypeBufferAccessor = chunk.GetBufferAccessorRW(ref EventTypeBufferTypeHandle);
            EventSubscriptionRegistry.Component* eventSubscriptionRegistryPtr = chunk.GetComponentDataPtrRW(ref EventSubscriptionRegistryComponentTypeHandle);

            // Route Events

            RouteEvents(
                entityPtr,
                eventEntityBufferAccessor,
                eventTypeBufferAccessor,
                eventSubscriptionRegistryPtr);

            // Clear Event Buffers

            // Get Event Entities
            UnsafeList<Entity> entitiesToDestroyList = EntityDynamicBufferUtility.ExtractEntityList(eventEntityBufferAccessor, TempAllocator, clearBuffers: true);

            if (Hint.Likely(!entitiesToDestroyList.IsEmpty))
            {
                // Destroy Event Entities
                CommandBuffer.DestroyEntity(entitiesToDestroyList.AsSpan());
            }

            // Clear Event Types
            DynamicBufferUtility.ClearBuffersIgnoreFilter(in chunk, ref EventTypeBufferTypeHandle);
        }

        private void RouteEvents(
            Entity* entityPtr,
            BufferAccessor<EventBuffer.EntityElement> eventEntityBufferAccessor,
            BufferAccessor<EventBuffer.TypeElement> eventTypeBufferAccessor,
            EventSubscriptionRegistry.Component* eventSubscriptionRegistryPtr)
        {
            for (int entityIndex = 0; entityIndex != eventEntityBufferAccessor.Length; ++entityIndex)
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

#if ENABLE_PROFILER
                    EventSystemProfiler.EventsNotRoutedCounter.Data.Value += eventSpanRO.Length;
#endif
                    continue;
                }

                UnsafeSpan<EventBuffer.TypeElement> eventTypeSpanRO = eventTypeBufferAccessor[entityIndex].AsSpanRO();
                Assert.AreEqual(eventSpanRO.Length, eventTypeSpanRO.Length);

                Entity eventFirerEntity = entityPtr[entityIndex];

                RouteEvents(
                    eventFirerEntity,
                    eventSpanRO,
                    eventTypeSpanRO,
                    ref eventSubscriptionRegistry);
            }
        }

        private void RouteEvents(
            Entity eventFirerEntity,
            UnsafeSpan<EventBuffer.EntityElement> eventSpanRO,
            UnsafeSpan<EventBuffer.TypeElement> eventTypeSpanRO,
            ref EventSubscriptionRegistry.Component eventSubscriptionRegistry)
        {
            HashMapHelperRef<TypeIndex> eventSubscriptionRegistryHelper = eventSubscriptionRegistry.EventTypeIndexListenerListMap.GetHelperRef();

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

                    // Route Event

                    DynamicBuffer<EventReceiveBuffer.Element> receiveBuffer = EventReceiveBufferLookup[listenerEntity];

                    _ = receiveBuffer.Add(new EventReceiveBuffer.Element()
                    {
                        EventFirerEntity = eventFirerEntity,
                        EventEntity = eventEntity
                    });

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
    }
}
#endif
