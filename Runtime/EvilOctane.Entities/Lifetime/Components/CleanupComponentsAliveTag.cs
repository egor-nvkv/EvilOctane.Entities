using Unity.Entities;

namespace EvilOctane.Entities
{
    /// <summary>
    /// A tag component signifying the presence of
    /// <see cref="ICleanupComponentData"/> or <see cref="ICleanupBufferElementData"/>.
    /// </summary>
    public struct CleanupComponentsAliveTag : ICleanupComponentsAliveTag
    {
    }
}
