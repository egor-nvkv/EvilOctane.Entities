using EvilOctane.Collections;
using System;
using Unity.Assertions;
using Unity.Burst;
using Unity.Burst.CompilerServices;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Entities.LowLevel.Unsafe;
using Unity.Mathematics;
using static System.Runtime.CompilerServices.Unsafe;
using UnityObject = UnityEngine.Object;

namespace EvilOctane.Entities.Internal
{
    [BurstCompile]
    public unsafe struct AssetLibraryUpdateAssetsJob : IJobChunk
    {
        [ReadOnly]
        public EntityTypeHandle EntityTypeHandle;

        public BufferTypeHandle<AssetLibrary.AssetBufferElement> AssetBufferTypeHandle;

        [ReadOnly]
        public BufferTypeHandle<AssetLibraryInternal.AssetReferenceBufferElement> AssetReferenceBufferTypeHandle;

        [ReadOnly]
        public ComponentLookup<Asset.UnityObjectComponent> UnityObjectLookup;

        public EntityArchetype AssetArchetype;

        public AllocatorManager.AllocatorHandle TempAllocator;
        public EntityCommandBuffer.ParallelWriter CommandBuffer;

        public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
        {
            Assert.IsFalse(useEnabledMask);

            Entity* entityPtr = chunk.GetEntityDataPtrRO(EntityTypeHandle);

            BufferAccessor<AssetLibrary.AssetBufferElement> assetBufferAccessor = chunk.GetBufferAccessorRW(ref AssetBufferTypeHandle);
            BufferAccessor<AssetLibraryInternal.AssetReferenceBufferElement> assetReferenceBufferAccessor = chunk.GetBufferAccessorRO(ref AssetReferenceBufferTypeHandle);

            AssetReferenceTable referenceTable = new();
            AssetInstanceTable instanceTable = new();

            UnsafeList<Entity> newAssetList = new();

            UnsafeList<Entity> toRebakeList = UnsafeListExtensions2.Create<Entity>(32, TempAllocator);
            UnsafeList<AssetReferenceData> rebakeDataList = UnsafeListExtensions2.Create<AssetReferenceData>(32, TempAllocator);

            UnsafeList<Entity> toDestroyList = UnsafeListExtensions2.Create<Entity>(16, TempAllocator);

            for (int entityIndex = 0; entityIndex != chunk.Count; ++entityIndex)
            {
                DynamicBuffer<AssetLibraryInternal.AssetReferenceBufferElement> assetReferenceBuffer = assetReferenceBufferAccessor[entityIndex];

                CreateReferenceTable(
                    ref referenceTable,
                    assetReferenceBuffer.AsSpanRO());

                DynamicBuffer<AssetLibrary.AssetBufferElement> assetBuffer = assetBufferAccessor[entityIndex];

                CreateInstanceTableDestroyInvalid(
                    ref referenceTable,
                    ref instanceTable,
                    ref toRebakeList,
                    ref rebakeDataList,
                    ref toDestroyList,
                    assetBuffer);

                Entity entity = entityPtr[entityIndex];

                CreateMissing(
                    unfilteredChunkIndex,
                    ref referenceTable,
                    ref instanceTable,
                    ref newAssetList,
                    entity);
            }

            // Update

            UpdateRebaked(
                unfilteredChunkIndex,
                toRebakeList.AsSpan(),
                rebakeDataList.AsSpan());

            // Cleanup

            if (!toDestroyList.IsEmpty)
            {
                CommandBuffer.DestroyEntity(unfilteredChunkIndex, toDestroyList.AsSpan());
            }

            CommandBuffer.RemoveComponent<AssetLibraryInternal.AssetReferenceBufferElement>(unfilteredChunkIndex, entityPtr, chunk.Count);
        }

        private void CreateReferenceTable(
            ref AssetReferenceTable referenceTable,
            UnsafeSpan<AssetLibraryInternal.AssetReferenceBufferElement> assetReferenceSpanRO)
        {
            if (referenceTable.Value.IsCreated)
            {
                referenceTable.Value.Clear();
                referenceTable.Value.EnsureCapacity(assetReferenceSpanRO.Length, keepOldData: false);
            }
            else
            {
                referenceTable = new AssetReferenceTable(assetReferenceSpanRO.Length, TempAllocator);
            }

            foreach (AssetLibraryInternal.AssetReferenceBufferElement assetReference in assetReferenceSpanRO)
            {
                Pointer<AssetReferenceData> referenceData = referenceTable.Value.GetOrAddNoResize(assetReference.Asset, out bool added);

                if (added)
                {
                    referenceData.AsRef = assetReference.Data;
                }
            }
        }

