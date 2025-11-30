using Unity.Collections;
using Unity.Entities;
using static System.Runtime.CompilerServices.Unsafe;

namespace EvilOctane.Entities.Internal
{
    public class AddNameAsComponentBaker : Baker<AddNameAsComponentAuthoring>
    {
        public override void Bake(AddNameAsComponentAuthoring authoring)
        {
            Entity entity = GetEntityWithoutDependency();

            SkipInit(out BakedEntityNameComponent bakedEntityName);
            _ = bakedEntityName.EntityName.CopyFromTruncated(GetName());

            AddComponent(entity, bakedEntityName);
        }
    }
}
