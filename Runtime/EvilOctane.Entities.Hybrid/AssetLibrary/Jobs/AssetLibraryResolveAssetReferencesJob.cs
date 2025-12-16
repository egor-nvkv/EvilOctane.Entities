using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace EvilOctane.Entities.Internal
{
    [BurstCompile]
    [WithPresent(typeof(AssetConsumer.RebakedTag))]
    [WithOptions(EntityQueryOptions.IncludePrefab)]
    public unsafe partial struct AssetLibraryResolveAssetReferencesJob : IJobEntity
    {
        [ReadOnly]
        public BufferLookup<AssetLibrary.AssetBufferElement> AssetBufferLookup;
        [ReadOnly]
        public ComponentLookup<AssetLibrary.UnityObjectComponent> AssetLibraryUnityObjectLookup;

        [ReadOnly]
        public ComponentLookup<Asset.UnityObjectComponent> AssetUnityObjectLookup;

        public void Execute(
            in AssetConsumer.DeclaredReference assetReference,
            in DynamicBuffer<AssetLibraryConsumer.AssetLibraryBufferElement> assetLibraryBuffer,
            ref AssetConsumer.DeclaredReference.Resolved assetReferenceResolved)
        {
            // Reset
            assetReferenceResolved.Asset = Entity.Null;

            foreach (AssetLibraryConsumer.AssetLibraryBufferElement assetLibrary in assetLibraryBuffer)
            {
                if (!AssetLibraryUnityObjectLookup.TryGetComponent(assetLibrary.Entity, out AssetLibrary.UnityObjectComponent assetLibraryObj))
                {
                    // Component missing
                    continue;
                }
                else if (assetReference.AssetLibrary != assetLibraryObj.Value)
                {
                    // Wrong asset library
                    continue;
                }

                if (!AssetBufferLookup.TryGetBuffer(assetLibrary.Entity, out DynamicBuffer<AssetLibrary.AssetBufferElement> assetBuffer))
                {
                    // Component missing
                    continue;
                }

                foreach (AssetLibrary.AssetBufferElement asset in assetBuffer)
                {
                    if (!AssetUnityObjectLookup.TryGetComponent(asset.Entity, out Asset.UnityObjectComponent assetObj))
                    {
                        // Component missing
                        continue;
                    }

                    if (assetReference.Asset == assetObj.Ref)
                    {
                        // Found
                        assetReferenceResolved.Asset = asset.Entity;
                        return;
                    }
                }
            }
        }
    }
}
