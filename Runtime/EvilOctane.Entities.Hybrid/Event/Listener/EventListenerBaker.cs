using System;
using Unity.Entities;

namespace EvilOctane.Entities
{
    public abstract class EventListenerBaker<T> : Baker<T>
        where T : EventListenerAuthoring
    {
        public override void Bake(T authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.None);

            // Tag

            AddComponent<EventListenerTag>(entity);

            // Declared Events

            DynamicBuffer<DeclaredEventTypeBufferElement> typeBuffer = AddBuffer<DeclaredEventTypeBufferElement>(entity);
            typeBuffer.CopyFrom(authoring.DeclaredEventTypes);

            // Subscribe

            Span<EventFirerAuthoring> eventFirers = this.DependsOnMultiple(authoring.eventFirersToSubscribe);

            DynamicBuffer<EventListener.EventSubscribeBuffer.SubscribeAutoElement> subscribeBuffer = AddBuffer<EventListener.EventSubscribeBuffer.SubscribeAutoElement>(entity);
            subscribeBuffer.EnsureCapacityTrashOldData(eventFirers.Length);

            foreach (EventFirerAuthoring eventFirer in eventFirers)
            {
                Entity eventFirerEntity = GetEntity(eventFirer, TransformUsageFlags.None);

                if (eventFirerEntity != Entity.Null)
                {
                    _ = subscribeBuffer.Add(new EventListener.EventSubscribeBuffer.SubscribeAutoElement()
                    {
                        EventFirerEntity = eventFirerEntity
                    });
                }
            }
        }
    }
}
