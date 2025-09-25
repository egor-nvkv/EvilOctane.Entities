using EvilOctane.Entities;
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
        private EntityQuery receiveBufferClearQuery;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            receiveBufferClearQuery = SystemAPI.QueryBuilder()
                .WithPresentRW<EventReceiveBuffer.Element>()
                .Build();

            receiveBufferClearQuery.SetChangedVersionFilter<EventReceiveBuffer.Element>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if (!receiveBufferClearQuery.IsEmpty)
            {
                ScheduleReceiveBufferClear(ref state);
            }
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
