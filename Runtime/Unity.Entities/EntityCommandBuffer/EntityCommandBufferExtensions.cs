using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Unity.Entities.LowLevel.Unsafe
{
    public static unsafe partial class EntityCommandBufferExtensions
    {
        /// <summary>
        /// <inheritdoc cref="EntityCommandBuffer.DestroyEntity(NativeArray{Entity})"/>
        /// </summary>
        /// <param name="self"></param>
        /// <param name="entities"></param>
        /// <param name="length"></param>
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

        /// <summary>
        /// <inheritdoc cref="EntityCommandBuffer.DestroyEntity(NativeArray{Entity})"/>
        /// </summary>
        /// <param name="self"></param>
        /// <param name="entities"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void DestroyEntity(this EntityCommandBuffer self, UnsafeSpan<Entity> entities)
        {
            DestroyEntity(self, entities.Ptr, entities.Length);
        }

        /// <summary>
        /// <inheritdoc cref="EntityCommandBuffer.AddComponent{T}(NativeArray{Entity}, T)"/>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="self"></param>
        /// <param name="entities"></param>
        /// <param name="length"></param>
        /// <param name="component"></param>
        public static void AddComponent<T>(this EntityCommandBuffer self, Entity* entities, int length, T component)
            where T : unmanaged, IComponentData
        {
            self.EnforceSingleThreadOwnership();
            self.AssertDidNotPlayback();

            Entity* entitiesCopy = self.m_Data->CloneAndSearchForDeferredEntities(entities, length, out bool containsDeferredEntities);

            _ = self.m_Data->AppendMultipleEntitiesComponentCommandWithValue(
                &self.m_Data->m_MainThreadChain,
                self.MainThreadSortKey,
                ECBCommand.AddComponentForMultipleEntities,
                entitiesCopy,
                length,
                containsDeferredEntities,
                component);
        }

        /// <summary>
        /// <inheritdoc cref="EntityCommandBuffer.AddComponent{T}(NativeArray{Entity}, T)"/>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="self"></param>
        /// <param name="entities"></param>
        /// <param name="component"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AddComponent<T>(this EntityCommandBuffer self, UnsafeSpan<Entity> entities, T component)
            where T : unmanaged, IComponentData
        {
            AddComponent(self, entities.Ptr, entities.Length, component);
        }

        /// <summary>
        /// <inheritdoc cref="EntityCommandBuffer.AddComponent{T}(NativeArray{Entity})"/>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="self"></param>
        /// <param name="entities"></param>
        /// <param name="length"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AddComponent<T>(this EntityCommandBuffer self, Entity* entities, int length)
        {
            AddComponent(self, entities, length, ComponentType.ReadWrite<T>());
        }

        /// <summary>
        /// <inheritdoc cref="EntityCommandBuffer.AddComponent{T}(NativeArray{Entity})"/>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="self"></param>
        /// <param name="entities"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AddComponent<T>(this EntityCommandBuffer self, UnsafeSpan<Entity> entities)
        {
            AddComponent<T>(self, entities.Ptr, entities.Length);
        }

        /// <summary>
        /// <inheritdoc cref="EntityCommandBuffer.AddComponent(NativeArray{Entity}, ComponentType)"/>
        /// </summary>
        /// <param name="self"></param>
        /// <param name="entities"></param>
        /// <param name="length"></param>
        /// <param name="componentType"></param>
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

        /// <summary>
        /// <inheritdoc cref="EntityCommandBuffer.AddComponent(NativeArray{Entity}, ComponentType)"/>
        /// </summary>
        /// <param name="self"></param>
        /// <param name="entities"></param>
        /// <param name="componentType"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AddComponent(this EntityCommandBuffer self, UnsafeSpan<Entity> entities, ComponentType componentType)
        {
            AddComponent(self, entities.Ptr, entities.Length, componentType);
        }

        /// <summary>
        /// <inheritdoc cref="EntityCommandBuffer.AddComponent(NativeArray{Entity}, in ComponentTypeSet)"/>
        /// </summary>
        /// <param name="self"></param>
        /// <param name="entities"></param>
        /// <param name="length"></param>
        /// <param name="componentTypeSet"></param>
        public static void AddComponent(this EntityCommandBuffer self, Entity* entities, int length, in ComponentTypeSet componentTypeSet)
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
                in componentTypeSet);
        }

        /// <summary>
        /// <inheritdoc cref="EntityCommandBuffer.AddComponent(NativeArray{Entity}, in ComponentTypeSet)"/>
        /// </summary>
        /// <param name="self"></param>
        /// <param name="entities"></param>
        /// <param name="componentTypeSet"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AddComponent(this EntityCommandBuffer self, UnsafeSpan<Entity> entities, in ComponentTypeSet componentTypeSet)
        {
            AddComponent(self, entities.Ptr, entities.Length, in componentTypeSet);
        }

        /// <summary>
        /// <inheritdoc cref="EntityCommandBuffer.RemoveComponent{T}(NativeArray{Entity})"/>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="self"></param>
        /// <param name="entities"></param>
        /// <param name="length"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void RemoveComponent<T>(this EntityCommandBuffer self, Entity* entities, int length)
        {
            RemoveComponent(self, entities, length, ComponentType.ReadWrite<T>());
        }

        /// <summary>
        /// <inheritdoc cref="EntityCommandBuffer.RemoveComponent{T}(NativeArray{Entity})"/>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="self"></param>
        /// <param name="entities"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void RemoveComponent<T>(this EntityCommandBuffer self, UnsafeSpan<Entity> entities)
        {
            RemoveComponent<T>(self, entities.Ptr, entities.Length);
        }

        /// <summary>
        /// <inheritdoc cref="EntityCommandBuffer.RemoveComponent(NativeArray{Entity}, ComponentType)"/>
        /// </summary>
        /// <param name="self"></param>
        /// <param name="entities"></param>
        /// <param name="length"></param>
        /// <param name="componentType"></param>
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

        /// <summary>
        /// <inheritdoc cref="EntityCommandBuffer.RemoveComponent(NativeArray{Entity}, ComponentType)"/>
        /// </summary>
        /// <param name="self"></param>
        /// <param name="entities"></param>
        /// <param name="componentType"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void RemoveComponent(this EntityCommandBuffer self, UnsafeSpan<Entity> entities, ComponentType componentType)
        {
            RemoveComponent(self, entities.Ptr, entities.Length, componentType);
        }

        /// <summary>
        /// <inheritdoc cref="EntityCommandBuffer.RemoveComponent(NativeArray{Entity}, in ComponentTypeSet)"/>
        /// </summary>
        /// <param name="self"></param>
        /// <param name="entities"></param>
        /// <param name="length"></param>
        /// <param name="componentTypeSet"></param>
        public static void RemoveComponent(this EntityCommandBuffer self, Entity* entities, int length, in ComponentTypeSet componentTypeSet)
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
                in componentTypeSet);
        }

        /// <summary>
        /// <inheritdoc cref="EntityCommandBuffer.RemoveComponent(NativeArray{Entity}, in ComponentTypeSet)"/>
        /// </summary>
        /// <param name="self"></param>
        /// <param name="entities"></param>
        /// <param name="componentTypeSet"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void RemoveComponent(this EntityCommandBuffer self, UnsafeSpan<Entity> entities, in ComponentTypeSet componentTypeSet)
        {
            RemoveComponent(self, entities.Ptr, entities.Length, in componentTypeSet);
        }
    }
}
