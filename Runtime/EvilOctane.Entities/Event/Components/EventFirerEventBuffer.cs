using System.Runtime.InteropServices;
using Unity.Entities;

namespace EvilOctane.Entities
{
    public partial struct EventFirer
    {
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

        public struct RawEventBuffer
        {
            [InternalBufferCapacity(0)]
            [StructLayout(LayoutKind.Sequential, Size = 1)]
            public struct Storage : ICleanupBufferElementData
            {
                public byte RawByte;
            }
        }
    }
}
