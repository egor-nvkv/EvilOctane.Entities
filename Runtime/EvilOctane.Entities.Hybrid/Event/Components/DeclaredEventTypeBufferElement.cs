using System;
using Unity.Burst.CompilerServices;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;

namespace EvilOctane.Entities
{
    [BakingType]
    [InternalBufferCapacity(0)]
    internal struct DeclaredEventTypeBufferElement : IBufferElementData
    {
        public TypeIndex EventTypeIndex;
    }

    internal static class DeclaredEventTypeBufferExtensions
    {
        public static void CopyFrom(this DynamicBuffer<DeclaredEventTypeBufferElement> self, ReadOnlySpan<TypeIndex> eventTypes)
        {
            self.ResizeUninitializedTrashOldData(eventTypes.Length);

            Span<TypeIndex> typeSpan = self.AsSpanRW().Reinterpret<TypeIndex>();
            eventTypes.CopyTo(typeSpan);
        }

        public static void GetUnique(this DynamicBuffer<DeclaredEventTypeBufferElement> self, ref UnsafeList<TypeIndex> eventTypeList)
        {
            eventTypeList.Clear();
            eventTypeList.EnsureCapacity(self.Length, keepOldData: false);

            foreach (DeclaredEventTypeBufferElement declaredEvent in self)
            {
                TypeIndex typeIndex = declaredEvent.EventTypeIndex;

                if (Hint.Unlikely(typeIndex == TypeIndex.Null))
                {
                    // Null
                    continue;
                }
                else if (Hint.Unlikely(eventTypeList.Contains(typeIndex)))
                {
                    // Duplicate
                    continue;
                }

                eventTypeList.AddNoResize(typeIndex);
            }
        }
    }
}
