using System;
using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Unity.Entities
{
    public static unsafe partial class DynamicBufferExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UnsafeSpan<T> AsSpanRW<T>(this DynamicBuffer<T> self)
            where T : unmanaged
        {
            return new UnsafeSpan<T>((T*)self.GetUnsafePtr(), self.Length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UnsafeSpan<T> AsSpanRO<T>(this DynamicBuffer<T> self)
            where T : unmanaged
        {
            return new UnsafeSpan<T>((T*)self.GetUnsafeReadOnlyPtr(), self.Length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AddRange<T>(this DynamicBuffer<T> self, UnsafeSpan<T> valueSpan)
            where T : unmanaged
        {
            int oldLength = self.Length;
            self.ResizeUninitialized(oldLength + valueSpan.Length);

            new UnsafeSpan<T>((T*)self.GetUnsafePtr() + oldLength, valueSpan.Length).CopyFrom(valueSpan);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool RemoveFirstMatchSwapBack<T, U>(this DynamicBuffer<T> self, U value)
            where T : unmanaged, IEquatable<U>
        {
            int index = NativeArrayExtensions.IndexOf<T, U>(self.GetUnsafeReadOnlyPtr(), self.Length, value);

            if (index < 0)
            {
                return false;
            }

            self.RemoveAtSwapBack(index);
            return true;
        }
    }
}
