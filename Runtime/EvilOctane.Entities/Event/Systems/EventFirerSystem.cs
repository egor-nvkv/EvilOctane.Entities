using EvilOctane.Entities.Internal;
using Unity.Burst;
using Unity.Entities;
using Unity.Jobs;

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
                .WithPresent<
                    EventSetup.FirerDeclaredEventTypeBufferElement>()
                .WithAbsent<
                    EventSubscriptionRegistry.StorageBufferElement>()
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
                .WithAbsent<
                    CleanupComponentsAliveTag>()
                .Build();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            JobHandle setupJobHandle = ScheduleEventFirerSetup(ref state);
            JobHandle subscribeUnsubscribeJobHandle = ScheduleSubscribeUnsubscribe(ref state);

            BeginInitializationEntityCommandBufferSystem.Singleton nextFrameCommandBufferSystem = SystemAPI.GetSingleton<BeginInitializationEntityCommandBufferSystem.Singleton>();

            EntityCommandBuffer nextFrameCommandBuffer = nextFrameCommandBufferSystem.CreateCommandBuffer(state.WorldUnmanaged);
            EntityCommandBuffer.ParallelWriter nextFrameParallelWriter = nextFrameCommandBuffer.AsParallelWriter();

            JobHandle routeJobHandle = ScheduleEventRoute(ref state, nextFrameCommandBuffer, subscribeUnsubscribeJobHandle);
            JobHandle cleanupJobHandle = ScheduleEventFirerCleanup(ref state, nextFrameParallelWriter, routeJobHandle);

            state.Dependency = JobHandle.CombineDependencies(setupJobHandle, cleanupJobHandle);
        }

        private JobHandle ScheduleEventFirerSetup(ref SystemState state)
        {
            EntityCommandBuffer commandBuffer = SystemAPI.GetSingleton<EndInitializationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);

            return new EventFirerSetupJob()
            {
                EntityTypeHandle = SystemAPI.GetEntityTypeHandle(),
                SetupDeclaredEventTypeBufferTypeHandle = SystemAPI.GetBufferTypeHandle<EventSetup.FirerDeclaredEventTypeBufferElement>(isReadOnly: true),
                TempAllocator = state.WorldUpdateAllocator,
                CommandBuffer = commandBuffer.AsParallelWriter()
            }.ScheduleParallel(eventFirerSetupQuery, state.Dependency);
        }

        private JobHandle ScheduleSubscribeUnsubscribe(ref SystemState state)
        {
            return new EventFirerSubscribeUnsubscribeJob()
            {
                EventSubscriptionRegistryStorageBufferTypeHandle = SystemAPI.GetBufferTypeHandle<EventSubscriptionRegistry.StorageBufferElement>(),
                EventSubscriptionRegistrySubscribeUnsubscribeBufferTypeHandle = SystemAPI.GetBufferTypeHandle<EventSubscriptionRegistry.SubscribeUnsubscribeBufferElement>(),
                ListenerSetupDeclaredEventTypeBufferLookup = SystemAPI.GetBufferLookup<EventSetup.ListenerDeclaredEventTypeBufferElement>(isReadOnly: true),
                ListenerDeclaredEventTypeBufferLookup = SystemAPI.GetBufferLookup<EventSettings.ListenerDeclaredEventTypeBufferElement>(isReadOnly: true),
                TempAllocator = state.WorldUpdateAllocator
            }.ScheduleParallel(subscribeUnsubscribeQuery, state.Dependency);
        }

        private JobHandle ScheduleEventRoute(ref SystemState state, EntityCommandBuffer commandBuffer, JobHandle dependsOn)
        {
            return new EventRouteJob()
            {
                EntityTypeHandle = SystemAPI.GetEntityTypeHandle(),

                EventSubscriptionRegistryStorageBufferTypeHandle = SystemAPI.GetBufferTypeHandle<EventSubscriptionRegistry.StorageBufferElement>(),
                EventEntityBufferTypeHandle = SystemAPI.GetBufferTypeHandle<EventBuffer.EntityElement>(),
                EventTypeBufferTypeHandle = SystemAPI.GetBufferTypeHandle<EventBuffer.TypeElement>(),

                EventReceiveBufferLookup = SystemAPI.GetBufferLookup<EventReceiveBuffer.Element>(),

                TempAllocator = state.WorldUpdateAllocator,
                CommandBuffer = commandBuffer
            }.Schedule(routeQuery, dependsOn);
        }

        private JobHandle ScheduleEventFirerCleanup(ref SystemState state, EntityCommandBuffer.ParallelWriter commandBuffer, JobHandle dependsOn)
        {
            return new EventFirerCleanupJob()
            {
                EntityTypeHandle = SystemAPI.GetEntityTypeHandle(),
                EntityLookup = SystemAPI.GetComponentLookup<CleanupComponentsAliveTag>(isReadOnly: true),
                EventEntityBufferTypeHandle = SystemAPI.GetBufferTypeHandle<EventBuffer.EntityElement>(),
                TempAllocator = state.WorldUpdateAllocator,
                CommandBuffer = commandBuffer
            }.ScheduleParallel(eventFirerCleanupQuery, dependsOn);
        }
    }
}
