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
    public unsafe struct EntityOwnerBufferCleanupJobChunk<TEntityOwnerElement, TAliveTag> : IJobChunk
        where TEntityOwnerElement : unmanaged, IEntityOwnerBufferElementData
        where TAliveTag : unmanaged, ICleanupComponentsAliveTag
    {
        [ReadOnly]
        public EntityTypeHandle EntityTypeHandle;
        [ReadOnly]
        public ComponentLookup<TAliveTag> EntityLookup;

        public BufferTypeHandle<TEntityOwnerElement> EntityOwnerBufferTypeHandle;

        public AllocatorManager.AllocatorHandle TempAllocator;
        public EntityCommandBuffer CommandBuffer;

        public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
        {
            Assert.IsFalse(useEnabledMask);

            BufferAccessor<TEntityOwnerElement> entityOwnerBufferAccessor = chunk.GetBufferAccessorRW(ref EntityOwnerBufferTypeHandle);

            // Get Entities
            UnsafeList<Entity> entitiesToDestroyList = EntityOwner.ExtractAliveOwnedEntityList(entityOwnerBufferAccessor, EntityLookup, TempAllocator, clearBuffers: true);

            if (Hint.Likely(!entitiesToDestroyList.IsEmpty))
            {
                // Destroy
                CommandBuffer.DestroyEntity(entitiesToDestroyList.Ptr, entitiesToDestroyList.Length);
            }

            Entity* entityPtr = chunk.GetEntityDataPtrRO(EntityTypeHandle);

            // Clean up
            CommandBuffer.RemoveComponent<TEntityOwnerElement>(entityPtr, chunk.Count);
        }
    }
}
