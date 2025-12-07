using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;

namespace EvilOctane.Entities.Internal
{
    [BurstCompile]
    [WithPresent(typeof(AssetLibrary.RebakedTag))]
    public unsafe partial struct AssetLibrarySortAssetsJob : IJobEntity
    {
        [ReadOnly]
        public EntityStorageInfoLookup EntityStorageInfoLookup;

        public void Execute(ref DynamicBuffer<AssetLibrary.AssetBufferElement> assetBuffer)
        {
            UnsafeSpan<Entity> assetSpan = assetBuffer.AsSpanRO().Reinterpret<Entity>();

            NativeSortExtension.Sort(assetSpan.Ptr, assetSpan.Length, new EntityInChunkComparer()
            {
                EntityStorageInfoLookup = EntityStorageInfoLookup
            });
        }
    }
}
