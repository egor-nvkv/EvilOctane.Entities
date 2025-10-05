using EvilOctane.Entities.Internal;
using NUnit.Framework;
using System;
using System.Text.RegularExpressions;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using UnityEngine;
using UnityEngine.TestTools;

namespace EvilOctane.Entities.Tests
{
    public unsafe class EventSystemTests
    {
        private static readonly int[] eventFirerCountArray = { 1, 8, 16 };
        private static readonly int[] eventListenerCountArray = { 1, 16, 32 };
        private static readonly int[] eventCountArray = { 1, 16, 32 };

        private static readonly Regex undeclaredTypeRegex = new("EventSystem.*UndeclaredEvent.*");

        private static int CreateEventId(int eventCount, int eventFirerIndex, int eventIndex)
        {
            return (eventFirerIndex * eventCount) + eventIndex;
        }

        private static EventSingle GetSingleEvent()
        {
            return new EventSingle()
            {
                Data = "single event"
            };
        }

        private static EventSingle GetSingleEvent(int eventId)
        {
            EventSingle result = new()
            {
                Data = "single event #"
            };

            _ = result.Data.Append(eventId);
            return result;
        }

        private static void SetTwoBufferEventElements(Span<EventBufferElement> span, int eventId)
        {
            span[0] = new EventBufferElement() { Data = 315 };
            span[1] = new EventBufferElement() { Data = eventId };
        }

        private static World CreateWorld()
        {
            World world = new("Test World", WorldFlags.None, Allocator.Persistent);

            InitializationSystemGroup group = world.CreateSystemManaged<InitializationSystemGroup>();
            group.AddSystemToUpdateList(world.CreateSystem<BeginInitializationEntityCommandBufferSystem>());
            group.AddSystemToUpdateList(world.CreateSystem<EndInitializationEntityCommandBufferSystem>());

            group.AddSystemToUpdateList(world.CreateSystem<EventListenerSystem>());
            group.AddSystemToUpdateList(world.CreateSystem<EventFirerSystem>());

            return world;
        }

        private static NativeArray<Entity> CreateEntities(EntityManager entityManager, ComponentTypeSet componentTypeSet, int entityCount, Allocator allocator = Allocator.Temp)
        {
            NativeArray<Entity> entityArray = new(entityCount, allocator, NativeArrayOptions.UninitializedMemory);

            NativeArray<ComponentType> componentTypes = componentTypeSet.GetComponentTypes(entityManager.WorldUnmanaged.UpdateAllocator.Handle);
            EntityArchetype entityArchetype = entityManager.CreateArchetype(componentTypes);

            entityManager.CreateEntity(entityArchetype, entityArray);
            return entityArray;
        }

        private static NativeArray<Entity> CreateEntities<T0, T1>(EntityManager entityManager, int entityCount, Allocator allocator = Allocator.Temp)
        {
            EntityArchetype entityArchetype = entityManager.CreateArchetype(stackalloc ComponentType[2]
            {
                ComponentType.ReadWrite<T0>(),
                ComponentType.ReadWrite<T1>()
            });

            NativeArray<Entity> entityArray = new(entityCount, allocator, NativeArrayOptions.UninitializedMemory);

            entityManager.CreateEntity(entityArchetype, entityArray);
            return entityArray;
        }

        private static UnsafeList<Entity> GetEventEntities(EntityManager entityManager, Entity eventFirerEntity, Allocator allocator = Allocator.Temp)
        {
            DynamicBuffer<EventFirer.EventBuffer.EntityElement> eventBuffer = entityManager.GetBuffer<EventFirer.EventBuffer.EntityElement>(eventFirerEntity);

            UnsafeList<Entity> entityList = new(eventBuffer.Length, allocator);
            entityList.AddRangeNoResize(eventBuffer.GetUnsafeReadOnlyPtr(), eventBuffer.Length);

            return entityList;
        }

