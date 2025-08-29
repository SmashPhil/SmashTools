using System;
using System.Collections.Generic;
using System.Threading;
using JetBrains.Annotations;
using SmashTools.Targeting;
using UnityEngine;
using UnityEngine.Assertions;
using Verse;

namespace SmashTools.Performance;

/// <summary>
/// Provides a Unity MonoBehaviour to marshal actions onto the main thread
/// and maintain per-frame and per-GUI delegates.
/// </summary>
[PublicAPI]
[StaticConstructorOnStartup]
public sealed class UnityThread : MonoBehaviour
{
	private static SynchronizationContext mainContext;

	private readonly List<OnUpdate> onUpdateMethods = [];
	private readonly List<OnGui> onGuiMethods = [];

	/// <summary>
	/// Delegate invoked each frame during Update.
	/// </summary>
	/// <returns>
	/// <see langword="true"/> if <see cref="OnUpdate"/> should remain in queue for the next frame.
	/// <see langword="false"/> if it should be dequeued immediately.
	/// </returns>
	public delegate bool OnUpdate();

	/// <summary>
	/// Delegate invoked each GUI event during OnGUI.
	/// </summary>
	/// <returns>
	/// <see langword="true"/> if <see cref="OnGui"/> should remain in queue for the next event.
	/// <see langword="false"/> if it should be dequeued immediately.
	/// </returns>
	public delegate bool OnGui();

	static UnityThread()
	{
		Instance = InjectToScene();
	}

	private static UnityThread Instance { get; }

	private void Awake()
	{
		mainContext = SynchronizationContext.Current;
	}

	private void Update()
	{
		TargeterDispatcher.TargeterUpdate();
		for (int i = onUpdateMethods.Count - 1; i >= 0; i--)
		{
			if (!onUpdateMethods[i]())
				onUpdateMethods.RemoveAt(i);
		}
	}

	private void OnGUI()
	{
		TargeterDispatcher.TargeterOnGUI();
		for (int i = onGuiMethods.Count - 1; i >= 0; i--)
		{
			try
			{
				if (!onGuiMethods[i]())
					onGuiMethods.RemoveAt(i);
			}
			catch (Exception ex)
			{
				onGuiMethods.RemoveAt(i);
				Log.Error($"Exception thrown from OnGUI.{Environment.NewLine}{ex}");
			}
		}
	}

	/// <summary>
	/// Removes a previously enqueued OnUpdate delegate. Must be called from the main thread.
	/// </summary>
	/// <param name="onUpdate">The delegate to remove.</param>
	public static void RemoveUpdate(OnUpdate onUpdate)
	{
		if (!UnityData.IsInMainThread)
		{
			Trace.Fail(
				"Trying to remove update method to queue from another thread. This can only be done from the main thread.");
			return;
		}
		Instance.onUpdateMethods.Remove(onUpdate);
	}

	/// <summary>
	/// Enqueues an OnUpdate delegate to run once per frame. Must be called from the main thread.
	/// </summary>
	/// <param name="onUpdate">The delegate to enqueue.</param>
	public static void StartUpdate(OnUpdate onUpdate)
	{
		if (!UnityData.IsInMainThread)
		{
			Trace.Fail(
				"Trying to add update method to queue from another thread. This can only be done from the main thread.");
			return;
		}
		Instance.onUpdateMethods.Add(onUpdate);
	}

	/// <summary>
	/// Enqueues an OnGui delegate to run each GUI event. Must be called from the main thread.
	/// </summary>
	/// <param name="onGui">The delegate to enqueue.</param>
	public static void StartGUI(OnGui onGui)
	{
		if (!UnityData.IsInMainThread)
		{
			Trace.Fail(
				"Trying to add OnGUI method to queue from another thread. This can only be done from the main thread.");
			return;
		}
		Instance.onGuiMethods.Add(onGui);
	}

	/// <summary>
	/// Removes a previously enqueued OnGui delegate.
	/// Must be called from the main thread.
	/// </summary>
	/// <param name="onGui">The delegate to remove.</param>
	public static void RemoveOnGUI(OnGui onGui)
	{
		if (!UnityData.IsInMainThread)
		{
			Trace.Fail(
				"Trying to remove OnGUI method to queue from another thread. This can only be done from the main thread.");
			return;
		}
		Instance.onGuiMethods.Remove(onGui);
	}

