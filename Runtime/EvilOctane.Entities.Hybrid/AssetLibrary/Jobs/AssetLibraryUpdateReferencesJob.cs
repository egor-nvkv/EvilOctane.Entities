using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Entities.Hybrid.Baking;
using static System.Runtime.CompilerServices.Unsafe;

namespace EvilOctane.Entities.Internal
{
    [BurstCompile]
    [WithPresent(typeof(AssetLibraryConsumer.RebakedTag))]
    [WithOptions(EntityQueryOptions.IncludePrefab | EntityQueryOptions.IncludeDisabledEntities)]
    public partial struct AssetLibraryUpdateReferencesJob : IJobEntity
    {
        [ReadOnly]
        public ComponentLookup<AssetLibraryConsumerAdditional.DeclaredReference> DeclaredReferenceLookup;

        [ReadOnly]
        public NativeReference<AssetLibraryInstanceTable> InstanceTableRef;

        public void Execute(
            in DynamicBuffer<AdditionalEntitiesBakingData> additionalEntities,
            ref DynamicBuffer<AssetLibraryConsumer.AssetLibraryBufferElement> assetLibraryBuffer)
        {
            ref AssetLibraryInstanceTable instanceTableRO = ref AsRef(in InstanceTableRef.GetRefReadOnly());
            assetLibraryBuffer.Clear();

            foreach (AdditionalEntitiesBakingData additionalEntity in additionalEntities)
            {
                if (!DeclaredReferenceLookup.TryGetComponent(additionalEntity.Value, out AssetLibraryConsumerAdditional.DeclaredReference reference))
                {
                    // Component missing
                    continue;
                }

                Pointer<Entity> instance = instanceTableRO.Value.TryGet(reference.AssetLibrary, out bool exists);

                if (!exists)
                {
                    continue;
                }

                if (!assetLibraryBuffer.AsSpanRO().Reinterpret<Entity>().Contains(instance.AsRef))
                {
                    // Add reference
                    _ = assetLibraryBuffer.Add(new AssetLibraryConsumer.AssetLibraryBufferElement() { Entity = instance.AsRef });
                }
            }
        }
    }
}
