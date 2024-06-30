using SmashTools.Xml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace SmashTools.Animations
{
	public sealed class AnimationClip
	{
		public const string FileExtension = ".rwa"; //RimWorld Animation

		public int frameCount = 60;

		public List<AnimationPropertyParent> properties = new List<AnimationPropertyParent>();


		//Can't use auto-properties or RimWorld will try to serialize their backing fields
		[Unsaved]
		private string fileName;
		[Unsaved]
		private string filePath;

		public string FilePath { get => filePath; internal set => filePath = value; }

		public string FileName { get => fileName; internal set => fileName = value; }

		public string FileNameWithExtension => fileName + FileExtension;
	}
}
