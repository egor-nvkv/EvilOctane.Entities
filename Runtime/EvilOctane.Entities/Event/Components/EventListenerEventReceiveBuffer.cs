using System.Runtime.InteropServices;
using Unity.Entities;

namespace EvilOctane.Entities
{
    public partial struct EventListener
    {
        public struct EventReceiveBuffer
        {
            [InternalBufferCapacity(0)]
            public struct Element : IBufferElementData
            {
                public Entity EventFirerEntity;
                public Entity EventEntity;
            }
        }

        public struct RawEventReceiveBuffer
        {
            [InternalBufferCapacity(0)]
            public struct FirerElement : IBufferElementData
            {
                public Entity EventFirerEntity;
            }

            [InternalBufferCapacity(0)]
            [StructLayout(LayoutKind.Sequential, Size = 1)]
            public struct Storage : IBufferElementData
            {
                public byte RawByte;
            }
        }
    }
}