        private static NativeArray<UnsafeList<Entity>> GetAllEventEntities(EntityManager entityManager, NativeArray<Entity> eventFirerEntities, Allocator allocator = Allocator.Temp)
        {
            NativeArray<UnsafeList<Entity>> result = new(eventFirerEntities.Length, allocator, NativeArrayOptions.UninitializedMemory);

            for (int eventFirerIndex = 0; eventFirerIndex < eventFirerEntities.Length; ++eventFirerIndex)
            {
                Entity eventFirerEntity = eventFirerEntities[eventFirerIndex];
                result[eventFirerIndex] = GetEventEntities(entityManager, eventFirerEntity);
            }

            return result;
        }

        private static void CreateEventFirersAndListeners(EntityManager entityManager, int eventFirerCount, int eventListenerCount, Allocator allocator, out NativeArray<Entity> eventFirerEntities, out NativeArray<Entity> eventListenerEntities)
        {
            EntityCommandBuffer commandBuffer = new(entityManager.WorldUnmanaged.UpdateAllocator.ToAllocator);

            // Entities

            if (eventFirerCount == 0)
            {
                // No Firers
                eventFirerEntities = new NativeArray<Entity>();
            }
            else
            {
                // Create Listeners
                eventFirerEntities = CreateEntities<EventFirer.EventDeclarationBuffer.StableTypeElement, ChunkFiller>(entityManager, eventFirerCount, allocator);

                // Set Up Firers

                foreach (Entity eventFirerEntity in eventFirerEntities)
                {
                    DynamicBuffer<EventFirer.EventDeclarationBuffer.StableTypeElement> firerDeclaredEventTypeBuffer = commandBuffer.SetBuffer<EventFirer.EventDeclarationBuffer.StableTypeElement>(eventFirerEntity);

                    _ = firerDeclaredEventTypeBuffer.Add(EventFirer.EventDeclarationBuffer.StableTypeElement.Default<EventSingle>());
                    _ = firerDeclaredEventTypeBuffer.Add(EventFirer.EventDeclarationBuffer.StableTypeElement.Default<EventBufferElement>());
                }
            }

            if (eventListenerCount == 0)
            {
                // No Listeners
                eventListenerEntities = new NativeArray<Entity>();
            }
            else
            {
                int totalEventListenerCount = eventListenerCount * 3;

                // Create Listeners
                eventListenerEntities = CreateEntities<EventListener.EventDeclarationBuffer.StableTypeElement, ChunkFiller>(entityManager, totalEventListenerCount, allocator);

                // Set Up Listeners

                for (int listenerIndex = 0; listenerIndex < eventListenerCount; ++listenerIndex)
                {
                    int eventListenerIndex = listenerIndex * 3;

                    Entity eventListenerEntity0 = eventListenerEntities[eventListenerIndex];
                    Entity eventListenerEntity1 = eventListenerEntities[eventListenerIndex + 1];
                    Entity eventListenerEntity2 = eventListenerEntities[eventListenerIndex + 2];

                    // Listener 0
                    {
                        // Single Event only
                        DynamicBuffer<EventListener.EventDeclarationBuffer.StableTypeElement> listenerDeclaredEventTypeBuffer0 = commandBuffer.SetBuffer<EventListener.EventDeclarationBuffer.StableTypeElement>(eventListenerEntity0);

                        _ = listenerDeclaredEventTypeBuffer0.Add(EventListener.EventDeclarationBuffer.StableTypeElement.Create<EventSingle>());
                    }

                    // Listener 1
                    {
                        // Buffer Event only
                        DynamicBuffer<EventListener.EventDeclarationBuffer.StableTypeElement> listenerDeclaredEventTypeBuffer1 = commandBuffer.SetBuffer<EventListener.EventDeclarationBuffer.StableTypeElement>(eventListenerEntity1);

                        _ = listenerDeclaredEventTypeBuffer1.Add(EventListener.EventDeclarationBuffer.StableTypeElement.Create<EventBufferElement>());
                    }

                    // Listener 2
                    {
                        // Single Event only
                        DynamicBuffer<EventListener.EventDeclarationBuffer.StableTypeElement> listenerDeclaredEventTypeBuffer2 = commandBuffer.SetBuffer<EventListener.EventDeclarationBuffer.StableTypeElement>(eventListenerEntity2);

                        _ = listenerDeclaredEventTypeBuffer2.Add(EventListener.EventDeclarationBuffer.StableTypeElement.Create<EventSingle>());
                    }
                }
            }

            commandBuffer.Playback(entityManager);
        }

