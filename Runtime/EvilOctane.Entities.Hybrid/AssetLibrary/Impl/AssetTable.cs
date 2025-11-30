using EvilOctane.Collections;
using EvilOctane.Collections.LowLevel.Unsafe;
using System;
using Unity.Burst.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;

namespace EvilOctane.Entities
{
    public struct AssetTable : IDisposable
    {
        public UnsafeSwissTable<AssetTableKey, AssetTableEntry, AssetTableKey.Hasher> Table;
        public UnsafeList<UnsafeText> AssetNameList;

        public readonly bool IsCreated => Table.IsCreated & AssetNameList.IsCreated;

        public AssetTable(int capacity, AllocatorManager.AllocatorHandle allocator)
        {
            Table = new UnsafeSwissTable<AssetTableKey, AssetTableEntry, AssetTableKey.Hasher>(capacity, allocator);
            AssetNameList = UnsafeListExtensions2.Create<UnsafeText>(capacity, allocator);
        }

        public void Dispose()
        {
            foreach (KeyValueRef<AssetTableKey, AssetTableEntry> kvPair in Table)
            {
                kvPair.ValueRef.Dispose();
            }

            Table.Dispose();

            foreach (UnsafeText item in AssetNameList)
            {
                item.Dispose();
            }

            AssetNameList.Dispose();
        }

        public void ClearEnsureCapacity(int capacity)
        {
            Table.Clear();
            Table.EnsureCapacity(capacity, keepOldData: false);

            foreach (UnsafeText item in AssetNameList)
            {
                item.Dispose();
            }

            AssetNameList.Clear();
            AssetNameList.EnsureCapacity(capacity, keepOldData: false);
        }

        public void AddNoResize(ulong assetTypeHash, ByteSpan assetName, Entity asset)
        {
            UnsafeText assetNamePersistent = UnsafeTextExtensions2.Create(assetName, Table.Allocator);

            AssetTableKey key = new(assetTypeHash, assetNamePersistent);
            Pointer<AssetTableEntry> entry = Table.GetOrAddNoResize(key, out bool added);

            ref UnsafeList<Entity> entityList = ref entry.AsRef.EntityList;

            if (Hint.Likely(added))
            {
                // Create

                AssetNameList.AddNoResize(assetNamePersistent);

                entityList = UnsafeListExtensions2.Create<Entity>(1, Table.Allocator);
                entityList.AddNoResize(asset);
            }
            else
            {
                // Add

                assetNamePersistent.Dispose();

                if (!entityList.AsSpan().Contains(asset))
                {
                    // Add unique
                    entityList.Add(asset);
                }
            }
        }
    }
}
