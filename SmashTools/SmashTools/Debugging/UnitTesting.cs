using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Verse;

namespace SmashTools.Debugging
{
	[StaticConstructorOnStartup]
	public static class UnitTesting
	{
		private static List<Action> postLoadActions = new List<Action>();

		static UnitTesting()
		{
			List<MethodInfo> methods = new List<MethodInfo>();
			foreach (Type type in GenTypes.AllTypes)
			{
				foreach (MethodInfo method in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static).Where(m => !m.GetParameters().Any()))
				{
					if (method.GetCustomAttribute<UnitTestAttribute>() is UnitTestAttribute unitTest && unitTest.Active)
					{
						postLoadActions.Add(() => method.Invoke(null, new object[] { }));
					}
				}
			}
		}

		public static void ExecutePostLoadTesting() => postLoadActions.ForEach(action => action());
	}
}
