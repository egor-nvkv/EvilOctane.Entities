using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Burst.CompilerServices;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;

namespace EvilOctane.Entities.Internal
{
    [BurstCompile]
    [WithOptions(EntityQueryOptions.IncludePrefab)]
    public partial struct AssetLibraryCheckReferencesJob : IJobEntity
    {
        public BufferLookup<AssetLibrary.EntityBufferElement> AssetLibraryEntityBufferLookup;

        public EntityCommandBuffer CommandBuffer;

        public void Execute(
            Entity entity,
            AssetLibraryInternal.Reference reference,
            ref DynamicBuffer<AssetLibraryInternal.ConsumerEntityBufferElement> consumerEntityBuffer)
        {
            bool isValid = reference.AssetLibrary.IsValid();

            if (Hint.Unlikely(!isValid))
            {
                // Invalid
                CleanupInvalid(entity, consumerEntityBuffer);
                return;
            }

            for (int index = 0; index != consumerEntityBuffer.Length;)
            {
                AssetLibraryInternal.ConsumerEntityBufferElement consumerEntity = consumerEntityBuffer[index];

                if (!AssetLibraryEntityBufferLookup.TryGetBuffer(consumerEntity.ConsumerEntity, out DynamicBuffer<AssetLibrary.EntityBufferElement> assetLibraryEntityBuffer))
                {
                    // No asset library references
                    goto Remove;
                }

                bool hasReference = !assetLibraryEntityBuffer.AsSpanRO().Reinterpret<Entity>().Contains(entity);

                if (!hasReference)
                {
                    // No references to this asset library
                    goto Remove;
                }

                ++index;
                continue;

            Remove:
                consumerEntityBuffer.RemoveAtSwapBack(index);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void CleanupInvalid(Entity entity, DynamicBuffer<AssetLibraryInternal.ConsumerEntityBufferElement> consumerEntityBuffer)
        {
            // Clean up consumers
            foreach (AssetLibraryInternal.ConsumerEntityBufferElement consumerEntity in consumerEntityBuffer)
            {
                if (AssetLibraryEntityBufferLookup.TryGetBuffer(consumerEntity.ConsumerEntity, out DynamicBuffer<AssetLibrary.EntityBufferElement> assetLibraryEntityBuffer))
                {
                    // Remove reference to asset library
                    _ = assetLibraryEntityBuffer.Reinterpret<Entity>().RemoveFirstMatchSwapBack(entity);
                }
            }

            // Destroy
            CommandBuffer.DestroyEntity(entity);
        }
    }
}
