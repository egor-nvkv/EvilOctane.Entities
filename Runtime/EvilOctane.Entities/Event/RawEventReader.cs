using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Unity.Burst.CompilerServices;
using static EvilOctane.Entities.EventAPI;
using static System.Runtime.CompilerServices.Unsafe;
using static Unity.Collections.CollectionHelper2;

namespace EvilOctane.Entities
{
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct RawEventReader
    {
        private readonly byte* buffer;
        private readonly int size;

        private int offset;
        private int currentEventIndex;

        public readonly bool IsEndOfBuffer => offset >= size;

        public readonly int CurrentEventIndex => currentEventIndex;

        public RawEventReader(byte* buffer, int size)
        {
            this.buffer = buffer;
            this.size = size;

            offset = 0;
            currentEventIndex = 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void PrepareNext()
        {
            _ = ReadHeader(ref offset, out ushort typeSize);

            offset += typeSize;
            ++currentEventIndex;
        }

        [SkipLocalsInit]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool TryReinterpretCurrent<T>(out T value)
            where T : unmanaged, IRawEvent
        {
            int currentOffset = offset;
            ulong typeHash = ReadHeader(ref currentOffset, out ushort typeSize);

            if (!IsRawEventType<T>(typeHash))
            {
                // Different type
                SkipInit(out value);
                return false;
            }

            if (typeSize != 0)
            {
                if (Hint.Likely(typeSize == sizeof(T)))
                {
                    // Value
                    value = ReadUnaligned<T>(buffer + currentOffset);
                }
                else
                {
                    // Different type size
                    SkipInit(out value);
                    return false;
                }
            }
            else
            {
                // No value
                value = new();
            }

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private readonly ulong ReadHeader(ref int offset, out ushort typeSize)
        {
            // Type hash
            CheckContainerIndexInRange(offset + sizeof(long) - 1, size);
            ulong typeHash = ReadUnaligned<ulong>(buffer + offset);
            offset += sizeof(long);

            // Type size
            CheckContainerIndexInRange(offset + sizeof(ushort) - 1, size);
            typeSize = ReadUnaligned<ushort>(buffer + offset);
            offset += sizeof(ushort);

            if (typeSize != 0)
            {
                CheckContainerElementSize(typeSize);
                CheckContainerIndexInRange(offset + typeSize - 1, size);
            }

            return typeHash;
        }
    }
}
