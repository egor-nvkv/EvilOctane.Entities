using System.Runtime.CompilerServices;
using Unity.Entities;

namespace EvilOctane.Entities.Internal
{
    public static partial class EventAPIInternal
    {
        // Firer

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ComponentTypeSet GetEventFirerComponentTypeSet(bool includeIsAliveTag = true)
        {
            return includeIsAliveTag ?
                ComponentTypeSetUtility.Create<
                // Allocated Tag
                EventFirer.IsAliveTag,

                // Listener Registry
                EventFirerInternal.EventSubscriptionRegistry.Storage,
                EventFirer.EventSubscriptionRegistry.CommandBufferElement,

                // Event Buffer
                EventFirer.EventBuffer.EntityElement,
                EventFirer.EventBuffer.TypeElement>() :

                ComponentTypeSetUtility.Create<
                // Listener Registry
                EventFirerInternal.EventSubscriptionRegistry.Storage,
                EventFirer.EventSubscriptionRegistry.CommandBufferElement,

                // Event Buffer
                EventFirer.EventBuffer.EntityElement,
                EventFirer.EventBuffer.TypeElement>();
        }

        // Listener

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ComponentTypeSet GetEventListenerComponentTypeSet()
        {
            return ComponentTypeSetUtility.Create<
                // Settings
                EventListener.EventDeclarationBuffer.TypeElement,

                // Receive Buffer
                EventListener.EventReceiveBuffer.Element

#if EVIL_OCTANE_ENABLE_PARALLEL_EVENT_ROUTING
                ,
                EventListener.EventReceiveBuffer.LockComponent
#endif
                >();
        }
    }
}
