using Unity.Collections;
using Unity.Entities;

namespace EvilOctane.Entities
{
    [BakingType]
    public struct BakedEntityNameComponent : IComponentData
    {
        public FixedString64Bytes EntityName;

        /// <summary>
        /// Will set <see cref="BakedEntityNameComponent"/> as <see cref="Entity"/>'s name.
        /// </summary>
        [BakingType]
        public struct PropagateTag : IComponentData { }
    }
}
