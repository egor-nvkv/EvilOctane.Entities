using System;
using Unity.Entities;

namespace EvilOctane.Entities
{
    /// <summary>
    /// Apply this attribute to <see cref="ICleanupComponentData"/>, <see cref="ICleanupBufferElementData"/>
    /// or <see cref="ICleanupSharedComponentData"/> components
    /// to express which <see cref="ComponentType"/> is required to be present on <see cref="Entity"/>
    /// to prevent them from getting cleaned up.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = true, Inherited = false)]
    public class KeptAliveByAttribute : Attribute
    {
        public readonly Type IsAliveTag;

        public KeptAliveByAttribute(Type isAliveTag)
        {
            IsAliveTag = isAliveTag;
        }
    }
}
