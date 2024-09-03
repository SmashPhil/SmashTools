using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Verse;
using RimWorld;
using UnityEngine;
using Verse.Noise;
using System.Diagnostics;

namespace SmashTools.Animations
{
	public class Dialog_AnimationClipLister : Dialog_ItemDropdown<AnimationClip>
	{
		private readonly IAnimator animator;
		private readonly AnimationClip animation;

		public Dialog_AnimationClipLister(IAnimator animator, Rect rect, AnimationClip animation, Action<AnimationClip> onFilePicked = null) 
			: base(rect, AnimationLoader.Cache<AnimationClip>.GetAll(), onFilePicked, FileName,
				  isSelected: (AnimationClip other) => other && animation && animation.FilePath == other.FilePath, 
				  createBtn: ("ST_CreateNewClip", AnimationClip.CreateEmpty))
		{
			this.animator = animator;
			this.animation = animation;
		}

		private static string FileName(AnimationClip clip)
		{
			return Path.GetFileNameWithoutExtension(clip.FilePath);
		}
	}
}
