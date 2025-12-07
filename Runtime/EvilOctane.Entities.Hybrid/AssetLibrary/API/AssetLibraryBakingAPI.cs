using Unity.Entities;

namespace EvilOctane.Entities.Internal
{
    public static class AssetLibraryBakingAPI
    {
        public static bool BakeAssetLibrary(IBaker baker, AssetLibraryReferenceAuthoring authoring)
        {
            if (DependsOnAssetLibraryAndAssets(baker, authoring))
            {
                // Reference
                DeclareReference(baker, authoring);

                return true;
            }

            return false;
        }

        public static bool DependsOnAssetLibraryAndAssets(IBaker baker, AssetLibraryReferenceAuthoring authoring)
        {
            AssetLibrary assetLibrary = baker.DependsOn(authoring.assetLibrary);
            bool result = assetLibrary;

            if (result)
            {
                // Assets
                baker.DependsOnMultiple(assetLibrary.assets);
            }

            return result;
        }

        public static bool DependsOnAssetLibrariesAndAssets(IBaker baker)
        {
            AssetLibraryReferenceAuthoring[] authoringArray = baker.GetComponents<AssetLibraryReferenceAuthoring>();
            bool result = false;

            foreach (AssetLibraryReferenceAuthoring authoring in authoringArray)
            {
                result |= DependsOnAssetLibraryAndAssets(baker, authoring);
            }

            return result;
        }

        private static void DeclareReference(IBaker baker, AssetLibraryReferenceAuthoring authoring)
        {
            AssetLibrary assetLibrary = authoring.assetLibrary;

            string entityName = $"{baker.GetName(authoring)} +> {assetLibrary.name}";
            Entity entity = baker.CreateAdditionalEntity(TransformUsageFlags.None, bakingOnlyEntity: true, entityName);

            // Rebaked
            baker.AddComponent<AssetLibraryConsumerAdditional.RebakedTag>(entity);

            // Reference
            baker.AddComponent(entity, new AssetLibraryConsumerAdditional.DeclaredReference()
            {
                AssetLibrary = assetLibrary
            });
        }
    }
}
