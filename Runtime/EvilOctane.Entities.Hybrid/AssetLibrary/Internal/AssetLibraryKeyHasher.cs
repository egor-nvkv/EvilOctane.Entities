using EvilOctane.Collections;
using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Mathematics;

namespace EvilOctane.Entities.Internal
{
    public unsafe struct AssetLibraryKeyHasher : IHasher64<AssetLibraryKey>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly ulong CalculateHash(AssetLibraryKey value)
        {
            uint2 hashLo = xxHash3.Hash64(value.AssetNameBytes, value.AssetNameLength);
            uint4 hashWide = new(hashLo, (uint)value.AssetTypeHash, (uint)(value.AssetTypeHash >> 32));

            uint2 hash = xxHash3.Hash64(&hashWide, sizeof(uint4));
            return hash.x | ((ulong)hash.y << 32);
        }
    }
}
