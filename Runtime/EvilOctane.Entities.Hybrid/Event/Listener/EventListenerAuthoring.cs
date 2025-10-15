using System;
using Unity.Entities;
using UnityEngine;

namespace EvilOctane.Entities
{
    [DisallowMultipleComponent]
    public class EventListenerAuthoring : MonoBehaviour
    {
        [Header("Event")]
        [SerializeField]
        internal EventFirerAuthoring[] eventFirersToSubscribe;

        public virtual ReadOnlySpan<TypeIndex> DeclaredEventTypes => ReadOnlySpan<TypeIndex>.Empty;
    }
}
