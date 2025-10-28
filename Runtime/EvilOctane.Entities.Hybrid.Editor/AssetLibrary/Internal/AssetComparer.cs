using System;
using System.Collections.Generic;
using UnityObject = UnityEngine.Object;

namespace EvilOctane.Entities.Editor
{
    internal sealed class AssetComparer : IComparer<UnityObject>
    {
        public int Compare(UnityObject x, UnityObject y)
        {
            bool xIsNull = x == null;
            bool yIsNull = y == null;

            if (xIsNull != yIsNull)
            {
                // One is null
                return xIsNull.CompareTo(yIsNull);
            }
            else if (xIsNull && yIsNull)
            {
                // Both are null
                return 0;
            }

            Type xType = x.GetType();
            Type yType = x.GetType();

            if (xType != yType)
            {
                // Order by type
                return ((long)xType.TypeHandle.Value).CompareTo((long)yType.TypeHandle.Value);
            }

            // Order by name
            return x.name.CompareTo(y.name);
        }
    }
}
