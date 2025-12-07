using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;

namespace EvilOctane.Entities.Internal
{
    [BurstCompile]
    [WithOptions(EntityQueryOptions.IncludePrefab | EntityQueryOptions.IncludeDisabledEntities)]
    public partial struct AssetLibraryGatherReferencesJob : IJobEntity, IJobEntityChunkBeginEnd
    {
        public NativeReference<AssetLibraryConsumerTable> ConsumerTableRef;
        public NativeReference<AssetLibraryRebakedSet> RebakedSetRef;

        private bool rebaked;

        public void Execute(
            in AdditionalEntityParent parent,
            in AssetLibraryConsumerAdditional.DeclaredReference reference)
        {
            ref AssetLibraryConsumerTable consumerTable = ref ConsumerTableRef.GetRef();
            Pointer<UnsafeList<Entity>> consumerList = consumerTable.Value.GetOrAddNoResize(reference.AssetLibrary, out bool added);

            Entity consumer = parent.Parent;

            if (added)
            {
                // List added
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

            if (rebaked)
            {
                // Rebaked
                _ = RebakedSetRef.GetRef().Value.AddNoResize(reference.AssetLibrary);
            }
        }

        public bool OnChunkBegin(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
        {
            ConsumerTableRef.GetRef().Value.EnsureSlack(chunk.Count);

            rebaked = chunk.Has<AssetLibraryConsumerAdditional.RebakedTag>();

            if (rebaked)
            {
                RebakedSetRef.GetRef().Value.EnsureSlack(chunk.Count);
            }

            return true;
        }

        public void OnChunkEnd(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask, bool chunkWasExecuted)
        {
        }
    }
}
