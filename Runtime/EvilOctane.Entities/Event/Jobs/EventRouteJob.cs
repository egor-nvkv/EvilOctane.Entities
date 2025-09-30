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
using EventSubscriberList = Unity.Collections.LowLevel.Unsafe.InlineList<Unity.Entities.Entity>;
using EventSubscriberListHeader = Unity.Collections.LowLevel.Unsafe.InlineListHeader<Unity.Entities.Entity>;
using EventSubscriptionMap = Unity.Collections.LowLevel.Unsafe.InlineHashMap<Unity.Entities.TypeIndex, EvilOctane.Entities.Internal.EventSubscriberListOffset>;
using EventSubscriptionMapHeader = Unity.Collections.LowLevel.Unsafe.InlineHashMapHeader<Unity.Entities.TypeIndex>;

namespace EvilOctane.Entities.Internal
{
    [BurstCompile]
    public unsafe struct EventRouteJob : IJobChunk
    {
        [ReadOnly]
        public EntityTypeHandle EntityTypeHandle;

        // Firer

        public BufferTypeHandle<EventSubscriptionRegistry.StorageBufferElement> EventSubscriptionRegistryStorageBufferTypeHandle;

        public BufferTypeHandle<EventBuffer.EntityElement> EventEntityBufferTypeHandle;
        public BufferTypeHandle<EventBuffer.TypeElement> EventTypeBufferTypeHandle;

        // Listener

        public BufferLookup<EventReceiveBuffer.Element> EventReceiveBufferLookup;

        public AllocatorManager.AllocatorHandle TempAllocator;
        public EntityCommandBuffer CommandBuffer;

        public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
        {
            Assert.IsFalse(useEnabledMask);

            Entity* entityPtr = chunk.GetEntityDataPtrRO(EntityTypeHandle);

            BufferAccessor<EventSubscriptionRegistry.StorageBufferElement> eventSubscriptionRegistryStorageBufferAccessor = chunk.GetBufferAccessorRW(ref EventSubscriptionRegistryStorageBufferTypeHandle);
            BufferAccessor<EventBuffer.EntityElement> eventEntityBufferAccessor = chunk.GetBufferAccessorRW(ref EventEntityBufferTypeHandle);
            BufferAccessor<EventBuffer.TypeElement> eventTypeBufferAccessor = chunk.GetBufferAccessorRW(ref EventTypeBufferTypeHandle);

            // Route Events

            RouteEvents(
                entityPtr,
                eventSubscriptionRegistryStorageBufferAccessor,
                eventEntityBufferAccessor,
                eventTypeBufferAccessor);

            // Destroy Event Entities

            UnsafeList<Entity> entitiesToDestroyList = EntityOwner.ExtractOwnedEntityList(eventEntityBufferAccessor, TempAllocator, clearBuffers: true);

            if (Hint.Likely(!entitiesToDestroyList.IsEmpty))
            {
                // Destroy
                CommandBuffer.DestroyEntity(entitiesToDestroyList.AsSpan());
            }

            // Clear
            DynamicBufferUtility.ClearAllBuffersInChunk(in chunk, eventTypeBufferAccessor);
        }

        private void RouteEvents(
            Entity* entityPtr,
            BufferAccessor<EventSubscriptionRegistry.StorageBufferElement> eventSubscriptionRegistryStorageBufferAccessor,
            BufferAccessor<EventBuffer.EntityElement> eventEntityBufferAccessor,
            BufferAccessor<EventBuffer.TypeElement> eventTypeBufferAccessor)
        {
            for (int entityIndex = 0; entityIndex != eventEntityBufferAccessor.Length; ++entityIndex)
            {
                DynamicBuffer<EventBuffer.EntityElement> eventEntityBuffer = eventEntityBufferAccessor[entityIndex];

                if (eventEntityBuffer.IsEmpty)
                {
                    // No Events
                    continue;
                }

                DynamicBuffer<EventSubscriptionRegistry.StorageBufferElement> eventSubscriptionRegistryStorageBuffer = eventSubscriptionRegistryStorageBufferAccessor[entityIndex];
                EventSubscriptionMapHeader* subscriptionMap = EventSubscriptionRegistry.GetSubscriptionMap(eventSubscriptionRegistryStorageBuffer, readOnly: true);

                DynamicBuffer<EventBuffer.TypeElement> eventTypeBuffer = eventTypeBufferAccessor[entityIndex];
                Assert.AreEqual(eventEntityBuffer.Length, eventTypeBuffer.Length);

                // Route

                RouteEvents(
                    entityPtr[entityIndex],
                    subscriptionMap,
                    eventEntityBuffer.AsSpanRO(),
                    eventTypeBuffer.AsSpanRO());
            }
        }

        private void RouteEvents(
            Entity entity,
            EventSubscriptionMapHeader* subscriptionMap,
            UnsafeSpan<EventBuffer.EntityElement> eventSpanRO,
            UnsafeSpan<EventBuffer.TypeElement> eventTypeSpanRO)
        {
            nint firstListOffset = EventSubscriptionRegistry.GetFirstSubscriberListOffset(subscriptionMap);

            for (int eventIndex = 0; eventIndex != eventSpanRO.Length; ++eventIndex)
            {
                TypeIndex eventTypeIndex = eventTypeSpanRO[eventIndex].EventTypeIndex;
                bool eventTypeRegistered = EventSubscriptionMap.TryGetValue(subscriptionMap, eventTypeIndex, out EventSubscriberListOffset subscriberListOffset);

                if (Hint.Unlikely(!eventTypeRegistered))
                {
                    // Event Type not registered

                    EventDebugUtility.LogFiredEventTypeNotRegistered(entity, eventTypeIndex);

#if ENABLE_PROFILER
                    ++EventSystemProfiler.EventsNotRoutedCounter.Data.Value;
                    // TODO: misfired event profile counter
#endif
                    continue;
                }

                // Listeners to this Event Type
                EventSubscriberListHeader* subscriberList = subscriberListOffset.GetList(subscriptionMap, firstListOffset);

                if (subscriberList->Length == 0)
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

                for (int listerIndex = 0; listerIndex != subscriberList->Length;)
                {
                    Entity listenerEntity = EventSubscriberList.GetElementPointer(subscriberList)[listerIndex];
                    bool listenerIsValid = EventReceiveBufferLookup.TryGetBuffer(listenerEntity, out DynamicBuffer<EventReceiveBuffer.Element> receiveBuffer);

                    if (Hint.Unlikely(!listenerIsValid))
                    {
                        // Remove Listener

                        EventSubscriberList.RemoveAtSwapBack(subscriberList, listerIndex);

#if ENABLE_PROFILER
                        ++EventSystemProfiler.PhantomListenersCounter.Data.Value;
#endif
                        continue;
                    }

                    // Route Event

                    _ = receiveBuffer.Add(new EventReceiveBuffer.Element()
                    {
                        EventFirerEntity = entity,
                        EventEntity = eventEntity
                    });

#if ENABLE_PROFILER
                    eventRouted = true;
#endif

                    ++listerIndex;
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
