using Unity.Entities;

namespace EvilOctane.Entities
{
    [UpdateBefore(typeof(AssetLibraryAssetBakingSystemGroup))]
    [UpdateInGroup(typeof(AssetLibraryBakingSystemGroup))]
    [WorldSystemFilter(WorldSystemFilterFlags.BakingSystem)]
    public sealed partial class AssetLibraryBeforeAssetBakingSystemGroup : ComponentSystemGroup
    {
    }
}
