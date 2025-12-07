using System;
using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Burst.CompilerServices;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using UnityEngine;
using static EvilOctane.Entities.Internal.AssetLibraryLogAPI;
using UnityObject = UnityEngine.Object;

namespace EvilOctane.Entities
{
    public static class AssetLibraryLowLevelAPI
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

        [HideInCallstack]
        public static AssetSearchResult SearchAssets<T>(
            Entity assetLibrary,
            in AssetTable assetTable,
            ByteSpan assetDescription,
            ByteSpan assetName,
            ref T searcher,
            AssetSearchOptions options)

            where T : struct, IAssetSearcher
        {
            AssetSearchResult result;

            if (assetName.IsEmpty)
            {
                // Empty name
                result = AssetSearchResult.NameIsEmpty;
            }
            else
            {
                Pointer<AssetTableEntry> entry = assetTable.Table.TryGet(assetName, out bool exists);

                if (exists)
                {
                    searcher.VisitAssets(options, assetLibrary, in entry.AsRef, out _);
                }

                result = searcher.Result;
            }

            if (result != AssetSearchResult.Found)
            {
                LogAssetSearchErrors(assetDescription, assetName, options, result);
            }

            return result;
        }

        [HideInCallstack]
        public static AssetSearchResult SearchAssets<T>(
            ref ComponentLookup<AssetLibrary.AssetTableComponent> assetTableLookupRO,
            UnsafeSpan<AssetLibraryConsumer.AssetLibraryBufferElement> assetLibrarySpanRO,
            ByteSpan assetDescription,
            ByteSpan assetName,
            ref T searcher,
            AssetSearchOptions options)

            where T : struct, IAssetSearcher
        {
            AssetSearchResult result;

            if (assetName.IsEmpty)
            {
                // Empty name
                result = AssetSearchResult.NameIsEmpty;
            }
            else
            {
                foreach (AssetLibraryConsumer.AssetLibraryBufferElement assetLibrary in assetLibrarySpanRO)
                {
                    if (Hint.Unlikely(!assetTableLookupRO.TryGetRefRO(assetLibrary.Entity, out RefRO<AssetLibrary.AssetTableComponent> assetTable)))
                    {
                        // Component missing
                        continue;
                    }

                    Pointer<AssetTableEntry> entry = assetTable.ValueRO.Value.Table.TryGet(assetName, out bool exists);

                    if (!exists)
                    {
                        // Not found
                        continue;
                    }

                    searcher.VisitAssets(options, assetLibrary.Entity, in entry.AsRef, out bool finished);

                    if (finished)
                    {
                        // Finished
                        break;
                    }
                }

                result = searcher.Result;
            }

            if (result != AssetSearchResult.Found)
            {
                LogAssetSearchErrors(assetDescription, assetName, options, result);
            }

            return result;
        }
    }
}
