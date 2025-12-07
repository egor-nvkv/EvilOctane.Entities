using EvilOctane.Entities;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using static Unity.Entities.SystemAPI;

[assembly: RegisterGenericJobType(typeof(OwnedEntityBufferCleanupJobParallel<AssetLibrary.AssetBufferElement, AssetLibrary.AliveTag>))]

namespace EvilOctane.Entities.Internal
{
    [UpdateInGroup(typeof(AssetLibraryLifetimeSystemGroup))]
    [WorldSystemFilter(WorldSystemFilterFlags.BakingSystem)]
    public partial struct AssetLibraryLifetimeSystem : ISystem
    {
        private EntityArchetype assetLibraryArchetype;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            assetLibraryArchetype = state.EntityManager.CreateArchetype(stackalloc ComponentType[]
            {
                ComponentType.ReadWrite<BakingOnlyEntity>(),

                ComponentType.ReadWrite<BakedEntityNameComponent>(),
                ComponentType.ReadWrite<BakedEntityNameComponent.PropagateTag>(),

                ComponentType.ReadWrite<AssetLibrary.AliveTag>(),
                ComponentType.ReadWrite<AssetLibrary.RebakedTag>(),
                ComponentType.ReadWrite<AssetLibrary.UnityObjectComponent>(),
                ComponentType.ReadWrite<AssetLibrary.AssetBufferElement>(),
                ComponentType.ReadWrite<AssetLibrary.AssetTableComponent>(),

                ComponentType.ReadWrite<AssetLibraryInternal.AssetReferenceBufferElement>()
            });
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            NativeReference<AssetLibraryInstanceTable> instanceTableRef = GatherInstancesProcessRebaked(
                ref state,
                out NativeReference<AssetLibraryConsumerTable> consumerTableRef);

            CreateEntities(
                ref state,
                instanceTableRef,
                consumerTableRef);

            UpdateReferences(
                ref state,
                instanceTableRef);

            Cleanup(
                ref state,
                consumerTableRef);
        }

        private NativeReference<AssetLibraryInstanceTable> GatherInstancesProcessRebaked(
            ref SystemState state,
            out NativeReference<AssetLibraryConsumerTable> consumerTableRef)
        {
            EntityCommandBuffer commandBuffer = new(state.WorldUpdateAllocator);

            // Gather instances
            NativeReference<AssetLibraryInstanceTable> instanceTableRef = new(new AssetLibraryInstanceTable(100, state.WorldUpdateAllocator), state.WorldUpdateAllocator);

            new AssetLibraryGatherInstancesJob()
            {
                InstanceTableRef = instanceTableRef,
                CommandBuffer = commandBuffer
            }.Schedule();

            // Gather references
            consumerTableRef = new(new AssetLibraryConsumerTable(100, state.WorldUpdateAllocator), state.WorldUpdateAllocator);
            NativeReference<AssetLibraryRebakedSet> rebakedSetRef = new(new AssetLibraryRebakedSet(50, state.WorldUpdateAllocator), state.WorldUpdateAllocator);

            new AssetLibraryGatherReferencesJob()
            {
                ConsumerTableRef = consumerTableRef,
                RebakedSetRef = rebakedSetRef
            }.Schedule();

            // Process rebaked
            state.Dependency = new AssetLibraryProcessRebakedJob()
            {
                InstanceTableRef = instanceTableRef,
                RebakedSetRef = rebakedSetRef,
                TempAllocator = state.WorldUpdateAllocator,
                CommandBuffer = commandBuffer
            }.Schedule(state.Dependency);

            state.CompleteDependency();
            commandBuffer.Playback(state.EntityManager);

            return instanceTableRef;
        }

        private void CreateEntities(
            ref SystemState state,
            NativeReference<AssetLibraryInstanceTable> instanceTableRef,
            NativeReference<AssetLibraryConsumerTable> consumerTableRef)
        {
            ExclusiveEntityTransaction transaction = state.EntityManager.BeginExclusiveEntityTransaction();

            // Create entities
            new AssetLibraryCreateJob()
            {
                InstanceTableRef = instanceTableRef,
                ConsumerTableRef = consumerTableRef,
                AssetLibraryArchetype = assetLibraryArchetype,
                Transaction = transaction
            }.Run();

            state.EntityManager.EndExclusiveEntityTransaction();
        }

        private void UpdateReferences(
            ref SystemState state,
            NativeReference<AssetLibraryInstanceTable> instanceTableRef)
        {
            // Update references
            new AssetLibraryUpdateReferencesJob()
            {
                DeclaredReferenceLookup = GetComponentLookup<AssetLibraryConsumerAdditional.DeclaredReference>(isReadOnly: true),
                InstanceTableRef = instanceTableRef
            }.ScheduleParallel();
        }

        private void Cleanup(
            ref SystemState state,
            NativeReference<AssetLibraryConsumerTable> consumerTableRef)
        {
            EntityCommandBuffer commandBuffer = new(state.WorldUpdateAllocator);
            EntityCommandBuffer.ParallelWriter parallelWriter = commandBuffer.AsParallelWriter();

            // GC
            state.Dependency = new AssetLibraryGCJob()
            {
                EntityTypeHandle = GetEntityTypeHandle(),
                UnityObjectLookup = GetComponentTypeHandle<AssetLibrary.UnityObjectComponent>(isReadOnly: true),
                ConsumerTableRef = consumerTableRef,
                CommandBuffer = parallelWriter
            }.ScheduleParallel(
                QueryBuilder()
                .WithPresent<AssetLibrary.UnityObjectComponent>()
                .Build(),
                state.Dependency);

            // Asset tables
            new AssetLibraryCleanupAssetTablesJob()
            {
                CommandBuffer = parallelWriter
            }.ScheduleParallel();

            // Asset buffers
            state.Dependency = new OwnedEntityBufferCleanupJobParallel<AssetLibrary.AssetBufferElement, AssetLibrary.AliveTag>()
            {
                EntityTypeHandle = GetEntityTypeHandle(),
                EntityLookup = GetComponentLookup<AssetLibrary.AliveTag>(isReadOnly: true),
                OwnedEntityBufferTypeHandle = GetBufferTypeHandle<AssetLibrary.AssetBufferElement>(),
                TempAllocator = state.WorldUpdateAllocator,
                CommandBuffer = parallelWriter
            }.ScheduleParallel(
                QueryBuilder()
                .WithPresent<AssetLibrary.AssetBufferElement>()
                .WithAbsent<AssetLibrary.AliveTag>()
                .Build(),
                state.Dependency);

            state.CompleteDependency();
            commandBuffer.Playback(state.EntityManager);
        }
    }
}
