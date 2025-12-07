using Unity.Entities;

namespace EvilOctane.Entities
{
    public struct AssetLibraryConsumerAdditional
    {
        [TemporaryBakingType]
        public struct RebakedTag : IComponentData { }

        [BakingType]
        public struct DeclaredReference : IComponentData
        {
            public UnityObjectRef<AssetLibrary> AssetLibrary;
        }
    }
}
