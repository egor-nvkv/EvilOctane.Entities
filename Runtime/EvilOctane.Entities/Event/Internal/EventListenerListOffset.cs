using System.Runtime.CompilerServices;
using static Unity.Collections.LowLevel.Unsafe.UnsafeUtility2;
using EventListenerList = Unity.Collections.LowLevel.Unsafe.InlineList<Unity.Entities.Entity>;
using EventListenerListHeader = Unity.Collections.LowLevel.Unsafe.InlineListHeader<Unity.Entities.Entity>;
using EventListenerMapHeader = Unity.Collections.LowLevel.Unsafe.InlineHashMapHeader<Unity.Entities.TypeIndex>;

namespace EvilOctane.Entities.Internal
{
    public unsafe struct EventListenerListOffset
    {
        public nint OffsetFromFirstListHeader;

        public readonly EventListenerListHeader* GetList(EventListenerMapHeader* listenerMap, nint firstListOffset)
        {
            byte* list = (byte*)listenerMap + firstListOffset + OffsetFromFirstListHeader;

            CheckIsAligned(list, EventListenerList.Alignment);
            return (EventListenerListHeader*)list;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator EventListenerListOffset(nint other)
        {
            return new EventListenerListOffset() { OffsetFromFirstListHeader = other };
        }
    }
}
