using Unity.Entities;

namespace EvilOctane.Entities
{
    [UpdateAfter(typeof(AssetLibraryLifetimeSystemGroup))]
    [UpdateInGroup(typeof(AssetLibrarySystemGroup))]
    [WorldSystemFilter(WorldSystemFilterFlags.BakingSystem)]
    public sealed partial class AssetLibraryAssetSystemGroup : ComponentSystemGroup
    {
    }
}
