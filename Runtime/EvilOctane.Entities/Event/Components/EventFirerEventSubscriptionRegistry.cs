using Unity.Entities;

namespace EvilOctane.Entities
{
    public partial struct EventFirer
    {
        public struct EventSubscriptionRegistry
        {
            [InternalBufferCapacity(0)]
            public struct CommandBufferElement : ICleanupBufferElementData
            {
                public Entity ListenerEntity;
                public TypeIndex EventTypeIndex;
                public Command Command;
            }

            public enum Command : byte
            {
                /// <summary>
                /// Subscribe to events declared in <see cref="EventListener.EventDeclarationBuffer"/>.
                /// </summary>
                SubscribeAuto,

                /// <summary>
                /// Subscribe to <see cref="CommandBufferElement.EventTypeIndex"/>.
                /// The <see cref="EventListener"/> is not required to declare that event type.
                /// </summary>
                SubscribeManual,

                /// <summary>
                /// Unsubscribe from events declared in <see cref="EventListener.EventDeclarationBuffer"/>.
                /// </summary>
                UnsubscribeAuto,

                /// <summary>
                /// Unsubscribe from <see cref="CommandBufferElement.EventTypeIndex"/>.
                /// </summary>
                UnsubscribeManual,

                /// <summary>
                /// Remove destroyed <see cref="EventListener"/>'s
                /// and recreate <see cref="EventSubscriptionRegistry"/> to take as little space as possible.
                /// <br/>
                /// Per-event-type <see cref="EventListener"/> lists's capacities can go below
                /// values specified in <see cref="EventDeclarationBuffer.StableTypeElement.ListenerListInitialCapacity"/>.
                /// </summary>
                Compact
            }
        }

        public struct RawEventSubscriptionRegistry
        {
            [InternalBufferCapacity(0)]
            public struct CommandBufferElement : ICleanupBufferElementData
            {
                public Entity ListenerEntity;
                public ulong EventTypeHash;
                public Command Command;
            }

            public enum Command : byte
            {
                /// <summary>
                /// Subscribe to events declared in <see cref="EventListener.RawEventDeclarationBuffer"/>.
                /// </summary>
                SubscribeAuto,

                /// <summary>
                /// Subscribe to <see cref="CommandBufferElement.EventTypeHash"/>.
                /// The <see cref="EventListener"/> is not required to declare that event type.
                /// </summary>
                SubscribeManual,

                /// <summary>
                /// Unsubscribe from events declared in <see cref="EventListener.RawEventDeclarationBuffer"/>.
                /// </summary>
                UnsubscribeAuto,

                /// <summary>
                /// Unsubscribe from <see cref="CommandBufferElement.EventTypeHash"/>.
                /// </summary>
                UnsubscribeManual,

                /// <summary>
                /// Remove destroyed <see cref="EventListener"/>'s
                /// and recreate <see cref="RawEventSubscriptionRegistry"/> to take as little space as possible.
                /// <br/>
                /// Per-event-type <see cref="EventListener"/> lists's capacities can go below
                /// values specified in <see cref="RawEventDeclarationBuffer.TypeElement.ListenerListInitialCapacity"/>.
                /// </summary>
                Compact
            }
        }
    }
}
