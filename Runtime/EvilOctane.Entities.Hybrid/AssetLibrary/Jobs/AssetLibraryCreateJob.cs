using EvilOctane.Collections;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using static System.Runtime.CompilerServices.Unsafe;

namespace EvilOctane.Entities.Internal
{
    [BurstCompile]
    public struct AssetLibraryCreateJob : IJob
    {
        public NativeReference<AssetLibraryInstanceTable> InstanceTableRef;

        [ReadOnly]
        public NativeReference<AssetLibraryConsumerTable> ConsumerTableRef;

        public EntityArchetype AssetLibraryArchetype;
        public ExclusiveEntityTransaction Transaction;

        public void Execute()
        {
            ref AssetLibraryInstanceTable instanceTable = ref InstanceTableRef.GetRef();
            ref AssetLibraryConsumerTable consumerTableRO = ref AsRef(in ConsumerTableRef.GetRefReadOnly());

            foreach (KeyValueRef<UnityObjectRef<AssetLibrary>, UnsafeList<Entity>> kvPair in consumerTableRO.Value)
            {
                UnityObjectRef<AssetLibrary> assetLibrary = kvPair.KeyRefRO;
                Pointer<Entity> entity = instanceTable.Value.GetOrAdd(assetLibrary, out bool added);

                if (!added)
                {
                    // Exists
                    continue;
                }

                // Create instance
                entity.AsRef = Transaction.CreateEntity(AssetLibraryArchetype);

                // Unity object
                Transaction.SetComponentData(entity.AsRef, new AssetLibrary.UnityObjectComponent()
                {
                    Value = assetLibrary
                });
            }
        }
    }
}
