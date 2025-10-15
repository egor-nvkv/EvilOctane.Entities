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

        private static bool HasCompactCommand(UnsafeSpan<EventFirer.EventSubscriptionRegistry.CommandBufferElement> commandSpan)
        {
            bool result = false;

            foreach (EventFirer.EventSubscriptionRegistry.CommandBufferElement command in commandSpan)
            {
                result |= command.Command == EventFirer.EventSubscriptionRegistry.Command.Compact;
            }

            return result;
        }

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

                Execute(
                    registryStorage,
                    registryCommandBuffer,
                    ref typeIndexList,
                    ref eventTypeListenerListMap,
                    clearTempMap: entityIndex != chunk.Count - 1);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
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

            if (typeIndexList.IsCreated)
            {
                typeIndexList.Clear();
                typeIndexList.EnsureCapacity(listenerEventStableTypeBuffer.Length, keepOldData: false);
            }
            else
            {
                typeIndexList = UnsafeListExtensions2.Create<TypeIndex>(listenerEventStableTypeBuffer.Length, TempAllocator);
            }

            // Manual convert

            int length = EventDeclarationFunctions.DeserializeEventTypes(listenerEventStableTypeBuffer.AsSpanRO(), typeIndexList.Ptr);
            listenerEventTypeSpanRO = new UnsafeSpan<TypeIndex>(typeIndexList.Ptr, length);

            return true;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private readonly UnsafeHashMap<TypeIndex, EventListenerListCapacityPair> AllocateEventTypeListenerListMap(EventListenerMapHeader* listenerMap)
        {
            int capacity = listenerMap->Count + 4;
            return UnsafeHashMapUtility.CreateHashMap<TypeIndex, EventListenerListCapacityPair>(capacity, 8, TempAllocator);
        }

        private void Execute(
            DynamicBuffer<EventFirerInternal.EventSubscriptionRegistry.Storage> registryStorage,
            DynamicBuffer<EventFirer.EventSubscriptionRegistry.CommandBufferElement> registryCommandBuffer,
            ref UnsafeList<TypeIndex> typeIndexList,
            ref UnsafeHashMap<TypeIndex, EventListenerListCapacityPair> eventTypeListenerListMap,
            bool clearTempMap)
        {
            UnsafeSpan<EventFirer.EventSubscriptionRegistry.CommandBufferElement> registryCommandSpanRO = registryCommandBuffer.AsSpanRO();

            bool hasCompactCommand = HasCompactCommand(registryCommandSpanRO);
            bool isInTempMapMode;

            if (Hint.Unlikely(hasCompactCommand))
            {
                // Copy existing data to temp map

                if (!eventTypeListenerListMap.IsCreated)
                {
                    EventListenerMapHeader* listenerMap = EventSubscriptionRegistryAPI.GetListenerMap(registryStorage);
                    eventTypeListenerListMap = AllocateEventTypeListenerListMap(listenerMap);
                }

                EventSubscriptionRegistryAPI.CopyToSkipDestroyed(registryStorage, ListenerEventTypeBufferLookup, ref eventTypeListenerListMap, TempAllocator);

                // In temp map mode
                isInTempMapMode = true;
            }
            else
            {
                // Execute over original storage

                bool isFull = !ExecuteInPlace(
                    registryStorage,
                    ref registryCommandSpanRO,
                    ref typeIndexList,
                    ref eventTypeListenerListMap);

                // In temp map mode when full
                isInTempMapMode = isFull;
            }

            if (isInTempMapMode)
            {
                // Execute on temp map
                // Copy back the result

                ExecuteOnTempMap(
                    registryCommandSpanRO,
                    ref typeIndexList,
                    ref eventTypeListenerListMap);

                // Copy back
                EventSubscriptionRegistryAPI.CopyFrom(registryStorage, ref eventTypeListenerListMap, compact: hasCompactCommand);

                // Clear map

                if (clearTempMap)
                {
                    UnsafeSpan<EventListenerListCapacityPair> valueSpan = eventTypeListenerListMap.GetHelperRef().GetValueSpan<EventListenerListCapacityPair>();

                    for (int index = 0; index != valueSpan.Length; ++index)
                    {
                        // Keep ptr / capacity
                        valueSpan.ElementAt(index).Clear();
                    }
                }
            }

            // Clear buffer
            registryCommandBuffer.Clear();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private bool ExecuteInPlace(
            DynamicBuffer<EventFirerInternal.EventSubscriptionRegistry.Storage> registryStorage,
            ref UnsafeSpan<EventFirer.EventSubscriptionRegistry.CommandBufferElement> registryCommandSpanRO,
            ref UnsafeList<TypeIndex> typeIndexList,
            ref UnsafeHashMap<TypeIndex, EventListenerListCapacityPair> eventTypeListenerListMap)
        {
            for (int index = 0; index != registryCommandSpanRO.Length; ++index)
            {
                EventFirer.EventSubscriptionRegistry.CommandBufferElement registryCommand = registryCommandSpanRO[index];

                if (registryCommand.Command == EventFirer.EventSubscriptionRegistry.Command.Compact)
                {
                    continue;
                }

                UnsafeSpan<TypeIndex> listenerDeclaredEventTypeSpanRO = new();

                switch (registryCommand.Command)
                {
                    case EventFirer.EventSubscriptionRegistry.Command.SubscribeAuto:
                    case EventFirer.EventSubscriptionRegistry.Command.UnsubscribeAuto:
                        bool isValid = TryGetListenerEventTypes(ref typeIndexList, registryCommand.ListenerEntity, out listenerDeclaredEventTypeSpanRO);

                        if (Hint.Unlikely(!isValid))
                        {
                            // Skip Listener
                            continue;
                        }

                        break;
                }

                bool isFull;

                switch (registryCommand.Command)
                {
                    case EventFirer.EventSubscriptionRegistry.Command.SubscribeAuto:
                    default:
                        isFull = !SubscribeAutoTryNoResize(
                            registryStorage,
                            ref eventTypeListenerListMap,
                            registryCommand.ListenerEntity,
                            listenerDeclaredEventTypeSpanRO);

                        break;

                    case EventFirer.EventSubscriptionRegistry.Command.SubscribeManual:
                        isFull = !SubscribeManualTryNoResize(
                            registryStorage,
                            ref eventTypeListenerListMap,
                            registryCommand.ListenerEntity,
                            registryCommand.EventTypeIndex);

                        break;

                    case EventFirer.EventSubscriptionRegistry.Command.UnsubscribeAuto:
                        EventSubscriptionRegistryAPI.Unsubscribe(
                            registryStorage,
                            registryCommand.ListenerEntity,
                            listenerDeclaredEventTypeSpanRO);

                        isFull = false;
                        break;

                    case EventFirer.EventSubscriptionRegistry.Command.UnsubscribeManual:
                        EventSubscriptionRegistryAPI.Unsubscribe(
                            registryStorage,
                            registryCommand.ListenerEntity,
                            registryCommand.EventTypeIndex);

                        isFull = false;
                        break;

                    case EventFirer.EventSubscriptionRegistry.Command.Compact:
                        throw new NotImplementedException();
                }

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
        private void ExecuteOnTempMap(
            UnsafeSpan<EventFirer.EventSubscriptionRegistry.CommandBufferElement> registryCommandSpanRO,
            ref UnsafeList<TypeIndex> typeIndexList,
            ref UnsafeHashMap<TypeIndex, EventListenerListCapacityPair> eventTypeListenerListMap)
        {
            for (int index = 0; index != registryCommandSpanRO.Length; ++index)
            {
                EventFirer.EventSubscriptionRegistry.CommandBufferElement registryCommand = registryCommandSpanRO[index];

                if (registryCommand.Command == EventFirer.EventSubscriptionRegistry.Command.Compact)
                {
                    continue;
                }

                UnsafeSpan<TypeIndex> listenerDeclaredEventTypeSpanRO = new();

                switch (registryCommand.Command)
                {
                    case EventFirer.EventSubscriptionRegistry.Command.SubscribeAuto:
                    case EventFirer.EventSubscriptionRegistry.Command.UnsubscribeAuto:
                        bool isValid = TryGetListenerEventTypes(ref typeIndexList, registryCommand.ListenerEntity, out listenerDeclaredEventTypeSpanRO);

                        if (Hint.Unlikely(!isValid))
                        {
                            // Skip Listener
                            continue;
                        }

                        break;
                }

                switch (registryCommand.Command)
                {
                    case EventFirer.EventSubscriptionRegistry.Command.SubscribeAuto:
                    default:
                        EventSubscriptionRegistryAPI.Subscribe(
                            ref eventTypeListenerListMap,
                            registryCommand.ListenerEntity,
                            listenerDeclaredEventTypeSpanRO,
                            TempAllocator);

                        break;

                    case EventFirer.EventSubscriptionRegistry.Command.SubscribeManual:
                        EventSubscriptionRegistryAPI.Subscribe(
                            ref eventTypeListenerListMap,
                            registryCommand.ListenerEntity,
                            registryCommand.EventTypeIndex,
                            TempAllocator);

                        break;

                    case EventFirer.EventSubscriptionRegistry.Command.UnsubscribeAuto:
                        EventSubscriptionRegistryAPI.Unsubscribe(
                            ref eventTypeListenerListMap,
                            registryCommand.ListenerEntity,
                            listenerDeclaredEventTypeSpanRO);

                        break;

                    case EventFirer.EventSubscriptionRegistry.Command.UnsubscribeManual:
                        EventSubscriptionRegistryAPI.Unsubscribe(
                            ref eventTypeListenerListMap,
                            registryCommand.ListenerEntity,
                            registryCommand.EventTypeIndex);

                        break;
                }
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private bool SubscribeAutoTryNoResize(
            DynamicBuffer<EventFirerInternal.EventSubscriptionRegistry.Storage> registryStorage,
            ref UnsafeHashMap<TypeIndex, EventListenerListCapacityPair> eventTypeListenerListMap,
            Entity listenerEntity,
            UnsafeSpan<TypeIndex> eventTypeIndexSpanRO)
        {
            bool isFull = !EventSubscriptionRegistryAPI.TrySubscribeNoResize(
                registryStorage,
                listenerEntity,
                eventTypeIndexSpanRO,
                out int processedCount);

            if (Hint.Unlikely(isFull))
            {
                // Copy existing data to temp map

                if (!eventTypeListenerListMap.IsCreated)
                {
                    EventListenerMapHeader* listenerMap = EventSubscriptionRegistryAPI.GetListenerMap(registryStorage);
                    eventTypeListenerListMap = AllocateEventTypeListenerListMap(listenerMap);
                }

                EventSubscriptionRegistryAPI.CopyToSkipDestroyed(registryStorage, ListenerEventTypeBufferLookup, ref eventTypeListenerListMap, TempAllocator);

                // Process remainder

                EventSubscriptionRegistryAPI.Subscribe(
                    ref eventTypeListenerListMap,
                    listenerEntity,
                    eventTypeIndexSpanRO[processedCount..],
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
            bool isFull = !EventSubscriptionRegistryAPI.TrySubscribeNoResize(
                registryStorage,
                listenerEntity,
                eventTypeIndex);

            if (Hint.Unlikely(isFull))
            {
                // Copy existing data to temp map

                if (!eventTypeListenerListMap.IsCreated)
                {
                    EventListenerMapHeader* listenerMap = EventSubscriptionRegistryAPI.GetListenerMap(registryStorage);
                    eventTypeListenerListMap = AllocateEventTypeListenerListMap(listenerMap);
                }

                EventSubscriptionRegistryAPI.CopyToSkipDestroyed(registryStorage, ListenerEventTypeBufferLookup, ref eventTypeListenerListMap, TempAllocator);

                // Process remainder

                EventSubscriptionRegistryAPI.Subscribe(
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
