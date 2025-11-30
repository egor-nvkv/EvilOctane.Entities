using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using UnityObject = UnityEngine.Object;

namespace EvilOctane.Entities.Internal
{
    public struct AssetLibraryInternal
    {
        [BakingType]
        public struct DeclaredReference : IComponentData
        {
            public UnityObjectRef<AssetLibrary> AssetLibrary;
        }

        [TemporaryBakingType]
        [InternalBufferCapacity(0)]
        public struct TempAssetBufferElement : IBufferElementData
        {
            public UnityObjectRef<UnityObject> Asset;
            public ulong TypeHash;
            public UnsafeText Name;
        }
    }
}
