using EvilOctane.Collections;
using EvilOctane.Collections.LowLevel.Unsafe;
using Unity.Collections;
using Unity.Entities;

namespace EvilOctane.Entities.Internal
{
    public struct AssetLibraryRebakedSet
    {
        public UnsafeSwissSet<UnityObjectRef<AssetLibrary>, XXH3PodHasher<UnityObjectRef<AssetLibrary>>> Value;

        public AssetLibraryRebakedSet(int capacity, AllocatorManager.AllocatorHandle allocator)
        {
            Value = new UnsafeSwissSet<UnityObjectRef<AssetLibrary>, XXH3PodHasher<UnityObjectRef<AssetLibrary>>>(capacity, allocator);
        }
    }
}
