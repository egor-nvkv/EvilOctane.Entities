using EvilOctane.Entities;
using EvilOctane.Entities.Internal;
using Unity.Burst;
using Unity.Entities;
using Unity.Jobs;
using static Unity.Entities.SystemAPI;

[assembly: RegisterGenericJobType(typeof(BufferClearJobChunk<EventListener.EventReceiveBuffer.Element>))]

namespace EvilOctane.Entities
{
    [UpdateAfter(typeof(BeginInitializationEntityCommandBufferSystem))]
    [UpdateInGroup(typeof(InitializationSystemGroup), OrderFirst = true)]
    public partial struct EventListenerSystem : ISystem
    {
        private EntityQuery setupQuery;
        private EntityQuery clearReceiveBufferQuery;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            setupQuery = QueryBuilder()
                // Setup
                .WithPresent<
                    EventListener.EventDeclarationBuffer.StableTypeElement>()
                .WithAbsent<
                    EventListener.EventDeclarationBuffer.TypeElement>()
                // Subscribe Auto
                .AddAdditionalQuery()
                .WithPresentRW<
                    EventListener.EventSubscribeBuffer.SubscribeAutoElement>()
                .Build();

            clearReceiveBufferQuery = QueryBuilder()
                .WithPresentRW<
                    EventListener.EventReceiveBuffer.Element>()
                .Build();

            clearReceiveBufferQuery.SetChangedVersionFilter<EventListener.EventReceiveBuffer.Element>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            EntityCommandBuffer commandBuffer = GetSingleton<EndInitializationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);

            JobHandle setupJobHandle = new EventListenerSetupJob()
            {
                EntityTypeHandle = GetEntityTypeHandle(),

                FirerStableTypeBufferTypeHandle = GetBufferLookup<EventFirer.EventDeclarationBuffer.StableTypeElement>(isReadOnly: true),
                FirerEventCommandBufferLookup = GetBufferLookup<EventFirer.EventSubscriptionRegistry.CommandBufferElement>(isReadOnly: true),

                ListenerStableTypeBufferTypeHandle = GetBufferTypeHandle<EventListener.EventDeclarationBuffer.StableTypeElement>(isReadOnly: true),
                ListenerSubscribeAutoBufferTypeHandle = GetBufferTypeHandle<EventListener.EventSubscribeBuffer.SubscribeAutoElement>(),

                TempAllocator = state.WorldUpdateAllocator,
                CommandBuffer = commandBuffer.AsParallelWriter()
            }.ScheduleParallel(setupQuery, state.Dependency);

            JobHandle clearJobHandle = new BufferClearJobChunk<EventListener.EventReceiveBuffer.Element>()
            {
                BufferTypeHandle = GetBufferTypeHandle<EventListener.EventReceiveBuffer.Element>()
            }.ScheduleParallel(clearReceiveBufferQuery, state.Dependency);

            state.Dependency = JobHandle.CombineDependencies(setupJobHandle, clearJobHandle);
        }
    }
}
