using Unity.Entities;

namespace EvilOctane.Entities
{
    [BakingType]
    [InternalBufferCapacity(0)]
    internal struct EventFirerDeclaredEventTypeBufferElement : IBufferElementData
    {
        public TypeIndex EventTypeIndex;
    }
}
