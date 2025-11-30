using Unity.Entities;

namespace EvilOctane.Entities.Internal
{
    public class AssetLibraryConsumerBaker : Baker<AssetLibraryConsumerAuthoring>
    {
        public override void Bake(AssetLibraryConsumerAuthoring authoring)
        {
            _ = GetComponents<AssetLibraryReferenceAuthoring>();

            Entity entity = GetEntityWithoutDependency();

            // Rebaked
            AddComponent<RebakedTag>(entity);

            // Reference buffer
            _ = AddBuffer<AssetLibrary.ReferenceBufferElement>(entity);
        }
    }
}
