using Unity.Assertions;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Entities;

namespace EvilOctane.Entities
{
    [BurstCompile]
    public struct ClearBuffersJob<TElement> : IJobChunk
        where TElement : unmanaged, IBufferElementData
    {
        public BufferTypeHandle<TElement> BufferTypeHandle;

        public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
        {
            Assert.IsFalse(useEnabledMask);

            BufferAccessor<TElement> bufferAccessor = chunk.GetBufferAccessorRW(ref BufferTypeHandle);
            DynamicBufferUtility.ClearAllBuffersInChunk(in chunk, ref bufferAccessor);
        }
    }
}
