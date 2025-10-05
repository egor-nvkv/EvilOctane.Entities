using Unity.Entities;

namespace EvilOctane.Entities
{
    public partial struct EventListener
    {
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
