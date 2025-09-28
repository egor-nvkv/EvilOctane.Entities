using System.Runtime.CompilerServices;
using Unity.Collections.LowLevel.Unsafe;

namespace Unity.Entities.LowLevel.Unsafe
{
    public static unsafe partial class EntityCommandBufferExtensions
    {
        [SupportedInEntitiesForEach]
        public static void DestroyEntity(this EntityCommandBuffer self, Entity* entities, int length)
        {
            self.EnforceSingleThreadOwnership();
            self.AssertDidNotPlayback();

            Entity* entitiesCopy = self.m_Data->CloneAndSearchForDeferredEntities(entities, length, out bool containsDeferredEntities);

            _ = self.m_Data->AppendMultipleEntitiesCommand(
                &self.m_Data->m_MainThreadChain,
                self.MainThreadSortKey,
                ECBCommand.DestroyMultipleEntities,
                entitiesCopy,
                length,
                containsDeferredEntities);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [SupportedInEntitiesForEach]
        public static void DestroyEntity(this EntityCommandBuffer self, UnsafeSpan<Entity> entities)
        {
            DestroyEntity(self, entities.Ptr, entities.Length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [SupportedInEntitiesForEach]
        public static void AddComponent<T>(this EntityCommandBuffer self, Entity* entities, int length)
        {
            AddComponent(self, entities, length, ComponentType.ReadWrite<T>());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [SupportedInEntitiesForEach]
        public static void AddComponent<T>(this EntityCommandBuffer self, UnsafeSpan<Entity> entities)
        {
            AddComponent<T>(self, entities.Ptr, entities.Length);
        }

        [SupportedInEntitiesForEach]
        public static void AddComponent(this EntityCommandBuffer self, Entity* entities, int length, ComponentType componentType)
        {
            self.EnforceSingleThreadOwnership();
            self.AssertDidNotPlayback();

            Entity* entitiesCopy = self.m_Data->CloneAndSearchForDeferredEntities(entities, length, out bool containsDeferredEntities);

            _ = self.m_Data->AppendMultipleEntitiesComponentCommand(
                &self.m_Data->m_MainThreadChain,
                self.MainThreadSortKey,
                ECBCommand.AddComponentForMultipleEntities,
                entitiesCopy,
                length,
                containsDeferredEntities,
                componentType);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [SupportedInEntitiesForEach]
        public static void AddComponent(this EntityCommandBuffer self, UnsafeSpan<Entity> entities, ComponentType componentType)
        {
            AddComponent(self, entities.Ptr, entities.Length, componentType);
        }

        [SupportedInEntitiesForEach]
        public static void AddComponent(this EntityCommandBuffer self, Entity* entities, int length, ComponentTypeSet componentTypeSet)
        {
            self.EnforceSingleThreadOwnership();
            self.AssertDidNotPlayback();

            Entity* entitiesCopy = self.m_Data->CloneAndSearchForDeferredEntities(entities, length, out bool containsDeferredEntities);

            _ = self.m_Data->AppendMultipleEntitiesMultipleComponentsCommand(
                &self.m_Data->m_MainThreadChain,
                self.MainThreadSortKey,
                ECBCommand.AddMultipleComponentsForMultipleEntities,
                entitiesCopy,
                length,
                containsDeferredEntities,
                componentTypeSet);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [SupportedInEntitiesForEach]
        public static void AddComponent(this EntityCommandBuffer self, UnsafeSpan<Entity> entities, ComponentTypeSet componentTypeSet)
        {
            AddComponent(self, entities.Ptr, entities.Length, componentTypeSet);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [SupportedInEntitiesForEach]
        public static void RemoveComponent<T>(this EntityCommandBuffer self, Entity* entities, int length)
        {
            RemoveComponent(self, entities, length, ComponentType.ReadWrite<T>());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [SupportedInEntitiesForEach]
        public static void RemoveComponent<T>(this EntityCommandBuffer self, UnsafeSpan<Entity> entities)
        {
            RemoveComponent<T>(self, entities.Ptr, entities.Length);
        }

        [SupportedInEntitiesForEach]
        public static void RemoveComponent(this EntityCommandBuffer self, Entity* entities, int length, ComponentType componentType)
        {
            self.EnforceSingleThreadOwnership();
            self.AssertDidNotPlayback();

            Entity* entitiesCopy = self.m_Data->CloneAndSearchForDeferredEntities(entities, length, out bool containsDeferredEntities);

            _ = self.m_Data->AppendMultipleEntitiesComponentCommand(
                &self.m_Data->m_MainThreadChain,
                self.MainThreadSortKey,
                ECBCommand.RemoveComponentForMultipleEntities,
                entitiesCopy,
                length,
                containsDeferredEntities,
                componentType);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [SupportedInEntitiesForEach]
        public static void RemoveComponent(this EntityCommandBuffer self, UnsafeSpan<Entity> entities, ComponentType componentType)
        {
            RemoveComponent(self, entities.Ptr, entities.Length, componentType);
        }

        [SupportedInEntitiesForEach]
        public static void RemoveComponent(this EntityCommandBuffer self, Entity* entities, int length, ComponentTypeSet componentTypeSet)
        {
            self.EnforceSingleThreadOwnership();
            self.AssertDidNotPlayback();

            Entity* entitiesCopy = self.m_Data->CloneAndSearchForDeferredEntities(entities, length, out bool containsDeferredEntities);

            _ = self.m_Data->AppendMultipleEntitiesMultipleComponentsCommand(
                &self.m_Data->m_MainThreadChain,
                self.MainThreadSortKey,
                ECBCommand.RemoveMultipleComponentsForMultipleEntities,
                entitiesCopy,
                length,
                containsDeferredEntities,
                componentTypeSet);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [SupportedInEntitiesForEach]
        public static void RemoveComponent(this EntityCommandBuffer self, UnsafeSpan<Entity> entities, ComponentTypeSet componentTypeSet)
        {
            RemoveComponent(self, entities.Ptr, entities.Length, componentTypeSet);
        }
    }
}
