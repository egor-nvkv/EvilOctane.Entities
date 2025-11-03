using Unity.Burst;
using Unity.Burst.CompilerServices;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;

namespace EvilOctane.Entities.Internal
{
    [BurstCompile]
    public partial struct AssetLibraryUpdateConsumersJob : IJobEntity
    {
        public BufferLookup<AssetLibrary.EntityBufferElement> AssetLibraryEntityBufferLookup;

        public EntityCommandBuffer CommandBuffer;

        public void Execute(
            Entity entity,
            DynamicBuffer<AssetLibraryInternal.ConsumerEntityBufferElement> consumerEntityBuffer)
        {
            foreach (AssetLibraryInternal.ConsumerEntityBufferElement consumerEntity in consumerEntityBuffer)
            {
                bool bufferExists = AssetLibraryEntityBufferLookup.TryGetBuffer(consumerEntity.ConsumerEntity, out DynamicBuffer<AssetLibrary.EntityBufferElement> assetLibraryEntityBuffer, out bool entityExists);

                if (Hint.Unlikely(!entityExists))
                {
                    // Consumer destroyed
                    continue;
                }

                if (bufferExists)
                {
                    // Asset library buffer exists

                    if (!assetLibraryEntityBuffer.AsSpanRO().Reinterpret<Entity>().Contains(entity))
                    {
                        // Add unique
                        _ = assetLibraryEntityBuffer.Add(new AssetLibrary.EntityBufferElement() { AssetLibraryEntity = entity });
                    }
                }
                else
                {
                    // Create asset library buffer
                    assetLibraryEntityBuffer = CommandBuffer.AddBuffer<AssetLibrary.EntityBufferElement>(consumerEntity.ConsumerEntity);

                    assetLibraryEntityBuffer.ResizeUninitializedTrashOldData(1);
                    assetLibraryEntityBuffer[0] = new AssetLibrary.EntityBufferElement() { AssetLibraryEntity = entity };
                }
            }
        }
    }
}
