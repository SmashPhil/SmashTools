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
		public const int DefaultFrameCount = 60;
		public const string FileExtension = ".rwa"; //RimWorld Animation

		public int frameCount = DefaultFrameCount;

		public List<AnimationPropertyParent> properties = new List<AnimationPropertyParent>();


		//Can't use auto-properties or RimWorld will try to serialize their backing fields
		[Unsaved]
		private string fileName;
		[Unsaved]
		private string filePath;

		public string FilePath { get => filePath; internal set => filePath = value; }

		public string FileName { get => fileName; internal set => fileName = value; }

		public string FileNameWithExtension => fileName + FileExtension;

		public void RecacheFrameCount()
		{
			frameCount = DefaultFrameCount;
			if (properties.Count > 0)
			{
				int max = 0;
				foreach (AnimationPropertyParent propertyParent in properties)
				{
					int propertyMax = -1;
					if (propertyParent.Single != null)
					{
						propertyMax = MaxFrame(propertyParent.Single);
					}
					else if (!propertyParent.Children.NullOrEmpty())
					{
						foreach (AnimationProperty property in propertyParent.Children)
						{
							propertyMax = MaxFrame(property);
						}
					}

					if (propertyMax > max)
					{
						max = propertyMax;
					}
				}

				if (max >= 0)
				{
					frameCount = max;
				}
			}

			int MaxFrame(AnimationProperty property)
			{
				if (property.curve.PointsCount > 0)
				{
					return property.curve.points.Max(keyFrame => keyFrame.frame);
				}
				return -1;
			}
		}
	}
}
