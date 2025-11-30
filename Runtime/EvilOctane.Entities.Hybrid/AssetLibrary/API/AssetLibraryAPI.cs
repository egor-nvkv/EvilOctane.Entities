using System;
using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Burst.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using static EvilOctane.Entities.Internal.AssetLibraryLogAPI;
using static System.Runtime.CompilerServices.Unsafe;
using static Unity.Collections.LowLevel.Unsafe.UnsafeUtility2;
using UnityObject = UnityEngine.Object;

namespace EvilOctane.Entities
{
    public static unsafe class AssetLibraryAPI
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong GetAssetTypeHash<T>()
            where T : UnityObject
        {
            return (ulong)BurstRuntime.GetHashCode64<T>();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong GetAssetTypeHash(Type type)
        {
            return (ulong)BurstRuntime.GetHashCode64(type);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void FindAssets(
            ref ComponentLookup<AssetLibrary.AssetTableComponent> assetTableLookupRO,
            UnsafeSpan<AssetLibrary.ReferenceBufferElement> assetLibrarySpanRO,
            ByteSpan assetName,
            ulong assetTypeHash,
            ref UnsafeList<Entity> outAssetList,
            bool stopOnFirstMatch)
        {
            outAssetList.Clear();

            AssetTableKey key = new(assetTypeHash, assetName);

            foreach (AssetLibrary.ReferenceBufferElement assetLibrary in assetLibrarySpanRO)
            {
                if (Hint.Unlikely(!assetTableLookupRO.TryGetRefRO(assetLibrary.Entity, out RefRO<AssetLibrary.AssetTableComponent> assetTable)))
                {
                    // Component missing
                    continue;
                }

                Pointer<AssetTableEntry> entry = assetTable.ValueRO.Value.Table.TryGet(key, out bool exists);

                if (!exists)
                {
                    // Not found
                    continue;
                }

                UnsafeList<Entity> entityList = entry.AsRef.EntityList;

                if (Hint.Unlikely(entityList.IsEmpty))
                {
                    // Empty
                    continue;
                }

                if (stopOnFirstMatch)
                {
                    // Add single
                    outAssetList.AddNoResize(entityList[0]);
                    break;
                }
                else
                {
                    // Add all
                    outAssetList.AddRange(entry.AsRef.EntityList);
                }
            }
        }

        public static AssetSearchResult FindAssetFirstMatch(
            ref ComponentLookup<AssetLibrary.AssetTableComponent> assetTableLookupRO,
            UnsafeSpan<AssetLibrary.ReferenceBufferElement> assetLibrarySpanRO,
            ByteSpan assetDescription,
            ByteSpan assetName,
            ulong assetTypeHash,
            AssetSearchOptions options,
            out Entity asset)
        {
            AssetSearchResult result;
            asset = Entity.Null;

            if (assetName.IsEmpty)
            {
                // Empty name
                result = AssetSearchResult.NameIsEmpty;
            }
            else
            {
                // Find

                UnsafeList<Entity> tempAssetList;

                fixed (Entity* assetPtr = &asset)
                {
                    tempAssetList = new UnsafeList<Entity>(assetPtr, 1)
                    {
                        m_length = 0
                    };

                    FindAssets(
                        ref assetTableLookupRO,
                        assetLibrarySpanRO,
                        assetName,
                        assetTypeHash,
                        ref tempAssetList,
                        stopOnFirstMatch: true);
                }

                result = tempAssetList.IsEmpty ? AssetSearchResult.NotFound : AssetSearchResult.Found;
            }

            if (result != AssetSearchResult.Found)
            {
                LogAssetSearchErrors(assetDescription, new AssetTableKey(assetTypeHash, assetName), options, result);
            }

            return result;
        }

        public static AssetSearchResult FindAssetFast(
            ref ComponentLookup<AssetLibrary.AssetTableComponent> assetTableLookupRO,
            ref ComponentLookup<Asset.UnityObjectComponent> unityObjectLookupRO,
            UnsafeSpan<AssetLibrary.ReferenceBufferElement> assetLibrarySpanRO,
            ByteSpan assetDescription,
            ByteSpan assetName,
            ulong assetTypeHash,
            AssetSearchOptions options,
            out UnityObjectRef<UnityObject> unityObjectRef)
        {
            AssetSearchResult result = FindAssetFirstMatch(
                ref assetTableLookupRO,
                assetLibrarySpanRO,
                assetDescription,
                assetName,
                assetTypeHash,
                options,
                out Entity asset);

            Asset.UnityObjectComponent unityObject = new();

            if (result == AssetSearchResult.Found)
            {
                if (!unityObjectLookupRO.TryGetComponent(asset, out unityObject))
                {
                    // Component missing
                    result = AssetSearchResult.NotFound;
                    LogAssetSearchErrors(assetDescription, new AssetTableKey(assetTypeHash, assetName), options, result);
                }
            }

            unityObjectRef = unityObject.Value;
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static AssetSearchResult FindAssetFast<T, S0, S1>(
            ref ComponentLookup<AssetLibrary.AssetTableComponent> assetTableLookupRO,
            ref ComponentLookup<Asset.UnityObjectComponent> unityObjectLookupRO,
            DynamicBuffer<AssetLibrary.ReferenceBufferElement> assetLibraryBuffer,
            in S0 assetDescription,
            in S1 assetName,
            AssetSearchOptions options,
            out UnityObjectRef<T> unityObjectRef)

            where S0 : unmanaged, INativeList<byte>, IUTF8Bytes
            where S1 : unmanaged, INativeList<byte>, IUTF8Bytes
            where T : UnityObject
        {
            AssetSearchResult result = FindAssetFast(
                ref assetTableLookupRO,
                ref unityObjectLookupRO,
                assetLibraryBuffer.AsSpanRO(),
                AsRef(in assetDescription).AsByteSpan(),
                AsRef(in assetName).AsByteSpan(),
                GetAssetTypeHash<T>(),
                options,
                out UnityObjectRef<UnityObject> unityObjectRefUntyped);

            unityObjectRef = ReinterpretExact<UnityObjectRef<UnityObject>, UnityObjectRef<T>>(ref unityObjectRefUntyped);
            return result;
        }
    }
}
