using Unity.Entities;

namespace EvilOctane.Entities
{
    [UpdateInGroup(typeof(AssetLibraryBakingSystemGroup))]
    [WorldSystemFilter(WorldSystemFilterFlags.BakingSystem)]
    public sealed partial class AssetLibraryAssetBakingSystemGroup : ComponentSystemGroup
    {
    }
}
