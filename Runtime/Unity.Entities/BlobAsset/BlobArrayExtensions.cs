using System.Runtime.CompilerServices;
using Unity.Collections.LowLevel.Unsafe;

namespace Unity.Entities
{
    public static unsafe partial class BlobArrayExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UnsafeSpan<T> AsSpan<T>(this ref BlobArray<T> self)
            where T : unmanaged
        {
            return new UnsafeSpan<T>((T*)self.GetUnsafePtr(), self.Length);
        }
    }
}
