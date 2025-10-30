using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Unity.Burst.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using static EvilOctane.Entities.AssetLibraryAPI;
using static System.Runtime.CompilerServices.Unsafe;
using Debug = UnityEngine.Debug;
using UnityObject = UnityEngine.Object;

namespace EvilOctane.Entities.Internal
{
    [StructLayout(LayoutKind.Sequential, Size = Size)]
    public unsafe struct AssetLibraryKey : IEquatable<AssetLibraryKey>
    {
        public const int Size = 256 - sizeof(ulong)/* UnityObjectRef */;

        public const int AssetNameMaxLength = Size - sizeof(ushort)/* Length */ - sizeof(ulong)/* TypeHash */;

        public ulong AssetTypeHash;
        public ushort AssetNameLength;
        public fixed byte AssetNameBytes[AssetNameMaxLength];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public AssetLibraryKey(UnityObject asset)
        {
            string assetName = asset.name;
            CheckAssetName(assetName);

            // Hash
            AssetTypeHash = GetAssetTypeHash(asset.GetType());

            // Name
            fixed (byte* assetNameBytes = AssetNameBytes)
            {
                int assetNameLength = Encoding.UTF8.GetBytes(assetName, new ByteSpan(assetNameBytes, AssetNameMaxLength));
                AssetNameLength = (ushort)assetNameLength;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public AssetLibraryKey(ulong assetTypeHash, ByteSpan assetName)
        {
            CheckAssetName(assetName);

            // Hash
            AssetTypeHash = assetTypeHash;

            // Name
            int assetNameLength = math.min(assetName.Length, AssetNameMaxLength);
            AssetNameLength = (ushort)assetNameLength;

            fixed (byte* assetNameBytes = AssetNameBytes)
            {
                new UnsafeSpan<byte>(assetNameBytes, assetNameLength).CopyFrom(new UnsafeSpan<byte>(assetName.Ptr, assetNameLength));
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        [Conditional("UNITY_DOTS_DEBUG")]
        private static void CheckAssetName(string assetName)
        {
            int lengthUtf8 = Encoding.UTF8.GetByteCount(assetName);

            if (lengthUtf8 > AssetNameMaxLength)
            {
                Debug.LogError($"AssetLibrary | Max asset name length exceeded: {assetName}.");
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        [Conditional("UNITY_DOTS_DEBUG")]
        private static void CheckAssetName(ByteSpan assetName)
        {
            if (Hint.Unlikely(assetName.Length > AssetNameMaxLength))
            {
                SkipInit(out FixedString512Bytes assetName512);
                _ = assetName.CopyFromTruncated(assetName);

                Debug.LogError($"AssetLibrary | Max asset name length exceeded: {assetName512}.");
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool Equals(AssetLibraryKey other)
        {
            if (Hint.Unlikely(AssetTypeHash != other.AssetTypeHash))
            {
                return false;
            }

            fixed (byte* assetNameBytes = AssetNameBytes)
            {
                return new ByteSpan(assetNameBytes, AssetNameLength) == new ByteSpan(other.AssetNameBytes, other.AssetNameLength);
            }
        }

        public override readonly string ToString()
        {
            return ToFixedString().ToString();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly FixedString512Bytes GetAssetName()
        {
            SkipInit(out FixedString512Bytes result);
            result.Length = 0;

            fixed (byte* assetNameBytes = AssetNameBytes)
            {
                ByteSpan assetName = new(assetNameBytes, AssetNameLength);
                _ = result.Append(assetName);
            }

            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly FixedString512Bytes ToFixedString()
        {
            FixedString512Bytes result = "(TypeHash=";
            _ = result.Append(AssetTypeHash);
            _ = result.Append((FixedString32Bytes)", Name=\"");

            fixed (byte* assetNameBytes = AssetNameBytes)
            {
                ByteSpan assetName = new(assetNameBytes, AssetNameLength);
                _ = result.Append(assetName);
            }

            _ = result.Append((FixedString32Bytes)"\")");
            return result;
        }
    }
}
