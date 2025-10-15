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
using static EvilOctane.Entities.EventAPI;
using static EvilOctane.Entities.Internal.EventAPIInternal;

namespace EvilOctane.Entities.Internal
{
    [BurstCompile]
    public unsafe struct EventListenerSetupJob : IJobChunk
    {
        [ReadOnly]
        public EntityTypeHandle EntityTypeHandle;

        // Firer

        [ReadOnly]
        public BufferLookup<EventFirer.EventDeclarationBuffer.StableTypeElement> FirerStableTypeBufferTypeHandle;
        [ReadOnly]
        public BufferLookup<EventFirer.EventSubscriptionRegistry.CommandBufferElement> FirerEventCommandBufferLookup;

        // Listener

        [ReadOnly]
        public BufferTypeHandle<EventListener.EventDeclarationBuffer.StableTypeElement> ListenerStableTypeBufferTypeHandle;

        public BufferTypeHandle<EventListener.EventSubscribeBuffer.SubscribeAutoElement> ListenerSubscribeAutoBufferTypeHandle;

        public AllocatorManager.AllocatorHandle TempAllocator;
        public EntityCommandBuffer.ParallelWriter CommandBuffer;

        public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
        {
            Assert.IsFalse(useEnabledMask);

            Entity* entityPtr = chunk.GetEntityDataPtrRO(EntityTypeHandle);

            if (chunk.Has<EventListener.EventDeclarationBuffer.StableTypeElement>())
            {
                SetupRuntimeComponents(in chunk, unfilteredChunkIndex, entityPtr);
            }
            // It is unlikely that firers will be ready the same frame we set up, so "else if"
            else if (chunk.Has<EventListener.EventSubscribeBuffer.SubscribeAutoElement>())
            {
                SetupSubscribeCommands(in chunk, unfilteredChunkIndex, entityPtr);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void SetupRuntimeComponents(
            in ArchetypeChunk chunk,
            int unfilteredChunkIndex,
            Entity* entityPtr)
        {
            // Remove setup component
            CommandBuffer.RemoveComponent<EventListener.EventDeclarationBuffer.StableTypeElement>(unfilteredChunkIndex, entityPtr, chunk.Count);

            // Add runtime components
            ComponentTypeSet componentTypeSet = GetEventListenerComponentTypeSet();
            CommandBuffer.AddComponent(unfilteredChunkIndex, entityPtr, chunk.Count, componentTypeSet);

            // Setup runtime components

            BufferAccessor<EventListener.EventDeclarationBuffer.StableTypeElement> eventStableTypeBufferAccessor = chunk.GetBufferAccessorRO(ref ListenerStableTypeBufferTypeHandle);

            for (int entityIndex = 0; entityIndex != chunk.Count; ++entityIndex)
            {
                Entity entity = entityPtr[entityIndex];
                DynamicBuffer<EventListener.EventDeclarationBuffer.StableTypeElement> eventStableTypeBuffer = eventStableTypeBufferAccessor[entityIndex];

                SetupEventListener(
                    unfilteredChunkIndex,
                    entity,
                    eventStableTypeBuffer.AsSpanRO());
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void SetupSubscribeCommands(
            in ArchetypeChunk chunk,
            int unfilteredChunkIndex,
            Entity* entityPtr)
        {
            BufferAccessor<EventListener.EventSubscribeBuffer.SubscribeAutoElement> eventSubscribeAutoBufferAccessor = chunk.GetBufferAccessorRW(ref ListenerSubscribeAutoBufferTypeHandle);

            for (int entityIndex = 0; entityIndex != chunk.Count; ++entityIndex)
            {
                Entity entity = entityPtr[entityIndex];
                DynamicBuffer<EventListener.EventSubscribeBuffer.SubscribeAutoElement> subscribeAutoBuffer = eventSubscribeAutoBufferAccessor[entityIndex];

                SetupSubscribeCommands(
                    unfilteredChunkIndex,
                    entity,
                    subscribeAutoBuffer);
            }
        }

        private void SetupEventListener(
            int sortKey,
            Entity entity,
            UnsafeSpan<EventListener.EventDeclarationBuffer.StableTypeElement> eventStableTypeSpanRO)
        {
            DynamicBuffer<EventListener.EventDeclarationBuffer.TypeElement> eventTypeBuffer = CommandBuffer.SetBuffer<EventListener.EventDeclarationBuffer.TypeElement>(sortKey, entity);
            eventTypeBuffer.EnsureCapacityTrashOldData(eventStableTypeSpanRO.Length);

            foreach (EventListener.EventDeclarationBuffer.StableTypeElement eventStableType in eventStableTypeSpanRO)
            {
                bool typeIndexFound = TypeManager.TryGetTypeIndexFromStableTypeHash(eventStableType.EventStableTypeHash, out TypeIndex typeIndex);

                if (Hint.Unlikely(!typeIndexFound))
                {
                    // Type Index not found
                    continue;
                }

                // Register Event Type

                bool alreadyRegistered = eventTypeBuffer.AsSpanRO().Reinterpret<TypeIndex>().Contains(typeIndex);

                if (Hint.Unlikely(alreadyRegistered))
                {
                    // Already registered
                    continue;
                }

                // Register

                _ = eventTypeBuffer.AddNoResize(new EventListener.EventDeclarationBuffer.TypeElement()
                {
                    EventTypeIndex = typeIndex
                });
            }
        }

        private void SetupSubscribeCommands(
           int sortKey,
           Entity entity,
           DynamicBuffer<EventListener.EventSubscribeBuffer.SubscribeAutoElement> subscribeAutoBuffer)
        {
            for (int index = 0; index != subscribeAutoBuffer.Length;)
            {
                EventListener.EventSubscribeBuffer.SubscribeAutoElement subscribe = subscribeAutoBuffer[index];

                if (FirerEventCommandBufferLookup.HasBuffer(subscribe.EventFirerEntity))
                {
                    // Ready
                    SubscribeAuto(CommandBuffer, sortKey, subscribe.EventFirerEntity, entity);
                    goto Remove;
                }

                if (Hint.Likely(FirerStableTypeBufferTypeHandle.HasBuffer(subscribe.EventFirerEntity)))
                {
                    // Not ready
                    ++index;
                    continue;
                }
                else
                {
                    // Invalid
                    goto Remove;
                }

            Remove:
                subscribeAutoBuffer.RemoveAtSwapBack(index);
            }

            // Cleanup

            if (subscribeAutoBuffer.IsEmpty)
            {
                CommandBuffer.RemoveComponent<EventListener.EventSubscribeBuffer.SubscribeAutoElement>(sortKey, entity);
            }
        }
    }
}