        private static void Subscribe(EntityManager entityManager, NativeArray<Entity> eventFirerEntities, NativeArray<Entity> eventListenerEntities, int eventListenerCount)
        {
            EntityCommandBuffer commandBuffer = new(entityManager.WorldUnmanaged.UpdateAllocator.ToAllocator);

            foreach (Entity eventFirerEntity in eventFirerEntities)
            {
                for (int listenerIndex = 0; listenerIndex < eventListenerCount; ++listenerIndex)
                {
                    int eventListenerIndex = listenerIndex * 3;

                    Entity eventListenerEntity0 = eventListenerEntities[eventListenerIndex];
                    Entity eventListenerEntity1 = eventListenerEntities[eventListenerIndex + 1];
                    Entity eventListenerEntity2 = eventListenerEntities[eventListenerIndex + 2];

                    // Listener 0
                    {
                        // Auto
                        EventSystem.SubscribeToDeclaredEvents(commandBuffer, eventFirerEntity, eventListenerEntity0);

                        // Duplicate
                        EventSystem.SubscribeToEvent<EventSingle>(commandBuffer, eventFirerEntity, eventListenerEntity0);
                    }

                    // Listener 1
                    {
                        // Auto
                        EventSystem.SubscribeToDeclaredEvents(commandBuffer, eventFirerEntity, eventListenerEntity1);

                        // Duplicate
                        EventSystem.SubscribeToDeclaredEvents(commandBuffer, eventFirerEntity, eventListenerEntity1);
                    }

                    // Listener 2
                    {
                        // Auto
                        EventSystem.SubscribeToDeclaredEvents(commandBuffer, eventFirerEntity, eventListenerEntity2);

                        // Manual
                        EventSystem.SubscribeToEvent<EventBufferElement>(commandBuffer, eventFirerEntity, eventListenerEntity2);
                    }
                }
            }

            commandBuffer.Playback(entityManager);
        }

        private static void Unsubscribe(EntityManager entityManager, NativeArray<Entity> eventFirerEntities, NativeArray<Entity> eventListenerEntities, int eventListenerCount)
        {
            EntityCommandBuffer commandBuffer = new(entityManager.WorldUnmanaged.UpdateAllocator.ToAllocator);

            foreach (Entity eventFirerEntity in eventFirerEntities)
            {
                for (int listenerIndex = 0; listenerIndex < eventListenerCount; ++listenerIndex)
                {
                    int eventListenerIndex = listenerIndex * 3;

                    Entity eventListenerEntity0 = eventListenerEntities[eventListenerIndex];
                    Entity eventListenerEntity1 = eventListenerEntities[eventListenerIndex + 1];
                    Entity eventListenerEntity2 = eventListenerEntities[eventListenerIndex + 2];

                    // Listener 0
                    {
                        // Auto
                        EventSystem.UnsubscribeFromDeclaredEvents(commandBuffer, eventFirerEntity, eventListenerEntity0);

                        // Duplicate
                        EventSystem.UnsubscribeFromEvent<EventSingle>(commandBuffer, eventFirerEntity, eventListenerEntity0);
                    }

                    // Listener 1
                    {
                        // Auto
                        EventSystem.UnsubscribeFromDeclaredEvents(commandBuffer, eventFirerEntity, eventListenerEntity1);

                        // Duplicate
                        EventSystem.UnsubscribeFromDeclaredEvents(commandBuffer, eventFirerEntity, eventListenerEntity1);
                    }

                    // Listener 2
                    {
                        // Auto
                        EventSystem.UnsubscribeFromDeclaredEvents(commandBuffer, eventFirerEntity, eventListenerEntity2);

                        // Manual
                        EventSystem.UnsubscribeFromEvent<EventBufferElement>(commandBuffer, eventFirerEntity, eventListenerEntity2);
                    }
                }
            }

            commandBuffer.Playback(entityManager);
        }

