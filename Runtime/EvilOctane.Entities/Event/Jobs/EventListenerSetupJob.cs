using Unity.Assertions;
using Unity.Burst;
using Unity.Burst.CompilerServices;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Entities.LowLevel.Unsafe;

namespace EvilOctane.Entities.Internal
{
    [BurstCompile]
    public unsafe struct EventListenerSetupJob : IJobChunk
    {
        [ReadOnly]
        public EntityTypeHandle EntityTypeHandle;

        [ReadOnly]
        public BufferTypeHandle<EventListener.EventDeclarationBuffer.StableTypeElement> EventStableTypeBufferTypeHandle;

        public AllocatorManager.AllocatorHandle TempAllocator;
        public EntityCommandBuffer.ParallelWriter CommandBuffer;

        public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
        {
            Assert.IsFalse(useEnabledMask);

            Entity* entityPtr = chunk.GetEntityDataPtrRO(EntityTypeHandle);

            // Remove setup component
            CommandBuffer.RemoveComponent<EventListener.EventDeclarationBuffer.StableTypeElement>(unfilteredChunkIndex, entityPtr, chunk.Count);

            // Add runtime components
            ComponentTypeSet componentTypeSet = EventSystemInternal.GetEventListenerComponentTypeSet();
            CommandBuffer.AddComponent(unfilteredChunkIndex, entityPtr, chunk.Count, componentTypeSet);

            // Setup runtime components

            BufferAccessor<EventListener.EventDeclarationBuffer.StableTypeElement> eventStableTypeBufferAccessor = chunk.GetBufferAccessorRO(ref EventStableTypeBufferTypeHandle);

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
    }
}
