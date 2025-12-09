using Unity.Collections.LowLevel.Unsafe;

namespace EvilOctane.Entities.Internal
{
    public struct AssetData
    {
        public UnsafeText Name;
        public ulong TypeHash;
    }
}
