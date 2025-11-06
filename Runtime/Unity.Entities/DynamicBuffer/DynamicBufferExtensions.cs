using System;
using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using static Unity.Collections.CollectionHelper2;
using static Unity.Collections.LowLevel.Unsafe.UnsafeUtility2;
using static Unity.Entities.LowLevel.Unsafe.DynamicBufferUnsafeExtensions;

namespace Unity.Entities
{
    public static unsafe partial class DynamicBufferExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref T ElementAtReadOnly<T>(this DynamicBuffer<T> self, int index)
            where T : unmanaged
        {
            CheckContainerIndexInRange(index, self.Length);
            return ref ((T*)self.GetUnsafeReadOnlyPtr())[index];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ReinterpretStorageRW<T, U>(this DynamicBuffer<T> self, out U* storage)
            where T : unmanaged
            where U : unmanaged
        {
            nint bufferSize = (nint)self.Length * sizeof(T);

            if (bufferSize != 0)
            {
                CheckContainerIndexInRange(sizeof(U) - 1, bufferSize);
            }

            void* ptr = self.GetUnsafePtr();
            CheckIsAligned<U>(ptr);

            storage = (U*)ptr;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ReinterpretStorageRO<T, U>(this DynamicBuffer<T> self, out U* storage)
            where T : unmanaged
            where U : unmanaged
        {
            nint bufferSize = (nint)self.Length * sizeof(T);

            if (bufferSize != 0)
            {
                CheckContainerIndexInRange(sizeof(U) - 1, bufferSize);
            }

            void* ptr = self.GetUnsafeReadOnlyPtr();
            CheckIsAligned<U>(ptr);

            storage = (U*)ptr;
        }

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

        /// <summary>
        /// <inheritdoc cref="DynamicBuffer{T}.ResizeUninitialized(int)"/>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="self"></param>
        /// <param name="length"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ResizeUninitializedTrashOldData<T>(this DynamicBuffer<T> self, int length)
            where T : unmanaged
        {
            EnsureCapacityTrashOldData(self, length);

            ref DynamicBufferExposed<T> exposed = ref UnsafeUtility2.Reinterpret<DynamicBuffer<T>, DynamicBufferExposed<T>>(ref self);
            exposed.m_Buffer->Length = length;
        }

        /// <summary>
        /// <inheritdoc cref="DynamicBuffer{T}.EnsureCapacity(int)"/>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="self"></param>
        /// <param name="length"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void EnsureCapacityTrashOldData<T>(this DynamicBuffer<T> self, int length)
            where T : unmanaged
        {
            CollectionHelper2.CheckContainerLength(length);

            ref DynamicBufferExposed<T> exposed = ref UnsafeUtility2.Reinterpret<DynamicBuffer<T>, DynamicBufferExposed<T>>(ref self);

            CheckWriteAccessAndInvalidateArrayAliases(ref exposed);
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            BufferHeader.EnsureCapacity(exposed.m_Buffer, length, sizeof(T), UnsafeUtility.AlignOf<T>(), BufferHeader.TrashMode.TrashOldData, exposed.m_useMemoryInitPattern == 1, exposed.m_memoryInitPattern);
#else
            BufferHeader.EnsureCapacity(exposed.m_Buffer, length, sizeof(T), UnsafeUtility.AlignOf<T>(), BufferHeader.TrashMode.TrashOldData, false, 0);
#endif
        }

        /// <summary>
        /// <inheritdoc cref="DynamicBuffer{T}.TrimExcess"/>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="self"></param>
        /// <param name="length"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void TrimExcessTrashOldData<T>(this DynamicBuffer<T> self)
            where T : unmanaged
        {
            ref DynamicBufferExposed<T> exposed = ref UnsafeUtility2.Reinterpret<DynamicBuffer<T>, DynamicBufferExposed<T>>(ref self);

            CheckWriteAccessAndInvalidateArrayAliases(ref exposed);

            byte* oldPtr = exposed.m_Buffer->Pointer;
            int length = exposed.m_Buffer->Length;

            if (length == exposed.m_Buffer->Capacity || oldPtr == null)
            {
                return;
            }

            bool isInternal;
            byte* newPtr;

            // If the size fits in the internal buffer, prefer to move the elements back there.
            if (length <= exposed.m_InternalCapacity)
            {
                newPtr = (byte*)(exposed.m_Buffer + 1);
                isInternal = true;
            }
            else
            {
                newPtr = (byte*)Memory.Unmanaged.Allocate(length * sizeof(T), UnsafeUtility.AlignOf<T>(), Allocator.Persistent);
                isInternal = false;
            }

            exposed.m_Buffer->Capacity = math.max(length, exposed.m_InternalCapacity);
            exposed.m_Buffer->Pointer = isInternal ? null : newPtr;

            Memory.Unmanaged.Free(oldPtr, Allocator.Persistent);
        }

        /// <summary>
        /// <inheritdoc cref="DynamicBuffer{T}.AddRange(NativeArray{T})"/>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="self"></param>
        /// <param name="newElems"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AddRange<T>(this DynamicBuffer<T> self, UnsafeSpan<T> newElems)
            where T : unmanaged
        {
            int oldLength = self.Length;
            self.ResizeUninitialized(oldLength + newElems.Length);

            new UnsafeSpan<T>((T*)self.GetUnsafePtr() + oldLength, newElems.Length).CopyFrom(newElems);
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
