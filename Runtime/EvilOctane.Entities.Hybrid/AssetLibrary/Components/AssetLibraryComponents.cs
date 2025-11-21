using System.Runtime.InteropServices;
using Unity.Entities;

namespace EvilOctane.Entities
{
    public partial class AssetLibrary
    {
        /// <summary>
        /// A reference to a baked <see cref=nameof(AssetLibrary)/>.
        /// </summary>
        [BakingType]
        public struct EntityBufferElement : IBufferElementData
        {
            public Entity AssetLibrary;
        }

        /// <summary>
        /// The unmanaged data of a baked <see cref=nameof(AssetLibrary)/>.
        /// </summary>
        [BakingType]
        [InternalBufferCapacity(0)]
        [StructLayout(LayoutKind.Sequential, Size = 1)]
        public struct Storage : IBufferElementData
        {
            public byte RawByte;
        }
    }
}
