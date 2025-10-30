using Unity.Entities;
using UnityEngine;

namespace EvilOctane.Entities.Internal
{
    public struct AssetLibraryInternal
    {
        [BakingType]
        public struct Reference : IComponentData
        {
            public UnityObjectRef<AssetLibrary> AssetLibrary;
        }

        [TemporaryBakingType]
        [InternalBufferCapacity(0)]
        public struct KeyBufferElement : IBufferElementData
        {
            public AssetLibraryKey Key;
        }

        [BakingType] // Not Temporary to keep refs alive
        [InternalBufferCapacity(0)]
        public struct AssetBufferElement : IBufferElementData
        {
            public UnityObjectRef<Object> Asset;
        }

        [BakingType]
        public struct ConsumerEntityBufferElement : IBufferElementData
        {
            public Entity ConsumerEntity;
        }

        [TemporaryBakingType]
        public struct ReferenceBufferElement : IBufferElementData
        {
            public UnityObjectRef<AssetLibrary> AssetLibrary;
        }
    }
}
