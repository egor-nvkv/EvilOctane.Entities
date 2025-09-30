using System.Runtime.CompilerServices;
using Unity.Burst.CompilerServices;
using Unity.Collections;
using static System.Runtime.CompilerServices.Unsafe;

namespace Unity.Entities
{
    public static partial class TypeManager
    {
        /// <summary>
        /// <inheritdoc cref="GetTypeIndexFromStableTypeHash(ulong)"/>
        /// </summary>
        /// <param name="stableTypeHash"></param>
        /// <param name="typeIndex"></param>
        /// <param name="logDevError"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryGetTypeIndexFromStableTypeHash(ulong stableTypeHash, out TypeIndex typeIndex, bool logDevError = true)
        {
            typeIndex = GetTypeIndexFromStableTypeHash(stableTypeHash);

            if (Hint.Unlikely(typeIndex == TypeIndex.Null))
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS || UNITY_DOTS_DEBUG
                Debug.LogError($"Type with stable hash = {stableTypeHash} not found.");
#endif
                return false;
            }

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static FixedString128Bytes StableTypeHashToDebugTypeName(ulong stableTypeHash)
        {
            SkipInit(out FixedString128Bytes result);

            TypeIndex typeIndex = GetTypeIndexFromStableTypeHash(stableTypeHash);

            if (typeIndex != TypeIndex.Null)
            {
                result.Length = 0;
                _ = FixedStringMethods.CopyFromTruncated(ref result, GetTypeInfo(typeIndex).DebugTypeName);
            }
            else
            {
                result.Length = 7;
                result[0] = (byte)'u';
                result[1] = (byte)'n';
                result[2] = (byte)'k';
                result[3] = (byte)'n';
                result[4] = (byte)'o';
                result[5] = (byte)'w';
                result[6] = (byte)'n';
            }

            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static FixedString128Bytes GetTypeNameTruncated(TypeIndex typeIndex)
        {
            SkipInit(out FixedString128Bytes typeName);
            typeName.Length = 0;

            _ = FixedStringMethods.CopyFromTruncated(ref typeName, GetTypeInfo(typeIndex).DebugTypeName);
            return typeName;
        }
    }
}
