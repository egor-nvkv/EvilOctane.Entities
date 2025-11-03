using Unity.Entities;

namespace EvilOctane.Entities
{
    [UpdateInGroup(typeof(BakingSystemGroup), OrderFirst = true)]
    [WorldSystemFilter(WorldSystemFilterFlags.BakingSystem)]
    public sealed partial class AssetLibraryBakingSystemGroup : ComponentSystemGroup
    {
    }
}
