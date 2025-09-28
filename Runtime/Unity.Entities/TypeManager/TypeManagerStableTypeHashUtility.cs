using System.Runtime.CompilerServices;
using Unity.Burst.CompilerServices;

namespace Unity.Entities
{
    public static partial class TypeManager
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryGetTypeIndexFromStableTypeHash(ulong stableTypeHash, out TypeIndex typeIndex, bool logDevError = true)
        {
            typeIndex = GetTypeIndexFromStableTypeHash(stableTypeHash);

            if (Hint.Unlikely(typeIndex == TypeIndex.Null))
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS || UNITY_DOTS_DEBUG || UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.LogError($"Type with stable hash = {stableTypeHash} not found.");
#endif
                return false;
            }

            return true;
        }
    }
}
