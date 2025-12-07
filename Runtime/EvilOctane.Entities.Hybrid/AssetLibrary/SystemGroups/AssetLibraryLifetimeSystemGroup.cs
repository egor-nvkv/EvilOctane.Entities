using Unity.Entities;

namespace EvilOctane.Entities
{
    [UpdateInGroup(typeof(AssetLibrarySystemGroup))]
    [WorldSystemFilter(WorldSystemFilterFlags.BakingSystem)]
    public sealed partial class AssetLibraryLifetimeSystemGroup : ComponentSystemGroup
    {
    }
}
