using System;
using System.Linq;

namespace SmashTools
{
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = true)]
	public class MustImplementAttribute : Attribute
	{
		private string methodName;

		public MustImplementAttribute(string methodName)
		{
			this.methodName = methodName;
		}

		public static bool MethodImplemented(Type type, out string methodName)
		{
			methodName = string.Empty;
			if (type.GetCustomAttributes(typeof(MustImplementAttribute), true).FirstOrDefault() is MustImplementAttribute mustImplement)
			{
				methodName = mustImplement.methodName;
				return type.GetMethods().Where(m => m.Name == mustImplement.methodName).NotNullAndAny();
			}
			return false;
		}
	}
}
