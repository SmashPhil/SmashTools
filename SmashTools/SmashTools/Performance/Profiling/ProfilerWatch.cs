//#define ONLY_RECORD_MAIN_THREAD
//#define JOIN_RESULTS
#define LAZY_PROFILING

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using static SmashTools.Debug;

namespace SmashTools.Performance
{
	/// <summary>
	/// Better implementation of <see cref="DeepProfiler"/> with exclusion from release builds and proper UI panel.
	/// </summary>
	/// <remarks>Results are joined and averaged across threads, the only separator is depth within a profile block.</remarks>
#if DEBUG && !LAZY_PROFILING
	[StaticConstructorOnStartup]
#endif
	[NoProfiling]
	public static class ProfilerWatch
	{
		private const bool PatchAll = true;
		private const int MaxEntries = 100000;
		private const int FramesPerCapture = 20;
		private const int TotalFramesCaptured = 2000;
		internal const int CacheSize = TotalFramesCaptured / FramesPerCapture;

		private static readonly HashSet<Assembly> autoPatchAssemblies = new HashSet<Assembly>();

		private static Dictionary<int, ThreadProfiler> profilers = new Dictionary<int, ThreadProfiler>();
		private static Dictionary<string, Block> results = new Dictionary<string, Block>();
		
		private static ConcurrentSet<MethodInfo> profilingMethods = new ConcurrentSet<MethodInfo>();

		private static bool profiling;
		private static bool routineRunning;

#if DEBUG
		private static Harmony harmony;
#endif

		static ProfilerWatch()
		{
#if DEBUG
			harmony = new Harmony("SmashTools.ProfilerWatch");
#endif
			FlagModsForProfiling();
			ClearCache();
#if DEBUG && !LAZY_PROFILING
			StartLoggingResults();
#endif

#if RELEASE
			Log.Error($"ProfilerWatch initialized in release build! This will affect performance.");
#endif
		}

		internal static BlockContainer Root { get; private set; } = AsyncPool<BlockContainer>.Get();

		internal static CircularArray<BlockContainer> Cache { get; private set; }

		private static ProfilePatching Patching { get; set; } = ProfilePatching.Disabled;

		public static bool Suspend
		{
			get
			{
				return Time.timeScale == 0;
			}
			set
			{
				Time.timeScale = value ? 0 : 1;
			}
		}

		private static bool Profiling
		{
			get
			{
				return profiling;
			}
			set
			{
				if (profiling == value)
				{
					return;
				}
				profiling = value;
				string status = profiling ? "<color=green>ENABLED</color>" : "<color=gray>DISABLED</color>";
				TSMessage($"<color=orange>[ProfilerWatch]</color> Profiling={status}");
				if (profiling && !routineRunning)
				{
					CoroutineManager.StartCoroutine(CaptureFrameRoutine);
				}
			}
		}

		private static ThreadProfiler Profile
		{
			get
			{
#if ONLY_RECORD_MAIN_THREAD
				if (!UnityData.IsInMainThread) return null;
#endif

				lock (profilers)
				{
					int threadId = Thread.CurrentThread.ManagedThreadId;
					if (!profilers.TryGetValue(threadId, out ThreadProfiler profile))
					{
						profile = new ThreadProfiler();
						profilers[threadId] = profile;
					}
					return profile;
				}
			}
		}

		[Conditional("DEBUG")]
		public static void Start(string label)
		{
			Profile?.Start(label);
		}

		[Conditional("DEBUG")]
		public static void Stop()
		{
			Profile?.Stop();
		}

		[Conditional("DEBUG")]
		public static void ClearCache()
		{
			//Reference assignment is atomic, this is thread safe
			profilers = new Dictionary<int, ThreadProfiler>();
			results = new Dictionary<string, Block>();
			Cache = new CircularArray<BlockContainer>(CacheSize);

			lock (Root)
			{
				if (Root != null)
				{
					Root.Clear();
					AsyncPool<BlockContainer>.Return(Root);
				}
				Root = AsyncPool<BlockContainer>.Get();
			}
		}

		private static IEnumerator CaptureFrameRoutine()
		{
			//Disallow simultaneous CaptureFrame loops from occuring
			if (routineRunning) yield break;

			routineRunning = true;
			while (Profiling)
			{
				yield return new WaitForSeconds(FramesPerCapture / 60f);

				lock (Root)
				{
					Root.Collapse();
					BlockContainer dropped = Cache.Push(Root);
					if (dropped != null)
					{
						dropped.Clear();
						AsyncPool<BlockContainer>.Return(dropped);
					}
					Root = AsyncPool<BlockContainer>.Get();

					lock (results)
					{
						results.Clear();
					}
				}
			}
			routineRunning = false;
		}

		private static void StashResults(Block head)
		{
			if (Suspend) return;

			lock (results)
			{
				if (!results.TryGetValue(head.Label, out Block sharedHead))
				{
					results[head.Label] = head;
					sharedHead = head;
					lock (Root)
					{
						Root.InnerList.Add(sharedHead);
					}
				}
				if (head != sharedHead)
				{
					sharedHead.JoinResults(head);
				}
			}
		}

