using Unity.Entities;

namespace EvilOctane.Entities.Tests
{
    // To create a reasonable number of chunks
    [MaximumChunkCapacity(4)]
    public struct ChunkFiller : IComponentData
    {
    }
}
