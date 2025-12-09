using Unity.Entities;
using UnityObject = UnityEngine.Object;

namespace EvilOctane.Entities.Internal
{
    public struct AssetLibraryInternal
    {
        [TemporaryBakingType]
        [InternalBufferCapacity(0)]
        public struct AssetDataBufferElement : IBufferElementData
        {
            public AssetData Data;
            public UnityObjectRef<UnityObject> Asset;
        }
    }
}
