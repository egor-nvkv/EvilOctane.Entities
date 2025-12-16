using System.Runtime.CompilerServices;
using Unity.Assertions;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;

namespace EvilOctane.Entities.Internal
{
    [BurstCompile]
    public unsafe struct AssetLibraryGatherReferencesJob : IJobChunk
    {
        [ReadOnly]
        public ComponentTypeHandle<AdditionalEntityParent> AdditionalEntityParentTypeHandle;
        [ReadOnly]
        public ComponentTypeHandle<AssetLibraryConsumerAdditional.DeclaredReference> DeclaredReferenceTypeHandle;

        public NativeReference<AssetLibraryConsumerTable> ConsumerTableRef;
        public NativeReference<AssetLibraryRebakedSet> RebakedSetRef;

        public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
        {
            Assert.IsFalse(useEnabledMask);

            AdditionalEntityParent* parentPtrRO = chunk.GetRequiredComponentDataPtrROTyped(ref AdditionalEntityParentTypeHandle);
            AssetLibraryConsumerAdditional.DeclaredReference* referencePtrRO = chunk.GetRequiredComponentDataPtrROTyped(ref DeclaredReferenceTypeHandle);

            ref AssetLibraryConsumerTable consumerTable = ref ConsumerTableRef.GetRef();
            consumerTable.Value.EnsureSlack(chunk.Count);

            for (int entityIndex = 0; entityIndex != chunk.Count; ++entityIndex)
            {
                Entity consumer = parentPtrRO[entityIndex].Parent;
                UnityObjectRef<AssetLibrary> assetLibrary = referencePtrRO[entityIndex].AssetLibrary;

                Pointer<UnsafeList<Entity>> consumerList = consumerTable.Value.GetOrAddNoResize(assetLibrary, out bool added);

                if (added)
                {
                    // Create list
                    consumerList.AsRef = UnsafeListExtensions2.Create<Entity>(8, consumerTable.Value.Allocator);
                    consumerList.AsRef.AddNoResize(consumer);
                }
                else
                {
                    // List exists

                    if (!consumerList.AsRef.Contains(consumer))
                    {
                        consumerList.AsRef.Add(consumer);
                    }
                }
            }

            if (chunk.Has<AssetLibraryConsumerAdditional.RebakedTag>())
            {
                // Rebaked
                AddRebaked(in chunk, referencePtrRO);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void AddRebaked(in ArchetypeChunk chunk, AssetLibraryConsumerAdditional.DeclaredReference* referencePtrRO)
        {
            ref AssetLibraryRebakedSet rebakedSet = ref RebakedSetRef.GetRef();
            rebakedSet.Value.EnsureSlack(chunk.Count);

            for (int entityIndex = 0; entityIndex != chunk.Count; ++entityIndex)
            {
                UnityObjectRef<AssetLibrary> assetLibrary = referencePtrRO[entityIndex].AssetLibrary;
                _ = rebakedSet.Value.AddNoResize(assetLibrary);
            }
        }
    }
}
