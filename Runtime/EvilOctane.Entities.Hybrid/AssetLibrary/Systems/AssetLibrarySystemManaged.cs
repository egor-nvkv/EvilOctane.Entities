using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Entities.LowLevel.Unsafe;
using static EvilOctane.Entities.AssetLibraryAPI;
using static System.Runtime.CompilerServices.Unsafe;
using static Unity.Entities.SystemAPI;
using UnityObject = UnityEngine.Object;

namespace EvilOctane.Entities.Internal
{
    [UpdateAfter(typeof(AssetLibraryReferenceSystem))]
    [UpdateInGroup(typeof(AssetLibraryBeforeAssetBakingSystemGroup))]
    [WorldSystemFilter(WorldSystemFilterFlags.BakingSystem)]
    public unsafe partial struct AssetLibrarySystemManaged : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            foreach ((
                AssetLibrary.UnityObjectComponent assetLibrary,
                DynamicBuffer<AssetLibraryInternal.TempAssetBufferElement> tempAssetBuffer,
                Entity entity) in

                Query<
                    AssetLibrary.UnityObjectComponent,
                    DynamicBuffer<AssetLibraryInternal.TempAssetBufferElement>>()
                .WithEntityAccess())
            {
                AssetLibrary assetLibrarySO = assetLibrary.Value;

                // Entity name
                SkipInit(out FixedString64Bytes entityName);
                entityName.Length = 0;

                _ = entityName.CopyFromTruncated(assetLibrarySO.name);
                state.EntityManager.SetName(entity, entityName);

                // Assets
                List<UnityObject> assets = assetLibrarySO.assets;

                tempAssetBuffer.Clear();

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

                tempAssetBuffer.EnsureCapacityTrashOldData(assetCount);

                foreach (UnityObject asset in assets)
                {
                    if (!asset)
                    {
                        // Invalid asset
                        continue;
                    }

                    _ = tempAssetBuffer.AddNoResize(new AssetLibraryInternal.TempAssetBufferElement()
                    {
                        Asset = asset,
                        TypeHash = GetAssetTypeHash(asset.GetType()),
                        Name = UnsafeTextExtensions2.Create(asset.name, state.WorldUpdateAllocator)
                    });
                }
            }
        }
    }
}
