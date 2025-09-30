using Unity.Entities;

namespace EvilOctane.Entities
{
    /// <summary>
    /// An interface for a component that identifies owner of this <see cref="Entity"/>.
    /// </summary>
    /// 
    /// <remarks>
    /// <see cref="IEntityOwnerBufferElementData"/> can be used on owner <see cref="Entity"/>
    /// as a means of managing owned <see cref="Entity"/>'s lifetime.
    /// <br/>
    /// Multiple <see cref="IComponentData"/> implementing <see cref="IOwnerEntityComponentData"/>
    /// are allowed to exist on <see cref="Entity"/>.
    /// </remarks>
    public interface IOwnerEntityComponentData : ICleanupComponentData
    {
        /// <summary>
        /// Owner <see cref="Entity"/> of this <see cref="IOwnerEntityComponentData"/>.
        /// </summary>
        /// 
        /// <remarks>
        /// Should not be assumed to exist.
        /// Should notify owner upon destruction.
        /// </remarks>
        Entity OwnerEntity { get; }
    }
}
