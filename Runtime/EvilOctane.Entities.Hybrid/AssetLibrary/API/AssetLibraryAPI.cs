using EvilOctane.Entities.Internal;
using System;
using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Burst.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using UnityEngine;
using static EvilOctane.Entities.LogUtility;
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
        public static bool TryFindAsset<T, S0, S1>(
            ref BufferLookup<AssetLibrary.Storage> assetLibraryStorageLookupRO,
            DynamicBuffer<AssetLibrary.EntityBufferElement> assetLibraryEntityBufferRO,
            S0 assetDescription,
            S1 assetName,
            out UnityObjectRef<T> assetRef,
            bool isOptionalIfNameIsEmpty = false)
            where S0 : unmanaged, INativeList<byte>, IUTF8Bytes
            where S1 : unmanaged, INativeList<byte>, IUTF8Bytes
            where T : UnityObject
        {
            bool result = !TryFindAsset(
                ref assetLibraryStorageLookupRO,
                assetLibraryEntityBufferRO.AsSpanRO(),
                assetDescription: assetDescription.AsByteSpan(),
                assetName: assetName.AsByteSpan(),
                assetTypeHash: GetAssetTypeHash<T>(),
                isOptionalIfNameIsEmpty: isOptionalIfNameIsEmpty,
                out UnityObjectRef<UnityObject> assetRefUntyped);

            assetRef = ReinterpretExact<UnityObjectRef<UnityObject>, UnityObjectRef<T>>(ref assetRefUntyped);
            return result;
        }

        private static bool TryFindAsset(
            ref BufferLookup<AssetLibrary.Storage> assetLibraryStorageLookupRO,
            UnsafeSpan<AssetLibrary.EntityBufferElement> assetLibraryEntitySpanRO,
            ByteSpan assetDescription,
            ByteSpan assetName,
            ulong assetTypeHash,
            bool isOptionalIfNameIsEmpty,
            out UnityObjectRef<UnityObject> assetRef)
        {
            if (assetName.IsEmpty)
            {
                // Empty name

                if (!isOptionalIfNameIsEmpty)
                {
                    // Required
                    LogEmptyAssetName(assetDescription);
                }

                assetRef = new UnityObjectRef<UnityObject>();
                return false;
            }

            AssetLibraryKey key = new(assetTypeHash, assetName);

            foreach (AssetLibrary.EntityBufferElement assetLibraryEntity in assetLibraryEntitySpanRO)
            {
                if (Hint.Unlikely(!assetLibraryStorageLookupRO.TryGetBuffer(assetLibraryEntity.AssetLibraryEntity, out DynamicBuffer<AssetLibrary.Storage> storage)))
                {
                    // No storage
                    continue;
                }

                storage.ReinterpretStorageRO(out AssetLibraryTableHeader* assetLibrary);
                Pointer<UnityObjectRef<UnityObject>> item = AssetLibraryTable.TryGet(assetLibrary, key, out bool exists);

                if (exists)
                {
                    // Found
                    assetRef = item.AsRef;
                    return true;
                }
            }

            // Not found
            LogAssetNotFound(assetDescription, key);

            assetRef = new UnityObjectRef<UnityObject>();
            return false;
        }

        [SkipLocalsInit]
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void LogEmptyAssetName(ByteSpan assetDescription)
        {
            SkipInit(out FixedString4096Bytes message);
            message.Length = 0;

            _ = message.Append(
                (FixedString32Bytes)"Asset \"",
                assetDescription,
                (FixedString64Bytes)"\" is marked as required but an empty name was received.");

            LogTagged(
                (FixedString32Bytes)"AssetLibrary",
                (FixedString32Bytes)"Baking",
                in message,
                LogType.Error);
        }

        [SkipLocalsInit]
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void LogAssetNotFound(ByteSpan assetDescription, AssetLibraryKey key)
        {
            SkipInit(out FixedString4096Bytes message);
            message.Length = 0;

            _ = message.Append(
                (FixedString32Bytes)"Asset \"",
                assetDescription,
                (FixedString32Bytes)"\" ",
                key.ToFixedString(),
                (FixedString32Bytes)" not found.");

            LogTagged(
                (FixedString32Bytes)"AssetLibrary",
                (FixedString32Bytes)"Baking",
                in message,
                LogType.Error);
        }

        [SkipLocalsInit]
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void LogDuplicateAsset(RefRO<BakedEntityNameComponent> assetLibraryName, AssetLibraryKey key)
        {
            SkipInit(out FixedString4096Bytes message);
            message.Length = 0;

            _ = message.Append(
                (FixedString32Bytes)"Multiple assets in library \"",
                in assetLibraryName.ValueRO.EntityName,
                (FixedString32Bytes)"\" have the same key: ",
                key.ToFixedString());

            LogTagged(
                (FixedString32Bytes)"AssetLibrary",
                (FixedString32Bytes)"Baking",
                in message,
                LogType.Error);
        }
    }
}
