using System;
using System.Linq;
using Verse;

namespace SmashTools
{
	[StaticConstructorOnStartup]
	public static class ForceMethodImplementation
	{
		static ForceMethodImplementation()
		{
			foreach (Type type in GenTypes.AllTypesWithAttribute<MustImplementAttribute>().Where(t => !t.IsAbstract))
			{
				if (!MustImplementAttribute.MethodImplemented(type, out string name))
				{
					SmashLog.Error($"Method <method>{name}</method> not implemented for <type>{type}</type>");
				}
			}
		}
	}
}
