using Unity.Entities;

namespace EvilOctane.Entities
{
    /// <summary>
    /// Will set <see cref="BakedEntityNameComponent"/> as <see cref="Entity"/>'s name.
    /// </summary>
    [BakingType]
    public struct PropagateBakedEntityNameTag : IComponentData
    {
    }
}
