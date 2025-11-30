using System;
using Unity.Entities;
using UnityObject = UnityEngine.Object;

namespace EvilOctane.Entities
{
    public struct Asset
    {
        [BakingType]
        public struct UnityObjectComponent : IComponentData
        {
            public UnityObjectRef<UnityObject> Value;
        }

        [BakingType]
        public struct TypeHashComponent : ISharedComponentData, IEquatable<TypeHashComponent>
        {
            public ulong Value;

            public readonly bool Equals(TypeHashComponent other)
            {
                return Value == other.Value;
            }

            public override readonly int GetHashCode()
            {
                return Value.GetHashCode();
            }
        }
    }
}
