using System.Runtime.CompilerServices;
using static Unity.Collections.LowLevel.Unsafe.UnsafeUtility2;
using EventSubscriberList = Unity.Collections.LowLevel.Unsafe.InlineList<Unity.Entities.Entity>;
using EventSubscriberListHeader = Unity.Collections.LowLevel.Unsafe.InlineListHeader<Unity.Entities.Entity>;
using EventSubscriptionMapHeader = Unity.Collections.LowLevel.Unsafe.InlineHashMapHeader<Unity.Entities.TypeIndex>;

namespace EvilOctane.Entities.Internal
{
    public unsafe struct EventSubscriberListOffset
    {
        public nint OffsetFromFirstListHeader;

        public readonly EventSubscriberListHeader* GetList(EventSubscriptionMapHeader* subscriptionMap, nint firstListOffset)
        {
            byte* list = (byte*)subscriptionMap + firstListOffset + OffsetFromFirstListHeader;

            CheckIsAligned(list, EventSubscriberList.Alignment);
            return (EventSubscriberListHeader*)list;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator EventSubscriberListOffset(nint other)
        {
            return new EventSubscriberListOffset() { OffsetFromFirstListHeader = other };
        }
    }
}
