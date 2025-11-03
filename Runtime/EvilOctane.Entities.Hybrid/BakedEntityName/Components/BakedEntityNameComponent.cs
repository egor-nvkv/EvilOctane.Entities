using Unity.Collections;
using Unity.Entities;

namespace EvilOctane.Entities
{
    [BakingType]
    public struct BakedEntityNameComponent : IComponentData
    {
        public FixedString64Bytes EntityName;
    }
}
