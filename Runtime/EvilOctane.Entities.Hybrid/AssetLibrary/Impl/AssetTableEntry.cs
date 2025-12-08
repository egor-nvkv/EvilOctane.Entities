using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using static Unity.Collections.CollectionHelper2;

namespace EvilOctane.Entities
{
    public unsafe struct AssetTableEntry
    {
        public Entity* EntityPtr;
        public int Length;
        public int Capacity;

        public readonly UnsafeSpan<Entity> EntitySpan => new(EntityPtr, Length);

        public AssetTableEntry(int capacity, AllocatorManager.AllocatorHandle allocator)
        {
            CheckContainerCapacity(capacity);

            EntityPtr = MemoryExposed.AllocateList<Entity>(capacity, allocator, out nint actualCapacity);
            Length = 0;
            Capacity = (int)actualCapacity;
        }

        public void AddUnique(Entity entity, AllocatorManager.AllocatorHandle allocator)
        {
            bool exists = EntitySpan.Contains(entity);

            if (exists)
            {
                // Duplicate
                return;
            }

            UnsafeList<Entity> tempList = new()
            {
                Ptr = EntityPtr,
                m_length = Length,
                m_capacity = Capacity,
                Allocator = allocator
            };

            tempList.Add(entity);

            EntityPtr = tempList.Ptr;
            Length = tempList.Length;
            Capacity = tempList.Capacity;
        }

        public void Dispose(AllocatorManager.AllocatorHandle allocator)
        {
            MemoryExposed.Unmanaged.Free(EntityPtr, allocator);
            EntityPtr = null;
        }
    }
}
