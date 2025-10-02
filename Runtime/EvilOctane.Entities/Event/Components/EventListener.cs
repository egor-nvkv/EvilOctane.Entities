using System.Runtime.CompilerServices;
using Unity.Entities;

namespace EvilOctane.Entities
{
    public struct EventListener
    {
        public struct EventDeclarationBuffer
        {
            [InternalBufferCapacity(0)]
            public struct TypeElement : IBufferElementData
            {
                public TypeIndex EventTypeIndex;
            }

            [InternalBufferCapacity(0)]
            public struct StableTypeElement : IBufferElementData
            {
                public ulong EventStableTypeHash;

                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                public static StableTypeElement Create<T>()
                {
                    return new StableTypeElement()
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

        public struct EventReceiveBuffer
        {
            [InternalBufferCapacity(0)]
            public struct Element : IBufferElementData
            {
                public Entity EventFirerEntity;
                public Entity EventEntity;
            }
        }
    }
}
