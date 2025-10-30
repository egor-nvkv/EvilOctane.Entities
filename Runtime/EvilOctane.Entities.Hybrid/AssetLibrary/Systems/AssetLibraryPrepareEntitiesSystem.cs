using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Entities.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine;
using static System.Runtime.CompilerServices.Unsafe;
using static Unity.Entities.SystemAPI;

namespace EvilOctane.Entities.Internal
{
    [UpdateAfter(typeof(AssetLibraryCreateEntitiesSystem))]
    [UpdateInGroup(typeof(BakingSystemGroup), OrderFirst = true)]
    [WorldSystemFilter(WorldSystemFilterFlags.BakingSystem)]
    public unsafe partial struct AssetLibraryPrepareEntitiesSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            int keyValueSize = sizeof(Collections.KeyValue<AssetLibraryKey, UnityObjectRef<Object>>);

            if (keyValueSize != math.ceilpow2(AssetLibraryKey.Size))
            {
                Debug.LogWarning($"AssetLibrary | KeyValue is not the expected size: {keyValueSize}.");
            }
        }

        public void OnUpdate(ref SystemState state)
        {
            foreach ((
                RefRW<BakedEntityNameComponent> bakedEntityName,
                AssetLibraryInternal.Reference reference,
                DynamicBuffer<AssetLibraryInternal.KeyBufferElement> keyBuffer,
                DynamicBuffer<AssetLibraryInternal.AssetBufferElement> assetBuffer,
                Entity entity) in

                Query<
                    RefRW<BakedEntityNameComponent>,
                    AssetLibraryInternal.Reference,
                    DynamicBuffer<AssetLibraryInternal.KeyBufferElement>,
                    DynamicBuffer<AssetLibraryInternal.AssetBufferElement>>()
                .WithEntityAccess()
                .WithOptions(EntityQueryOptions.IncludePrefab))
            {
                keyBuffer.Clear();
                assetBuffer.Clear();

                AssetLibrary assetLibrary = reference.AssetLibrary;

                if (!assetLibrary)
                {
                    // Invalid reference
                    continue;
                }

                // Entity name
                SkipInit(out FixedString64Bytes entityName);
                entityName.Length = 0;

                _ = entityName.CopyFromTruncated(assetLibrary.name);

                state.EntityManager.SetName(entity, entityName);
                bakedEntityName.ValueRW.EntityName = entityName;

                if (assetLibrary.assets == null)
                {
                    // No assets
                    continue;
                }

                // Assets
                int assetCount = assetLibrary.assets.Count;

                keyBuffer.EnsureCapacityTrashOldData(assetCount);
                assetBuffer.EnsureCapacityTrashOldData(assetCount);

                foreach (Object asset in assetLibrary.assets)
                {
                    if (!asset)
                    {
                        // Invalid asset
                        continue;
                    }

                    // Key

                    int oldLength = keyBuffer.Length;
                    keyBuffer.SetLengthNoResize(oldLength + 1);

                    ref AssetLibraryInternal.KeyBufferElement key = ref keyBuffer.ElementAt(oldLength);

                    key = new AssetLibraryInternal.KeyBufferElement()
                    {
                        Key = new AssetLibraryKey(asset)
                    };

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
