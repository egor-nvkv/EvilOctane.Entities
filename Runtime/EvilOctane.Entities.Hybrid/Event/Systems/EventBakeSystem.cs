using Unity.Burst;
using Unity.Entities;
using Unity.Jobs;
using static Unity.Entities.SystemAPI;

namespace EvilOctane.Entities
{
    [BurstCompile]
    [WorldSystemFilter(WorldSystemFilterFlags.BakingSystem)]
    internal partial struct EventBakeSystem : ISystem
    {
        private EntityQuery firerConvertToStableTypeQuery;
        private EntityQuery listenerConvertToStableTypeQuery;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            firerConvertToStableTypeQuery = QueryBuilder()
                .WithAll<
                    EventFirerDeclaredEventTypeBufferElement>()
                .Build();

            listenerConvertToStableTypeQuery = QueryBuilder()
                .WithAll<
                    EventListenerDeclaredEventTypeBufferElement>()
                .Build();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            EntityCommandBuffer commandBuffer = new(state.WorldUpdateAllocator);
            EntityCommandBuffer.ParallelWriter parallelWriter = commandBuffer.AsParallelWriter();

            JobHandle jobHandle = new EventFirerBakeJob()
            {
                EntityTypeHandle = GetEntityTypeHandle(),
                EventTypeBufferTypeHandle = GetBufferTypeHandle<EventFirerDeclaredEventTypeBufferElement>(isReadOnly: true),
                TempAllocator = state.WorldUpdateAllocator,
                CommandBuffer = parallelWriter
            }.ScheduleParallel(firerConvertToStableTypeQuery, state.Dependency);

            jobHandle = new EventListenerBakeJob()
            {
                EntityTypeHandle = GetEntityTypeHandle(),
                EventTypeBufferTypeHandle = GetBufferTypeHandle<EventListenerDeclaredEventTypeBufferElement>(isReadOnly: true),
                TempAllocator = state.WorldUpdateAllocator,
                CommandBuffer = parallelWriter
            }.ScheduleParallel(listenerConvertToStableTypeQuery, jobHandle);

            jobHandle.Complete();
            commandBuffer.Playback(state.EntityManager);
        }
    }
}
