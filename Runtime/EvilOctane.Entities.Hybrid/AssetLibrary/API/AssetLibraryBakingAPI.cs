using Unity.Entities;

namespace EvilOctane.Entities.Internal
{
    public static class AssetLibraryBakingAPI
    {
        public static void BakeAssetLibrary(IBaker baker, AssetLibraryReferenceAuthoring authoring)
        {
            if (baker.DependsOn(authoring.assetLibrary))
            {
                AssetLibrary assetLibrary = authoring.assetLibrary;

                // Assets
                baker.DependsOnMultiple(assetLibrary.assets);

                // Reference
                DeclareReference(baker, authoring);
            }
        }

        private static void DeclareReference(IBaker baker, AssetLibraryReferenceAuthoring authoring)
        {
            AssetLibrary assetLibrary = authoring.assetLibrary;

            string entityName = $"{baker.GetName(authoring)} +> {assetLibrary.name}";
            Entity entity = baker.CreateAdditionalEntity(TransformUsageFlags.None, bakingOnlyEntity: true, entityName);

            // Rebaked
            baker.AddComponent<RebakedTag>(entity);

            // Reference
            baker.AddComponent(entity, new AssetLibraryInternal.DeclaredReference()
            {
                AssetLibrary = assetLibrary
            });
        }
    }
}
