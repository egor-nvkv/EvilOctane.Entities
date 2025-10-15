using Unity.Burst;
using Unity.Entities;
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
                    EventFirerTag,
                    DeclaredEventTypeBufferElement>()
                .Build();

            listenerConvertToStableTypeQuery = QueryBuilder()
                .WithAll<
                    EventListenerTag,
                    DeclaredEventTypeBufferElement>()
                .Build();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            state.Dependency.Complete();

            EntityCommandBuffer commandBuffer = new(state.WorldUpdateAllocator);

            new EventFirerBakeJob()
            {
                EntityTypeHandle = GetEntityTypeHandle(),
                DeclaredEventTypeBufferTypeHandle = GetBufferTypeHandle<DeclaredEventTypeBufferElement>(isReadOnly: true),
                TempAllocator = state.WorldUpdateAllocator,
                CommandBuffer = commandBuffer.AsParallelWriter()
            }.Run(firerConvertToStableTypeQuery);

            new EventListenerBakeJob()
            {
                EntityTypeHandle = GetEntityTypeHandle(),
                DeclaredEventTypeBufferTypeHandle = GetBufferTypeHandle<DeclaredEventTypeBufferElement>(isReadOnly: true),
                TempAllocator = state.WorldUpdateAllocator,
                CommandBuffer = commandBuffer.AsParallelWriter()
            }.Run(listenerConvertToStableTypeQuery);

            commandBuffer.Playback(state.EntityManager);
        }
    }
}
