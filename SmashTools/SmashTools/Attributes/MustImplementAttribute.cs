using System;
using System.Linq;
using System.Reflection;

namespace SmashTools
{
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface, Inherited = true)]
	public class MustImplementAttribute : Attribute
	{
		private string MethodName { get; set; }

		public MustImplementAttribute(string methodName)
		{
			MethodName = methodName;
		}

		public static bool MethodImplemented(Type type, out string methodName)
		{
			methodName = string.Empty;
			if (type.GetCustomAttributes(typeof(MustImplementAttribute), true).FirstOrDefault() is MustImplementAttribute mustImplement)
			{
				methodName = mustImplement.MethodName;
				return type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static).Where(m => m.Name == mustImplement.MethodName).NotNullAndAny();
			}
			return false;
		}
	}
}
