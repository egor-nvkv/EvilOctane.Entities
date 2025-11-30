using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Entities.Hybrid.Baking;
using static System.Runtime.CompilerServices.Unsafe;

namespace EvilOctane.Entities.Internal
{
    [BurstCompile]
    [WithOptions(EntityQueryOptions.IncludePrefab | EntityQueryOptions.IncludeDisabledEntities)]
    public partial struct AssetLibraryUpdateReferencesJob : IJobEntity
    {
        [ReadOnly]
        public ComponentLookup<AssetLibraryInternal.DeclaredReference> DeclaredReferenceLookup;

        [ReadOnly]
        public NativeReference<AssetLibraryInstanceTable> InstanceTableRef;

        public void Execute(
            in DynamicBuffer<AdditionalEntitiesBakingData> additionalEntities,
            ref DynamicBuffer<AssetLibrary.ReferenceBufferElement> referenceBuffer)
        {
            ref AssetLibraryInstanceTable instanceTable = ref AsRef(in InstanceTableRef.GetRefReadOnly());
            referenceBuffer.Clear();

            foreach (AdditionalEntitiesBakingData additionalEntity in additionalEntities)
            {
                if (!DeclaredReferenceLookup.TryGetComponent(additionalEntity.Value, out AssetLibraryInternal.DeclaredReference reference))
                {
                    // Component missing
                    continue;
                }

                Pointer<Entity> instance = instanceTable.Value.TryGet(reference.AssetLibrary, out bool exists);

                if (!exists)
                {
                    continue;
                }

                if (!referenceBuffer.AsSpanRO().Reinterpret<Entity>().Contains(instance.AsRef))
                {
                    // Add reference
                    _ = referenceBuffer.Add(new AssetLibrary.ReferenceBufferElement() { Entity = instance.AsRef });
                }
            }
        }
    }
}
