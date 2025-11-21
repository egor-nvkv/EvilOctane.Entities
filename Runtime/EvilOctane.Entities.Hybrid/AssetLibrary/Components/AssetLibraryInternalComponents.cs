using System.Runtime.InteropServices;
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
        public struct ReferenceBufferElement : IBufferElementData
        {
            public UnityObjectRef<AssetLibrary> AssetLibrary;
        }

        [TemporaryBakingType]
        [InternalBufferCapacity(0)]
        [StructLayout(LayoutKind.Sequential, Size = 1)]
        public struct KeyStorage : IBufferElementData
        {
            public byte RawByte;
        }

        [TemporaryBakingType]
        [InternalBufferCapacity(0)]
        public struct KeyBufferElement : IBufferElementData
        {
            public ulong AssetTypeHash;
            public nint AssetNameOffset;
            public int AssetNameLength;
        }

        [BakingType] // Not Temporary to keep refs alive
        [InternalBufferCapacity(0)]
        public struct AssetBufferElement : IBufferElementData
        {
            public UnityObjectRef<Object> Asset;
        }

        [BakingType]
        public struct ConsumerBufferElement : IBufferElementData
        {
            public Entity Entity;
        }
    }
}
