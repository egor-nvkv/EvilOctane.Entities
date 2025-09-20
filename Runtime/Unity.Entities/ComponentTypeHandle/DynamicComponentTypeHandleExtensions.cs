using System.Runtime.CompilerServices;

namespace Unity.Entities
{
    public static partial class DynamicComponentTypeHandleExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TypeIndex GetTypeIndex(this DynamicComponentTypeHandle self)
        {
            return self.m_TypeIndex;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ComponentType GetComponentType(this DynamicComponentTypeHandle self)
        {
            return ComponentType.FromTypeIndex(self.m_TypeIndex);
        }
    }
}
