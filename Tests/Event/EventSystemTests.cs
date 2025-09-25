using EvilOctane.Entities.Internal;
using NUnit.Framework;
using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;

namespace EvilOctane.Entities.Tests
{
    public unsafe class EventSystemTests
    {
        private static readonly int[] eventFirerCountArray = { 1, 16, 32 };
        private static readonly int[] eventListenerCountArray = { 1, 16, 32 };
        private static readonly int[] eventCountArray = { 1, 16, 64 };

        private static int CreateEventId(int eventCount, int eventFirerIndex, int eventIndex)
        {
            return (eventFirerIndex * eventCount) + eventIndex;
        }

        private static EventDataComponent GetComponentEvent0()
        {
            return new EventDataComponent()
            {
                Data = "test"
            };
        }

        private static EventDataComponent GetComponentEvent1(int eventId)
        {
            EventDataComponent result = new()
            {
                Data = "hoge "
            };

            _ = result.Data.Append(eventId);
            return result;
        }

        private static void SetTwoEventDataBufferElements(Span<EventDataBufferElement> span, int eventId)
        {
            span[0] = new EventDataBufferElement() { Data = 123 };
            span[1] = new EventDataBufferElement() { Data = eventId };
        }

        private static NativeArray<Entity> CreateEntities(EntityManager entityManager, ComponentTypeSet componentTypeSet, int entityCount, Allocator allocator = Allocator.Temp)
        {
            NativeArray<Entity> entityArray = new(entityCount, allocator, NativeArrayOptions.UninitializedMemory);

            NativeArray<ComponentType> componentTypes = componentTypeSet.GetComponentTypes(entityManager.WorldUnmanaged.UpdateAllocator.Handle);
            EntityArchetype entityArchetype = entityManager.CreateArchetype(componentTypes);

            entityManager.CreateEntity(entityArchetype, entityArray);
            return entityArray;
        }

