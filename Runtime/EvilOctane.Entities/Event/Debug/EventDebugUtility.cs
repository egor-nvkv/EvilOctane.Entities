using System.Diagnostics;
using System.Runtime.CompilerServices;
using Unity.Entities;
using Debug = UnityEngine.Debug;

namespace EvilOctane.Entities.Internal
{
    public static class EventDebugUtility
    {
        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        [Conditional("UNITY_DOTS_DEBUG")]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void LogFiredEventTypeNotRegistered(Entity entity, TypeIndex eventTypeIndex)
        {
            Debug.LogError($"EventSystem | Event \"{TypeManager.GetTypeNameTruncated(eventTypeIndex)}\" that event firer {entity.ToFixedString()} does not declare was fired. It will not get routed.");
        }
    }
}
