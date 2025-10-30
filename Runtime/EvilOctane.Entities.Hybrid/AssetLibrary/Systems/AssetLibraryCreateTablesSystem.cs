using Unity.Burst;
using Unity.Entities;
using static Unity.Entities.SystemAPI;

namespace EvilOctane.Entities.Internal
{
    [UpdateAfter(typeof(AssetLibraryPrepareEntitiesSystem))]
    [UpdateInGroup(typeof(BakingSystemGroup), OrderFirst = true)]
    [WorldSystemFilter(WorldSystemFilterFlags.BakingSystem)]
    public partial struct AssetLibraryCreateTablesSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // Create tables
            new AssetLibraryCreateTablesJob().ScheduleParallel();

            EntityCommandBuffer commandBuffer = new(state.WorldUpdateAllocator);

            // Update consumers
            new AssetLibraryUpdateConsumersJob()
            {
                AssetLibraryEntityBufferLookup = GetBufferLookup<AssetLibrary.EntityBufferElement>(),
                CommandBuffer = commandBuffer
            }.Schedule();

            state.CompleteDependency();
            commandBuffer.Playback(state.EntityManager);
        }
    }
}
