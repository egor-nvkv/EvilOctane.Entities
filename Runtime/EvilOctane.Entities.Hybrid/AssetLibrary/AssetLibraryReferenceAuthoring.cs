using UnityEngine;

namespace EvilOctane.Entities
{
    [RequireComponent(typeof(AssetLibraryConsumerAuthoring))]
    public class AssetLibraryReferenceAuthoring : MonoBehaviour
    {
        [SerializeField]
        internal AssetLibrary assetLibrary;

        public AssetLibrary AssetLibrary => assetLibrary;
    }
}
