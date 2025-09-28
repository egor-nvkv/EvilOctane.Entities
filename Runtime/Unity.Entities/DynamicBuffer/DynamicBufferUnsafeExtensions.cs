using System.Diagnostics;
using System.Runtime.CompilerServices;
using Unity.Collections.LowLevel.Unsafe;
using static Unity.Collections.CollectionHelper;
using static Unity.Collections.CollectionHelper2;

namespace Unity.Entities.LowLevel.Unsafe
{
    public static unsafe partial class DynamicBufferUnsafeExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetLengthNoResize<T>(this DynamicBuffer<T> self, int length)
            where T : unmanaged
        {
            CheckContainerLength(length);
            CheckCapacityInRange(self.Capacity, length);

            ref DynamicBufferExposed<T> exposed = ref UnsafeUtility2.Reinterpret<DynamicBuffer<T>, DynamicBufferExposed<T>>(ref self);

            CheckWriteAccessAndInvalidateArrayAliases(ref exposed);
            exposed.m_Buffer->Length = length;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AddNoResize<T>(this DynamicBuffer<T> self, T item)
            where T : unmanaged
        {
            CheckAddNoResizeHasEnoughCapacity(self.Length, self.Capacity, 1);

            int length = self.Length;
            SetLengthNoResize(self, length + 1);

            self[length] = item;
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void CheckWriteAccessAndInvalidateArrayAliases<T>(ref DynamicBufferExposed<T> exposed)
            where T : unmanaged
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndThrow(exposed.m_Safety0);
            AtomicSafetyHandle.CheckWriteAndBumpSecondaryVersion(exposed.m_Safety1);
#endif
        }

        internal struct DynamicBufferExposed<T>
            where T : unmanaged
        {
            public BufferHeader* m_Buffer;
            public int m_InternalCapacity;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            public AtomicSafetyHandle m_Safety0;
            public AtomicSafetyHandle m_Safety1;
            public int m_SafetyReadOnlyCount;
            public int m_SafetyReadWriteCount;

            public byte m_IsReadOnly;
            public byte m_useMemoryInitPattern;
            public byte m_memoryInitPattern;
#endif
        }
    }
}
