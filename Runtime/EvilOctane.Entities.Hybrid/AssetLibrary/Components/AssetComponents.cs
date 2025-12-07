using System.Runtime.InteropServices;
using Unity.Entities;
using static System.Runtime.CompilerServices.Unsafe;
using UnityObject = UnityEngine.Object;

namespace EvilOctane.Entities
{
    public struct Asset
    {
        [TemporaryBakingType]
        public struct RebakedTag : IComponentData { }

        [BakingType]
        [StructLayout(LayoutKind.Sequential)]
        public struct UnityObjectComponent : IComponentData
        {
            public UnityObjectRef<UnityObject> Ref;
            public uint TypeHash0;
            public uint TypeHash1;

            public ulong TypeHash
            {
                readonly get
                {
                    ref byte typeHashRO = ref As<uint, byte>(ref AsRef(in TypeHash0));
                    return ReadUnaligned<ulong>(ref typeHashRO);
                }
                set
                {
                    ref byte typeHash = ref As<uint, byte>(ref TypeHash0);
                    WriteUnaligned(ref typeHash, value);
                }
            }
        }

        [BakingType]
        [InternalBufferCapacity(0)]
        [StructLayout(LayoutKind.Sequential, Size = 1)]
        public struct BakingNameStorage : IBufferElementData
        {
            public byte Utf8Byte;
        }
    }
}
