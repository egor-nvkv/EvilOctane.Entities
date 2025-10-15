using EvilOctane.Collections.LowLevel.Unsafe;
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
using static Unity.Collections.LowLevel.Unsafe.UnsafeUtility;
using static Unity.Collections.LowLevel.Unsafe.UnsafeUtility2;
using EventListenerList = EvilOctane.Collections.LowLevel.Unsafe.InPlaceList<Unity.Entities.Entity>;
using EventListenerListHeader = EvilOctane.Collections.LowLevel.Unsafe.InPlaceListHeader<Unity.Entities.Entity>;
using EventListenerTable = EvilOctane.Collections.LowLevel.Unsafe.InPlaceSwissTable<Unity.Entities.TypeIndex, EvilOctane.Entities.Internal.EventListenerListOffset, EvilOctane.Collections.XXH3PodHasher<Unity.Entities.TypeIndex>>;
using EventListenerTableHeader = EvilOctane.Collections.LowLevel.Unsafe.InPlaceSwissTableHeader<Unity.Entities.TypeIndex, EvilOctane.Entities.Internal.EventListenerListOffset>;

namespace EvilOctane.Entities.Internal
{
    public static unsafe class EventSubscriptionRegistryAPI
    {
        public static int ListenerListDefaultInitialCapacity
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                int elementOffset = Align(sizeof(EventListenerListHeader), AlignOf<Entity>());
                int elementCount = ((CacheLineSize / 2) - elementOffset) / sizeof(Entity);