		#region DEBUG_ONLY

		[Conditional("DEBUG")]
		internal static void StartLoggingResults()
		{
			LongEventHandler.ExecuteWhenFinished(delegate ()
			{
				TaskManager.RunAsync(ApplyProfilePatchesToAllTypes);
				Profiling = true;
			});
		}

		[Conditional("DEBUG")]
		[Conditional("LAZY_PROFILING")]
		internal static void StopLoggingResults()
		{
			LongEventHandler.ExecuteWhenFinished(delegate ()
			{
				Profiling = false;
				//TaskManager.RunAsync(ApplyUnpatchToAllTypes);
			});
		}

		private static void FlagModsForProfiling()
		{
			foreach (ModContentPack mod in LoadedModManager.RunningModsListForReading)
			{
				foreach (Assembly assembly in mod.assemblies.loadedAssemblies)
				{
					if (SmashSettings.profileAssemblies.Contains(assembly.GetName().Name))
					{
						autoPatchAssemblies.Add(assembly);
					}
				}
			}
		}

		private static void ApplyProfilePatchesToAllTypes()
		{
			if (Patching == ProfilePatching.Applying || Patching == ProfilePatching.Enabled) return;

			//Wait for game to finish loading if patches are applied on startup
			while (!PlayDataLoader.Loaded) Thread.Sleep(1000); 

			float elapsedTime = 0;
			while (Patching == ProfilePatching.Removing)
			{
				Thread.Sleep(1000);
				elapsedTime += 1;

				if (elapsedTime > 10) //Abort if process hasn't started after awhile
				{
					return;
				}
			}

			Patching = ProfilePatching.Applying;
			int count = 0;
			foreach (Type type in GenTypes.AllTypes)
			{
				if (type.HasAttribute<NoProfilingAttribute>()) continue;

				bool patchAll = autoPatchAssemblies.Contains(type.Assembly); //Assembly has been flagged for profiling
				patchAll |= PatchAll && type.HasAttribute<ProfileAttribute>(); //Class-wide profiling

				count += ApplyProfilePatchesForType(type, patchAll);
			}
			TSMessage($"<color=orange>[ProfilerWatch]</color> {count} methods being profiled");
			CoroutineManager.QueueInvoke(delegate ()
			{
				Messages.Message($"{count} methods patched for profiling.", MessageTypeDefOf.NeutralEvent);
			});
			Patching = ProfilePatching.Enabled;
		}

