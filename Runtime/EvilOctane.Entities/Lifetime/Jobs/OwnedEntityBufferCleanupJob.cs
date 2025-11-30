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
    public unsafe struct OwnedEntityBufferCleanupJob<TOwnedEntityBufferElement, TAliveTag> : IJobChunk
        where TOwnedEntityBufferElement : unmanaged, IOwnedEntityBufferElementData
        where TAliveTag : unmanaged, ICleanupComponentsAliveTag
    {
        [ReadOnly]
        public EntityTypeHandle EntityTypeHandle;
        [ReadOnly]
        public ComponentLookup<TAliveTag> EntityLookup;

        public BufferTypeHandle<TOwnedEntityBufferElement> OwnedEntityBufferTypeHandle;

        public AllocatorManager.AllocatorHandle TempAllocator;
        public EntityCommandBuffer CommandBuffer;

        public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
        {
            Assert.IsFalse(useEnabledMask);

            BufferAccessor<TOwnedEntityBufferElement> ownedEntityBufferAccessor = chunk.GetBufferAccessorRW(ref OwnedEntityBufferTypeHandle);

            // Get entities
            UnsafeList<Entity> toDestroyList = EntityOwnerAPI.ExtractAliveOwnedEntityList(ref ownedEntityBufferAccessor, ref EntityLookup, TempAllocator, clearBuffers: true);

            if (Hint.Likely(!toDestroyList.IsEmpty))
            {
                // Destroy
                CommandBuffer.DestroyEntity(toDestroyList.Ptr, toDestroyList.Length);
            }

            Entity* entityPtr = chunk.GetEntityDataPtrRO(EntityTypeHandle);

            // Clean up
            CommandBuffer.RemoveComponent<TOwnedEntityBufferElement>(entityPtr, chunk.Count);
        }
    }
}
