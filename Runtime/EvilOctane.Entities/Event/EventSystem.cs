using EvilOctane.Entities.Internal;
using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;

namespace EvilOctane.Entities
{
    public static partial class EventSystem
    {
        // Firer

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ComponentTypeSet GetEventFirerComponentTypeSet(bool includeAllocatedTag = true)
        {
            return includeAllocatedTag ?
                ComponentTypeSetUtility.Create<
                // Allocated Tag
                CleanupComponentsAliveTag,

                // Listener Registry
                EventFirer.EventSubscriptionRegistry.Storage,
                EventFirer.EventSubscriptionRegistry.CommandBufferElement,

                // Event Buffer
                EventFirer.EventBuffer.EntityElement,
                EventFirer.EventBuffer.TypeElement>() :

                ComponentTypeSetUtility.Create<
                // Listener Registry
                EventFirer.EventSubscriptionRegistry.Storage,
                EventFirer.EventSubscriptionRegistry.CommandBufferElement,

                // Event Buffer
                EventFirer.EventBuffer.EntityElement,
                EventFirer.EventBuffer.TypeElement>();
        }

        // Listener

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ComponentTypeSet GetEventListenerComponentTypeSet()
        {
            return ComponentTypeSetUtility.Create<
                // Settings
                EventListener.EventDeclarationBuffer.TypeElement,

                // Receive Buffer
                EventListener.EventReceiveBuffer.Element

#if EVIL_OCTANE_ENABLE_PARALLEL_EVENT_ROUTING
                ,
                EventListener.EventReceiveBuffer.LockComponent
#endif
                >();
        }

        public static void AddEventListenerComponents(EntityCommandBuffer commandBuffer, Entity eventListenerEntity)
        {
            commandBuffer.AddComponent(eventListenerEntity, GetEventListenerComponentTypeSet());
        }

        public static void AddEventListenerComponents(EntityCommandBuffer.ParallelWriter commandBuffer, int sortKey, Entity eventListenerEntity)
        {
            commandBuffer.AddComponent(sortKey, eventListenerEntity, GetEventListenerComponentTypeSet());
        }

