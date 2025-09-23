using EvilOctane.Entities;
using EvilOctane.Entities.Internal;
using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Jobs.LowLevel.Unsafe;
using static Unity.Collections.CollectionHelper;
using EventList = Unity.Collections.LowLevel.Unsafe.UnsafeList<EvilOctane.Entities.EventReceiveBuffer.Element>;

[assembly: RegisterGenericJobType(typeof(BufferClearJobChunk<EventReceiveBuffer.Element>))]

namespace EvilOctane.Entities
{
    [UpdateAfter(typeof(BeginInitializationEntityCommandBufferSystem))]
    [UpdateInGroup(typeof(InitializationSystemGroup), OrderFirst = true)]
    public partial struct EventSystem : ISystem
    {
        private EntityQuery changeSubscriptionStatusQuery;
        private EntityQuery receiveBufferClearQuery;
        private EntityQuery routeQuery;
        private EntityQuery eventBufferClearQuery;
        private EntityQuery eventBufferCleanupQuery;

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
            receiveBufferClearQuery = SystemAPI.QueryBuilder()
                .WithPresentRW<EventReceiveBuffer.Element>()
                .Build();

            receiveBufferClearQuery.SetChangedVersionFilter<EventReceiveBuffer.Element>();

            // 2
            routeQuery = SystemAPI.QueryBuilder()
                .WithPresent<
                    EventBuffer.EntityElement,
                    EventBuffer.TypeElement>()
                .WithPresentRW<EventSubscriptionRegistry.Component>()

                // No Allocated Tag so destroyed Entities can fire Events

                .Build();

            routeQuery.SetChangedVersionFilter<EventBuffer.EntityElement>();

            // 3
            eventBufferClearQuery = SystemAPI.QueryBuilder()
                .WithPresentRW<
                    EventBuffer.EntityElement,
                    EventBuffer.TypeElement>()
                .WithPresent<CleanupComponentAllocatedTag>()
                .Build();

            eventBufferClearQuery.SetChangedVersionFilter<EventBuffer.EntityElement>();

            // 4
            eventBufferCleanupQuery = SystemAPI.QueryBuilder()
                .WithAny<
                    EventBuffer.EntityElement,
                    EventBuffer.TypeElement,
                    EventSubscriptionRegistry.Component>()
                .WithAbsent<CleanupComponentAllocatedTag>()
                .Build();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            bool changeSubscriptionStatusQueryIsEmpty = changeSubscriptionStatusQuery.IsEmpty;
            bool receiveBufferClearQueryIsEmpty = receiveBufferClearQuery.IsEmpty;
            bool routeQueryIsEmpty = routeQuery.IsEmpty;
            bool eventBufferClearQueryIsEmpty = eventBufferClearQuery.IsEmpty;
            bool eventBufferCleanupQueryIsEmpty = eventBufferCleanupQuery.IsEmptyIgnoreFilter;

            bool allQueriesEmpty =
                changeSubscriptionStatusQueryIsEmpty &
                receiveBufferClearQueryIsEmpty &
                routeQueryIsEmpty &
                eventBufferClearQueryIsEmpty &
                eventBufferCleanupQueryIsEmpty;

            if (allQueriesEmpty)
            {
                // Skip Update
                return;
            }

            if (!changeSubscriptionStatusQueryIsEmpty)
            {
                ScheduleChangeSubscriptionStatus(ref state);
            }

            if (!receiveBufferClearQueryIsEmpty)
            {
                ScheduleReceiveBufferClear(ref state);
            }

            if (!routeQueryIsEmpty)
            {
                ScheduleEventRoute(ref state);
            }

            if (eventBufferClearQueryIsEmpty & eventBufferCleanupQueryIsEmpty)
            {
                // Skip creating Command Buffer
                return;
            }

            BeginInitializationEntityCommandBufferSystem.Singleton nextFrameCommandBufferSystem = SystemAPI.GetSingleton<BeginInitializationEntityCommandBufferSystem.Singleton>();

            EntityCommandBuffer commandBuffer = nextFrameCommandBufferSystem.CreateCommandBuffer(state.WorldUnmanaged);
            EntityCommandBuffer.ParallelWriter parallelWriter = commandBuffer.AsParallelWriter();

