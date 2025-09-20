using Unity.Assertions;
using Unity.Burst;
using Unity.Burst.CompilerServices;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Entities.LowLevel.Unsafe;

namespace EvilOctane.Entities
{
    [BurstCompile]
    public unsafe struct EntityOwnerBufferCleanupJobChunk<TEntityOwnerElement, TAllocatedTag> : IJobChunk
        where TEntityOwnerElement : unmanaged, IEntityOwnerBufferElementData
        where TAllocatedTag : unmanaged, ICleanupComponentAllocatedTag
    {
        [ReadOnly]
        public EntityTypeHandle EntityTypeHandle;
        [ReadOnly]
        public ComponentLookup<TAllocatedTag> EntityLookup;

        public BufferTypeHandle<TEntityOwnerElement> EntityOwnerBufferTypeHandle;

        public AllocatorManager.AllocatorHandle TempAllocator;
        public EntityCommandBuffer CommandBuffer;

        public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
        {
            Assert.IsFalse(useEnabledMask);

            BufferAccessor<TEntityOwnerElement> entityOwnerBufferAccessor = chunk.GetBufferAccessorRW(ref EntityOwnerBufferTypeHandle);

            // Get Entities
            // Don't clear Buffers as the Entities holding them are already [scheduled to be] destroyed
            UnsafeList<Entity> entitiesToDestroyList = EntityBufferUtility.ExtractAliveEntityList(entityOwnerBufferAccessor, EntityLookup, TempAllocator, clearBuffers: false);

            if (Hint.Likely(!entitiesToDestroyList.IsEmpty))
            {
                // Destroy
                CommandBuffer.DestroyEntity(entitiesToDestroyList.AsSpan());
            }

            Entity* entityPtr = chunk.GetNativeArray(EntityTypeHandle).GetReadOnlyPtr();

            // Clean up
            CommandBuffer.RemoveComponent<TEntityOwnerElement>(entityPtr, chunk.Count);
        }
    }
}