                return math.max(elementCount, 1);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsCreated(DynamicBuffer<Storage> storage)
        {
            return storage.Length >= sizeof(EventListenerTableHeader);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static EventListenerTableHeader* GetListenerMap(DynamicBuffer<Storage> storage, bool readOnly = false)
        {
            void* listenerTable = readOnly ? storage.GetUnsafeReadOnlyPtr() : storage.GetUnsafePtr();

            CheckIsAligned<EventListenerTableHeader>(listenerTable);
            return (EventListenerTableHeader*)listenerTable;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static nint GetFirstListenerListOffset(int listenerTableCount)
        {
            nint mapSize = EventListenerTable.GetAllocationSize(listenerTableCount, out _);
            return Align(mapSize, EventListenerList.BufferAlignment);
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

            nint requiredSize = GetRequiredAllocationSize(ref eventTypeListenerCapacityMap, out int actualCapacity, out nint firstListOffset);
            int requiredLength = AllocationSizeToStorageLength(requiredSize);

            storage.ResizeUninitializedTrashOldData(requiredLength);
            byte* storagePtr = (byte*)storage.GetUnsafePtr();

            // Create Listener map

            EventListenerTableHeader* listenerTable = GetListenerMap(storage);
            EventListenerTable.Initialize(listenerTable, actualCapacity);

            // Create Listener lists

            nint offset = firstListOffset;

            foreach (KVPair<TypeIndex, int> kvPair in eventTypeListenerCapacityMap)
            {
                kvPair.AssumeIndexIsValid();

                // Create list

                offset = Align(offset, EventListenerList.BufferAlignment);
                EventListenerListHeader* list = (EventListenerListHeader*)(storagePtr + offset);

                int listenerListCapacity = math.max(kvPair.Value, 1);
                EventListenerList.Create(list, listenerListCapacity);

                // Register list

                nint listOffset = offset - firstListOffset;
                EventListenerTable.AddUncheckedNoResize(listenerTable, kvPair.Key) = listOffset;

                offset += EventListenerList.GetAllocationSize(listenerListCapacity);
            }
        }

        public static void CopyTo(DynamicBuffer<Storage> storage, ref UnsafeHashMap<TypeIndex, EventListenerListCapacityPair> eventTypeListenerListMap, AllocatorManager.AllocatorHandle tempAllocator)
        {
            Assert.IsTrue(IsCreated(storage));
            CopyTo(storage, default, ref eventTypeListenerListMap, tempAllocator, skipDestroyed: false);
        }

        public static void CopyToSkipDestroyed(DynamicBuffer<Storage> storage, BufferLookup<EventListener.EventDeclarationBuffer.TypeElement> entityLookup, ref UnsafeHashMap<TypeIndex, EventListenerListCapacityPair> eventTypeListenerListMap, AllocatorManager.AllocatorHandle tempAllocator)
        {
            Assert.IsTrue(IsCreated(storage));
            CopyTo(storage, entityLookup, ref eventTypeListenerListMap, tempAllocator, skipDestroyed: true);
        }

        public static void CopyFrom(DynamicBuffer<Storage> storage, ref UnsafeHashMap<TypeIndex, EventListenerListCapacityPair> eventTypeListenerListMap, bool compact = false)
        {
            // Calculate size

            nint requiredSize = GetRequiredAllocationSize(ref eventTypeListenerListMap, compact: compact, out int actualCapacity, out nint firstListOffset);
            int requiredLength = AllocationSizeToStorageLength(requiredSize);

            if (compact)
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

            EventListenerTableHeader* listenerTable = GetListenerMap(storage);
            EventListenerTable.Initialize(listenerTable, actualCapacity);

            // Create Listener lists

            nint offset = firstListOffset;

            foreach (KVPair<TypeIndex, EventListenerListCapacityPair> kvPair in eventTypeListenerListMap)
            {
                kvPair.AssumeIndexIsValid();
                EventListenerListCapacityPair listenerListCapacityPair = kvPair.Value;

                // Create list

                offset = Align(offset, EventListenerList.BufferAlignment);
                EventListenerListHeader* list = (EventListenerListHeader*)(storagePtr + offset);

                int requiredCapacity = GetListRequiredCapacity(kvPair.Value, compact: compact);
                EventListenerList.Create(list, requiredCapacity);

                // Copy list

                if (listenerListCapacityPair.Length != 0)
                {
                    EventListenerList.AddRangeNoResize(list, listenerListCapacityPair.AsSpan());
                }

                // Register list

                nint listOffset = offset - firstListOffset;
                EventListenerTable.AddUncheckedNoResize(listenerTable, kvPair.Key) = listOffset;

                offset += EventListenerList.GetAllocationSize(requiredCapacity);
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
                int capacity = listenerListCapacityPair.RequiredCapacity;
                listenerListCapacityPair = new EventListenerListCapacityPair(capacity, capacity, tempAllocator);
            }

            listenerListCapacityPair.AddNoResize(listenerEntity);
        }

        public static void Subscribe(ref UnsafeHashMap<TypeIndex, EventListenerListCapacityPair> eventTypeListenerListMap, Entity listenerEntity, UnsafeSpan<TypeIndex> eventTypeIndexSpanRO, AllocatorManager.AllocatorHandle tempAllocator)
        {
            foreach (TypeIndex eventTypeIndex in eventTypeIndexSpanRO)
            {
                Subscribe(ref eventTypeListenerListMap, listenerEntity, eventTypeIndex, tempAllocator);
            }
        }

        public static bool TrySubscribeNoResize(DynamicBuffer<Storage> storage, Entity listenerEntity, TypeIndex eventTypeIndex)
        {
            EventListenerTableHeader* listenerTable = GetListenerMap(storage);
            ref EventListenerListOffset listOffset = ref EventListenerTable.TryGet(listenerTable, eventTypeIndex, out bool exists);

            if (!exists)
            {
                // Event Type not declared
                return true;
            }

            nint firstListOffset = GetFirstListenerListOffset(listenerTable->Count);
            EventListenerListHeader* list = listOffset.GetList(listenerTable, firstListOffset);

            bool alreadySubscribed = EventListenerList.AsSpan(list).Contains(listenerEntity);

            if (Hint.Unlikely(alreadySubscribed))
            {
                // Already subscribed
                return true;
            }

            bool isFull = list->Length == list->Capacity;

            if (Hint.Unlikely(isFull))
            {
                // List is full
                return false;
            }

            EventListenerList.AddNoResize(list, listenerEntity);
            return true;
        }

        public static bool TrySubscribeNoResize(DynamicBuffer<Storage> storage, Entity listenerEntity, UnsafeSpan<TypeIndex> eventTypeIndexSpanRO, out int processedCount)
        {
            processedCount = 0;

            foreach (TypeIndex eventTypeIndex in eventTypeIndexSpanRO)
            {
                if (!TrySubscribeNoResize(storage, listenerEntity, eventTypeIndex))
                {
                    // Full
                    return false;
                }

                ++processedCount;
            }

            return processedCount == eventTypeIndexSpanRO.Length;
        }

        public static void Unsubscribe(DynamicBuffer<Storage> storage, Entity listenerEntity, TypeIndex eventTypeIndex)
        {
            EventListenerTableHeader* listenerTable = GetListenerMap(storage);
            ref EventListenerListOffset listOffset = ref EventListenerTable.TryGet(listenerTable, eventTypeIndex, out bool exists);

            if (!exists)
            {
                // Event Type not declared
                return;
            }

            nint firstListOffset = GetFirstListenerListOffset(listenerTable->Count);
            EventListenerListHeader* list = listOffset.GetList(listenerTable, firstListOffset);

            int index = EventListenerList.AsSpan(list).IndexOf(listenerEntity);

            if (Hint.Unlikely(index < 0))
            {
                // Not subscribed
                return;
            }

            EventListenerList.RemoveAtSwapBack(list, index);
        }

        public static void Unsubscribe(DynamicBuffer<Storage> storage, Entity listenerEntity, UnsafeSpan<TypeIndex> eventTypeIndexSpanRO)
        {
            foreach (TypeIndex eventTypeIndex in eventTypeIndexSpanRO)
            {
                Unsubscribe(storage, listenerEntity, eventTypeIndex);
            }
        }

        public static void Unsubscribe(ref UnsafeHashMap<TypeIndex, EventListenerListCapacityPair> eventTypeListenerListMap, Entity listenerEntity, TypeIndex eventTypeIndex)
        {
            HashMapHelperRef<TypeIndex> mapHelper = eventTypeListenerListMap.GetHelperRef();
            ref EventListenerListCapacityPair listenerListCapacityPair = ref mapHelper.TryGetValueRef<EventListenerListCapacityPair>(eventTypeIndex, out bool exists);

            if (!exists)
            {
                // Event Type not declared
                return;
            }

            int index = listenerListCapacityPair.AsSpan().IndexOf(listenerEntity);

            if (index < 0)
            {
                // Not subscribed
                return;
            }

            listenerListCapacityPair.RemoveAtSwapBack(index);
        }

        public static void Unsubscribe(ref UnsafeHashMap<TypeIndex, EventListenerListCapacityPair> eventTypeListenerListMap, Entity listenerEntity, UnsafeSpan<TypeIndex> eventTypeIndexSpanRO)
        {
            foreach (TypeIndex eventTypeIndex in eventTypeIndexSpanRO)
            {
                Unsubscribe(ref eventTypeListenerListMap, listenerEntity, eventTypeIndex);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int AllocationSizeToStorageLength(nint size)
        {
            nint result = (size + sizeof(Storage) - 1) / sizeof(Storage);

            Assert.IsTrue(result <= int.MaxValue);
            return (int)result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int GetListRequiredCapacity(EventListenerListCapacityPair pair, bool compact)
        {
            if (compact)
            {
                // Use actual length
                return pair.Length;
            }
            else
            {
                // Use max capacity
                return math.max(pair.Length, pair.RequiredCapacity);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static nint GetRequiredAllocationSize(ref UnsafeHashMap<TypeIndex, int> eventTypeListenerCapacityMap, out int actualCapacity, out nint firstListOffset)
        {
            nint mapSize = EventListenerTable.GetAllocationSize(eventTypeListenerCapacityMap.Count, out actualCapacity);
            firstListOffset = Align(mapSize, EventListenerList.BufferAlignment);

            nint totalSize = firstListOffset;

            foreach (KVPair<TypeIndex, int> kvPair in eventTypeListenerCapacityMap)
            {
                kvPair.AssumeIndexIsValid();

                nint listSize = EventListenerList.GetAllocationSize(kvPair.Value);
                totalSize = Align(totalSize, EventListenerList.BufferAlignment) + listSize;
            }

            return totalSize;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static nint GetRequiredAllocationSize(ref UnsafeHashMap<TypeIndex, EventListenerListCapacityPair> eventTypeListenerListMap, bool compact, out int actualCapacity, out nint firstListOffset)
        {
            nint mapSize = EventListenerTable.GetAllocationSize(eventTypeListenerListMap.Count, out actualCapacity);
            firstListOffset = Align(mapSize, EventListenerList.BufferAlignment);

            nint totalSize = firstListOffset;

            foreach (KVPair<TypeIndex, EventListenerListCapacityPair> kvPair in eventTypeListenerListMap)
            {
                kvPair.AssumeIndexIsValid();
                int requiredCapacity = GetListRequiredCapacity(kvPair.Value, compact: compact);

                nint listOffset = Align(totalSize, EventListenerList.BufferAlignment);
                nint listSize = EventListenerList.GetAllocationSize(requiredCapacity);

                totalSize = Align(totalSize, EventListenerList.BufferAlignment) + listSize;
            }

            return totalSize;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void CopyTo(DynamicBuffer<Storage> storage, BufferLookup<EventListener.EventDeclarationBuffer.TypeElement> entityLookup, ref UnsafeHashMap<TypeIndex, EventListenerListCapacityPair> eventTypeListenerListMap, AllocatorManager.AllocatorHandle tempAllocator, bool skipDestroyed)
        {
            EventListenerTableHeader* listenerTable = GetListenerMap(storage, readOnly: true);
            nint firstListOffset = GetFirstListenerListOffset(listenerTable->Count);

            HashMapHelperRef<TypeIndex> mapHelper = eventTypeListenerListMap.GetHelperRef();
            mapHelper.EnsureCapacity(listenerTable->Count);

            UnsafeSwissTableEnumerator<TypeIndex, EventListenerListOffset> enumerator = EventListenerTable.GetEnumerator(listenerTable);

            while (enumerator.MoveNext())
            {
                Pointer<Collections.KeyValue<TypeIndex, EventListenerListOffset>> kvPair = enumerator.Current;
                EventListenerListHeader* list = kvPair.Ref.Value.GetList(listenerTable, firstListOffset);

                ref EventListenerListCapacityPair listenerListCapacityPair = ref mapHelper.GetOrAddValueNoResize<EventListenerListCapacityPair>(kvPair.Ref.Key, out bool added);

                if (added)
                {
                    bool sourceListIsEmpty = list->Length == 0;

                    if (sourceListIsEmpty)
                    {
                        // Empty

                        listenerListCapacityPair = new EventListenerListCapacityPair()
                        {
                            // Keep original capacity
                            RequiredCapacity = list->Capacity
                        };

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
                        listenerListCapacityPair.EnsureCapacity(list->Length, tempAllocator, keepOldData: false);

                        // Keep original capacity
                        listenerListCapacityPair.RequiredCapacity = list->Capacity;
                    }
                    else
                    {
                        // Allocate
                        goto Allocate;
                    }
                }

            Copy:
                if (skipDestroyed)
                {
                    // Skip destroyed listeners

                    foreach (Entity listenerEntity in EventListenerList.AsSpan(list))
                    {
                        bool exists = entityLookup.EntityExists(listenerEntity);

                        if (Hint.Unlikely(!exists))
                        {
#if ENABLE_PROFILER
                            // Phantom Listener
                            ++EventSystemProfiler.PhantomListenersCounter.Data.Value;
#endif

                            // Destroyed
                            continue;
                        }

                        listenerListCapacityPair.AddNoResize(listenerEntity);
                    }
                }
                else
                {
                    // Copy all listeners
                    listenerListCapacityPair.AddRangeNoResize(EventListenerList.AsSpan(list));
                }

                continue;

            Allocate:
                listenerListCapacityPair = new EventListenerListCapacityPair(list->Length, list->Capacity, tempAllocator);
                goto Copy;
            }
        }
    }
}
