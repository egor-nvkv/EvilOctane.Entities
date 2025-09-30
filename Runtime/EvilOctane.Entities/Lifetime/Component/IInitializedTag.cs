using Unity.Entities;

namespace EvilOctane.Entities
{
    /// <summary>
    /// An interface for a tag component to signify that the <see cref="Entity"/> has been initialized.
    /// </summary>
    public interface IInitializedTag : IComponentData
    {
    }
}
