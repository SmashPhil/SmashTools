using System;

namespace SmashTools.Performance;

public readonly struct ProfilerScope : IDisposable
{
	public ProfilerScope(string label)
	{
		Profiler.Start(label);
	}

	void IDisposable.Dispose()
	{
		Profiler.Stop();
	}
}