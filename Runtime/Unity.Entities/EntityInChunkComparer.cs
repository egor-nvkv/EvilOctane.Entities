using System.Collections.Generic;
using Unity.Collections;

namespace Unity.Entities
{
    public unsafe struct EntityInChunkComparer : IComparer<Entity>
    {
        [ReadOnly]
        public EntityStorageInfoLookup EntityStorageInfoLookup;

        public readonly int Compare(Entity x, Entity y)
        {
            EntityStorageInfo entityStorageInfoX = EntityStorageInfoLookup[x];
            EntityStorageInfo entityStorageInfoY = EntityStorageInfoLookup[y];

            int cmp0 = ((ulong)entityStorageInfoX.Chunk.m_EntityComponentStore).CompareTo((ulong)entityStorageInfoY.Chunk.m_EntityComponentStore);
            int cmp1 = ((int)entityStorageInfoX.Chunk.m_Chunk).CompareTo((int)entityStorageInfoY.Chunk.m_Chunk);
            int cmp2 = entityStorageInfoX.IndexInChunk.CompareTo(entityStorageInfoY.IndexInChunk);

            return cmp0 != 0 ? cmp0 : (cmp1 != 0 ? cmp1 : cmp2);
        }
    }
}
