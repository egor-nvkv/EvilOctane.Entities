using System;
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
    public unsafe struct EventFirerCleanupJob : IJobChunk
    {
        [ReadOnly]
        public EntityTypeHandle EntityTypeHandle;
        [ReadOnly]
        public ComponentLookup<CleanupComponentsAliveTag> EntityLookup;

        public BufferTypeHandle<EventBuffer.EntityElement> EventEntityBufferTypeHandle;

        public AllocatorManager.AllocatorHandle TempAllocator;
        public EntityCommandBuffer.ParallelWriter CommandBuffer;

        public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
        {
            Assert.IsFalse(useEnabledMask);

            // Event Entity Buffer

            if (Hint.Likely(chunk.Has<EventBuffer.EntityElement>()))
            {
                BufferAccessor<EventBuffer.EntityElement> eventEntityBufferAccessor = chunk.GetBufferAccessorRW(ref EventEntityBufferTypeHandle);

                // Get Entities
                UnsafeList<Entity> entitiesToDestroyList = EntityOwner.ExtractAliveOwnedEntityList(eventEntityBufferAccessor, EntityLookup, TempAllocator, clearBuffers: true);

                if (Hint.Likely(!entitiesToDestroyList.IsEmpty))
                {
                    // Destroy
                    CommandBuffer.DestroyEntity(unfilteredChunkIndex, entitiesToDestroyList.AsSpan());
                }
            }

            Entity* entityPtr = chunk.GetEntityDataPtrRO(EntityTypeHandle);

            // Clean up

            ComponentTypeSet componentTypeSet = EventSystem.GetEventFirerComponentTypeSet(includeAllocatedTag: false);
            CommandBuffer.RemoveComponent(unfilteredChunkIndex, entityPtr, chunk.Count, componentTypeSet);
        }
    }
}
