using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Unity.Entities;
using static EvilOctane.Entities.EventAPI;
using static System.Runtime.CompilerServices.Unsafe;

namespace EvilOctane.Entities
{
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct RawEventWriter
    {
        private DynamicBuffer<byte> storage;

        public RawEventWriter(DynamicBuffer<byte> storage)
        {
            this.storage = storage;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write<T>(T value)
            where T : unmanaged, IRawEvent
        {
            const int headerSize = sizeof(long) + sizeof(ushort);
            int totalSize = headerSize + sizeof(T);

            int offset = storage.Length;

            storage.ResizeUninitialized(offset + totalSize);
            byte* buffer = (byte*)storage.GetUnsafePtr();

            // Type hash
            WriteUnaligned(buffer + offset, GetRawEventTypeHashCode<T>());
            offset += sizeof(long);

            // Type size
            WriteUnaligned(buffer + offset, (ushort)sizeof(T));
            offset += sizeof(ushort);

            // Value
            WriteUnaligned(buffer + offset, value);
        }
    }
}
