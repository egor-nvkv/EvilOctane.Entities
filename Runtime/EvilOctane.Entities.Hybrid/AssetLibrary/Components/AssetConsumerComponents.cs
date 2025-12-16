using Unity.Entities;
using UnityObject = UnityEngine.Object;

namespace EvilOctane.Entities
{
    public struct AssetConsumer
    {
        [TemporaryBakingType]
        public struct RebakedTag : IComponentData { }

        [BakingType]
        public struct DeclaredReference : IComponentData
        {
            public UnityObjectRef<AssetLibrary> AssetLibrary;
            public UnityObjectRef<UnityObject> Asset;

            [BakingType]
            public struct Resolved : IComponentData
            {
                public Entity Asset;
            }
        }
    }
}
