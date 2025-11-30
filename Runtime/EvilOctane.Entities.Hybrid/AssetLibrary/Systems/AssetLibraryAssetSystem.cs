using Unity.Burst;
using Unity.Entities;

namespace EvilOctane.Entities.Internal
{
    [UpdateInGroup(typeof(AssetLibraryAssetBakingSystemGroup))]
    [WorldSystemFilter(WorldSystemFilterFlags.BakingSystem)]
    public partial struct AssetLibraryAssetSystem : ISystem
    {
        private EntityArchetype assetArchetype;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            assetArchetype = state.EntityManager.CreateArchetype(stackalloc ComponentType[]
            {
                ComponentType.ReadWrite<BakingOnlyEntity>(),
                ComponentType.ReadWrite<RebakedTag>(),

                ComponentType.ReadWrite<Asset.UnityObjectComponent>(),
                ComponentType.ReadWrite<Asset.TypeHashComponent>(),

                ComponentType.ReadWrite<AssetLibrary.AssetBufferElement.OwnerShared>()
            });
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            EntityCommandBuffer commandBuffer = new(state.WorldUpdateAllocator);

            // Create asset entities
            new AssetLibraryCreateAssetEntitiesJob()
            {
                AssetArchetype = assetArchetype,
                CommandBuffer = commandBuffer.AsParallelWriter()
            }.ScheduleParallel();

            state.CompleteDependency();
            commandBuffer.Playback(state.EntityManager);
        }
    }
}
