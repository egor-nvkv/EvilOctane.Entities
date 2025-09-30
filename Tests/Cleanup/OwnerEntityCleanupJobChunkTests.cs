using EvilOctane.Entities;
using EvilOctane.Entities.Tests;
using NUnit.Framework;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

[assembly: RegisterGenericJobType(typeof(OwnerEntityCleanupJobChunk<OwnerEntityComponent, EntityOwnerBufferElement>))]

namespace EvilOctane.Entities.Tests
{
    public class OwnerEntityCleanupJobChunkTests
    {
        [Test]
        public void DoTest()
        {
            using World world = new("Test World", WorldFlags.None, Allocator.TempJob);

            SystemHandle systemHandle = world.CreateSystem<OwnerEntityCleanupSystem>();
            world.CreateSystemManaged<SimulationSystemGroup>().AddSystemToUpdateList(systemHandle);

            Entity ownerEntity = world.EntityManager.CreateEntity();
            Entity ownedEntity = world.EntityManager.CreateEntity();

            DynamicBuffer<EntityOwnerBufferElement> entityOwnerBuffer = world.EntityManager.AddBuffer<EntityOwnerBufferElement>(ownerEntity);
            _ = entityOwnerBuffer.Add(new EntityOwnerBufferElement() { OwnedEntity = ownedEntity });

            _ = world.EntityManager.AddComponent<CleanupComponentsAliveTag>(ownerEntity);

            _ = world.EntityManager.AddComponentData(ownedEntity, new OwnerEntityComponent() { OwnerEntity = ownerEntity });
            _ = world.EntityManager.AddComponent<CleanupComponentsAliveTag>(ownedEntity);

            // Update with Allocated Tag present
            world.Update();

            Assert.AreEqual(ownedEntity, world.EntityManager.GetBuffer<EntityOwnerBufferElement>(ownerEntity)[0].OwnedEntity);
            Assert.IsTrue(world.EntityManager.HasComponent<OwnerEntityComponent>(ownedEntity));

            // Remove Allocated Tag
            _ = world.EntityManager.RemoveComponent<CleanupComponentsAliveTag>(ownedEntity);

            // Update with Allocated Tag removed
            world.Update();

            Assert.IsTrue(world.EntityManager.GetBuffer<EntityOwnerBufferElement>(ownerEntity).IsEmpty, "EntityOwnerBuffer element was not removed.");
            Assert.IsFalse(world.EntityManager.HasComponent<OwnerEntityComponent>(ownedEntity), "OwnerEntityComponent was not removed.");
        }
    }

    public struct OwnerEntityComponent : IOwnerEntityComponentData
    {
        public Entity OwnerEntity;

        readonly Entity IOwnerEntityComponentData.OwnerEntity => OwnerEntity;
    }

    [DisableAutoCreation]
    public partial struct OwnerEntityCleanupSystem : ISystem
    {
        [BurstCompile(CompileSynchronously = true)]
        public void OnUpdate(ref SystemState state)
        {
            state.CompleteDependency();

            EntityQuery query = SystemAPI.QueryBuilder()
                .WithAll<OwnerEntityComponent>()
                .WithNone<CleanupComponentsAliveTag>()
                .Build();

            EntityCommandBuffer commandBuffer = new(state.WorldUpdateAllocator);

            new OwnerEntityCleanupJobChunk<OwnerEntityComponent, EntityOwnerBufferElement>()
            {
                EntityTypeHandle = SystemAPI.GetEntityTypeHandle(),
                OwnerEntityComponentTypeHandle = SystemAPI.GetComponentTypeHandle<OwnerEntityComponent>(isReadOnly: true),
                EntityOwnerBufferLookup = SystemAPI.GetBufferLookup<EntityOwnerBufferElement>(),
                CommandBuffer = commandBuffer
            }.Run(query);

            commandBuffer.Playback(state.EntityManager);
        }
    }
}
