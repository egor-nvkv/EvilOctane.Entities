using EvilOctane.Entities;
using EvilOctane.Entities.Tests;
using NUnit.Framework;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

[assembly: RegisterGenericJobType(typeof(OwnedEntityBufferCleanupJob<OwnedEntityBufferElement, IsAliveTag>))]
[assembly: RegisterGenericJobType(typeof(OwnedEntityBufferCleanupJobParallel<OwnedEntityBufferElement, IsAliveTag>))]

namespace EvilOctane.Entities.Tests
{
    public class OwnedEntityBufferCleanupJobTests
    {
        [Test]
        public void DoTest([Values(true, false)] bool runParallel)
        {
            using World world = new("Test World", WorldFlags.None, Allocator.Persistent);

            SystemHandle systemHandle = world.CreateSystem<OwnedEntityCleanupSystem>();
            world.Unmanaged.GetUnsafeSystemRef<OwnedEntityCleanupSystem>(systemHandle).RunParallel = runParallel;

            world.CreateSystemManaged<SimulationSystemGroup>().AddSystemToUpdateList(systemHandle);

            Entity ownerEntity = world.EntityManager.CreateEntity();
            Entity ownedEntity = world.EntityManager.CreateEntity();

            DynamicBuffer<OwnedEntityBufferElement> ownedEntityBuffer = world.EntityManager.AddBuffer<OwnedEntityBufferElement>(ownerEntity);
            _ = ownedEntityBuffer.Add(new OwnedEntityBufferElement() { OwnedEntity = ownedEntity });

            _ = world.EntityManager.AddComponent<IsAliveTag>(ownerEntity);

            // Update with Allocated Tag present
            world.Update();

            Assert.IsTrue(world.EntityManager.HasBuffer<OwnedEntityBufferElement>(ownerEntity));
            Assert.IsTrue(world.EntityManager.Exists(ownedEntity));

            // Remove Allocated Tag
            _ = world.EntityManager.RemoveComponent<IsAliveTag>(ownerEntity);

            // Update with Allocated Tag removed
            world.Update();

            Assert.IsFalse(world.EntityManager.HasBuffer<OwnedEntityBufferElement>(ownerEntity), "OwnedEntityBuffer was not removed.");
            Assert.IsFalse(world.EntityManager.Exists(ownedEntity), "OwnedEntity was not destroyed.");
        }
    }

    public struct OwnedEntityBufferElement : IOwnedEntityBufferElementData
    {
        public Entity OwnedEntity;

        readonly Entity IOwnedEntityBufferElementData.OwnedEntity => OwnedEntity;
    }

    [DisableAutoCreation]
    public partial struct OwnedEntityCleanupSystem : ISystem
    {
        public bool RunParallel;

        [BurstCompile(CompileSynchronously = true)]
        public void OnUpdate(ref SystemState state)
        {
            state.CompleteDependency();

            EntityQuery query = SystemAPI.QueryBuilder()
                .WithAllRW<OwnedEntityBufferElement>()
                .WithNone<IsAliveTag>()
                .Build();

            EntityCommandBuffer commandBuffer = new(state.WorldUpdateAllocator);

            if (RunParallel)
            {
                new OwnedEntityBufferCleanupJobParallel<OwnedEntityBufferElement, IsAliveTag>()
                {
                    EntityTypeHandle = SystemAPI.GetEntityTypeHandle(),
                    EntityLookup = SystemAPI.GetComponentLookup<IsAliveTag>(isReadOnly: true),
                    OwnedEntityBufferTypeHandle = SystemAPI.GetBufferTypeHandle<OwnedEntityBufferElement>(),
                    TempAllocator = state.WorldUpdateAllocator,
                    CommandBuffer = commandBuffer.AsParallelWriter()
                }.ScheduleParallel(query, new JobHandle()).Complete();
            }
            else
            {
                new OwnedEntityBufferCleanupJob<OwnedEntityBufferElement, IsAliveTag>()
                {
                    EntityTypeHandle = SystemAPI.GetEntityTypeHandle(),
                    EntityLookup = SystemAPI.GetComponentLookup<IsAliveTag>(isReadOnly: true),
                    OwnedEntityBufferTypeHandle = SystemAPI.GetBufferTypeHandle<OwnedEntityBufferElement>(),
                    TempAllocator = state.WorldUpdateAllocator,
                    CommandBuffer = commandBuffer
                }.Run(query);
            }

            commandBuffer.Playback(state.EntityManager);
        }
    }
}
