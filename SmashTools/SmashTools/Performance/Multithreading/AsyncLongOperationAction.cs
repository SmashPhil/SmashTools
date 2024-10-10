using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using SmashTools;

namespace SmashTools.Performance
{
	public class AsyncLongOperationAction : AsyncAction
	{
		private Action action;

		public override bool LongOperation => true;

		public override bool IsValid => action != null;

		public void Set(Action action)
		{
			this.action = action;
		}

		public override void Invoke()
		{
			action.Invoke();
		}

		public override void ReturnToPool()
		{
			action = null;
			AsyncPool<AsyncLongOperationAction>.Return(this);
		}
	}
}
