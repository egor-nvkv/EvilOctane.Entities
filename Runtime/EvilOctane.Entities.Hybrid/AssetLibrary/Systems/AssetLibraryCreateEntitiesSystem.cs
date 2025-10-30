using EvilOctane.Collections;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Entities.LowLevel.Unsafe;
using static Unity.Entities.SystemAPI;
using AssetLibraryConsumerEntityListTable = EvilOctane.Collections.LowLevel.Unsafe.UnsafeSwissTable<Unity.Entities.UnityObjectRef<EvilOctane.Entities.AssetLibrary>, Unity.Collections.LowLevel.Unsafe.UnsafeList<Unity.Entities.Entity>, EvilOctane.Collections.XXH3PodHasher<Unity.Entities.UnityObjectRef<EvilOctane.Entities.AssetLibrary>>>;
using AssetLibraryEntityConsumerEntityListPairTable = EvilOctane.Collections.LowLevel.Unsafe.UnsafeSwissTable<Unity.Entities.UnityObjectRef<EvilOctane.Entities.AssetLibrary>, EvilOctane.Collections.KeyValue<Unity.Entities.Entity, Unity.Collections.LowLevel.Unsafe.UnsafeList<Unity.Entities.Entity>>, EvilOctane.Collections.XXH3PodHasher<Unity.Entities.UnityObjectRef<EvilOctane.Entities.AssetLibrary>>>;

namespace EvilOctane.Entities.Internal
{
    [UpdateInGroup(typeof(BakingSystemGroup), OrderFirst = true)]
    [WorldSystemFilter(WorldSystemFilterFlags.BakingSystem)]
    public unsafe partial struct AssetLibraryCreateEntitiesSystem : ISystem
    {
        private ComponentTypeSet assetLibraryComponentTypeSet;
        private EntityArchetype assetLibraryArchetype;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            assetLibraryComponentTypeSet = ComponentTypeSetUtility.Create<
                BakedEntityNameComponent,
                AssetLibrary.Storage,
                AssetLibraryInternal.KeyBufferElement,
                AssetLibraryInternal.AssetBufferElement,
                AssetLibraryInternal.Reference,
                AssetLibraryInternal.ConsumerEntityBufferElement>();

            assetLibraryArchetype = state.EntityManager.CreateArchetype(stackalloc ComponentType[]
            {
                ComponentType.ReadWrite<BakingOnlyEntity>(),
                ComponentType.ReadWrite<BakedEntityNameComponent>(),
                ComponentType.ReadWrite<AssetLibrary.Storage>(),
                ComponentType.ReadWrite<AssetLibraryInternal.KeyBufferElement>(),
                ComponentType.ReadWrite<AssetLibraryInternal.AssetBufferElement>(),
                ComponentType.ReadWrite<AssetLibraryInternal.Reference>(),
                ComponentType.ReadWrite<AssetLibraryInternal.ConsumerEntityBufferElement>()
            });
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            EntityCommandBuffer commandBuffer = new(state.WorldUpdateAllocator);

            // Check references
            new AssetLibraryCheckReferencesJob()
            {
                AssetLibraryEntityBufferLookup = GetBufferLookup<AssetLibrary.EntityBufferElement>(),
                CommandBuffer = commandBuffer
            }.Schedule();

            NativeReference<AssetLibraryConsumerEntityListTable> bakedReferenceTableRef = new(state.WorldUpdateAllocator, NativeArrayOptions.UninitializedMemory)
            {
                Value = new AssetLibraryConsumerEntityListTable(state.WorldUpdateAllocator)
            };

            // Gather newly baked references
            new AssetLibraryCreateBakedReferenceTableJob()
            {
                BakedReferenceTableRef = bakedReferenceTableRef,
                Allocator = state.WorldUpdateAllocator
            }.Schedule();

            NativeReference<AssetLibraryEntityConsumerEntityListPairTable> existingReferenceTableRef = new(state.WorldUpdateAllocator, NativeArrayOptions.UninitializedMemory)
            {
                Value = new AssetLibraryEntityConsumerEntityListPairTable(state.WorldUpdateAllocator)
            };

            // Gather existing references
            new AssetLibraryCreateExistingReferenceTableJob()
            {
                BakedReferenceTableRef = bakedReferenceTableRef,
                ExistingReferenceTableRef = existingReferenceTableRef,
                Allocator = state.WorldUpdateAllocator
            }.Schedule();

            state.CompleteDependency();
            commandBuffer.Playback(state.EntityManager);

            CreateOrUpdateAssetLibraryEntities(
                ref state,
                ref *bakedReferenceTableRef.GetUnsafeReadOnlyPtr(),
                ref *existingReferenceTableRef.GetUnsafeReadOnlyPtr());
        }

        private void CreateOrUpdateAssetLibraryEntities(
            ref SystemState state,
            ref AssetLibraryConsumerEntityListTable bakedReferenceTableRO,
            ref AssetLibraryEntityConsumerEntityListPairTable existingReferenceTableRO)
        {
            foreach (KeyValueRef<UnityObjectRef<AssetLibrary>, UnsafeList<Entity>> kvPairBaked in bakedReferenceTableRO)
            {
                ref Collections.KeyValue<Entity, UnsafeList<Entity>> entityConsumerEntityListPair = ref existingReferenceTableRO.TryGet(kvPairBaked.KeyRefRO, out bool wasRebaked);

                Entity assetLibraryEntity;

                if (wasRebaked)
                {
                    // Asset library already exists
                    // Entity referencing it has been re-baked so we need to re-initialize the asset library

                    assetLibraryEntity = entityConsumerEntityListPair.Key;
                    state.EntityManager.AddComponent(assetLibraryEntity, assetLibraryComponentTypeSet);
                }
                else
                {
                    // Brand new asset library
                    assetLibraryEntity = state.EntityManager.CreateEntity(assetLibraryArchetype);
                }

                // Asset library reference
                state.EntityManager.SetComponentData(assetLibraryEntity, new AssetLibraryInternal.Reference()
                {
                    AssetLibrary = kvPairBaked.KeyRefRO
                });

                // Consumer entities
                DynamicBuffer<AssetLibraryInternal.ConsumerEntityBufferElement> consumerEntityBuffer = state.EntityManager.GetBuffer<AssetLibraryInternal.ConsumerEntityBufferElement>(assetLibraryEntity);

                // Newly baked references
                UnsafeSpan<Entity> entitySpan = kvPairBaked.ValueRef.AsSpan();
                consumerEntityBuffer.ResizeUninitializedTrashOldData(entitySpan.Length);
                consumerEntityBuffer.AsSpanRW().Reinterpret<Entity>().CopyFrom(entitySpan);

                if (wasRebaked)
                {
                    // Existing references

                    UnsafeSpan<Entity> consumerEntitySpan = entityConsumerEntityListPair.Value.AsSpan().Reinterpret<Entity>();
                    consumerEntityBuffer.EnsureCapacity(consumerEntityBuffer.Length + consumerEntitySpan.Length);

                    foreach (Entity referencingEntity in consumerEntitySpan)
                    {
                        if (!consumerEntityBuffer.AsSpanRO().Reinterpret<Entity>().Contains(referencingEntity))
                        {
                            // Add existing reference (unique)
                            _ = consumerEntityBuffer.AddNoResize(new AssetLibraryInternal.ConsumerEntityBufferElement()
                            {
                                ConsumerEntity = referencingEntity
                            });
                        }
                    }
                }
            }
        }
    }
}
