using System.Diagnostics;
using System.Runtime.CompilerServices;
using Unity.Burst.CompilerServices;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using static EvilOctane.Entities.LogUtility;
using static System.Runtime.CompilerServices.Unsafe;

namespace EvilOctane.Entities.Internal
{
    public static class EventDebugUtility
    {
        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        [Conditional("UNITY_DOTS_DEBUG")]
        [SkipLocalsInit]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void LogFiredEventTypeNotRegistered(Entity entity, TypeIndex eventTypeIndex)
        {
            SkipInit(out FixedString4096Bytes message);
            message.Length = 0;

            _ = message.Append(
                (FixedString32Bytes)"Event \"",
                TypeManager.GetTypeInfo(eventTypeIndex).DebugTypeName,
                (FixedString32Bytes)"\" that event firer ",
                entity.ToFixedString(),
                (FixedString64Bytes)" does not declare was fired. It will not get routed.");

            LogTagged(
                (FixedString32Bytes)"EventSystem",
                in message,
                LogType.Error);
        }
    }
}
