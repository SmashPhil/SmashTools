using System;
using System.Diagnostics;

namespace JetBrains.Annotations;

[PublicAPI, MeansImplicitUse(ImplicitUseTargetFlags.Itself)]
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Field |
                AttributeTargets.Method | AttributeTargets.Constructor)]
[Conditional("JETBRAINS_ANNOTATIONS")]
public class UsedWithReflection : Attribute
{
}