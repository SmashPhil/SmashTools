using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace SmashTools
{
	public class AnimationEvent<T>
	{
		public float triggerAt = 0;
		public ResolvedMethod<T> method;
		public AnimationTrigger type = AnimationTrigger.EqualTo;
		public AnimationFrequency frequency = AnimationFrequency.OneShot;

		public bool EventFrame(float t)
		{
			switch (type)
			{
				case AnimationTrigger.GreaterThan:
					if (t >= triggerAt)
					{
						return true;
					}
					break;
				case AnimationTrigger.EqualTo:
					if (Mathf.Approximately(t, triggerAt))
					{
						return true;
					}
					break;
			}
			return false;
		}

		public enum AnimationTrigger
		{
			EqualTo,
			GreaterThan,
		}

		public enum AnimationFrequency
		{
			OneShot,
			Continuous
		}
	}
}
