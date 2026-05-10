using System;
using System.Diagnostics;

namespace JetBrains.Annotations;

[PublicAPI, MeansImplicitUse]
[AttributeUsage(AttributeTargets.Field)]
[Conditional("JETBRAINS_ANNOTATIONS")]
public class UsedWithReflection : Attribute
{
}