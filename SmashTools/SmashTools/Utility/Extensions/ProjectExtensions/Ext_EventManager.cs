using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace SmashTools
{
	public static class Ext_EventManager
	{
		public static void FillEvents<T>(this IEventManager<T> manager, IEnumerable<T> events)
		{
			if (events.EnumerableNullOrEmpty())
			{
				Log.Error($"Tried to fill IEventManager with empty events. Any attempt to execute an event's actions will throw an exception. Manager=\"{manager}\"");
				return;
			}
			manager.EventRegistry = new Dictionary<T, EventTrigger>();
			foreach (T value in events)
			{
				manager.RegisterEventType(value);
			}
		}

		public static void FillEvents_Enum<T>(this IEventManager<T> manager)
		{
			if (!typeof(T).IsEnum)
			{
				Log.Error($"Tried to fill IEventManager with enum values and non-enum type. Type=\"{typeof(T)}\" Manager=\"{manager}\"");
				return;
			}
			manager.EventRegistry = new Dictionary<T, EventTrigger>();
			foreach (T value in Enum.GetValues(typeof(T)))
			{
				manager.RegisterEventType(value);
			}
		}

		public static void FillEvents_Def<T>(this IEventManager<T> manager) where T : Def
		{
			manager.EventRegistry = new Dictionary<T, EventTrigger>();
			foreach (T def in DefDatabase<T>.AllDefsListForReading)
			{
				manager.RegisterEventType(def);
			}
		}

		public static void RegisterEventType<T>(this IEventManager<T> manager, T @event)
		{
			manager.EventRegistry[@event] = new EventTrigger();
		}

		public static void AddEvent<T>(this IEventManager<T> manager, T @event, Action action, params Action[] actions)
		{
			manager.EventRegistry[@event].Add(action);
			if (!actions.NullOrEmpty())
			{
				foreach (Action additionalAction in actions)
				{
					manager.EventRegistry[@event].Add(additionalAction);
				}
			}
		}

		public static void AddSingleEvent<T>(this IEventManager<T> manager, T @event, Action action, params Action[] actions)
		{
			manager.EventRegistry[@event].AddSingle(action);
			if (!actions.NullOrEmpty())
			{
				foreach (Action additionalAction in actions)
				{
					manager.EventRegistry[@event].AddSingle(additionalAction);
				}
			}
		}

		public static void RemoveEvent<T>(this IEventManager<T> manager, T @event, Action action)
		{
			manager.EventRegistry[@event].Remove(action);
		}

		public static void RemoveSingleEvent<T>(this IEventManager<T> manager, T @event, Action action)
		{
			manager.EventRegistry[@event].RemoveSingle(action);
		}

		public static void ClearAll<T>(this IEventManager<T> manager, T @event)
		{
			manager.EventRegistry[@event].ClearAll();
		}
	}
}
