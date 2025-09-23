using System.Runtime.CompilerServices;
using Unity.Collections;
using static System.Runtime.CompilerServices.Unsafe;
using static Unity.Collections.CollectionHelper;

namespace Unity.Entities
{
    public static class ComponentTypeSetExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static FixedList128Bytes<ComponentType> GetComponentTypes(this ComponentTypeSet self)
        {
            SkipInit(out FixedList128Bytes<ComponentType> result);
            result.Length = self.Length;

            for (int index = 0; index != self.Length; ++index)
            {
                result[index] = self.GetComponentType(index);
            }

            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static NativeArray<ComponentType> GetComponentTypes(this ComponentTypeSet self, AllocatorManager.AllocatorHandle allocator)
        {
            NativeArray<ComponentType> result = CreateNativeArray<ComponentType>(self.Length, allocator, NativeArrayOptions.UninitializedMemory);

            for (int index = 0; index != self.Length; ++index)
            {
                result[index] = self.GetComponentType(index);
            }

            return result;
        }
    }
}
