using System;
using System.Runtime.CompilerServices;
using Unity.Assertions;
using Unity.Burst;
using Unity.Burst.CompilerServices;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using static System.Runtime.CompilerServices.Unsafe;
using EventListenerMapHeader = Unity.Collections.LowLevel.Unsafe.InlineHashMapHeader<Unity.Entities.TypeIndex>;

namespace EvilOctane.Entities.Internal
{
    [BurstCompile]
    public unsafe struct EventFirerProcessCommandsJob : IJobChunk
    {
        // Firer

        public BufferTypeHandle<EventFirerInternal.EventSubscriptionRegistry.Storage> SubscriptionRegistryStorageTypeHandle;
        public BufferTypeHandle<EventFirer.EventSubscriptionRegistry.CommandBufferElement> SubscriptionRegistryCommandBufferTypeHandle;

        // Listener

        [ReadOnly]
        public BufferLookup<EventListener.EventDeclarationBuffer.TypeElement> ListenerEventTypeBufferLookup;
        [ReadOnly]
        public BufferLookup<EventListener.EventDeclarationBuffer.StableTypeElement> ListenerEventStableTypeBufferLookup;

        public AllocatorManager.AllocatorHandle TempAllocator;

        public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
        {
            Assert.IsFalse(useEnabledMask);

            BufferAccessor<EventFirerInternal.EventSubscriptionRegistry.Storage> registryStorageAccessor = chunk.GetBufferAccessorRW(ref SubscriptionRegistryStorageTypeHandle);
            BufferAccessor<EventFirer.EventSubscriptionRegistry.CommandBufferElement> registryCommandBufferAccessor = chunk.GetBufferAccessorRW(ref SubscriptionRegistryCommandBufferTypeHandle);

            UnsafeList<TypeIndex> typeIndexList = new();
            UnsafeHashMap<TypeIndex, EventListenerListCapacityPair> eventTypeListenerListMap = new();

            for (int entityIndex = 0; entityIndex != chunk.Count; ++entityIndex)
            {
                DynamicBuffer<EventFirer.EventSubscriptionRegistry.CommandBufferElement> registryCommandBuffer = registryCommandBufferAccessor[entityIndex];

                if (registryCommandBuffer.IsEmpty)
                {
                    // Empty
                    continue;
                }

                DynamicBuffer<EventFirerInternal.EventSubscriptionRegistry.Storage> registryStorage = registryStorageAccessor[entityIndex];
                UnsafeSpan<EventFirer.EventSubscriptionRegistry.CommandBufferElement> registryCommandSpanRO = registryCommandBuffer.AsSpanRO();

                bool isFull = !ExecuteTryAddNoResize(
                    registryStorage,
                    ref registryCommandSpanRO,
                    ref typeIndexList,
                    ref eventTypeListenerListMap);

                if (Hint.Unlikely(isFull))
                {
                    // Execute on temp map
                    // Copy back the result

                    ExecuteTempMap(
                        registryStorage,
                        registryCommandSpanRO,
                        ref typeIndexList,
                        ref eventTypeListenerListMap,
                        clearMap: entityIndex != chunk.Count - 1);
                }

                // Clear buffer
                registryCommandBuffer.Clear();
            }
        }

