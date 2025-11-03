using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using static Unity.Entities.SystemAPI;
using AssetLibraryConsumerEntityListTable = EvilOctane.Collections.LowLevel.Unsafe.UnsafeSwissTable<Unity.Entities.UnityObjectRef<EvilOctane.Entities.AssetLibrary>, Unity.Collections.LowLevel.Unsafe.UnsafeList<Unity.Entities.Entity>, EvilOctane.Collections.XXH3PodHasher<Unity.Entities.UnityObjectRef<EvilOctane.Entities.AssetLibrary>>>;

namespace EvilOctane.Entities.Internal
{
    [UpdateInGroup(typeof(AssetLibraryBakingSystemGroup))]
    [WorldSystemFilter(WorldSystemFilterFlags.BakingSystem)]
    public unsafe partial struct AssetLibraryPrepareEntitiesSystem : ISystem
    {
        private EntityArchetype assetLibraryArchetype;

        private EntityQuery garbageCollectQuery;
        private EntityQuery updateRebakedTablesQuery;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            assetLibraryArchetype = state.EntityManager.CreateArchetype(stackalloc ComponentType[]
            {
                ComponentType.ReadWrite<BakedEntityNameComponent>(),
                ComponentType.ReadWrite<PropagateBakedEntityNameTag>(),
                ComponentType.ReadWrite<AssetLibrary.Storage>(),
                ComponentType.ReadWrite<AssetLibraryInternal.KeyStorage>(),
                ComponentType.ReadWrite<AssetLibraryInternal.KeyBufferElement>(),
                ComponentType.ReadWrite<AssetLibraryInternal.AssetBufferElement>(),
                ComponentType.ReadWrite<AssetLibraryInternal.Reference>(),
                ComponentType.ReadWrite<AssetLibraryInternal.ConsumerEntityBufferElement>()
            });

            garbageCollectQuery = QueryBuilder()
                .WithAll<
                    AssetLibraryInternal.Reference>()
                .WithAllRW<
                    AssetLibraryInternal.ConsumerEntityBufferElement>()
                .Build();

            updateRebakedTablesQuery = QueryBuilder()
                .WithAll<
                    AssetLibraryInternal.Reference>()
                .WithAllRW<
                    AssetLibraryInternal.ConsumerEntityBufferElement>()
                .Build();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            EntityCommandBuffer commandBuffer = new(state.WorldUpdateAllocator);

            // Garbage collection
            JobHandle garbageCollectJobHandle = new AssetLibraryGarbageCollectJob()
            {
                EntityTypeHandle = GetEntityTypeHandle(),

                ReferenceTypeHandle = GetComponentTypeHandle<AssetLibraryInternal.Reference>(isReadOnly: true),
                ConsumerEntityBufferTypeHandle = GetBufferTypeHandle<AssetLibraryInternal.ConsumerEntityBufferElement>(),

                AssetLibraryEntityBufferLookup = GetBufferLookup<AssetLibrary.EntityBufferElement>(),
                CommandBuffer = commandBuffer
            }.Schedule(garbageCollectQuery, state.Dependency);

            NativeReference<AssetLibraryConsumerEntityListTable> bakedReferenceTableRef = new(state.WorldUpdateAllocator, NativeArrayOptions.UninitializedMemory)
            {
                Value = new AssetLibraryConsumerEntityListTable(state.WorldUpdateAllocator)
            };

            // Gather baked references
            JobHandle gatherBakedReferencesJobHandle = new AssetLibraryGatherBakedReferencesJob()
            {
                BakedReferenceTableRef = bakedReferenceTableRef,
                Allocator = state.WorldUpdateAllocator
            }.Schedule(state.Dependency);

            // Sync point
            state.Dependency = JobHandle.CombineDependencies(garbageCollectJobHandle, gatherBakedReferencesJobHandle);
            state.CompleteDependency();

            EntityCommandBuffer.ParallelWriter parallelWriter = commandBuffer.AsParallelWriter();

            JobHandle updateRebakedJobHandle = new AssetLibraryUpdateRebakedJob()
            {
                EntityTypeHandle = GetEntityTypeHandle(),

                ReferenceTypeHandle = GetComponentTypeHandle<AssetLibraryInternal.Reference>(isReadOnly: true),
                ConsumerEntityBufferTypeHandle = GetBufferTypeHandle<AssetLibraryInternal.ConsumerEntityBufferElement>(),

                BakedReferenceTableRef = bakedReferenceTableRef,
                CommandBuffer = parallelWriter
            }.ScheduleParallel(updateRebakedTablesQuery, state.Dependency);

            JobHandle createEntitiesJobHandle = new AssetLibraryCreateEntitiesJob()
            {
                BakedReferenceTableRef = bakedReferenceTableRef,
                CommandBuffer = parallelWriter,
                AssetLibraryArchetype = assetLibraryArchetype
            }.Schedule(state.Dependency);

            // Sync point
            state.Dependency = JobHandle.CombineDependencies(updateRebakedJobHandle, createEntitiesJobHandle);
            state.CompleteDependency();
            commandBuffer.Playback(state.EntityManager);

            commandBuffer = new(state.WorldUpdateAllocator);

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
