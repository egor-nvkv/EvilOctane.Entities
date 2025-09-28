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
    public unsafe struct EventListenerSetupJob : IJobChunk
    {
        [ReadOnly]
        public EntityTypeHandle EntityTypeHandle;

        [ReadOnly]
        public BufferTypeHandle<EventSetup.ListenerDeclaredEventTypeBufferElement> SetupDeclaredEventTypeBufferTypeHandle;

        public AllocatorManager.AllocatorHandle TempAllocator;
        public EntityCommandBuffer.ParallelWriter CommandBuffer;

        public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
        {
            Assert.IsFalse(useEnabledMask);

            Entity* entityPtr = chunk.GetEntityDataPtrRO(EntityTypeHandle);

            // Remove setup component
            CommandBuffer.RemoveComponent<EventSetup.ListenerDeclaredEventTypeBufferElement>(unfilteredChunkIndex, entityPtr, chunk.Count);

            // Add runtime components

            ComponentTypeSet eventListenerComponentTypeSet = EventUtility.GetEventListenerComponentTypeSet();
            CommandBuffer.AddComponent(unfilteredChunkIndex, entityPtr, chunk.Count, eventListenerComponentTypeSet);

            // Setup runtime components

            BufferAccessor<EventSetup.ListenerDeclaredEventTypeBufferElement> setupDeclaredEventTypeBufferAccessor = chunk.GetBufferAccessorRO(ref SetupDeclaredEventTypeBufferTypeHandle);

            for (int entityIndex = 0; entityIndex != chunk.Count; ++entityIndex)
            {
                Entity entity = entityPtr[entityIndex];
                DynamicBuffer<EventSetup.ListenerDeclaredEventTypeBufferElement> setupDeclaredEventTypeBuffer = setupDeclaredEventTypeBufferAccessor[entityIndex];

                SetupEventListener(unfilteredChunkIndex, entity, setupDeclaredEventTypeBuffer.AsSpanRO());
            }
        }

        private void SetupEventListener(
            int sortKey,
            Entity entity,
            UnsafeSpan<EventSetup.ListenerDeclaredEventTypeBufferElement> setupDeclaredEventTypeSpanRO)
        {
            DynamicBuffer<EventSettings.ListenerDeclaredEventTypeBufferElement> declaredEventTypeBufferTypeHandle = CommandBuffer.SetBuffer<EventSettings.ListenerDeclaredEventTypeBufferElement>(sortKey, entity);
            declaredEventTypeBufferTypeHandle.EnsureCapacity(setupDeclaredEventTypeSpanRO.Length);

            for (int eventIndex = 0; eventIndex != setupDeclaredEventTypeSpanRO.Length; ++eventIndex)
            {
                EventSetup.ListenerDeclaredEventTypeBufferElement setupDeclaredEventType = setupDeclaredEventTypeSpanRO[eventIndex];

                if (!TypeManager.TryGetTypeIndexFromStableTypeHash(setupDeclaredEventType.EventStableTypeHash, out TypeIndex eventTypeIndex))
                {
                    // Event Type Index not found
                    continue;
                }

                // Register Event Type

                bool alreadyRegistered = declaredEventTypeBufferTypeHandle.AsSpanRO().Reinterpret<TypeIndex>().Contains(eventTypeIndex);

                if (Hint.Unlikely(alreadyRegistered))
                {
                    // Already registered
                    continue;
                }

                // Register

                _ = declaredEventTypeBufferTypeHandle.Add(new EventSettings.ListenerDeclaredEventTypeBufferElement()
                {
                    EventTypeIndex = eventTypeIndex
                });
            }
        }
    }
}
