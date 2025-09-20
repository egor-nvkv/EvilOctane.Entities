using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Unity.Collections.LowLevel.Unsafe;

namespace Unity.Entities.LowLevel.Unsafe
{
    public static unsafe partial class EntityCommandBufferParallelWriterExtensions
    {
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void DestroyEntity(this EntityCommandBuffer.ParallelWriter self, int sortKey, UnsafeSpan<Entity> entities)
        {
            DestroyEntity(self, sortKey, entities.Ptr, entities.Length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void RemoveComponent<T>(this EntityCommandBuffer.ParallelWriter self, int sortKey, Entity* entities, int length)
        {
            RemoveComponent(self, sortKey, entities, length, ComponentType.ReadWrite<T>());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void RemoveComponent<T>(this EntityCommandBuffer.ParallelWriter self, int sortKey, UnsafeSpan<Entity> entities)
        {
            RemoveComponent<T>(self, sortKey, entities.Ptr, entities.Length);
        }

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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void RemoveComponent(this EntityCommandBuffer.ParallelWriter self, int sortKey, UnsafeSpan<Entity> entities, ComponentType componentType)
        {
            RemoveComponent(self, sortKey, entities.Ptr, entities.Length, componentType);
        }

        public static void RemoveComponent(this EntityCommandBuffer.ParallelWriter self, int sortKey, Entity* entities, int length, ComponentTypeSet componentTypeSet)
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
                componentTypeSet);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void RemoveComponent(this EntityCommandBuffer.ParallelWriter self, int sortKey, UnsafeSpan<Entity> entities, ComponentTypeSet componentTypeSet)
        {
            RemoveComponent(self, sortKey, entities.Ptr, entities.Length, componentTypeSet);
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
