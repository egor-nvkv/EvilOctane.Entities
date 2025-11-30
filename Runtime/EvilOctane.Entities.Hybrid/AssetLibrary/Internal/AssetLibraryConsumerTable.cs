using EvilOctane.Collections;
using EvilOctane.Collections.LowLevel.Unsafe;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;

namespace EvilOctane.Entities.Internal
{
    public struct AssetLibraryConsumerTable
    {
        public UnsafeSwissTable<UnityObjectRef<AssetLibrary>, UnsafeList<Entity>, XXH3PodHasher<UnityObjectRef<AssetLibrary>>> Value;

        public AssetLibraryConsumerTable(int capacity, AllocatorManager.AllocatorHandle allocator)
        {
            Value = new UnsafeSwissTable<UnityObjectRef<AssetLibrary>, UnsafeList<Entity>, XXH3PodHasher<UnityObjectRef<AssetLibrary>>>(capacity, allocator);
        }
    }
}
