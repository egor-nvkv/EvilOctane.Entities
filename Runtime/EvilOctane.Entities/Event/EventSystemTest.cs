using System.Collections;
using Unity.Entities;
using UnityEngine;

namespace EvilOctane.Entities.Test
{
    public class EventSystemTest : MonoBehaviour
    {
        private Entity eventFirerEntity;
        private Entity eventListenerEntity;

        private void CreateEventFirer(EntityManager entityManager)
        {
            eventFirerEntity = entityManager.CreateEntity();
            entityManager.SetName(eventFirerEntity, "Test Event Firer");

            DynamicBuffer<EventSetup.FirerDeclaredEventTypeBufferElement> declaredEvents = entityManager.AddBuffer<EventSetup.FirerDeclaredEventTypeBufferElement>(eventFirerEntity);
            _ = declaredEvents.Add(EventSetup.FirerDeclaredEventTypeBufferElement.Default<TestEvent>());
        }

        private void CreateEventListener(EntityManager entityManager)
        {
            eventListenerEntity = entityManager.CreateEntity();
            entityManager.SetName(eventListenerEntity, "Test Event Listener");

            DynamicBuffer<EventSetup.ListenerDeclaredEventTypeBufferElement> declaredEvents = entityManager.AddBuffer<EventSetup.ListenerDeclaredEventTypeBufferElement>(eventListenerEntity);
            _ = declaredEvents.Add(EventSetup.ListenerDeclaredEventTypeBufferElement.Create<TestEvent>());
        }

        private IEnumerator Start()
        {
            World world = World.DefaultGameObjectInjectionWorld;
            EntityManager entityManager = world.EntityManager;

            // Firer
            CreateEventFirer(entityManager);

            // Listener
            CreateEventListener(entityManager);

            yield return null;

            EntityCommandBuffer commandBuffer = new(world.UpdateAllocator.ToAllocator);

            // Subscribe
            EventUtility.SubscribeToDeclaredEvents(commandBuffer, eventFirerEntity, eventListenerEntity);

            commandBuffer.Playback(world.EntityManager);

            yield return null;

            commandBuffer = new(world.UpdateAllocator.ToAllocator);

            // Fire
            _ = EventUtility.FireEvent(commandBuffer, eventFirerEntity, new TestEvent()
            {
                Value = 123
            });

            commandBuffer.Playback(world.EntityManager);

            yield return null;

            DynamicBuffer<EventReceiveBuffer.Element> eventReceiveBuffer = entityManager.GetBuffer<EventReceiveBuffer.Element>(eventListenerEntity, isReadOnly: true);
            TestEvent testEvent = entityManager.GetComponentData<TestEvent>(eventReceiveBuffer[0].EventEntity);

            Debug.Log($"Received event data: {testEvent.Value}");
        }
    }

    public struct TestEvent : IComponentData, IEventComponent
    {
        public int Value;
    }
}
