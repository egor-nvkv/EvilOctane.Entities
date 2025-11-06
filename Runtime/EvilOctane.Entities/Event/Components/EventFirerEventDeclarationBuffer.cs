using EvilOctane.Entities.Internal;
using System.Runtime.CompilerServices;
using Unity.Entities;
using static EvilOctane.Entities.EventAPI;
using static Unity.Collections.CollectionHelper2;

namespace EvilOctane.Entities
{
    public partial struct EventFirer
    {
        public struct EventDeclarationBuffer
        {
            [InternalBufferCapacity(0)]
            public unsafe struct StableTypeElement : IBufferElementData
            {
                public ulong EventStableTypeHash;
                public int ListenerListInitialCapacity;

                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                public static StableTypeElement Default<T>()
                {
                    return Create<T>(EventSubscriptionRegistryAPI.ListenerListDefaultInitialCapacity);
                }

                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                public static StableTypeElement Create<T>(int listenerListInitialCapacity)
                {
                    CheckContainerCapacity(listenerListInitialCapacity);

                    return new StableTypeElement()
                    {
                        EventStableTypeHash = TypeManager.GetTypeInfo<T>().StableTypeHash,
                        ListenerListInitialCapacity = listenerListInitialCapacity
                    };
                }

                public override readonly string ToString()
                {
                    return TypeManager.StableTypeHashToDebugTypeName(EventStableTypeHash).ToString();
                }
            }
        }

        public struct RawEventDeclarationBuffer
        {
            [InternalBufferCapacity(0)]
            public struct TypeElement : IBufferElementData
            {
                public ulong EventTypeHash;
                public int ListenerListInitialCapacity;

                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                public static TypeElement Default<T>()
                    where T : unmanaged, IRawEvent
                {
                    return Create<T>(EventSubscriptionRegistryAPI.ListenerListDefaultInitialCapacity);
                }

                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                public static TypeElement Create<T>(int listenerListInitialCapacity)
                    where T : unmanaged, IRawEvent
                {
                    CheckContainerCapacity(listenerListInitialCapacity);

                    return new TypeElement()
                    {
                        EventTypeHash = GetRawEventTypeHashCode<T>(),
                        ListenerListInitialCapacity = listenerListInitialCapacity
                    };
                }
            }
        }
    }
}
