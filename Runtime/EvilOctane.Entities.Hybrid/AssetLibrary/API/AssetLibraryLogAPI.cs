using System.Runtime.CompilerServices;
using Unity.Burst.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using static EvilOctane.Entities.LogUtility;
using static System.Runtime.CompilerServices.Unsafe;

namespace EvilOctane.Entities.Internal
{
    public static class AssetLibraryLogAPI
    {
        public const string BakingTag = "Baking";

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void LogAssetSearchErrors(ByteSpan assetDescription, AssetTableKey key, AssetSearchOptions options, AssetSearchResult result)
        {
            if ((options & AssetSearchOptions.LogErrors) != AssetSearchOptions.LogErrors)
            {
                // No log
                return;
            }

            switch (result)
            {
                case AssetSearchResult.NotFound:
                    LogAssetNotFound(assetDescription, key);
                    break;

                case AssetSearchResult.MultipleExist:
                    if ((options & AssetSearchOptions.UseFirstIfMultipleExist) != AssetSearchOptions.UseFirstIfMultipleExist)
                    {
                        // Multiple assets exist
                        LogMultipleAssetsExist(assetDescription, key);
                    }

                    break;

                case AssetSearchResult.NameIsEmpty:
                    if ((options & AssetSearchOptions.OptionalIfNameIsEmpty) != AssetSearchOptions.OptionalIfNameIsEmpty)
                    {
                        // Asset is required
                        LogEmptyAssetName(assetDescription);
                    }

                    break;
            }
        }

        [SkipLocalsInit]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void LogAssetNotFound(ByteSpan assetDescription, AssetTableKey key)
        {
            SkipInit(out FixedString512Bytes message);
            message.Length = 0;

            _ = message.Append(
                (FixedString32Bytes)"Asset \"",
                assetDescription,
                (FixedString32Bytes)"\" ",
                key.ToFixedString(),
                (FixedString32Bytes)" not found.");

            LogTagged(
                (FixedString32Bytes)nameof(AssetLibrary),
                (FixedString32Bytes)BakingTag,
                in message,
                LogType.Error);
        }

        [SkipLocalsInit]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void LogEmptyAssetName(ByteSpan assetDescription)
        {
            SkipInit(out FixedString512Bytes message);
            message.Length = 0;

            _ = message.Append(
                (FixedString32Bytes)"Asset \"",
                assetDescription,
                (FixedString64Bytes)"\" is marked as required but an empty name was received.");

            LogTagged(
                (FixedString32Bytes)nameof(AssetLibrary),
                (FixedString32Bytes)BakingTag,
                in message,
                LogType.Error);
        }

        [SkipLocalsInit]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void LogMultipleAssetsExist(ByteSpan assetDescription, AssetTableKey key)
        {
            SkipInit(out FixedString512Bytes message);
            message.Length = 0;

            _ = message.Append(
                (FixedString32Bytes)"Multiple assets \"",
                assetDescription,
                (FixedString32Bytes)"\" ",
                key.ToFixedString(),
                (FixedString32Bytes)" exist.");

            LogTagged(
                (FixedString32Bytes)nameof(AssetLibrary),
                (FixedString32Bytes)BakingTag,
                in message,
                LogType.Error);
        }
    }
}
