using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Unity.Burst.CompilerServices;
using Unity.Collections;
using Unity.Mathematics;
using static System.Runtime.CompilerServices.Unsafe;
using static Unity.Collections.LowLevel.Unsafe.UnsafeUtility2;

namespace Unity.Entities
{
    public static class ComponentTypeSetUtility
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ComponentTypeSet Create<T0, T1>()
        {
            SkipInit(out ComponentTypeSetMutable mutable);

            TypeIndex typeIndex0 = TypeManager.GetTypeIndex<T0>();
            TypeIndex typeIndex1 = TypeManager.GetTypeIndex<T1>();

            OrderLess(ref typeIndex0, ref typeIndex1);

            mutable._sorted.Length = 2;
            mutable._sorted[0] = typeIndex0;
            mutable._sorted[1] = typeIndex1;

            mutable.m_masks = new ComponentTypeSet.Masks(mutable._sorted);

            CheckForDuplicates(mutable._sorted);
            return Reinterpret<ComponentTypeSetMutable, ComponentTypeSet>(ref mutable);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ComponentTypeSet Create<T0, T1, T2>()
        {
            SkipInit(out ComponentTypeSetMutable mutable);

            TypeIndex typeIndex0 = TypeManager.GetTypeIndex<T0>();
            TypeIndex typeIndex1 = TypeManager.GetTypeIndex<T1>();
            TypeIndex typeIndex2 = TypeManager.GetTypeIndex<T2>();

            // https://bertdobbelaere.github.io/sorting_networks.html#N3L3D3

            OrderLess(ref typeIndex0, ref typeIndex2);
            OrderLess(ref typeIndex0, ref typeIndex1);
            OrderLess(ref typeIndex1, ref typeIndex2);

            mutable._sorted.Length = 3;
            mutable._sorted[0] = typeIndex0;
            mutable._sorted[1] = typeIndex1;
            mutable._sorted[2] = typeIndex2;

            mutable.m_masks = new ComponentTypeSet.Masks(mutable._sorted);

            CheckForDuplicates(mutable._sorted);
            return Reinterpret<ComponentTypeSetMutable, ComponentTypeSet>(ref mutable);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ComponentTypeSet Create<T0, T1, T2, T3>()
        {
            SkipInit(out ComponentTypeSetMutable mutable);

            TypeIndex typeIndex0 = TypeManager.GetTypeIndex<T0>();
            TypeIndex typeIndex1 = TypeManager.GetTypeIndex<T1>();
            TypeIndex typeIndex2 = TypeManager.GetTypeIndex<T2>();
            TypeIndex typeIndex3 = TypeManager.GetTypeIndex<T3>();

            // https://bertdobbelaere.github.io/sorting_networks.html#N4L5D3

            OrderLess(ref typeIndex0, ref typeIndex2);
            OrderLess(ref typeIndex1, ref typeIndex3);

            OrderLess(ref typeIndex0, ref typeIndex1);
            OrderLess(ref typeIndex2, ref typeIndex3);

            OrderLess(ref typeIndex1, ref typeIndex2);

            mutable._sorted.Length = 4;
            mutable._sorted[0] = typeIndex0;
            mutable._sorted[1] = typeIndex1;
            mutable._sorted[2] = typeIndex2;
            mutable._sorted[3] = typeIndex3;

            mutable.m_masks = new ComponentTypeSet.Masks(mutable._sorted);

            CheckForDuplicates(mutable._sorted);
            return Reinterpret<ComponentTypeSetMutable, ComponentTypeSet>(ref mutable);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ComponentTypeSet Create<T0, T1, T2, T3, T4>()
        {
            SkipInit(out ComponentTypeSetMutable mutable);

            TypeIndex typeIndex0 = TypeManager.GetTypeIndex<T0>();
            TypeIndex typeIndex1 = TypeManager.GetTypeIndex<T1>();
            TypeIndex typeIndex2 = TypeManager.GetTypeIndex<T2>();
            TypeIndex typeIndex3 = TypeManager.GetTypeIndex<T3>();
            TypeIndex typeIndex4 = TypeManager.GetTypeIndex<T4>();

            // https://bertdobbelaere.github.io/sorting_networks.html#N5L9D5

            OrderLess(ref typeIndex0, ref typeIndex3);
            OrderLess(ref typeIndex1, ref typeIndex4);

            OrderLess(ref typeIndex0, ref typeIndex2);
            OrderLess(ref typeIndex1, ref typeIndex3);

            OrderLess(ref typeIndex0, ref typeIndex1);
            OrderLess(ref typeIndex2, ref typeIndex4);

            OrderLess(ref typeIndex1, ref typeIndex2);
            OrderLess(ref typeIndex3, ref typeIndex4);

            OrderLess(ref typeIndex2, ref typeIndex3);

            mutable._sorted.Length = 5;
            mutable._sorted[0] = typeIndex0;
            mutable._sorted[1] = typeIndex1;
            mutable._sorted[2] = typeIndex2;
            mutable._sorted[3] = typeIndex3;
            mutable._sorted[4] = typeIndex4;

            mutable.m_masks = new ComponentTypeSet.Masks(mutable._sorted);

            CheckForDuplicates(mutable._sorted);
            return Reinterpret<ComponentTypeSetMutable, ComponentTypeSet>(ref mutable);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ComponentTypeSet Create<T0, T1, T2, T3, T4, T5>()
        {
            SkipInit(out ComponentTypeSetMutable mutable);

            TypeIndex typeIndex0 = TypeManager.GetTypeIndex<T0>();
            TypeIndex typeIndex1 = TypeManager.GetTypeIndex<T1>();
            TypeIndex typeIndex2 = TypeManager.GetTypeIndex<T2>();
            TypeIndex typeIndex3 = TypeManager.GetTypeIndex<T3>();
            TypeIndex typeIndex4 = TypeManager.GetTypeIndex<T4>();
            TypeIndex typeIndex5 = TypeManager.GetTypeIndex<T5>();

            // https://bertdobbelaere.github.io/sorting_networks.html#N6L12D5

            OrderLess(ref typeIndex0, ref typeIndex5);
            OrderLess(ref typeIndex1, ref typeIndex3);
            OrderLess(ref typeIndex2, ref typeIndex4);

            OrderLess(ref typeIndex1, ref typeIndex2);
            OrderLess(ref typeIndex3, ref typeIndex4);

            OrderLess(ref typeIndex0, ref typeIndex3);
            OrderLess(ref typeIndex2, ref typeIndex5);

            OrderLess(ref typeIndex0, ref typeIndex1);
            OrderLess(ref typeIndex2, ref typeIndex3);
            OrderLess(ref typeIndex4, ref typeIndex5);

            OrderLess(ref typeIndex1, ref typeIndex2);
            OrderLess(ref typeIndex3, ref typeIndex4);

            mutable._sorted.Length = 6;
            mutable._sorted[0] = typeIndex0;
            mutable._sorted[1] = typeIndex1;
            mutable._sorted[2] = typeIndex2;
            mutable._sorted[3] = typeIndex3;
            mutable._sorted[4] = typeIndex4;
            mutable._sorted[5] = typeIndex5;

            mutable.m_masks = new ComponentTypeSet.Masks(mutable._sorted);

            CheckForDuplicates(mutable._sorted);
            return Reinterpret<ComponentTypeSetMutable, ComponentTypeSet>(ref mutable);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ComponentTypeSet Create<T0, T1, T2, T3, T4, T5, T6>()
        {
            SkipInit(out ComponentTypeSetMutable mutable);

            TypeIndex typeIndex0 = TypeManager.GetTypeIndex<T0>();
            TypeIndex typeIndex1 = TypeManager.GetTypeIndex<T1>();
            TypeIndex typeIndex2 = TypeManager.GetTypeIndex<T2>();
            TypeIndex typeIndex3 = TypeManager.GetTypeIndex<T3>();
            TypeIndex typeIndex4 = TypeManager.GetTypeIndex<T4>();
            TypeIndex typeIndex5 = TypeManager.GetTypeIndex<T5>();
            TypeIndex typeIndex6 = TypeManager.GetTypeIndex<T6>();

            // https://bertdobbelaere.github.io/sorting_networks.html#N7L16D6

            OrderLess(ref typeIndex0, ref typeIndex6);
            OrderLess(ref typeIndex2, ref typeIndex3);
            OrderLess(ref typeIndex4, ref typeIndex5);

            OrderLess(ref typeIndex0, ref typeIndex2);
            OrderLess(ref typeIndex1, ref typeIndex4);
            OrderLess(ref typeIndex3, ref typeIndex6);

            OrderLess(ref typeIndex0, ref typeIndex1);
            OrderLess(ref typeIndex2, ref typeIndex5);
            OrderLess(ref typeIndex3, ref typeIndex4);

            OrderLess(ref typeIndex1, ref typeIndex2);
            OrderLess(ref typeIndex4, ref typeIndex6);

            OrderLess(ref typeIndex2, ref typeIndex3);
            OrderLess(ref typeIndex4, ref typeIndex5);

            OrderLess(ref typeIndex1, ref typeIndex2);
            OrderLess(ref typeIndex3, ref typeIndex4);
            OrderLess(ref typeIndex5, ref typeIndex6);

            mutable._sorted.Length = 7;
            mutable._sorted[0] = typeIndex0;
            mutable._sorted[1] = typeIndex1;
            mutable._sorted[2] = typeIndex2;
            mutable._sorted[3] = typeIndex3;
            mutable._sorted[4] = typeIndex4;
            mutable._sorted[5] = typeIndex5;
            mutable._sorted[6] = typeIndex6;

            mutable.m_masks = new ComponentTypeSet.Masks(mutable._sorted);

            CheckForDuplicates(mutable._sorted);
            return Reinterpret<ComponentTypeSetMutable, ComponentTypeSet>(ref mutable);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ComponentTypeSet Create<T0, T1, T2, T3, T4, T5, T6, T7>()
        {
            SkipInit(out ComponentTypeSetMutable mutable);

            TypeIndex typeIndex0 = TypeManager.GetTypeIndex<T0>();
            TypeIndex typeIndex1 = TypeManager.GetTypeIndex<T1>();
            TypeIndex typeIndex2 = TypeManager.GetTypeIndex<T2>();
            TypeIndex typeIndex3 = TypeManager.GetTypeIndex<T3>();
            TypeIndex typeIndex4 = TypeManager.GetTypeIndex<T4>();
            TypeIndex typeIndex5 = TypeManager.GetTypeIndex<T5>();
            TypeIndex typeIndex6 = TypeManager.GetTypeIndex<T6>();
            TypeIndex typeIndex7 = TypeManager.GetTypeIndex<T7>();

            // https://bertdobbelaere.github.io/sorting_networks.html#N8L19D6

            OrderLess(ref typeIndex0, ref typeIndex2);
            OrderLess(ref typeIndex1, ref typeIndex3);
            OrderLess(ref typeIndex4, ref typeIndex6);
            OrderLess(ref typeIndex5, ref typeIndex7);

            OrderLess(ref typeIndex0, ref typeIndex4);
            OrderLess(ref typeIndex1, ref typeIndex5);
            OrderLess(ref typeIndex2, ref typeIndex6);
            OrderLess(ref typeIndex3, ref typeIndex7);

            OrderLess(ref typeIndex0, ref typeIndex1);
            OrderLess(ref typeIndex2, ref typeIndex3);
            OrderLess(ref typeIndex4, ref typeIndex5);
            OrderLess(ref typeIndex6, ref typeIndex7);

            OrderLess(ref typeIndex2, ref typeIndex4);
            OrderLess(ref typeIndex3, ref typeIndex5);

            OrderLess(ref typeIndex1, ref typeIndex4);
            OrderLess(ref typeIndex3, ref typeIndex6);

            OrderLess(ref typeIndex1, ref typeIndex2);
            OrderLess(ref typeIndex3, ref typeIndex4);
            OrderLess(ref typeIndex5, ref typeIndex6);

            mutable._sorted.Length = 8;
            mutable._sorted[0] = typeIndex0;
            mutable._sorted[1] = typeIndex1;
            mutable._sorted[2] = typeIndex2;
            mutable._sorted[3] = typeIndex3;
            mutable._sorted[4] = typeIndex4;
            mutable._sorted[5] = typeIndex5;
            mutable._sorted[6] = typeIndex6;
            mutable._sorted[7] = typeIndex7;

            mutable.m_masks = new ComponentTypeSet.Masks(mutable._sorted);

            CheckForDuplicates(mutable._sorted);
            return Reinterpret<ComponentTypeSetMutable, ComponentTypeSet>(ref mutable);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void OrderLess(ref TypeIndex lhs, ref TypeIndex rhs)
        {
            TypeIndex min = math.min(lhs, rhs);
            TypeIndex max = math.max(lhs, rhs);

            lhs = min;
            rhs = max;
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        [Conditional("UNITY_DOTS_DEBUG")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void CheckForDuplicates(FixedList64Bytes<TypeIndex> sorted)
        {
            if (Hint.Unlikely(sorted.IsEmpty))
            {
                return;
            }

            TypeIndex prev = sorted[0];

            for (int i = 1; i < sorted.Length; i++)
            {
                TypeIndex current = sorted[i];

                if (Hint.Unlikely(prev == current))
                {
                    FixedString128Bytes typeStr = sorted[i].ToFixedString();
                    throw new ArgumentException($"ComponentTypeSet cannot contain duplicate types. Remove all but one occurrence of \"{typeStr}\"");
                }

                prev = current;
            }
        }

        internal struct ComponentTypeSetMutable
        {
            public FixedList64Bytes<TypeIndex> _sorted;
            public ComponentTypeSet.Masks m_masks;
        }
    }
}
