using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Entities.LowLevel.Unsafe;
using Unity.Jobs;
using static System.Runtime.CompilerServices.Unsafe;

namespace EvilOctane.Entities.Internal
{
    [BurstCompile]
    public struct AssetLibraryProcessRebakedJob : IJob
    {
        [ReadOnly]
        public NativeReference<AssetLibraryInstanceTable> InstanceTableRef;
        [ReadOnly]
        public NativeReference<AssetLibraryRebakedSet> RebakedSetRef;

        public AllocatorManager.AllocatorHandle TempAllocator;
        public EntityCommandBuffer CommandBuffer;

        public void Execute()
        {
            ref AssetLibraryRebakedSet rebakedSetRO = ref AsRef(in RebakedSetRef.GetRefReadOnly());

            if (rebakedSetRO.Value.IsEmpty)
            {
                // No rebaked
                return;
            }

            UnsafeList<Entity> toUpdateList = UnsafeListExtensions2.Create<Entity>(rebakedSetRO.Value.Count, TempAllocator);

            foreach (Pointer<UnityObjectRef<AssetLibrary>> assetLibrary in rebakedSetRO.Value)
            {
                Pointer<Entity> entity = InstanceTableRef.Value.Value.TryGet(assetLibrary.AsRef, out bool exists);

                if (!exists)
                {
                    // Not created
                    continue;
                }

                // Set for update
                toUpdateList.AddNoResize(entity.AsRef);
            }

            if (!toUpdateList.IsEmpty)
            {
                // Set for update
                // As simple as adding temp components

                ComponentTypeSet rebakedComponentTypeSet = ComponentTypeSetUtility.Create<
                    RebakedTag,
                    AssetLibraryInternal.TempAssetBufferElement>();

                CommandBuffer.AddComponent(toUpdateList.AsSpan(), in rebakedComponentTypeSet);
            }
        }
    }
}
