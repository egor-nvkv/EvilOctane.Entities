using Unity.Assertions;
using Unity.Burst;
using Unity.Burst.CompilerServices;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using ListenerList = EvilOctane.Entities.Internal.EventSubscriptionRegistry.Component.ListenerList;

namespace EvilOctane.Entities.Internal
{
    [BurstCompile]
    public unsafe struct EventChangeSubscriptionStatusJob : IJobChunk
    {
        public ComponentTypeHandle<EventSubscriptionRegistry.Component> EventSubscriptionRegistryComponentTypeHandle;
        public BufferTypeHandle<EventSubscriptionRegistry.ChangeSubscriptionStatusBufferElement> EventSubscriptionRegistryChangeSubscriptionStatusBufferTypeHandle;

        public AllocatorManager.AllocatorHandle TempAllocator;

        private static int CalculateNewSubscriptionRegistryEntryCount(
            ref UnsafeHashMap<TypeIndex, ListenerList> eventSubscriptionRegistry,
            ref UnsafeHashSet<TypeIndex> typeIndexSet,
            UnsafeSpan<EventSubscriptionRegistry.ChangeSubscriptionStatusBufferElement> changeStatusSpanRO)
        {
            Assert.IsTrue(typeIndexSet.IsEmpty);

            HashMapHelperRef<TypeIndex> eventSubscriptionRegistryHelper = eventSubscriptionRegistry.GetHelperRef();
            HashMapHelperRef<TypeIndex> typeIndexSetHelper = typeIndexSet.GetHelperRef();

            typeIndexSetHelper.EnsureCapacity(changeStatusSpanRO.Length);
            int newSubscriptionRegistryEntryCount = 0;

            foreach (EventSubscriptionRegistry.ChangeSubscriptionStatusBufferElement changeStatus in changeStatusSpanRO)
            {
                if (changeStatus.Selector == EventSubscribeUnsubscribeSelector.Subscribe)
                {
                    _ = typeIndexSetHelper.TryAddNoResize(changeStatus.EventTypeIndex, out bool added);

                    if (!added)
                    {
                        // Multiple subscribes to the same Type Index
                        continue;
                    }

                    bool typeIndexIsInRegistry = eventSubscriptionRegistryHelper.ContainsKey(changeStatus.EventTypeIndex);

                    if (!typeIndexIsInRegistry)
                    {
                        // New Type Index
                        ++newSubscriptionRegistryEntryCount;
                    }
                }
            }

            return newSubscriptionRegistryEntryCount;
        }

        private static void Subscribe(
            ref UnsafeHashMap<TypeIndex, ListenerList> eventSubscriptionRegistry,
            Entity listenerEntity,
            TypeIndex eventTypeIndex)
        {
            ref ListenerList listenerList = ref eventSubscriptionRegistry.GetHelperRef().GetOrAddValue<ListenerList>(eventTypeIndex, out bool added);

            if (added)
            {
                // No subscriptions for this Event Type

                listenerList = new(1);

                // Subscribe
                listenerList.AddListener(listenerEntity, noResize: true);
            }
            else
            {
                // Subscriptions exist for this Event Type

                bool alreadySubscribed = listenerList.IsSubscribed(listenerEntity);

                if (Hint.Unlikely(alreadySubscribed))
                {
                    // Already subscribed

#if ENABLE_PROFILER
                    ++EventSystemProfiler.DuplicateSubscribesCounter.Data.Value;
#endif
                    return;
                }

                // Subscribe
                listenerList.AddListener(listenerEntity);
            }
        }

        private static void Unsubscribe(
            ref UnsafeHashMap<TypeIndex, ListenerList> eventSubscriptionRegistry,
            Entity listenerEntity,
            TypeIndex eventTypeIndex)
        {
            ref ListenerList listenerList = ref eventSubscriptionRegistry.GetHelperRef().TryGetValueRef<ListenerList>(eventTypeIndex, out bool exists);

            if (Hint.Unlikely(!exists))
            {
                // No subscriptions for this Event Type

#if ENABLE_PROFILER
                ++EventSystemProfiler.PhantomUnsubscribesCounter.Data.Value;
#endif
                return;
            }

            int listenerIndex = listenerList.FindListenerIndex(listenerEntity);

            if (Hint.Unlikely(listenerIndex < 0))
            {
                // Lister not subscribed

#if ENABLE_PROFILER
                ++EventSystemProfiler.PhantomUnsubscribesCounter.Data.Value;
#endif
                return;
            }

            if (listenerList.Length > 1)
            {
                // Unsubscribe
                listenerList.RemoveListener(listenerIndex);
            }
            else
            {
                // Last Listener unsubscribed

                listenerList.Dispose();
                _ = eventSubscriptionRegistry.GetHelperRef().Remove(eventTypeIndex);
            }
        }

        public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
        {
            Assert.IsFalse(useEnabledMask);

            UnsafeHashSet<TypeIndex> typeIndexSet = UnsafeHashSetUtility.CreateHashSet<TypeIndex>(16, 16, TempAllocator);

            EventSubscriptionRegistry.Component* eventSubscriptionRegistryPtr = chunk.GetComponentDataPtrRW(ref EventSubscriptionRegistryComponentTypeHandle);
            BufferAccessor<EventSubscriptionRegistry.ChangeSubscriptionStatusBufferElement> eventSubscriptionRegistryChangeSubscriptionStatusBufferAccessor = chunk.GetBufferAccessorRW(ref EventSubscriptionRegistryChangeSubscriptionStatusBufferTypeHandle);

            for (int entityIndex = 0; entityIndex != chunk.Count; ++entityIndex)
            {
                DynamicBuffer<EventSubscriptionRegistry.ChangeSubscriptionStatusBufferElement> changeStatusBuffer = eventSubscriptionRegistryChangeSubscriptionStatusBufferAccessor[entityIndex];

                if (changeStatusBuffer.IsEmpty)
                {
                    // No Subscribe / Unsubscribe
                    continue;
                }

                ref UnsafeHashMap<TypeIndex, ListenerList> eventSubscriptionRegistry = ref eventSubscriptionRegistryPtr[entityIndex].EventTypeIndexListenerListMap;
                UnsafeSpan<EventSubscriptionRegistry.ChangeSubscriptionStatusBufferElement> changeStatusSpanRO = changeStatusBuffer.AsSpanRO();

                // Ensure Subscription Registry capacity

                int newSubscriptionRegistryEntryCount = CalculateNewSubscriptionRegistryEntryCount(
                    ref eventSubscriptionRegistry,
                    ref typeIndexSet,
                    changeStatusSpanRO);

                eventSubscriptionRegistry.GetHelperRef().EnsureSlack(newSubscriptionRegistryEntryCount);

                // Update subscriptions

                foreach (EventSubscriptionRegistry.ChangeSubscriptionStatusBufferElement changeStatus in changeStatusSpanRO)
                {
                    switch (changeStatus.Selector)
                    {
                        case EventSubscribeUnsubscribeSelector.Subscribe:
                            Subscribe(
                                ref eventSubscriptionRegistry,
                                changeStatus.ListenerEntity,
                                changeStatus.EventTypeIndex);

                            break;

                        case EventSubscribeUnsubscribeSelector.Unsubscribe:
                            Unsubscribe(
                                ref eventSubscriptionRegistry,
                                changeStatus.ListenerEntity,
                                changeStatus.EventTypeIndex);

                            break;
                    }
                }

                // Clear Buffer
                changeStatusBuffer.Clear();

                if (entityIndex != chunk.Count - 1)
                {
                    // Clear Type Index set
                    typeIndexSet.Clear();
                }
            }
        }
    }
}
