using Unity.Entities;

namespace EvilOctane.Entities.Internal
{
    public struct EventUnsubscribe
    {
        public TypeIndex EventTypeIndex;
        public Entity ListenerEntity;
    }
}
