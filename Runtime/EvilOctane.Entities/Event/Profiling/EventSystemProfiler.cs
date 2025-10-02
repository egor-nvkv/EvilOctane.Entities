#if ENABLE_PROFILER
using Unity.Burst;
using Unity.Profiling;
using UnityEngine;
using static Unity.Burst.SharedStatic<Unity.Profiling.ProfilerCounterValue<int>>;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace EvilOctane.Entities.Internal
{
    public static class EventSystemProfiler
    {
        public static readonly ProfilerCategory ProfilerCategory = ProfilerCategory.Scripts;

        public const string DuplicateSubscribes = "Duplicate Listen";
        public const string PhantomUnsubscribes = "Phantom Stop Listening";
        public const string PhantomListeners = "Phantom Listeners";

        public const string EventsFired = "Events Fired";
        public const string EventsRouted = "Events Routed";
        public const string EventsNotRouted = "Events Not Routed";

        public static readonly SharedStatic<ProfilerCounterValue<int>> DuplicateSubscribesCounter = GetOrCreate<DuplicateSubscribesTag>();
        public static readonly SharedStatic<ProfilerCounterValue<int>> PhantomUnsubscribesCounter = GetOrCreate<PhantomUnsubscribesTag>();
        public static readonly SharedStatic<ProfilerCounterValue<int>> PhantomListenersCounter = GetOrCreate<PhantomListenersTag>();
        public static readonly SharedStatic<ProfilerCounterValue<int>> EventsFiredCounter = GetOrCreate<EventsFiredTag>();
        public static readonly SharedStatic<ProfilerCounterValue<int>> EventsRoutedCounter = GetOrCreate<EventsRoutedTag>();
        public static readonly SharedStatic<ProfilerCounterValue<int>> EventsNotRoutedCounter = GetOrCreate<EventsNotRoutedTag>();

#if UNITY_EDITOR
        [InitializeOnLoadMethod]
#endif
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        private static void Initialize()
        {
            DuplicateSubscribesCounter.Data = new(
                ProfilerCategory,
                DuplicateSubscribes,
                ProfilerMarkerDataUnit.Count,
                ProfilerCounterOptions.FlushOnEndOfFrame);

            PhantomUnsubscribesCounter.Data = new(
                ProfilerCategory,
                PhantomUnsubscribes,
                ProfilerMarkerDataUnit.Count,
                ProfilerCounterOptions.FlushOnEndOfFrame);

            PhantomListenersCounter.Data = new(
                ProfilerCategory,
                PhantomListeners,
                ProfilerMarkerDataUnit.Count,
                ProfilerCounterOptions.FlushOnEndOfFrame);

            EventsFiredCounter.Data = new(
                ProfilerCategory,
                EventsFired,
                ProfilerMarkerDataUnit.Count,
                ProfilerCounterOptions.FlushOnEndOfFrame | ProfilerCounterOptions.ResetToZeroOnFlush);

            EventsRoutedCounter.Data = new(
                ProfilerCategory,
                EventsRouted,
                ProfilerMarkerDataUnit.Count,
                ProfilerCounterOptions.FlushOnEndOfFrame | ProfilerCounterOptions.ResetToZeroOnFlush);

            EventsNotRoutedCounter.Data = new(
                ProfilerCategory,
                EventsNotRouted,
                ProfilerMarkerDataUnit.Count,
                ProfilerCounterOptions.FlushOnEndOfFrame | ProfilerCounterOptions.ResetToZeroOnFlush);
        }

        private struct DuplicateSubscribesTag { }
        private struct PhantomUnsubscribesTag { }
        private struct PhantomListenersTag { }
        private struct EventsFiredTag { }
        private struct EventsRoutedTag { }
        private struct EventsNotRoutedTag { }
    }
}
#endif
