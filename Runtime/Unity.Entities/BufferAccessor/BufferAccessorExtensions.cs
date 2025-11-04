using System.Runtime.CompilerServices;

namespace Unity.Entities
{
    public static partial class BufferAccessorExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static nint GetTotalElementCount<T>(this BufferAccessor<T> self)
            where T : unmanaged, IBufferElementData
        {
            nint totalElementCount = 0;

            for (int index = 0; index != self.Length; ++index)
            {
                totalElementCount += self[index].Length;
            }

            return totalElementCount;
        }
    }
}
