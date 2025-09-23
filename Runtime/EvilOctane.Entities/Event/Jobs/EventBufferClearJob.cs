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
    public struct EventBufferClearJob : IJobChunk
    {
        public BufferTypeHandle<EventBuffer.EntityElement> EventEntityBufferTypeHandle;
        public BufferTypeHandle<EventBuffer.TypeElement> EventTypeBufferTypeHandle;

        public AllocatorManager.AllocatorHandle TempAllocator;
        public EntityCommandBuffer.ParallelWriter CommandBuffer;

        public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
        {
            Assert.IsFalse(useEnabledMask);

            ClearEventEntityBuffer(in chunk, unfilteredChunkIndex);
            DynamicBufferUtility.ClearBuffersIgnoreFilter(in chunk, ref EventTypeBufferTypeHandle);
        }

        private void ClearEventEntityBuffer(in ArchetypeChunk chunk, int unfilteredChunkIndex)
        {
            BufferAccessor<EventBuffer.EntityElement> eventEntityBufferAccessor = chunk.GetBufferAccessorRO(ref EventEntityBufferTypeHandle);

            // Get Event Entities
            UnsafeList<Entity> entitiesToDestroyList = EntityDynamicBufferUtility.ExtractEntityList(eventEntityBufferAccessor, TempAllocator, clearBuffers: true);

            if (Hint.Likely(!entitiesToDestroyList.IsEmpty))
            {
                // Destroy Event Entities
                CommandBuffer.DestroyEntity(unfilteredChunkIndex, entitiesToDestroyList.AsSpan());
            }
        }
    }
}
