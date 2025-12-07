using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Unity.Entities
{
    [NativeContainer]
    [NativeContainerIsReadOnly]
    public unsafe struct SharedComponentLookup<T>
        where T : unmanaged, ISharedComponentData
    {
        [NativeDisableUnsafePtrRestriction]
        private readonly EntityDataAccess* m_Access;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        internal AtomicSafetyHandle m_Safety;
#endif

        private readonly TypeIndex m_TypeIndex;

        internal SharedComponentLookup(TypeIndex typeIndex, EntityDataAccess* access)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            ComponentSafetyHandles* safetyHandles = &access->DependencyManager->Safety;
            m_Safety = safetyHandles->GetSafetyHandleForComponentLookup(typeIndex, isReadOnly: true);
#endif
            m_TypeIndex = typeIndex;
            m_Access = access;
        }

        /// <summary>
        /// <inheritdoc cref="ComponentLookup{T}.Update(SystemBase)"/>
        /// </summary>
        /// <param name="system"></param>
        public void Update(SystemBase system)
        {
            Update(ref *system.m_StatePtr);
        }

        /// <summary>
        /// <inheritdoc cref="ComponentLookup{T}.Update(ref SystemState)"/>
        /// </summary>
        /// <param name="systemState"></param>
        public void Update(ref SystemState systemState)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            ComponentSafetyHandles* safetyHandles = &m_Access->DependencyManager->Safety;
            m_Safety = safetyHandles->GetSafetyHandleForComponentLookup(m_TypeIndex, isReadOnly: true);
#endif
        }

        /// <summary>
        /// <inheritdoc cref="ComponentLookup{T}.EntityExists(Entity)"/>
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public readonly bool EntityExists(Entity entity)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
#endif
            EntityComponentStore* ecs = m_Access->EntityComponentStore;
            return ecs->Exists(entity);
        }

        /// <summary>
        /// <inheritdoc cref="ComponentLookup{T}.HasComponent(Entity)"/>
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public readonly bool HasComponent(Entity entity)
        {
            return HasComponent(entity, out _);
        }

        /// <summary>
        /// <inheritdoc cref="ComponentLookup{T}.HasComponent(Entity, out bool)"/>
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="entityExists"></param>
        /// <returns></returns>
        public readonly bool HasComponent(Entity entity, out bool entityExists)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
#endif
            EntityComponentStore* ecs = m_Access->EntityComponentStore;
            return ecs->HasComponent(entity, m_TypeIndex, out entityExists);
        }

        /// <summary>
        /// <inheritdoc cref="ComponentLookup{T}.TryGetComponent(Entity, out T, out bool)"/>
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="sharedData"></param>
        /// <param name="entityExists"></param>
        /// <returns></returns>
        public readonly bool TryGetComponent(Entity entity, out T sharedData, out bool entityExists)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
#endif
            if (!HasComponent(entity, out entityExists))
            {
                sharedData = default;
                return false;
            }

            sharedData = m_Access->GetSharedComponentData_Unmanaged<T>(entity);
            return true;
        }

        /// <summary>
        /// <inheritdoc cref="ComponentLookup{T}.TryGetComponent(Entity, out T)"/>
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="sharedData"></param>
        /// <returns></returns>
        public readonly bool TryGetComponent(Entity entity, out T sharedData)
        {
            return TryGetComponent(entity, out sharedData, out _);
        }

        /// <summary>
        /// <inheritdoc cref="ComponentLookup{T}.TryGetComponent(Entity, out T, out bool)"/>
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="sharedData"></param>
        /// <param name="entityExists"></param>
        /// <returns></returns>
        public readonly bool TryGetSharedComponentIndex(Entity entity, out int sharedComponentIndex, out bool entityExists)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
#endif
            if (!HasComponent(entity, out entityExists))
            {
                sharedComponentIndex = default;
                return false;
            }

            sharedComponentIndex = GetSharedComponentIndex(entity);
            return true;
        }

        /// <summary>
        /// <inheritdoc cref="ComponentLookup{T}.TryGetComponent(Entity, out T)"/>
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="sharedData"></param>
        /// <returns></returns>
        public readonly bool TryGetSharedComponentIndex(Entity entity, out int sharedComponentIndex)
        {
            return TryGetSharedComponentIndex(entity, out sharedComponentIndex, out _);
        }

        /// <summary>
        /// <inheritdoc cref="EntityManager.GetSharedComponentIndex{T}(Entity)"/>
        /// </summary>
        /// <param name="sharedData"></param>
        /// <returns></returns>
        public readonly int GetSharedComponentIndex(Entity entity)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
#endif
            EntityComponentStore* ecs = m_Access->EntityComponentStore;
            ecs->AssertEntityHasComponent(entity, m_TypeIndex);

            return ecs->GetSharedComponentDataIndex(entity, m_TypeIndex);
        }

        /// <summary>
        /// <inheritdoc cref="EntityManager.GetSharedComponentOrderVersion{T}(T)"/>
        /// </summary>
        /// <param name="sharedData"></param>
        /// <returns></returns>
        public readonly int GetSharedComponentVersion(T sharedData)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
#endif
            return m_Access->GetSharedComponentVersion_Unmanaged(sharedData);
        }

        /// <summary>
        /// <inheritdoc cref="EntityManager.GetSharedComponent{T}(Entity)"/>
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public readonly T this[Entity entity]
        {
            get
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
#endif
                return m_Access->GetSharedComponentData_Unmanaged<T>(entity);
            }
        }

        /// <summary>
        /// <inheritdoc cref="EntityManager.GetSharedComponent{T}(int)"/>
        /// </summary>
        /// <param name="sharedComponentIndex"></param>
        /// <returns></returns>
        public readonly T this[int sharedComponentIndex]
        {
            get
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
#endif
                return m_Access->GetSharedComponentData_Unmanaged<T>(sharedComponentIndex);
            }
        }
    }

    [GenerateTestsForBurstCompatibility]
    public static unsafe partial class SystemStateExtensions
    {
        /// <summary>
        /// <inheritdoc cref="SystemState.GetComponentLookup{T}(bool)"/>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="self"></param>
        /// <returns></returns>
        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(BurstCompatibleSharedComponentData) })]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static SharedComponentLookup<T> GetSharedComponentLookup<T>(this ref SystemState self)
            where T : unmanaged, ISharedComponentData
        {
            TypeIndex typeIndex = TypeManager.GetTypeIndex<T>();
            EntityDataAccess* access = self.EntityManager.GetCheckedEntityDataAccess();
            return new SharedComponentLookup<T>(typeIndex, access);
        }
    }
}
