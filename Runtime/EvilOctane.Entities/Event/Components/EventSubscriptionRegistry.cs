using System;
using System.Runtime.CompilerServices;
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
                foreach (KVPair<TypeIndex, ListenerList> kvPair in EventTypeIndexListenerListMap)
                {
                    kvPair.AssumeIndexIsValid();
                    kvPair.Value.Dispose();
                }

                EventTypeIndexListenerListMap.GetHelperRef().DisposeMap(Allocator);
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
                public readonly UnsafeSpan<Entity> AsSpan()
                {
                    return new UnsafeSpan<Entity>(Ptr, Length);
                }

                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                public readonly UnsafeList<Entity> AsUnsafeList()
                {
                    return new UnsafeList<Entity>()
                    {
                        Ptr = Ptr,
                        m_length = Length,
                        m_capacity = Capacity,
                        Allocator = Allocator
                    };
                }

                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                public void OverrideFromUnsafeList(UnsafeList<Entity> list)
                {
                    Ptr = list.Ptr;
                    Length = list.m_length;
                    Capacity = list.m_capacity;
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
