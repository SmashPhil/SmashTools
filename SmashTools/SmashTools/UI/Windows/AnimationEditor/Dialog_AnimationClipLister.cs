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
	public class Dialog_AnimationClipLister : Dialog_ItemDropdown<FileInfo>
	{
		private readonly IAnimator animator;
		private readonly AnimationClip animation;

		public Dialog_AnimationClipLister(IAnimator animator, Rect rect, AnimationClip animation, Action<FileInfo> onFilePicked = null) 
			: base(rect, AnimationLoader.GetAnimationClipFileInfo(animator.ModContentPack), onFilePicked, FileName,
				  isSelected: (FileInfo file) => animation != null && animation.FilePath == file.FullName, createBtn: ("ST_CreateNewClip", () => CreateNewClip(animator)))
		{
			this.animator = animator;
			this.animation = animation;
		}

		private static string FileName(FileInfo file)
		{
			return Path.GetFileNameWithoutExtension(file.Name);
		}

		private static FileInfo CreateNewClip(IAnimator animator)
		{
			DirectoryInfo directoryInfo = AnimationLoader.AnimationDirectory(animator.ModContentPack);
			if (directoryInfo == null || !directoryInfo.Exists)
			{
				directoryInfo = Directory.CreateDirectory(Path.Combine(animator.ModContentPack.RootDir, AnimationLoader.AnimationFolderName));
			}
			return AnimationLoader.CreateEmptyAnimFile(directoryInfo);
		}
	}
}
