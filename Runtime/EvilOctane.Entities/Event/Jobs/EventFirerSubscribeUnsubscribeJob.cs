using System;
using System.Runtime.CompilerServices;
using Unity.Assertions;
using Unity.Burst;
using Unity.Burst.CompilerServices;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using EventSubscriptionMapHeader = Unity.Collections.LowLevel.Unsafe.InlineHashMapHeader<Unity.Entities.TypeIndex>;

namespace EvilOctane.Entities.Internal
{
    [BurstCompile]
    public unsafe struct EventFirerSubscribeUnsubscribeJob : IJobChunk
    {
        // Firer

        public BufferTypeHandle<EventSubscriptionRegistry.StorageBufferElement> EventSubscriptionRegistryStorageBufferTypeHandle;
        public BufferTypeHandle<EventSubscriptionRegistry.SubscribeUnsubscribeBufferElement> EventSubscriptionRegistrySubscribeUnsubscribeBufferTypeHandle;

        // Listener

        [ReadOnly]
        public BufferLookup<EventSettings.ListenerDeclaredEventTypeBufferElement> ListenerDeclaredEventTypeBufferLookup;

        public AllocatorManager.AllocatorHandle TempAllocator;

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static UnsafeHashMap<TypeIndex, EventListenerListCapacityPair> AllocateEventTypeListenerListMap(EventSubscriptionMapHeader* subscriptionMap, AllocatorManager.AllocatorHandle allocator)
        {
            int capacity = subscriptionMap->Count + 4;
            return UnsafeHashMapUtility.CreateHashMap<TypeIndex, EventListenerListCapacityPair>(capacity, 8, allocator);
        }

        public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
        {
            Assert.IsFalse(useEnabledMask);

            BufferAccessor<EventSubscriptionRegistry.StorageBufferElement> eventSubscriptionRegistryStorageBufferAccessor = chunk.GetBufferAccessorRW(ref EventSubscriptionRegistryStorageBufferTypeHandle);
            BufferAccessor<EventSubscriptionRegistry.SubscribeUnsubscribeBufferElement> eventSubscriptionRegistrySubscribeUnsubscribeBufferAccessor = chunk.GetBufferAccessorRW(ref EventSubscriptionRegistrySubscribeUnsubscribeBufferTypeHandle);

            UnsafeHashMap<TypeIndex, EventListenerListCapacityPair> eventTypeListenerListMap = new();

            for (int entityIndex = 0; entityIndex != chunk.Count; ++entityIndex)
            {
                DynamicBuffer<EventSubscriptionRegistry.SubscribeUnsubscribeBufferElement> eventSubscriptionRegistrySubscribeUnsubscribeBuffer = eventSubscriptionRegistrySubscribeUnsubscribeBufferAccessor[entityIndex];
                UnsafeSpan<EventSubscriptionRegistry.SubscribeUnsubscribeBufferElement> eventSubscriptionRegistrySubscribeUnsubscribeSpanRO = eventSubscriptionRegistrySubscribeUnsubscribeBuffer.AsSpanRO();

                if (eventSubscriptionRegistrySubscribeUnsubscribeSpanRO.IsEmpty)
                {
                    // Empty
                    continue;
                }

                DynamicBuffer<EventSubscriptionRegistry.StorageBufferElement> eventSubscriptionRegistryStorage = eventSubscriptionRegistryStorageBufferAccessor[entityIndex];

                bool isFull = !ExecuteTryAddNoResize(
                    eventSubscriptionRegistryStorage,
                    ref eventSubscriptionRegistrySubscribeUnsubscribeSpanRO,
                    ref eventTypeListenerListMap);

                if (Hint.Unlikely(isFull))
                {
                    // Execute on temp map
                    // Copy back the result

                    ExecuteTempMap(
                        eventSubscriptionRegistryStorage,
                        eventSubscriptionRegistrySubscribeUnsubscribeSpanRO,
                        ref eventTypeListenerListMap,
                        clearMap: entityIndex != chunk.Count - 1);
                }

                // Clear buffer
                eventSubscriptionRegistrySubscribeUnsubscribeBuffer.Clear();
            }
        }

