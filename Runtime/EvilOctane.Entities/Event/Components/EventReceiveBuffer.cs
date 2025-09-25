using Unity.Entities;

#if EVIL_OCTANE_ENABLE_PARALLEL_EVENT_ROUTING
using Unity.Collections.LowLevel.Unsafe;
#endif

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

#if EVIL_OCTANE_ENABLE_PARALLEL_EVENT_ROUTING
        public struct LockComponent : IComponentData
        {
            public Spinner Spinner;
        }
#endif
    }
}
