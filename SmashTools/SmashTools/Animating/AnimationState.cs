using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmashTools.Animations
{
	public class AnimationState
	{
		public AnimationClip clip;
		public float speed = 1;
		public bool writeDefaults = true;

		public List<AnimationTransition> transitions;
	}
}
