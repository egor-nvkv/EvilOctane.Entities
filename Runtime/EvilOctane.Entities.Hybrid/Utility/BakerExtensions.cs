using System;
using System.Collections.Generic;
using Unity.Entities;
using UnityObject = UnityEngine.Object;

namespace EvilOctane.Entities
{
    public static partial class BakerExtensions
    {
        public static void DependsOnMultiple<T>(this IBaker self, IList<T> dependencies)
            where T : UnityObject
        {
            if (dependencies == null)
            {
                return;
            }

            foreach (T dependency in dependencies)
            {
                _ = self.DependsOn(dependency);
            }
        }

        public static ArraySegment<T> DependsOnMultiple<T>(this IBaker self, IList<T> dependencies, bool unique)
            where T : UnityObject
        {
            if (dependencies == null)
            {
                return ArraySegment<T>.Empty;
            }

            int count = dependencies.Count;

            T[] resultDependencies = new T[count];
            int resultCount = 0;

            foreach (T dependency in dependencies)
            {
                T resultDependency = self.DependsOn(dependency);

                if (!resultDependency)
                {
                    continue;
                }

                if (unique)
                {
                    bool duplicate = false;

                    for (int index = 0; index != resultCount; ++index)
                    {
                        if (resultDependency == resultDependencies[index])
                        {
                            duplicate = true;
                            break;
                        }
                    }

                    if (duplicate)
                    {
                        continue;
                    }
                }

                resultDependencies[resultCount++] = resultDependency;
            }

            return new ArraySegment<T>(resultDependencies, 0, resultCount);
        }
    }
}
