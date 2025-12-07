using Unity.Entities;

namespace EvilOctane.Entities
{
    public struct AssetLibraryConsumer
    {
        [TemporaryBakingType]
        public struct RebakedTag : IComponentData { }

        /// <summary>
        /// A reference to a baked <see cref="AssetLibrary"/>.
        /// </summary>
        [BakingType]
        public struct AssetLibraryBufferElement : IBufferElementData
        {
            public Entity Entity;
        }
    }
}