        private bool ExecuteTryAddNoResize(
            DynamicBuffer<EventSubscriptionRegistry.StorageBufferElement> eventSubscriptionRegistryStorage,
            ref UnsafeSpan<EventSubscriptionRegistry.SubscribeUnsubscribeBufferElement> eventSubscriptionRegistrySubscribeUnsubscribeSpan,
            ref UnsafeHashMap<TypeIndex, EventListenerListCapacityPair> eventTypeListenerListMap)
        {
            for (int index = 0; index != eventSubscriptionRegistrySubscribeUnsubscribeSpan.Length; ++index)
            {
                EventSubscriptionRegistry.SubscribeUnsubscribeBufferElement subscribeUnsubscribe = eventSubscriptionRegistrySubscribeUnsubscribeSpan[index];
                bool isFull = subscribeUnsubscribe.Mode switch
                {
                    EventSubscriptionRegistry.SubscribeUnsubscribeMode.SubscribeManual => !SubscribeManualTryNoResize(
                                                eventSubscriptionRegistryStorage,
                                                ref eventTypeListenerListMap,
                                                subscribeUnsubscribe.ListenerEntity,
                                                subscribeUnsubscribe.EventTypeIndex),
                    EventSubscriptionRegistry.SubscribeUnsubscribeMode.UnsubscribeAuto => throw new NotImplementedException(),
                    EventSubscriptionRegistry.SubscribeUnsubscribeMode.UnsubscribeManual => throw new NotImplementedException(),
                    _ => !SubscribeAutoTryNoResize(
                                                eventSubscriptionRegistryStorage,
                                                ref eventTypeListenerListMap,
                                                subscribeUnsubscribe.ListenerEntity),
                };
                if (Hint.Unlikely(isFull))
                {
                    // Full

                    eventSubscriptionRegistrySubscribeUnsubscribeSpan = eventSubscriptionRegistrySubscribeUnsubscribeSpan[index..];
                    return false;
                }
            }

            return true;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void ExecuteTempMap(
            DynamicBuffer<EventSubscriptionRegistry.StorageBufferElement> eventSubscriptionRegistryStorage,
            UnsafeSpan<EventSubscriptionRegistry.SubscribeUnsubscribeBufferElement> eventSubscriptionRegistrySubscribeUnsubscribeSpan,
            ref UnsafeHashMap<TypeIndex, EventListenerListCapacityPair> eventTypeListenerListMap,
            bool clearMap)
        {
            for (int index = 0; index != eventSubscriptionRegistrySubscribeUnsubscribeSpan.Length; ++index)
            {
                EventSubscriptionRegistry.SubscribeUnsubscribeBufferElement subscribeUnsubscribe = eventSubscriptionRegistrySubscribeUnsubscribeSpan[index];

                switch (subscribeUnsubscribe.Mode)
                {
                    case EventSubscriptionRegistry.SubscribeUnsubscribeMode.SubscribeAuto:
                    default:
                        SubscribeAutoTempMap(
                            ref eventTypeListenerListMap,
                            subscribeUnsubscribe.ListenerEntity);

                        break;

                    case EventSubscriptionRegistry.SubscribeUnsubscribeMode.SubscribeManual:
                        SubscribeManualTempMap(
                            ref eventTypeListenerListMap,
                            subscribeUnsubscribe.ListenerEntity,
                            subscribeUnsubscribe.EventTypeIndex);

                        break;

                    case EventSubscriptionRegistry.SubscribeUnsubscribeMode.UnsubscribeAuto:
                        throw new NotImplementedException();

                    case EventSubscriptionRegistry.SubscribeUnsubscribeMode.UnsubscribeManual:
                        throw new NotImplementedException();
                }
            }

            // Copy back
            EventSubscriptionRegistry.CopyFrom(eventSubscriptionRegistryStorage, ref eventTypeListenerListMap);

            // Clear map

            if (clearMap)
            {
                UnsafeSpan<EventListenerListCapacityPair> valueSpan = eventTypeListenerListMap.GetHelperRef().GetValueSpan<EventListenerListCapacityPair>();

                for (int index = 0; index != valueSpan.Length; ++index)
                {
                    // Keep allocation
                    valueSpan.ElementAt(index).Clear();
                }
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private bool SubscribeAutoTryNoResize(
            DynamicBuffer<EventSubscriptionRegistry.StorageBufferElement> eventSubscriptionRegistryStorage,
            ref UnsafeHashMap<TypeIndex, EventListenerListCapacityPair> eventTypeListenerListMap,
            Entity listenerEntity)
        {
            bool listenerExists = ListenerDeclaredEventTypeBufferLookup.EntityExists(listenerEntity);

            if (Hint.Unlikely(!listenerExists))
            {
                // Listener destroyed
                return true;
            }

            UnsafeSpan<EventSettings.ListenerDeclaredEventTypeBufferElement> listenerDeclaredEventTypeSpanRO = ListenerDeclaredEventTypeBufferLookup[listenerEntity].AsSpanRO();

            bool isFull = !EventSubscriptionRegistry.TrySubscribeNoResize(
                eventSubscriptionRegistryStorage,
                listenerDeclaredEventTypeSpanRO,
                listenerEntity,
                out int processedCount);

            if (Hint.Unlikely(isFull))
            {
                // Full

                if (!eventTypeListenerListMap.IsCreated)
                {
                    EventSubscriptionMapHeader* subscriptionMap = EventSubscriptionRegistry.GetSubscriptionMap(eventSubscriptionRegistryStorage);
                    eventTypeListenerListMap = AllocateEventTypeListenerListMap(subscriptionMap, TempAllocator);
                }

                EventSubscriptionRegistry.CopyTo(eventSubscriptionRegistryStorage, ref eventTypeListenerListMap, TempAllocator);

                // Process remainder

                EventSubscriptionRegistry.Subscribe(
                    ref eventTypeListenerListMap,
                    listenerDeclaredEventTypeSpanRO[processedCount..],
                    listenerEntity,
                    TempAllocator);

                return false;
            }

            return true;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void SubscribeAutoTempMap(
            ref UnsafeHashMap<TypeIndex, EventListenerListCapacityPair> eventTypeListenerListMap,
            Entity listenerEntity)
        {
            bool listenerExists = ListenerDeclaredEventTypeBufferLookup.EntityExists(listenerEntity);

            if (Hint.Unlikely(!listenerExists))
            {
                // Listener destroyed
                return;
            }

            UnsafeSpan<EventSettings.ListenerDeclaredEventTypeBufferElement> listenerDeclaredEventTypeSpanRO = ListenerDeclaredEventTypeBufferLookup[listenerEntity].AsSpanRO();

            EventSubscriptionRegistry.Subscribe(
                ref eventTypeListenerListMap,
                listenerDeclaredEventTypeSpanRO,
                listenerEntity,
                TempAllocator);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private bool SubscribeManualTryNoResize(
            DynamicBuffer<EventSubscriptionRegistry.StorageBufferElement> eventSubscriptionRegistryStorage,
            ref UnsafeHashMap<TypeIndex, EventListenerListCapacityPair> eventTypeListenerListMap,
            Entity listenerEntity,
            TypeIndex eventTypeIndex)
        {
            bool listenerExists = ListenerDeclaredEventTypeBufferLookup.EntityExists(listenerEntity);

            if (Hint.Unlikely(!listenerExists))
            {
                // Listener destroyed
                return true;
            }

            bool isFull = !EventSubscriptionRegistry.TrySubscribeNoResize(
                eventSubscriptionRegistryStorage,
                listenerEntity,
                eventTypeIndex);

            if (Hint.Unlikely(isFull))
            {
                // Full

                if (!eventTypeListenerListMap.IsCreated)
                {
                    EventSubscriptionMapHeader* subscriptionMap = EventSubscriptionRegistry.GetSubscriptionMap(eventSubscriptionRegistryStorage);
                    eventTypeListenerListMap = AllocateEventTypeListenerListMap(subscriptionMap, TempAllocator);
                }

                EventSubscriptionRegistry.CopyTo(eventSubscriptionRegistryStorage, ref eventTypeListenerListMap, TempAllocator);
                return false;
            }

            return true;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void SubscribeManualTempMap(
            ref UnsafeHashMap<TypeIndex, EventListenerListCapacityPair> eventTypeListenerListMap,
            Entity listenerEntity,
            TypeIndex eventTypeIndex)
        {
            bool listenerExists = ListenerDeclaredEventTypeBufferLookup.EntityExists(listenerEntity);

            if (Hint.Unlikely(!listenerExists))
            {
                // Listener destroyed
                return;
            }

            EventSubscriptionRegistry.Subscribe(
                ref eventTypeListenerListMap,
                listenerEntity,
                eventTypeIndex,
                TempAllocator);
        }
    }
}
