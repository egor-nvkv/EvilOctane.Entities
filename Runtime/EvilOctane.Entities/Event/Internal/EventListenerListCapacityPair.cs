using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;

namespace EvilOctane.Entities.Internal
{
    public unsafe struct EventListenerListCapacityPair
    {
        public Entity* ListenerListPtr;
        public int ListenerListLength;
        public int ListenerListCapacity;

        /// <summary>
        /// <seealso cref="EventSetup.FirerDeclaredEventTypeBufferElement.ListenerListStartingCapacity"/>
        /// </summary>
        public int ListenerListStartingCapacity;

        public readonly bool IsCreated => ListenerListPtr != null;
        public readonly int ListenerRequiredCapacity => IsCreated ? ListenerListLength : ListenerListStartingCapacity;

        public EventListenerListCapacityPair(int capacity, AllocatorManager.AllocatorHandle allocator)
        {
            ListenerListPtr = MemoryExposed.AllocateList_NoInline<Entity>(capacity, allocator, out ListenerListCapacity);
            ListenerListLength = 0;

            ListenerListStartingCapacity = 0;
        }

        public readonly UnsafeSpan<Entity> AsSpan()
        {
            return new UnsafeSpan<Entity>(ListenerListPtr, ListenerListLength);
        }

        public void Clear()
        {
            ListenerListLength = 0;
        }

        public void EnsureCapacity(int capacity, AllocatorManager.AllocatorHandle allocator)
        {
            UntypedUnsafeListMutable mutable = new()
            {
                Ptr = ListenerListPtr,
                m_length = ListenerListLength,
                m_capacity = ListenerListCapacity,
                Allocator = allocator
            };

            MemoryExposed.EnsureListCapacity<Entity>(ref mutable, capacity);

            ListenerListPtr = (Entity*)mutable.Ptr;
            ListenerListCapacity = mutable.m_capacity;
        }

        public void EnsureSlack(int slack, AllocatorManager.AllocatorHandle allocator)
        {
            EnsureCapacity(ListenerListLength + slack, allocator);
        }

        public void AddNoResize(Entity listenerEntity)
        {
            CollectionHelper2.CheckAddNoResizeHasEnoughCapacity(ListenerListLength, ListenerListCapacity, 1);
            ListenerListPtr[ListenerListLength++] = listenerEntity;
        }

        public void AddRangeNoResize(UnsafeSpan<Entity> span)
        {
            CollectionHelper2.CheckAddNoResizeHasEnoughCapacity(ListenerListLength, ListenerListCapacity, span.Length);

            int oldLength = ListenerListLength;
            ListenerListLength += span.Length;

            new UnsafeSpan<Entity>(ListenerListPtr + oldLength, span.Length).CopyFrom(span);
        }
    }
}
