using Unity.Entities;

namespace EvilOctane.Entities.Internal
{
    public class AssetLibraryConsumerBaker : Baker<AssetLibraryConsumerAuthoring>
    {
        public override void Bake(AssetLibraryConsumerAuthoring authoring)
        {
            // Asset libraries
            _ = GetComponents<AssetLibraryReferenceAuthoring>();

            Entity entity = GetEntityWithoutDependency();

            // Rebaked
            AddComponent<AssetLibraryConsumer.RebakedTag>(entity);

            // Asset library buffer
            _ = AddBuffer<AssetLibraryConsumer.AssetLibraryBufferElement>(entity);
        }
    }
}
