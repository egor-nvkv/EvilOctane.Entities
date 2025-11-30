using Unity.Entities;

namespace EvilOctane.Entities
{
    [UpdateAfter(typeof(AssetLibraryAssetBakingSystemGroup))]
    [UpdateInGroup(typeof(AssetLibraryBakingSystemGroup))]
    [WorldSystemFilter(WorldSystemFilterFlags.BakingSystem)]
    public sealed partial class AssetLibraryAfterAssetBakingSystemGroup : ComponentSystemGroup
    {
    }
}