        private static void CompactRegistry(EntityManager entityManager, NativeArray<Entity> eventFirerEntities)
        {
            EntityCommandBuffer commandBuffer = new(entityManager.World.UpdateAllocator.ToAllocator);

            foreach (Entity eventFirerEntity in eventFirerEntities)
            {
                EventSystem.CompactEventSubscriptionRegistry(commandBuffer, eventFirerEntity);
            }

            commandBuffer.Playback(entityManager);
        }

        private static void DestroyEventFirers(EntityManager entityManager, NativeArray<Entity> eventFirerEntities)
        {
            EntityCommandBuffer commandBuffer = new(entityManager.World.UpdateAllocator.ToAllocator);
            commandBuffer.DestroyEntity(eventFirerEntities);
            commandBuffer.Playback(entityManager);
        }

        private static void FireEvents(EntityManager entityManager, NativeArray<Entity> eventFirerEntities, int eventCount, bool expectErrorLog = true)
        {
            EntityCommandBuffer commandBuffer = new(entityManager.World.UpdateAllocator.ToAllocator);

            for (int eventFirerIndex = 0; eventFirerIndex < eventFirerEntities.Length; ++eventFirerIndex)
            {
                Entity eventFirerEntity = eventFirerEntities[eventFirerIndex];

                for (int eventIndex = 0; eventIndex < eventCount; ++eventIndex)
                {
                    int eventId = CreateEventId(eventCount, eventFirerIndex, eventIndex);

                    // Undeclared Event
                    {
                        _ = EventSystem.FireEvent(commandBuffer, eventFirerEntity, new UndeclaredEvent());

                        if (expectErrorLog)
                        {
                            LogAssert.Expect(LogType.Error, undeclaredTypeRegex);
                        }
                    }

                    // Single Event
                    {
                        _ = EventSystem.FireEvent(commandBuffer, eventFirerEntity, GetSingleEvent());
                        _ = EventSystem.FireEvent(commandBuffer, eventFirerEntity, GetSingleEvent(eventId));
                    }

                    // Buffer Event
                    {
                        _ = EventSystem.FireEvent(commandBuffer, eventFirerEntity, out DynamicBuffer<EventBufferElement> eventDataBuffer);

                        eventDataBuffer.ResizeUninitialized(2);
                        SetTwoBufferEventElements(eventDataBuffer.AsSpanRW(), eventId);
                    }
                }
            }

            commandBuffer.Playback(entityManager);
        }

