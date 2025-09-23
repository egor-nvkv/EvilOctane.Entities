using EvilOctane.Entities.Internal;
using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;

namespace EvilOctane.Entities
{
    public static partial class EventUtility
    {
        // Firer

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ComponentTypeSet GetEventFirerComponentTypeSet()
        {
            return ComponentTypeSetUtility.Create<
                // Allocated Tag
                CleanupComponentAllocatedTag,

                // Event Buffer
                EventBuffer.EntityElement,
                EventBuffer.TypeElement,

                // Subscription Registry
                EventSubscriptionRegistry.Component,
                EventSubscriptionRegistry.ChangeSubscriptionStatusBufferElement>();
        }

        public static void SetUpEventFirerComponents(EntityCommandBuffer commandBuffer, Entity eventFirerEntity)
        {
            commandBuffer.AddComponent(eventFirerEntity, GetEventFirerComponentTypeSet());

            // Subscription Registry
            commandBuffer.SetComponent(eventFirerEntity, new EventSubscriptionRegistry.Component(1));
        }

        public static void SetUpEventFirerComponents(EntityCommandBuffer.ParallelWriter commandBuffer, int sortKey, Entity eventFirerEntity)
        {
            commandBuffer.AddComponent(sortKey, eventFirerEntity, GetEventFirerComponentTypeSet());

            // Subscription Registry
            commandBuffer.SetComponent(sortKey, eventFirerEntity, new EventSubscriptionRegistry.Component(1));
        }

        // Listener

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ComponentTypeSet GetEventListenerComponentTypeSet()
        {
            return ComponentTypeSetUtility.Create<
                // Event Receive Buffer
                EventReceiveBuffer.Element,
                EventReceiveBuffer.LockComponent>();
        }

