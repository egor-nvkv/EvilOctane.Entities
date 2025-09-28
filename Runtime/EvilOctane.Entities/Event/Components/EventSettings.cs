using Unity.Entities;

namespace EvilOctane.Entities.Internal
{
    public struct EventSettings
    {
        [InternalBufferCapacity(0)]
        public struct ListenerDeclaredEventTypeBufferElement : IBufferElementData
        {
            public TypeIndex EventTypeIndex;
        }
    }
}
