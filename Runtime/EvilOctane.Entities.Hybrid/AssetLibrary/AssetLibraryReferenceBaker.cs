using Unity.Entities;
using static EvilOctane.Entities.Internal.AssetLibraryBakingAPI;

namespace EvilOctane.Entities.Internal
{
    public class AssetLibraryReferenceBaker : Baker<AssetLibraryReferenceAuthoring>
    {
        public override void Bake(AssetLibraryReferenceAuthoring authoring)
        {
            BakeAssetLibrary(this, authoring);
        }
    }
}
