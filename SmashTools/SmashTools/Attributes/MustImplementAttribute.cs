using System;
using System.Linq;

namespace SmashTools
{
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = true)]
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
				return type.GetMethods().Where(m => m.Name == mustImplement.MethodName).NotNullAndAny();
			}
			return false;
		}
	}
}