        private void CreateInstanceTableDestroyInvalid(
            ref AssetReferenceTable referenceTableRO,
            ref AssetInstanceTable instanceTable,
            ref UnsafeList<Entity> toRebakeList,
            ref UnsafeList<AssetReferenceData> rebakeDataList,
            ref UnsafeList<Entity> toDestroyList,
            DynamicBuffer<AssetLibrary.AssetBufferElement> assetBuffer)
        {
            if (instanceTable.Value.IsCreated)
            {
                instanceTable.Value.Clear();
                instanceTable.Value.EnsureCapacity(assetBuffer.Length, keepOldData: false);
            }
            else
            {
                instanceTable = new AssetInstanceTable(assetBuffer.Length, TempAllocator);
            }

            int maxRebakeCount = math.min(assetBuffer.Length, referenceTableRO.Value.Count);
            toRebakeList.EnsureSlack(maxRebakeCount);
            rebakeDataList.EnsureSlack(maxRebakeCount);

            toDestroyList.EnsureSlack(assetBuffer.Length);

            for (int index = 0; index != assetBuffer.Length;)
            {
                Entity asset = assetBuffer[index].Entity;

                if (!UnityObjectLookup.TryGetComponent(asset, out Asset.UnityObjectComponent assetObj))
                {
                    // Invalid
                    goto Remove;
                }

                Pointer<AssetReferenceData> referenceData = referenceTableRO.Value.TryGet(assetObj.Ref, out bool exists);

                if (!exists)
                {
                    // Not referenced
                    goto Remove;
                }

                Pointer<Entity> instance = instanceTable.Value.GetOrAddNoResize(assetObj.Ref, out bool added);

                if (!added)
                {
                    // Duplicate
                    goto Remove;
                }

                instance.AsRef = asset;

                // Until there is a way to detect changes on per-asset basis,
                // everything referenced gets rebaked

                toRebakeList.AddNoResize(asset);
                rebakeDataList.AddNoResize(referenceData.AsRef);

                ++index;
                continue;

            Remove:
                assetBuffer.RemoveAtSwapBack(index);
                toDestroyList.AddNoResize(asset);
            }
        }

        private void CreateMissing(
            int unfilteredChunkIndex,
            ref AssetReferenceTable referenceTableRO,
            ref AssetInstanceTable instanceTableRO,
            ref UnsafeList<Entity> newAssetList,
            Entity entity)
        {
            if (newAssetList.IsCreated)
            {
                newAssetList.Clear();
                newAssetList.EnsureCapacity(referenceTableRO.Value.Count, keepOldData: false);
            }
            else
            {
                newAssetList = UnsafeListExtensions2.Create<Entity>(referenceTableRO.Value.Count, TempAllocator);
            }

            // Create

            foreach (KeyValueRef<UnityObjectRef<UnityObject>, AssetReferenceData> kvPair in referenceTableRO.Value)
            {
                _ = instanceTableRO.Value.TryGet(kvPair.KeyRefRO, out bool exists);

                if (exists)
                {
                    // Exists
                    continue;
                }

                // Create
                Entity asset = CommandBuffer.CreateEntity(unfilteredChunkIndex, AssetArchetype);
                newAssetList.AddNoResize(asset);

                // Unity object

                CommandBuffer.SetComponent(unfilteredChunkIndex, asset, new Asset.UnityObjectComponent()
                {
                    Ref = kvPair.KeyRefRO,
                    TypeHash = kvPair.ValueRef.TypeHash
                });

                SetName(
                    unfilteredChunkIndex,
                    asset,
                    kvPair.ValueRef);

                SetBakingName(
                    unfilteredChunkIndex,
                    asset,
                    kvPair.ValueRef);
            }

            // Register

            foreach (Entity asset in newAssetList)
            {
                CommandBuffer.AppendToBuffer(unfilteredChunkIndex, entity, new AssetLibrary.AssetBufferElement() { Entity = asset });
            }

            CommandBuffer.SetSharedComponent(unfilteredChunkIndex, newAssetList.AsSpan(), new AssetLibrary.AssetBufferElement.OwnerShared()
            {
                AssetLibrary = entity
            });
        }

        private void UpdateRebaked(
            int unfilteredChunkIndex,
            UnsafeSpan<Entity> toRebakeSpanRO,
            UnsafeSpan<AssetReferenceData> rebakeDataSpanRO)
        {
            // Rebaked tag
            CommandBuffer.AddComponent<Asset.RebakedTag>(unfilteredChunkIndex, toRebakeSpanRO);

            // Entity name

            for (int index = 0; index != toRebakeSpanRO.Length; ++index)
            {
                SetName(
                    unfilteredChunkIndex,
                    toRebakeSpanRO[index],
                    rebakeDataSpanRO[index]);
            }

            // Baking name

            for (int index = 0; index != toRebakeSpanRO.Length; ++index)
            {
                SetBakingName(
                    unfilteredChunkIndex,
                    toRebakeSpanRO[index],
                    rebakeDataSpanRO[index]);
            }
        }

        [SkipLocalsInit]
        private void SetName(
            int unfilteredChunkIndex,
            Entity asset,
            AssetReferenceData referenceData)
        {
            SkipInit(out FixedString64Bytes entityName);
            entityName.Length = 0;

            entityName.AppendTruncateUnchecked(referenceData.Name);
            CommandBuffer.SetName(unfilteredChunkIndex, asset, entityName);
        }

        private void SetBakingName(
            int unfilteredChunkIndex,
            Entity asset,
            AssetReferenceData referenceData)
        {
            DynamicBuffer<Asset.BakingNameStorage> bakingName = CommandBuffer.AddBuffer<Asset.BakingNameStorage>(unfilteredChunkIndex, asset);
            bakingName.Reinterpret<byte>().CopyFrom(referenceData.Name.AsByteSpan().AsUnsafeSpan());
        }
    }
}
