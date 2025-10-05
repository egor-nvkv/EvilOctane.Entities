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
            return storage.Length >= sizeof(EventListenerMapHeader);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static EventListenerMapHeader* GetListenerMap(DynamicBuffer<Storage> storage, bool readOnly = false)
        {
            void* listenerMap = readOnly ? storage.GetUnsafeReadOnlyPtr() : storage.GetUnsafePtr();

            CheckIsAligned<EventListenerMapHeader>(listenerMap);
            return (EventListenerMapHeader*)listenerMap;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static nint GetFirstListenerListOffset(int listenerMapCount)
        {
            nint mapSize = EventListenerMap.GetTotalAllocationSize(listenerMapCount);
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

            nint requiredSize = GetRequiredAllocationSize(ref eventTypeListenerCapacityMap, out nint firstListOffset);
            int requiredLength = AllocationSizeToStorageLength(requiredSize);

            storage.ResizeUninitializedTrashOldData(requiredLength);
            byte* storagePtr = (byte*)storage.GetUnsafePtr();

            // Create Listener map

            EventListenerMapHeader* listenerMap = GetListenerMap(storage);
            EventListenerMap.Create(listenerMap, eventTypeListenerCapacityMap.Count);

            // Create Listener lists

            nint offset = firstListOffset;

            foreach (KVPair<TypeIndex, int> kvPair in eventTypeListenerCapacityMap)
            {
                kvPair.AssumeIndexIsValid();

                // Create list

                offset = Align(offset, EventListenerList.Alignment);
                EventListenerListHeader* list = (EventListenerListHeader*)(storagePtr + offset);

                int listenerListCapacity = math.max(kvPair.Value, 1);
                EventListenerList.Create(list, listenerListCapacity);

                // Register list

                nint listOffset = offset - firstListOffset;
                EventListenerMap.AddUncheckedNoResize(listenerMap, kvPair.Key, listOffset);

                offset += EventListenerList.GetTotalAllocationSize(listenerListCapacity);
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

            nint requiredSize = GetRequiredAllocationSize(ref eventTypeListenerListMap, compact: compact, out nint firstListOffset);
            int requiredLength = AllocationSizeToStorageLength(requiredSize);

            if (compact)
            {
                // Trim

                storage.SetLengthNoResize(requiredLength);

                // TODO 251005: this sometimes crashes immediately or during world dispose
                //            : original TrimExcess crashes as well
                //            : probably has to do with element pointer getting corrupted before Free
                //storage.TrimExcess();
                //storage.TrimExcessTrashOldData();
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

            nint offset = firstListOffset;

            foreach (KVPair<TypeIndex, EventListenerListCapacityPair> kvPair in eventTypeListenerListMap)
            {
                kvPair.AssumeIndexIsValid();
                EventListenerListCapacityPair listenerListCapacityPair = kvPair.Value;

                // Create list

                offset = Align(offset, EventListenerList.Alignment);
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
            EventListenerMapHeader* listenerMap = GetListenerMap(storage);
            bool eventTypeDeclared = EventListenerMap.TryGetValue(listenerMap, eventTypeIndex, out EventListenerListOffset listOffset);

            if (!eventTypeDeclared)
            {
                // Event Type not declared
                return true;
            }

            nint firstListOffset = GetFirstListenerListOffset(listenerMap->Count);
            EventListenerListHeader* list = listOffset.GetList(listenerMap, firstListOffset);

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
            EventListenerMapHeader* listenerMap = GetListenerMap(storage);
            bool eventTypeDeclared = EventListenerMap.TryGetValue(listenerMap, eventTypeIndex, out EventListenerListOffset listOffset);

            if (!eventTypeDeclared)
            {
                // Event Type not declared
                return;
            }

            nint firstListOffset = GetFirstListenerListOffset(listenerMap->Count);
            EventListenerListHeader* list = listOffset.GetList(listenerMap, firstListOffset);

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
        private static nint GetRequiredAllocationSize(ref UnsafeHashMap<TypeIndex, int> eventTypeListenerCapacityMap, out nint firstListOffset)
        {
            firstListOffset = GetFirstListenerListOffset(eventTypeListenerCapacityMap.Count);
            nint totalSize = firstListOffset;

            foreach (KVPair<TypeIndex, int> kvPair in eventTypeListenerCapacityMap)
            {
                kvPair.AssumeIndexIsValid();

                nint listSize = EventListenerList.GetTotalAllocationSize(kvPair.Value);
                totalSize = Align(totalSize, EventListenerList.Alignment) + listSize;
            }

            return totalSize;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static nint GetRequiredAllocationSize(ref UnsafeHashMap<TypeIndex, EventListenerListCapacityPair> eventTypeListenerListMap, bool compact, out nint firstListOffset)
        {
            firstListOffset = GetFirstListenerListOffset(eventTypeListenerListMap.Count);
            nint totalSize = firstListOffset;

            foreach (KVPair<TypeIndex, EventListenerListCapacityPair> kvPair in eventTypeListenerListMap)
            {
                kvPair.AssumeIndexIsValid();
                int requiredCapacity = GetListRequiredCapacity(kvPair.Value, compact: compact);

                nint listOffset = Align(totalSize, EventListenerList.Alignment);
                nint listSize = EventListenerList.GetTotalAllocationSize(requiredCapacity);

                totalSize = Align(totalSize, EventListenerList.Alignment) + listSize;
            }

            return totalSize;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void CopyTo(DynamicBuffer<Storage> storage, BufferLookup<EventListener.EventDeclarationBuffer.TypeElement> entityLookup, ref UnsafeHashMap<TypeIndex, EventListenerListCapacityPair> eventTypeListenerListMap, AllocatorManager.AllocatorHandle tempAllocator, bool skipDestroyed)
        {
            EventListenerMapHeader* listenerMap = GetListenerMap(storage, readOnly: true);
            nint firstListOffset = GetFirstListenerListOffset(listenerMap->Count);

            HashMapHelperRef<TypeIndex> mapHelper = eventTypeListenerListMap.GetHelperRef();
            mapHelper.EnsureCapacity(listenerMap->Count);

            EventListenerMap.Enumerator enumerator = EventListenerMap.GetEnumerator(listenerMap);

            while (enumerator.MoveNext())
            {
                InlineHashMapKVPair<TypeIndex, EventListenerListOffset> kvPair = enumerator.Current;
                EventListenerListHeader* list = kvPair.ValueRefRW.GetList(listenerMap, firstListOffset);

                ref EventListenerListCapacityPair listenerListCapacityPair = ref mapHelper.GetOrAddValueNoResize<EventListenerListCapacityPair>(kvPair.Key, out bool added);

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
