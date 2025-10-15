using System.Runtime.CompilerServices;
using static Unity.Collections.LowLevel.Unsafe.UnsafeUtility2;
using EventListenerList = EvilOctane.Collections.LowLevel.Unsafe.InPlaceList<Unity.Entities.Entity>;
using EventListenerListHeader = EvilOctane.Collections.LowLevel.Unsafe.InPlaceListHeader<Unity.Entities.Entity>;
using EventListenerTableHeader = EvilOctane.Collections.LowLevel.Unsafe.InPlaceSwissTableHeader<Unity.Entities.TypeIndex, EvilOctane.Entities.Internal.EventListenerListOffset>;

namespace EvilOctane.Entities.Internal
{
    public unsafe struct EventListenerListOffset
    {
        public nint OffsetFromFirstListHeader;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly EventListenerListHeader* GetList(EventListenerTableHeader* listenerTable, nint firstListOffset)
        {
            byte* list = (byte*)listenerTable + firstListOffset + OffsetFromFirstListHeader;

            CheckIsAligned(list, EventListenerList.BufferAlignment);
            return (EventListenerListHeader*)list;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator EventListenerListOffset(nint other)
        {
            return new EventListenerListOffset() { OffsetFromFirstListHeader = other };
        }
    }
}
