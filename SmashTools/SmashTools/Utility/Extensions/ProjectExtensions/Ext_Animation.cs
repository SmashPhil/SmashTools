﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmashTools
{
	public static class Ext_Animation
	{
		public static bool AnimationLocked(this IAnimationTarget animationTarget)
		{
			return AnimationManager.AnimationTarget == animationTarget && AnimationManager.CurrentDriver != null;
		}
	}
}
