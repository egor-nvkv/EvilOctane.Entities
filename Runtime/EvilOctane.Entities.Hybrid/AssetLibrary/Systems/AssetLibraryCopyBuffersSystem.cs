using System;
using System.Collections.Generic;
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
    public unsafe partial struct AssetLibraryCopyBuffersSystem : ISystem
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

                // Assets
                List<UnityObject> assets = assetLibrary.assets;

                if (assets == null)
                {
                    // No assets
                    continue;
                }

                int assetCount = assets.Count;

                if (assetCount == 0)
                {
                    // No assets
                    continue;
                }

                keyStorage.EnsureCapacityTrashOldData(assetCount * 32);
                keyBuffer.EnsureCapacityTrashOldData(assetCount);
                assetBuffer.EnsureCapacityTrashOldData(assetCount);

                foreach (UnityObject asset in assets)
                {
                    if (!asset)
                    {
                        // Invalid asset
                        continue;
                    }

                    string assetName = asset.name;
                    int assetNameMaxByteCount = Encoding.UTF8.GetMaxByteCount(assetName.Length);

                    int keyStorageOldLength = keyStorage.Length;
                    keyStorage.EnsureCapacity(keyStorageOldLength + assetNameMaxByteCount);

                    // Key storage
                    Span<byte> assetNameUtf8Span = new((byte*)keyStorage.GetUnsafePtr() + keyStorageOldLength, assetNameMaxByteCount);
                    int assetNameByteCount = Encoding.UTF8.GetBytes(assetName, assetNameUtf8Span);

                    keyStorage.SetLengthNoResize(keyStorageOldLength + assetNameByteCount);

                    // Key
                    _ = keyBuffer.AddNoResize(new AssetLibraryInternal.KeyBufferElement()
                    {
                        AssetTypeHash = GetAssetTypeHash(asset.GetType()),
                        AssetNameOffset = keyStorageOldLength,
                        AssetNameLength = assetNameByteCount
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
