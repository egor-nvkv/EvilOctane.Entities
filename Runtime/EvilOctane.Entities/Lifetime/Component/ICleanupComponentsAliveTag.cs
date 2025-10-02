using Unity.Entities;

namespace EvilOctane.Entities
{
    /// <summary>
    /// An interface for a tag component to signify the presence of
    /// <see cref="ICleanupComponentData"/>, <see cref="ICleanupBufferElementData"/> or
    /// <see cref="ICleanupSharedComponentData"/>.
    /// </summary>
    public interface ICleanupComponentsAliveTag : IComponentData
    {
    }
}
