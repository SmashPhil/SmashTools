using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace SmashTools
{
	public enum OperationType
	{
		Addition,
		Subtraction,
		Multiplication,
		Division,
		Remainder,
		Root,
		Pow,
	}

	public static class MathOp
	{
		public static float Apply(this OperationType operationType, int x, int y)
		{
			return operationType switch
			{
				OperationType.Addition => x + y,
				OperationType.Subtraction => x - y,
				OperationType.Multiplication => x * y,
				OperationType.Division => x / y,
				OperationType.Remainder => x % y, //remainder of x ÷ y
				OperationType.Root => Mathf.Pow(Mathf.Abs(y), 1 / x), // x√y (x root of y)
				OperationType.Pow => Mathf.Pow(x, y), //x raised to the power of y
				_ => 0
			};
		}

		public static float Apply(this OperationType operationType, float x, float y)
		{
			return operationType switch
			{
				OperationType.Addition => x + y,
				OperationType.Subtraction => x - y,
				OperationType.Multiplication => x * y,
				OperationType.Division => x / y,
				OperationType.Remainder => x % y, //remainder of x ÷ y
				OperationType.Root => Mathf.Pow(Mathf.Abs(y), 1 / x), // x√y (x root of y)
				OperationType.Pow => Mathf.Pow(x, y), //x raised to the power of y
				_ => 0
			};
		}
	}
}
