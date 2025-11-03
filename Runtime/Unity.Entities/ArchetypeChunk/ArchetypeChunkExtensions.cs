using System.Runtime.CompilerServices;

namespace Unity.Entities
{
    public static unsafe partial class ArchetypeChunkExtensions
    {
        /// <summary>
        /// <inheritdoc cref="ArchetypeChunk.GetRequiredComponentDataPtrRO{T}(ref ComponentTypeHandle{T})"/>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="self"></param>
        /// <param name="typeHandle"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T* GetRequiredComponentDataPtrROTyped<T>(this ArchetypeChunk self, ref ComponentTypeHandle<T> typeHandle)
            where T : unmanaged, IComponentData
        {
            return (T*)self.GetRequiredComponentDataPtrRO(ref typeHandle);
        }

        /// <summary>
        /// <inheritdoc cref="ArchetypeChunk.GetRequiredComponentDataPtrRW{T}(ref ComponentTypeHandle{T})"/>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="self"></param>
        /// <param name="typeHandle"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T* GetRequiredComponentDataPtrRWTyped<T>(this ArchetypeChunk self, ref ComponentTypeHandle<T> typeHandle)
            where T : unmanaged, IComponentData
        {
            return (T*)self.GetRequiredComponentDataPtrRW(ref typeHandle);
        }
    }
}
