using Unity.Burst;
using Unity.Entities;
using static Unity.Entities.SystemAPI;

namespace EvilOctane.Entities.Internal
{
    [UpdateInGroup(typeof(AssetLibraryAssetSystemGroup))]
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

                ComponentType.ReadWrite<Asset.RebakedTag>(),
                ComponentType.ReadWrite<Asset.UnityObjectComponent>(),
                ComponentType.ReadWrite<Asset.BakingNameStorage>(),

                ComponentType.ReadWrite<AssetLibrary.AssetBufferElement.OwnerShared>()
            });
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            UpdateAssets(ref state);
            ResolveReferences(ref state);
        }

        private void UpdateAssets(ref SystemState state)
        {
            EntityCommandBuffer commandBuffer = new(state.WorldUpdateAllocator);

            state.Dependency = new AssetLibraryUpdateAssetsJob()
            {
                EntityTypeHandle = GetEntityTypeHandle(),

                AssetBufferTypeHandle = GetBufferTypeHandle<AssetLibrary.AssetBufferElement>(),
                AssetDataBufferTypeHandle = GetBufferTypeHandle<AssetLibraryInternal.AssetDataBufferElement>(isReadOnly: true),
                UnityObjectLookup = GetComponentLookup<Asset.UnityObjectComponent>(isReadOnly: true),

                AssetArchetype = assetArchetype,

                TempAllocator = state.WorldUpdateAllocator,
                CommandBuffer = commandBuffer.AsParallelWriter()
            }.ScheduleParallel(
                QueryBuilder()
                .WithPresentRW<AssetLibrary.AssetBufferElement>()
                .WithPresent<AssetLibraryInternal.AssetDataBufferElement>()
                .Build(),
                state.Dependency);

            state.CompleteDependency();
            commandBuffer.Playback(state.EntityManager);
        }

        private void ResolveReferences(ref SystemState state)
        {
            EntityCommandBuffer commandBuffer = new(state.WorldUpdateAllocator);

            new AssetLibraryResolveAssetReferencesJob()
            {
                AssetBufferLookup = GetBufferLookup<AssetLibrary.AssetBufferElement>(isReadOnly: true),
                UnityObjectLookup = GetComponentLookup<Asset.UnityObjectComponent>(isReadOnly: true),
                CommandBuffer = commandBuffer
            }.Schedule();

            state.CompleteDependency();
            commandBuffer.Playback(state.EntityManager);
        }
    }
}