        private static void AssertCorrectEventsGetReceivedInOrder(NativeArray<Entity> eventFirerEntities, NativeArray<UnsafeList<Entity>> eventEntityListPerEventFirer, DynamicBuffer<EventListener.EventReceiveBuffer.Element> eventReceiveBuffer, int eventIndexOffset, int eventCount)
        {
            Assert.AreEqual(eventCount * eventFirerEntities.Length, eventReceiveBuffer.Length, "Wrong number of events received");

            // Events from a particular Firer are expected to arrive together
            // and in the same order they were fired

            int eventIndex = 0;

            for (; eventIndex < eventReceiveBuffer.Length; ++eventIndex)
            {
                EventListener.EventReceiveBuffer.Element firstReceivedEvent = eventReceiveBuffer[eventIndex];
                EventListener.EventReceiveBuffer.Element previousReceivedEvent = firstReceivedEvent;

                // Find Firer
                int eventFirerIndex = eventFirerEntities.IndexOf(firstReceivedEvent.EventFirerEntity);
                Assert.GreaterOrEqual(eventFirerIndex, 0, "Event received from invalid firer");

                UnsafeList<Entity> eventFirerEventList = eventEntityListPerEventFirer[eventFirerIndex];
                UnsafeSpan<Entity> eventFirerEventView = eventFirerEventList.AsSpan().Slice(eventIndexOffset, eventCount);

                for (int eventFirerEventIndex = 0; eventIndex < eventReceiveBuffer.Length && eventFirerEventIndex < eventFirerEventView.Length; ++eventFirerEventIndex)
                {
                    EventListener.EventReceiveBuffer.Element receivedEvent = eventReceiveBuffer[eventIndex];
                    Assert.GreaterOrEqual(receivedEvent.EventEntity.Index, previousReceivedEvent.EventEntity.Index, "Event received out of order");

                    if (receivedEvent.EventFirerEntity != firstReceivedEvent.EventFirerEntity)
                    {
                        // Finished looking at Events from current Firer
                        break;
                    }

                    Entity eventFirerEventEntity = eventFirerEventView[eventFirerEventIndex];

                    if (receivedEvent.EventEntity != eventFirerEventEntity)
                    {
                        // Finished looking at Events of interest from current Firer
                        break;
                    }

                    previousReceivedEvent = receivedEvent;
                    ++eventIndex;
                }
            }

            Assert.GreaterOrEqual(eventIndex, eventReceiveBuffer.Length, "Unknown events received");
        }

        [Test]
        public void TestEventFirersGetCleanedUp([ValueSource(nameof(eventFirerCountArray))] int eventFirerCount)
        {
            // Set up

            using World world = CreateWorld();
            EntityManager entityManager = world.EntityManager;

            CreateEventFirersAndListeners(entityManager, eventFirerCount, 0, Allocator.Temp, out NativeArray<Entity> eventFirerEntities, out _);
            world.Update();

            // Destroy
            DestroyEventFirers(entityManager, eventFirerEntities);

            // Update for cleanup
            world.Update();

            // Update for ECBS playback
            world.Update();

            foreach (Entity eventFirerEntity in eventFirerEntities)
            {
                Assert.IsFalse(entityManager.Exists(eventFirerEntity), "Firer was not cleaned up");
            }
        }

        [Test]
        public void TestEventEntitiesGetCorrectData(
            [ValueSource(nameof(eventFirerCountArray))] int eventFirerCount,
            [ValueSource(nameof(eventCountArray))] int eventCount)
        {
            // Set up

            using World world = CreateWorld();
            EntityManager entityManager = world.EntityManager;

            CreateEventFirersAndListeners(entityManager, eventFirerCount, 0, Allocator.Temp, out NativeArray<Entity> eventFirerEntities, out _);
            world.Update();

            // Fire Events
            FireEvents(entityManager, eventFirerEntities, eventCount, expectErrorLog: false);

            // Get Event Entities
            NativeArray<UnsafeList<Entity>> eventEntityListPerEventFirer = GetAllEventEntities(entityManager, eventFirerEntities);

            // Check Event data

            Span<EventBufferElement> actualBufferEventDataSpan = stackalloc EventBufferElement[2];

            for (int eventFirerIndex = 0; eventFirerIndex < eventFirerCount; ++eventFirerIndex)
            {
                // To be safe
                actualBufferEventDataSpan.Fill(new EventBufferElement());

                UnsafeList<Entity> eventList = eventEntityListPerEventFirer[eventFirerIndex];

                for (int eventIndex = 0; eventIndex < eventCount; ++eventIndex)
                {
                    int eventId = CreateEventId(eventCount, eventFirerIndex, eventIndex);

                    Entity eventEntity0 = eventList[eventIndex * 4];
                    Entity eventEntity1 = eventList[(eventIndex * 4) + 1];
                    Entity eventEntity2 = eventList[(eventIndex * 4) + 2];
                    Entity eventEntity3 = eventList[(eventIndex * 4) + 3];

                    // Undeclared Event
                    {
                        Assert.IsTrue(entityManager.HasComponent<UndeclaredEvent>(eventEntity0));
                    }

                    // Single Event
                    {
                        Assert.AreEqual(GetSingleEvent(), entityManager.GetComponentData<EventSingle>(eventEntity1), "Event has wrong data");
                        Assert.AreEqual(GetSingleEvent(eventId), entityManager.GetComponentData<EventSingle>(eventEntity2), "Event has wrong data");
                    }

                    // Buffer Event
                    {
                        SetTwoBufferEventElements(actualBufferEventDataSpan, eventId);
                        DynamicBuffer<EventBufferElement> eventDataBuffer = entityManager.GetBuffer<EventBufferElement>(eventEntity3);

                        CollectionAssert.AreEqual(actualBufferEventDataSpan.ToArray(), eventDataBuffer.AsSpanRO().ToArray(), "Event has wrong data");
                    }
                }
            }
        }

