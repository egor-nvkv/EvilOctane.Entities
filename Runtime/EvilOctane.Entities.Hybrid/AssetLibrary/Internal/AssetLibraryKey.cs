using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Unity.Burst.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using static System.Runtime.CompilerServices.Unsafe;

namespace EvilOctane.Entities.Internal
{
    [StructLayout(LayoutKind.Sequential)]
    public struct AssetLibraryKey : IEquatable<AssetLibraryKey>
    {
        public ulong AssetTypeHash;
        public ByteSpan AssetName;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public AssetLibraryKey(ulong assetTypeHash, ByteSpan assetName)
        {
            AssetTypeHash = assetTypeHash;
            AssetName = assetName;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool Equals(AssetLibraryKey other)
        {
            return
                AssetTypeHash == other.AssetTypeHash &&
                AssetName == other.AssetName;
        }

        public override readonly string ToString()
        {
            return ToFixedString().ToString();
        }

        [SkipLocalsInit]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly FixedString512Bytes ToFixedString()
        {
            SkipInit(out FixedString512Bytes result);

            _ = result.Append((FixedString32Bytes)"(TypeHash=");
            _ = result.Append(AssetTypeHash);
            _ = result.Append((FixedString32Bytes)", Name=");
            result.AppendTruncateUnchecked(AssetName);
            _ = result.AppendRawByte((byte)')');

            return result;
        }
    }
}
