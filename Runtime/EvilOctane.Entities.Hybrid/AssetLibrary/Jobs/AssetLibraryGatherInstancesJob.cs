using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;

namespace EvilOctane.Entities.Internal
{
    [BurstCompile]
    [WithOptions(EntityQueryOptions.IncludeDisabledEntities)]
    public partial struct AssetLibraryGatherInstancesJob : IJobEntity, IJobEntityChunkBeginEnd
    {
        public NativeReference<AssetLibraryInstanceTable> InstanceTableRef;

        public EntityCommandBuffer CommandBuffer;

        public void Execute(
            Entity entity,
            in AssetLibrary.UnityObjectComponent assetLibrary)
        {
            ref AssetLibraryInstanceTable instanceTable = ref InstanceTableRef.GetRef();
            Pointer<Entity> instance = instanceTable.Value.GetOrAddNoResize(assetLibrary.Value, out bool added);

            if (!added)
            {
                // Duplicate
                CommandBuffer.DestroyEntity(entity);
            }

            instance.AsRef = entity;
        }

        public bool OnChunkBegin(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
        {
            InstanceTableRef.GetRef().Value.EnsureSlack(chunk.Count);
            return true;
        }

        public void OnChunkEnd(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask, bool chunkWasExecuted)
        {
        }
    }
}