        [Test]
        public void TestEventEntitiesGetCleanedUp(
            [ValueSource(nameof(eventFirerCountArray))] int eventFirerCount,
            [ValueSource(nameof(eventCountArray))] int eventCount)
        {
            // Set up

            using World world = CreateWorld();
            EntityManager entityManager = world.EntityManager;

            CreateEventFirersAndListeners(entityManager, eventFirerCount, 0, Allocator.Temp, out NativeArray<Entity> eventFirerEntities, out _);
            world.Update();

            // Fire Events
            FireEvents(entityManager, eventFirerEntities, eventCount);

            // Get Event Entities
            NativeArray<UnsafeList<Entity>> eventEntityListPerEventFirer = GetAllEventEntities(entityManager, eventFirerEntities);

            // Update to cleanup Event Entities
            world.Update();
            world.Update();

            // Checks Event Entities were destroyed

            foreach (UnsafeList<Entity> eventEntityList in eventEntityListPerEventFirer)
            {
                foreach (Entity eventEntity in eventEntityList)
                {
                    Assert.IsFalse(entityManager.Exists(eventEntity), "Event entity was not destroyed");
                }
            }
        }

        [Test]
        public void TestCorrectEventsGetReceivedInOrder(
            [ValueSource(nameof(eventFirerCountArray))] int eventFirerCount,
            [ValueSource(nameof(eventListenerCountArray))] int eventListenerCount,
            [ValueSource(nameof(eventCountArray))] int eventCount)
        {
            // Set up

            using World world = CreateWorld();
            EntityManager entityManager = world.EntityManager;

            CreateEventFirersAndListeners(entityManager, eventFirerCount, eventListenerCount, Allocator.Temp, out NativeArray<Entity> eventFirerEntities, out NativeArray<Entity> eventListenerEntities);
            world.Update();

            // Subscribe
            Subscribe(entityManager, eventFirerEntities, eventListenerEntities, eventListenerCount);

            // Fire Events
            FireEvents(entityManager, eventFirerEntities, eventCount);

            // Get Event Entities
            NativeArray<UnsafeList<Entity>> eventEntityListPerEventFirer = GetAllEventEntities(entityManager, eventFirerEntities);

            // Update to get Events to Receive Buffers
            world.Update();

            // Check Receive Buffers

            for (int listenerIndex = 0; listenerIndex != eventListenerCount; ++listenerIndex)
            {
                int eventListenerIndex = listenerIndex * 3;

                Entity eventListenerEntity0 = eventListenerEntities[eventListenerIndex];
                Entity eventListenerEntity1 = eventListenerEntities[eventListenerIndex + 1];
                Entity eventListenerEntity2 = eventListenerEntities[eventListenerIndex + 2];

                // Listener 0
                {
                    DynamicBuffer<EventListener.EventReceiveBuffer.Element> eventReceiveBuffer0 = entityManager.GetBuffer<EventListener.EventReceiveBuffer.Element>(eventListenerEntity0);
                    AssertCorrectEventsGetReceivedInOrder(eventFirerEntities, eventEntityListPerEventFirer, eventReceiveBuffer0, 0, eventCount * 2);
                }

                // Listener 1
                {
                    DynamicBuffer<EventListener.EventReceiveBuffer.Element> eventReceiveBuffer1 = entityManager.GetBuffer<EventListener.EventReceiveBuffer.Element>(eventListenerEntity1);
                    AssertCorrectEventsGetReceivedInOrder(eventFirerEntities, eventEntityListPerEventFirer, eventReceiveBuffer1, eventCount * 2, eventCount);
                }

                // Listener 2
                {
                    DynamicBuffer<EventListener.EventReceiveBuffer.Element> eventReceiveBuffer2 = entityManager.GetBuffer<EventListener.EventReceiveBuffer.Element>(eventListenerEntity2);
                    AssertCorrectEventsGetReceivedInOrder(eventFirerEntities, eventEntityListPerEventFirer, eventReceiveBuffer2, 0, eventCount * 3);
                }
            }
        }

