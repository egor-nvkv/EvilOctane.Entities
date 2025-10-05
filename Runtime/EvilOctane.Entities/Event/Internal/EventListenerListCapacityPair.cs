using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;

namespace EvilOctane.Entities.Internal
{
    public unsafe struct EventListenerListCapacityPair
    {
        public Entity* ListenerListPtr;
        public int ListenerListLength;
        public int ListenerListCapacity;

        /// <summary>
        /// <see cref="EventFirer.EventDeclarationBuffer.StableTypeElement.ListenerListInitialCapacity"/>
        /// </summary>
        public int ListenerListStartingCapacity;

        public readonly bool IsCreated
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ListenerListPtr != null;
        }

        public readonly int ListenerListRequiredCapacity
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => math.max(ListenerListLength, ListenerListStartingCapacity);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EventListenerListCapacityPair(int capacity, AllocatorManager.AllocatorHandle allocator)
        {
            ListenerListPtr = MemoryExposed.AllocateList<Entity>(capacity, allocator, out ListenerListCapacity);
            ListenerListLength = 0;

            ListenerListStartingCapacity = 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly UnsafeSpan<Entity> AsSpan()
        {
            return new UnsafeSpan<Entity>(ListenerListPtr, ListenerListLength);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clear()
        {
            ListenerListLength = 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void EnsureCapacity(int capacity, AllocatorManager.AllocatorHandle allocator, bool keepOldData = true)
        {
            UntypedUnsafeListMutable mutable = new()
            {
                Ptr = ListenerListPtr,
                m_length = ListenerListLength,
                m_capacity = ListenerListCapacity,
                Allocator = allocator
            };

            MemoryExposed.EnsureListCapacity<Entity>(ref mutable, capacity, keepOldData: keepOldData);

            ListenerListPtr = (Entity*)mutable.Ptr;
            ListenerListCapacity = mutable.m_capacity;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void EnsureSlack(int slack, AllocatorManager.AllocatorHandle allocator)
        {
            EnsureCapacity(ListenerListLength + slack, allocator);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddNoResize(Entity listenerEntity)
        {
            CollectionHelper2.CheckAddNoResizeHasEnoughCapacity(ListenerListLength, ListenerListCapacity, 1);
            ListenerListPtr[ListenerListLength++] = listenerEntity;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddRangeNoResize(UnsafeSpan<Entity> span)
        {
            CollectionHelper2.CheckAddNoResizeHasEnoughCapacity(ListenerListLength, ListenerListCapacity, span.Length);

            int oldLength = ListenerListLength;
            ListenerListLength += span.Length;

            new UnsafeSpan<Entity>(ListenerListPtr + oldLength, span.Length).CopyFrom(span);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RemoveAtSwapBack(int index)
        {
            CollectionHelper2.CheckContainerIndexInRange(index, ListenerListLength);

            ListenerListPtr[index] = ListenerListPtr[ListenerListLength - 1];
            --ListenerListLength;
        }
    }
}
