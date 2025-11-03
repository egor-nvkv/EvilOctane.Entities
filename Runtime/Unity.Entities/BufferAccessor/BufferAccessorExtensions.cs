using System.Runtime.CompilerServices;

namespace Unity.Entities
{
    public static partial class BufferAccessorExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint GetTotalElementCount<T>(this BufferAccessor<T> self)
            where T : unmanaged, IBufferElementData
        {
            uint totalElementCount = 0;

            for (int index = 0; index != self.Length; ++index)
            {
                totalElementCount += (uint)self[index].Length;
            }

            return totalElementCount;
        }
    }
}
