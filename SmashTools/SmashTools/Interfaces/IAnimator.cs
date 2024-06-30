using SmashTools.Animations;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using UnityEngine;

namespace SmashTools.Animations
{
	public interface IAnimator
	{
		public AnimationController Controller { get; }

		public ModContentPack ModContentPack { get; }

		public IEnumerable<object> ExtraAnimators { get; }

		public Vector3 DrawPos { get; }
	}
}
