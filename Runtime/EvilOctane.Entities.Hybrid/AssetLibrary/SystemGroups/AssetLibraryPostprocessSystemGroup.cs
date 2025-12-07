using Unity.Entities;

namespace EvilOctane.Entities
{
    [UpdateAfter(typeof(AssetLibraryAssetTableSystemGroup))]
    [UpdateInGroup(typeof(AssetLibrarySystemGroup))]
    [WorldSystemFilter(WorldSystemFilterFlags.BakingSystem)]
    public sealed partial class AssetLibraryPostprocessSystemGroup : ComponentSystemGroup
    {
    }
}
