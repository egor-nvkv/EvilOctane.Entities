using Unity.Entities;

namespace EvilOctane.Entities
{
    [UpdateAfter(typeof(AssetLibraryAssetSystemGroup))]
    [UpdateInGroup(typeof(AssetLibrarySystemGroup))]
    [WorldSystemFilter(WorldSystemFilterFlags.BakingSystem)]
    public sealed partial class AssetLibraryAssetTableSystemGroup : ComponentSystemGroup
    {
    }
}
