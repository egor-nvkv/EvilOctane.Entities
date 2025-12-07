using System.Runtime.CompilerServices;
using Unity.Entities;

namespace EvilOctane.Entities
{
    public struct AssetWithBufferSearcherFirstMatch<T> : IAssetSearcher
        where T : unmanaged, IBufferElementData
    {
        public BufferLookup<T> BufferLookup;

        public Entity ResultAsset;
        public DynamicBuffer<T> ResultBuffer;

        public readonly AssetSearchResult Result => ResultAsset == Entity.Null ? AssetSearchResult.NotFound : AssetSearchResult.Found;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void VisitAssets(AssetSearchOptions options, Entity assetLibrary, in AssetTableEntry entry, out bool finished)
        {
            foreach (Entity asset in entry.EntitySpan)
            {
                if (!BufferLookup.TryGetBuffer(asset, out DynamicBuffer<T> buffer))
                {
                    // Component missing
                    continue;
                }

                // Found
                ResultAsset = asset;
                ResultBuffer = buffer;
                finished = true;
                return;
            }

            finished = false;
        }
    }
}
