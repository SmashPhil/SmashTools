using SmashTools.Animations;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using UnityEngine;

namespace SmashTools.Animations
{
	public interface IAnimator : IAnimationObject
	{
		AnimationController Controller { get; }

		ModContentPack ModContentPack { get; }

		IEnumerable<IAnimationObject> ExtraAnimators { get; }

		Vector3 DrawPos { get; }
	}
}
