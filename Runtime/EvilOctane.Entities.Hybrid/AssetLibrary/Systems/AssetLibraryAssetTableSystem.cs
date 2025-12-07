using Unity.Burst;
using Unity.Entities;
using static Unity.Entities.SystemAPI;

namespace EvilOctane.Entities.Internal
{
    [UpdateInGroup(typeof(AssetLibraryAssetTableSystemGroup))]
    [WorldSystemFilter(WorldSystemFilterFlags.BakingSystem)]
    public partial struct AssetLibraryAssetTableSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // Create tables
            new AssetLibraryCreateAssetTablesJob()
            {
                BakingNameLookup = GetBufferLookup<Asset.BakingNameStorage>(isReadOnly: true)
            }.ScheduleParallel();
        }
    }
}
