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

            // Asset library reference buffer         
            DynamicBuffer<AssetLibraryInternal.ReferenceBufferElement> referenceBuffer = AddBuffer<AssetLibraryInternal.ReferenceBufferElement>(entity);

            ArraySegment<AssetLibrary> assetLibraries = this.DependsOnMultiple(authoring.assetLibraries);
            referenceBuffer.EnsureCapacityTrashOldData(assetLibraries.Count);

            foreach (AssetLibrary assetLibrary in assetLibraries)
            {
                _ = referenceBuffer.AddNoResize(new AssetLibraryInternal.ReferenceBufferElement() { AssetLibrary = assetLibrary });
            }
        }
    }
}
