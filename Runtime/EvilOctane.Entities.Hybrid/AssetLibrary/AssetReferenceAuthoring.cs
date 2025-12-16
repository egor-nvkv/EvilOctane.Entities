using UnityEngine;

namespace EvilOctane.Entities
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(AssetLibraryReferenceAuthoring))]
    public class AssetReferenceAuthoring : MonoBehaviour
    {
        [SerializeField]
        internal AssetLibrary assetLibrary;
        [SerializeField]
        internal Object asset;

        public AssetLibrary AssetLibrary => assetLibrary;
        public Object Asset => asset;
    }
}
