using System;
using Unity.Entities;

namespace EvilOctane.Entities
{
    public abstract class EventFirerBaker<T> : Baker<T>
        where T : EventFirerAuthoring
    {
        public override void Bake(T authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.None);

            // Declared Events

            DynamicBuffer<EventFirerDeclaredEventTypeBufferElement> typeBuffer = AddBuffer<EventFirerDeclaredEventTypeBufferElement>(entity);

            ReadOnlySpan<TypeIndex> declaredEventTypes = authoring.DeclaredEventTypes;
            typeBuffer.ResizeUninitializedTrashOldData(declaredEventTypes.Length);

            Span<TypeIndex> typeSpan = typeBuffer.AsSpanRW().Reinterpret<TypeIndex>();
            declaredEventTypes.CopyTo(typeSpan);
        }
    }
}
