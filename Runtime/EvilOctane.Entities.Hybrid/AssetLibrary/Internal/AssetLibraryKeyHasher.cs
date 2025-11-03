using EvilOctane.Collections;
using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Mathematics;
using static System.Runtime.CompilerServices.Unsafe;

namespace EvilOctane.Entities.Internal
{
    public unsafe struct AssetLibraryKeyHasher : IHasher64<AssetLibraryKey>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly ulong CalculateHash(in AssetLibraryKey value)
        {
            uint2 nameHash = xxHash3.Hash64(value.AssetName.Ptr, value.AssetName.Length);
            return value.AssetTypeHash ^ ReadUnaligned<ulong>(&nameHash);
        }
    }
}
