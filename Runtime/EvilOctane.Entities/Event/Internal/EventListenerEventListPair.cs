using System;
using Unity.Entities;
using EventSpan = Unity.Collections.LowLevel.Unsafe.UnsafeSpan<EvilOctane.Entities.EventReceiveBuffer.Element>;

namespace EvilOctane.Entities.Internal
{
    public struct EventListenerEventListPair : IComparable<EventListenerEventListPair>
    {
        public Entity ListenerEntity;
        public EventSpan EventSpan;

        public readonly int CompareTo(EventListenerEventListPair other)
        {
            return ListenerEntity.CompareTo(other.ListenerEntity);
        }
    }
}
