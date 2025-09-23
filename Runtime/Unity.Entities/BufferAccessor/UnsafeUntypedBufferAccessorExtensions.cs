using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Unity.Collections.LowLevel.Unsafe;
using static Unity.Collections.LowLevel.Unsafe.UnsafeUtility2;

namespace Unity.Entities.LowLevel.Unsafe
{
    public static unsafe partial class UnsafeUntypedBufferAccessorExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetTotalElementCount(this UnsafeUntypedBufferAccessor self)
        {
            int totalElementCount = 0;

            for (int index = 0; index != self.Length; ++index)
            {
                totalElementCount += self.GetBufferLength(index);
            }

            return totalElementCount;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static BufferAccessor<T> Reinterpret<T>(this UnsafeUntypedBufferAccessor self, int index, bool readOnly = false)
            where T : unmanaged, IBufferElementData
        {
            ref UnsafeUntypedBufferAccessorExposed exposed = ref Reinterpret<UnsafeUntypedBufferAccessor, UnsafeUntypedBufferAccessorExposed>(ref self);
            CheckReinterpretArgs<T>(ref exposed);

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            return new BufferAccessor<T>(exposed.m_Pointer, exposed.m_Length, exposed.m_Stride, readOnly, exposed.m_Safety0, exposed.m_ArrayInvalidationSafety, exposed.m_InternalCapacity);
#else
            return new BufferAccessor<T>(exposed.m_Pointer, exposed.m_Length, exposed.m_Stride, exposed.m_InternalCapacity);
#endif
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DynamicBuffer<T> GetBufferReinterpret<T>(this UnsafeUntypedBufferAccessor self, int index, bool readOnly = false)
            where T : unmanaged
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS || UNITY_DOTS_DEBUG
            if (readOnly)
            {
                self.GetUnsafeReadOnlyPtr(index);
            }
            else
            {
                self.GetUnsafePtr(index);
            }
#endif
            ref UnsafeUntypedBufferAccessorExposed exposed = ref Reinterpret<UnsafeUntypedBufferAccessor, UnsafeUntypedBufferAccessorExposed>(ref self);
            CheckReinterpretArgs<T>(ref exposed);

            BufferHeader* hdr = (BufferHeader*)(exposed.m_Pointer + (index * exposed.m_Stride));

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            return new DynamicBuffer<T>(hdr, exposed.m_Safety0, exposed.m_ArrayInvalidationSafety, readOnly, false, 0, exposed.m_InternalCapacity);
#else
            return new DynamicBuffer<T>(hdr, exposed.m_InternalCapacity);
#endif
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UnsafeSpan<T> GetSpanReinterpret<T>(this UnsafeUntypedBufferAccessor self, int index, bool readOnly = false)
            where T : unmanaged
        {
            ref UnsafeUntypedBufferAccessorExposed exposed = ref Reinterpret<UnsafeUntypedBufferAccessor, UnsafeUntypedBufferAccessorExposed>(ref self);
            CheckReinterpretArgs<T>(ref exposed);

            void* ptr = readOnly ?
                self.GetUnsafeReadOnlyPtrAndLength(index, out int length) :
                self.GetUnsafePtrAndLength(index, out length);

            return new UnsafeSpan<T>((T*)ptr, length);
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        [Conditional("UNITY_DOTS_DEBUG")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void CheckReinterpretArgs<T>(ref UnsafeUntypedBufferAccessorExposed bufferAccessor)
            where T : unmanaged
        {
            if (sizeof(T) != bufferAccessor.m_ElementSize)
            {
                throw new InvalidOperationException("Target type has wrong size.");
            }

            if (UnsafeUtility.AlignOf<T>() > bufferAccessor.m_ElementAlign)
            {
                throw new InvalidOperationException("Target type is over-aligned.");
            }
        }

        internal struct UnsafeUntypedBufferAccessorExposed
        {
            public byte* m_Pointer;
            public int m_InternalCapacity;
            public int m_Stride;
            public int m_Length;
            public int m_ElementSize;
            public int m_ElementAlign;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            public AtomicSafetyHandle m_Safety0;
            public AtomicSafetyHandle m_ArrayInvalidationSafety;
            public int m_SafetyReadOnlyCount;
            public int m_SafetyReadWriteCount;
#endif
        }
    }
}
