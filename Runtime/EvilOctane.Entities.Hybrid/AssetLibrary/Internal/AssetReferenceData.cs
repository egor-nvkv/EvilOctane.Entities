using Unity.Collections.LowLevel.Unsafe;

namespace EvilOctane.Entities.Internal
{
    public struct AssetReferenceData
    {
        public UnsafeText Name;
        public ulong TypeHash;
    }
}
