using System.Runtime.CompilerServices;
using Unity.Burst.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Entities.LowLevel.Unsafe;
using static Unity.Collections.LowLevel.Unsafe.UnsafeUtility2;

namespace EvilOctane.Entities
{
    public static class EntityOwnerAPI
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UnsafeList<Entity> ExtractOwnedEntityList<T>(
            ref BufferAccessor<T> bufferAccessor,
            AllocatorManager.AllocatorHandle allocator,
            bool clearBuffers)

            where T : unmanaged, IOwnedEntityBufferElementData
        {
            CheckReinterpretArgs<T, Entity>();

            int totalElementCount = (int)bufferAccessor.GetTotalElementCount();

            if (Hint.Unlikely(totalElementCount == 0))
            {
                return new UnsafeList<Entity>();
            }

            UnsafeList<Entity> entityList = UnsafeListExtensions2.Create<Entity>(totalElementCount, allocator);

            for (int index = 0; index != bufferAccessor.Length; ++index)
            {
                DynamicBuffer<T> buffer = bufferAccessor[index];

                // Add entities
                entityList.AddRangeNoResize(buffer.AsSpanRO().Reinterpret<Entity>());

                if (clearBuffers)
                {
                    // Clear
                    buffer.Clear();
                }
            }

            return entityList;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UnsafeList<Entity> ExtractAliveOwnedEntityList<TElement, TAnyComponent>(
            ref BufferAccessor<TElement> bufferAccessor,
            ref ComponentLookup<TAnyComponent> entityLookupRO,
            AllocatorManager.AllocatorHandle allocator,
            bool clearBuffers)

            where TElement : unmanaged, IOwnedEntityBufferElementData
            where TAnyComponent : unmanaged, IComponentData
        {
            CheckReinterpretArgs<TElement, Entity>();

            int totalElementCount = (int)bufferAccessor.GetTotalElementCount();

            if (Hint.Unlikely(totalElementCount == 0))
            {
                return new UnsafeList<Entity>();
            }

            UnsafeList<Entity> entityList = UnsafeListExtensions2.Create<Entity>(totalElementCount, allocator);

            for (int index = 0; index != bufferAccessor.Length; ++index)
            {
                DynamicBuffer<TElement> buffer = bufferAccessor[index];

                if (buffer.IsEmpty)
                {
                    // Empty
                    continue;
                }

                foreach (TElement element in buffer)
                {
                    Entity ownedEntity = element.OwnedEntity;

                    if (entityLookupRO.EntityExists(ownedEntity))
                    {
                        // Add entity
                        entityList.AddNoResize(ownedEntity);
                    }
                }

                if (clearBuffers)
                {
                    // Clear
                    buffer.Clear();
                }
            }

            return entityList;
        }
    }
}
