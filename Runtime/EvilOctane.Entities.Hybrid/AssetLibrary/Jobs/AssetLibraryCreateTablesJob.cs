using Unity.Assertions;
using Unity.Burst;
using Unity.Burst.CompilerServices;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using static EvilOctane.Entities.AssetLibraryAPI;
using AssetLibraryTable = EvilOctane.Collections.LowLevel.Unsafe.InPlaceSwissTable<EvilOctane.Entities.Internal.AssetLibraryKey, Unity.Entities.UnityObjectRef<UnityEngine.Object>, EvilOctane.Entities.Internal.AssetLibraryKeyHasher>;
using AssetLibraryTableHeader = EvilOctane.Collections.LowLevel.Unsafe.InPlaceSwissTableHeader<EvilOctane.Entities.Internal.AssetLibraryKey, Unity.Entities.UnityObjectRef<UnityEngine.Object>>;
using UnityObject = UnityEngine.Object;

namespace EvilOctane.Entities.Internal
{
    [BurstCompile]
    public unsafe partial struct AssetLibraryCreateTablesJob : IJobEntity
    {
        public void Execute(
            RefRO<BakedEntityNameComponent> entityName,
            DynamicBuffer<AssetLibraryInternal.KeyStorage> keyStorage,
            DynamicBuffer<AssetLibraryInternal.KeyBufferElement> keyBuffer,
            DynamicBuffer<AssetLibraryInternal.AssetBufferElement> assetBuffer,
            ref DynamicBuffer<AssetLibrary.Storage> storage)
        {
            Assert.AreEqual(keyBuffer.Length, assetBuffer.Length);

            nint tableSize = AssetLibraryTable.GetAllocationSize(keyBuffer.Length, out int capacityCeilGroupSize);
            nint totalSize = tableSize + keyStorage.Length;
            storage.ResizeUninitializedTrashOldData((int)totalSize);

            // Copy key storage
            storage.ReinterpretStorageRW(out byte* tableAssetNameStorage);
            tableAssetNameStorage += tableSize;

            UnsafeUtility.MemCpy(tableAssetNameStorage, keyStorage.GetUnsafeReadOnlyPtr(), keyStorage.Length);

            // Create table
            storage.ReinterpretStorageRW(out AssetLibraryTableHeader* assetLibrary);
            AssetLibraryTable.Initialize(assetLibrary, capacityCeilGroupSize);

            UnsafeSpan<AssetLibraryInternal.KeyBufferElement> keySpan = keyBuffer.AsSpanRO();
            UnsafeSpan<AssetLibraryInternal.AssetBufferElement> assetSpan = assetBuffer.AsSpanRO();

            for (int index = 0; index != keySpan.Length; ++index)
            {
                AssetLibraryInternal.KeyBufferElement tempKey = keySpan[index];
                ByteSpan tableAssetNameSpan = new(tableAssetNameStorage + tempKey.AssetNameOffset, tempKey.AssetNameLength);

                // Add to table
                AssetLibraryKey tableKey = new(tempKey.AssetTypeHash, tableAssetNameSpan);
                Ref<UnityObjectRef<UnityObject>> item = AssetLibraryTable.GetOrAddNoResize(assetLibrary, tableKey, out bool added);

                if (Hint.Likely(added))
                {
                    // Added
                    item.RefRW = assetSpan[index].Asset;
                }
                else
                {
                    // Duplicate
                    // This is a bug and should be handled by asset library's Scriptable Object
                    LogDuplicateAsset(entityName, tableKey);
                }
            }
        }
    }
}
