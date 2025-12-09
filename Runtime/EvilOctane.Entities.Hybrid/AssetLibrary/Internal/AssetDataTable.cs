using EvilOctane.Collections;
using EvilOctane.Collections.LowLevel.Unsafe;
using Unity.Collections;
using Unity.Entities;
using UnityObject = UnityEngine.Object;

namespace EvilOctane.Entities.Internal
{
    public struct AssetDataTable
    {
        public UnsafeSwissTable<UnityObjectRef<UnityObject>, AssetData, XXH3PodHasher<UnityObjectRef<UnityObject>>> Value;

        public AssetDataTable(int capacity, AllocatorManager.AllocatorHandle allocator)
        {
            Value = new UnsafeSwissTable<UnityObjectRef<UnityObject>, AssetData, XXH3PodHasher<UnityObjectRef<UnityObject>>>(capacity, allocator);
        }
    }
}
