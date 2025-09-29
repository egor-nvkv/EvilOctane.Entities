using System.Runtime.CompilerServices;
using Unity.Entities;

namespace EvilOctane.Entities
{
    public static partial class DynamicBufferUtility
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ClearAllBuffersInChunk<T>(in ArchetypeChunk chunk, ref BufferTypeHandle<T> bufferTypeHandle)
            where T : unmanaged, IBufferElementData
        {
            BufferAccessor<T> bufferAccessor = chunk.GetBufferAccessorRW(ref bufferTypeHandle);

            for (int index = 0; index != chunk.Count; ++index)
            {
                DynamicBuffer<T> buffer = bufferAccessor[index];
                buffer.Clear();
            }
        }
    }
}
