using Unity.Burst;

#if !DEVELOPMENT_BUILD
using Unity.IL2CPP.CompilerServices;
#endif

#if UNITY_WEBGL
[assembly: BurstCompile(OptimizeFor = OptimizeFor.Size)]
#else
[assembly: BurstCompile(OptimizeFor = OptimizeFor.Performance)]
#endif

#if !DEVELOPMENT_BUILD
[assembly: Il2CppSetOption(Option.NullChecks, false)]
[assembly: Il2CppSetOption(Option.ArrayBoundsChecks, false)]
#endif
