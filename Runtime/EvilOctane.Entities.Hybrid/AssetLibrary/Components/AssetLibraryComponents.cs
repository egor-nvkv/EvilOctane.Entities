using Unity.Entities;

namespace EvilOctane.Entities
{
    public partial class AssetLibrary
    {
        [BakingType]
        public struct AliveTag : ICleanupComponentsAliveTag { }

        [TemporaryBakingType]
        public struct RebakedTag : IComponentData { }

        [BakingType]
        public struct UnityObjectComponent : IComponentData
        {
            public UnityObjectRef<AssetLibrary> Value;
        }

        [BakingType]
        [InternalBufferCapacity(0)]
        public struct AssetBufferElement : IOwnedEntityBufferElementData
        {
            public Entity Entity;

            public readonly Entity OwnedEntity => Entity;

            public struct OwnerShared : ISharedComponentData
            {
                public Entity AssetLibrary;
            }
        }

        [BakingType]
        public struct AssetTableComponent : ICleanupComponentData
        {
            public AssetTable Value;
        }
    }
}
