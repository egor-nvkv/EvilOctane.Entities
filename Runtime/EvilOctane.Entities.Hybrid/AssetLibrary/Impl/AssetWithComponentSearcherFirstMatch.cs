using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Entities;

namespace EvilOctane.Entities
{
    public struct AssetWithComponentSearcherFirstMatch<T> : IAssetSearcher
        where T : unmanaged, IComponentData
    {
        [ReadOnly]
        public ComponentLookup<T> ComponentLookup;

        public Entity ResultAsset;
        public RefRO<T> ResultComponent;

        public readonly AssetSearchResult Result => ResultAsset == Entity.Null ? AssetSearchResult.NotFound : AssetSearchResult.Found;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void VisitAssets(AssetSearchOptions options, Entity assetLibrary, in AssetTableEntry entry, out bool finished)
        {
            foreach (Entity asset in entry.EntitySpan)
            {
                if (!ComponentLookup.TryGetRefRO(asset, out RefRO<T> component))
                {
                    // Component missing
                    continue;
                }

                // Found
                ResultAsset = asset;
                ResultComponent = component;
                finished = true;
                return;
            }

            finished = false;
        }
    }
}
