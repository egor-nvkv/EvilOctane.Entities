using EvilOctane.Entities;
using EvilOctane.Entities.Internal;
using Unity.Burst;
using Unity.Entities;
using Unity.Jobs;

[assembly: RegisterGenericJobType(typeof(BufferClearJobChunk<EventFirer.EventBuffer.TypeElement>))]

namespace EvilOctane.Entities
{
    [UpdateAfter(typeof(BeginInitializationEntityCommandBufferSystem))]
    [UpdateAfter(typeof(EventListenerSystem))]
    [UpdateInGroup(typeof(InitializationSystemGroup), OrderFirst = true)]
    public partial struct EventFirerSystem : ISystem
    {
        private EntityQuery setupQuery;
        private EntityQuery processCommandsQuery;
        private EntityQuery routeEventsQuery;
        private EntityQuery clearEntityBufferQuery;
        private EntityQuery clearTypeBufferQuery;
        private EntityQuery cleanupQuery;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            setupQuery = SystemAPI.QueryBuilder()
                .WithPresent<
                    EventFirer.EventDeclarationBuffer.StableTypeElement>()
                .WithAbsent<
                    EventFirerInternal.EventSubscriptionRegistry.Storage>()
                .Build();

            processCommandsQuery = SystemAPI.QueryBuilder()
                .WithPresentRW<
                    EventFirerInternal.EventSubscriptionRegistry.Storage,
                    EventFirer.EventSubscriptionRegistry.CommandBufferElement>()
                .Build();

            processCommandsQuery.SetChangedVersionFilter<EventFirer.EventSubscriptionRegistry.CommandBufferElement>();

            // Required for instantiated Firers with immediate subscriptions to work
            processCommandsQuery.AddOrderVersionFilter();

            routeEventsQuery = SystemAPI.QueryBuilder()
                .WithPresentRW<
                    EventFirerInternal.EventSubscriptionRegistry.Storage>()
                .WithPresent<
                    EventFirer.EventBuffer.EntityElement,
                    EventFirer.EventBuffer.TypeElement>()

                // Missing the following:
                // .WithPresent<EventFirer.IsAliveTag>()
                // so that Events from destroyed Entities also get routed

                .Build();

            routeEventsQuery.SetChangedVersionFilter<EventFirer.EventBuffer.EntityElement>();

            clearEntityBufferQuery = SystemAPI.QueryBuilder()
                .WithPresentRW<
                    EventFirer.EventBuffer.EntityElement>()
                .Build();

            clearEntityBufferQuery.SetChangedVersionFilter<EventFirer.EventBuffer.EntityElement>();

            clearTypeBufferQuery = SystemAPI.QueryBuilder()
                .WithPresentRW<
                    EventFirer.EventBuffer.TypeElement>()
                .WithPresent<
                    EventFirer.IsAliveTag>()
                .Build();

            clearTypeBufferQuery.SetChangedVersionFilter<EventFirer.EventBuffer.TypeElement>();

            cleanupQuery = SystemAPI.QueryBuilder()
                .WithAny<
                    EventFirerInternal.EventSubscriptionRegistry.Storage,
                    EventFirer.EventSubscriptionRegistry.CommandBufferElement,
                    EventFirer.EventBuffer.EntityElement,
                    EventFirer.EventBuffer.TypeElement>()
                .WithAbsent<
                    EventFirer.IsAliveTag>()
                .Build();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            JobHandle setupJobHandle = ScheduleSetup(ref state);
            JobHandle processCommandsJobHandle = ScheduleProcessCommands(ref state);

            BeginInitializationEntityCommandBufferSystem.Singleton nextFrameCommandBufferSystem = SystemAPI.GetSingleton<BeginInitializationEntityCommandBufferSystem.Singleton>();

            EntityCommandBuffer nextFrameCommandBuffer = nextFrameCommandBufferSystem.CreateCommandBuffer(state.WorldUnmanaged);
            EntityCommandBuffer.ParallelWriter nextFrameParallelWriter = nextFrameCommandBuffer.AsParallelWriter();

            JobHandle routeEventsJobHandle = ScheduleRouteEvents(ref state, nextFrameCommandBuffer, processCommandsJobHandle);
            JobHandle clearEntityBufferJobHandle = ScheduleClearEntityBuffer(ref state, nextFrameParallelWriter, routeEventsJobHandle);
            JobHandle clearTypeBufferJobHandle = ScheduleClearTypeBuffer(ref state, routeEventsJobHandle);
            JobHandle cleanupJobHandle = ScheduleCleanup(ref state, nextFrameParallelWriter, clearEntityBufferJobHandle);

