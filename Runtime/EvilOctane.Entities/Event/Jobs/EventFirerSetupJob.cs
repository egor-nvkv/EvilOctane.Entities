using Unity.Assertions;
using Unity.Burst;
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
        public BufferTypeHandle<EventFirer.EventDeclarationBuffer.StableTypeElement> EventStableTypeBufferTypeHandle;

        public AllocatorManager.AllocatorHandle TempAllocator;
        public EntityCommandBuffer.ParallelWriter CommandBuffer;

        public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
        {
            Assert.IsFalse(useEnabledMask);

            Entity* entityPtr = chunk.GetEntityDataPtrRO(EntityTypeHandle);

            // Remove setup component
            CommandBuffer.RemoveComponent<EventFirer.EventDeclarationBuffer.StableTypeElement>(unfilteredChunkIndex, entityPtr, chunk.Count);

            // Add runtime components
            ComponentTypeSet componentTypeSet = EventSystemInternal.GetEventFirerComponentTypeSet();
            CommandBuffer.AddComponent(unfilteredChunkIndex, entityPtr, chunk.Count, componentTypeSet);

            // Setup runtime components

            BufferAccessor<EventFirer.EventDeclarationBuffer.StableTypeElement> eventStableTypeBufferAccessor = chunk.GetBufferAccessorRO(ref EventStableTypeBufferTypeHandle);

            UnsafeHashMap<TypeIndex, int> eventTypeListenerCapacityMap = UnsafeHashMapUtility.CreateHashMap<TypeIndex, int>(16, 16, TempAllocator);

            for (int entityIndex = 0; entityIndex != chunk.Count; ++entityIndex)
            {
                Entity entity = entityPtr[entityIndex];
                DynamicBuffer<EventFirer.EventDeclarationBuffer.StableTypeElement> eventStableTypeBuffer = eventStableTypeBufferAccessor[entityIndex];

                SetupEventFirer(
                    unfilteredChunkIndex,
                    entity,
                    eventStableTypeBuffer.AsSpanRO(),
                    ref eventTypeListenerCapacityMap);

                if (entityIndex != chunk.Count - 1)
                {
                    eventTypeListenerCapacityMap.Clear();
                }
            }
        }

        private void SetupEventFirer(
            int sortKey,
            Entity entity,
            UnsafeSpan<EventFirer.EventDeclarationBuffer.StableTypeElement> eventStableTypeSpanRO,
            ref UnsafeHashMap<TypeIndex, int> eventTypeListenerCapacityMap)
        {
            // Deserialize Event Types
            EventDeclarationFunctions.DeserializeEventTypes(eventStableTypeSpanRO, ref eventTypeListenerCapacityMap);

            // Setup Event Listener Registry
            DynamicBuffer<EventFirerInternal.EventSubscriptionRegistry.Storage> registryStorage = CommandBuffer.SetBuffer<EventFirerInternal.EventSubscriptionRegistry.Storage>(sortKey, entity);
            EventSubscriptionRegistryFunctions.Create(registryStorage, ref eventTypeListenerCapacityMap);
        }
    }
}
