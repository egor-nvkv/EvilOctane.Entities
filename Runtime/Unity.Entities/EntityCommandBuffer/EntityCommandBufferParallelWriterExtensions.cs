using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Unity.Entities.LowLevel.Unsafe
{
    public static unsafe partial class EntityCommandBufferParallelWriterExtensions
    {
        /// <summary>
        /// <inheritdoc cref="EntityCommandBuffer.ParallelWriter.DestroyEntity(int, NativeArray{Entity})"/>
        /// </summary>
        /// <param name="self"></param>
        /// <param name="sortKey"></param>
        /// <param name="entities"></param>
        /// <param name="length"></param>
        public static void DestroyEntity(this EntityCommandBuffer.ParallelWriter self, int sortKey, Entity* entities, int length)
        {
            CheckWriteAccess(self);

            EntityCommandBufferChain* chain = GetThreadChain(self);
            Entity* entitiesCopy = self.m_Data->CloneAndSearchForDeferredEntities(entities, length, out bool containsDeferredEntities);

            _ = self.m_Data->AppendMultipleEntitiesCommand(
                chain,
                sortKey,
                ECBCommand.DestroyMultipleEntities,
                entitiesCopy,
                length,
                containsDeferredEntities);
        }

        /// <summary>
        /// <inheritdoc cref="EntityCommandBuffer.ParallelWriter.DestroyEntity(int, NativeArray{Entity})"/>
        /// </summary>
        /// <param name="self"></param>
        /// <param name="sortKey"></param>
        /// <param name="entities"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void DestroyEntity(this EntityCommandBuffer.ParallelWriter self, int sortKey, UnsafeSpan<Entity> entities)
        {
            DestroyEntity(self, sortKey, entities.Ptr, entities.Length);
        }

        /// <summary>
        /// <inheritdoc cref="EntityCommandBuffer.ParallelWriter.AddComponent{T}(int, NativeArray{Entity}, T)"/>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="self"></param>
        /// <param name="sortKey"></param>
        /// <param name="entities"></param>
        /// <param name="length"></param>
        /// <param name="component"></param>
        public static void AddComponent<T>(this EntityCommandBuffer.ParallelWriter self, int sortKey, Entity* entities, int length, T component)
            where T : unmanaged, IComponentData
        {
            CheckWriteAccess(self);

            EntityCommandBufferChain* chain = GetThreadChain(self);
            Entity* entitiesCopy = self.m_Data->CloneAndSearchForDeferredEntities(entities, length, out bool containsDeferredEntities);

            _ = self.m_Data->AppendMultipleEntitiesComponentCommandWithValue(
                chain,
                sortKey,
                ECBCommand.AddComponentForMultipleEntities,
                entitiesCopy,
                length,
                containsDeferredEntities,
                component);
        }

        /// <summary>
        /// <inheritdoc cref="EntityCommandBuffer.ParallelWriter.AddComponent{T}(int, NativeArray{Entity}, T)"/>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="self"></param>
        /// <param name="sortKey"></param>
        /// <param name="entities"></param>
        /// <param name="component"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AddComponent<T>(this EntityCommandBuffer.ParallelWriter self, int sortKey, UnsafeSpan<Entity> entities, T component)
            where T : unmanaged, IComponentData
        {
            AddComponent(self, sortKey, entities.Ptr, entities.Length, component);
        }

        /// <summary>
        /// <inheritdoc cref="EntityCommandBuffer.ParallelWriter.AddComponent{T}(int, NativeArray{Entity})"/>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="self"></param>
        /// <param name="sortKey"></param>
        /// <param name="entities"></param>
        /// <param name="length"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AddComponent<T>(this EntityCommandBuffer.ParallelWriter self, int sortKey, Entity* entities, int length)
        {
            AddComponent(self, sortKey, entities, length, ComponentType.ReadWrite<T>());
        }

        /// <summary>
        /// <inheritdoc cref="EntityCommandBuffer.ParallelWriter.AddComponent{T}(int, NativeArray{Entity})"/>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="self"></param>
        /// <param name="sortKey"></param>
        /// <param name="entities"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AddComponent<T>(this EntityCommandBuffer.ParallelWriter self, int sortKey, UnsafeSpan<Entity> entities)
        {
            AddComponent<T>(self, sortKey, entities.Ptr, entities.Length);
        }

        /// <summary>
        /// <inheritdoc cref="EntityCommandBuffer.ParallelWriter.AddComponent(int, NativeArray{Entity}, ComponentType)"/>
        /// </summary>
        /// <param name="self"></param>
        /// <param name="sortKey"></param>
        /// <param name="entities"></param>
        /// <param name="length"></param>
        /// <param name="componentType"></param>
        public static void AddComponent(this EntityCommandBuffer.ParallelWriter self, int sortKey, Entity* entities, int length, ComponentType componentType)
        {
            CheckWriteAccess(self);

            EntityCommandBufferChain* chain = GetThreadChain(self);
            Entity* entitiesCopy = self.m_Data->CloneAndSearchForDeferredEntities(entities, length, out bool containsDeferredEntities);

            _ = self.m_Data->AppendMultipleEntitiesComponentCommand(
                chain,
                sortKey,
                ECBCommand.AddComponentForMultipleEntities,
                entitiesCopy,
                length,
                containsDeferredEntities,
                componentType);
        }

        /// <summary>
        /// <inheritdoc cref="EntityCommandBuffer.ParallelWriter.AddComponent(int, NativeArray{Entity}, ComponentType)"/>
        /// </summary>
        /// <param name="self"></param>
        /// <param name="sortKey"></param>
        /// <param name="entities"></param>
        /// <param name="componentType"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AddComponent(this EntityCommandBuffer.ParallelWriter self, int sortKey, UnsafeSpan<Entity> entities, ComponentType componentType)
        {
            AddComponent(self, sortKey, entities.Ptr, entities.Length, componentType);
        }

        /// <summary>
        /// <inheritdoc cref="EntityCommandBuffer.ParallelWriter.AddComponent(int, NativeArray{Entity}, in ComponentTypeSet)"/>
        /// </summary>
        /// <param name="self"></param>
        /// <param name="sortKey"></param>
        /// <param name="entities"></param>
        /// <param name="length"></param>
        /// <param name="componentTypeSet"></param>
        public static void AddComponent(this EntityCommandBuffer.ParallelWriter self, int sortKey, Entity* entities, int length, in ComponentTypeSet componentTypeSet)
        {
            CheckWriteAccess(self);

            EntityCommandBufferChain* chain = GetThreadChain(self);
            Entity* entitiesCopy = self.m_Data->CloneAndSearchForDeferredEntities(entities, length, out bool containsDeferredEntities);

            _ = self.m_Data->AppendMultipleEntitiesMultipleComponentsCommand(
                chain,
                sortKey,
                ECBCommand.AddMultipleComponentsForMultipleEntities,
                entitiesCopy,
                length,
                containsDeferredEntities,
                in componentTypeSet);
        }

        /// <summary>
        /// <inheritdoc cref="EntityCommandBuffer.ParallelWriter.AddComponent(int, NativeArray{Entity}, in ComponentTypeSet)"/>
        /// </summary>
        /// <param name="self"></param>
        /// <param name="sortKey"></param>
        /// <param name="entities"></param>
        /// <param name="componentTypeSet"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AddComponent(this EntityCommandBuffer.ParallelWriter self, int sortKey, UnsafeSpan<Entity> entities, in ComponentTypeSet componentTypeSet)
        {
            AddComponent(self, sortKey, entities.Ptr, entities.Length, in componentTypeSet);
        }

        /// <summary>
        /// <inheritdoc cref="EntityCommandBuffer.ParallelWriter.RemoveComponent{T}(int, NativeArray{Entity})"/>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="self"></param>
        /// <param name="sortKey"></param>
        /// <param name="entities"></param>
        /// <param name="length"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void RemoveComponent<T>(this EntityCommandBuffer.ParallelWriter self, int sortKey, Entity* entities, int length)
        {
            RemoveComponent(self, sortKey, entities, length, ComponentType.ReadWrite<T>());
        }

        /// <summary>
        /// <inheritdoc cref="EntityCommandBuffer.ParallelWriter.RemoveComponent{T}(int, NativeArray{Entity})"/>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="self"></param>
        /// <param name="sortKey"></param>
        /// <param name="entities"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void RemoveComponent<T>(this EntityCommandBuffer.ParallelWriter self, int sortKey, UnsafeSpan<Entity> entities)
        {
            RemoveComponent<T>(self, sortKey, entities.Ptr, entities.Length);
        }

        /// <summary>
        /// <inheritdoc cref="EntityCommandBuffer.ParallelWriter.RemoveComponent(int, NativeArray{Entity}, ComponentType)"/>
        /// </summary>
        /// <param name="self"></param>
        /// <param name="sortKey"></param>
        /// <param name="entities"></param>
        /// <param name="length"></param>
        /// <param name="componentType"></param>
        public static void RemoveComponent(this EntityCommandBuffer.ParallelWriter self, int sortKey, Entity* entities, int length, ComponentType componentType)
        {
            CheckWriteAccess(self);

            EntityCommandBufferChain* chain = GetThreadChain(self);
            Entity* entitiesCopy = self.m_Data->CloneAndSearchForDeferredEntities(entities, length, out bool containsDeferredEntities);

            _ = self.m_Data->AppendMultipleEntitiesComponentCommand(
                chain,
                sortKey,
                ECBCommand.RemoveComponentForMultipleEntities,
                entitiesCopy,
                length,
                containsDeferredEntities,
                componentType);
        }

        /// <summary>
        /// <inheritdoc cref="EntityCommandBuffer.ParallelWriter.RemoveComponent(int, NativeArray{Entity}, ComponentType)"/>
        /// </summary>
        /// <param name="self"></param>
        /// <param name="sortKey"></param>
        /// <param name="entities"></param>
        /// <param name="componentType"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void RemoveComponent(this EntityCommandBuffer.ParallelWriter self, int sortKey, UnsafeSpan<Entity> entities, ComponentType componentType)
        {
            RemoveComponent(self, sortKey, entities.Ptr, entities.Length, componentType);
        }

        /// <summary>
        /// <inheritdoc cref="EntityCommandBuffer.ParallelWriter.RemoveComponent(int, NativeArray{Entity}, in ComponentTypeSet)"/>
        /// </summary>
        /// <param name="self"></param>
        /// <param name="sortKey"></param>
        /// <param name="entities"></param>
        /// <param name="length"></param>
        /// <param name="componentTypeSet"></param>
        public static void RemoveComponent(this EntityCommandBuffer.ParallelWriter self, int sortKey, Entity* entities, int length, in ComponentTypeSet componentTypeSet)
        {
            CheckWriteAccess(self);

            EntityCommandBufferChain* chain = GetThreadChain(self);
            Entity* entitiesCopy = self.m_Data->CloneAndSearchForDeferredEntities(entities, length, out bool containsDeferredEntities);

            _ = self.m_Data->AppendMultipleEntitiesMultipleComponentsCommand(
                chain,
                sortKey,
                ECBCommand.RemoveMultipleComponentsForMultipleEntities,
                entitiesCopy,
                length,
                containsDeferredEntities,
                in componentTypeSet);
        }

        /// <summary>
        /// <inheritdoc cref="EntityCommandBuffer.ParallelWriter.RemoveComponent(int, NativeArray{Entity}, in ComponentTypeSet)"/>
        /// </summary>
        /// <param name="self"></param>
        /// <param name="sortKey"></param>
        /// <param name="entities"></param>
        /// <param name="componentTypeSet"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void RemoveComponent(this EntityCommandBuffer.ParallelWriter self, int sortKey, UnsafeSpan<Entity> entities, in ComponentTypeSet componentTypeSet)
        {
            RemoveComponent(self, sortKey, entities.Ptr, entities.Length, in componentTypeSet);
        }

        /// <summary>
        /// <inheritdoc cref="EntityCommandBuffer.ParallelWriter.AddSharedComponent{T}(int, NativeArray{Entity}, T)"/>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="self"></param>
        /// <param name="sortKey"></param>
        /// <param name="entities"></param>
        /// <param name="length"></param>
        /// <param name="sharedComponent"></param>
        public static void AddSharedComponent<T>(this EntityCommandBuffer.ParallelWriter self, int sortKey, Entity* entities, int length, T sharedComponent)
            where T : unmanaged, ISharedComponentData
        {
            CheckWriteAccess(self);

            EntityCommandBufferChain* chain = GetThreadChain(self);
            Entity* entitiesCopy = self.m_Data->CloneAndSearchForDeferredEntities(entities, length, out bool containsDeferredEntities);
            bool isDefaultObject = EntityCommandBufferExtensions.IsDefaultObjectUnmanaged(ref sharedComponent, out int hashCode);

            _ = self.m_Data->AppendMultipleEntitiesCommand_WithUnmanagedSharedValue<T>(
                chain,
                sortKey,
                ECBCommand.AddUnmanagedSharedComponentValueForMultipleEntities,
                entitiesCopy,
                length,
                containsDeferredEntities,
                hashCode,
                isDefaultObject ? null : UnsafeUtility.AddressOf(ref sharedComponent));
        }

        /// <summary>
        /// <inheritdoc cref="EntityCommandBuffer.ParallelWriter.AddSharedComponent{T}(int, NativeArray{Entity}, T)"/>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="self"></param>
        /// <param name="sortKey"></param>
        /// <param name="entities"></param>
        /// <param name="sharedComponent"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AddSharedComponent<T>(this EntityCommandBuffer.ParallelWriter self, int sortKey, UnsafeSpan<Entity> entities, T sharedComponent)
            where T : unmanaged, ISharedComponentData
        {
            AddSharedComponent(self, sortKey, entities.Ptr, entities.Length, sharedComponent);
        }

        /// <summary>
        /// <inheritdoc cref="EntityCommandBuffer.ParallelWriter.SetSharedComponent{T}(int, NativeArray{Entity}, T)"/>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="self"></param>
        /// <param name="sortKey"></param>
        /// <param name="entities"></param>
        /// <param name="length"></param>
        /// <param name="sharedComponent"></param>
        public static void SetSharedComponent<T>(this EntityCommandBuffer.ParallelWriter self, int sortKey, Entity* entities, int length, T sharedComponent)
            where T : unmanaged, ISharedComponentData
        {
            CheckWriteAccess(self);

            EntityCommandBufferChain* chain = GetThreadChain(self);
            Entity* entitiesCopy = self.m_Data->CloneAndSearchForDeferredEntities(entities, length, out bool containsDeferredEntities);
            bool isDefaultObject = EntityCommandBufferExtensions.IsDefaultObjectUnmanaged(ref sharedComponent, out int hashCode);

            _ = self.m_Data->AppendMultipleEntitiesCommand_WithUnmanagedSharedValue<T>(
                chain,
                sortKey,
                ECBCommand.SetUnmanagedSharedComponentValueForMultipleEntities,
                entitiesCopy,
                length,
                containsDeferredEntities,
                hashCode,
                isDefaultObject ? null : UnsafeUtility.AddressOf(ref sharedComponent));
        }

        /// <summary>
        /// <inheritdoc cref="EntityCommandBuffer.ParallelWriter.SetSharedComponent{T}(int, NativeArray{Entity}, T)"/>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="self"></param>
        /// <param name="sortKey"></param>
        /// <param name="entities"></param>
        /// <param name="sharedComponent"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetSharedComponent<T>(this EntityCommandBuffer.ParallelWriter self, int sortKey, UnsafeSpan<Entity> entities, T sharedComponent)
            where T : unmanaged, ISharedComponentData
        {
            SetSharedComponent(self, sortKey, entities.Ptr, entities.Length, sharedComponent);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static EntityCommandBufferChain* GetThreadChain(EntityCommandBuffer.ParallelWriter parallelWriter)
        {
            return (parallelWriter.m_ThreadIndex >= 0) ? &parallelWriter.m_Data->m_ThreadedChains[parallelWriter.m_ThreadIndex] : &parallelWriter.m_Data->m_MainThreadChain;
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        [Conditional("UNITY_DOTS_DEBUG")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void CheckWriteAccess(EntityCommandBuffer.ParallelWriter parallelWriter)
        {
            if (parallelWriter.m_Data == null)
            {
                throw new NullReferenceException("The EntityCommandBuffer has not been initialized! The EntityCommandBuffer needs to be passed an Allocator when created!");
            }

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndThrow(parallelWriter.m_Safety0);
#endif
        }
    }
}
