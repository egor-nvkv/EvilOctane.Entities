using Unity.Entities;

namespace EvilOctane.Entities
{
    public interface IAssetSearcher
    {
        AssetSearchResult Result { get; }

        void VisitAssets(AssetSearchOptions options, Entity assetLibrary, in AssetTableEntry entry, out bool finished);
    }
}
