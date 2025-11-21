using System;
using Unity.Assertions;
using Unity.Burst;
using Unity.Burst.CompilerServices;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Entities.LowLevel.Unsafe;
using UnityEngine;

namespace EvilOctane.Entities.Internal
{
    [BurstCompile]
    public unsafe struct AssetLibraryGarbageCollectJob : IJobChunk
    {
        [ReadOnly]
        public EntityTypeHandle EntityTypeHandle;

        [ReadOnly]
        public ComponentTypeHandle<AssetLibraryInternal.Reference> ReferenceTypeHandle;
        public BufferTypeHandle<AssetLibraryInternal.ConsumerBufferElement> ConsumerBufferTypeHandle;

        public BufferLookup<AssetLibrary.EntityBufferElement> AssetLibraryEntityBufferLookup;

        public EntityCommandBuffer CommandBuffer;

        private static void GetEntities(
            Entity* entityPtrIn,
            Entity* entityPtrOut,
            int* entityIndexPtr,
            int count)
        {
            for (int index = 0; index != count; ++index)
            {
                int entityIndex = entityIndexPtr[index];
                Entity entity = entityPtrIn[entityIndex];
                entityPtrOut[index] = entity;
            }
        }

        [SkipLocalsInit]
        private static void SplitEntityIndices(
            in ArchetypeChunk chunk,
            AssetLibraryInternal.Reference* referencePtr,
            int* validEntityIndexPtr,
            ref int validEntityCount,
            int* invalidEntityIndexPtr,
            ref int invalidEntityCount)
        {
            bool* isValidPtr = stackalloc bool[TypeManager.MaximumChunkCapacity];

            // Get is valid
            UnsafeSpan<AssetLibraryInternal.Reference> referenceSpan = new(referencePtr, chunk.Count);
            UnsafeSpan<bool> isValidSpan = new(isValidPtr, chunk.Count);
            Resources.EntityIdsToValidArray(referenceSpan.Reinterpret<EntityId>(), isValidSpan);

            // Split indices
            for (int entityIndex = 0; entityIndex != chunk.Count; ++entityIndex)
            {
                bool isValid = isValidPtr[entityIndex];

                int* destPtr = isValid ? validEntityIndexPtr : invalidEntityIndexPtr;
                ref int destEntityCount = ref isValid ? ref validEntityCount : ref invalidEntityCount;

                destPtr[destEntityCount++] = entityIndex;
            }
        }

        [SkipLocalsInit]
        public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
        {
            Assert.IsFalse(useEnabledMask);

            Entity* entityPtr = chunk.GetEntityDataPtrRO(EntityTypeHandle);

            AssetLibraryInternal.Reference* referencePtr = chunk.GetRequiredComponentDataPtrROTyped(ref ReferenceTypeHandle);
            BufferAccessor<AssetLibraryInternal.ConsumerBufferElement> consumerBufferAccessor = chunk.GetBufferAccessorRW(ref ConsumerBufferTypeHandle);

            int* invalidEntityIndexPtr = stackalloc int[TypeManager.MaximumChunkCapacity];
            int invalidEntityCount = 0;

            int* setToInvalidEntityIndexPtr = stackalloc int[TypeManager.MaximumChunkCapacity];
            int setToInvalidCount = 0;

            // Valid Entity index stackalloc scope
            {
                int* validEntityIndexPtr = stackalloc int[TypeManager.MaximumChunkCapacity];
                int validEntityCount = 0;

                SplitEntityIndices(
                    in chunk,
                    referencePtr,
                    validEntityIndexPtr,
                    ref validEntityCount,
                    invalidEntityIndexPtr,
                    ref invalidEntityCount);

                CheckAccessibleFromConsumers(
                    entityPtr,
                    ref consumerBufferAccessor,
                    validEntityIndexPtr,
                    validEntityCount,
                    setToInvalidEntityIndexPtr,
                    ref setToInvalidCount);
            }

            bool allValid = invalidEntityCount + setToInvalidCount == 0;

            if (Hint.Likely(allValid))
            {
                // All valid
                return;
            }

            CleanupConsumersOfInvalid(
                entityPtr,
                ref consumerBufferAccessor,
                invalidEntityIndexPtr,
                invalidEntityCount);

            DestroyInvalid(
                entityPtr,
                invalidEntityIndexPtr,
                invalidEntityCount,
                setToInvalidEntityIndexPtr,
                setToInvalidCount);
        }

