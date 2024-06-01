using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Linq;
using System.Threading;
using Verse;
using UnityEngine;

namespace SmashTools
{
	/// <summary>
	/// Queue up tasks that require being on the MainThread
	/// </summary>
	/// <remarks>Can split up loops of logic across multiple frames or queue up actions from other threads to be executed on the MainThread</remarks>
	public class CoroutineManager : MonoBehaviour
	{
		//Execution time to maintain max 1 fps impact converted from ms to seconds
		private const float maxExecutionTimePerFrame = 1000 / (60 * 1000);

		private ConcurrentQueue<Enumerator> enumerators = new ConcurrentQueue<Enumerator>();
		private float executionTimeElapsed = 0;

		private static CoroutineManager instance;

		public bool Running { get; private set; }

		public bool NeedsRestart => !Running && enumerators.Count > 0;

		public static CoroutineManager Instance
		{
			get
			{
				if (!instance)
				{
					instance = InjectToScene();
				}
				return instance;
			}
		}

		public static void QueueInvoke(Action action)
		{
			Instance.enumerators.Enqueue(new Enumerator(action));
		}

		public static void QueueInvoke(Func<IEnumerator> enumerator)
		{
			Instance.enumerators.Enqueue(new Enumerator(enumerator));
		}

		public static void QueueOrInvoke(Action action, float waitSeconds = 0)
		{
			if (waitSeconds > 0)
			{
				CoroutineManager.QueueInvoke(() => YieldForInvoking(action, waitSeconds));
			}
			else
			{
				action();
			}
		}

		public static void StartCoroutine(Func<IEnumerator> enumerator)
		{
			Instance.StartCoroutine(enumerator());
		}

		private static IEnumerator YieldForInvoking(Action action, float waitSeconds)
		{
			action();
			yield return new WaitForSeconds(waitSeconds);
		}

		public static IEnumerator YieldTillKeyDown(KeyCode keyCode)
		{ 
			while (!Input.GetKeyDown(keyCode))
			{
				yield return null;
			}
		}

		[MethodImpl(MethodImplOptions.NoInlining)]
		internal void RunQueue()
		{
			StartCoroutine(ExecuteQueue());
		}

		public static void Reset()
		{
			while (Instance.enumerators.Count > 0)
			{
				Instance.enumerators.TryDequeue(out _);
			}
		}

		/// <summary>
		/// Run enumerator methods or actions in queue taking maximum elapsed ms allowed.
		/// </summary>
		private IEnumerator ExecuteQueue()
		{
			Running = true;
			{
				executionTimeElapsed = Time.realtimeSinceStartup;
				while (enumerators.Count > 0)
				{
					if (!enumerators.TryDequeue(out Enumerator enumerator))
					{
						if (enumerators.Count > 0)
						{
							Log.Error($"Failed to dequeue next enumerator from CoroutineManager. Was this called on the MainThread?");
						}
						yield break;
					}
					if (enumerator.Enumerate)
					{
						yield return enumerator.GetEnumerator();
					}
					else
					{
						enumerator.Invoke();
					}
					
					if (executionTimeElapsed > Time.realtimeSinceStartup + maxExecutionTimePerFrame)
					{
						executionTimeElapsed = Time.realtimeSinceStartup;
						yield return null;
					}
				}
			}
			Running = false;
		}

		internal static CoroutineManager InjectToScene()
		{
			GameObject gameObject = new GameObject("CoroutineManager");
			CoroutineManager coroutineManager = gameObject.AddComponent<CoroutineManager>();
			return coroutineManager;
		}

		private class Enumerator
		{
			public Action action;
			public Func<IEnumerator> enumerator;

			public Enumerator(Func<IEnumerator> enumerator)
			{
				this.enumerator = enumerator;
			}

			public Enumerator(Action action)
			{
				this.action = action;
			}

			public bool Enumerate => enumerator != null;

			public IEnumerator GetEnumerator() => enumerator();

			public void Invoke() => action();

			public override string ToString()
			{
				return enumerator.Method.Name;
			}
		}
	}
}
