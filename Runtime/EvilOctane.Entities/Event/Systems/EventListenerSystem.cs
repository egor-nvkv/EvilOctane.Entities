using EvilOctane.Entities;
using EvilOctane.Entities.Internal;
using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Entities;
using Unity.Jobs;

[assembly: RegisterGenericJobType(typeof(BufferClearJobChunk<EventReceiveBuffer.Element>))]

namespace EvilOctane.Entities
{
    [UpdateAfter(typeof(BeginInitializationEntityCommandBufferSystem))]
    [UpdateInGroup(typeof(InitializationSystemGroup), OrderFirst = true)]
    public partial struct EventListenerSystem : ISystem
    {
        private EntityQuery eventListenerSetupQuery;
        private EntityQuery receiveBufferClearQuery;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            eventListenerSetupQuery = SystemAPI.QueryBuilder()
                .WithPresent<EventSetup.ListenerDeclaredEventTypeBufferElement>()
                .WithAbsent<EventSettings.ListenerDeclaredEventTypeBufferElement>()
                .Build();

            receiveBufferClearQuery = SystemAPI.QueryBuilder()
                .WithPresentRW<EventReceiveBuffer.Element>()
                .Build();

            receiveBufferClearQuery.SetChangedVersionFilter<EventReceiveBuffer.Element>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if (!eventListenerSetupQuery.IsEmpty)
            {
                ScheduleEventListenerSetup(ref state);
            }

            if (!receiveBufferClearQuery.IsEmpty)
            {
                ScheduleReceiveBufferClear(ref state);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void ScheduleEventListenerSetup(ref SystemState state)
        {
            EntityCommandBuffer commandBuffer = SystemAPI.GetSingleton<EndInitializationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);

            state.Dependency = new EventListenerSetupJob()
            {
                EntityTypeHandle = SystemAPI.GetEntityTypeHandle(),
                SetupDeclaredEventTypeBufferTypeHandle = SystemAPI.GetBufferTypeHandle<EventSetup.ListenerDeclaredEventTypeBufferElement>(isReadOnly: true),
                TempAllocator = state.WorldUpdateAllocator,
                CommandBuffer = commandBuffer.AsParallelWriter()
            }.ScheduleParallel(eventListenerSetupQuery, state.Dependency);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ScheduleReceiveBufferClear(ref SystemState state)
        {
            state.Dependency = new BufferClearJobChunk<EventReceiveBuffer.Element>()
            {
                BufferTypeHandle = SystemAPI.GetBufferTypeHandle<EventReceiveBuffer.Element>()
            }.ScheduleParallel(receiveBufferClearQuery, state.Dependency);
        }
    }
}
