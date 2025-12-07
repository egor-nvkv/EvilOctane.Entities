using UnityEngine;

namespace EvilOctane.Entities
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(AssetLibraryReferenceAuthoring))]
    public class AssetReferenceAuthoring : MonoBehaviour
    {
        [SerializeField]
        internal Object asset;

        public Object Asset => asset;
    }
}
