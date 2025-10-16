using EvilOctane.Collections;
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
using EventTypeListenerCapacityTable = EvilOctane.Collections.LowLevel.Unsafe.UnsafeSwissTable<Unity.Entities.TypeIndex, int, EvilOctane.Collections.XXH3PodHasher<Unity.Entities.TypeIndex>>;
using EventTypeListenerListTable = EvilOctane.Collections.LowLevel.Unsafe.UnsafeSwissTable<Unity.Entities.TypeIndex, EvilOctane.Entities.Internal.EventListenerListCapacityPair, EvilOctane.Collections.XXH3PodHasher<Unity.Entities.TypeIndex>>;

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
        public static EventListenerTableHeader* GetListenerTable(DynamicBuffer<Storage> storage, bool readOnly = false)
        {
            void* listenerTable = readOnly ? storage.GetUnsafeReadOnlyPtr() : storage.GetUnsafePtr();

            CheckIsAligned(listenerTable, EventListenerTable.Alignment);
            return (EventListenerTableHeader*)listenerTable;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static nint GetFirstListenerListOffset(int listenerTableCount)
        {
            nint mapSize = EventListenerTable.GetAllocationSize(listenerTableCount, out _);
            return Align(mapSize, EventListenerList.Alignment);
        }

        public static void Create(DynamicBuffer<Storage> storage, ref EventTypeListenerCapacityTable eventTypeListenerCapacityTable)
        {
            Assert.IsTrue(storage.IsEmpty);

            if (Hint.Unlikely(eventTypeListenerCapacityTable.Count == 0))
            {
                // Empty
                return;
            }

            // Calculate size

            nint requiredSize = GetRequiredAllocationSize(ref eventTypeListenerCapacityTable, out int actualCapacity, out nint firstListOffset);
            int requiredLength = AllocationSizeToStorageLength(requiredSize);

            storage.ResizeUninitializedTrashOldData(requiredLength);
            byte* storagePtr = (byte*)storage.GetUnsafePtr();

            // Create Listener map

            EventListenerTableHeader* listenerTable = GetListenerTable(storage);
            EventListenerTable.Initialize(listenerTable, actualCapacity);

            // Create Listener lists

            nint offset = firstListOffset;

            foreach (KeyValueRef<TypeIndex, int> kvPair in eventTypeListenerCapacityTable)
            {
                // Create list

                offset = Align(offset, EventListenerList.Alignment);
                EventListenerListHeader* list = (EventListenerListHeader*)(storagePtr + offset);

                int listenerListCapacity = math.max(kvPair.ValueRef, 1);
                EventListenerList.Create(list, listenerListCapacity);

                // Register list

                nint listOffset = offset - firstListOffset;
                EventListenerTable.AddNoResize(listenerTable, kvPair.KeyRefRO) = listOffset;

                offset += EventListenerList.GetAllocationSize(listenerListCapacity);
            }
        }

        public static void CopyTo(DynamicBuffer<Storage> storage, ref EventTypeListenerListTable eventTypeListenerListTable, AllocatorManager.AllocatorHandle tempAllocator)
        {
            Assert.IsTrue(IsCreated(storage));
            CopyTo(storage, default, ref eventTypeListenerListTable, tempAllocator, skipDestroyed: false);
        }

        public static void CopyToSkipDestroyed(DynamicBuffer<Storage> storage, BufferLookup<EventListener.EventDeclarationBuffer.TypeElement> entityLookup, ref EventTypeListenerListTable eventTypeListenerListTable, AllocatorManager.AllocatorHandle tempAllocator)
        {
            Assert.IsTrue(IsCreated(storage));
            CopyTo(storage, entityLookup, ref eventTypeListenerListTable, tempAllocator, skipDestroyed: true);
        }

        public static void CopyFrom(DynamicBuffer<Storage> storage, ref EventTypeListenerListTable eventTypeListenerListTable, bool compact = false)
        {
            // Calculate size

            nint requiredSize = GetRequiredAllocationSize(ref eventTypeListenerListTable, compact: compact, out int actualCapacity, out nint firstListOffset);
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

            EventListenerTableHeader* listenerTable = GetListenerTable(storage);
            EventListenerTable.Initialize(listenerTable, actualCapacity);

            // Create Listener lists

            nint offset = firstListOffset;

            foreach (KeyValueRef<TypeIndex, EventListenerListCapacityPair> kvPair in eventTypeListenerListTable)
            {
                EventListenerListCapacityPair listenerListCapacityPair = kvPair.ValueRef;

                // Create list

                offset = Align(offset, EventListenerList.Alignment);
                EventListenerListHeader* list = (EventListenerListHeader*)(storagePtr + offset);

                int requiredCapacity = GetListRequiredCapacity(listenerListCapacityPair, compact: compact);
                EventListenerList.Create(list, requiredCapacity);

                // Copy list

                if (listenerListCapacityPair.Length != 0)
                {
                    EventListenerList.AddRangeNoResize(list, listenerListCapacityPair.AsSpan());
                }

                // Register list

                nint listOffset = offset - firstListOffset;
                EventListenerTable.AddNoResize(listenerTable, kvPair.KeyRefRO) = listOffset;

                offset += EventListenerList.GetAllocationSize(requiredCapacity);
            }
        }

        public static void Subscribe(ref EventTypeListenerListTable eventTypeListenerListTable, Entity listenerEntity, TypeIndex eventTypeIndex, AllocatorManager.AllocatorHandle tempAllocator)
        {
            ref EventListenerListCapacityPair listenerListCapacityPair = ref eventTypeListenerListTable.TryGet(eventTypeIndex, out bool exists);

            if (!exists)
            {
                // Event type not declared
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

        public static void Subscribe(ref EventTypeListenerListTable eventTypeListenerListTable, Entity listenerEntity, UnsafeSpan<TypeIndex> eventTypeIndexSpanRO, AllocatorManager.AllocatorHandle tempAllocator)
        {
            foreach (TypeIndex eventTypeIndex in eventTypeIndexSpanRO)
            {
                Subscribe(ref eventTypeListenerListTable, listenerEntity, eventTypeIndex, tempAllocator);
            }
        }

        public static bool TrySubscribeNoResize(DynamicBuffer<Storage> storage, Entity listenerEntity, TypeIndex eventTypeIndex)
        {
            EventListenerTableHeader* listenerTable = GetListenerTable(storage);
            ref EventListenerListOffset listOffset = ref EventListenerTable.TryGet(listenerTable, eventTypeIndex, out bool exists);

            if (!exists)
            {
                // Event type not declared
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
            EventListenerTableHeader* listenerTable = GetListenerTable(storage);
            ref EventListenerListOffset listOffset = ref EventListenerTable.TryGet(listenerTable, eventTypeIndex, out bool exists);

            if (!exists)
            {
                // Event type not declared
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

        public static void Unsubscribe(ref EventTypeListenerListTable eventTypeListenerListTable, Entity listenerEntity, TypeIndex eventTypeIndex)
        {
            ref EventListenerListCapacityPair listenerListCapacityPair = ref eventTypeListenerListTable.TryGet(eventTypeIndex, out bool exists);

            if (!exists)
            {
                // Event type not declared
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

        public static void Unsubscribe(ref EventTypeListenerListTable eventTypeListenerListTable, Entity listenerEntity, UnsafeSpan<TypeIndex> eventTypeIndexSpanRO)
        {
            foreach (TypeIndex eventTypeIndex in eventTypeIndexSpanRO)
            {
                Unsubscribe(ref eventTypeListenerListTable, listenerEntity, eventTypeIndex);
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
        private static nint GetRequiredAllocationSize(ref EventTypeListenerCapacityTable eventTypeListenerCapacityTable, out int actualCapacity, out nint firstListOffset)
        {
            nint mapSize = EventListenerTable.GetAllocationSize(eventTypeListenerCapacityTable.Count, out actualCapacity);
            firstListOffset = Align(mapSize, EventListenerList.Alignment);

            nint totalSize = firstListOffset;

            foreach (KeyValueRef<TypeIndex, int> kvPair in eventTypeListenerCapacityTable)
            {
                nint listSize = EventListenerList.GetAllocationSize(kvPair.ValueRef);
                totalSize = Align(totalSize, EventListenerList.Alignment) + listSize;
            }

            return totalSize;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static nint GetRequiredAllocationSize(ref EventTypeListenerListTable eventTypeListenerListTable, bool compact, out int actualCapacity, out nint firstListOffset)
        {
            nint mapSize = EventListenerTable.GetAllocationSize(eventTypeListenerListTable.Count, out actualCapacity);
            firstListOffset = Align(mapSize, EventListenerList.Alignment);

            nint totalSize = firstListOffset;

            foreach (KeyValueRef<TypeIndex, EventListenerListCapacityPair> kvPair in eventTypeListenerListTable)
            {
                int requiredCapacity = GetListRequiredCapacity(kvPair.ValueRef, compact: compact);

                nint listOffset = Align(totalSize, EventListenerList.Alignment);
                nint listSize = EventListenerList.GetAllocationSize(requiredCapacity);

                totalSize = Align(totalSize, EventListenerList.Alignment) + listSize;
            }

            return totalSize;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void CopyTo(DynamicBuffer<Storage> storage, BufferLookup<EventListener.EventDeclarationBuffer.TypeElement> entityLookup, ref EventTypeListenerListTable eventTypeListenerListTable, AllocatorManager.AllocatorHandle tempAllocator, bool skipDestroyed)
        {
            EventListenerTableHeader* listenerTable = GetListenerTable(storage, readOnly: true);
            nint firstListOffset = GetFirstListenerListOffset(listenerTable->Count);

            eventTypeListenerListTable.EnsureCapacity(listenerTable->Count);

            SwissTable<TypeIndex, EventListenerListOffset>.UnsafeEnumerator enumerator = EventListenerTable.GetEnumerator(listenerTable);

            while (enumerator.MoveNext())
            {
                KeyValueRef<TypeIndex, EventListenerListOffset> kvPair = enumerator.Current;
                EventListenerListHeader* list = kvPair.ValueRef.GetList(listenerTable, firstListOffset);

                ref EventListenerListCapacityPair listenerListCapacityPair = ref eventTypeListenerListTable.GetOrAddNoResize(kvPair.KeyRefRO, out bool added);

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
