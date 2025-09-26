using EvilOctane.Entities.Internal;
using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Entities;
using Unity.Jobs;

#if EVIL_OCTANE_ENABLE_PARALLEL_EVENT_ROUTING
using Unity.Collections;
using Unity.Jobs.LowLevel.Unsafe;
using static Unity.Collections.CollectionHelper;
#endif

namespace EvilOctane.Entities
{
    [UpdateAfter(typeof(BeginInitializationEntityCommandBufferSystem))]
    [UpdateAfter(typeof(EventListenerSystem))]
    [UpdateInGroup(typeof(InitializationSystemGroup), OrderFirst = true)]
    public partial struct EventFirerSystem : ISystem
    {
        private EntityQuery changeSubscriptionStatusQuery;
        private EntityQuery routeQuery;
        private EntityQuery eventFirerCleanupQuery;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            // 0
            changeSubscriptionStatusQuery = SystemAPI.QueryBuilder()
                .WithPresentRW<
                    EventSubscriptionRegistry.Component,
                    EventSubscriptionRegistry.ChangeSubscriptionStatusBufferElement>()
                .Build();

            changeSubscriptionStatusQuery.SetChangedVersionFilter<EventSubscriptionRegistry.ChangeSubscriptionStatusBufferElement>();

            // 1
            routeQuery = SystemAPI.QueryBuilder()
                .WithPresent<
                    EventBuffer.EntityElement,
                    EventBuffer.TypeElement>()
                .WithPresentRW<EventSubscriptionRegistry.Component>()

                // Missing the following:
                // .WithPresent<CleanupComponentsAliveTag>()
                // so that Events from destroyed Entities also get routed

                .Build();

            routeQuery.SetChangedVersionFilter<EventBuffer.EntityElement>();

            // 2
            eventFirerCleanupQuery = SystemAPI.QueryBuilder()
                .WithAny<
                    EventSubscriptionRegistry.Component,
                    EventBuffer.EntityElement,
                    EventBuffer.TypeElement>()
                .WithAbsent<CleanupComponentsAliveTag>()
                .Build();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if (!changeSubscriptionStatusQuery.IsEmpty)
            {
                ScheduleChangeSubscriptionStatus(ref state);
            }

            bool routeQueryIsEmpty = routeQuery.IsEmpty;
            bool eventFirerCleanupQueryIsEmpty = eventFirerCleanupQuery.IsEmptyIgnoreFilter;

            if (routeQueryIsEmpty & eventFirerCleanupQueryIsEmpty)
            {
                // Skip creating Command Buffer
                return;
            }

            BeginInitializationEntityCommandBufferSystem.Singleton nextFrameCommandBufferSystem = SystemAPI.GetSingleton<BeginInitializationEntityCommandBufferSystem.Singleton>();

            EntityCommandBuffer commandBuffer = nextFrameCommandBufferSystem.CreateCommandBuffer(state.WorldUnmanaged);
            EntityCommandBuffer.ParallelWriter parallelWriter = commandBuffer.AsParallelWriter();

            if (!routeQueryIsEmpty)
            {
#if EVIL_OCTANE_ENABLE_PARALLEL_EVENT_ROUTING
                ScheduleEventRouteParallel(ref state, parallelWriter);
#else
                ScheduleEventRoute(ref state, commandBuffer);
#endif
            }

            if (!eventFirerCleanupQueryIsEmpty)
            {
                ScheduleEventBufferCleanup(ref state, parallelWriter);
            }
        }

#if UNITY_EDITOR
        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
            // It looks like components holding malloc'ed memory
            // need to be freed manually to prevent leaks

            EntityCommandBuffer commandBuffer = new(state.WorldUpdateAllocator);

            foreach ((
                RefRW<EventSubscriptionRegistry.Component> subscriptionRegistry,
                Entity entity) in SystemAPI
                .Query<RefRW<EventSubscriptionRegistry.Component>>()
                .WithEntityAccess())
            {
                subscriptionRegistry.ValueRW.Dispose();
                commandBuffer.RemoveComponent<EventSubscriptionRegistry.Component>(entity);
            }

            commandBuffer.Playback(state.EntityManager);
        }
