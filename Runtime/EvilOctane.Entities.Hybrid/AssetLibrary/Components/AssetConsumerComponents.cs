using Unity.Entities;
using UnityObject = UnityEngine.Object;

namespace EvilOctane.Entities
{
    public struct AssetConsumer
    {
        [TemporaryBakingType]
        public struct RebakedTag : IComponentData { }

        [BakingType]
        public struct DeclaredAssetReference : IComponentData
        {
            public UnityObjectRef<UnityObject> Asset;
        }

        [BakingType]
        public struct ResolvedAssetReference : IComponentData
        {
            public Entity Asset;
        }
    }
}
