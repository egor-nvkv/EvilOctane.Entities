using System;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Entities.LowLevel.Unsafe;

namespace EvilOctane.Entities
{
    public abstract class EventListenerBaker<T> : Baker<T>
        where T : EventListenerAuthoring
    {
        public override void Bake(T authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.None);

            // Declared Events

            DynamicBuffer<EventListenerDeclaredEventTypeBufferElement> typeBuffer = AddBuffer<EventListenerDeclaredEventTypeBufferElement>(entity);

            ReadOnlySpan<TypeIndex> declaredEventTypes = authoring.DeclaredEventTypes;
            typeBuffer.ResizeUninitializedTrashOldData(declaredEventTypes.Length);

            Span<TypeIndex> typeSpan = typeBuffer.AsSpanRW().Reinterpret<TypeIndex>();
            declaredEventTypes.CopyTo(typeSpan);

            // Subscribe

            Span<EventFirerAuthoring> eventFirers = this.DependsOnMultiple(authoring.eventFirersToSubscribe);

            DynamicBuffer<EventListener.EventSubscribeBuffer.SubscribeAutoElement> subscribeBuffer = AddBuffer<EventListener.EventSubscribeBuffer.SubscribeAutoElement>(entity);
            subscribeBuffer.EnsureCapacityTrashOldData(eventFirers.Length);

            foreach (EventFirerAuthoring eventFirer in eventFirers)
            {
                Entity eventFirerEntity = GetEntity(eventFirer, TransformUsageFlags.None);

                if (eventFirerEntity == Entity.Null)
                {
                    // Null
                    continue;
                }

                bool alreadyAdded = subscribeBuffer.AsSpanRO().Reinterpret<Entity>().Contains(eventFirerEntity);

                if (alreadyAdded)
                {
                    // Duplicate
                    continue;
                }

                _ = subscribeBuffer.AddNoResize(new EventListener.EventSubscribeBuffer.SubscribeAutoElement()
                {
                    EventFirerEntity = eventFirerEntity
                });
            }
        }
    }
}