        private void CheckAccessibleFromConsumers(
            Entity* entityPtr,
            ref BufferAccessor<AssetLibraryInternal.ConsumerBufferElement> consumerBufferAccessor,
            int* validEntityIndexPtr,
            int validEntityCount,
            int* setToInvalidEntityIndexPtr,
            ref int setToInvalidCount)
        {
            for (int index = 0; index != validEntityCount; ++index)
            {
                int entityIndex = validEntityIndexPtr[index];
                Entity entity = entityPtr[entityIndex];

                // Check whether we're still referenced by something
                DynamicBuffer<AssetLibraryInternal.ConsumerBufferElement> consumerBuffer = consumerBufferAccessor[entityIndex];

                for (int consumerIndex = 0; consumerIndex != consumerBuffer.Length;)
                {
                    AssetLibraryInternal.ConsumerBufferElement consumerEntity = consumerBuffer[consumerIndex];

                    if (!AssetLibraryEntityBufferLookup.TryGetBuffer(consumerEntity.Entity, out DynamicBuffer<AssetLibrary.EntityBufferElement> assetLibraryEntityBuffer))
                    {
                        // No asset library references
                        goto Remove;
                    }

                    bool hasReference = assetLibraryEntityBuffer.AsSpanRO().Reinterpret<Entity>().Contains(entity);

                    if (!hasReference)
                    {
                        // No references to this asset library
                        goto Remove;
                    }

                    ++consumerIndex;
                    continue;

                Remove:
                    consumerBuffer.RemoveAtSwapBack(consumerIndex);
                }

                bool stillAccessible = !consumerBuffer.IsEmpty;

                if (!stillAccessible)
                {
                    // Set to invalid
                    setToInvalidEntityIndexPtr[setToInvalidCount++] = entityIndex;
                }
            }
        }

        private void CleanupConsumersOfInvalid(
            Entity* entityPtr,
            ref BufferAccessor<AssetLibraryInternal.ConsumerBufferElement> consumerBufferAccessor,
            int* invalidEntityIndexPtr,
            int invalidEntityCount)
        {
            for (int index = 0; index != invalidEntityCount; ++index)
            {
                int entityIndex = invalidEntityIndexPtr[index];
                Entity entity = entityPtr[entityIndex];

                // Clean up whoever is still referencing us
                DynamicBuffer<AssetLibraryInternal.ConsumerBufferElement> consumerBuffer = consumerBufferAccessor[entityIndex];

                foreach (AssetLibraryInternal.ConsumerBufferElement consumerEntity in consumerBuffer)
                {
                    if (AssetLibraryEntityBufferLookup.TryGetBuffer(consumerEntity.Entity, out DynamicBuffer<AssetLibrary.EntityBufferElement> assetLibraryEntityBuffer))
                    {
                        // Remove reference
                        _ = assetLibraryEntityBuffer.Reinterpret<Entity>().RemoveFirstMatchSwapBack(entity);
                    }
                }

                consumerBuffer.Clear();
            }
        }

        [SkipLocalsInit]
        private void DestroyInvalid(
            Entity* entityPtr,
            int* invalidEntityIndexPtr,
            int invalidEntityCount,
            int* setToInvalidEntityIndexPtr,
            int setToInvalidCount)
        {
            Entity* entitiesToDestroy = stackalloc Entity[TypeManager.MaximumChunkCapacity];

            // Invalid
            GetEntities(
                entityPtr,
                entitiesToDestroy,
                invalidEntityIndexPtr,
                invalidEntityCount);

            // Set to invalid
            GetEntities(
                entityPtr,
                entitiesToDestroy + invalidEntityCount,
                setToInvalidEntityIndexPtr,
                setToInvalidCount);

            // Destroy
            CommandBuffer.DestroyEntity(entitiesToDestroy, invalidEntityCount);
        }
    }
}
