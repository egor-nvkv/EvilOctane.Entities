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
        private EntityQuery eventFirerSetupQuery;
        private EntityQuery subscribeUnsubscribeQuery;
        private EntityQuery routeQuery;
        private EntityQuery eventFirerCleanupQuery;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            eventFirerSetupQuery = SystemAPI.QueryBuilder()
                .WithPresent<EventSetup.FirerDeclaredEventTypeBufferElement>()
                .WithAbsent<EventSubscriptionRegistry.StorageBufferElement>()
                .Build();

            subscribeUnsubscribeQuery = SystemAPI.QueryBuilder()
                .WithPresentRW<
                    EventSubscriptionRegistry.StorageBufferElement,
                    EventSubscriptionRegistry.SubscribeUnsubscribeBufferElement>()
                .Build();

            subscribeUnsubscribeQuery.SetChangedVersionFilter<EventSubscriptionRegistry.SubscribeUnsubscribeBufferElement>();

            // Required for instantiated Firers with immediate subscriptions to work
            subscribeUnsubscribeQuery.AddOrderVersionFilter();

            routeQuery = SystemAPI.QueryBuilder()
                .WithPresentRW<
                    EventSubscriptionRegistry.StorageBufferElement>()
                .WithPresent<
                    EventBuffer.EntityElement,
                    EventBuffer.TypeElement>()

                // Missing the following:
                // .WithPresent<CleanupComponentsAliveTag>()
                // so that Events from destroyed Entities also get routed

                .Build();

            routeQuery.SetChangedVersionFilter<EventBuffer.EntityElement>();

            eventFirerCleanupQuery = SystemAPI.QueryBuilder()
                .WithAny<
                    EventSubscriptionRegistry.StorageBufferElement,
                    EventBuffer.EntityElement,
                    EventBuffer.TypeElement>()
                .WithAbsent<CleanupComponentsAliveTag>()
                .Build();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if (!eventFirerSetupQuery.IsEmptyIgnoreFilter)
            {
                ScheduleEventFirerSetup(ref state);
            }

            if (!subscribeUnsubscribeQuery.IsEmpty)
            {
                ScheduleSubscribeUnsubscribe(ref state);
            }

            bool routeQueryIsEmpty = routeQuery.IsEmpty;
            bool eventFirerCleanupQueryIsEmpty = eventFirerCleanupQuery.IsEmptyIgnoreFilter;

            if (routeQueryIsEmpty & eventFirerCleanupQueryIsEmpty)
            {
                // Skip creating Command Buffer
                return;
            }

            BeginInitializationEntityCommandBufferSystem.Singleton nextFrameCommandBufferSystem = SystemAPI.GetSingleton<BeginInitializationEntityCommandBufferSystem.Singleton>();

            EntityCommandBuffer nextFrameCommandBuffer = nextFrameCommandBufferSystem.CreateCommandBuffer(state.WorldUnmanaged);
            EntityCommandBuffer.ParallelWriter nextFrameParallelWriter = nextFrameCommandBuffer.AsParallelWriter();

            if (!routeQueryIsEmpty)
            {
#if EVIL_OCTANE_ENABLE_PARALLEL_EVENT_ROUTING
                ScheduleEventRouteParallel(ref state, parallelWriter);
#else
                ScheduleEventRoute(ref state, nextFrameCommandBuffer);
#endif
            }

            if (!eventFirerCleanupQueryIsEmpty)
            {
                ScheduleEventFirerCleanup(ref state, nextFrameParallelWriter);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void ScheduleEventFirerSetup(ref SystemState state)
        {
            EntityCommandBuffer commandBuffer = SystemAPI.GetSingleton<EndInitializationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);

            state.Dependency = new EventFirerSetupJob()
            {
                EntityTypeHandle = SystemAPI.GetEntityTypeHandle(),
                SetupDeclaredEventTypeBufferTypeHandle = SystemAPI.GetBufferTypeHandle<EventSetup.FirerDeclaredEventTypeBufferElement>(isReadOnly: true),
                TempAllocator = state.WorldUpdateAllocator,
                CommandBuffer = commandBuffer.AsParallelWriter()
            }.ScheduleParallel(eventFirerSetupQuery, state.Dependency);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void ScheduleSubscribeUnsubscribe(ref SystemState state)
        {
            state.Dependency = new EventFirerSubscribeUnsubscribeJob()
            {
                EventSubscriptionRegistryStorageBufferTypeHandle = SystemAPI.GetBufferTypeHandle<EventSubscriptionRegistry.StorageBufferElement>(),
                EventSubscriptionRegistrySubscribeUnsubscribeBufferTypeHandle = SystemAPI.GetBufferTypeHandle<EventSubscriptionRegistry.SubscribeUnsubscribeBufferElement>(),
                ListenerSetupDeclaredEventTypeBufferLookup = SystemAPI.GetBufferLookup<EventSetup.ListenerDeclaredEventTypeBufferElement>(isReadOnly: true),
                ListenerDeclaredEventTypeBufferLookup = SystemAPI.GetBufferLookup<EventSettings.ListenerDeclaredEventTypeBufferElement>(isReadOnly: true),
                TempAllocator = state.WorldUpdateAllocator
            }.ScheduleParallel(subscribeUnsubscribeQuery, state.Dependency);
        }

#if EVIL_OCTANE_ENABLE_PARALLEL_EVENT_ROUTING
        [MethodImpl(MethodImplOptions.NoInlining)]
        private void ScheduleEventRouteParallel(ref SystemState state, EntityCommandBuffer.ParallelWriter commandBuffer)
        {
            NativeArray<EventRouteJobParallel.PerThreadTempContainers> perThreadTempContainersArray = CreateNativeArray<EventRouteJobParallel.PerThreadTempContainers>(JobsUtility.MaxJobThreadCount, state.WorldUpdateAllocator, NativeArrayOptions.ClearMemory);

            state.Dependency = new EventRouteJobParallel()
            {
                EntityTypeHandle = SystemAPI.GetEntityTypeHandle(),

                EventSubscriptionRegistryComponentTypeHandle = SystemAPI.GetComponentTypeHandle<EventSubscriptionRegistryComponent>(),
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

                EventSubscriptionRegistryStorageBufferTypeHandle = SystemAPI.GetBufferTypeHandle<EventSubscriptionRegistry.StorageBufferElement>(),
                EventEntityBufferTypeHandle = SystemAPI.GetBufferTypeHandle<EventBuffer.EntityElement>(),
                EventTypeBufferTypeHandle = SystemAPI.GetBufferTypeHandle<EventBuffer.TypeElement>(),

                EventReceiveBufferLookup = SystemAPI.GetBufferLookup<EventReceiveBuffer.Element>(),

                TempAllocator = state.WorldUpdateAllocator,
                CommandBuffer = commandBuffer
            }.Schedule(routeQuery, state.Dependency);
        }
#endif

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void ScheduleEventFirerCleanup(ref SystemState state, EntityCommandBuffer.ParallelWriter commandBuffer)
        {
            state.Dependency = new EventFirerCleanupJob()
            {
                EntityTypeHandle = SystemAPI.GetEntityTypeHandle(),
                EntityLookup = SystemAPI.GetComponentLookup<CleanupComponentsAliveTag>(isReadOnly: true),
                EventEntityBufferTypeHandle = SystemAPI.GetBufferTypeHandle<EventBuffer.EntityElement>(isReadOnly: true),
                TempAllocator = state.WorldUpdateAllocator,
                CommandBuffer = commandBuffer
            }.ScheduleParallel(eventFirerCleanupQuery, state.Dependency);
        }
    }
}
