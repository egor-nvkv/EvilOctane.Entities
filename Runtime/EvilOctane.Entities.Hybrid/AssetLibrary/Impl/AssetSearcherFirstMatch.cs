using System.Runtime.CompilerServices;
using Unity.Burst.CompilerServices;
using Unity.Collections;
using Unity.Entities;
using UnityObject = UnityEngine.Object;

namespace EvilOctane.Entities
{
    public struct AssetSearcherFirstMatch : IAssetSearcher
    {
        [ReadOnly]
        public ComponentLookup<Asset.UnityObjectComponent> UnityObjectLookup;

        public ulong AssetTypeHash;
        public UnityObjectRef<UnityObject> ResultAsset;

        public readonly AssetSearchResult Result => ResultAsset == new UnityObjectRef<UnityObject>() ? AssetSearchResult.NotFound : AssetSearchResult.Found;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void VisitAssets(AssetSearchOptions options, Entity assetLibrary, in AssetTableEntry entry, out bool finished)
        {
            foreach (Entity asset in entry.EntitySpan)
            {
                if (Hint.Unlikely(!UnityObjectLookup.TryGetComponent(asset, out Asset.UnityObjectComponent assetObj)))
                {
                    // Component missing
                    continue;
                }
                else if (Hint.Unlikely(AssetTypeHash != assetObj.TypeHash))
                {
                    // Wrong type
                    continue;
                }

                // Found
                ResultAsset = assetObj.Ref;
                finished = true;
                return;
            }

            finished = false;
        }
    }
}
