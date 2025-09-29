using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using static Unity.Collections.CollectionHelper;
using static Unity.Collections.CollectionHelper2;
using static Unity.Collections.LowLevel.Unsafe.UnsafeUtility;
using EventSubscriberListHeader = Unity.Collections.LowLevel.Unsafe.InlineListHeader<Unity.Entities.Entity>;

namespace EvilOctane.Entities
{
    public unsafe partial struct EventSetup
    {
        [InternalBufferCapacity(0)]
        public struct FirerDeclaredEventTypeBufferElement : IBufferElementData
        {
            public ulong EventStableTypeHash;
            public int ListenerListStartingCapacity;

            public static int DefaultCapacity
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get
                {
                    int elementOffset = Align(sizeof(EventSubscriberListHeader), AlignOf<Entity>());
                    int elementCount = (CacheLineSize - elementOffset) / sizeof(Entity);

                    return math.max(elementCount, 1);
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static FirerDeclaredEventTypeBufferElement Default<T>()
            {
                return new FirerDeclaredEventTypeBufferElement()
                {
                    EventStableTypeHash = TypeManager.GetTypeInfo<T>().StableTypeHash,
                    ListenerListStartingCapacity = DefaultCapacity
                };
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static FirerDeclaredEventTypeBufferElement Create<T>(int subscriberCapacity)
            {
                CheckContainerCapacity(subscriberCapacity);

                return new FirerDeclaredEventTypeBufferElement()
                {
                    EventStableTypeHash = TypeManager.GetTypeInfo<T>().StableTypeHash,
                    ListenerListStartingCapacity = subscriberCapacity
                };
            }

            public override readonly string ToString()
            {
                return TypeManager.StableTypeHashToDebugTypeName(EventStableTypeHash).ToString();
            }
        }

        [InternalBufferCapacity(0)]
        public struct ListenerDeclaredEventTypeBufferElement : IBufferElementData
        {
            public ulong EventStableTypeHash;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static ListenerDeclaredEventTypeBufferElement Create<T>()
            {
                return new ListenerDeclaredEventTypeBufferElement()
                {
                    EventStableTypeHash = TypeManager.GetTypeInfo<T>().StableTypeHash
                };
            }

            public override readonly string ToString()
            {
                return TypeManager.StableTypeHashToDebugTypeName(EventStableTypeHash).ToString();
            }
        }
    }

    public partial struct EventSetup
    {
        public static void ToTypeIndexList(DynamicBuffer<FirerDeclaredEventTypeBufferElement> declaredEventTypeBuffer, ref UnsafeList<TypeIndex> typeIndexList)
        {
            typeIndexList.Clear();
            typeIndexList.EnsureCapacity(declaredEventTypeBuffer.Length);

            foreach (FirerDeclaredEventTypeBufferElement declaredEventType in declaredEventTypeBuffer)
            {
                if (TypeManager.TryGetTypeIndexFromStableTypeHash(declaredEventType.EventStableTypeHash, out TypeIndex typeIndex))
                {
                    typeIndexList.AddNoResize(typeIndex);
                }
            }
        }

        public static void ToTypeIndexList(DynamicBuffer<ListenerDeclaredEventTypeBufferElement> declaredEventTypeBuffer, ref UnsafeList<TypeIndex> typeIndexList)
        {
            typeIndexList.Clear();
            typeIndexList.EnsureCapacity(declaredEventTypeBuffer.Length);

            foreach (ListenerDeclaredEventTypeBufferElement declaredEventType in declaredEventTypeBuffer)
            {
                if (TypeManager.TryGetTypeIndexFromStableTypeHash(declaredEventType.EventStableTypeHash, out TypeIndex typeIndex))
                {
                    typeIndexList.AddNoResize(typeIndex);
                }
            }
        }
    }
}
