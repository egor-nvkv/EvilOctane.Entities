#if !EVIL_OCTANE_ENABLE_PARALLEL_EVENT_ROUTING
using Unity.Assertions;
using Unity.Burst;
using Unity.Burst.CompilerServices;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using EventListenerList = Unity.Collections.LowLevel.Unsafe.InlineList<Unity.Entities.Entity>;
using EventListenerListHeader = Unity.Collections.LowLevel.Unsafe.InlineListHeader<Unity.Entities.Entity>;
using EventListenerMap = Unity.Collections.LowLevel.Unsafe.InlineHashMap<Unity.Entities.TypeIndex, EvilOctane.Entities.Internal.EventListenerListOffset>;
using EventListenerMapHeader = Unity.Collections.LowLevel.Unsafe.InlineHashMapHeader<Unity.Entities.TypeIndex>;

namespace EvilOctane.Entities.Internal
{
    [BurstCompile]
    public unsafe struct EventFirerRouteEventsJob : IJobChunk
    {
        [ReadOnly]
        public EntityTypeHandle EntityTypeHandle;

        // Firer

        public BufferTypeHandle<EventFirerInternal.EventSubscriptionRegistry.Storage> SubscriptionRegistryStorageTypeHandle;

        [ReadOnly]
        public BufferTypeHandle<EventFirer.EventBuffer.EntityElement> EventEntityBufferTypeHandle;
        [ReadOnly]
        public BufferTypeHandle<EventFirer.EventBuffer.TypeElement> EventTypeBufferTypeHandle;

        // Listener

        public BufferLookup<EventListener.EventReceiveBuffer.Element> EventReceiveBufferLookup;

        public AllocatorManager.AllocatorHandle TempAllocator;
        public EntityCommandBuffer CommandBuffer;

        public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
        {
            Assert.IsFalse(useEnabledMask);

            Entity* entityPtr = chunk.GetEntityDataPtrRO(EntityTypeHandle);

            BufferAccessor<EventFirerInternal.EventSubscriptionRegistry.Storage> registryStorageAccessor = chunk.GetBufferAccessorRW(ref SubscriptionRegistryStorageTypeHandle);
            BufferAccessor<EventFirer.EventBuffer.EntityElement> eventEntityBufferAccessor = chunk.GetBufferAccessorRO(ref EventEntityBufferTypeHandle);
            BufferAccessor<EventFirer.EventBuffer.TypeElement> eventTypeBufferAccessor = chunk.GetBufferAccessorRO(ref EventTypeBufferTypeHandle);

            // Route Events

            for (int entityIndex = 0; entityIndex != eventEntityBufferAccessor.Length; ++entityIndex)
            {
                DynamicBuffer<EventFirer.EventBuffer.EntityElement> eventEntityBuffer = eventEntityBufferAccessor[entityIndex];

                if (eventEntityBuffer.IsEmpty)
                {
                    // No Events
                    continue;
                }

                DynamicBuffer<EventFirerInternal.EventSubscriptionRegistry.Storage> registryStorage = registryStorageAccessor[entityIndex];
                EventListenerMapHeader* listenerMap = EventSubscriptionRegistryAPI.GetListenerMap(registryStorage, readOnly: true);

                DynamicBuffer<EventFirer.EventBuffer.TypeElement> eventTypeBuffer = eventTypeBufferAccessor[entityIndex];
                Assert.AreEqual(eventEntityBuffer.Length, eventTypeBuffer.Length);

                // Route

                RouteEvents(
                    entityPtr[entityIndex],
                    listenerMap,
                    eventEntityBuffer.AsSpanRO(),
                    eventTypeBuffer.AsSpanRO());
            }
        }

        private void RouteEvents(
            Entity entity,
            EventListenerMapHeader* listenerMap,
            UnsafeSpan<EventFirer.EventBuffer.EntityElement> eventSpanRO,
            UnsafeSpan<EventFirer.EventBuffer.TypeElement> eventTypeSpanRO)
        {
            nint firstListOffset = EventSubscriptionRegistryAPI.GetFirstListenerListOffset(listenerMap->Count);

            for (int eventIndex = 0; eventIndex != eventSpanRO.Length; ++eventIndex)
            {
                TypeIndex eventTypeIndex = eventTypeSpanRO[eventIndex].EventTypeIndex;
                bool eventTypeRegistered = EventListenerMap.TryGetValue(listenerMap, eventTypeIndex, out EventListenerListOffset listenerListOffset);

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
                EventListenerListHeader* listenerList = listenerListOffset.GetList(listenerMap, firstListOffset);

                if (listenerList->Length == 0)
                {
                    // No Listeners for this Event Type

#if ENABLE_PROFILER
                    ++EventSystemProfiler.EventsNotRoutedCounter.Data.Value;
#endif
                    continue;
                }

                Entity eventEntity = eventSpanRO[eventIndex].EventEntity;

#if ENABLE_PROFILER
                bool eventRouted = false;
#endif

                for (int listerIndex = 0; listerIndex != listenerList->Length;)
                {
                    Entity listenerEntity = EventListenerList.GetElementPointer(listenerList)[listerIndex];
                    bool hasReceiveBuffer = EventReceiveBufferLookup.TryGetBuffer(listenerEntity, out DynamicBuffer<EventListener.EventReceiveBuffer.Element> receiveBuffer);

                    if (Hint.Unlikely(!hasReceiveBuffer))
                    {
                        // Remove Listener

                        EventListenerList.RemoveAtSwapBack(listenerList, listerIndex);

#if ENABLE_PROFILER
                        ++EventSystemProfiler.PhantomListenersCounter.Data.Value;
#endif
                        continue;
                    }

                    // Route Event

                    _ = receiveBuffer.Add(new EventListener.EventReceiveBuffer.Element()
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
