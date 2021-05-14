using System;
using System.Reflection;
using Verse;

namespace SmashTools
{
	public static class Ext_Type
	{
		/// <param name="o"></param>
		/// <returns>True if <paramref name="type"/> is a Byte, SByte, UInt16, UInt32, UInt64, Int16, Int32, Int64, Decimal, Double, or Single</returns>
		public static bool IsNumericType(this Type type)
		{   
			switch (Type.GetTypeCode(type))
			{
				case TypeCode.Byte:
				case TypeCode.SByte:
				case TypeCode.UInt16:
				case TypeCode.UInt32:
				case TypeCode.UInt64:
				case TypeCode.Int16:
				case TypeCode.Int32:
				case TypeCode.Int64:
				case TypeCode.Decimal:
				case TypeCode.Double:
				case TypeCode.Single:
					return true;
				default:
					return false;
			}
		}

		/// <summary>
		/// Check if both types are <c>Numeric</c> inside Pair
		/// </summary>
		/// <typeparam name="T1"></typeparam>
		/// <typeparam name="T2"></typeparam>
		/// <param name="o"></param>
		public static bool IsNumericType<T1, T2>(this Pair<T1, T2> pair)
		{
			return pair.First.GetType().IsNumericType() && pair.Second.GetType().IsNumericType();
		}

		/// <summary>
		/// Create object given <paramref name="type"/> containing the default value of this Type.
		/// </summary>
		/// <param name="type"></param>
		public static object GetDefaultValue(this Type type)
		{
			if (type.IsValueType)
			{
				return Activator.CreateInstance(type);
			}
			else
			{
				return null;
			}
		}

		/// <summary>
		/// Determine if method is overridden in any child class
		/// </summary>
		/// <param name="method"></param>
		public static bool MethodImplemented(this MethodInfo method)
		{
			return method != null && method.GetBaseDefinition().DeclaringType != method.DeclaringType && !method.IsAbstract;
		}
	}
}
