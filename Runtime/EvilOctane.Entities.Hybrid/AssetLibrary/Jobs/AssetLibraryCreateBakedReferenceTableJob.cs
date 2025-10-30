using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using AssetLibraryConsumerEntityListTable = EvilOctane.Collections.LowLevel.Unsafe.UnsafeSwissTable<Unity.Entities.UnityObjectRef<EvilOctane.Entities.AssetLibrary>, Unity.Collections.LowLevel.Unsafe.UnsafeList<Unity.Entities.Entity>, EvilOctane.Collections.XXH3PodHasher<Unity.Entities.UnityObjectRef<EvilOctane.Entities.AssetLibrary>>>;

namespace EvilOctane.Entities.Internal
{
    [BurstCompile]
    [WithOptions(EntityQueryOptions.IncludePrefab)]
    public unsafe partial struct AssetLibraryCreateBakedReferenceTableJob : IJobEntity
    {
        public NativeReference<AssetLibraryConsumerEntityListTable> BakedReferenceTableRef;

        public AllocatorManager.AllocatorHandle Allocator;

        public void Execute(
            Entity entity,
            DynamicBuffer<AssetLibraryInternal.ReferenceBufferElement> referenceBuffer)
        {
            ref AssetLibraryConsumerEntityListTable referenceTable = ref *BakedReferenceTableRef.GetUnsafePtr();

            foreach (AssetLibraryInternal.ReferenceBufferElement reference in referenceBuffer)
            {
                ref UnsafeList<Entity> entityList = ref referenceTable.GetOrAdd(reference.AssetLibrary, out bool added);

                if (added)
                {
                    // List added
                    entityList = new UnsafeList<Entity>(16, Allocator);
                }
                else
                {
                    // List exists

                    if (entityList.Contains(entity))
                    {
                        // Already added
                        continue;
                    }

                    entityList.EnsureCapacity(1);
                }

                entityList.AddNoResize(entity);
            }
        }
    }
}
