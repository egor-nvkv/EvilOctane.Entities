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
        public ComponentLookup<Asset.UnityObjectComponent> UnityObjectLookup;

        public EntityCommandBuffer CommandBuffer;

        public void Execute(
            Entity entity,
            in AssetConsumer.DeclaredAssetReference assetReference,
            in DynamicBuffer<AssetLibraryConsumer.AssetLibraryBufferElement> assetLibraryBuffer)
        {
            foreach (AssetLibraryConsumer.AssetLibraryBufferElement assetLibrary in assetLibraryBuffer)
            {
                if (!AssetBufferLookup.TryGetBuffer(assetLibrary.Entity, out DynamicBuffer<AssetLibrary.AssetBufferElement> assetBuffer))
                {
                    // Component missing
                    continue;
                }

                foreach (AssetLibrary.AssetBufferElement asset in assetBuffer)
                {
                    if (!UnityObjectLookup.TryGetComponent(asset.Entity, out Asset.UnityObjectComponent assetObj))
                    {
                        // Component missing
                        continue;
                    }

                    if (assetReference.Asset == assetObj.Ref)
                    {
                        // Found

                        CommandBuffer.AddComponent(entity, new AssetConsumer.ResolvedAssetReference()
                        {
                            Asset = asset.Entity
                        });

                        return;
                    }
                }
            }
        }
    }
}