            state.Dependency = JobHandle.CombineDependencies(
                setupJobHandle,
                clearTypeBufferJobHandle,
                cleanupJobHandle);
        }

        private JobHandle ScheduleSetup(ref SystemState state)
        {
            EntityCommandBuffer commandBuffer = SystemAPI.GetSingleton<EndInitializationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);

            return new EventFirerSetupJob()
            {
                EntityTypeHandle = SystemAPI.GetEntityTypeHandle(),
                EventStableTypeBufferTypeHandle = SystemAPI.GetBufferTypeHandle<EventFirer.EventDeclarationBuffer.StableTypeElement>(isReadOnly: true),
                TempAllocator = state.WorldUpdateAllocator,
                CommandBuffer = commandBuffer.AsParallelWriter()
            }.ScheduleParallel(setupQuery, state.Dependency);
        }

        private JobHandle ScheduleProcessCommands(ref SystemState state)
        {
            return new EventFirerProcessCommandsJob()
            {
                SubscriptionRegistryStorageTypeHandle = SystemAPI.GetBufferTypeHandle<EventFirerInternal.EventSubscriptionRegistry.Storage>(),
                SubscriptionRegistryCommandBufferTypeHandle = SystemAPI.GetBufferTypeHandle<EventFirer.EventSubscriptionRegistry.CommandBufferElement>(),
                ListenerEventTypeBufferLookup = SystemAPI.GetBufferLookup<EventListener.EventDeclarationBuffer.TypeElement>(isReadOnly: true),
                ListenerEventStableTypeBufferLookup = SystemAPI.GetBufferLookup<EventListener.EventDeclarationBuffer.StableTypeElement>(isReadOnly: true),
                TempAllocator = state.WorldUpdateAllocator
            }.ScheduleParallel(processCommandsQuery, state.Dependency);
        }

        private JobHandle ScheduleRouteEvents(ref SystemState state, EntityCommandBuffer commandBuffer, JobHandle dependsOn)
        {
            return new EventFirerRouteEventsJob()
            {
                EntityTypeHandle = SystemAPI.GetEntityTypeHandle(),

                SubscriptionRegistryStorageTypeHandle = SystemAPI.GetBufferTypeHandle<EventFirerInternal.EventSubscriptionRegistry.Storage>(),
                EventEntityBufferTypeHandle = SystemAPI.GetBufferTypeHandle<EventFirer.EventBuffer.EntityElement>(isReadOnly: true),
                EventTypeBufferTypeHandle = SystemAPI.GetBufferTypeHandle<EventFirer.EventBuffer.TypeElement>(isReadOnly: true),

                EventReceiveBufferLookup = SystemAPI.GetBufferLookup<EventListener.EventReceiveBuffer.Element>(),

                TempAllocator = state.WorldUpdateAllocator,
                CommandBuffer = commandBuffer
            }.Schedule(routeEventsQuery, dependsOn);
        }

        private JobHandle ScheduleClearEntityBuffer(ref SystemState state, EntityCommandBuffer.ParallelWriter commandBuffer, JobHandle dependsOn)
        {
            return new EventFirerClearEntityBufferJob()
            {
                EventEntityBufferTypeHandle = SystemAPI.GetBufferTypeHandle<EventFirer.EventBuffer.EntityElement>(),
                TempAllocator = state.WorldUpdateAllocator,
                CommandBuffer = commandBuffer
            }.ScheduleParallel(clearEntityBufferQuery, dependsOn);
        }

        private JobHandle ScheduleClearTypeBuffer(ref SystemState state, JobHandle dependsOn)
        {
            return new BufferClearJobChunk<EventFirer.EventBuffer.TypeElement>()
            {
                BufferTypeHandle = SystemAPI.GetBufferTypeHandle<EventFirer.EventBuffer.TypeElement>()
            }.ScheduleParallel(clearTypeBufferQuery, dependsOn);
        }

        private JobHandle ScheduleCleanup(ref SystemState state, EntityCommandBuffer.ParallelWriter commandBuffer, JobHandle dependsOn)
        {
            return new EventFirerCleanupJob()
            {
                EntityTypeHandle = SystemAPI.GetEntityTypeHandle(),
                CommandBuffer = commandBuffer
            }.ScheduleParallel(cleanupQuery, dependsOn);
        }
    }
}
