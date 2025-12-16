using Unity.Entities;
using UnityEngine;

namespace EvilOctane.Entities.Internal
{
    public class AssetReferenceBaker : Baker<AssetReferenceAuthoring>
    {
        public override void Bake(AssetReferenceAuthoring authoring)
        {
            Entity entity = GetEntityWithoutDependency();

            // Rebaked tag
            AddComponent<AssetConsumer.RebakedTag>(entity);

            AssetLibrary assetLibrary = DependsOn(authoring.assetLibrary);
            Object asset = DependsOn(authoring.asset);

            if (assetLibrary && asset)
            {
                // Declared reference
                AddComponent(entity, new AssetConsumer.DeclaredReference()
                {
                    AssetLibrary = assetLibrary,
                    Asset = asset
                });

                // Resolved reference
                AddComponent<AssetConsumer.DeclaredReference.Resolved>(entity);
            }
        }
    }
}
