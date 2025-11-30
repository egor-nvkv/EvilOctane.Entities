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
    public unsafe struct EventFirerClearEntityBufferJob : IJobChunk
    {
        public BufferTypeHandle<EventFirer.EventBuffer.EntityElement> EventEntityBufferTypeHandle;

        public AllocatorManager.AllocatorHandle TempAllocator;
        public EntityCommandBuffer.ParallelWriter CommandBuffer;

        public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
        {
            Assert.IsFalse(useEnabledMask);

            BufferAccessor<EventFirer.EventBuffer.EntityElement> eventEntityBufferAccessor = chunk.GetBufferAccessorRW(ref EventEntityBufferTypeHandle);

            // Get Entities
            UnsafeList<Entity> entitiesToDestroyList = EntityOwnerAPI.ExtractOwnedEntityList(ref eventEntityBufferAccessor, TempAllocator, clearBuffers: true);

            if (Hint.Likely(!entitiesToDestroyList.IsEmpty))
            {
                // Destroy
                CommandBuffer.DestroyEntity(unfilteredChunkIndex, entitiesToDestroyList.Ptr, entitiesToDestroyList.Length);
            }
        }
    }
}
