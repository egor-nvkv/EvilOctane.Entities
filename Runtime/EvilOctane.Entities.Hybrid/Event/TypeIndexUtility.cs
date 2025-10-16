using System;
using Unity.Burst.CompilerServices;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;

namespace EvilOctane.Entities
{
    public static partial class TypeIndexUtility
    {
        public static void GetUnique(this UnsafeSpan<TypeIndex> self, ref UnsafeList<TypeIndex> uniqueTypeIndexList)
        {
            uniqueTypeIndexList.Clear();
            uniqueTypeIndexList.EnsureCapacity(self.Length, keepOldData: false);

            foreach (TypeIndex typeIndex in self)
            {
                if (Hint.Unlikely(typeIndex == TypeIndex.Null))
                {
                    // Null
                    continue;
                }
                else if (Hint.Unlikely(uniqueTypeIndexList.Contains(typeIndex)))
                {
                    // Duplicate
                    continue;
                }

                uniqueTypeIndexList.AddNoResize(typeIndex);
            }
        }
    }
}