        // Subscribe

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SubscribeToEvent<T>(EntityCommandBuffer commandBuffer, Entity eventFirerEntity, Entity eventListenerEntity)
        {
            SubscribeToEvent(commandBuffer, eventFirerEntity, eventListenerEntity, ComponentType.ReadWrite<T>());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SubscribeToEvent<T>(EntityCommandBuffer.ParallelWriter commandBuffer, int sortKey, Entity eventFirerEntity, Entity eventListenerEntity)
        {
            SubscribeToEvent(commandBuffer, sortKey, eventFirerEntity, eventListenerEntity, ComponentType.ReadWrite<T>());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SubscribeToEvent(EntityCommandBuffer commandBuffer, Entity eventFirerEntity, Entity eventListenerEntity, ComponentType eventComponentType)
        {
            commandBuffer.AppendToBuffer(eventFirerEntity, new EventSubscriptionRegistry.ChangeSubscriptionStatusBufferElement()
            {
                ListenerEntity = eventListenerEntity,
                EventTypeIndex = eventComponentType.TypeIndex,
                Selector = EventSubscribeUnsubscribeSelector.Subscribe
            });
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SubscribeToEvent(EntityCommandBuffer.ParallelWriter commandBuffer, int sortKey, Entity eventFirerEntity, Entity eventListenerEntity, ComponentType eventComponentType)
        {
            commandBuffer.AppendToBuffer(sortKey, eventFirerEntity, new EventSubscriptionRegistry.ChangeSubscriptionStatusBufferElement()
            {
                ListenerEntity = eventListenerEntity,
                EventTypeIndex = eventComponentType.TypeIndex,
                Selector = EventSubscribeUnsubscribeSelector.Subscribe
            });
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SubscribeToEvent(EntityCommandBuffer commandBuffer, Entity eventFirerEntity, Entity eventListenerEntity, ComponentTypeSet eventComponentTypeSet)
        {
            for (int index = 0; index != eventComponentTypeSet.Length; ++index)
            {
                ComponentType eventComponentType = eventComponentTypeSet.GetComponentType(index);
                SubscribeToEvent(commandBuffer, eventFirerEntity, eventListenerEntity, eventComponentType);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SubscribeToEvent(EntityCommandBuffer.ParallelWriter commandBuffer, int sortKey, Entity eventFirerEntity, Entity eventListenerEntity, ComponentTypeSet eventComponentTypeSet)
        {
            for (int index = 0; index != eventComponentTypeSet.Length; ++index)
            {
                ComponentType eventComponentType = eventComponentTypeSet.GetComponentType(index);
                SubscribeToEvent(commandBuffer, sortKey, eventFirerEntity, eventListenerEntity, eventComponentType);
            }
        }

        // Unsubscribe

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void UnsubscribeFromEvent<T>(EntityCommandBuffer commandBuffer, Entity eventFirerEntity, Entity eventListenerEntity)
        {
            UnsubscribeFromEvent(commandBuffer, eventFirerEntity, eventListenerEntity, ComponentType.ReadWrite<T>());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void UnsubscribeFromEvent<T>(EntityCommandBuffer.ParallelWriter commandBuffer, int sortKey, Entity eventFirerEntity, Entity eventListenerEntity)
        {
            UnsubscribeFromEvent(commandBuffer, sortKey, eventFirerEntity, eventListenerEntity, ComponentType.ReadWrite<T>());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void UnsubscribeFromEvent(EntityCommandBuffer commandBuffer, Entity eventFirerEntity, Entity eventListenerEntity, ComponentType eventComponentType)
        {
            commandBuffer.AppendToBuffer(eventFirerEntity, new EventSubscriptionRegistry.ChangeSubscriptionStatusBufferElement()
            {
                ListenerEntity = eventListenerEntity,
                EventTypeIndex = eventComponentType.TypeIndex,
                Selector = EventSubscribeUnsubscribeSelector.Unsubscribe
            });
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void UnsubscribeFromEvent(EntityCommandBuffer.ParallelWriter commandBuffer, int sortKey, Entity eventFirerEntity, Entity eventListenerEntity, ComponentType eventComponentType)
        {
            commandBuffer.AppendToBuffer(sortKey, eventFirerEntity, new EventSubscriptionRegistry.ChangeSubscriptionStatusBufferElement()
            {
                ListenerEntity = eventListenerEntity,
                EventTypeIndex = eventComponentType.TypeIndex,
                Selector = EventSubscribeUnsubscribeSelector.Unsubscribe
            });
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void UnsubscribeFromEvent(EntityCommandBuffer commandBuffer, Entity eventFirerEntity, Entity eventListenerEntity, ComponentTypeSet eventComponentTypeSet)
        {
            for (int index = 0; index != eventComponentTypeSet.Length; ++index)
            {
                ComponentType eventComponentType = eventComponentTypeSet.GetComponentType(index);
                UnsubscribeFromEvent(commandBuffer, eventFirerEntity, eventListenerEntity, eventComponentType);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void UnsubscribeFromEvent(EntityCommandBuffer.ParallelWriter commandBuffer, int sortKey, Entity eventFirerEntity, Entity eventListenerEntity, ComponentTypeSet eventComponentTypeSet)
        {
            for (int index = 0; index != eventComponentTypeSet.Length; ++index)
            {
                ComponentType eventComponentType = eventComponentTypeSet.GetComponentType(index);
                UnsubscribeFromEvent(commandBuffer, sortKey, eventFirerEntity, eventListenerEntity, eventComponentType);
            }
        }

        // Fire

        public static Entity FireEvent<T>(EntityCommandBuffer commandBuffer, Entity eventFirerEntity, T eventComponentData)
            where T : unmanaged, IComponentData
        {
            // Create Entity
            Entity eventEntity = commandBuffer.CreateEntity();

            // Add Event Data
            commandBuffer.AddComponent(eventEntity, eventComponentData);

            // Append to Event Buffer
            TypeIndex eventTypeIndex = RegisterEvent<T>(commandBuffer, eventFirerEntity, eventEntity);

#if !DOTS_DISABLE_DEBUG_NAMES
            // Set Name
            FixedString64Bytes eventEntityName = CreateEventEntityName(eventTypeIndex);
            commandBuffer.SetName(eventEntity, eventEntityName);
#endif

            return eventEntity;
        }

        public static Entity FireEvent<T>(EntityCommandBuffer.ParallelWriter commandBuffer, int sortKey, Entity eventFirerEntity, T eventComponentData)
            where T : unmanaged, IComponentData
        {
            // Create Entity
            Entity eventEntity = commandBuffer.CreateEntity(sortKey);

            // Add Event Data
            commandBuffer.AddComponent(sortKey, eventEntity, eventComponentData);

            // Append to Event Buffer
            TypeIndex eventTypeIndex = RegisterEvent<T>(commandBuffer, sortKey, eventFirerEntity, eventEntity);

#if !DOTS_DISABLE_DEBUG_NAMES
            // Set Name
            FixedString64Bytes eventEntityName = CreateEventEntityName(eventTypeIndex);
            commandBuffer.SetName(sortKey, eventEntity, eventEntityName);
#endif

            return eventEntity;
        }

        public static Entity FireEvent<T>(EntityCommandBuffer commandBuffer, Entity eventFirerEntity, out DynamicBuffer<T> eventDataBuffer)
            where T : unmanaged, IBufferElementData
        {
            // Create Entity
            Entity eventEntity = commandBuffer.CreateEntity();

            // Add Event Data Buffer
            eventDataBuffer = commandBuffer.AddBuffer<T>(eventEntity);

            // Append to Event Buffer
            TypeIndex eventTypeIndex = RegisterEvent<T>(commandBuffer, eventFirerEntity, eventEntity);

#if !DOTS_DISABLE_DEBUG_NAMES
            // Set Name
            FixedString64Bytes eventEntityName = CreateEventEntityName(eventTypeIndex);
            commandBuffer.SetName(eventEntity, eventEntityName);
#endif

            return eventEntity;
        }

        public static Entity FireEvent<T>(EntityCommandBuffer.ParallelWriter commandBuffer, int sortKey, Entity eventFirerEntity, out DynamicBuffer<T> eventDataBuffer)
            where T : unmanaged, IBufferElementData
        {
            // Create Entity
            Entity eventEntity = commandBuffer.CreateEntity(sortKey);

            // Add Event Data Buffer
            eventDataBuffer = commandBuffer.AddBuffer<T>(sortKey, eventEntity);

            // Append to Event Buffer
            TypeIndex eventTypeIndex = RegisterEvent<T>(commandBuffer, sortKey, eventFirerEntity, eventEntity);

#if !DOTS_DISABLE_DEBUG_NAMES
            // Set Name
            FixedString64Bytes eventEntityName = CreateEventEntityName(eventTypeIndex);
            commandBuffer.SetName(sortKey, eventEntity, eventEntityName);
#endif

            return eventEntity;
        }

        // Other

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static FixedString64Bytes CreateEventEntityName(TypeIndex eventTypeIndex)
        {
            FixedString64Bytes entityName = "Event | ";
            int typeNameMaxLength = entityName.Capacity - entityName.Length;

            NativeText.ReadOnly typeName = TypeManager.GetTypeInfo(eventTypeIndex).DebugTypeName;
            ByteSpan typeNameTruncated = typeName.AsByteSpan()[..typeNameMaxLength];

            _ = entityName.Append(typeNameTruncated);
            return entityName;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static TypeIndex RegisterEvent<T>(EntityCommandBuffer commandBuffer, Entity eventFirerEntity, Entity eventEntity)
            where T : unmanaged
        {
            commandBuffer.AppendToBuffer(eventFirerEntity, new EventBuffer.EntityElement()
            {
                EventEntity = eventEntity
            });

            TypeIndex eventTypeIndex = TypeManager.GetTypeIndex<T>();

            commandBuffer.AppendToBuffer(eventFirerEntity, new EventBuffer.TypeElement()
            {
                EventTypeIndex = eventTypeIndex
            });

            return eventTypeIndex;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static TypeIndex RegisterEvent<T>(EntityCommandBuffer.ParallelWriter commandBuffer, int sortKey, Entity eventFirerEntity, Entity eventEntity)
            where T : unmanaged
        {
            commandBuffer.AppendToBuffer(sortKey, eventFirerEntity, new EventBuffer.EntityElement()
            {
                EventEntity = eventEntity
            });

            TypeIndex eventTypeIndex = TypeManager.GetTypeIndex<T>();

            commandBuffer.AppendToBuffer(sortKey, eventFirerEntity, new EventBuffer.TypeElement()
            {
                EventTypeIndex = eventTypeIndex
            });

            return eventTypeIndex;
        }
    }
}
