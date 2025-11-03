using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using AssetLibraryConsumerEntityListTable = EvilOctane.Collections.LowLevel.Unsafe.UnsafeSwissTable<Unity.Entities.UnityObjectRef<EvilOctane.Entities.AssetLibrary>, Unity.Collections.LowLevel.Unsafe.UnsafeList<Unity.Entities.Entity>, EvilOctane.Collections.XXH3PodHasher<Unity.Entities.UnityObjectRef<EvilOctane.Entities.AssetLibrary>>>;

namespace EvilOctane.Entities.Internal
{
    [BurstCompile]
    public struct AssetLibraryCreateEntitiesJob : IJob
    {
        [ReadOnly]
        public NativeReference<AssetLibraryConsumerEntityListTable> BakedReferenceTableRef;

        [NativeDisableContainerSafetyRestriction]
        public EntityCommandBuffer.ParallelWriter CommandBuffer;

        public EntityArchetype AssetLibraryArchetype;

        [NativeSetThreadIndex]
        public int ThreadIndex;

        public void Execute()
        {
            foreach (Collections.KeyValueRef<UnityObjectRef<AssetLibrary>, UnsafeList<Entity>> kvPair in BakedReferenceTableRef.Value)
            {
                Entity entity = CommandBuffer.CreateEntity(ThreadIndex, AssetLibraryArchetype);

                // Asset library reference
                CommandBuffer.SetComponent(ThreadIndex, entity, new AssetLibraryInternal.Reference()
                {
                    AssetLibrary = kvPair.KeyRefRO
                });

                // Consumer buffer

                UnsafeSpan<Entity> consumerEntitySpan = kvPair.ValueRef.AsSpan();
                DynamicBuffer<AssetLibraryInternal.ConsumerEntityBufferElement> consumerEntityBuffer = CommandBuffer.SetBuffer<AssetLibraryInternal.ConsumerEntityBufferElement>(ThreadIndex, entity);

                consumerEntityBuffer.ResizeUninitializedTrashOldData(consumerEntitySpan.Length);
                consumerEntityBuffer.AsSpanRW().Reinterpret<Entity>().CopyFrom(consumerEntitySpan);
            }
        }
    }
}
