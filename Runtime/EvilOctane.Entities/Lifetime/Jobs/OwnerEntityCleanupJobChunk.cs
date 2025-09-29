using Unity.Assertions;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.LowLevel.Unsafe;

namespace EvilOctane.Entities
{
    [BurstCompile]
    public unsafe struct OwnerEntityCleanupJobChunk<TOwnerEntityComponent, TEntityOwnerElement> : IJobChunk
        where TOwnerEntityComponent : unmanaged, IOwnerEntityComponentData
        where TEntityOwnerElement : unmanaged, IEntityOwnerBufferElementData
    {
        [ReadOnly]
        public EntityTypeHandle EntityTypeHandle;

        [ReadOnly]
        public ComponentTypeHandle<TOwnerEntityComponent> OwnerEntityComponentTypeHandle;

        public BufferLookup<TEntityOwnerElement> EntityOwnerBufferLookup;

        public EntityCommandBuffer CommandBuffer;

        public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
        {
            Assert.IsFalse(useEnabledMask);

            Entity* entityPtr = chunk.GetEntityDataPtrRO(EntityTypeHandle);
            Entity* ownerEntityPtr = chunk.GetComponentDataPtrROReinterpret<TOwnerEntityComponent, Entity>(ref OwnerEntityComponentTypeHandle);

            for (int entityIndex = 0; entityIndex != chunk.Count; ++entityIndex)
            {
                Entity ownerEntity = ownerEntityPtr[entityIndex];

                if (!EntityOwnerBufferLookup.TryGetBuffer(ownerEntity, out DynamicBuffer<TEntityOwnerElement> entityOwnerBuffer))
                {
                    // Owner (or buffer) destroyed
                    continue;
                }

                Entity entity = entityPtr[entityIndex];

                // Unregister from Owner Buffer
                _ = entityOwnerBuffer.Reinterpret<Entity>().RemoveFirstMatchSwapBack(entity);
            }

            // Clean up
            CommandBuffer.RemoveComponent<TOwnerEntityComponent>(entityPtr, chunk.Count);
        }
    }
}
