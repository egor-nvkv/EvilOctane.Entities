using EvilOctane.Entities.Internal;
using System;
using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Burst.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using UnityEngine;
using static System.Runtime.CompilerServices.Unsafe;
using static Unity.Collections.LowLevel.Unsafe.UnsafeUtility2;
using AssetLibraryTable = EvilOctane.Collections.LowLevel.Unsafe.InPlaceSwissTable<EvilOctane.Entities.Internal.AssetLibraryKey, Unity.Entities.UnityObjectRef<UnityEngine.Object>, EvilOctane.Entities.Internal.AssetLibraryKeyHasher>;
using AssetLibraryTableHeader = EvilOctane.Collections.LowLevel.Unsafe.InPlaceSwissTableHeader<EvilOctane.Entities.Internal.AssetLibraryKey, Unity.Entities.UnityObjectRef<UnityEngine.Object>>;
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
        public static bool TryFindAsset<T>(
            ref BufferLookup<AssetLibrary.Storage> assetLibraryStorageLookupRO,
            DynamicBuffer<AssetLibrary.EntityBufferElement> assetLibraryEntityBuffer,
            ByteSpan assetName,
            out UnityObjectRef<T> assetRef,
            bool isOptional = false)
            where T : UnityObject
        {
            bool result = !TryFindAsset(
                ref assetLibraryStorageLookupRO,
                assetLibraryEntityBuffer,
                GetAssetTypeHash<T>(),
                assetName,
                isOptional,
                out UnityObjectRef<UnityObject> assetRefUntyped);

            assetRef = Reinterpret<UnityObjectRef<UnityObject>, UnityObjectRef<T>>(ref assetRefUntyped);
            return result;
        }

        private static bool TryFindAsset(
            ref BufferLookup<AssetLibrary.Storage> assetLibraryStorageLookupRO,
            DynamicBuffer<AssetLibrary.EntityBufferElement> assetLibraryEntityBuffer,
            ulong assetTypeHash,
            ByteSpan assetName,
            bool isOptional,
            out UnityObjectRef<UnityObject> assetRef)
        {
            foreach (AssetLibrary.EntityBufferElement assetLibraryEntity in assetLibraryEntityBuffer)
            {
                if (Hint.Unlikely(!assetLibraryStorageLookupRO.TryGetBuffer(assetLibraryEntity.AssetLibraryEntity, out DynamicBuffer<AssetLibrary.Storage> storage)))
                {
                    // No asset library
                    continue;
                }

                AssetLibraryTableHeader* assetLibrary = (AssetLibraryTableHeader*)storage.GetUnsafeReadOnlyPtr();

                AssetLibraryKey key = new(assetTypeHash, assetName);
                ref UnityObjectRef<UnityObject> item = ref AssetLibraryTable.TryGet(assetLibrary, key, out bool exists);

                if (exists)
                {
                    // Found
                    assetRef = item;
                    return true;
                }
            }

            // Not found

            if (!isOptional)
            {
                LogAssetNotFound(assetName);
            }

            assetRef = new UnityObjectRef<UnityObject>();
            return false;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void LogAssetNotFound(ByteSpan assetName)
        {
            SkipInit(out FixedString512Bytes assetName512);
            _ = FixedStringMethods.CopyFromTruncated(ref assetName512, assetName);

            Debug.LogError($"AssetLibrary | Asset not found: \"{assetName512}\"");
        }
    }
}
