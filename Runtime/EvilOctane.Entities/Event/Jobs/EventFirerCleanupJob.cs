using Unity.Assertions;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.LowLevel.Unsafe;
using static EvilOctane.Entities.Internal.EventAPIInternal;

namespace EvilOctane.Entities.Internal
{
    [BurstCompile]
    public unsafe struct EventFirerCleanupJob : IJobChunk
    {
        [ReadOnly]
        public EntityTypeHandle EntityTypeHandle;

        public EntityCommandBuffer.ParallelWriter CommandBuffer;

        public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
        {
            Assert.IsFalse(useEnabledMask);

            Entity* entityPtr = chunk.GetEntityDataPtrRO(EntityTypeHandle);

            // Remove cleanup components
            ComponentTypeSet componentTypeSet = GetEventFirerComponentTypeSet(includeIsAliveTag: false);
            CommandBuffer.RemoveComponent(unfilteredChunkIndex, entityPtr, chunk.Count, componentTypeSet);
        }
    }
}
