using Unity.Burst;
using Unity.Entities;
using static Unity.Entities.SystemAPI;

namespace EvilOctane.Entities.Internal
{
    [UpdateInGroup(typeof(PostBakingSystemGroup))]
    [WorldSystemFilter(WorldSystemFilterFlags.BakingSystem)]
    public partial struct BakedEntityNameSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            foreach ((
                RefRO<BakedEntityNameComponent> bakedEntityName,
                Entity entity) in

                Query<
                    RefRO<BakedEntityNameComponent>>()
                .WithAll<
                    PropagateBakedEntityNameTag>()
                .WithEntityAccess()
                .WithOptions(EntityQueryOptions.IncludePrefab | EntityQueryOptions.IncludeDisabledEntities))
            {
                // Set entity name
                state.EntityManager.SetName(entity, bakedEntityName.ValueRO.EntityName);
            }
        }
    }
}
