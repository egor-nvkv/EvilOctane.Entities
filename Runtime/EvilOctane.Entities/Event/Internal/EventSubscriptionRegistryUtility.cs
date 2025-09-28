using System;
using System.Runtime.CompilerServices;
using Unity.Assertions;
using Unity.Burst.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Entities.LowLevel.Unsafe;
using Unity.Mathematics;
using static Unity.Collections.CollectionHelper;
using static Unity.Collections.CollectionHelper2;
using static Unity.Collections.LowLevel.Unsafe.UnsafeUtility2;
using EventSubscriberList = Unity.Collections.LowLevel.Unsafe.InlineList<Unity.Entities.Entity>;
using EventSubscriberListHeader = Unity.Collections.LowLevel.Unsafe.InlineListHeader<Unity.Entities.Entity>;
using EventSubscriptionMap = Unity.Collections.LowLevel.Unsafe.InlineHashMap<Unity.Entities.TypeIndex, EvilOctane.Entities.Internal.EventSubscriberListOffset>;
using EventSubscriptionMapHeader = Unity.Collections.LowLevel.Unsafe.InlineHashMapHeader<Unity.Entities.TypeIndex>;

namespace EvilOctane.Entities.Internal
{
    public unsafe partial struct EventSubscriptionRegistry
    {
        public static int Alignment
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => math.max(EventSubscriptionMap.Alignment, EventSubscriberList.Alignment);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsCreated(DynamicBuffer<StorageBufferElement> buffer)
        {
            return buffer.Length >= sizeof(EventSubscriptionMapHeader);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool NeedsTrimming(DynamicBuffer<StorageBufferElement> buffer, int actualLength)
        {
            return buffer.Length / 2 >= actualLength;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static EventSubscriptionMapHeader* GetSubscriptionMap(DynamicBuffer<StorageBufferElement> buffer, bool readOnly = false)
        {
            void* subscriptionMap = readOnly ? buffer.GetUnsafeReadOnlyPtr() : buffer.GetUnsafePtr();

            CheckIsAligned<EventSubscriptionMapHeader>(subscriptionMap);
            return (EventSubscriptionMapHeader*)subscriptionMap;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static nint GetFirstSubscriberListOffset(EventSubscriptionMapHeader* subscriptionMap)
        {
            nint mapSize = EventSubscriptionMap.GetTotalAllocationSize(subscriptionMap->Count);
            return Align(mapSize, EventSubscriberList.Alignment);
        }

        public static void Create(DynamicBuffer<StorageBufferElement> buffer, ref UnsafeHashMap<TypeIndex, int> eventTypeSubscriberCapacityMap)
        {
            if (Hint.Unlikely(eventTypeSubscriberCapacityMap.IsEmpty))
            {
                // Empty
                return;
            }

            // Calculate size

            nint requiredSize = GetRequiredAllocationSize(ref eventTypeSubscriberCapacityMap, out nint listsOffset);
            buffer.ResizeUninitialized(AllocationSizeToStorageLength(requiredSize));

            byte* bufferPtr = (byte*)buffer.GetUnsafePtr();

            // Create Subscription map

            EventSubscriptionMapHeader* subscriptionMap = GetSubscriptionMap(buffer);
            EventSubscriptionMap.Create(subscriptionMap, eventTypeSubscriberCapacityMap.Count);

            // Create Subscriber lists

            nint offset = listsOffset;

            foreach (KVPair<TypeIndex, int> kvPair in eventTypeSubscriberCapacityMap)
            {
                kvPair.AssumeIndexIsValid();

                // Create list

                offset = Align(offset, EventSubscriberList.Alignment);
                EventSubscriberListHeader* list = (EventSubscriberListHeader*)(bufferPtr + offset);

                int subscriberCapacity = math.max(kvPair.Value, 1);
                EventSubscriberList.Create(list, subscriberCapacity);

                // Register list

                nint listOffset = offset - listsOffset;
                EventSubscriptionMap.AddUncheckedNoResize(subscriptionMap, kvPair.Key, listOffset);

                offset += EventSubscriberList.GetTotalAllocationSize(subscriberCapacity);
            }
        }

        public static void CopyTo(DynamicBuffer<StorageBufferElement> buffer, ref UnsafeHashMap<TypeIndex, EventListenerListCapacityPair> eventTypeListenerListMap, AllocatorManager.AllocatorHandle tempAllocator)
        {
            EventSubscriptionMapHeader* subscriptionMap = GetSubscriptionMap(buffer, readOnly: true);
            nint firstListOffset = GetFirstSubscriberListOffset(subscriptionMap);

            HashMapHelperRef<TypeIndex> mapHelper = eventTypeListenerListMap.GetHelperRef();
            mapHelper.EnsureCapacity(subscriptionMap->Count);

            EventSubscriptionMap.Enumerator enumerator = EventSubscriptionMap.GetEnumerator(subscriptionMap);

            while (enumerator.MoveNext())
            {
                InlineHashMapKVPair<TypeIndex, EventSubscriberListOffset> kvPair = enumerator.Current;
                EventSubscriberListHeader* list = kvPair.ValueRefRW.GetList(subscriptionMap, firstListOffset);

                ref EventListenerListCapacityPair listenerListCapacityPair = ref mapHelper.GetOrAddValueNoResize<EventListenerListCapacityPair>(kvPair.Key, out bool added);

                if (added)
                {
                    bool isEmpty = list->Length == 0;

                    if (isEmpty)
                    {
                        // Don't allocate
                        listenerListCapacityPair.ListenerListPtr = null;

                        // Keep original capacity
                        listenerListCapacityPair.ListenerListStartingCapacity = list->Capacity;

                        continue;
                    }
                    else
                    {
                        // Allocate
                        goto Allocate;
                    }
                }
                else
                {
                    if (listenerListCapacityPair.IsCreated)
                    {
                        // Ensure capacity

                        listenerListCapacityPair.Clear();
                        listenerListCapacityPair.EnsureCapacity(list->Length, tempAllocator);
                    }
                    else
                    {
                        // Allocate
                        goto Allocate;
                    }
                }

            AddRange:
                listenerListCapacityPair.AddRangeNoResize(EventSubscriberList.AsSpan(list));
                continue;

            Allocate:
                listenerListCapacityPair = new EventListenerListCapacityPair(list->Length, tempAllocator);
                goto AddRange;
            }
        }

        public static void CopyFrom(DynamicBuffer<StorageBufferElement> buffer, ref UnsafeHashMap<TypeIndex, EventListenerListCapacityPair> eventTypeListenerListMap)
        {
            // Calculate size

            nint requiredSize = GetRequiredAllocationSize(ref eventTypeListenerListMap, out nint listsOffset);
            int requiredLength = AllocationSizeToStorageLength(requiredSize);

            if (NeedsTrimming(buffer, requiredLength))
            {
                // Trim

                buffer.SetLengthNoResize(requiredLength);
                buffer.TrimExcess();
            }
            else
            {
                // Resize
                buffer.ResizeUninitialized(requiredLength);
            }

            byte* bufferPtr = (byte*)buffer.GetUnsafePtr();

            // Create Subscription map

            EventSubscriptionMapHeader* subscriptionMap = GetSubscriptionMap(buffer);
            EventSubscriptionMap.Create(subscriptionMap, eventTypeListenerListMap.Count);

            // Create Subscriber lists

            nint offset = listsOffset;

            foreach (KVPair<TypeIndex, EventListenerListCapacityPair> kvPair in eventTypeListenerListMap)
            {
                kvPair.AssumeIndexIsValid();
                EventListenerListCapacityPair listenerListCapacityPair = kvPair.Value;

                // Create list

                offset = Align(offset, EventSubscriberList.Alignment);
                EventSubscriberListHeader* list = (EventSubscriberListHeader*)(bufferPtr + offset);

                int subscriberCapacity = kvPair.Value.ListenerRequiredCapacity;
                EventSubscriberList.Create(list, subscriberCapacity);

                // Copy list

                if (listenerListCapacityPair.ListenerListLength != 0)
                {
                    EventSubscriberList.AddRangeNoResize(list, listenerListCapacityPair.AsSpan());
                }

                // Register list

                nint listOffset = offset - listsOffset;
                EventSubscriptionMap.AddUncheckedNoResize(subscriptionMap, kvPair.Key, listOffset);

                offset += EventSubscriberList.GetTotalAllocationSize(subscriberCapacity);
            }
        }

        public static void Subscribe(ref UnsafeHashMap<TypeIndex, EventListenerListCapacityPair> eventTypeListenerListMap, Entity listenerEntity, TypeIndex eventTypeIndex, AllocatorManager.AllocatorHandle tempAllocator)
        {
            HashMapHelperRef<TypeIndex> mapHelper = eventTypeListenerListMap.GetHelperRef();
            ref EventListenerListCapacityPair listenerListCapacityPair = ref mapHelper.TryGetValueRef<EventListenerListCapacityPair>(eventTypeIndex, out bool exists);

            if (!exists)
            {
                // Event Type not declared
                return;
            }

            if (listenerListCapacityPair.IsCreated)
            {
                bool alreadySubscribed = listenerListCapacityPair.AsSpan().Contains(listenerEntity);

                if (Hint.Unlikely(alreadySubscribed))
                {
                    // Already Subscribed
                    return;
                }

                // Ensure capacity
                listenerListCapacityPair.EnsureSlack(4, tempAllocator);
            }
            else
            {
                // Allocate

                int subscriberCapacity = listenerListCapacityPair.ListenerRequiredCapacity;
                listenerListCapacityPair = new EventListenerListCapacityPair(subscriberCapacity, tempAllocator);
            }

            listenerListCapacityPair.AddNoResize(listenerEntity);
        }

        public static void Subscribe(ref UnsafeHashMap<TypeIndex, EventListenerListCapacityPair> eventTypeListenerListMap, UnsafeSpan<EventSettings.ListenerDeclaredEventTypeBufferElement> listenerDeclaredEventTypeSpanRO, Entity listenerEntity, AllocatorManager.AllocatorHandle tempAllocator)
        {
            foreach (EventSettings.ListenerDeclaredEventTypeBufferElement listenerDeclaredEvent in listenerDeclaredEventTypeSpanRO)
            {
                Subscribe(ref eventTypeListenerListMap, listenerEntity, listenerDeclaredEvent.EventTypeIndex, tempAllocator);
            }
        }

        public static bool TrySubscribeNoResize(DynamicBuffer<StorageBufferElement> buffer, Entity listenerEntity, TypeIndex eventTypeIndex)
        {
            EventSubscriptionMapHeader* subscriptionMap = GetSubscriptionMap(buffer);
            bool eventTypeDeclared = EventSubscriptionMap.TryGetValue(subscriptionMap, eventTypeIndex, out EventSubscriberListOffset listOffset);

            if (!eventTypeDeclared)
            {
                // Event Type not declared
                return true;
            }

            nint firstListOffset = GetFirstSubscriberListOffset(subscriptionMap);
            EventSubscriberListHeader* list = listOffset.GetList(subscriptionMap, firstListOffset);

            bool alreadySubscribed = EventSubscriberList.AsSpan(list).Contains(listenerEntity);

            if (Hint.Unlikely(alreadySubscribed))
            {
                // Already Subscribed
                return true;
            }

            bool isFull = list->Capacity == list->Length;

            if (Hint.Unlikely(isFull))
            {
                // List is full
                return false;
            }

            EventSubscriberList.AddNoResize(list, listenerEntity);
            return true;
        }

        public static bool TrySubscribeNoResize(DynamicBuffer<StorageBufferElement> buffer, UnsafeSpan<EventSettings.ListenerDeclaredEventTypeBufferElement> listenerDeclaredEventTypeSpanRO, Entity listenerEntity, out int processedCount)
        {
            processedCount = 0;

            foreach (EventSettings.ListenerDeclaredEventTypeBufferElement listenerDeclaredEvent in listenerDeclaredEventTypeSpanRO)
            {
                if (!TrySubscribeNoResize(buffer, listenerEntity, listenerDeclaredEvent.EventTypeIndex))
                {
                    // Full
                    return false;
                }

                ++processedCount;
            }

            return processedCount == listenerDeclaredEventTypeSpanRO.Length;
        }

        private static nint GetRequiredAllocationSize(ref UnsafeHashMap<TypeIndex, int> eventTypeSubscriberCapacityMap, out nint listsOffset)
        {
            nint mapSize = EventSubscriptionMap.GetTotalAllocationSize(eventTypeSubscriberCapacityMap.Count);
            listsOffset = Align(mapSize, EventSubscriberList.Alignment);

            nint totalSize = listsOffset;

            foreach (KVPair<TypeIndex, int> kvPair in eventTypeSubscriberCapacityMap)
            {
                kvPair.AssumeIndexIsValid();

                nint listSize = EventSubscriberList.GetTotalAllocationSize(kvPair.Value);
                totalSize = Align(totalSize, EventSubscriberList.Alignment) + listSize;
            }

            return totalSize;
        }

        private static nint GetRequiredAllocationSize(ref UnsafeHashMap<TypeIndex, EventListenerListCapacityPair> eventTypeListenerListMap, out nint listsOffset)
        {
            nint mapSize = EventSubscriptionMap.GetTotalAllocationSize(eventTypeListenerListMap.Count);
            listsOffset = Align(mapSize, EventSubscriberList.Alignment);

            nint totalSize = listsOffset;

            foreach (KVPair<TypeIndex, EventListenerListCapacityPair> kvPair in eventTypeListenerListMap)
            {
                kvPair.AssumeIndexIsValid();
                int requiredCapacity = kvPair.Value.ListenerRequiredCapacity;

                nint listOffset = Align(totalSize, EventSubscriberList.Alignment);
                nint listSize = EventSubscriberList.GetTotalAllocationSize(requiredCapacity);

                totalSize = Align(totalSize, EventSubscriberList.Alignment) + listSize;
            }

            return totalSize;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int AllocationSizeToStorageLength(nint size)
        {
            Assert.AreEqual(1, sizeof(StorageBufferElement));
            nint result = (size + sizeof(StorageBufferElement) - 1) / sizeof(StorageBufferElement);

            Assert.IsTrue(result <= int.MaxValue);
            return (int)result;
        }
    }
}
