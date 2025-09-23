using System.Runtime.CompilerServices;
using static Unity.Collections.LowLevel.Unsafe.UnsafeUtility2;

namespace Unity.Entities
{
    public static unsafe partial class ArchetypeChunkExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static U* GetComponentDataPtrROReinterpret<T, U>(this ArchetypeChunk self, ref ComponentTypeHandle<T> typeHandle)
            where T : unmanaged, IComponentData
            where U : unmanaged
        {
            CheckReinterpretArgs<T, U>();
            return (U*)self.GetComponentDataPtrRO(ref typeHandle);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static U* GetComponentDataPtrRWReinterpret<T, U>(this ArchetypeChunk self, ref ComponentTypeHandle<T> typeHandle)
            where T : unmanaged, IComponentData
            where U : unmanaged
        {
            CheckReinterpretArgs<T, U>();
            return (U*)self.GetComponentDataPtrRW(ref typeHandle);
        }
    }
}
