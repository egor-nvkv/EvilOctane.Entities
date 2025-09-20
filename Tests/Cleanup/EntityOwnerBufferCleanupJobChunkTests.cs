using EvilOctane.Entities;
using EvilOctane.Entities.Tests;
using NUnit.Framework;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

[assembly: RegisterGenericJobType(typeof(EntityOwnerBufferCleanupJobChunk<EntityOwnerBufferElement, CleanupComponentAllocatedTag>))]

namespace EvilOctane.Entities.Tests
{
    public class EntityOwnerBufferCleanupJobChunkTests
    {
        [Test]
        public void DoTest()
        {
            using World world = new("Test World", WorldFlags.None, Allocator.TempJob);

            SystemHandle systemHandle = world.CreateSystem<EntityOwnerBufferCleanupSystem>();
            world.CreateSystemManaged<SimulationSystemGroup>().AddSystemToUpdateList(systemHandle);

            Entity ownerEntity = world.EntityManager.CreateEntity();
            Entity ownedEntity = world.EntityManager.CreateEntity();

            DynamicBuffer<EntityOwnerBufferElement> entityOwnerBuffer = world.EntityManager.AddBuffer<EntityOwnerBufferElement>(ownerEntity);
            _ = entityOwnerBuffer.Add(new EntityOwnerBufferElement() { OwnedEntity = ownedEntity });

            _ = world.EntityManager.AddComponent<CleanupComponentAllocatedTag>(ownerEntity);

            // Update with Allocated Tag present
            world.Update();

            Assert.IsTrue(world.EntityManager.HasBuffer<EntityOwnerBufferElement>(ownerEntity));
            Assert.IsTrue(world.EntityManager.Exists(ownedEntity));

            // Remove Allocated Tag
            _ = world.EntityManager.RemoveComponent<CleanupComponentAllocatedTag>(ownerEntity);

            // Update with Allocated Tag removed
            world.Update();

            Assert.IsFalse(world.EntityManager.HasBuffer<EntityOwnerBufferElement>(ownerEntity), "EntityOwnerBuffer was not removed.");
            Assert.IsFalse(world.EntityManager.Exists(ownedEntity), "OwnedEntity was not destroyed.");
        }
    }

    public struct EntityOwnerBufferElement : IEntityOwnerBufferElementData
    {
        public Entity OwnedEntity;
    }

    [DisableAutoCreation]
    public partial struct EntityOwnerBufferCleanupSystem : ISystem
    {
        [BurstCompile(CompileSynchronously = true)]
        public void OnUpdate(ref SystemState state)
        {
            state.CompleteDependency();

            EntityQuery query = SystemAPI.QueryBuilder()
                .WithAllRW<EntityOwnerBufferElement>()
                .WithNone<CleanupComponentAllocatedTag>()
                .Build();

            EntityCommandBuffer commandBuffer = new(state.WorldUpdateAllocator);

            new EntityOwnerBufferCleanupJobChunk<EntityOwnerBufferElement, CleanupComponentAllocatedTag>()
            {
                EntityTypeHandle = SystemAPI.GetEntityTypeHandle(),
                EntityLookup = SystemAPI.GetComponentLookup<CleanupComponentAllocatedTag>(isReadOnly: true),
                EntityOwnerBufferTypeHandle = SystemAPI.GetBufferTypeHandle<EntityOwnerBufferElement>(),
                TempAllocator = state.WorldUpdateAllocator,
                CommandBuffer = commandBuffer
            }.Run(query);

            commandBuffer.Playback(state.EntityManager);
        }
    }
}
