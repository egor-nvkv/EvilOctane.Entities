using Unity.Burst;
using Unity.Entities;

namespace EvilOctane.Entities.Internal
{
    [UpdateAfter(typeof(AssetLibraryCopyListsSystem))]
    [UpdateInGroup(typeof(AssetLibraryBakingSystemGroup))]
    [WorldSystemFilter(WorldSystemFilterFlags.BakingSystem)]
    public partial struct AssetLibraryCreateTablesSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // Create tables
            new AssetLibraryCreateTablesJob().ScheduleParallel();
            state.CompleteDependency();
        }
    }
}
