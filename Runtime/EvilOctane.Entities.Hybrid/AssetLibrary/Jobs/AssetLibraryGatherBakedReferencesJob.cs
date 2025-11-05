using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using AssetLibraryConsumerEntityListTable = EvilOctane.Collections.LowLevel.Unsafe.UnsafeSwissTable<Unity.Entities.UnityObjectRef<EvilOctane.Entities.AssetLibrary>, Unity.Collections.LowLevel.Unsafe.UnsafeList<Unity.Entities.Entity>, EvilOctane.Collections.XXH3PodHasher<Unity.Entities.UnityObjectRef<EvilOctane.Entities.AssetLibrary>>>;

namespace EvilOctane.Entities.Internal
{
    [BurstCompile]
    [WithOptions(EntityQueryOptions.IncludePrefab | EntityQueryOptions.IncludeDisabledEntities)]
    public partial struct AssetLibraryGatherBakedReferencesJob : IJobEntity
    {
        public NativeReference<AssetLibraryConsumerEntityListTable> BakedReferenceTableRef;

        public AllocatorManager.AllocatorHandle Allocator;

        public void Execute(
            Entity entity,
            DynamicBuffer<AssetLibraryInternal.ReferenceBufferElement> referenceBuffer)
        {
            ref AssetLibraryConsumerEntityListTable referenceTable = ref BakedReferenceTableRef.GetRef();
            referenceTable.EnsureSlack(referenceBuffer.Length);

            foreach (AssetLibraryInternal.ReferenceBufferElement reference in referenceBuffer)
            {
                Ref<UnsafeList<Entity>> entityList = referenceTable.GetOrAddNoResize(reference.AssetLibrary, out bool added);

                if (added)
                {
                    // List added
                    entityList.RefRW = UnsafeListExtensions2.Create<Entity>(8, Allocator);
                }
                else
                {
                    // List exists

                    if (entityList.RefRW.Contains(entity))
                    {
                        // Already added
                        continue;
                    }

                    entityList.RefRW.EnsureCapacity(1);
                }

                entityList.RefRW.AddNoResize(entity);
            }
        }
    }
}
