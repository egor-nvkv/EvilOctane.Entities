using System.Runtime.InteropServices;
using Unity.Entities;

namespace EvilOctane.Entities.Internal
{
    public unsafe partial struct EventSubscriptionRegistry
    {
        [InternalBufferCapacity(0)]
        [StructLayout(LayoutKind.Sequential, Size = 1)]
        public struct StorageBufferElement : ICleanupBufferElementData
        {
            public byte RawByte;
        }

        [InternalBufferCapacity(0)]
        public struct SubscribeUnsubscribeBufferElement : ICleanupBufferElementData
        {
            public Entity ListenerEntity;
            public TypeIndex EventTypeIndex;
            public SubscribeUnsubscribeMode Mode;
        }

        public enum SubscribeUnsubscribeMode : byte
        {
            SubscribeAuto,
            SubscribeManual,
            UnsubscribeAuto,
            UnsubscribeManual
        }
    }
}
