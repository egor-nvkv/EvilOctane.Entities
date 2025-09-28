using System.Diagnostics;
using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Entities;
using static System.Runtime.CompilerServices.Unsafe;
using Debug = UnityEngine.Debug;

namespace EvilOctane.Entities.Internal
{
    public static class EventDebugUtility
    {
        public const string LogPrefix = "EventSystem";

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static FixedString128Bytes GetTypeNameTruncated(TypeIndex typeIndex)
        {
            SkipInit(out FixedString128Bytes typeName);
            typeName.Length = 0;

            _ = FixedStringMethods.CopyFromTruncated(ref typeName, TypeManager.GetTypeInfo(typeIndex).DebugTypeName);
            return typeName;
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        [Conditional("UNITY_DOTS_DEBUG")]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void LogFiredEventTypeNotRegistered(Entity entity, TypeIndex eventTypeIndex)
        {
            Debug.LogError($"{(FixedString32Bytes)LogPrefix} | Event \"{GetTypeNameTruncated(eventTypeIndex)}\" that event firer {entity.ToFixedString()} does not declare was fired. It will not get routed.");
        }
    }
}
