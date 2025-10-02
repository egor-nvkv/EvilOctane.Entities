using System;
using System.Runtime.CompilerServices;
using Unity.Assertions;
using Unity.Burst.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Entities.LowLevel.Unsafe;
using Unity.Mathematics;
using static EvilOctane.Entities.Internal.EventFirerInternal.EventSubscriptionRegistry;
using static Unity.Collections.CollectionHelper;
using static Unity.Collections.CollectionHelper2;
using static Unity.Collections.LowLevel.Unsafe.UnsafeUtility2;
using EventListenerList = Unity.Collections.LowLevel.Unsafe.InlineList<Unity.Entities.Entity>;
using EventListenerListHeader = Unity.Collections.LowLevel.Unsafe.InlineListHeader<Unity.Entities.Entity>;
using EventListenerMap = Unity.Collections.LowLevel.Unsafe.InlineHashMap<Unity.Entities.TypeIndex, EvilOctane.Entities.Internal.EventListenerListOffset>;
using EventListenerMapHeader = Unity.Collections.LowLevel.Unsafe.InlineHashMapHeader<Unity.Entities.TypeIndex>;

namespace EvilOctane.Entities.Internal
{
    public static unsafe class EventSubscriptionRegistryFunctions
    {
        public static int Alignment
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => math.max(EventListenerMap.Alignment, EventListenerList.Alignment);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsCreated(DynamicBuffer<Storage> storage)
        {
            return storage.Length >= sizeof(EventListenerMapHeader);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool NeedsTrimming(DynamicBuffer<Storage> storage, int actualLength)
        {
            return storage.Length / 2 >= actualLength;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static EventListenerMapHeader* GetListenerMap(DynamicBuffer<Storage> storage, bool readOnly = false)
        {
            void* listenerMap = readOnly ? storage.GetUnsafeReadOnlyPtr() : storage.GetUnsafePtr();

            CheckIsAligned<EventListenerMapHeader>(listenerMap);
            return (EventListenerMapHeader*)listenerMap;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static nint GetFirstListenerListOffset(EventListenerMapHeader* listenerMap)
        {
            nint mapSize = EventListenerMap.GetTotalAllocationSize(listenerMap->Count);
            return Align(mapSize, EventListenerList.Alignment);
        }

        public static void Create(DynamicBuffer<Storage> storage, ref UnsafeHashMap<TypeIndex, int> eventTypeListenerCapacityMap)
        {
            Assert.IsTrue(storage.IsEmpty);

            if (Hint.Unlikely(eventTypeListenerCapacityMap.Count == 0))
            {
                // Empty
                return;
            }

            // Calculate size

            nint requiredSize = GetRequiredAllocationSize(ref eventTypeListenerCapacityMap, out nint listsOffset);
            storage.ResizeUninitializedTrashOldData(AllocationSizeToStorageLength(requiredSize));

            byte* storagePtr = (byte*)storage.GetUnsafePtr();

            // Create Listener map

            EventListenerMapHeader* listenerMap = GetListenerMap(storage);
            EventListenerMap.Create(listenerMap, eventTypeListenerCapacityMap.Count);

            // Create Listener lists

            nint offset = listsOffset;

            foreach (KVPair<TypeIndex, int> kvPair in eventTypeListenerCapacityMap)
            {
                kvPair.AssumeIndexIsValid();

                // Create list

                offset = Align(offset, EventListenerList.Alignment);
                EventListenerListHeader* list = (EventListenerListHeader*)(storagePtr + offset);

                int listenerListCapacity = math.max(kvPair.Value, 1);
                EventListenerList.Create(list, listenerListCapacity);

                // Register list

                nint listOffset = offset - listsOffset;
                EventListenerMap.AddUncheckedNoResize(listenerMap, kvPair.Key, listOffset);

                offset += EventListenerList.GetTotalAllocationSize(listenerListCapacity);
            }
        }

        public static void CopyTo(DynamicBuffer<Storage> storage, BufferLookup<EventListener.EventDeclarationBuffer.TypeElement> entityLookup, ref UnsafeHashMap<TypeIndex, EventListenerListCapacityPair> eventTypeListenerListMap, AllocatorManager.AllocatorHandle tempAllocator)
        {
            Assert.IsTrue(IsCreated(storage));

            EventListenerMapHeader* listenerMap = GetListenerMap(storage, readOnly: true);

            HashMapHelperRef<TypeIndex> mapHelper = eventTypeListenerListMap.GetHelperRef();
            mapHelper.EnsureCapacity(listenerMap->Count);

            EventListenerMap.Enumerator enumerator = EventListenerMap.GetEnumerator(listenerMap);
            nint firstListOffset = GetFirstListenerListOffset(listenerMap);

            while (enumerator.MoveNext())
            {
                InlineHashMapKVPair<TypeIndex, EventListenerListOffset> kvPair = enumerator.Current;
                EventListenerListHeader* list = kvPair.ValueRefRW.GetList(listenerMap, firstListOffset);

                ref EventListenerListCapacityPair listenerListCapacityPair = ref mapHelper.GetOrAddValueNoResize<EventListenerListCapacityPair>(kvPair.Key, out bool added);

                // Keep original capacity
                listenerListCapacityPair.ListenerListStartingCapacity = list->Capacity;

                if (added)
                {
                    bool sourceListIsEmpty = list->Length == 0;

                    if (sourceListIsEmpty)
                    {
                        // Empty
                        listenerListCapacityPair.ListenerListPtr = null;
                        listenerListCapacityPair.ListenerListLength = 0;
                        listenerListCapacityPair.ListenerListCapacity = 0;
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
                        // TODO: trash old data
                        listenerListCapacityPair.EnsureCapacity(list->Length, tempAllocator);
                    }
                    else
                    {
                        // Allocate
                        goto Allocate;
                    }
                }

            Copy:
                foreach (Entity listenerEntity in EventListenerList.AsSpan(list))
                {
                    bool exists = entityLookup.EntityExists(listenerEntity);

                    if (Hint.Unlikely(!exists))
                    {
                        // TODO: profile counter

                        // Destroyed
                        continue;
                    }

                    listenerListCapacityPair.AddNoResize(listenerEntity);
                }

                continue;

            Allocate:
                listenerListCapacityPair = new EventListenerListCapacityPair(list->Length, tempAllocator);
                goto Copy;
            }
        }

        public static void CopyFrom(DynamicBuffer<Storage> storage, ref UnsafeHashMap<TypeIndex, EventListenerListCapacityPair> eventTypeListenerListMap)
        {
            // Calculate size

            nint requiredSize = GetRequiredAllocationSize(ref eventTypeListenerListMap, out nint listsOffset);
            int requiredLength = AllocationSizeToStorageLength(requiredSize);

            if (NeedsTrimming(storage, requiredLength))
            {
                // Trim

                storage.SetLengthNoResize(requiredLength);
                storage.TrimExcessTrashOldData();
            }
            else
            {
                // Resize
                storage.ResizeUninitializedTrashOldData(requiredLength);
            }

            byte* storagePtr = (byte*)storage.GetUnsafePtr();

            // Create Listener map

            EventListenerMapHeader* listenerMap = GetListenerMap(storage);
            EventListenerMap.Create(listenerMap, eventTypeListenerListMap.Count);

            // Create Listener lists

            nint offset = listsOffset;

            foreach (KVPair<TypeIndex, EventListenerListCapacityPair> kvPair in eventTypeListenerListMap)
            {
                kvPair.AssumeIndexIsValid();
                EventListenerListCapacityPair listenerListCapacityPair = kvPair.Value;

                // Create list

                offset = Align(offset, EventListenerList.Alignment);
                EventListenerListHeader* list = (EventListenerListHeader*)(storagePtr + offset);

                int requiredCapacity = kvPair.Value.ListenerListRequiredCapacity;
                EventListenerList.Create(list, requiredCapacity);

                // Copy list

                if (listenerListCapacityPair.ListenerListLength != 0)
                {
                    EventListenerList.AddRangeNoResize(list, listenerListCapacityPair.AsSpan());
                }

                // Register list

                nint listOffset = offset - listsOffset;
                EventListenerMap.AddUncheckedNoResize(listenerMap, kvPair.Key, listOffset);

                offset += EventListenerList.GetTotalAllocationSize(requiredCapacity);
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
                    // Already subscribed
                    return;
                }

                listenerListCapacityPair.EnsureSlack(4, tempAllocator);
            }
            else
            {
                // Allocate
                int capacity = listenerListCapacityPair.ListenerListStartingCapacity;
                listenerListCapacityPair = new EventListenerListCapacityPair(capacity, tempAllocator);
            }

            listenerListCapacityPair.AddNoResize(listenerEntity);
        }

        public static void Subscribe(ref UnsafeHashMap<TypeIndex, EventListenerListCapacityPair> eventTypeListenerListMap, Entity listenerEntity, UnsafeSpan<TypeIndex> listenerDeclaredEventTypeSpanRO, AllocatorManager.AllocatorHandle tempAllocator)
        {
            foreach (TypeIndex listenerDeclaredEvent in listenerDeclaredEventTypeSpanRO)
            {
                Subscribe(ref eventTypeListenerListMap, listenerEntity, listenerDeclaredEvent, tempAllocator);
            }
        }

        public static bool TrySubscribeNoResize(DynamicBuffer<Storage> storage, Entity listenerEntity, TypeIndex eventTypeIndex)
        {
            EventListenerMapHeader* listenerMap = GetListenerMap(storage);
            bool eventTypeDeclared = EventListenerMap.TryGetValue(listenerMap, eventTypeIndex, out EventListenerListOffset listOffset);

            if (!eventTypeDeclared)
            {
                // Event Type not declared
                return true;
            }

            nint firstListOffset = GetFirstListenerListOffset(listenerMap);
            EventListenerListHeader* list = listOffset.GetList(listenerMap, firstListOffset);

            bool alreadySubscribed = EventListenerList.AsSpan(list).Contains(listenerEntity);

            if (Hint.Unlikely(alreadySubscribed))
            {
                // Already subscribed
                return true;
            }

            bool isFull = list->Capacity == list->Length;

            if (Hint.Unlikely(isFull))
            {
                // List is full
                return false;
            }

            EventListenerList.AddNoResize(list, listenerEntity);
            return true;
        }

        public static bool TrySubscribeNoResize(DynamicBuffer<Storage> storage, Entity listenerEntity, UnsafeSpan<TypeIndex> listenerDeclaredEventTypeSpanRO, out int processedCount)
        {
            processedCount = 0;

            foreach (TypeIndex listenerDeclaredEvent in listenerDeclaredEventTypeSpanRO)
            {
                if (!TrySubscribeNoResize(storage, listenerEntity, listenerDeclaredEvent))
                {
                    // Full
                    return false;
                }

                ++processedCount;
            }

            return processedCount == listenerDeclaredEventTypeSpanRO.Length;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int AllocationSizeToStorageLength(nint size)
        {
            nint result = (size + sizeof(Storage) - 1) / sizeof(Storage);

            Assert.IsTrue(result <= int.MaxValue);
            return (int)result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static nint GetRequiredAllocationSize(ref UnsafeHashMap<TypeIndex, int> eventTypeListenerCapacityMap, out nint listsOffset)
        {
            nint mapSize = EventListenerMap.GetTotalAllocationSize(eventTypeListenerCapacityMap.Count);
            listsOffset = Align(mapSize, EventListenerList.Alignment);

            nint totalSize = listsOffset;

            foreach (KVPair<TypeIndex, int> kvPair in eventTypeListenerCapacityMap)
            {
                kvPair.AssumeIndexIsValid();

                nint listSize = EventListenerList.GetTotalAllocationSize(kvPair.Value);
                totalSize = Align(totalSize, EventListenerList.Alignment) + listSize;
            }

            return totalSize;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static nint GetRequiredAllocationSize(ref UnsafeHashMap<TypeIndex, EventListenerListCapacityPair> eventTypeListenerListMap, out nint listsOffset)
        {
            nint mapSize = EventListenerMap.GetTotalAllocationSize(eventTypeListenerListMap.Count);
            listsOffset = Align(mapSize, EventListenerList.Alignment);

            nint totalSize = listsOffset;

            foreach (KVPair<TypeIndex, EventListenerListCapacityPair> kvPair in eventTypeListenerListMap)
            {
                kvPair.AssumeIndexIsValid();
                int requiredCapacity = kvPair.Value.ListenerListRequiredCapacity;

                nint listOffset = Align(totalSize, EventListenerList.Alignment);
                nint listSize = EventListenerList.GetTotalAllocationSize(requiredCapacity);

                totalSize = Align(totalSize, EventListenerList.Alignment) + listSize;
            }

            return totalSize;
        }
    }
}
