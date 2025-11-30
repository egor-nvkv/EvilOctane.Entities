using System;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;

namespace EvilOctane.Entities
{
    public struct AssetTableEntry : IDisposable
    {
        public UnsafeList<Entity> EntityList;

        public void Dispose()
        {
            EntityList.Dispose();
        }
    }
}
