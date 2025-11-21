using EvilOctane.Collections;
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
            foreach (KeyValueRef<UnityObjectRef<AssetLibrary>, UnsafeList<Entity>> kvPair in BakedReferenceTableRef.Value)
            {
                Entity entity = CommandBuffer.CreateEntity(ThreadIndex, AssetLibraryArchetype);

                // Asset library reference
                CommandBuffer.SetComponent(ThreadIndex, entity, new AssetLibraryInternal.Reference()
                {
                    AssetLibrary = kvPair.KeyRefRO
                });

                // Consumer buffer

                UnsafeSpan<Entity> consumerEntitySpan = kvPair.ValueRef.AsSpan();
                DynamicBuffer<AssetLibraryInternal.ConsumerBufferElement> consumerBuffer = CommandBuffer.SetBuffer<AssetLibraryInternal.ConsumerBufferElement>(ThreadIndex, entity);

                consumerBuffer.ResizeUninitializedTrashOldData(consumerEntitySpan.Length);
                consumerBuffer.AsSpanRW().Reinterpret<Entity>().CopyFrom(consumerEntitySpan);
            }
        }
    }
}
