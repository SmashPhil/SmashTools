using System;
using System.Diagnostics;

namespace JetBrains.Annotations;

[PublicAPI, MeansImplicitUse(ImplicitUseTargetFlags.Itself)]
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Constructor)]
[Conditional("JETBRAINS_ANNOTATIONS")]
public class UsedWithReflection : Attribute
{
}