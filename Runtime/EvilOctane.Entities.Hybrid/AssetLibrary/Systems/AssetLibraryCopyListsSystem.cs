using System;
using System.Text;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Entities.LowLevel.Unsafe;
using static EvilOctane.Entities.AssetLibraryAPI;
using static Unity.Entities.SystemAPI;
using UnityObject = UnityEngine.Object;

namespace EvilOctane.Entities.Internal
{
    [UpdateAfter(typeof(AssetLibraryPrepareEntitiesSystem))]
    [UpdateInGroup(typeof(AssetLibraryBakingSystemGroup))]
    [WorldSystemFilter(WorldSystemFilterFlags.BakingSystem)]
    public unsafe partial struct AssetLibraryCopyListsSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            foreach ((
                RefRW<BakedEntityNameComponent> bakedEntityName,
                AssetLibraryInternal.Reference reference,
                DynamicBuffer<AssetLibraryInternal.KeyStorage> keyStorage,
                DynamicBuffer<AssetLibraryInternal.KeyBufferElement> keyBuffer,
                DynamicBuffer<AssetLibraryInternal.AssetBufferElement> assetBuffer,
                Entity entity) in

                Query<
                    RefRW<BakedEntityNameComponent>,
                    AssetLibraryInternal.Reference,
                    DynamicBuffer<AssetLibraryInternal.KeyStorage>,
                    DynamicBuffer<AssetLibraryInternal.KeyBufferElement>,
                    DynamicBuffer<AssetLibraryInternal.AssetBufferElement>>()
                .WithEntityAccess())
            {
                keyStorage.Clear();
                keyBuffer.Clear();
                assetBuffer.Clear();

                AssetLibrary assetLibrary = reference.AssetLibrary;

                // Entity name
                _ = bakedEntityName.ValueRW.EntityName.CopyFromTruncated(assetLibrary.name);

                if (assetLibrary.assets == null)
                {
                    // No assets
                    continue;
                }

                // Assets
                int assetCount = assetLibrary.assets.Count;

                keyStorage.EnsureCapacityTrashOldData(assetCount * 32);
                keyBuffer.EnsureCapacityTrashOldData(assetCount);
                assetBuffer.EnsureCapacityTrashOldData(assetCount);

                foreach (UnityObject asset in assetLibrary.assets)
                {
                    if (!asset)
                    {
                        // Invalid asset
                        continue;
                    }

                    string assetName = asset.name;
                    int maxAssetNameLengthUtf8 = assetName.Length + (assetName.Length / 2) + 1;

                    int keyStorageOldLength = keyStorage.Length;
                    keyStorage.EnsureCapacity(keyStorageOldLength + maxAssetNameLengthUtf8);

                    // Key storage
                    Span<byte> assetNameUtf8Span = new((byte*)keyStorage.GetUnsafePtr() + keyStorageOldLength, maxAssetNameLengthUtf8);
                    int assetNameLength = Encoding.UTF8.GetBytes(assetName, assetNameUtf8Span);

                    keyStorage.SetLengthNoResize(keyStorageOldLength + assetNameLength);

                    // Key
                    _ = keyBuffer.AddNoResize(new AssetLibraryInternal.KeyBufferElement()
                    {
                        AssetTypeHash = GetAssetTypeHash(asset.GetType()),
                        AssetNameOffset = keyStorageOldLength,
                        AssetNameLength = assetNameLength
                    });

                    // Value
                    _ = assetBuffer.AddNoResize(new AssetLibraryInternal.AssetBufferElement()
                    {
                        Asset = asset
                    });
                }
            }
        }
    }
}
