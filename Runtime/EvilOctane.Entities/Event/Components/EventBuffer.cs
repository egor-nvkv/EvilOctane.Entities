using Unity.Entities;

namespace EvilOctane.Entities.Internal
{
    public struct EventBuffer
    {
        [InternalBufferCapacity(0)]
        public struct EntityElement : IEntityOwnerBufferElementData
        {
            public Entity EventEntity;
        }

        [InternalBufferCapacity(0)]
        public struct TypeElement : ICleanupBufferElementData
        {
            public TypeIndex EventTypeIndex;
        }
    }
}
