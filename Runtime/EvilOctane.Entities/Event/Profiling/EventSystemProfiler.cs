#if ENABLE_PROFILER
using Unity.Burst;
using Unity.Profiling;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace EvilOctane.Entities.Internal
{
    public static class EventSystemProfiler
    {
        public static readonly ProfilerCategory ProfilerCategory = ProfilerCategory.Scripts;

        public const string DuplicateSubscribes = "Duplicate Subscribes";
        public const string PhantomUnsubscribes = "Phantom Unsubscribes";
        public const string PhantomListeners = "Phantom Listeners";

        public const string EventsFired = "Events Fired";
        public const string EventsRouted = "Events Routed";
        public const string EventsNotRouted = "Events Not Routed";

        private struct DuplicateSubscribesTag { }
        public static readonly SharedStatic<ProfilerCounterValue<int>> DuplicateSubscribesCounter = SharedStatic<ProfilerCounterValue<int>>.GetOrCreate<DuplicateSubscribesTag>();

        private struct PhantomUnsubscribesTag { }
        public static readonly SharedStatic<ProfilerCounterValue<int>> PhantomUnsubscribesCounter = SharedStatic<ProfilerCounterValue<int>>.GetOrCreate<PhantomUnsubscribesTag>();

        private struct PhantomListenersTag { }
        public static readonly SharedStatic<ProfilerCounterValue<int>> PhantomListenersCounter = SharedStatic<ProfilerCounterValue<int>>.GetOrCreate<PhantomListenersTag>();

        private struct EventsFiredTag { }
        public static readonly SharedStatic<ProfilerCounterValue<int>> EventsFiredCounter = SharedStatic<ProfilerCounterValue<int>>.GetOrCreate<EventsFiredTag>();

        private struct EventsRoutedTag { }
        public static readonly SharedStatic<ProfilerCounterValue<int>> EventsRoutedCounter = SharedStatic<ProfilerCounterValue<int>>.GetOrCreate<EventsRoutedTag>();

        private struct EventsNotRoutedTag { }
        public static readonly SharedStatic<ProfilerCounterValue<int>> EventsNotRoutedCounter = SharedStatic<ProfilerCounterValue<int>>.GetOrCreate<EventsNotRoutedTag>();

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
    }
}
#endif
