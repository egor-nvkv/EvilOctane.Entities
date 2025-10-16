using Unity.Assertions;
using Unity.Burst.CompilerServices;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using EventTypeListenerCapacityTable = EvilOctane.Collections.LowLevel.Unsafe.UnsafeSwissTable<Unity.Entities.TypeIndex, int, EvilOctane.Collections.XXH3PodHasher<Unity.Entities.TypeIndex>>;

namespace EvilOctane.Entities.Internal
{
    public static unsafe class EventDeclarationAPI
    {
        public static void DeserializeEventTypes(
            UnsafeSpan<EventFirer.EventDeclarationBuffer.StableTypeElement> eventStableTypeSpanRO,
            ref EventTypeListenerCapacityTable eventTypeListenerCapacityTable)
        {
            Assert.IsTrue(eventTypeListenerCapacityTable.IsEmpty);
            eventTypeListenerCapacityTable.EnsureCapacity(eventStableTypeSpanRO.Length, keepOldData: false);

            foreach (EventFirer.EventDeclarationBuffer.StableTypeElement eventStableType in eventStableTypeSpanRO)
            {
                bool typeIndexFound = TypeManager.TryGetTypeIndexFromStableTypeHash(eventStableType.EventStableTypeHash, out TypeIndex typeIndex);

                if (Hint.Unlikely(!typeIndexFound))
                {
                    // Type Index not found
                    continue;
                }

                // Register Event type
                eventTypeListenerCapacityTable.GetOrAddNoResize(typeIndex, out _) = eventStableType.ListenerListInitialCapacity;
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
