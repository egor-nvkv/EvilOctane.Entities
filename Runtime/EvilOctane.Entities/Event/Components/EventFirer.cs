using System.Runtime.CompilerServices;
using Unity.Entities;
using Unity.Mathematics;
using static Unity.Collections.CollectionHelper;
using static Unity.Collections.CollectionHelper2;
using static Unity.Collections.LowLevel.Unsafe.UnsafeUtility;
using EventListenerListHeader = Unity.Collections.LowLevel.Unsafe.InlineListHeader<Unity.Entities.Entity>;

namespace EvilOctane.Entities
{
    public struct EventFirer
    {
        public struct IsAliveTag : ICleanupComponentsAliveTag
        {
        }

        public struct EventDeclarationBuffer
        {
            [InternalBufferCapacity(0)]
            public unsafe struct StableTypeElement : IBufferElementData
            {
                public ulong EventStableTypeHash;
                public int ListenerListInitialCapacity;

                public static int DefaultListenerListCapacity
                {
                    [MethodImpl(MethodImplOptions.AggressiveInlining)]
                    get
                    {
                        int elementOffset = Align(sizeof(EventListenerListHeader), AlignOf<Entity>());
                        int elementCount = (CacheLineSize - elementOffset) / sizeof(Entity);

                        return math.max(elementCount, 1);
                    }
                }

                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                public static StableTypeElement Default<T>()
                {
                    return Create<T>(DefaultListenerListCapacity);
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

        public struct EventSubscriptionRegistry
        {
            [InternalBufferCapacity(0)]
            public struct CommandBufferElement : ICleanupBufferElementData
            {
                public Entity ListenerEntity;
                public TypeIndex EventTypeIndex;
                public Command Command;
            }

            public enum Command : byte
            {
                SubscribeAuto,
                SubscribeManual,
                UnsubscribeAuto,
                UnsubscribeManual
            }
        }

        public struct EventBuffer
        {
            [InternalBufferCapacity(0)]
            public struct EntityElement : IEntityOwnerBufferElementData
            {
                public Entity EventEntity;

                public readonly Entity OwnedEntity => EventEntity;
            }

            [InternalBufferCapacity(0)]
            public struct TypeElement : ICleanupBufferElementData
            {
                public TypeIndex EventTypeIndex;
            }
        }
    }
}
