using Unity.Entities;

namespace EvilOctane.Entities
{
    [BakingType]
    [InternalBufferCapacity(0)]
    internal struct EventListenerDeclaredEventTypeBufferElement : IBufferElementData
    {
        public TypeIndex EventTypeIndex;
    }
}