#endif

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void ScheduleChangeSubscriptionStatus(ref SystemState state)
        {
            state.Dependency = new EventChangeSubscriptionStatusJob()
            {
                EventSubscriptionRegistryComponentTypeHandle = SystemAPI.GetComponentTypeHandle<EventSubscriptionRegistry.Component>(),
                EventSubscriptionRegistryChangeSubscriptionStatusBufferTypeHandle = SystemAPI.GetBufferTypeHandle<EventSubscriptionRegistry.ChangeSubscriptionStatusBufferElement>(),
                TempAllocator = state.WorldUpdateAllocator
            }.ScheduleParallel(changeSubscriptionStatusQuery, state.Dependency);
        }

#if EVIL_OCTANE_ENABLE_PARALLEL_EVENT_ROUTING
        [MethodImpl(MethodImplOptions.NoInlining)]
        private void ScheduleEventRouteParallel(ref SystemState state, EntityCommandBuffer.ParallelWriter commandBuffer)
        {
            NativeArray<EventRouteJobParallel.PerThreadTempContainers> perThreadTempContainersArray = CreateNativeArray<EventRouteJobParallel.PerThreadTempContainers>(JobsUtility.MaxJobThreadCount, state.WorldUpdateAllocator, NativeArrayOptions.ClearMemory);

            state.Dependency = new EventRouteJobParallel()
            {
                EntityTypeHandle = SystemAPI.GetEntityTypeHandle(),

                EventSubscriptionRegistryComponentTypeHandle = SystemAPI.GetComponentTypeHandle<EventSubscriptionRegistry.Component>(),
                EventEntityBufferTypeHandle = SystemAPI.GetBufferTypeHandle<EventBuffer.EntityElement>(),
                EventTypeBufferTypeHandle = SystemAPI.GetBufferTypeHandle<EventBuffer.TypeElement>(),

                EventReceiveBufferLookup = SystemAPI.GetBufferLookup<EventReceiveBuffer.Element>(),
                EventReceiveBufferLockComponentLookup = SystemAPI.GetComponentLookup<EventReceiveBuffer.LockComponent>(),

                PerThreadTempContainersArray = perThreadTempContainersArray,

                TempAllocator = state.WorldUpdateAllocator,
                CommandBuffer = commandBuffer
            }.ScheduleParallel(routeQuery, state.Dependency);
        }
#else
        [MethodImpl(MethodImplOptions.NoInlining)]
        private void ScheduleEventRoute(ref SystemState state, EntityCommandBuffer commandBuffer)
        {
            state.Dependency = new EventRouteJob()
            {
                EntityTypeHandle = SystemAPI.GetEntityTypeHandle(),

                EventSubscriptionRegistryComponentTypeHandle = SystemAPI.GetComponentTypeHandle<EventSubscriptionRegistry.Component>(),
                EventEntityBufferTypeHandle = SystemAPI.GetBufferTypeHandle<EventBuffer.EntityElement>(),
                EventTypeBufferTypeHandle = SystemAPI.GetBufferTypeHandle<EventBuffer.TypeElement>(),

                EventReceiveBufferLookup = SystemAPI.GetBufferLookup<EventReceiveBuffer.Element>(),

                TempAllocator = state.WorldUpdateAllocator,
                CommandBuffer = commandBuffer
            }.Schedule(routeQuery, state.Dependency);
        }
#endif

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void ScheduleEventBufferCleanup(ref SystemState state, EntityCommandBuffer.ParallelWriter commandBuffer)
        {
            state.Dependency = new EventFirerCleanupJob()
            {
                EntityTypeHandle = SystemAPI.GetEntityTypeHandle(),
                EntityLookup = SystemAPI.GetComponentLookup<CleanupComponentsAliveTag>(isReadOnly: true),
                EventSubscriptionRegistryComponentTypeHandle = SystemAPI.GetComponentTypeHandle<EventSubscriptionRegistry.Component>(),
                EventEntityBufferTypeHandle = SystemAPI.GetBufferTypeHandle<EventBuffer.EntityElement>(isReadOnly: true),
                TempAllocator = state.WorldUpdateAllocator,
                CommandBuffer = commandBuffer
            }.ScheduleParallel(eventFirerCleanupQuery, state.Dependency);
        }
    }
}
