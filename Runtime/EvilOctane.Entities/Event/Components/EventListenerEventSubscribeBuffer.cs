using Unity.Entities;

namespace EvilOctane.Entities
{
    public partial struct EventListener
    {
        public struct EventSubscribeBuffer
        {
            [InternalBufferCapacity(0)]
            public struct SubscribeAutoElement : IBufferElementData
            {
                public Entity EventFirerEntity;
            }
        }
    }
}
