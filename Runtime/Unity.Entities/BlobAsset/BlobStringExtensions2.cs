using System.Runtime.CompilerServices;
using Unity.Burst.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using static System.Runtime.CompilerServices.Unsafe;

namespace Unity.Entities
{
    public static unsafe partial class BlobStringExtensions2
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ByteSpan AsByteSpan(this ref BlobString self)
        {
            return new ByteSpan((byte*)self.Data.GetUnsafePtr(), self.Length);
        }

        [SkipLocalsInit]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T ToFixedStringTruncateUnchecked<T>(this ref BlobString self)
            where T : unmanaged, INativeList<byte>, IUTF8Bytes
        {
            SkipInit(out T result);

            int length = math.min(self.Length, result.Capacity);
            result.Length = length;

            new ByteSpan(result.GetUnsafePtr(), length).CopyFrom(self.AsByteSpan());
            return result;
        }
    }
}
