using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;

namespace EvilOctane.Entities.Internal
{
    [BurstCompile]
    [WithPresent(typeof(AssetLibrary.RebakedTag))]
    public partial struct AssetLibraryCreateAssetTablesJob : IJobEntity
    {
        private const Allocator allocator = Allocator.Domain;

        [ReadOnly]
        public BufferLookup<Asset.BakingNameStorage> BakingNameLookup;

        public void Execute(
            in DynamicBuffer<AssetLibrary.AssetBufferElement> assetBuffer,
            ref AssetLibrary.AssetTableComponent assetTable)
        {
            ref AssetTable table = ref assetTable.Value;

            if (table.IsCreated)
            {
                // Clear
                table.ClearEnsureCapacity(assetBuffer.Length);
            }
            else
            {
                // Allocate
                table = new AssetTable(assetBuffer.Length, allocator);
            }

            foreach (AssetLibrary.AssetBufferElement asset in assetBuffer)
            {
                if (!BakingNameLookup.TryGetBuffer(asset.Entity, out DynamicBuffer<Asset.BakingNameStorage> bakingName))
                {
                    // Component missing
                    continue;
                }

                table.AddNoResize(bakingName.AsSpanRO().Reinterpret<byte>().AsByteSpan(), asset.Entity);
            }
        }
    }
}
