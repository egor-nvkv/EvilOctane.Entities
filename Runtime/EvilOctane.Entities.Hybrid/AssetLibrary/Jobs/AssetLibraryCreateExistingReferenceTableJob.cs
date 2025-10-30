using Unity.Burst;
using Unity.Burst.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Entities.LowLevel.Unsafe;
using UnityEngine;
using AssetLibraryConsumerEntityListTable = EvilOctane.Collections.LowLevel.Unsafe.UnsafeSwissTable<Unity.Entities.UnityObjectRef<EvilOctane.Entities.AssetLibrary>, Unity.Collections.LowLevel.Unsafe.UnsafeList<Unity.Entities.Entity>, EvilOctane.Collections.XXH3PodHasher<Unity.Entities.UnityObjectRef<EvilOctane.Entities.AssetLibrary>>>;
using AssetLibraryEntityConsumerEntityListPairTable = EvilOctane.Collections.LowLevel.Unsafe.UnsafeSwissTable<Unity.Entities.UnityObjectRef<EvilOctane.Entities.AssetLibrary>, EvilOctane.Collections.KeyValue<Unity.Entities.Entity, Unity.Collections.LowLevel.Unsafe.UnsafeList<Unity.Entities.Entity>>, EvilOctane.Collections.XXH3PodHasher<Unity.Entities.UnityObjectRef<EvilOctane.Entities.AssetLibrary>>>;

namespace EvilOctane.Entities.Internal
{
    [BurstCompile]
    [WithOptions(EntityQueryOptions.IncludePrefab)]
    public unsafe partial struct AssetLibraryCreateExistingReferenceTableJob : IJobEntity
    {
        [ReadOnly]
        public NativeReference<AssetLibraryConsumerEntityListTable> BakedReferenceTableRef;

        public NativeReference<AssetLibraryEntityConsumerEntityListPairTable> ExistingReferenceTableRef;

        public AllocatorManager.AllocatorHandle Allocator;

        public void Execute(
            Entity entity,
            AssetLibraryInternal.Reference reference,
            DynamicBuffer<AssetLibraryInternal.ConsumerEntityBufferElement> consumerEntityBuffer)
        {
            bool isValid = reference.AssetLibrary.IsValid();

            if (Hint.Unlikely(!isValid))
            {
                // Invalid
                return;
            }

            ref AssetLibraryConsumerEntityListTable bakedReferenceTable = ref *BakedReferenceTableRef.GetUnsafeReadOnlyPtr();
            _ = bakedReferenceTable.TryGet(reference.AssetLibrary, out bool wasRebaked);

            if (!wasRebaked)
            {
                // Not re-baked
                return;
            }

            ref AssetLibraryEntityConsumerEntityListPairTable existingReferenceTable = ref *ExistingReferenceTableRef.GetUnsafePtr();
            ref Collections.KeyValue<Entity, UnsafeList<Entity>> entityConsumerEntityListPair = ref existingReferenceTable.GetOrAdd(reference.AssetLibrary, out bool added);

            if (Hint.Unlikely(!added))
            {
                // Duplicate
                Debug.LogError("AssetLibrary | Same asset library is managed by multiple entities.");
                return;
            }

            // Asset library entity
            entityConsumerEntityListPair.Key = entity;

            // Consumer entities
            ref UnsafeList<Entity> consumerEntityList = ref entityConsumerEntityListPair.Value;

            consumerEntityList = new UnsafeList<Entity>(consumerEntityBuffer.Length, Allocator);
            consumerEntityList.AddRangeNoResize(consumerEntityBuffer.AsSpanRO().Reinterpret<Entity>());
        }
    }
}
