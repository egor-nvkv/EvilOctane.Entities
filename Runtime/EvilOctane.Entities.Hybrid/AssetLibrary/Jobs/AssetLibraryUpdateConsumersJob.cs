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
            in DynamicBuffer<AssetLibraryInternal.ConsumerBufferElement> consumerBuffer)
        {
            ref UnsafeSwissSet<Entity, XXH3PodHasher<Entity>> bufferAddedSet = ref AssetLibraryEntityBufferAddedSetRef.GetRef();
            bufferAddedSet.EnsureSlack(consumerBuffer.Length);

            foreach (AssetLibraryInternal.ConsumerBufferElement consumerEntity in consumerBuffer)
            {
                bool bufferExists = AssetLibraryEntityBufferLookup.TryGetBuffer(consumerEntity.Entity, out DynamicBuffer<AssetLibrary.EntityBufferElement> assetLibraryEntityBuffer, out bool entityExists);

                if (Hint.Unlikely(!entityExists))
                {
                    // Consumer destroyed
                    continue;
                }

                AssetLibrary.EntityBufferElement assetLibraryEntity = new() { AssetLibrary = entity };

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
                    bool createBuffer = bufferAddedSet.AddNoResize(consumerEntity.Entity);

                    if (createBuffer)
                    {
                        // Create asset library buffer
                        assetLibraryEntityBuffer = CommandBuffer.AddBuffer<AssetLibrary.EntityBufferElement>(consumerEntity.Entity);

                        assetLibraryEntityBuffer.ResizeUninitializedTrashOldData(1);
                        assetLibraryEntityBuffer[0] = assetLibraryEntity;
                    }
                    else
                    {
                        // Asset library buffer created                    
                        CommandBuffer.AppendToBuffer(consumerEntity.Entity, assetLibraryEntity);
                    }
                }
            }
        }
    }
}
