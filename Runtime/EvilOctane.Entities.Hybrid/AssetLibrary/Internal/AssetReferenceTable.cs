using EvilOctane.Collections;
using EvilOctane.Collections.LowLevel.Unsafe;
using Unity.Collections;
using Unity.Entities;
using UnityObject = UnityEngine.Object;

namespace EvilOctane.Entities.Internal
{
    public struct AssetReferenceTable
    {
        public UnsafeSwissTable<UnityObjectRef<UnityObject>, AssetReferenceData, XXH3PodHasher<UnityObjectRef<UnityObject>>> Value;

        public AssetReferenceTable(int capacity, AllocatorManager.AllocatorHandle allocator)
        {
            Value = new UnsafeSwissTable<UnityObjectRef<UnityObject>, AssetReferenceData, XXH3PodHasher<UnityObjectRef<UnityObject>>>(capacity, allocator);
        }
    }
}
