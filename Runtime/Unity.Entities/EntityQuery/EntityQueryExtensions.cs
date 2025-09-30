using System.Runtime.CompilerServices;

namespace Unity.Entities
{
    public static partial class EntityQueryExtensions
    {
        /// <summary>
        /// <inheritdoc cref="EntityQuery.SetChangedVersionFilter(ComponentType)"/>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="self"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetChangedVersionFilter<T>(this EntityQuery self)
        {
            self.SetChangedVersionFilter(ComponentType.ReadWrite<T>());
        }

        /// <summary>
        /// <inheritdoc cref="EntityQuery.SetChangedVersionFilter(ComponentType[])"/>
        /// </summary>
        /// <typeparam name="T0"></typeparam>
        /// <typeparam name="T1"></typeparam>
        /// <param name="self"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetChangedVersionFilter<T0, T1>(this EntityQuery self)
        {
            self.SetChangedVersionFilter(ComponentType.ReadWrite<T0>());
            self.AddChangedVersionFilter(ComponentType.ReadWrite<T1>());
        }

        /// <summary>
        /// <inheritdoc cref="EntityQuery.AddChangedVersionFilter(ComponentType)"/>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="self"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AddChangedVersionFilter<T>(this EntityQuery self)
        {
            self.AddChangedVersionFilter(ComponentType.ReadWrite<T>());
        }

        /// <summary>
        /// <inheritdoc cref="EntityQuery.AddChangedVersionFilter(ComponentType)"/>
        /// </summary>
        /// <typeparam name="T0"></typeparam>
        /// <typeparam name="T1"></typeparam>
        /// <param name="self"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AddChangedVersionFilter<T0, T1>(this EntityQuery self)
        {
            self.AddChangedVersionFilter(ComponentType.ReadWrite<T0>());
            self.AddChangedVersionFilter(ComponentType.ReadWrite<T1>());
        }
    }
}
