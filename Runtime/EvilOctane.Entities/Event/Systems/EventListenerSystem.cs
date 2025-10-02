using EvilOctane.Entities;
using EvilOctane.Entities.Internal;
using Unity.Burst;
using Unity.Entities;
using Unity.Jobs;

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
            setupQuery = SystemAPI.QueryBuilder()
                .WithPresent<
                    EventListener.EventDeclarationBuffer.StableTypeElement>()
                .WithAbsent<
                    EventListener.EventDeclarationBuffer.TypeElement>()
                .Build();

            clearReceiveBufferQuery = SystemAPI.QueryBuilder()
                .WithPresentRW<
                    EventListener.EventReceiveBuffer.Element>()
                .Build();

            clearReceiveBufferQuery.SetChangedVersionFilter<EventListener.EventReceiveBuffer.Element>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            EntityCommandBuffer commandBuffer = SystemAPI.GetSingleton<EndInitializationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);

            JobHandle setupJobHandle = new EventListenerSetupJob()
            {
                EntityTypeHandle = SystemAPI.GetEntityTypeHandle(),
                EventStableTypeBufferTypeHandle = SystemAPI.GetBufferTypeHandle<EventListener.EventDeclarationBuffer.StableTypeElement>(isReadOnly: true),
                TempAllocator = state.WorldUpdateAllocator,
                CommandBuffer = commandBuffer.AsParallelWriter()
            }.ScheduleParallel(setupQuery, state.Dependency);

            JobHandle clearJobHandle = new BufferClearJobChunk<EventListener.EventReceiveBuffer.Element>()
            {
                BufferTypeHandle = SystemAPI.GetBufferTypeHandle<EventListener.EventReceiveBuffer.Element>()
            }.ScheduleParallel(clearReceiveBufferQuery, state.Dependency);

            state.Dependency = JobHandle.CombineDependencies(setupJobHandle, clearJobHandle);
        }
    }
}
