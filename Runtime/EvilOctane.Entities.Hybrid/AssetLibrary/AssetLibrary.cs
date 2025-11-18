using System.Collections.Generic;
using UnityEngine;

namespace EvilOctane.Entities
{
    [CreateAssetMenu(fileName = nameof(AssetLibrary), menuName = "Evil Octane/Asset Library")]
    public partial class AssetLibrary : ScriptableObject
    {
        [SerializeField, HideInInspector]
        internal List<Object> assets = new();
    }
}
