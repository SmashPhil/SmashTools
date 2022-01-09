using System;

namespace SmashTools
{
	[AttributeUsage(AttributeTargets.Class, Inherited = false)]
	public class IsMainThreadAttribute : Attribute
	{
	}
}
