using Unity.Entities;
using static EvilOctane.Entities.Internal.AssetLibraryBakingAPI;

namespace EvilOctane.Entities.Internal
{
    [BakeDerivedTypes]
    public class AssetLibraryReferenceBaker : Baker<AssetLibraryReferenceAuthoring>
    {
        public override void Bake(AssetLibraryReferenceAuthoring authoring)
        {
            _ = BakeAssetLibrary(this, authoring);
        }
    }
}
