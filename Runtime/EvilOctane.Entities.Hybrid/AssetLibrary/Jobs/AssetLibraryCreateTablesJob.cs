using Unity.Assertions;
using Unity.Burst;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using UnityEngine;
using AssetLibraryTable = EvilOctane.Collections.LowLevel.Unsafe.InPlaceSwissTable<EvilOctane.Entities.Internal.AssetLibraryKey, Unity.Entities.UnityObjectRef<UnityEngine.Object>, EvilOctane.Entities.Internal.AssetLibraryKeyHasher>;
using AssetLibraryTableHeader = EvilOctane.Collections.LowLevel.Unsafe.InPlaceSwissTableHeader<EvilOctane.Entities.Internal.AssetLibraryKey, Unity.Entities.UnityObjectRef<UnityEngine.Object>>;
using UnityObject = UnityEngine.Object;

namespace EvilOctane.Entities.Internal
{
    [BurstCompile]
    [WithOptions(EntityQueryOptions.IncludePrefab)]
    public unsafe partial struct AssetLibraryCreateTablesJob : IJobEntity
    {
        public void Execute(
            RefRO<BakedEntityNameComponent> entityName,
            ref DynamicBuffer<AssetLibraryInternal.KeyBufferElement> keyBuffer,
            ref DynamicBuffer<AssetLibraryInternal.AssetBufferElement> assetBuffer,
            ref DynamicBuffer<AssetLibrary.Storage> storage)
        {
            Assert.AreEqual(keyBuffer.Length, assetBuffer.Length);

            nint totalSize = AssetLibraryTable.GetAllocationSize(keyBuffer.Length, out int capacityCeilGroupSize);
            storage.ResizeUninitializedTrashOldData((int)totalSize);

            // Initialize
            AssetLibraryTableHeader* assetLibrary = (AssetLibraryTableHeader*)storage.GetUnsafePtr();
            AssetLibraryTable.Initialize(assetLibrary, capacityCeilGroupSize);

            // Copy key/values
            UnsafeSpan<AssetLibraryInternal.KeyBufferElement> keySpan = keyBuffer.AsSpanRO();
            UnsafeSpan<AssetLibraryInternal.AssetBufferElement> assetSpan = assetBuffer.AsSpanRO();

            for (int index = 0; index != keySpan.Length; ++index)
            {
                ref AssetLibraryKey keyRO = ref keySpan.ElementAt(index).Key;
                ref UnityObjectRef<UnityObject> item = ref AssetLibraryTable.GetOrAddNoResize(assetLibrary, keyRO, out bool added);

                if (added)
                {
                    // Add
                    item = assetSpan[index].Asset;
                }
                else
                {
                    Debug.LogError($"AssetLibrary | Multiple assets in library \"{entityName.ValueRO.EntityName}\" have the same name: \"{keyRO.GetAssetName()}\"");
                }
            }
        }
    }
}
