using System;
using System.Reflection;

namespace SmashTools.Performance;

public sealed unsafe class FuncPtr<R>
{
	private readonly delegate*<R> function;

	public FuncPtr(MethodInfo method)
	{
		if (method is null)
			throw new ArgumentNullException(nameof(method));
		if (method.ReturnType != typeof(R))
			throw new InvalidOperationException($"Unable to get function pointer from {method.Name}, does not have return type {typeof(R).Name}.");
		if (method.GetParameters().Length != 0)
			throw new InvalidOperationException($"Unable to get function pointer from {method.Name}, mismatched parameters.");

		function = (delegate*<R>)method.MethodHandle.GetFunctionPointer();
	}

	public R Invoke()
	{
		return function();
	}
}

public sealed unsafe class FuncPtr<T, R>
{
	private readonly delegate*<T, R> function;

	public FuncPtr(MethodInfo method)
	{
		if (method is null)
			throw new ArgumentNullException(nameof(method));
		if (method.ReturnType != typeof(R))
			throw new InvalidOperationException($"Unable to get function pointer from {method.Name}, does not have return type {typeof(R).Name}.");
		if (method.GetParameters().Length != 1)
			throw new InvalidOperationException($"Unable to get function pointer from {method.Name}, mismatched parameters.");

		function = (delegate*<T, R>)method.MethodHandle.GetFunctionPointer();
	}

	public R Invoke(T arg)
	{
		return function(arg);
	}
}

public sealed unsafe class FuncPtr<T1, T2, R>
{
	private readonly delegate*<T1, T2, R> function;

	public FuncPtr(MethodInfo method)
	{
		if (method is null)
			throw new ArgumentNullException(nameof(method));
		if (method.ReturnType != typeof(R))
			throw new InvalidOperationException($"Unable to get function pointer from {method.Name}, does not have return type {typeof(R).Name}.");
		if (method.GetParameters().Length != 2)
			throw new InvalidOperationException($"Unable to get function pointer from {method.Name}, mismatched parameters.");

		function = (delegate*<T1, T2, R>)method.MethodHandle.GetFunctionPointer();
	}

	public R Invoke(T1 arg1, T2 arg2)
	{
		return function(arg1, arg2);
	}
}

public sealed unsafe class FuncPtr<T1, T2, T3, R>
{
	private readonly delegate*<T1, T2, T3, R> function;

	public FuncPtr(MethodInfo method)
	{
		if (method is null)
			throw new ArgumentNullException(nameof(method));
		if (method.ReturnType != typeof(R))
			throw new InvalidOperationException($"Unable to get function pointer from {method.Name}, does not have return type {typeof(R).Name}.");
		if (method.GetParameters().Length != 3)
			throw new InvalidOperationException($"Unable to get function pointer from {method.Name}, mismatched parameters.");

		function = (delegate*<T1, T2, T3, R>)method.MethodHandle.GetFunctionPointer();
	}

	public R Invoke(T1 arg1, T2 arg2, T3 arg3)
	{
		return function(arg1, arg2, arg3);
	}
}

public sealed unsafe class FuncPtr<T1, T2, T3, T4, R>
{
	private readonly delegate*<T1, T2, T3, T4, R> function;

	public FuncPtr(MethodInfo method)
	{
		if (method is null)
			throw new ArgumentNullException(nameof(method));
		if (method.ReturnType != typeof(R))
			throw new InvalidOperationException($"Unable to get function pointer from {method.Name}, does not have return type {typeof(R).Name}.");
		if (method.GetParameters().Length != 3)
			throw new InvalidOperationException($"Unable to get function pointer from {method.Name}, mismatched parameters.");

		function = (delegate*<T1, T2, T3, T4, R>)method.MethodHandle.GetFunctionPointer();
	}

	public R Invoke(T1 arg1, T2 arg2, T3 arg3, T4 arg4)
	{
		return function(arg1, arg2, arg3, arg4);
	}
}