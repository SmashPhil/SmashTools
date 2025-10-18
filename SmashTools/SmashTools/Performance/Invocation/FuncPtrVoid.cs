using System;
using System.Reflection;

namespace SmashTools.Performance;

public sealed unsafe class FuncPtrVoid
{
	private readonly delegate*<void> function;

	public FuncPtrVoid(MethodInfo method)
	{
		if (method is null)
			throw new ArgumentNullException(nameof(method));
		if (method.ReturnType != typeof(void))
			throw new InvalidOperationException($"Unable to get function pointer from {method.Name}, does not have return type void.");
		if (method.GetParameters().Length != 0)
			throw new InvalidOperationException($"Unable to get function pointer from {method.Name}, mismatched parameters.");

		function = (delegate*<void>)method.MethodHandle.GetFunctionPointer();
	}

	public void Invoke()
	{
		function();
	}
}

public sealed unsafe class FuncPtrVoid<T>
{
	private readonly delegate*<T, void> function;

	public FuncPtrVoid(MethodInfo method)
	{
		if (method is null)
			throw new ArgumentNullException(nameof(method));
		if (method.ReturnType != typeof(void))
			throw new InvalidOperationException($"Unable to get function pointer from {method.Name}, does not have return type void.");
		if (method.GetParameters().Length != 0)
			throw new InvalidOperationException($"Unable to get function pointer from {method.Name}, mismatched parameters.");

		function = (delegate*<T, void>)method.MethodHandle.GetFunctionPointer();
	}

	public void Invoke(T arg)
	{
		function(arg);
	}
}

public sealed unsafe class FuncPtrVoid<T1, T2>
{
	private readonly delegate*<T1, T2, void> function;

	public FuncPtrVoid(MethodInfo method)
	{
		if (method is null)
			throw new ArgumentNullException(nameof(method));
		if (method.ReturnType != typeof(void))
			throw new InvalidOperationException($"Unable to get function pointer from {method.Name}, does not have return type void.");
		if (method.GetParameters().Length != 2)
			throw new InvalidOperationException($"Unable to get function pointer from {method.Name}, mismatched parameters.");

		function = (delegate*<T1, T2, void>)method.MethodHandle.GetFunctionPointer();
	}

	public void Invoke(T1 arg1, T2 arg2)
	{
		function(arg1, arg2);
	}
}

public sealed unsafe class FuncPtrVoid<T1, T2, T3>
{
	private readonly delegate*<T1, T2, T3, void> function;

	public FuncPtrVoid(MethodInfo method)
	{
		if (method is null)
			throw new ArgumentNullException(nameof(method));
		if (method.ReturnType != typeof(void))
			throw new InvalidOperationException($"Unable to get function pointer from {method.Name}, does not have return type void.");
		if (method.GetParameters().Length != 3)
			throw new InvalidOperationException($"Unable to get function pointer from {method.Name}, mismatched parameters.");

		function = (delegate*<T1, T2, T3, void>)method.MethodHandle.GetFunctionPointer();
	}

	public void Invoke(T1 arg1, T2 arg2, T3 arg3)
	{
		function(arg1, arg2, arg3);
	}
}

public sealed unsafe class FuncPtrVoid<T1, T2, T3, T4>
{
	private readonly delegate*<T1, T2, T3, T4, void> function;

	public FuncPtrVoid(MethodInfo method)
	{
		if (method is null)
			throw new ArgumentNullException(nameof(method));
		if (method.ReturnType != typeof(void))
			throw new InvalidOperationException($"Unable to get function pointer from {method.Name}, does not have return type void.");
		if (method.GetParameters().Length != 3)
			throw new InvalidOperationException($"Unable to get function pointer from {method.Name}, mismatched parameters.");

		function = (delegate*<T1, T2, T3, T4, void>)method.MethodHandle.GetFunctionPointer();
	}

	public void Invoke(T1 arg1, T2 arg2, T3 arg3, T4 arg4)
	{
		function(arg1, arg2, arg3, arg4);
	}
}