        private static UnsafeList<Entity> GetEventEntities(EntityManager entityManager, Entity eventFirerEntity, Allocator allocator = Allocator.Temp)
        {
            DynamicBuffer<EventBuffer.EntityElement> eventBuffer = entityManager.GetBuffer<EventBuffer.EntityElement>(eventFirerEntity);

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

        private static World CreateWorld()
        {
            World world = new("Test World", WorldFlags.None, Allocator.TempJob);

            InitializationSystemGroup group = world.CreateSystemManaged<InitializationSystemGroup>();
            group.AddSystemToUpdateList(world.CreateSystem<BeginInitializationEntityCommandBufferSystem>());

            group.AddSystemToUpdateList(world.CreateSystem<EventListenerSystem>());
            group.AddSystemToUpdateList(world.CreateSystem<EventFirerSystem>());

            return world;
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
                eventFirerEntities = CreateEntities(entityManager, EventUtility.GetEventFirerComponentTypeSet(), eventFirerCount, allocator);

                // Set Up Firers

                for (int eventFirerIndex = 0; eventFirerIndex < eventFirerCount; ++eventFirerIndex)
                {
                    Entity eventFirerEntity = eventFirerEntities[eventFirerIndex];
                    EventUtility.SetUpEventFirerComponents(commandBuffer, eventFirerEntity);
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
                eventListenerEntities = CreateEntities(entityManager, EventUtility.GetEventListenerComponentTypeSet(), totalEventListenerCount, allocator);
            }

            // Subscribe

            for (int eventFirerIndex = 0; eventFirerIndex < eventFirerCount; ++eventFirerIndex)
            {
                Entity eventFirerEntity = eventFirerEntities[eventFirerIndex];

                for (int listenerIndex = 0; listenerIndex < eventListenerCount; ++listenerIndex)
                {
                    int eventListenerIndex = listenerIndex * 3;

                    Entity eventListenerEntity0 = eventListenerEntities[eventListenerIndex];
                    Entity eventListenerEntity1 = eventListenerEntities[eventListenerIndex + 1];
                    Entity eventListenerEntity2 = eventListenerEntities[eventListenerIndex + 2];

                    // Listener 0
                    {
                        // Component Event only
                        EventUtility.SubscribeToEvent<EventDataComponent>(commandBuffer, eventFirerEntity, eventListenerEntity0);
                    }

                    // Listener 1
                    {
                        // Buffer Event only
                        EventUtility.SubscribeToEvent<EventDataBufferElement>(commandBuffer, eventFirerEntity, eventListenerEntity1);
                    }

                    // Listener 2
                    {
                        // Both Events
                        EventUtility.SubscribeToEvent<EventDataComponent>(commandBuffer, eventFirerEntity, eventListenerEntity2);
                        EventUtility.SubscribeToEvent<EventDataBufferElement>(commandBuffer, eventFirerEntity, eventListenerEntity2);
                    }
                }
            }

            commandBuffer.Playback(entityManager);
        }

        private static void CleanupEventFirers(World world, NativeArray<Entity> eventFirerEntities)
        {
            EntityCommandBuffer commandBuffer = new(world.UpdateAllocator.ToAllocator);
            commandBuffer.RemoveComponent<CleanupComponentAllocatedTag>(eventFirerEntities);
            commandBuffer.Playback(world.EntityManager);

            // Update for cleanup to run
            world.Update();
        }

        private static void FireEvents(EntityCommandBuffer commandBuffer, NativeArray<Entity> eventFirerEntities, int eventCount)
        {
            for (int eventFirerIndex = 0; eventFirerIndex < eventFirerEntities.Length; ++eventFirerIndex)
            {
                Entity eventFirerEntity = eventFirerEntities[eventFirerIndex];

                for (int eventIndex = 0; eventIndex < eventCount; ++eventIndex)
                {
                    int eventId = CreateEventId(eventCount, eventFirerIndex, eventIndex);

                    // Component Event
                    {
                        _ = EventUtility.FireEvent(commandBuffer, eventFirerEntity, GetComponentEvent0());
                        _ = EventUtility.FireEvent(commandBuffer, eventFirerEntity, GetComponentEvent1(eventId));
                    }

                    // Buffer Event
                    {
                        _ = EventUtility.FireEvent(commandBuffer, eventFirerEntity, out DynamicBuffer<EventDataBufferElement> eventDataBuffer);
                        eventDataBuffer.ResizeUninitialized(2);
                        SetTwoEventDataBufferElements(eventDataBuffer.AsSpanRW(), eventId);
                    }
                }
            }
        }

        private static void AssertCorrectEventsGetReceivedInOrder(NativeArray<Entity> eventFirerEntities, NativeArray<UnsafeList<Entity>> eventEntityListPerEventFirer, DynamicBuffer<EventReceiveBuffer.Element> eventReceiveBuffer, int eventIndexOffset, int eventCount)
        {
            Assert.AreEqual(eventCount * eventFirerEntities.Length, eventReceiveBuffer.Length, "Wrong number of events received");

            // Events from a particular Firer are expected to arrive together
            // and in the same order they were fired

            int eventIndex = 0;

            for (; eventIndex < eventReceiveBuffer.Length; ++eventIndex)
            {
                EventReceiveBuffer.Element firstReceivedEvent = eventReceiveBuffer[eventIndex];
                EventReceiveBuffer.Element previousReceivedEvent = firstReceivedEvent;

                // Find Firer
                int eventFirerIndex = eventFirerEntities.IndexOf(firstReceivedEvent.EventFirerEntity);
                Assert.GreaterOrEqual(eventFirerIndex, 0, "Event received from invalid firer");

                UnsafeList<Entity> eventFirerEventList = eventEntityListPerEventFirer[eventFirerIndex];
                UnsafeSpan<Entity> eventFirerEventView = eventFirerEventList.AsSpan().Slice(eventIndexOffset, eventCount);

                for (int eventFirerEventIndex = 0; eventIndex < eventReceiveBuffer.Length && eventFirerEventIndex < eventFirerEventView.Length; ++eventFirerEventIndex)
                {
                    EventReceiveBuffer.Element receivedEvent = eventReceiveBuffer[eventIndex];
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
        public void TestEventEntitiesGetCorrectData(
            [ValueSource(nameof(eventFirerCountArray))] int eventFirerCount,
            [ValueSource(nameof(eventCountArray))] int eventCount)
        {
            // Set up Entities

            using World world = CreateWorld();
            EntityManager entityManager = world.EntityManager;

            CreateEventFirersAndListeners(entityManager, eventFirerCount, 0, Allocator.Temp, out NativeArray<Entity> eventFirerEntities, out NativeArray<Entity> eventListenerEntities);

            // Fire Events

            EntityCommandBuffer commandBuffer = new(world.UpdateAllocator.ToAllocator);
            FireEvents(commandBuffer, eventFirerEntities, eventCount);
            commandBuffer.Playback(entityManager);

            // Get Event Entities
            NativeArray<UnsafeList<Entity>> eventEntityListPerEventFirer = GetAllEventEntities(entityManager, eventFirerEntities);

            // Check Event data

            Span<EventDataBufferElement> actualBufferEventDataSpan = stackalloc EventDataBufferElement[2];

            for (int eventFirerIndex = 0; eventFirerIndex < eventFirerCount; ++eventFirerIndex)
            {
                // To be safe
                actualBufferEventDataSpan.Fill(new EventDataBufferElement());

                UnsafeList<Entity> eventList = eventEntityListPerEventFirer[eventFirerIndex];

                for (int eventIndex = 0; eventIndex < eventCount; ++eventIndex)
                {
                    int eventId = CreateEventId(eventCount, eventFirerIndex, eventIndex);

                    Entity eventEntity0 = eventList[eventIndex * 3];
                    Entity eventEntity1 = eventList[(eventIndex * 3) + 1];
                    Entity eventEntity2 = eventList[(eventIndex * 3) + 2];

                    // Component Event
                    {
                        Assert.AreEqual(GetComponentEvent0(), entityManager.GetComponentData<EventDataComponent>(eventEntity0), "Event has wrong data");
                        Assert.AreEqual(GetComponentEvent1(eventId), entityManager.GetComponentData<EventDataComponent>(eventEntity1), "Event has wrong data");
                    }

                    // Buffer Event
                    {
                        SetTwoEventDataBufferElements(actualBufferEventDataSpan, eventId);
                        DynamicBuffer<EventDataBufferElement> eventDataBuffer = entityManager.GetBuffer<EventDataBufferElement>(eventEntity2);

                        CollectionAssert.AreEqual(actualBufferEventDataSpan.ToArray(), eventDataBuffer.AsSpanRO().ToArray(), "Event has wrong data");
                    }
                }
            }

            // Cleanup
            CleanupEventFirers(world, eventFirerEntities);
        }

        [Test]
        public void TestCorrectEventsGetReceivedInOrder(
            [ValueSource(nameof(eventFirerCountArray))] int eventFirerCount,
            [ValueSource(nameof(eventListenerCountArray))] int eventListenerCount,
            [ValueSource(nameof(eventCountArray))] int eventCount)
        {
            // Set up Entities

            using World world = CreateWorld();
            EntityManager entityManager = world.EntityManager;

            CreateEventFirersAndListeners(entityManager, eventFirerCount, eventListenerCount, Allocator.Temp, out NativeArray<Entity> eventFirerEntities, out NativeArray<Entity> eventListenerEntities);

            // Fire Events

            EntityCommandBuffer commandBuffer = new(world.UpdateAllocator.ToAllocator);
            FireEvents(commandBuffer, eventFirerEntities, eventCount);
            commandBuffer.Playback(entityManager);

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
                    DynamicBuffer<EventReceiveBuffer.Element> eventReceiveBuffer0 = entityManager.GetBuffer<EventReceiveBuffer.Element>(eventListenerEntity0);
                    AssertCorrectEventsGetReceivedInOrder(eventFirerEntities, eventEntityListPerEventFirer, eventReceiveBuffer0, 0, eventCount * 2);
                }

                // Listener 1
                {
                    DynamicBuffer<EventReceiveBuffer.Element> eventReceiveBuffer1 = entityManager.GetBuffer<EventReceiveBuffer.Element>(eventListenerEntity1);
                    AssertCorrectEventsGetReceivedInOrder(eventFirerEntities, eventEntityListPerEventFirer, eventReceiveBuffer1, eventCount * 2, eventCount);
                }

                // Listener 2
                {
                    DynamicBuffer<EventReceiveBuffer.Element> eventReceiveBuffer2 = entityManager.GetBuffer<EventReceiveBuffer.Element>(eventListenerEntity2);
                    AssertCorrectEventsGetReceivedInOrder(eventFirerEntities, eventEntityListPerEventFirer, eventReceiveBuffer2, 0, eventCount * 3);
                }
            }

            // Cleanup
            CleanupEventFirers(world, eventFirerEntities);
        }
    }

    public struct EventDataComponent : IComponentData, IEventComponent, IEquatable<EventDataComponent>
    {
        public FixedString32Bytes Data;

        public readonly bool Equals(EventDataComponent other)
        {
            return Data == other.Data;
        }

        public override readonly string ToString()
        {
            return Data.ToString();
        }
    }

    public struct EventDataBufferElement : IBufferElementData, IEventComponent, IEquatable<EventDataBufferElement>
    {
        public int Data;

        public readonly bool Equals(EventDataBufferElement other)
        {
            return Data == other.Data;
        }

        public override readonly string ToString()
        {
            return Data.ToString();
        }
    }
}
