﻿using SmashTools.Xml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace SmashTools.Animations
{
	public sealed class AnimationClip : IAnimationFile
	{
		public const string DefaultAnimName = "New-Animation";
		public const string FileExtension = ".rwa"; //RimWorld Animation
		public const int DefaultFrameCount = 60;

		public int frameCount = DefaultFrameCount;

		public List<AnimationPropertyParent> properties = new List<AnimationPropertyParent>();
		public List<AnimationEvent> events = new List<AnimationEvent>();

		public string FilePath { get; set; }

		public string FileName { get; set; }

		public string FileNameWithExtension => FileName + FileExtension;

		public void SetFrame()
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
		}

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
		}

		private static int MaxFrame(AnimationProperty property)
		{
			if (property.curve.PointsCount > 0)
			{
				return property.curve.points.Max(keyFrame => keyFrame.frame);
			}
			return -1;
		}

		internal void ValidateEventOrder()
		{
			events.Sort();
		}

		public static AnimationClip CreateEmpty()
		{
			AnimationClip animationClip = new AnimationClip();
			animationClip.FileName = DefaultAnimName;
			return animationClip;
		}

		void IAnimationFile.PostLoad()
		{
			if (!properties.NullOrEmpty())
			{
				foreach (AnimationPropertyParent propertyParent in properties)
				{
					propertyParent.PostLoad();
				}
			}
		}

		void IXmlExport.Export()
		{
			ValidateEventOrder();

			XmlExporter.WriteObject(nameof(frameCount), frameCount);
			XmlExporter.WriteCollection(nameof(properties), properties);
			XmlExporter.WriteCollection(nameof(events), events);
		}

		public static implicit operator bool(AnimationClip clip)
		{
			return clip != null;
		}
	}
}
