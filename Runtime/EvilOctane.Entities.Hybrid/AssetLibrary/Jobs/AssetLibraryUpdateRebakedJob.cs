using Unity.Assertions;
using Unity.Burst;
using Unity.Burst.CompilerServices;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Entities.LowLevel.Unsafe;
using AssetLibraryConsumerEntityListTable = EvilOctane.Collections.LowLevel.Unsafe.UnsafeSwissTable<Unity.Entities.UnityObjectRef<EvilOctane.Entities.AssetLibrary>, Unity.Collections.LowLevel.Unsafe.UnsafeList<Unity.Entities.Entity>, EvilOctane.Collections.XXH3PodHasher<Unity.Entities.UnityObjectRef<EvilOctane.Entities.AssetLibrary>>>;

namespace EvilOctane.Entities.Internal
{
    [BurstCompile]
    public unsafe struct AssetLibraryUpdateRebakedJob : IJobChunk
    {
        [ReadOnly]
        public EntityTypeHandle EntityTypeHandle;

        [ReadOnly]
        public ComponentTypeHandle<AssetLibraryInternal.Reference> ReferenceTypeHandle;
        public BufferTypeHandle<AssetLibraryInternal.ConsumerEntityBufferElement> ConsumerEntityBufferTypeHandle;

        [ReadOnly]
        public NativeReference<AssetLibraryConsumerEntityListTable> BakedReferenceTableRef;

        [NativeDisableContainerSafetyRestriction]
        public EntityCommandBuffer.ParallelWriter CommandBuffer;

        /// <summary>
        /// For concurrent scheduling with <see cref="AssetLibraryCreateEntitiesJob"/>.
        /// </summary>
        [NativeSetThreadIndex]
        public int ThreadIndex;

        [SkipLocalsInit]
        public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
        {
            Assert.IsFalse(useEnabledMask);

            Entity* entityPtr = chunk.GetEntityDataPtrRO(EntityTypeHandle);

            AssetLibraryInternal.Reference* referencePtr = chunk.GetRequiredComponentDataPtrROTyped(ref ReferenceTypeHandle);
            BufferAccessor<AssetLibraryInternal.ConsumerEntityBufferElement> consumerEntityBufferAccessor = chunk.GetBufferAccessorRW(ref ConsumerEntityBufferTypeHandle);

            Entity* entitiesToUpdatePtr = stackalloc Entity[TypeManager.MaximumChunkCapacity];

            int entitiesToUpdateCount = PrepareForUpdate(
                in chunk,
                entityPtr,
                referencePtr,
                ref consumerEntityBufferAccessor,
                entitiesToUpdatePtr);

            if (entitiesToUpdateCount == 0)
            {
                // Nothing to update
                return;
            }

            // Set for update
            // As simple as adding temp components
            ComponentTypeSet tempComponentTypeSet = ComponentTypeSetUtility.Create<
                AssetLibraryInternal.KeyStorage,
                AssetLibraryInternal.KeyBufferElement>();

            CommandBuffer.AddComponent(ThreadIndex, entitiesToUpdatePtr, entitiesToUpdateCount, tempComponentTypeSet);
        }

        private int PrepareForUpdate(
            in ArchetypeChunk chunk,
            Entity* entityPtr,
            AssetLibraryInternal.Reference* referencePtr,
            ref BufferAccessor<AssetLibraryInternal.ConsumerEntityBufferElement> consumerEntityBufferAccessor,
            Entity* entitiesToUpdatePtr)
        {
            int entitiesToUpdateCount = 0;

            ref AssetLibraryConsumerEntityListTable bakedReferenceTableRO = ref BakedReferenceTableRef.GetRefReadOnly();

            for (int entityIndex = 0; entityIndex != chunk.Count; ++entityIndex)
            {
                UnityObjectRef<AssetLibrary> assetLibrary = referencePtr[entityIndex].AssetLibrary;
                ref UnsafeList<Entity> consumerEntityList = ref bakedReferenceTableRO.TryGet(assetLibrary, out bool wasRebaked);

                if (!wasRebaked)
                {
                    // Keep as-is
                    continue;
                }

                // Set for update
                Entity entity = entityPtr[entityIndex];
                entitiesToUpdatePtr[entitiesToUpdateCount++] = entity;

                // Add newly baked consumers
                DynamicBuffer<AssetLibraryInternal.ConsumerEntityBufferElement> consumerEntityBuffer = consumerEntityBufferAccessor[entityIndex];

                int oldLength = consumerEntityBuffer.Length;
                consumerEntityBuffer.EnsureCapacity(oldLength + consumerEntityList.Length);

                // Ptr won't move
                UnsafeSpan<Entity> existingConsumerEntitySpan = consumerEntityBuffer.AsSpanRO()[..oldLength].Reinterpret<Entity>();

                foreach (Entity consumerEntity in consumerEntityList)
                {
                    if (existingConsumerEntitySpan.Contains(consumerEntity))
                    {
                        // Already exists
                        continue;
                    }

                    // Add consumer
                    _ = consumerEntityBuffer.AddNoResize(new AssetLibraryInternal.ConsumerEntityBufferElement()
                    {
                        ConsumerEntity = consumerEntity
                    });
                }
            }

            return entitiesToUpdateCount;
        }
    }
}
