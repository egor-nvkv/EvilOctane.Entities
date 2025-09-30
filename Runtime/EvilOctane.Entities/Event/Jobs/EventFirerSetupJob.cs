using Unity.Assertions;
using Unity.Burst;
using Unity.Burst.CompilerServices;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Entities.LowLevel.Unsafe;

namespace EvilOctane.Entities.Internal
{
    [BurstCompile]
    public unsafe struct EventFirerSetupJob : IJobChunk
    {
        [ReadOnly]
        public EntityTypeHandle EntityTypeHandle;

        [ReadOnly]
        public BufferTypeHandle<EventSetup.FirerDeclaredEventTypeBufferElement> SetupDeclaredEventTypeBufferTypeHandle;

        public AllocatorManager.AllocatorHandle TempAllocator;
        public EntityCommandBuffer.ParallelWriter CommandBuffer;

        private static void DeserializeEventTypes(
            UnsafeSpan<EventSetup.FirerDeclaredEventTypeBufferElement> setupDeclaredEventTypeSpanRO,
            ref UnsafeHashMap<TypeIndex, int> eventTypeSubscriberCapacityMap)
        {
            HashMapHelperRef<TypeIndex> mapHelper = eventTypeSubscriberCapacityMap.GetHelperRef();
            mapHelper.EnsureCapacity(setupDeclaredEventTypeSpanRO.Length);

            for (int eventIndex = 0; eventIndex != setupDeclaredEventTypeSpanRO.Length; ++eventIndex)
            {
                EventSetup.FirerDeclaredEventTypeBufferElement setupDeclaredEventType = setupDeclaredEventTypeSpanRO[eventIndex];

                bool typeIndexFound = TypeManager.TryGetTypeIndexFromStableTypeHash(setupDeclaredEventType.EventStableTypeHash, out TypeIndex eventTypeIndex);

                if (Hint.Unlikely(!typeIndexFound))
                {
                    // Event Type Index not found
                    continue;
                }

                // Register Event Type
                _ = mapHelper.TryAddNoResize(eventTypeIndex, setupDeclaredEventType.ListenerListStartingCapacity);
            }
        }

        public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
        {
            Assert.IsFalse(useEnabledMask);

            Entity* entityPtr = chunk.GetEntityDataPtrRO(EntityTypeHandle);

            // Remove setup component
            CommandBuffer.RemoveComponent<EventSetup.FirerDeclaredEventTypeBufferElement>(unfilteredChunkIndex, entityPtr, chunk.Count);

            // Add runtime components
            ComponentTypeSet eventFirerComponentTypeSet = EventSystem.GetEventFirerComponentTypeSet();
            CommandBuffer.AddComponent(unfilteredChunkIndex, entityPtr, chunk.Count, eventFirerComponentTypeSet);

            // Setup runtime components

            BufferAccessor<EventSetup.FirerDeclaredEventTypeBufferElement> setupDeclaredEventTypeBufferAccessor = chunk.GetBufferAccessorRO(ref SetupDeclaredEventTypeBufferTypeHandle);

            UnsafeHashMap<TypeIndex, int> eventTypeSubscriberCapacityMap = UnsafeHashMapUtility.CreateHashMap<TypeIndex, int>(16, 16, TempAllocator);

            for (int entityIndex = 0; entityIndex != chunk.Count; ++entityIndex)
            {
                Entity entity = entityPtr[entityIndex];
                DynamicBuffer<EventSetup.FirerDeclaredEventTypeBufferElement> setupDeclaredEventTypeBuffer = setupDeclaredEventTypeBufferAccessor[entityIndex];

                SetupEventFirer(
                    unfilteredChunkIndex,
                    entity,
                    setupDeclaredEventTypeBuffer.AsSpanRO(),
                    ref eventTypeSubscriberCapacityMap);

                if (entityIndex != chunk.Count - 1)
                {
                    eventTypeSubscriberCapacityMap.Clear();
                }
            }
        }

        private void SetupEventFirer(
            int sortKey,
            Entity entity,
            UnsafeSpan<EventSetup.FirerDeclaredEventTypeBufferElement> setupDeclaredEventTypeSpanRO,
            ref UnsafeHashMap<TypeIndex, int> eventTypeSubscriberCapacityMap)
        {
            // Deserialize Event Types
            DeserializeEventTypes(setupDeclaredEventTypeSpanRO, ref eventTypeSubscriberCapacityMap);

            // Setup Event Subscription Registry
            DynamicBuffer<EventSubscriptionRegistry.StorageBufferElement> eventSubscriptionRegistryStorage = CommandBuffer.SetBuffer<EventSubscriptionRegistry.StorageBufferElement>(sortKey, entity);
            EventSubscriptionRegistry.Create(eventSubscriptionRegistryStorage, ref eventTypeSubscriberCapacityMap);
        }
    }
}
