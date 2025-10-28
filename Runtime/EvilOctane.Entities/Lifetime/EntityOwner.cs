using System.Runtime.CompilerServices;
using Unity.Burst.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Entities.LowLevel.Unsafe;

namespace EvilOctane.Entities
{
    public static unsafe class EntityOwner
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UnsafeList<Entity> ExtractOwnedEntityList<T>(ref BufferAccessor<T> bufferAccessor, AllocatorManager.AllocatorHandle allocator, bool clearBuffers)
            where T : unmanaged, IEntityOwnerBufferElementData
        {
            UnsafeUtility2.CheckReinterpretArgs<T, Entity>(requireExactAlignment: true);

            int totalElementCount = bufferAccessor.GetTotalElementCount();

            if (Hint.Unlikely(totalElementCount == 0))
            {
                return new UnsafeList<Entity>();
            }

            UnsafeList<Entity> entityList = UnsafeListExtensions2.Create<Entity>(totalElementCount, allocator);

            for (int index = 0; index != bufferAccessor.Length; ++index)
            {
                DynamicBuffer<T> buffer = bufferAccessor[index];

                // Add Entities
                entityList.AddRangeNoResize(buffer.AsSpanRO().Reinterpret<Entity>());

                if (clearBuffers)
                {
                    // Clear Buffer
                    buffer.Clear();
                }
            }

            return entityList;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UnsafeList<Entity> ExtractAliveOwnedEntityList<TElement, TDummyComponent>(ref BufferAccessor<TElement> bufferAccessor, ref ComponentLookup<TDummyComponent> entityLookup, AllocatorManager.AllocatorHandle allocator, bool clearBuffers)
            where TElement : unmanaged, IEntityOwnerBufferElementData
            where TDummyComponent : unmanaged, IComponentData
        {
            UnsafeUtility2.CheckReinterpretArgs<TElement, Entity>(requireExactAlignment: true);

            int totalElementCount = bufferAccessor.GetTotalElementCount();

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

                    if (entityLookup.EntityExists(ownedEntity))
                    {
                        // Add Entity
                        entityList.AddNoResize(ownedEntity);
                    }
                }

                if (clearBuffers)
                {
                    // Clear Buffer
                    buffer.Clear();
                }
            }

            return entityList;
        }
    }
}
