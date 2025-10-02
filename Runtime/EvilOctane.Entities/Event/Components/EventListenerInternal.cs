#if EVIL_OCTANE_ENABLE_PARALLEL_EVENT_ROUTING
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;

namespace EvilOctane.Entities.Internal
{
    public struct EventListenerInternal
    {
        public struct EventReceiveBuffer
        {
            public struct LockComponent : IComponentData
            {
                public Spinner Spinner;
            }
        }
    }
}
#endif