        // Subscribe

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SubscribeToDeclaredEvents(EntityCommandBuffer commandBuffer, Entity eventFirerEntity, Entity eventListenerEntity)
        {
            commandBuffer.AppendToBuffer(eventFirerEntity, new EventFirer.EventSubscriptionRegistry.CommandBufferElement()
            {
                ListenerEntity = eventListenerEntity,
                EventTypeIndex = TypeIndex.Null,
                Command = EventFirer.EventSubscriptionRegistry.Command.SubscribeAuto
            });
        }

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
            commandBuffer.AppendToBuffer(eventFirerEntity, new EventFirer.EventSubscriptionRegistry.CommandBufferElement()
            {
                ListenerEntity = eventListenerEntity,
                EventTypeIndex = eventComponentType.TypeIndex,
                Command = EventFirer.EventSubscriptionRegistry.Command.SubscribeManual
            });
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SubscribeToEvent(EntityCommandBuffer.ParallelWriter commandBuffer, int sortKey, Entity eventFirerEntity, Entity eventListenerEntity, ComponentType eventComponentType)
        {
            commandBuffer.AppendToBuffer(sortKey, eventFirerEntity, new EventFirer.EventSubscriptionRegistry.CommandBufferElement()
            {
                ListenerEntity = eventListenerEntity,
                EventTypeIndex = eventComponentType.TypeIndex,
                Command = EventFirer.EventSubscriptionRegistry.Command.SubscribeManual
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
            commandBuffer.AppendToBuffer(eventFirerEntity, new EventFirer.EventSubscriptionRegistry.CommandBufferElement()
            {
                ListenerEntity = eventListenerEntity,
                EventTypeIndex = TypeIndex.Null,
                Command = EventFirer.EventSubscriptionRegistry.Command.UnsubscribeManual
            });
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void UnsubscribeFromEvent(EntityCommandBuffer.ParallelWriter commandBuffer, int sortKey, Entity eventFirerEntity, Entity eventListenerEntity, ComponentType eventComponentType)
        {
            commandBuffer.AppendToBuffer(sortKey, eventFirerEntity, new EventFirer.EventSubscriptionRegistry.CommandBufferElement()
            {
                ListenerEntity = eventListenerEntity,
                EventTypeIndex = TypeIndex.Null,
                Command = EventFirer.EventSubscriptionRegistry.Command.UnsubscribeManual
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
            where T : unmanaged, IComponentData, IEventComponent
        {
            // Create Entity
            Entity eventEntity = commandBuffer.CreateEntity();

            // Add Event Data
            commandBuffer.AddComponent(eventEntity, eventComponentData);

            // Register Event
            RegisterEvent(commandBuffer, eventFirerEntity, eventEntity, TypeManager.GetTypeIndex<T>());

            return eventEntity;
        }

        public static Entity FireEvent<T>(EntityCommandBuffer.ParallelWriter commandBuffer, int sortKey, Entity eventFirerEntity, T eventComponentData)
            where T : unmanaged, IComponentData, IEventComponent
        {
            // Create Entity
            Entity eventEntity = commandBuffer.CreateEntity(sortKey);

            // Add Event Data
            commandBuffer.AddComponent(sortKey, eventEntity, eventComponentData);

            // Register Event
            RegisterEvent(commandBuffer, sortKey, eventFirerEntity, eventEntity, TypeManager.GetTypeIndex<T>());

            return eventEntity;
        }

        public static Entity FireEvent<T>(EntityCommandBuffer commandBuffer, Entity eventFirerEntity, out DynamicBuffer<T> eventDataBuffer)
            where T : unmanaged, IBufferElementData, IEventComponent
        {
            // Create Entity
            Entity eventEntity = commandBuffer.CreateEntity();

            // Add Event Data Buffer
            eventDataBuffer = commandBuffer.AddBuffer<T>(eventEntity);

            // Register Event
            RegisterEvent(commandBuffer, eventFirerEntity, eventEntity, TypeManager.GetTypeIndex<T>());

            return eventEntity;
        }

        public static Entity FireEvent<T>(EntityCommandBuffer.ParallelWriter commandBuffer, int sortKey, Entity eventFirerEntity, out DynamicBuffer<T> eventDataBuffer)
            where T : unmanaged, IBufferElementData, IEventComponent
        {
            // Create Entity
            Entity eventEntity = commandBuffer.CreateEntity(sortKey);

            // Add Event Data Buffer
            eventDataBuffer = commandBuffer.AddBuffer<T>(sortKey, eventEntity);

            // Register Event
            RegisterEvent(commandBuffer, sortKey, eventFirerEntity, eventEntity, TypeManager.GetTypeIndex<T>());

            return eventEntity;
        }

        public static Entity FireEvent(EntityCommandBuffer commandBuffer, Entity eventFirerEntity, ComponentType eventComponentType)
        {
            // Create Entity
            Entity eventEntity = commandBuffer.CreateEntity();

            // Add Event Component
            commandBuffer.AddComponent(eventEntity, eventComponentType);

            // Register Event
            RegisterEvent(commandBuffer, eventFirerEntity, eventEntity, eventComponentType.TypeIndex);

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
        private static void RegisterEvent(EntityCommandBuffer commandBuffer, Entity eventFirerEntity, Entity eventEntity, TypeIndex eventTypeIndex)
        {
            commandBuffer.AppendToBuffer(eventFirerEntity, new EventFirer.EventBuffer.EntityElement()
            {
                EventEntity = eventEntity
            });

            commandBuffer.AppendToBuffer(eventFirerEntity, new EventFirer.EventBuffer.TypeElement()
            {
                EventTypeIndex = eventTypeIndex
            });

#if !DOTS_DISABLE_DEBUG_NAMES
            // Set Name
            FixedString64Bytes eventEntityName = CreateEventEntityName(eventTypeIndex);
            commandBuffer.SetName(eventEntity, eventEntityName);
#endif

#if ENABLE_PROFILER
            ++EventSystemProfiler.EventsFiredCounter.Data.Value;
#endif
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void RegisterEvent(EntityCommandBuffer.ParallelWriter commandBuffer, int sortKey, Entity eventFirerEntity, Entity eventEntity, TypeIndex eventTypeIndex)
        {
            commandBuffer.AppendToBuffer(sortKey, eventFirerEntity, new EventFirer.EventBuffer.EntityElement()
            {
                EventEntity = eventEntity
            });

            commandBuffer.AppendToBuffer(sortKey, eventFirerEntity, new EventFirer.EventBuffer.TypeElement()
            {
                EventTypeIndex = eventTypeIndex
            });

#if !DOTS_DISABLE_DEBUG_NAMES
            // Set Name
            FixedString64Bytes eventEntityName = CreateEventEntityName(eventTypeIndex);
            commandBuffer.SetName(sortKey, eventEntity, eventEntityName);
#endif

#if ENABLE_PROFILER
            ++EventSystemProfiler.EventsFiredCounter.Data.Value;
#endif
        }
    }
}
