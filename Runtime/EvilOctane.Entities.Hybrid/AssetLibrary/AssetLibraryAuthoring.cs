using UnityEngine;

namespace EvilOctane.Entities
{
    [DisallowMultipleComponent]
    public class AssetLibraryAuthoring : MonoBehaviour
    {
        [SerializeField]
        internal AssetLibrary[] assetLibraries;
    }
}