            if (!eventBufferClearQueryIsEmpty)
            {
                ScheduleEventBufferClear(ref state, parallelWriter);
            }

            if (!eventBufferCleanupQueryIsEmpty)
            {
                ScheduleEventBufferCleanup(ref state, parallelWriter);
            }
        }

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

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void ScheduleReceiveBufferClear(ref SystemState state)
        {
            state.Dependency = new BufferClearJobChunk<EventReceiveBuffer.Element>()
            {
                BufferTypeHandle = SystemAPI.GetBufferTypeHandle<EventReceiveBuffer.Element>()
            }.ScheduleParallel(receiveBufferClearQuery, state.Dependency);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void ScheduleEventRoute(ref SystemState state)
        {
            NativeArray<UnsafeHashMap<Entity, EventList>> listenerEventListMapPerThreadArray = CreateNativeArray<UnsafeHashMap<Entity, EventList>>(JobsUtility.MaxJobThreadCount, state.WorldUpdateAllocator, NativeArrayOptions.ClearMemory);
            NativeArray<UnsafeList<EventListenerEventListPair>> listenerEventListsPerThreadArray = CreateNativeArray<UnsafeList<EventListenerEventListPair>>(JobsUtility.MaxJobThreadCount, state.WorldUpdateAllocator, NativeArrayOptions.ClearMemory);

            state.Dependency = new EventRouteJob()
            {
                EntityTypeHandle = SystemAPI.GetEntityTypeHandle(),

                EventEntityBufferTypeHandle = SystemAPI.GetBufferTypeHandle<EventBuffer.EntityElement>(isReadOnly: true),
                EventTypeBufferTypeHandle = SystemAPI.GetBufferTypeHandle<EventBuffer.TypeElement>(isReadOnly: true),
                EventSubscriptionRegistryComponentTypeHandle = SystemAPI.GetComponentTypeHandle<EventSubscriptionRegistry.Component>(),

                EventReceiveBufferLookup = SystemAPI.GetBufferLookup<EventReceiveBuffer.Element>(),
                EventReceiveBufferLockComponentLookup = SystemAPI.GetComponentLookup<EventReceiveBuffer.LockComponent>(),

                ListenerEventListMapPerThreadArray = listenerEventListMapPerThreadArray,
                ListenerEventListsPerThreadArray = listenerEventListsPerThreadArray,

                TempAllocator = state.WorldUpdateAllocator
            }.ScheduleParallel(routeQuery, state.Dependency);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void ScheduleEventBufferClear(ref SystemState state, EntityCommandBuffer.ParallelWriter commandBuffer)
        {
            state.Dependency = new EventBufferClearJob()
            {
                EventEntityBufferTypeHandle = SystemAPI.GetBufferTypeHandle<EventBuffer.EntityElement>(),
                EventTypeBufferTypeHandle = SystemAPI.GetBufferTypeHandle<EventBuffer.TypeElement>(),
                TempAllocator = state.WorldUpdateAllocator,
                CommandBuffer = commandBuffer
            }.ScheduleParallel(eventBufferClearQuery, state.Dependency);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void ScheduleEventBufferCleanup(ref SystemState state, EntityCommandBuffer.ParallelWriter commandBuffer)
        {
            state.Dependency = new EventBufferCleanupJob()
            {
                EntityTypeHandle = SystemAPI.GetEntityTypeHandle(),
                EntityLookup = SystemAPI.GetComponentLookup<CleanupComponentAllocatedTag>(isReadOnly: true),
                EventEntityBufferTypeHandle = SystemAPI.GetBufferTypeHandle<EventBuffer.EntityElement>(isReadOnly: true),
                EventSubscriptionRegistryComponentTypeHandle = SystemAPI.GetComponentTypeHandle<EventSubscriptionRegistry.Component>(isReadOnly: true),
                TempAllocator = state.WorldUpdateAllocator,
                CommandBuffer = commandBuffer
            }.ScheduleParallel(eventBufferCleanupQuery, state.Dependency);
        }
    }
}
