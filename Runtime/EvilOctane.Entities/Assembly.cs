using Unity.Burst;

#if !DEVELOPMENT_BUILD
using Unity.IL2CPP.CompilerServices;
#endif

[assembly: BurstCompile(OptimizeFor = OptimizeFor.Performance)]

#if !DEVELOPMENT_BUILD
[assembly: Il2CppSetOption(Option.NullChecks, false)]
[assembly: Il2CppSetOption(Option.ArrayBoundsChecks, false)]
#endif
