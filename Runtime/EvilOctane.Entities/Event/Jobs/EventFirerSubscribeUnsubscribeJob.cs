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
        public BufferLookup<EventSetup.ListenerDeclaredEventTypeBufferElement> ListenerSetupDeclaredEventTypeBufferLookup;
        [ReadOnly]
        public BufferLookup<EventSettings.ListenerDeclaredEventTypeBufferElement> ListenerDeclaredEventTypeBufferLookup;

        public AllocatorManager.AllocatorHandle TempAllocator;

        public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
        {
            Assert.IsFalse(useEnabledMask);

            BufferAccessor<EventSubscriptionRegistry.StorageBufferElement> eventSubscriptionRegistryStorageBufferAccessor = chunk.GetBufferAccessorRW(ref EventSubscriptionRegistryStorageBufferTypeHandle);
            BufferAccessor<EventSubscriptionRegistry.SubscribeUnsubscribeBufferElement> eventSubscriptionRegistrySubscribeUnsubscribeBufferAccessor = chunk.GetBufferAccessorRW(ref EventSubscriptionRegistrySubscribeUnsubscribeBufferTypeHandle);

            UnsafeList<TypeIndex> typeIndexList = new();
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
                    ref typeIndexList,
                    ref eventTypeListenerListMap);

                if (Hint.Unlikely(isFull))
                {
                    // Execute on temp map
                    // Copy back the result

                    ExecuteTempMap(
                        eventSubscriptionRegistryStorage,
                        eventSubscriptionRegistrySubscribeUnsubscribeSpanRO,
                        ref typeIndexList,
                        ref eventTypeListenerListMap,
                        clearMap: entityIndex != chunk.Count - 1);
                }

                // Clear buffer
                eventSubscriptionRegistrySubscribeUnsubscribeBuffer.Clear();
            }
        }

        private readonly bool ListenerExistGetDeclaredEventTypes(ref UnsafeList<TypeIndex> typeIndexList, Entity listenerEntity, out UnsafeSpan<TypeIndex> declaredEventTypeSpanRO)
        {
            bool notSetUp = ListenerSetupDeclaredEventTypeBufferLookup.TryGetBuffer(listenerEntity, out DynamicBuffer<EventSetup.ListenerDeclaredEventTypeBufferElement> setupDeclaredEventTypeBuffer, out bool entityExists);

            if (Hint.Unlikely(!entityExists))
            {
                // Listener does not exist

                declaredEventTypeSpanRO = new();
                return false;
            }

            if (Hint.Unlikely(notSetUp))
            {
                // Listener not set up

                if (!typeIndexList.IsCreated)
                {
                    typeIndexList = UnsafeListExtensions2.Create<TypeIndex>(setupDeclaredEventTypeBuffer.Length, TempAllocator);
                }

                // Manual convert

                EventSetup.ToTypeIndexList(setupDeclaredEventTypeBuffer, ref typeIndexList);
                declaredEventTypeSpanRO = typeIndexList.AsSpan();
            }
            else
            {
                // Get declared Event Types

                DynamicBuffer<EventSettings.ListenerDeclaredEventTypeBufferElement> declaredEventTypeBuffer = ListenerDeclaredEventTypeBufferLookup[listenerEntity];
                declaredEventTypeSpanRO = declaredEventTypeBuffer.AsSpanRO().Reinterpret<TypeIndex>();
            }

            return true;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private readonly UnsafeHashMap<TypeIndex, EventListenerListCapacityPair> AllocateEventTypeListenerListMap(EventSubscriptionMapHeader* subscriptionMap)
        {
            int capacity = subscriptionMap->Count + 4;
            return UnsafeHashMapUtility.CreateHashMap<TypeIndex, EventListenerListCapacityPair>(capacity, 8, TempAllocator);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool ExecuteTryAddNoResize(
            DynamicBuffer<EventSubscriptionRegistry.StorageBufferElement> eventSubscriptionRegistryStorage,
            ref UnsafeSpan<EventSubscriptionRegistry.SubscribeUnsubscribeBufferElement> eventSubscriptionRegistrySubscribeUnsubscribeSpan,
            ref UnsafeList<TypeIndex> typeIndexList,
            ref UnsafeHashMap<TypeIndex, EventListenerListCapacityPair> eventTypeListenerListMap)
        {
            for (int index = 0; index != eventSubscriptionRegistrySubscribeUnsubscribeSpan.Length; ++index)
            {
                EventSubscriptionRegistry.SubscribeUnsubscribeBufferElement subscribeUnsubscribe = eventSubscriptionRegistrySubscribeUnsubscribeSpan[index];

                UnsafeSpan<TypeIndex> listenerDeclaredEventTypeSpanRO = new();

                bool listenerExist = subscribeUnsubscribe.Mode switch
                {
                    EventSubscriptionRegistry.SubscribeUnsubscribeMode.SubscribeAuto or
                    EventSubscriptionRegistry.SubscribeUnsubscribeMode.UnsubscribeAuto => ListenerExistGetDeclaredEventTypes(ref typeIndexList, subscribeUnsubscribe.ListenerEntity, out listenerDeclaredEventTypeSpanRO),
                    _ => ListenerDeclaredEventTypeBufferLookup.EntityExists(subscribeUnsubscribe.ListenerEntity)
                };

                if (Hint.Unlikely(!listenerExist))
                {
                    // Listener does not exist
                    continue;
                }

                bool isFull;

#pragma warning disable IDE0066
                switch (subscribeUnsubscribe.Mode)
                {
                    case EventSubscriptionRegistry.SubscribeUnsubscribeMode.SubscribeAuto:
                    default:
                        isFull = !SubscribeAutoTryNoResize(
                            eventSubscriptionRegistryStorage,
                            listenerDeclaredEventTypeSpanRO,
                            ref eventTypeListenerListMap,
                            subscribeUnsubscribe.ListenerEntity);

                        break;

                    case EventSubscriptionRegistry.SubscribeUnsubscribeMode.SubscribeManual:
                        isFull = !SubscribeManualTryNoResize(
                            eventSubscriptionRegistryStorage,
                            ref eventTypeListenerListMap,
                            subscribeUnsubscribe.ListenerEntity,
                            subscribeUnsubscribe.EventTypeIndex);

                        break;

                    case EventSubscriptionRegistry.SubscribeUnsubscribeMode.UnsubscribeAuto:
                        throw new NotImplementedException();

                    case EventSubscriptionRegistry.SubscribeUnsubscribeMode.UnsubscribeManual:
                        throw new NotImplementedException();
                }
#pragma warning restore IDE0066

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
            ref UnsafeList<TypeIndex> typeIndexList,
            ref UnsafeHashMap<TypeIndex, EventListenerListCapacityPair> eventTypeListenerListMap,
            bool clearMap)
        {
            for (int index = 0; index != eventSubscriptionRegistrySubscribeUnsubscribeSpan.Length; ++index)
            {
                EventSubscriptionRegistry.SubscribeUnsubscribeBufferElement subscribeUnsubscribe = eventSubscriptionRegistrySubscribeUnsubscribeSpan[index];

                UnsafeSpan<TypeIndex> listenerDeclaredEventTypeSpanRO = new();

                bool listenerExist = subscribeUnsubscribe.Mode switch
                {
                    EventSubscriptionRegistry.SubscribeUnsubscribeMode.SubscribeAuto or
                    EventSubscriptionRegistry.SubscribeUnsubscribeMode.UnsubscribeAuto => ListenerExistGetDeclaredEventTypes(ref typeIndexList, subscribeUnsubscribe.ListenerEntity, out listenerDeclaredEventTypeSpanRO),
                    _ => ListenerDeclaredEventTypeBufferLookup.EntityExists(subscribeUnsubscribe.ListenerEntity)
                };

                if (Hint.Unlikely(!listenerExist))
                {
                    // Listener does not exist
                    continue;
                }

                switch (subscribeUnsubscribe.Mode)
                {
                    case EventSubscriptionRegistry.SubscribeUnsubscribeMode.SubscribeAuto:
                    default:
                        EventSubscriptionRegistry.Subscribe(
                            ref eventTypeListenerListMap,
                            listenerDeclaredEventTypeSpanRO,
                            subscribeUnsubscribe.ListenerEntity,
                            TempAllocator);

                        break;

                    case EventSubscriptionRegistry.SubscribeUnsubscribeMode.SubscribeManual:
                        EventSubscriptionRegistry.Subscribe(
                            ref eventTypeListenerListMap,
                            subscribeUnsubscribe.ListenerEntity,
                            subscribeUnsubscribe.EventTypeIndex,
                            TempAllocator);

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
                    // Keep ptr / capacity
                    valueSpan.ElementAt(index).Clear();
                }
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private bool SubscribeAutoTryNoResize(
            DynamicBuffer<EventSubscriptionRegistry.StorageBufferElement> eventSubscriptionRegistryStorage,
            UnsafeSpan<TypeIndex> listenerDeclaredEventTypeSpanRO,
            ref UnsafeHashMap<TypeIndex, EventListenerListCapacityPair> eventTypeListenerListMap,
            Entity listenerEntity)
        {
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
                    eventTypeListenerListMap = AllocateEventTypeListenerListMap(subscriptionMap);
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
        private bool SubscribeManualTryNoResize(
            DynamicBuffer<EventSubscriptionRegistry.StorageBufferElement> eventSubscriptionRegistryStorage,
            ref UnsafeHashMap<TypeIndex, EventListenerListCapacityPair> eventTypeListenerListMap,
            Entity listenerEntity,
            TypeIndex eventTypeIndex)
        {
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
                    eventTypeListenerListMap = AllocateEventTypeListenerListMap(subscriptionMap);
                }

                EventSubscriptionRegistry.CopyTo(eventSubscriptionRegistryStorage, ref eventTypeListenerListMap, TempAllocator);
                return false;
            }

            return true;
        }
    }
}
