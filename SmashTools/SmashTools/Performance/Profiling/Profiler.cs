using System.Collections.Generic;
using System.Diagnostics;

namespace SmashTools.Performance;

public static class Profiler
{
	private const int MaxPoolSize = 100;
	private const int PreWarmSize = 50;

	private static readonly ObjectPool<Block> Pool = new(MaxPoolSize, PreWarmSize);
	private static readonly Stack<Block> Blocks = [];


	private static readonly RingBuffer<Result> ResultBuffer = new(1000);


	private static Block current;
	private static Result currentResult;

	static Profiler()
	{
		currentResult = new Result();
		for (int i = 0; i < ResultBuffer.Length; i++)
		{
			ResultBuffer.Push(new Result());
		}
	}

	public static void Start(string label)
	{
		Block block = Pool.Get();
		Blocks.Push(block);
		block.Label = label;
		current = block;
		current.Begin();
	}

	public static void Stop()
	{
		current.End();
		if (Blocks.Pop() != current)
		{
			Trace.Fail("Out of sequence profiler. The results will be incorrect.");
		}
		currentResult.Record(current);
		currentResult = ResultBuffer.Push(currentResult);
		Pool.Return(current);
		Blocks.TryPeek(out current);
	}

	private class Block : IPoolable
	{
		private readonly Stopwatch stopwatch = new();

		public string Label { get; set; }

		bool IPoolable.InPool { get; set; }

		public long ElapsedTicks => stopwatch.ElapsedTicks;

		void IPoolable.Reset()
		{
			stopwatch.Reset();
			Label = null;
		}

		public void Begin()
		{
			stopwatch.Start();
		}

		public void End()
		{
			stopwatch.Stop();
		}
	}

	private record Result
	{
		private string name;
		private long ticks;

		public string Name => name;

		public long Ticks => ticks;

		public void Record(Block block)
		{
			name = block.Label;
			ticks = block.ElapsedTicks;
		}

		public override string ToString()
		{
			return $"{name}: {ticks * 1000 / Stopwatch.Frequency}ms";
		}
	}
}