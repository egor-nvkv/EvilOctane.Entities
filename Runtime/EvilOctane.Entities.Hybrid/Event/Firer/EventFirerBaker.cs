using Unity.Entities;

namespace EvilOctane.Entities
{
    public abstract class EventFirerBaker<T> : Baker<T>
        where T : EventFirerAuthoring
    {
        public override void Bake(T authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.None);

            // Tag

            AddComponent<EventFirerTag>(entity);

            // Declared Events

            DynamicBuffer<DeclaredEventTypeBufferElement> typeBuffer = AddBuffer<DeclaredEventTypeBufferElement>(entity);
            typeBuffer.CopyFrom(authoring.DeclaredEventTypes);
        }
    }
}
