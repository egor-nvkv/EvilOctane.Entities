using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Unity.Entities.LowLevel.Unsafe
{
    internal static unsafe partial class EntityCommandBufferDataExtensions
    {
        public static Entity* CloneAndSearchForDeferredEntities(this ref EntityCommandBufferData self, Entity* entities, int length, out bool containsDeferredEntities)
        {
            Entity* output = (Entity*)Memory.Unmanaged.Allocate(length * sizeof(Entity), EntityCommandBufferData.ALIGN_64_BIT, self.m_Allocator);
            UnsafeUtility.MemCpy(output, entities, length * sizeof(Entity));

            containsDeferredEntities = false;

            for (int index = 0; index != length; ++index)
            {
                Entity entity = entities[index];
                containsDeferredEntities |= entity.Index < 0;
            }

            return output;
        }
    }
}
