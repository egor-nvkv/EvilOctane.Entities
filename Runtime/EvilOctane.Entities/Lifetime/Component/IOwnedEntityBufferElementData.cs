using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;

namespace EvilOctane.Entities
{
    /// <summary>
    /// An interface for a buffer component that owns and manages a list of <see cref="Entity"/>'s.
    /// </summary>
    /// 
    /// <remarks>
    /// <see cref="IOwnerEntityComponentData"/> can be used on owned <see cref="Entity"/>'s
    /// as a means of notifying about their destruction.
    /// <br/>
    /// Multiple <see cref="IBufferElementData"/> implementing <see cref="IOwnedEntityBufferElementData"/>
    /// are allowed to exist on <see cref="Entity"/>.
    /// <br/>
    /// Must be reinterpretable as <see cref="Entity"/>
    /// (<see cref="UnsafeUtility2.CanBeReinterpretedExactly{TSource, TDestination}"/>).
    /// </remarks>
    public interface IOwnedEntityBufferElementData : ICleanupBufferElementData
    {
        /// <summary>
        /// <see cref="Entity"/> owned by this <see cref="IOwnedEntityBufferElementData"/>.
        /// </summary>
        /// 
        /// <remarks>
        /// Not allowed to be <see cref="Entity.Null"/>
        /// but should not be assumed to exist.
        /// Should be released by owner upon his destruction.
        /// </remarks>
        Entity OwnedEntity { get; }
    }
}
