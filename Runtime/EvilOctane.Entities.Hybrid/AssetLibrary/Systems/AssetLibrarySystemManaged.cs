using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Entities.LowLevel.Unsafe;
using static EvilOctane.Entities.AssetLibraryLowLevelAPI;
using static Unity.Entities.SystemAPI;
using UnityObject = UnityEngine.Object;

namespace EvilOctane.Entities.Internal
{
    [UpdateAfter(typeof(AssetLibrarySystem))]
    [UpdateInGroup(typeof(AssetLibraryLifetimeSystemGroup))]
    [WorldSystemFilter(WorldSystemFilterFlags.BakingSystem)]
    public unsafe partial struct AssetLibrarySystemManaged : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            foreach ((
                RefRW<BakedEntityNameComponent> entityName,
                AssetLibrary.UnityObjectComponent assetLibrary,
                DynamicBuffer<AssetLibraryInternal.AssetDataBufferElement> assetDataBuffer,
                Entity entity) in

                Query<
                    RefRW<BakedEntityNameComponent>,
                    AssetLibrary.UnityObjectComponent,
                    DynamicBuffer<AssetLibraryInternal.AssetDataBufferElement>>()
                .WithEntityAccess())
            {
                AssetLibrary assetLibrarySO = assetLibrary.Value;

                // Entity name
                _ = entityName.ValueRW.EntityName.CopyFromTruncated(assetLibrarySO.name);

                // Assets
                List<UnityObject> assets = assetLibrarySO.assets;

                assetDataBuffer.Clear();

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

                assetDataBuffer.EnsureCapacityTrashOldData(assetCount);

                foreach (UnityObject asset in assets)
                {
                    if (!asset)
                    {
                        // Invalid asset
                        continue;
                    }

                    // Asset data
                    _ = assetDataBuffer.AddNoResize(new AssetLibraryInternal.AssetDataBufferElement()
                    {
                        Data = new AssetData()
                        {
                            Name = UnsafeTextExtensions2.Create(asset.name, state.WorldUpdateAllocator),
                            TypeHash = GetAssetTypeHash(asset.GetType())
                        },
                        Asset = asset
                    });
                }
            }
        }
    }
}
