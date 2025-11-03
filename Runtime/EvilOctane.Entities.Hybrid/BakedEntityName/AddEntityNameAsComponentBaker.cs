using Unity.Collections;
using Unity.Entities;
using static System.Runtime.CompilerServices.Unsafe;

namespace EvilOctane.Entities.Internal
{
    public class AddEntityNameAsComponentBaker : Baker<AddEntityNameAsComponentAuthoring>
    {
        public override void Bake(AddEntityNameAsComponentAuthoring authoring)
        {
            Entity entity = GetEntityWithoutDependency();

            SkipInit(out BakedEntityNameComponent bakedEntityName);
            _ = bakedEntityName.EntityName.CopyFromTruncated(authoring.name);

            AddComponent(entity, bakedEntityName);
        }
    }
}
