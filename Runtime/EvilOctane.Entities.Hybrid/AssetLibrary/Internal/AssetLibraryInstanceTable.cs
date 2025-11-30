using EvilOctane.Collections;
using EvilOctane.Collections.LowLevel.Unsafe;
using Unity.Collections;
using Unity.Entities;

namespace EvilOctane.Entities.Internal
{
    public struct AssetLibraryInstanceTable
    {
        public UnsafeSwissTable<UnityObjectRef<AssetLibrary>, Entity, XXH3PodHasher<UnityObjectRef<AssetLibrary>>> Value;

        public AssetLibraryInstanceTable(int capacity, AllocatorManager.AllocatorHandle allocator)
        {
            Value = new UnsafeSwissTable<UnityObjectRef<AssetLibrary>, Entity, XXH3PodHasher<UnityObjectRef<AssetLibrary>>>(capacity, allocator);
        }
    }
}