		private static int ApplyProfilePatchesForType(Type type, bool patchAll)
		{
			int count = 0;
			foreach (MethodInfo methodInfo in type.GetMethods(BindingFlags.Instance | BindingFlags.Static |
					BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
			{
				if (CanPatch(methodInfo, patchAll))
				{
					count += ProfileMethod(methodInfo);
				}
			}
			foreach (PropertyInfo propertyInfo in type.GetProperties(BindingFlags.Instance | BindingFlags.Static |
					BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
			{
				if (CanPatch(propertyInfo, patchAll))
				{
					count += ProfileProperty(propertyInfo);
				}
			}
			return count;
		}

		private static bool CanPatch(MemberInfo memberInfo, bool patchAll)
		{
			if (memberInfo.HasAttribute<NoProfilingAttribute>())
			{
				return false;
			}
			return patchAll || memberInfo.HasAttribute<ProfileAttribute>();
		}

		private static void ApplyUnpatchToAllTypes()
		{
			if (Patching == ProfilePatching.Removing || Patching == ProfilePatching.Disabled) return;

			float elapsedTime = 0;
			while (Patching == ProfilePatching.Applying)
			{
				Thread.Sleep(1000);
				elapsedTime += 1;

				if (elapsedTime > 10) //Abort if process hasn't started after awhile
				{
					return;
				}
			}
			
			Patching = ProfilePatching.Removing;
			{
				harmony.UnpatchAll(harmony.Id);
				profilingMethods.Clear();
			}
			Patching = ProfilePatching.Disabled;
			CoroutineManager.QueueInvoke(delegate ()
			{
				Messages.Message($"All Profile patches removed.", MessageTypeDefOf.NeutralEvent);
			});
			
		}

		private static int ProfileMethod(MethodInfo methodInfo)
		{
			if (profilingMethods.Contains(methodInfo)) return 0;
			if (methodInfo.ContainsGenericParameters) return 0;								// Generics
			if (methodInfo.IsAbstract) return 0;											// Abstract
			if (!methodInfo.HasMethodBody()) return 0;										// Extern
			if (methodInfo.ReturnType == typeof(IEnumerable<CodeInstruction>)) return 0;	// Transpilers
			if (!methodInfo.IsDeclaredMember()) return 0;									// Concrete implementations

			try
			{
				HarmonyMethod prefix = new HarmonyMethod(AccessTools.Method(typeof(ProfilerWatch), nameof(StartPatch)), priority: Priority.First);
				HarmonyMethod postfix = new HarmonyMethod(AccessTools.Method(typeof(ProfilerWatch), nameof(StopPatch)), priority: Priority.Last);
				harmony.Patch(methodInfo, prefix: prefix, postfix: postfix);
			}
			catch (Exception ex)
			{
				TSError($"Failed to apply harmony patch on {methodInfo.DeclaringType}.{methodInfo.Name}. Exception={ex}");
			}

			CoroutineManager.QueueInvoke(delegate ()
			{
				Messages.Message($"Profiling {methodInfo.DeclaringType.Name}.{methodInfo.Name}", MessageTypeDefOf.SilentInput);
			});

			return 1;
		}

		private static int ProfileProperty(PropertyInfo propertyInfo)
		{
			int patched = 0;
			if (propertyInfo.GetMethod != null)
			{
				patched += ProfileMethod(propertyInfo.GetMethod);
			}
			if (propertyInfo.SetMethod != null)
			{
				patched += ProfileMethod(propertyInfo.SetMethod);
			}
			return patched;
		}

		private static void StartPatch(MethodInfo __originalMethod)
		{
			ProfilerWatch.Start($"{__originalMethod.DeclaringType.Name}.{__originalMethod.Name}");
		}

		private static void StopPatch(MethodInfo __originalMethod)
		{
			ProfilerWatch.Stop();
		}

		#endregion DEBUG_ONLY

		public enum ProfilePatching
		{
			Disabled,
			Enabled,
			Applying,
			Removing,
		}

		/// <summary>
		/// Tracker for execution time of code blocks
		/// </summary>
		/// <remarks>Only accessible from the thread it was created on, so inherently thread safe.</remarks>
		private class ThreadProfiler
		{
			private readonly Stack<Block> blocks = new Stack<Block>();

			public void Start(string label)
			{
				Block parent = blocks.Count > 0 ? blocks.Peek() : null;
				Block block;
				lock (results)
				{
					if (!results.TryGetValue(label, out block))
					{
						block = AsyncPool<Block>.Get();
					}
				}
				block.Set(label, parent);

				blocks.Push(block);
				block.Stopwatch.Start();
			}

			public void Stop()
			{
				Assert(blocks.Count > 0);
				Block block = blocks.Pop();
				block.Stopwatch.Stop();
				block.Record();
				if (block.Parent == null) //Only stash the results from the head of the profile block. Child results can be fetched from there
				{
					StashResults(block);
				}
			}
		}

		internal class BlockContainer
		{
			public List<Block> roots = new List<Block>();

			public double TotalElapsed { get; private set; }

			public List<Block> InnerList => roots;

			public void Collapse()
			{
				TotalElapsed = 0;
				foreach (Block block in roots)
				{
					TotalElapsed += block.Elapsed;
				}
			}

			public void Clear()
			{
				EraseAndReturnRecursive(roots);
				roots.Clear();
				TotalElapsed = 0;
			}

			private static void EraseAndReturnRecursive(List<Block> blocks)
			{
				foreach (Block block in blocks)
				{
					if (!block.Children.NullOrEmpty())
					{
						EraseAndReturnRecursive(block.Children);
					}
					block.Erase();
					AsyncPool<Block>.Return(block);
				}
			}
		}

		/// <summary>
		/// Stopwatch object tree for nested profile scopes
		/// </summary>
		internal class Block
		{
			//Measured in milliseconds
			private double max;
			private double elapsed;
			private int count;

			public string Label { get; private set; }

			public Block Parent { get; private set; }

			public List<Block> Children { get; private set; } = new List<Block>();

			public Stopwatch Stopwatch { get; } = new Stopwatch();

			public double Max => max;

			public double Elapsed => elapsed;

			public int CallCount => count;

			public double Self
			{
				get
				{
					double self = Elapsed;
					if (Children.Count > 0)
					{
						foreach (Block child in Children)
						{
							self -= child.Elapsed;
						}
					}
					return self;
				}
			}

			internal void Set(string label, Block parent)
			{
#if JOIN_RESULTS
				Label = label;
#else
				if (parent == null)
				{
					Label = UnityData.IsInMainThread ? $"{label} (MainThread)" : $"{label} (Thread {Thread.CurrentThread.ManagedThreadId})";
				}
				else
				{
					Label = label;
				}
#endif

				Parent = parent;
				Parent?.Children.Add(this);
			}

			public void JoinResults(Block block)
			{
				if (block.max > max)
				{
					max = block.max;
				}
				elapsed += block.elapsed;
				count += block.count;

				block.count = count; //Syncronize count so both stop recording at max limit
			}

			public void Record()
			{
				if (CallCount < MaxEntries)
				{
					double elapsed = Stopwatch.Elapsed.TotalMilliseconds;
					if (elapsed > max)
					{
						max = elapsed;
					}
					this.elapsed += elapsed;
					count++;
				}
				Stopwatch.Reset();
			}

			public void Erase()
			{
				max = 0;
				elapsed = 0;
				count = 0;
				Parent = null;
				Children.Clear();
			}
		}
	}
}
