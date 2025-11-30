using Unity.Assertions;
using Unity.Burst;
using Unity.Burst.CompilerServices;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.LowLevel.Unsafe;
using static Unity.Collections.LowLevel.Unsafe.UnsafeUtility2;

namespace EvilOctane.Entities
{
    [BurstCompile]
    public unsafe struct OwnerEntityCleanupJob<TOwnerEntityComponent, TOwnedEntityBufferElement> : IJobChunk
        where TOwnerEntityComponent : unmanaged, IOwnerEntityComponentData
        where TOwnedEntityBufferElement : unmanaged, IOwnedEntityBufferElementData
    {
        [ReadOnly]
        public EntityTypeHandle EntityTypeHandle;

        [ReadOnly]
        public ComponentTypeHandle<TOwnerEntityComponent> OwnerEntityComponentTypeHandle;

        public BufferLookup<TOwnedEntityBufferElement> OwnedEntityBufferLookup;

        public EntityCommandBuffer CommandBuffer;

        public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
        {
            CheckReinterpretArgs<TOwnedEntityBufferElement, Entity>();
            Assert.IsFalse(useEnabledMask);

            Entity* entityPtr = chunk.GetEntityDataPtrRO(EntityTypeHandle);
            TOwnerEntityComponent* ownerEntityPtrRO = chunk.GetRequiredComponentDataPtrROTyped(ref OwnerEntityComponentTypeHandle);

            for (int entityIndex = 0; entityIndex != chunk.Count; ++entityIndex)
            {
                Entity ownerEntity = ownerEntityPtrRO[entityIndex].OwnerEntity;

                if (Hint.Unlikely(!OwnedEntityBufferLookup.TryGetBuffer(ownerEntity, out DynamicBuffer<TOwnedEntityBufferElement> ownedEntityBuffer)))
                {
                    // Component missing
                    continue;
                }

                // Unregister from owner
                Entity entity = entityPtr[entityIndex];
                _ = ownedEntityBuffer.Reinterpret<Entity>().RemoveFirstMatchSwapBack(entity);
            }

            // Clean up
            CommandBuffer.RemoveComponent<TOwnerEntityComponent>(entityPtr, chunk.Count);
        }
    }
}
