using Unity.Entities;
using UnityEngine;
using static EvilOctane.Entities.Internal.AssetLibraryBakingAPI;

namespace EvilOctane.Entities.Internal
{
    public class AssetReferenceBaker : Baker<AssetReferenceAuthoring>
    {
        public override void Bake(AssetReferenceAuthoring authoring)
        {
            Object asset = DependsOn(authoring.asset);

            if (DependsOnAssetLibrariesAndAssets(this) && asset)
            {
                Entity entity = GetEntityWithoutDependency();

                // Rebaked tag
                AddComponent<AssetConsumer.RebakedTag>(entity);

                // Asset reference
                AddComponent(entity, new AssetConsumer.DeclaredAssetReference()
                {
                    Asset = authoring.asset
                });
            }
        }
    }
}
