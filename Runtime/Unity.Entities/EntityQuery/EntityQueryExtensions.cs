using System.Runtime.CompilerServices;

namespace Unity.Entities
{
    public static partial class EntityQueryExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetChangedVersionFilter<T>(this EntityQuery self)
        {
            self.SetChangedVersionFilter(ComponentType.ReadWrite<T>());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetChangedVersionFilter<T0, T1>(this EntityQuery self)
        {
            self.SetChangedVersionFilter(ComponentType.ReadWrite<T0>());
            self.AddChangedVersionFilter(ComponentType.ReadWrite<T1>());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AddChangedVersionFilter<T>(this EntityQuery self)
        {
            self.AddChangedVersionFilter(ComponentType.ReadWrite<T>());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AddChangedVersionFilter<T0, T1>(this EntityQuery self)
        {
            self.AddChangedVersionFilter(ComponentType.ReadWrite<T0>());
            self.AddChangedVersionFilter(ComponentType.ReadWrite<T1>());
        }
    }
}
