using Unity.Assertions;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace EvilOctane.Entities.Internal
{
    [BurstCompile]
    public partial struct AssetLibraryCreateAssetTablesJob : IJobEntity
    {
        private const Allocator allocator = Allocator.Domain;

        public void Execute(
            in DynamicBuffer<AssetLibrary.AssetBufferElement> assetBuffer,
            in DynamicBuffer<AssetLibraryInternal.TempAssetBufferElement> tempAssetBuffer,
            ref AssetLibrary.AssetTableComponent assetTable)
        {
            Assert.AreEqual(assetBuffer.Length, tempAssetBuffer.Length);

            ref AssetTable table = ref assetTable.Value;

            if (table.IsCreated)
            {
                // Clear
                table.ClearEnsureCapacity(tempAssetBuffer.Length);
            }
            else
            {
                // Allocate
                table = new AssetTable(tempAssetBuffer.Length, allocator);
            }

            for (int index = 0; index != assetBuffer.Length; ++index)
            {
                Entity asset = assetBuffer[index].Entity;
                AssetLibraryInternal.TempAssetBufferElement tempAsset = tempAssetBuffer[index];
                table.AddNoResize(tempAsset.TypeHash, tempAsset.Name, asset);
            }
        }
    }
}
