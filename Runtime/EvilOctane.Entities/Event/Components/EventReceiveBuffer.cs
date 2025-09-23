using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;

namespace EvilOctane.Entities
{
    public struct EventReceiveBuffer
    {
        [InternalBufferCapacity(0)]
        public struct Element : IBufferElementData
        {
            public Entity EventFirerEntity;
            public Entity EventEntity;
        }

        public struct LockComponent : IComponentData
        {
            public Spinner Spinner;
        }
    }
}
