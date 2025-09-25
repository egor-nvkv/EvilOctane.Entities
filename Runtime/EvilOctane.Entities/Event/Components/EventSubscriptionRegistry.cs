using System;
using System.Runtime.CompilerServices;
using Unity.Assertions;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;

namespace EvilOctane.Entities.Internal
{
    public struct EventSubscriptionRegistry
    {
        public const Allocator Allocator = Unity.Collections.Allocator.Persistent;

        public struct Component : ICleanupComponentData, IDisposable
        {
            public UnsafeHashMap<TypeIndex, ListenerList> EventTypeIndexListenerListMap;

            public readonly bool IsEmpty => EventTypeIndexListenerListMap.IsEmpty;

            public Component(int capacity)
            {
                EventTypeIndexListenerListMap = UnsafeHashMapUtility.CreateHashMap<TypeIndex, ListenerList>(capacity, 2, Allocator);
            }

            public void Dispose()
            {
                if (EventTypeIndexListenerListMap.IsCreated)
                {
                    foreach (KVPair<TypeIndex, ListenerList> kvPair in EventTypeIndexListenerListMap)
                    {
                        kvPair.AssumeIndexIsValid();
                        kvPair.Value.Dispose();
                    }

                    EventTypeIndexListenerListMap.GetHelperRef().DisposeMap(Allocator);
                }
            }

            public unsafe struct ListenerList : IDisposable
            {
                [NativeDisableUnsafePtrRestriction]
                public Entity* Ptr;
                public int Length;
                public int Capacity;

                public ListenerList(int capacity)
                {
                    Ptr = MemoryExposed.AllocateList_Inline<Entity>(capacity, Allocator, out Capacity);
                    Length = 0;
                }

                public void Dispose()
                {
                    MemoryExposed.Unmanaged.Free(Ptr, Allocator);
                    Ptr = null;
                }

                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                public readonly bool IsSubscribed(Entity listenerEntity)
                {
                    return AsSpan().Contains(listenerEntity);
                }

                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                public readonly int FindListenerIndex(Entity listenerEntity)
                {
                    return AsSpan().IndexOf(listenerEntity);
                }

                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                public readonly UnsafeSpan<Entity> AsSpan()
                {
                    return new UnsafeSpan<Entity>(Ptr, Length);
                }

                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                public void AddListener(Entity listenerEntity, bool noResize = false)
                {
                    if (noResize)
                    {
                        Assert.IsTrue(Capacity > Length);
                    }
                    else
                    {
                        UntypedUnsafeListMutable list = new()
                        {
                            Ptr = Ptr,
                            m_length = Length,
                            m_capacity = Capacity,
                            Allocator = Allocator
                        };

                        MemoryExposed.EnsureListSlack<Entity>(ref list, 1);

                        Ptr = (Entity*)list.Ptr;
                        Capacity = list.m_capacity;
                    }

                    Ptr[Length] = listenerEntity;
                    ++Length;
                }

                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                public void RemoveListener(int index)
                {
                    Assert.IsTrue((uint)index < (uint)Length);

                    // Listener order is not important
                    Ptr[index] = Ptr[Length - 1];

                    --Length;
                }
            }
        }

        [InternalBufferCapacity(0)]
        public struct ChangeSubscriptionStatusBufferElement : ICleanupBufferElementData
        {
            public Entity ListenerEntity;
            public TypeIndex EventTypeIndex;
            public EventSubscribeUnsubscribeSelector Selector;
        }
    }
}
