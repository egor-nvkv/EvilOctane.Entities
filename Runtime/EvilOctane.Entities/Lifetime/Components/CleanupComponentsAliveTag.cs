using System;
using Unity.Entities;

namespace EvilOctane.Entities
{
    /// <summary>
    /// A tag component signifying the presence of
    /// <see cref="ICleanupComponentData"/> or <see cref="ICleanupBufferElementData"/>.
    /// </summary>
    [Obsolete("Using the same tag for everything creates hard to debug Entity leaks. Consider using KeptAliveByAttribute for annotations instead.", true)]
    public struct CleanupComponentsAliveTag
    {
    }
}
