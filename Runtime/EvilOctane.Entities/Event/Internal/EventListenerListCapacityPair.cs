using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;

namespace EvilOctane.Entities.Internal
{
    public unsafe struct EventListenerListCapacityPair
    {
        public Entity* Ptr;
        public int Length;
        public int Capacity;

        public int RequiredCapacity;

        public readonly bool IsCreated
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Ptr != null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EventListenerListCapacityPair(int capacity, int requiredCapacity, AllocatorManager.AllocatorHandle allocator)
        {
            Ptr = MemoryExposed.AllocateList<Entity>(capacity, allocator, out nint capacityNint);
            Capacity = (int)capacityNint;
            Length = 0;
            RequiredCapacity = requiredCapacity;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly UnsafeSpan<Entity> AsSpan()
        {
            return new UnsafeSpan<Entity>(Ptr, Length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clear()
        {
            Length = 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void EnsureCapacity(int capacity, AllocatorManager.AllocatorHandle allocator, bool keepOldData = true)
        {
            UntypedUnsafeListMutable mutable = new()
            {
                Ptr = Ptr,
                m_length = Length,
                m_capacity = Capacity,
                Allocator = allocator
            };

            MemoryExposed.EnsureListCapacity<Entity>(ref mutable, capacity, keepOldData: keepOldData);

            Ptr = (Entity*)mutable.Ptr;
            Capacity = mutable.m_capacity;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void EnsureSlack(int slack, AllocatorManager.AllocatorHandle allocator)
        {
            EnsureCapacity(Length + slack, allocator);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddNoResize(Entity listenerEntity)
        {
            CollectionHelper2.CheckAddNoResizeHasEnoughCapacity(Length, Capacity, 1);
            Ptr[Length++] = listenerEntity;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddRangeNoResize(UnsafeSpan<Entity> span)
        {
            CollectionHelper2.CheckAddNoResizeHasEnoughCapacity(Length, Capacity, span.Length);

            int oldLength = Length;
            Length += span.Length;

            new UnsafeSpan<Entity>(Ptr + oldLength, span.Length).CopyFrom(span);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RemoveAtSwapBack(int index)
        {
            CollectionHelper2.CheckContainerIndexInRange(index, Length);

            Ptr[index] = Ptr[Length - 1];
            --Length;
        }
    }
}
