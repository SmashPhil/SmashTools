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
		private DedicatedThread dedicatedThread;
		private Func<DedicatedThread> dedicatedThreadGetter;

		private int queueLimit = 50;

		public Dialog_DedicatedThreadActivity(DedicatedThread dedicatedThread)
		{
			this.dedicatedThread = dedicatedThread;
			this.resizeable = true;
			this.doCloseX = true;
			this.closeOnClickedOutside = false;
		}

		public Dialog_DedicatedThreadActivity(Func<DedicatedThread> dedicatedThreadGetter)
		{
			this.dedicatedThreadGetter = dedicatedThreadGetter;
			this.resizeable = true;
			this.doCloseX = true;
			this.closeOnClickedOutside = false;
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

		public override void PostOpen()
		{
			base.PostOpen();
		}

		public override void DoWindowContents(Rect inRect)
		{
			DedicatedThread dedicatedThread = DedicatedThread;
			if (dedicatedThread == null)
			{
				return;
			}

			Rect labelRect = inRect.ContractedBy(5);
			labelRect.height = 24;
			Widgets.Label(labelRect, $"DedicatedThread #{dedicatedThread.id}");

			Rect activityRect = labelRect;
			activityRect.yMin += 24;
			Widgets.DrawMenuSection(activityRect);

			int index = 0;
			using (var enumerator = dedicatedThread.GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					if (index >= queueLimit)
					{
						break;
					}


					index++;
				}
			}
		}
	}
}
