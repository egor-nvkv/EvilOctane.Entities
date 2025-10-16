using Unity.Assertions;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Entities.LowLevel.Unsafe;
using static EvilOctane.Entities.Internal.EventSubscriptionRegistryAPI;

namespace EvilOctane.Entities
{
    [BurstCompile]
    internal unsafe struct EventFirerBakeJob : IJobChunk
    {
        [ReadOnly]
        public EntityTypeHandle EntityTypeHandle;

        [ReadOnly]
        public BufferTypeHandle<EventFirerDeclaredEventTypeBufferElement> EventTypeBufferTypeHandle;

        public AllocatorManager.AllocatorHandle TempAllocator;
        public EntityCommandBuffer.ParallelWriter CommandBuffer;

        public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
        {
            Assert.IsFalse(useEnabledMask);

            Entity* entityPtr = chunk.GetEntityDataPtrRO(EntityTypeHandle);
            BufferAccessor<EventFirerDeclaredEventTypeBufferElement> eventTypeBufferAccessor = chunk.GetBufferAccessorRO(ref EventTypeBufferTypeHandle);

            UnsafeList<TypeIndex> eventTypeList = UnsafeListExtensions2.Create<TypeIndex>(16, TempAllocator);

            for (int entityIndex = 0; entityIndex != chunk.Count; ++entityIndex)
            {
                DynamicBuffer<EventFirerDeclaredEventTypeBufferElement> eventTypeBuffer = eventTypeBufferAccessor[entityIndex];

                // Get unique
                eventTypeBuffer.AsSpanRO().Reinterpret<TypeIndex>().GetUnique(ref eventTypeList);

                // Create stable buffer

                Entity entity = entityPtr[entityIndex];

                DynamicBuffer<EventFirer.EventDeclarationBuffer.StableTypeElement> stableTypeBuffer = CommandBuffer.AddBuffer<EventFirer.EventDeclarationBuffer.StableTypeElement>(unfilteredChunkIndex, entity);
                stableTypeBuffer.EnsureCapacityTrashOldData(eventTypeList.Length);

                foreach (TypeIndex typeIndex in eventTypeList)
                {
                    _ = stableTypeBuffer.AddNoResize(new()
                    {
                        EventStableTypeHash = TypeManager.GetTypeInfo(typeIndex).StableTypeHash,
                        ListenerListInitialCapacity = ListenerListDefaultInitialCapacity
                    });
                }
            }

            // Cleanup
            CommandBuffer.RemoveComponent<EventFirerDeclaredEventTypeBufferElement>(unfilteredChunkIndex, entityPtr, chunk.Count);
        }
    }
}
