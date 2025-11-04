using EvilOctane.Collections;
using EvilOctane.Collections.LowLevel.Unsafe;
using Unity.Burst;
using Unity.Burst.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;

namespace EvilOctane.Entities.Internal
{
    [BurstCompile]
    public partial struct AssetLibraryUpdateConsumersJob : IJobEntity
    {
        public BufferLookup<AssetLibrary.EntityBufferElement> AssetLibraryEntityBufferLookup;

        public NativeReference<UnsafeSwissSet<Entity, XXH3PodHasher<Entity>>> AssetLibraryEntityBufferAddedSetRef;

        public EntityCommandBuffer CommandBuffer;

        public void Execute(
            Entity entity,
            DynamicBuffer<AssetLibraryInternal.ConsumerEntityBufferElement> consumerEntityBuffer)
        {
            ref UnsafeSwissSet<Entity, XXH3PodHasher<Entity>> bufferAddedSet = ref AssetLibraryEntityBufferAddedSetRef.GetRef();
            bufferAddedSet.EnsureSlack(consumerEntityBuffer.Length);

            foreach (AssetLibraryInternal.ConsumerEntityBufferElement consumerEntity in consumerEntityBuffer)
            {
                bool bufferExists = AssetLibraryEntityBufferLookup.TryGetBuffer(consumerEntity.ConsumerEntity, out DynamicBuffer<AssetLibrary.EntityBufferElement> assetLibraryEntityBuffer, out bool entityExists);

                if (Hint.Unlikely(!entityExists))
                {
                    // Consumer destroyed
                    continue;
                }

                AssetLibrary.EntityBufferElement assetLibraryEntity = new() { AssetLibraryEntity = entity };

                if (bufferExists)
                {
                    // Asset library buffer exists

                    if (!assetLibraryEntityBuffer.AsSpanRO().Reinterpret<Entity>().Contains(entity))
                    {
                        // Add unique
                        _ = assetLibraryEntityBuffer.Add(assetLibraryEntity);
                    }
                }
                else
                {
                    bool createBuffer = bufferAddedSet.AddNoResize(consumerEntity.ConsumerEntity);

                    if (createBuffer)
                    {
                        // Create asset library buffer
                        assetLibraryEntityBuffer = CommandBuffer.AddBuffer<AssetLibrary.EntityBufferElement>(consumerEntity.ConsumerEntity);

                        assetLibraryEntityBuffer.ResizeUninitializedTrashOldData(1);
                        assetLibraryEntityBuffer[0] = assetLibraryEntity;
                    }
                    else
                    {
                        // Asset library buffer created                    
                        CommandBuffer.AppendToBuffer(consumerEntity.ConsumerEntity, assetLibraryEntity);
                    }
                }
            }
        }
    }
}
