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
    }
}
