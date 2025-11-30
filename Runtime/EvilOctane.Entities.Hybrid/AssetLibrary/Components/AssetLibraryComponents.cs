using System;
using Unity.Entities;

namespace EvilOctane.Entities
{
    public partial class AssetLibrary
    {
        [BakingType]
        public struct AliveTag : ICleanupComponentsAliveTag { }

        [BakingType]
        public struct UnityObjectComponent : IComponentData
        {
            public UnityObjectRef<AssetLibrary> Value;
        }

        /// <summary>
        /// A reference to a baked <see cref="AssetLibrary"/>.
        /// </summary>
        [BakingType]
        public struct ReferenceBufferElement : IBufferElementData
        {
            public Entity Entity;
        }

        [BakingType]
        [InternalBufferCapacity(0)]
        public struct AssetBufferElement : IOwnedEntityBufferElementData
        {
            public Entity Entity;

            public readonly Entity OwnedEntity => Entity;

            public struct OwnerShared : ISharedComponentData, IEquatable<OwnerShared>
            {
                public Entity AssetLibrary;

                public readonly bool Equals(OwnerShared other)
                {
                    return AssetLibrary == other.AssetLibrary;
                }

                public override readonly int GetHashCode()
                {
                    return AssetLibrary.GetHashCode();
                }
            }
        }

        [BakingType]
        public struct AssetTableComponent : ICleanupComponentData
        {
            public AssetTable Value;
        }
    }
}