        [Test]
        public void TestUnsubscribeWorks(
            [ValueSource(nameof(eventFirerCountArray))] int eventFirerCount,
            [ValueSource(nameof(eventListenerCountArray))] int eventListenerCount,
            [ValueSource(nameof(eventCountArray))] int eventCount)
        {
            // Set up

            using World world = CreateWorld();
            EntityManager entityManager = world.EntityManager;

            CreateEventFirersAndListeners(entityManager, eventFirerCount, eventListenerCount, Allocator.Temp, out NativeArray<Entity> eventFirerEntities, out NativeArray<Entity> eventListenerEntities);
            world.Update();

            // Subscribe
            Subscribe(entityManager, eventFirerEntities, eventListenerEntities, eventListenerCount);

            // Unsubscribe
            Unsubscribe(entityManager, eventFirerEntities, eventListenerEntities, eventListenerCount);

            // Fire Events
            FireEvents(entityManager, eventFirerEntities, eventCount);

            // Get Event Entities
            NativeArray<UnsafeList<Entity>> eventEntityListPerEventFirer = GetAllEventEntities(entityManager, eventFirerEntities);

            // Update to get Events to Receive Buffers
            world.Update();

            // Check Receive Buffers

            for (int listenerIndex = 0; listenerIndex != eventListenerCount; ++listenerIndex)
            {
                int eventListenerIndex = listenerIndex * 3;

                Entity eventListenerEntity0 = eventListenerEntities[eventListenerIndex];
                Entity eventListenerEntity1 = eventListenerEntities[eventListenerIndex + 1];
                Entity eventListenerEntity2 = eventListenerEntities[eventListenerIndex + 2];

                // Listener 0
                {
                    DynamicBuffer<EventListener.EventReceiveBuffer.Element> eventReceiveBuffer0 = entityManager.GetBuffer<EventListener.EventReceiveBuffer.Element>(eventListenerEntity0);
                    Assert.IsTrue(eventReceiveBuffer0.IsEmpty);
                }

                // Listener 1
                {
                    DynamicBuffer<EventListener.EventReceiveBuffer.Element> eventReceiveBuffer1 = entityManager.GetBuffer<EventListener.EventReceiveBuffer.Element>(eventListenerEntity1);
                    Assert.IsTrue(eventReceiveBuffer1.IsEmpty);
                }

                // Listener 2
                {
                    DynamicBuffer<EventListener.EventReceiveBuffer.Element> eventReceiveBuffer2 = entityManager.GetBuffer<EventListener.EventReceiveBuffer.Element>(eventListenerEntity2);
                    Assert.IsTrue(eventReceiveBuffer2.IsEmpty);
                }
            }
        }

