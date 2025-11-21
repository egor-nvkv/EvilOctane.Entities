using System;
using Unity.Entities;
using Unity.Entities.LowLevel.Unsafe;

namespace EvilOctane.Entities.Internal
{
    public class AssetLibraryBaker : Baker<AssetLibraryAuthoring>
    {
        public override void Bake(AssetLibraryAuthoring authoring)
        {
            Entity entity = GetEntityWithoutDependency();

            ArraySegment<AssetLibrary> assetLibraries = this.DependsOnMultiple(authoring.assetLibraries);

            if (assetLibraries.Count != 0)
            {
                foreach (AssetLibrary assetLibrary in assetLibraries)
                {
                    // Depends on asset in library
                    _ = this.DependsOnMultiple(assetLibrary.assets);
                }

                // Asset library reference buffer         
                DynamicBuffer<AssetLibraryInternal.ReferenceBufferElement> referenceBuffer = AddBuffer<AssetLibraryInternal.ReferenceBufferElement>(entity);
                referenceBuffer.EnsureCapacityTrashOldData(assetLibraries.Count);

                foreach (AssetLibrary assetLibrary in assetLibraries)
                {
                    _ = referenceBuffer.AddNoResize(new AssetLibraryInternal.ReferenceBufferElement()
                    {
                        AssetLibrary = assetLibrary
                    });
                }
            }
        }
    }
}
