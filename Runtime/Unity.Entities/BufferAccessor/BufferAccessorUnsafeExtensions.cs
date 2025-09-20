using System.Runtime.CompilerServices;
using Unity.Collections.LowLevel.Unsafe;
using static System.Runtime.CompilerServices.Unsafe;

namespace Unity.Entities.LowLevel.Unsafe
{
    public static unsafe partial class BufferAccessorUnsafeExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UnsafeUntypedBufferAccessor AsUntyped<T>(this BufferAccessor<T> self)
            where T : unmanaged, IBufferElementData
        {
            ref BufferAccessorExposed<T> exposed = ref As<BufferAccessor<T>, BufferAccessorExposed<T>>(ref self);

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            return new UnsafeUntypedBufferAccessor(exposed.m_BasePointer, exposed.m_Length, exposed.m_Stride, sizeof(T), UnsafeUtility.AlignOf<T>(), exposed.m_InternalCapacity, self.IsReadOnly, exposed.m_Safety0, exposed.m_ArrayInvalidationSafety);
#else
            return new UnsafeUntypedBufferAccessor(exposed.m_BasePointer, exposed.m_Length, exposed.m_Stride, sizeof(T), UnsafeUtility.AlignOf<T>(), exposed.m_InternalCapacity);
#endif
        }

        internal unsafe struct BufferAccessorExposed<T>
            where T : unmanaged, IBufferElementData
        {
            public byte* m_BasePointer;
            public int m_Length;
            public int m_Stride;
            public int m_InternalCapacity;
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            public byte m_IsReadOnly;
            public AtomicSafetyHandle m_Safety0;
            public AtomicSafetyHandle m_ArrayInvalidationSafety;
            public int m_SafetyReadOnlyCount;
            public int m_SafetyReadWriteCount;
#endif
        }
    }
}
