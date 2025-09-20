using System.Runtime.CompilerServices;

namespace Unity.Entities
{
    public static partial class BufferAccessorExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetTotalElementCount<T>(this BufferAccessor<T> self)
            where T : unmanaged, IBufferElementData
        {
            int totalElementCount = 0;

            for (int index = 0; index != self.Length; ++index)
            {
                totalElementCount += self[index].Length;
            }

            return totalElementCount;
        }
    }
}
