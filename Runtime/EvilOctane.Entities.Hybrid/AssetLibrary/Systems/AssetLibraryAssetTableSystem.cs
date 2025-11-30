using Unity.Burst;
using Unity.Entities;

namespace EvilOctane.Entities.Internal
{
    [UpdateInGroup(typeof(AssetLibraryAfterAssetBakingSystemGroup))]
    [WorldSystemFilter(WorldSystemFilterFlags.BakingSystem)]
    public partial struct AssetLibraryAssetTableSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // Create tables
            new AssetLibraryCreateAssetTablesJob().ScheduleParallel();
            state.CompleteDependency();
        }
    }
}
