using System.Runtime.CompilerServices;
using Unity.Burst.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Entities.LowLevel.Unsafe;

namespace EvilOctane.Entities
{
    public static class EntityBufferUtility
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UnsafeList<Entity> ExtractEntityList<T>(BufferAccessor<T> bufferAccessor, AllocatorManager.AllocatorHandle allocator, bool clearBuffers)
            where T : unmanaged, IBufferElementData
        {
            int totalElementCount = bufferAccessor.GetTotalElementCount();

            if (Hint.Unlikely(totalElementCount == 0))
            {
                return new UnsafeList<Entity>();
            }

            UnsafeList<Entity> entityList = UnsafeListExtensions2.Create<Entity>(totalElementCount, allocator);

            for (int index = 0; index != bufferAccessor.Length; ++index)
            {
                DynamicBuffer<Entity> entityBuffer = bufferAccessor[index].Reinterpret<Entity>();

                // Add Entities
                entityList.AddRangeNoResize(entityBuffer.AsSpanRO());

                if (clearBuffers)
                {
                    // Clear Buffer
                    entityBuffer.Clear();
                }
            }

            return entityList;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UnsafeList<Entity> ExtractAliveEntityList<TElement, TDummyComponent>(BufferAccessor<TElement> bufferAccessor, ComponentLookup<TDummyComponent> entityLookup, AllocatorManager.AllocatorHandle allocator, bool clearBuffers)
            where TElement : unmanaged, IBufferElementData
            where TDummyComponent : unmanaged, IComponentData
        {
            int totalElementCount = bufferAccessor.GetTotalElementCount();

            if (Hint.Unlikely(totalElementCount == 0))
            {
                return new UnsafeList<Entity>();
            }

            UnsafeList<Entity> entityList = UnsafeListExtensions2.Create<Entity>(totalElementCount, allocator);

            for (int index = 0; index != bufferAccessor.Length; ++index)
            {
                DynamicBuffer<Entity> entityBuffer = bufferAccessor[index].Reinterpret<Entity>();

                if (entityBuffer.IsEmpty)
                {
                    // Empty
                    continue;
                }

                foreach (Entity entity in entityBuffer)
                {
                    if (entityLookup.EntityExists(entity))
                    {
                        // Add Entity
                        entityList.AddNoResize(entity);
                    }
                }

                if (clearBuffers)
                {
                    // Clear Buffer
                    entityBuffer.Clear();
                }
            }

            return entityList;
        }
    }
}
