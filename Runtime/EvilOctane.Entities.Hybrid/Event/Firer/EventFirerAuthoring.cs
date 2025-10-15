using System;
using Unity.Entities;
using UnityEngine;

namespace EvilOctane.Entities
{
    [DisallowMultipleComponent]
    public class EventFirerAuthoring : MonoBehaviour
    {
        public virtual ReadOnlySpan<TypeIndex> DeclaredEventTypes => ReadOnlySpan<TypeIndex>.Empty;
    }
}
