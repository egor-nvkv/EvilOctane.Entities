using Unity.Burst;
using Unity.Entities;

namespace EvilOctane.Entities.Internal
{
    [BurstCompile]
    [WithAbsent(typeof(AssetLibrary.AliveTag))]
    public partial struct AssetLibraryCleanupAssetTablesJob : IJobEntity
    {
        public EntityCommandBuffer.ParallelWriter CommandBuffer;

        public void Execute(
            [ChunkIndexInQuery] int chunkIndexInQuery,
            Entity entity,
            ref AssetLibrary.AssetTableComponent assetTable)
        {
            assetTable.Value.Dispose();
            CommandBuffer.RemoveComponent<AssetLibrary.AssetTableComponent>(chunkIndexInQuery, entity);
        }
    }
}
