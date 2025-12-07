using EvilOctane.Collections;
using EvilOctane.Collections.LowLevel.Unsafe;
using Unity.Collections;
using Unity.Entities;
using UnityObject = UnityEngine.Object;

namespace EvilOctane.Entities.Internal
{
    public struct AssetInstanceTable
    {
        public UnsafeSwissTable<UnityObjectRef<UnityObject>, Entity, XXH3PodHasher<UnityObjectRef<UnityObject>>> Value;

        public AssetInstanceTable(int capacity, AllocatorManager.AllocatorHandle allocator)
        {
            Value = new UnsafeSwissTable<UnityObjectRef<UnityObject>, Entity, XXH3PodHasher<UnityObjectRef<UnityObject>>>(capacity, allocator);
        }
    }
}
