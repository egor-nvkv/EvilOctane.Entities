using Unity.Assertions;
using Unity.Burst.CompilerServices;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;

namespace EvilOctane.Entities.Internal
{
    public static unsafe class EventDeclarationAPI
    {
        public static void DeserializeEventTypes(
            UnsafeSpan<EventFirer.EventDeclarationBuffer.StableTypeElement> eventStableTypeSpanRO,
            ref UnsafeHashMap<TypeIndex, int> eventTypeListenerCapacityMap)
        {
            Assert.IsTrue(eventTypeListenerCapacityMap.IsEmpty);

            HashMapHelperRef<TypeIndex> mapHelper = eventTypeListenerCapacityMap.GetHelperRef();
            mapHelper.EnsureCapacity(eventStableTypeSpanRO.Length, keepOldData: false);

            foreach (EventFirer.EventDeclarationBuffer.StableTypeElement eventStableType in eventStableTypeSpanRO)
            {
                bool typeIndexFound = TypeManager.TryGetTypeIndexFromStableTypeHash(eventStableType.EventStableTypeHash, out TypeIndex typeIndex);

                if (Hint.Unlikely(!typeIndexFound))
                {
                    // Type Index not found
                    continue;
                }

                // Register Event Type
                _ = mapHelper.TryAddNoResize(typeIndex, eventStableType.ListenerListInitialCapacity);
            }
        }

        public static int DeserializeEventTypes(
            UnsafeSpan<EventListener.EventDeclarationBuffer.StableTypeElement> eventStableTypeSpanRO,
            TypeIndex* eventTypeIndexPtr)
        {
            int count = 0;

            foreach (EventListener.EventDeclarationBuffer.StableTypeElement eventStableType in eventStableTypeSpanRO)
            {
                bool typeIndexFound = TypeManager.TryGetTypeIndexFromStableTypeHash(eventStableType.EventStableTypeHash, out TypeIndex typeIndex);

                if (Hint.Unlikely(!typeIndexFound))
                {
                    // Type Index not found
                    continue;
                }

                eventTypeIndexPtr[count] = typeIndex;
                ++count;
            }

            return count;
        }
    }
}
