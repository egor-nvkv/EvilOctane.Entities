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
    [UpdateAfter(typeof(AssetLibraryLifetimeSystem))]
    [UpdateInGroup(typeof(AssetLibraryLifetimeSystemGroup))]
    [WorldSystemFilter(WorldSystemFilterFlags.BakingSystem)]
    public unsafe partial struct AssetLibraryLifetimeSystemManaged : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            foreach ((
                RefRW<BakedEntityNameComponent> entityName,
                AssetLibrary.UnityObjectComponent assetLibrary,
                DynamicBuffer<AssetLibraryInternal.AssetReferenceBufferElement> assetReferenceBuffer,
                Entity entity) in

                Query<
                    RefRW<BakedEntityNameComponent>,
                    AssetLibrary.UnityObjectComponent,
                    DynamicBuffer<AssetLibraryInternal.AssetReferenceBufferElement>>()
                .WithEntityAccess())
            {
                AssetLibrary assetLibrarySO = assetLibrary.Value;

                // Entity name
                _ = entityName.ValueRW.EntityName.CopyFromTruncated(assetLibrarySO.name);

                // Assets
                List<UnityObject> assets = assetLibrarySO.assets;

                assetReferenceBuffer.Clear();

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

                assetReferenceBuffer.EnsureCapacityTrashOldData(assetCount);

                foreach (UnityObject asset in assets)
                {
                    if (!asset)
                    {
                        // Invalid asset
                        continue;
                    }

                    // Asset reference
                    _ = assetReferenceBuffer.AddNoResize(new AssetLibraryInternal.AssetReferenceBufferElement()
                    {
                        Data = new AssetReferenceData()
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
