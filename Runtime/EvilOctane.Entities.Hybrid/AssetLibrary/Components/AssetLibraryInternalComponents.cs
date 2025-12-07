using Unity.Entities;
using UnityObject = UnityEngine.Object;

namespace EvilOctane.Entities.Internal
{
    public struct AssetLibraryInternal
    {
        [TemporaryBakingType]
        [InternalBufferCapacity(0)]
        public struct AssetReferenceBufferElement : IBufferElementData
        {
            public AssetReferenceData Data;
            public UnityObjectRef<UnityObject> Asset;
        }
    }
}
