using Unity.Assertions;
using Unity.Burst;
using Unity.Burst.CompilerServices;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Entities.LowLevel.Unsafe;
using static System.Runtime.CompilerServices.Unsafe;

namespace EvilOctane.Entities.Internal
{
    [BurstCompile]
    public unsafe struct AssetLibraryGCJob : IJobChunk
    {
        [ReadOnly]
        public EntityTypeHandle EntityTypeHandle;

        [ReadOnly]
        public ComponentTypeHandle<AssetLibrary.UnityObjectComponent> UnityObjectLookup;

        [ReadOnly]
        public NativeReference<AssetLibraryConsumerTable> ConsumerTableRef;

        public EntityCommandBuffer.ParallelWriter CommandBuffer;

        [SkipLocalsInit]
        public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
        {
            Assert.IsFalse(useEnabledMask);

            Entity* entityPtr = chunk.GetEntityDataPtrRO(EntityTypeHandle);
            AssetLibrary.UnityObjectComponent* unityObjectPtrRO = chunk.GetRequiredComponentDataPtrROTyped(ref UnityObjectLookup);

            Entity* toDestroyPtr = stackalloc Entity[TypeManager.MaximumChunkCapacity];
            int toDestroyCount = 0;

            ref AssetLibraryConsumerTable consumerTableRO = ref AsRef(in ConsumerTableRef.GetRefReadOnly());

            for (int entityIndex = 0; entityIndex != chunk.Count; ++entityIndex)
            {
                Entity entity = entityPtr[entityIndex];
                UnityObjectRef<AssetLibrary> assetLibrary = unityObjectPtrRO[entityIndex].Value;

                if (assetLibrary == new UnityObjectRef<AssetLibrary>())
                {
                    // Null
                    toDestroyPtr[toDestroyCount++] = entity;
                }
                else
                {
                    _ = consumerTableRO.Value.TryGet(assetLibrary, out bool exists);

                    if (!exists)
                    {
                        // Not referenced
                        toDestroyPtr[toDestroyCount++] = entity;
                    }
                }
            }

            if (toDestroyCount != 0)
            {
                // Destroy
                CommandBuffer.DestroyEntity(unfilteredChunkIndex, toDestroyPtr, toDestroyCount);
            }
        }
    }
}