        private readonly bool TryGetListenerEventTypes(ref UnsafeList<TypeIndex> typeIndexList, Entity listenerEntity, out UnsafeSpan<TypeIndex> listenerEventTypeSpanRO)
        {
            bool hasTypeBuffer = ListenerEventTypeBufferLookup.TryGetBuffer(listenerEntity, out DynamicBuffer<EventListener.EventDeclarationBuffer.TypeElement> listenerEventTypeBuffer, out bool entityExists);

            if (Hint.Unlikely(!entityExists))
            {
                // Listener does not exist

                SkipInit(out listenerEventTypeSpanRO);
                return false;
            }

            if (Hint.Likely(hasTypeBuffer))
            {
                // Get Event Type buffer

                listenerEventTypeSpanRO = listenerEventTypeBuffer.AsSpanRO().Reinterpret<TypeIndex>();
                return true;
            }

            bool hasStableTypeBuffer = ListenerEventStableTypeBufferLookup.TryGetBuffer(listenerEntity, out DynamicBuffer<EventListener.EventDeclarationBuffer.StableTypeElement> listenerEventStableTypeBuffer);

            if (Hint.Unlikely(!hasStableTypeBuffer))
            {
                // No Event Stable Type buffer either

                SkipInit(out listenerEventTypeSpanRO);
                return false;
            }

            // Listener not set up

            if (!typeIndexList.IsCreated)
            {
                typeIndexList = UnsafeListExtensions2.Create<TypeIndex>(listenerEventStableTypeBuffer.Length, TempAllocator);
            }

            // Manual convert

            EventDeclarationFunctions.DeserializeEventTypes(listenerEventStableTypeBuffer, ref typeIndexList);
            listenerEventTypeSpanRO = typeIndexList.AsSpan();

            return true;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private readonly UnsafeHashMap<TypeIndex, EventListenerListCapacityPair> AllocateEventTypeListenerListMap(EventListenerMapHeader* listenerMap)
        {
            int capacity = listenerMap->Count + 4;
            return UnsafeHashMapUtility.CreateHashMap<TypeIndex, EventListenerListCapacityPair>(capacity, 8, TempAllocator);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool ExecuteTryAddNoResize(
            DynamicBuffer<EventFirerInternal.EventSubscriptionRegistry.Storage> registryStorage,
            ref UnsafeSpan<EventFirer.EventSubscriptionRegistry.CommandBufferElement> registryCommandSpanRO,
            ref UnsafeList<TypeIndex> typeIndexList,
            ref UnsafeHashMap<TypeIndex, EventListenerListCapacityPair> eventTypeListenerListMap)
        {
            for (int index = 0; index != registryCommandSpanRO.Length; ++index)
            {
                EventFirer.EventSubscriptionRegistry.CommandBufferElement registryCommand = registryCommandSpanRO[index];

                UnsafeSpan<TypeIndex> listenerDeclaredEventTypeSpanRO = new();

                bool listenerIsValid = registryCommand.Command switch
                {
                    EventFirer.EventSubscriptionRegistry.Command.SubscribeAuto or
                    EventFirer.EventSubscriptionRegistry.Command.UnsubscribeAuto => TryGetListenerEventTypes(ref typeIndexList, registryCommand.ListenerEntity, out listenerDeclaredEventTypeSpanRO),
                    _ => ListenerEventTypeBufferLookup.EntityExists(registryCommand.ListenerEntity)
                };

                if (Hint.Unlikely(!listenerIsValid))
                {
                    // Skip Listener
                    continue;
                }

                bool isFull;

#pragma warning disable IDE0066
                switch (registryCommand.Command)
                {
                    case EventFirer.EventSubscriptionRegistry.Command.SubscribeAuto:
                    default:
                        isFull = !SubscribeAutoTryNoResize(
                            registryStorage,
                            listenerDeclaredEventTypeSpanRO,
                            ref eventTypeListenerListMap,
                            registryCommand.ListenerEntity);

                        break;

                    case EventFirer.EventSubscriptionRegistry.Command.SubscribeManual:
                        isFull = !SubscribeManualTryNoResize(
                            registryStorage,
                            ref eventTypeListenerListMap,
                            registryCommand.ListenerEntity,
                            registryCommand.EventTypeIndex);

                        break;

                    case EventFirer.EventSubscriptionRegistry.Command.UnsubscribeAuto:
                        throw new NotImplementedException();

                    case EventFirer.EventSubscriptionRegistry.Command.UnsubscribeManual:
                        throw new NotImplementedException();
                }
#pragma warning restore IDE0066

                if (Hint.Unlikely(isFull))
                {
                    // Full

                    registryCommandSpanRO = registryCommandSpanRO[index..];
                    return false;
                }
            }

            return true;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void ExecuteTempMap(
            DynamicBuffer<EventFirerInternal.EventSubscriptionRegistry.Storage> registryStorage,
            UnsafeSpan<EventFirer.EventSubscriptionRegistry.CommandBufferElement> registryCommandSpanRO,
            ref UnsafeList<TypeIndex> typeIndexList,
            ref UnsafeHashMap<TypeIndex, EventListenerListCapacityPair> eventTypeListenerListMap,
            bool clearMap)
        {
            for (int index = 0; index != registryCommandSpanRO.Length; ++index)
            {
                EventFirer.EventSubscriptionRegistry.CommandBufferElement registryCommand = registryCommandSpanRO[index];

                UnsafeSpan<TypeIndex> listenerDeclaredEventTypeSpanRO = new();

                bool listenerIsValid = registryCommand.Command switch
                {
                    EventFirer.EventSubscriptionRegistry.Command.SubscribeAuto or
                    EventFirer.EventSubscriptionRegistry.Command.UnsubscribeAuto => TryGetListenerEventTypes(ref typeIndexList, registryCommand.ListenerEntity, out listenerDeclaredEventTypeSpanRO),
                    _ => ListenerEventTypeBufferLookup.EntityExists(registryCommand.ListenerEntity)
                };

                if (Hint.Unlikely(!listenerIsValid))
                {
                    // Skip Listener
                    continue;
                }

                switch (registryCommand.Command)
                {
                    case EventFirer.EventSubscriptionRegistry.Command.SubscribeAuto:
                    default:
                        EventSubscriptionRegistryFunctions.Subscribe(
                            ref eventTypeListenerListMap,
                            registryCommand.ListenerEntity,
                            listenerDeclaredEventTypeSpanRO,
                            TempAllocator);

                        break;

                    case EventFirer.EventSubscriptionRegistry.Command.SubscribeManual:
                        EventSubscriptionRegistryFunctions.Subscribe(
                            ref eventTypeListenerListMap,
                            registryCommand.ListenerEntity,
                            registryCommand.EventTypeIndex,
                            TempAllocator);

                        break;

                    case EventFirer.EventSubscriptionRegistry.Command.UnsubscribeAuto:
                        throw new NotImplementedException();

                    case EventFirer.EventSubscriptionRegistry.Command.UnsubscribeManual:
                        throw new NotImplementedException();
                }
            }

            // Copy back
            EventSubscriptionRegistryFunctions.CopyFrom(registryStorage, ref eventTypeListenerListMap);

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
            DynamicBuffer<EventFirerInternal.EventSubscriptionRegistry.Storage> registryStorage,
            UnsafeSpan<TypeIndex> listenerDeclaredEventTypeSpanRO,
            ref UnsafeHashMap<TypeIndex, EventListenerListCapacityPair> eventTypeListenerListMap,
            Entity listenerEntity)
        {
            bool isFull = !EventSubscriptionRegistryFunctions.TrySubscribeNoResize(
                registryStorage,
                listenerEntity,
                listenerDeclaredEventTypeSpanRO,
                out int processedCount);

            if (Hint.Unlikely(isFull))
            {
                // Full

                if (!eventTypeListenerListMap.IsCreated)
                {
                    EventListenerMapHeader* listenerMap = EventSubscriptionRegistryFunctions.GetListenerMap(registryStorage);
                    eventTypeListenerListMap = AllocateEventTypeListenerListMap(listenerMap);
                }

                EventSubscriptionRegistryFunctions.CopyTo(registryStorage, ListenerEventTypeBufferLookup, ref eventTypeListenerListMap, TempAllocator);

                // Process remainder

                EventSubscriptionRegistryFunctions.Subscribe(
                    ref eventTypeListenerListMap,
                    listenerEntity,
                    listenerDeclaredEventTypeSpanRO[processedCount..],
                    TempAllocator);

                return false;
            }

            return true;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private bool SubscribeManualTryNoResize(
            DynamicBuffer<EventFirerInternal.EventSubscriptionRegistry.Storage> registryStorage,
            ref UnsafeHashMap<TypeIndex, EventListenerListCapacityPair> eventTypeListenerListMap,
            Entity listenerEntity,
            TypeIndex eventTypeIndex)
        {
            bool isFull = !EventSubscriptionRegistryFunctions.TrySubscribeNoResize(
                registryStorage,
                listenerEntity,
                eventTypeIndex);

            if (Hint.Unlikely(isFull))
            {
                // Full

                if (!eventTypeListenerListMap.IsCreated)
                {
                    EventListenerMapHeader* listenerMap = EventSubscriptionRegistryFunctions.GetListenerMap(registryStorage);
                    eventTypeListenerListMap = AllocateEventTypeListenerListMap(listenerMap);
                }

                EventSubscriptionRegistryFunctions.CopyTo(registryStorage, ListenerEventTypeBufferLookup, ref eventTypeListenerListMap, TempAllocator);

                // Process remainder

                EventSubscriptionRegistryFunctions.Subscribe(
                    ref eventTypeListenerListMap,
                    listenerEntity,
                    eventTypeIndex,
                    TempAllocator);

                return false;
            }

            return true;
        }
    }
}
