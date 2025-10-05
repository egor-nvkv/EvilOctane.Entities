namespace EvilOctane.Entities
{
    public partial struct EventFirer
    {
        public struct IsAliveTag : ICleanupComponentsAliveTag
        {
        }
    }
}