	/// <summary>
	/// Posts one or more actions to run asynchronously on the main Unity thread.
	/// </summary>
	/// <param name="invokeList">Actions to execute.</param>
	/// <exception cref="ArgumentNullException">If <paramref name="invokeList"/> is null or empty.</exception>
	public static void ExecuteOnMainThread(params Action[] invokeList)
	{
		if (invokeList.NullOrEmpty())
			throw new ArgumentNullException(nameof(invokeList));

		if (UnityData.IsInMainThread)
		{
			foreach (Action action in invokeList)
				action();
			return;
		}
		ConcurrentAction concurrentAction = new(invokeList);
		mainContext.Post(concurrentAction.InvokeAndDispose, null);
	}

	/// <summary>
	/// Posts one or more actions to run on the main Unity thread, blocking until completion or timeout.
	/// </summary>
	/// <param name="waitTimeout">Milliseconds to wait before timing out.</param>
	/// <param name="invokeList">Actions to execute.</param>
	/// <exception cref="ArgumentNullException">If <paramref name="invokeList"/> is null or empty.</exception>
	/// <exception cref="ArgumentException">If <paramref name="waitTimeout"/> is not positive.</exception>
	/// <exception cref="AssertionException">If the wait timed out.</exception>
	public static void ExecuteOnMainThreadAndWait(int waitTimeout = 5000, params Action[] invokeList)
	{
		if (invokeList.NullOrEmpty())
			throw new ArgumentNullException(nameof(invokeList));
		if (waitTimeout <= 0)
			throw new ArgumentException("waitTimeout must be greater than 0.");

		if (UnityData.IsInMainThread)
		{
			foreach (Action action in invokeList)
				action();
			return;
		}
		ConcurrentAction concurrentAction = new(invokeList);
		mainContext.Post(concurrentAction.InvokeAndDispose, null);
		bool waited = concurrentAction.Wait(waitTimeout);
		Assert.IsTrue(waited, "WaitHandle timed out.");
	}

	/// <summary>
	/// Instantiates this MonoBehaviour in a new GameObject and marks it DontDestroyOnLoad.
	/// </summary>
	private static UnityThread InjectToScene()
	{
		GameObject gameObject = new("UnityThread");
		UnityThread manager = gameObject.AddComponent<UnityThread>();
		DontDestroyOnLoad(gameObject);
		return manager;
	}

	/// <summary>
	/// UnitTest hook for validating that the update list enqueued the delegate correctly.
	/// </summary>
	internal static bool InUpdateQueue(OnUpdate update)
	{
		return Instance.onUpdateMethods.Contains(update);
	}

	/// <summary>
	/// Helper that wraps multiple actions and a wait handle for ExecuteOnMainThreadAndWait.
	/// </summary>
	private class ConcurrentAction : IDisposable
	{
		private readonly Action[] actions;
		private readonly ManualResetEventSlim waitHandle = new();

		public ConcurrentAction(Action[] actions)
		{
			this.actions = actions;
		}

		/// <summary>
		/// Invokes all actions on the main thread, then disposes the wait handle.
		/// </summary>
		public void InvokeAndDispose(object state)
		{
			try
			{
				Assert.IsTrue(UnityData.IsInMainThread);
				foreach (Action action in actions)
				{
					action();
				}
			}
			finally
			{
				Dispose();
			}
		}

		/// <summary>
		/// Blocks the calling thread until the actions have completed or timeout.
		/// </summary>
		/// <param name="waitTimeout">Milliseconds to wait.</param>
		/// <returns><see langword="true"/> if signaled within the timeout; otherwise <see langword="false"/>.</returns>
		public bool Wait(int waitTimeout)
		{
			Assert.IsFalse(UnityData.IsInMainThread);
			bool waited = waitHandle.Wait(waitTimeout);
			return waited;
		}

		/// <summary>
		/// Dispose the wait handle.
		/// </summary>
		public void Dispose()
		{
			waitHandle.Dispose();
			GC.SuppressFinalize(this);
		}
	}
}