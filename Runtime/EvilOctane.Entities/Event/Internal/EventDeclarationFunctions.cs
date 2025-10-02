using EvilOctane.Entities.Internal;
using Unity.Burst.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;

namespace EvilOctane.Entities
{
    public static class EventDeclarationFunctions
    {
        public static void DeserializeEventTypes(
            UnsafeSpan<EventFirer.EventDeclarationBuffer.StableTypeElement> eventStableTypeSpanRO,
            ref UnsafeHashMap<TypeIndex, int> eventTypeListenerCapacityMap)
        {
            HashMapHelperRef<TypeIndex> mapHelper = eventTypeListenerCapacityMap.GetHelperRef();
            mapHelper.EnsureCapacity(eventStableTypeSpanRO.Length);

            foreach (EventFirer.EventDeclarationBuffer.StableTypeElement eventStableType in eventStableTypeSpanRO)
            {
                bool typeIndexFound = TypeManager.TryGetTypeIndexFromStableTypeHash(eventStableType.EventStableTypeHash, out TypeIndex eventTypeIndex);

                if (Hint.Unlikely(!typeIndexFound))
                {
                    // Type Index not found
                    continue;
                }

                // Register Event Type
                _ = mapHelper.TryAddNoResize(eventTypeIndex, eventStableType.ListenerListInitialCapacity);
            }
        }

        public static void DeserializeEventTypes(
            DynamicBuffer<EventListener.EventDeclarationBuffer.StableTypeElement> eventStableTypeBuffer,
            ref UnsafeList<TypeIndex> eventTypeIndexList)
        {
            eventTypeIndexList.Clear();
            eventTypeIndexList.EnsureCapacity(eventStableTypeBuffer.Length);

            foreach (EventListener.EventDeclarationBuffer.StableTypeElement eventStableType in eventStableTypeBuffer)
            {
                if (TypeManager.TryGetTypeIndexFromStableTypeHash(eventStableType.EventStableTypeHash, out TypeIndex typeIndex))
                {
                    eventTypeIndexList.AddNoResize(typeIndex);
                }
            }
        }
    }
}
