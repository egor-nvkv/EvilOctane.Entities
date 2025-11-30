using Unity.Burst;
using Unity.Burst.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Entities.LowLevel.Unsafe;
using static System.Runtime.CompilerServices.Unsafe;

namespace EvilOctane.Entities.Internal
{
    [BurstCompile]
    public partial struct AssetLibraryCreateAssetEntitiesJob : IJobEntity
    {
        public EntityArchetype AssetArchetype;
        public EntityCommandBuffer.ParallelWriter CommandBuffer;

        [SkipLocalsInit]
        public void Execute(
            [ChunkIndexInQuery] int chunkIndexInQuery,
            Entity entity,
            in DynamicBuffer<AssetLibraryInternal.TempAssetBufferElement> tempAssetBuffer,
            ref DynamicBuffer<AssetLibrary.AssetBufferElement> assetBuffer)
        {
            if (!assetBuffer.IsEmpty)
            {
                // Destroy old
                CommandBuffer.DestroyEntity(chunkIndexInQuery, assetBuffer.AsSpanRO().Reinterpret<Entity>());
                assetBuffer.Clear();
            }

            foreach (AssetLibraryInternal.TempAssetBufferElement tempAsset in tempAssetBuffer)
            {
                Entity asset = CommandBuffer.CreateEntity(chunkIndexInQuery, AssetArchetype);

                // Entity name
                SkipInit(out FixedString64Bytes entityName);
                entityName.Length = 0;

                entityName.AppendTruncateUnchecked(tempAsset.Name);
                CommandBuffer.SetName(chunkIndexInQuery, asset, entityName);

                // Register
                CommandBuffer.AppendToBuffer(chunkIndexInQuery, entity, new AssetLibrary.AssetBufferElement() { Entity = asset });

                // Owner
                CommandBuffer.SetSharedComponent(chunkIndexInQuery, asset, new AssetLibrary.AssetBufferElement.OwnerShared()
                {
                    AssetLibrary = entity
                });

                // Unity object
                CommandBuffer.SetComponent(chunkIndexInQuery, asset, new Asset.UnityObjectComponent()
                {
                    Value = tempAsset.Asset
                });

                // Type hash
                CommandBuffer.SetSharedComponent(chunkIndexInQuery, asset, new Asset.TypeHashComponent()
                {
                    Value = tempAsset.TypeHash
                });
            }
        }
    }
}
