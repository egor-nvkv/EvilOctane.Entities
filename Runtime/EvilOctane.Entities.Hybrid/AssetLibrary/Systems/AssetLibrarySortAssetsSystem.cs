using Unity.Burst;
using Unity.Entities;
using static Unity.Entities.SystemAPI;

namespace EvilOctane.Entities.Internal
{
    [UpdateInGroup(typeof(AssetLibraryPostprocessSystemGroup))]
    [WorldSystemFilter(WorldSystemFilterFlags.BakingSystem)]
    public partial struct AssetLibrarySortAssetsSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // Sort assets
            new AssetLibrarySortAssetsJob()
            {
                EntityStorageInfoLookup = GetEntityStorageInfoLookup()
            }.ScheduleParallel();
        }
    }
}
