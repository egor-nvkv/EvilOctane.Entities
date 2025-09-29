using Unity.Assertions;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Entities;

namespace EvilOctane.Entities
{
    [BurstCompile]
    public struct BufferClearJobChunk<TElement> : IJobChunk
        where TElement : unmanaged, IBufferElementData
    {
        public BufferTypeHandle<TElement> BufferTypeHandle;

        public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
        {
            Assert.IsFalse(useEnabledMask);

            DynamicBufferUtility.ClearAllBuffersInChunk(in chunk, ref BufferTypeHandle);
        }
    }
}
