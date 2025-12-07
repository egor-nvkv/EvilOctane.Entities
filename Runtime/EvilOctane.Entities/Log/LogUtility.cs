using System;
using System.Runtime.CompilerServices;
using Unity.Burst.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine;
using static System.Runtime.CompilerServices.Unsafe;

namespace EvilOctane.Entities
{
    [GenerateTestsForBurstCompatibility]
    public static unsafe class LogUtility
    {
        public const int MaxTagLength = 128;

        [HideInCallstack]
        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new Type[] { typeof(FixedString32Bytes), typeof(FixedString32Bytes) })]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void LogTagged<S0, S1>(in S0 primaryTag, in S1 message, LogType logType = LogType.Log)
            where S0 : unmanaged, INativeList<byte>, IUTF8Bytes
            where S1 : unmanaged, INativeList<byte>, IUTF8Bytes
        {
            LogTaggedImpl(
                in primaryTag,
                ByteSpan.Empty,
                in message,
                logType);
        }

        [HideInCallstack]
        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new Type[] { typeof(FixedString32Bytes), typeof(FixedString32Bytes), typeof(FixedString32Bytes) })]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void LogTagged<S0, S1, S2>(in S0 primaryTag, in S1 secondaryTag, in S2 message, LogType logType = LogType.Log)
            where S0 : unmanaged, INativeList<byte>, IUTF8Bytes
            where S1 : unmanaged, INativeList<byte>, IUTF8Bytes
            where S2 : unmanaged, INativeList<byte>, IUTF8Bytes
        {
            LogTaggedImpl(
                in primaryTag,
                in secondaryTag,
                in message,
                logType);
        }

        [HideInCallstack]
        [SkipLocalsInit]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void LogTaggedImpl<S0, S1, S2>(in S0 primaryTag, in S1 secondaryTag, in S2 message, LogType logType)
            where S0 : unmanaged, INativeList<byte>, IUTF8Bytes
            where S1 : unmanaged, INativeList<byte>, IUTF8Bytes
            where S2 : unmanaged, INativeList<byte>, IUTF8Bytes
        {
            SkipInit(out FixedString4096Bytes log);

            ref S0 primaryTagRefRo = ref AsRef(in primaryTag);
            ref S1 secondaryTagRefRo = ref AsRef(in secondaryTag);
            ref S2 messageRefRo = ref AsRef(in message);

            bool hasPrimaryTag = primaryTagRefRo.Length != 0;
            bool hasSecondaryTag = secondaryTagRefRo.Length != 0;

            int primaryTagTruncatedLength = math.min(primaryTagRefRo.Length, MaxTagLength);
            int secondaryTagTruncatedLength = math.min(secondaryTagRefRo.Length, MaxTagLength);

            int prologueLength =
                primaryTagTruncatedLength + (hasPrimaryTag ? 1/* */ : 0) +
                secondaryTagTruncatedLength + (hasSecondaryTag ? 3/*[] */ : 0) +
                2/*| */;

            int messageTruncatedLength = math.min(messageRefRo.Length, log.Capacity - prologueLength);
            int totalLength = prologueLength + messageTruncatedLength;

            log.Length = totalLength;
            int offset = 0;

            // Primary tag
            if (hasPrimaryTag)
            {
                new ByteSpan(log.GetUnsafePtr() + offset, primaryTagTruncatedLength).CopyFrom(new ByteSpan(primaryTagRefRo.GetUnsafePtr(), primaryTagTruncatedLength));
                offset += primaryTagTruncatedLength;

                log[offset++] = (byte)' ';
            }

            // Secondary tag
            if (hasSecondaryTag)
            {
                log[offset++] = (byte)'[';

                new ByteSpan(log.GetUnsafePtr() + offset, secondaryTagTruncatedLength).CopyFrom(new ByteSpan(secondaryTagRefRo.GetUnsafePtr(), secondaryTagTruncatedLength));
                offset += secondaryTagTruncatedLength;

                log[offset++] = (byte)']';
                log[offset++] = (byte)' ';
            }

            // Separator
            log[offset++] = (byte)'|';
            log[offset++] = (byte)' ';

            // Message
            new ByteSpan(log.GetUnsafePtr() + offset, messageTruncatedLength).CopyFrom(new ByteSpan(messageRefRo.GetUnsafePtr(), messageTruncatedLength));

            switch (logType)
            {
                case LogType.Error:
                case LogType.Assert:
                case LogType.Exception:
                    Debug.LogError(log);
                    break;

                case LogType.Warning:
                    Debug.LogWarning(log);
                    break;

                default:
                case LogType.Log:
                    Debug.Log(log);
                    break;
            }
        }
    }
}
