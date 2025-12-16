using System;
using System.Collections.Generic;
using UnityEditor;
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

            Type typeX = x.GetType();
            Type typeY = y.GetType();

            if (typeX != typeY)
            {
                // Order by type
                return typeX.AssemblyQualifiedName.CompareTo(typeY.AssemblyQualifiedName);
            }

            string pathX = AssetDatabase.GetAssetPath(x.GetEntityId());
            string pathY = AssetDatabase.GetAssetPath(y.GetEntityId());

            // Order by path
            return pathX.CompareTo(pathY);
        }
    }
}
