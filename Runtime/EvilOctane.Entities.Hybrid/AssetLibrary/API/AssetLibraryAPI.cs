using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using UnityEngine;
using static EvilOctane.Entities.AssetLibraryLowLevelAPI;
using static System.Runtime.CompilerServices.Unsafe;
using static Unity.Collections.LowLevel.Unsafe.UnsafeUtility2;
using UnityObject = UnityEngine.Object;

namespace EvilOctane.Entities
{
    public static class AssetLibraryAPI
    {
        [HideInCallstack]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static AssetSearchResult FindAssetFast<T, S0, S1>(
            ref ComponentLookup<Asset.UnityObjectComponent> unityObjectLookupRO,
            Entity assetLibrary,
            in AssetTable assetTable,
            in S0 assetDescription,
            in S1 assetName,
            AssetSearchOptions options,
            out UnityObjectRef<T> asset)

            where T : UnityObject
            where S0 : unmanaged, INativeList<byte>, IUTF8Bytes
            where S1 : unmanaged, INativeList<byte>, IUTF8Bytes
        {
            AssetSearcherFirstMatch searcher = new()
            {
                UnityObjectLookup = unityObjectLookupRO,
                AssetTypeHash = GetAssetTypeHash<T>()
            };

            AssetSearchResult result = SearchAssets(
                assetLibrary,
                in AsRef(in assetTable),
                AsRef(in assetDescription).AsByteSpan(),
                AsRef(in assetName).AsByteSpan(),
                ref searcher,
                options);

            asset = ReinterpretExact<UnityObjectRef<UnityObject>, UnityObjectRef<T>>(ref searcher.ResultAsset);
            return result;
        }

        [HideInCallstack]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static AssetSearchResult FindAssetFast<T, S0, S1>(
            ref ComponentLookup<AssetLibrary.AssetTableComponent> assetTableLookupRO,
            ref ComponentLookup<Asset.UnityObjectComponent> unityObjectLookupRO,
            DynamicBuffer<AssetLibraryConsumer.AssetLibraryBufferElement> assetLibraryBuffer,
            in S0 assetDescription,
            in S1 assetName,
            AssetSearchOptions options,
            out UnityObjectRef<T> asset)

            where T : UnityObject
            where S0 : unmanaged, INativeList<byte>, IUTF8Bytes
            where S1 : unmanaged, INativeList<byte>, IUTF8Bytes
        {
            AssetSearcherFirstMatch searcher = new()
            {
                UnityObjectLookup = unityObjectLookupRO,
                AssetTypeHash = GetAssetTypeHash<T>()
            };

            AssetSearchResult result = SearchAssets(
                ref assetTableLookupRO,
                assetLibraryBuffer.AsSpanRO(),
                AsRef(in assetDescription).AsByteSpan(),
                AsRef(in assetName).AsByteSpan(),
                ref searcher,
                options);

            asset = ReinterpretExact<UnityObjectRef<UnityObject>, UnityObjectRef<T>>(ref searcher.ResultAsset);
            return result;
        }

        [HideInCallstack]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static AssetSearchResult FindAssetWithComponentFast<T, S0, S1>(
            ref ComponentLookup<T> componentLookupRO,
            Entity assetLibrary,
            in AssetTable assetTable,
            in S0 assetDescription,
            in S1 assetName,
            AssetSearchOptions options,
            out Entity asset,
            out RefRO<T> component)

            where T : unmanaged, IComponentData
            where S0 : unmanaged, INativeList<byte>, IUTF8Bytes
            where S1 : unmanaged, INativeList<byte>, IUTF8Bytes
        {
            AssetWithComponentSearcherFirstMatch<T> searcher = new()
            {
                ComponentLookup = componentLookupRO
            };

            AssetSearchResult result = SearchAssets(
                assetLibrary,
                in AsRef(in assetTable),
                AsRef(in assetDescription).AsByteSpan(),
                AsRef(in assetName).AsByteSpan(),
                ref searcher,
                options);

            asset = searcher.ResultAsset;
            component = searcher.ResultComponent;
            return result;
        }

        [HideInCallstack]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static AssetSearchResult FindAssetWithComponentFast<T, S0, S1>(
            ref ComponentLookup<AssetLibrary.AssetTableComponent> assetTableLookupRO,
            ref ComponentLookup<T> componentLookupRO,
            DynamicBuffer<AssetLibraryConsumer.AssetLibraryBufferElement> assetLibraryBuffer,
            in S0 assetDescription,
            in S1 assetName,
            AssetSearchOptions options,
            out Entity asset,
            out RefRO<T> component)

            where T : unmanaged, IComponentData
            where S0 : unmanaged, INativeList<byte>, IUTF8Bytes
            where S1 : unmanaged, INativeList<byte>, IUTF8Bytes
        {
            AssetWithComponentSearcherFirstMatch<T> searcher = new()
            {
                ComponentLookup = componentLookupRO
            };

            AssetSearchResult result = SearchAssets(
                ref assetTableLookupRO,
                assetLibraryBuffer.AsSpanRO(),
                AsRef(in assetDescription).AsByteSpan(),
                AsRef(in assetName).AsByteSpan(),
                ref searcher,
                options);

            asset = searcher.ResultAsset;
            component = searcher.ResultComponent;
            return result;
        }

        [HideInCallstack]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static AssetSearchResult FindAssetWithBufferFast<T, S0, S1>(
            ref BufferLookup<T> bufferLookup,
            Entity assetLibrary,
            in AssetTable assetTable,
            in S0 assetDescription,
            in S1 assetName,
            AssetSearchOptions options,
            out Entity asset,
            out DynamicBuffer<T> buffer)

            where T : unmanaged, IBufferElementData
            where S0 : unmanaged, INativeList<byte>, IUTF8Bytes
            where S1 : unmanaged, INativeList<byte>, IUTF8Bytes
        {
            AssetWithBufferSearcherFirstMatch<T> searcher = new()
            {
                BufferLookup = bufferLookup
            };

            AssetSearchResult result = SearchAssets(
                assetLibrary,
                in AsRef(in assetTable),
                AsRef(in assetDescription).AsByteSpan(),
                AsRef(in assetName).AsByteSpan(),
                ref searcher,
                options);

            asset = searcher.ResultAsset;
            buffer = searcher.ResultBuffer;
            return result;
        }

        [HideInCallstack]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static AssetSearchResult FindAssetWithBufferFast<T, S0, S1>(
            ref ComponentLookup<AssetLibrary.AssetTableComponent> assetTableLookupRO,
            ref BufferLookup<T> bufferLookup,
            DynamicBuffer<AssetLibraryConsumer.AssetLibraryBufferElement> assetLibraryBuffer,
            in S0 assetDescription,
            in S1 assetName,
            AssetSearchOptions options,
            out Entity asset,
            out DynamicBuffer<T> buffer)

            where T : unmanaged, IBufferElementData
            where S0 : unmanaged, INativeList<byte>, IUTF8Bytes
            where S1 : unmanaged, INativeList<byte>, IUTF8Bytes
        {
            AssetWithBufferSearcherFirstMatch<T> searcher = new()
            {
                BufferLookup = bufferLookup
            };

            AssetSearchResult result = SearchAssets(
                ref assetTableLookupRO,
                assetLibraryBuffer.AsSpanRO(),
                AsRef(in assetDescription).AsByteSpan(),
                AsRef(in assetName).AsByteSpan(),
                ref searcher,
                options);

            asset = searcher.ResultAsset;
            buffer = searcher.ResultBuffer;
            return result;
        }
    }
}
