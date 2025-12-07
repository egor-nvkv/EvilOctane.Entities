using EvilOctane.Collections;
using EvilOctane.Collections.LowLevel.Unsafe;
using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using static Unity.Collections.LowLevel.Unsafe.UnsafeUtility;

namespace EvilOctane.Entities
{
    public unsafe struct AssetTable : IDisposable
    {
        public UnsafeSwissTable<ByteSpan, AssetTableEntry, XXH3StringHasher<ByteSpan>> Table;

        public readonly bool IsCreated => Table.IsCreated;

        public AssetTable(int capacity, AllocatorManager.AllocatorHandle allocator)
        {
            Table = new UnsafeSwissTable<ByteSpan, AssetTableEntry, XXH3StringHasher<ByteSpan>>(capacity, allocator);
        }

        public void Dispose()
        {
            DisposeTableElements();
            Table.Dispose();
        }

        public void ClearEnsureCapacity(int capacity)
        {
            DisposeTableElements();
            Table.Clear();
            Table.EnsureCapacity(capacity, keepOldData: false);
        }

        public void AddNoResize(ByteSpan assetName, Entity asset)
        {
            Pointer<AssetTableEntry> entry = Table.TryGet(assetName, out bool exists);

            if (!exists)
            {
                // Create
                ByteSpan assetNamePersistent = AllocateAssetName(assetName);
                entry = Table.Add(assetNamePersistent);
                entry.AsRef = new AssetTableEntry(4, Table.Allocator);
            }

            entry.AsRef.AddUnique(asset, Table.Allocator);
        }

        private readonly ByteSpan AllocateAssetName(ByteSpan tempAssetName)
        {
            byte* ptr = (byte*)MemoryExposed.Unmanaged.Allocate(tempAssetName.Length, AlignOf<ulong>(), Table.Allocator);
            ByteSpan assetName = new(ptr, tempAssetName.Length);
            assetName.CopyFrom(tempAssetName);
            return assetName;
        }

        private readonly void DisposeAssetName(ByteSpan assetName)
        {
            MemoryExposed.Unmanaged.Free(assetName.Ptr, Table.Allocator);
        }

        private readonly void DisposeTableElements()
        {
            foreach (KeyValueRef<ByteSpan, AssetTableEntry> kvPair in Table)
            {
                DisposeAssetName(kvPair.KeyRefRO);
                kvPair.ValueRef.Dispose(Table.Allocator);
            }
        }
    }
}
