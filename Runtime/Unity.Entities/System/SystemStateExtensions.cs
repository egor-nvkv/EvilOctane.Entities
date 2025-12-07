using System.Runtime.CompilerServices;

namespace Unity.Entities
{
    public static unsafe partial class SystemStateExtensions
    {
        /// <summary>
        /// <inheritdoc cref="SystemState.RequireAnyForUpdate(EntityQuery[])"/>
        /// </summary>
        /// <param name="self"></param>
        /// <param name="entityQuery0"></param>
        /// <param name="entityQuery1"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void RequireAnyForUpdate(this ref SystemState self, EntityQuery entityQuery0, EntityQuery entityQuery1)
        {
            EntityQuery* entityQueries = stackalloc EntityQuery[]
            {
                entityQuery0,
                entityQuery1
            };

            self.RequireAnyForUpdate(entityQueries, 2);
        }

        /// <summary>
        /// <inheritdoc cref="SystemState.RequireAnyForUpdate(EntityQuery[])"/>
        /// </summary>
        /// <param name="self"></param>
        /// <param name="entityQuery0"></param>
        /// <param name="entityQuery1"></param>
        /// <param name="entityQuery2"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void RequireAnyForUpdate(this ref SystemState self, EntityQuery entityQuery0, EntityQuery entityQuery1, EntityQuery entityQuery2)
        {
            EntityQuery* entityQueries = stackalloc EntityQuery[]
            {
                entityQuery0,
                entityQuery1,
                entityQuery2
            };

            self.RequireAnyForUpdate(entityQueries, 3);
        }
    }
}
