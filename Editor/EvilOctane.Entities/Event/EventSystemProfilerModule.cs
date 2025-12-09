#if ENABLE_PROFILER
using System;
using Unity.Profiling.Editor;
using static EvilOctane.Entities.Internal.EventSystemProfiler;

namespace EvilOctane.Entities.Editor
{
    [Serializable]
    [ProfilerModuleMetadata("Event System")]
    public class EventSystemProfilerModule : ProfilerModule
    {
        private static readonly ProfilerCounterDescriptor[] counterDescriptors = new ProfilerCounterDescriptor[]
        {
            new(EventsFired, ProfilerCategory),
            new(EventsRouted, ProfilerCategory),
            new(EventsNotRouted, ProfilerCategory),
            new(DuplicateSubscribes, ProfilerCategory),
            new(PhantomUnsubscribes, ProfilerCategory),
            new(PhantomListeners, ProfilerCategory)
        };

        public EventSystemProfilerModule() : base(counterDescriptors)
        {
        }
    }
}
#endif
