using Unity.Entities;

namespace EvilOctane.Entities
{
    /// <summary>
    /// An interface for a tag component to signify the presence of
    /// <see cref="ICleanupComponentData"/> or <see cref="ICleanupBufferElementData"/>.
    /// </summary>
    public interface ICleanupComponentsAliveTag : IComponentData
    {
    }
}
