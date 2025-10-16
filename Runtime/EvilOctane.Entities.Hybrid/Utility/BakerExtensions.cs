using System;
using Unity.Entities;
using UnityEngine;
using UnityObject = UnityEngine.Object;

namespace EvilOctane.Entities
{
    public static partial class BakerExtensions
    {
        public static Span<T1> DependsOnMultiple<T0, T1>(this Baker<T0> self, T1[] dependencies)
            where T0 : Component
            where T1 : UnityObject
        {
            int count = dependencies?.Length ?? 0;

            T1[] resultDependencies = new T1[count];
            int resultCount = 0;

            if (count != 0)
            {
                foreach (T1 dependency in dependencies)
                {
                    T1 resultDependency = self.DependsOn(dependency);

                    if (resultDependency)
                    {
                        resultDependencies[resultCount++] = resultDependency;
                    }
                }
            }

            return new Span<T1>(resultDependencies, 0, resultCount);
        }
    }
}