        [Test]
        public void TestCompactWorks(
            [ValueSource(nameof(eventFirerCountArray))] int eventFirerCount,
            [ValueSource(nameof(eventListenerCountArray))] int eventListenerCount)
        {
            // Set up

            using World world = CreateWorld();
            EntityManager entityManager = world.EntityManager;

            CreateEventFirersAndListeners(entityManager, eventFirerCount, eventListenerCount, Allocator.Temp, out NativeArray<Entity> eventFirerEntities, out NativeArray<Entity> eventListenerEntities);
            world.Update();

            // Subscribe
            Subscribe(entityManager, eventFirerEntities, eventListenerEntities, eventListenerCount);

            // Get registry capacities before compact

            NativeArray<int> registryCapacityArray = new(eventFirerCount, Allocator.Temp, NativeArrayOptions.UninitializedMemory);

            for (int firerIndex = 0; firerIndex != eventFirerCount; ++firerIndex)
            {
                Entity eventFirerEntity = eventFirerEntities[firerIndex];
                DynamicBuffer<EventFirerInternal.EventSubscriptionRegistry.Storage> registryStorage = entityManager.GetBuffer<EventFirerInternal.EventSubscriptionRegistry.Storage>(eventFirerEntity, isReadOnly: true);
                registryCapacityArray[firerIndex] = registryStorage.Capacity;
            }

            // Update for subscribe to complete
            world.Update();

            // Unsubscribe
            Unsubscribe(entityManager, eventFirerEntities, eventListenerEntities, eventListenerCount);

            // Compact
            CompactRegistry(entityManager, eventFirerEntities);

            // Update for Compact to complete
            world.Update();

            for (int firerIndex = 0; firerIndex != eventFirerCount; ++firerIndex)
            {
                Entity eventFirerEntity = eventFirerEntities[firerIndex];
                int registryOriginalCapacity = registryCapacityArray[firerIndex];

                // Registry

                DynamicBuffer<EventFirerInternal.EventSubscriptionRegistry.Storage> registryStorage = entityManager.GetBuffer<EventFirerInternal.EventSubscriptionRegistry.Storage>(eventFirerEntity, isReadOnly: true);
                Assert.IsTrue(EventSubscriptionRegistryFunctions.IsCreated(registryStorage));

                if (registryStorage.Capacity >= registryOriginalCapacity)
                {
                    Assert.Inconclusive("Compact is disabled while DynamicBuffer.TrimExcess crash is being investigated");
                }

                // Copy to temp map

                UnsafeHashMap<TypeIndex, EventListenerListCapacityPair> eventTypeListenerListMap = new(16, Allocator.Temp);
                EventSubscriptionRegistryFunctions.CopyTo(registryStorage, ref eventTypeListenerListMap, Allocator.Temp);

                Assert.IsFalse(eventTypeListenerListMap.IsEmpty, "Registry keys were cleared");

                foreach (KVPair<TypeIndex, EventListenerListCapacityPair> kvPair in eventTypeListenerListMap)
                {
                    EventListenerListCapacityPair listenerList = kvPair.Value;
                    Assert.LessOrEqual(listenerList.RequiredCapacity, EventSubscriptionRegistryFunctions.ListenerListDefaultInitialCapacity, "Listener list not trimmed");
                }
            }
        }
    }

    public struct UndeclaredEvent : IComponentData, IEventComponent
    {
    }

    public struct EventSingle : IComponentData, IEventComponent, IEquatable<EventSingle>
    {
        public FixedString32Bytes Data;

        public readonly bool Equals(EventSingle other)
        {
            return Data == other.Data;
        }

        public override readonly string ToString()
        {
            return Data.ToString();
        }
    }

    public struct EventBufferElement : IBufferElementData, IEventComponent, IEquatable<EventBufferElement>
    {
        public int Data;

        public readonly bool Equals(EventBufferElement other)
        {
            return Data == other.Data;
        }

        public override readonly string ToString()
        {
            return Data.ToString();
        }
    }
}
