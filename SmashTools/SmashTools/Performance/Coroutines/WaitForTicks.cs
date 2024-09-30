using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace SmashTools.Performance
{
	public class WaitForTicks : CustomYieldInstruction
	{
		private int ticks;

		private bool absolute;
		private int startTick;

		public WaitForTicks(int ticks, bool absolute = false)
		{
			this.ticks = ticks;
			this.absolute = absolute;

			this.startTick = CurrentTick;
		}

		public int CurrentTick => absolute ? Find.TickManager.TicksAbs : Find.TickManager.TicksGame;

		public override bool keepWaiting => CurrentTick < startTick + ticks;
	}
}
