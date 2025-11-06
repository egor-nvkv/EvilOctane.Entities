using System.Runtime.InteropServices;
using Unity.Entities;

namespace EvilOctane.Entities.Internal
{
    public struct EventFirerInternal
    {
        public struct EventSubscriptionRegistry
        {
            [InternalBufferCapacity(0)]
            [StructLayout(LayoutKind.Sequential, Size = 1)]
            public struct Storage : ICleanupBufferElementData
            {
                public byte RawByte;
            }
        }

        public struct RawEventSubscriptionRegistry
        {
            [InternalBufferCapacity(0)]
            [StructLayout(LayoutKind.Sequential, Size = 1)]
            public struct Storage : ICleanupBufferElementData
            {
                public byte RawByte;
            }
        }
    }
}
