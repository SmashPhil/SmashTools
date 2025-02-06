using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Verse;
using UnityEngine;
using SmashTools.Performance;

namespace SmashTools
{
	public class Dialog_DedicatedThreadActivity : Window
	{
		private const float AsyncActionEntryHeight = 30;

		private DedicatedThread dedicatedThread;
		private Func<DedicatedThread> dedicatedThreadGetter;

		private int queueLimit = 50;

		private Vector2 scrollPos;
		private Rect viewRect;

		public Dialog_DedicatedThreadActivity(DedicatedThread dedicatedThread)
		{
			this.dedicatedThread = dedicatedThread;

			SetWindowProperties();
		}

		public Dialog_DedicatedThreadActivity(Func<DedicatedThread> dedicatedThreadGetter)
		{
			this.dedicatedThreadGetter = dedicatedThreadGetter;

			SetWindowProperties();
		}

		public override Vector2 InitialSize => new Vector2(600, 400);

		public DedicatedThread DedicatedThread
		{
			get
			{
				if (dedicatedThread != null)
				{
					return dedicatedThread;
				}
				else if (dedicatedThreadGetter != null)
				{
					return dedicatedThreadGetter();
				}
				return null;
			}
		}

		private void SetWindowProperties()
		{
			this.resizeable = true;
			this.doCloseX = true;
			this.closeOnClickedOutside = false;
			this.draggable = true;
			this.absorbInputAroundWindow = false;
			this.preventCameraMotion = false;
		}

		public override void PostOpen()
		{
			base.PostOpen();
		}

		private void RecalculateViewRect(Rect rect, int entryCount)
		{
			viewRect = rect;
			viewRect.height = entryCount * AsyncActionEntryHeight;
		}

		public override void DoWindowContents(Rect inRect)
		{
			DedicatedThread dedicatedThread = DedicatedThread;
			if (dedicatedThread == null)
			{
				return;
			}

			using (new TextBlock(GameFont.Medium))
			{
				Rect labelRect = inRect.ContractedBy(5);
				labelRect.height = 32;

				int count = dedicatedThread.QueueCount;
				string countReadout = count < 5 ? $"~{count}" : count.ToString();
				Widgets.Label(labelRect, $"DedicatedThread #{dedicatedThread.id} (Count = {countReadout})");

				Text.Font = GameFont.Small;

				Rect activityRect = inRect;
				activityRect.yMin = labelRect.yMax + 5;
				activityRect.height -= 5;
				Widgets.DrawMenuSection(activityRect);

				Rect outRect = activityRect.ContractedBy(2);

				int index = 0;

				Widgets.BeginScrollView(outRect, ref scrollPos, viewRect);
				{
					using (var enumerator = dedicatedThread.GetEnumerator())
					{
						while (enumerator.MoveNext())
						{
							if (index >= queueLimit)
							{
								break;
							}
							AsyncAction asyncAction = enumerator.Current;

							Rect entryRect = viewRect;
							entryRect.y = index * AsyncActionEntryHeight;
							entryRect.height = AsyncActionEntryHeight;

							Widgets.Label(entryRect, $"{index}. {asyncAction.GetType().Name}");
							Widgets.DrawLineHorizontal(entryRect.x, entryRect.yMax, entryRect.width);

							index++;
						}
					}
				}
				Widgets.EndScrollView();

				RecalculateViewRect(outRect, index);
			}
		}
	}
}
