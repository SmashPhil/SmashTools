using RimWorld;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace SmashTools.Animations
{
	public class Dialog_PropertySelect : Window
	{
		private const float EntryHeight = 28;
		private const float SubPropertyPadding = 15;
		
		private static readonly Color backgroundColor = new ColorInt(56, 56, 56).ToColor;
		private static readonly Color backgroundOutlineColor = new ColorInt(74, 74, 74).ToColor;

		private readonly IAnimator animator;
		private readonly AnimationClip animation;

		private Vector2 position;
		private Action<AnimationPropertyParent> propertyAdded;

		private List<Type> propertyListOrder = new List<Type>();
		private Dictionary<Type, List<AnimationPropertyParent>> properties = new Dictionary<Type, List<AnimationPropertyParent>>();
		private bool[] expandedContainers;

		public Dialog_PropertySelect(IAnimator animator, AnimationClip animation, Vector2 position, Action<AnimationPropertyParent> propertyAdded = null)
		{
			this.animator = animator;
			this.animation = animation;
			this.position = position;
			this.propertyAdded = propertyAdded;

			this.closeOnClickedOutside = true;
			this.absorbInputAroundWindow = false;
			this.preventCameraMotion = false;
			this.doWindowBackground = false;
			this.layer = WindowLayer.Super;
		}

		public override Vector2 InitialSize => new Vector2(300, 350);

		protected override float Margin => 0;

		public override void PreOpen()
		{
			HashSet<(Type parentType, string name)> existingProperties = new HashSet<(Type parentType, string fieldName)>();
			if (!animation.properties.NullOrEmpty())
			{
				foreach (AnimationPropertyParent container in animation.properties)
				{
					existingProperties.Add((container.Type, container.Name));
				}
			}
			foreach (AnimationPropertyParent container in AnimationPropertyRegistry.GetAnimationProperties(animator))
			{
				if (!existingProperties.Contains((container.Type, container.Name)))
				{
					if (!properties.ContainsKey(container.Type))
					{
						propertyListOrder.Add(container.Type);
					}
					properties.AddOrInsert(container.Type, container);
				}
			}
			expandedContainers = new bool[properties.Count];
			base.PreOpen();
		}

		public override void Notify_ClickOutsideWindow()
		{
			base.Notify_ClickOutsideWindow();
			Close();
		}

		private void RecalculateHeight()
		{
			float propertiesHeight = properties.Count * EntryHeight;
			float propertyContainersHeight = properties.Count * EntryHeight;
			float height = propertiesHeight + propertyContainersHeight;
		}

		protected override void SetInitialSizeAndPosition()
		{
			if (position.x + InitialSize.x > UI.screenWidth)
			{
				position.x = UI.screenWidth - InitialSize.x;
			}
			if (position.y + InitialSize.y > UI.screenHeight)
			{
				position.y = UI.screenHeight - InitialSize.y;
			}
			windowRect = new Rect(position.x, position.y, InitialSize.x, InitialSize.y);
		}

		public override void DoWindowContents(Rect inRect)
		{
			GUIState.Push();
			try
			{
				Widgets.DrawBoxSolidWithOutline(inRect, backgroundColor, backgroundOutlineColor, outlineThickness: 2);

				Text.Font = GameFont.Small;
				Text.WordWrap = false;
				Text.Anchor = TextAnchor.MiddleLeft;

				Rect rowRect = new Rect(inRect.x, inRect.y, inRect.width, EntryHeight).ContractedBy(3);
				for (int i = 0; i < properties.Count; i++)
				{
					Type parentType = propertyListOrder[i];
					bool expanded = expandedContainers[i];
					rowRect.SplitVertically(EntryHeight, out Rect checkboxRect, out Rect fileLabelRect);

					if (Widgets.ButtonImage(checkboxRect.ContractedBy(2), expanded ? TexButton.Collapse : TexButton.Reveal))
					{
						expanded = !expanded;
						expandedContainers[i] = expanded;

						SoundDefOf.Click.PlayOneShotOnCamera(null);
					}

					Widgets.Label(fileLabelRect, parentType.Name);

					if (expanded)
					{
						List<AnimationPropertyParent> containers = properties[parentType];
						foreach (AnimationPropertyParent container in containers)
						{
							rowRect.y += rowRect.height;
							Rect propertyParentRect = new Rect(fileLabelRect.x + SubPropertyPadding, rowRect.y, fileLabelRect.width - SubPropertyPadding, fileLabelRect.height);
							Widgets.Label(propertyParentRect, container.Name);
							if (AddPropertyButton(propertyParentRect))
							{
								animation.properties.Add(container);
								propertyAdded?.Invoke(container);
								Close();
								break;
							}
						}
					}

					rowRect.y += rowRect.height;
				}
			}
			finally
			{
				GUIState.Pop();
			}
		}

		private bool AddPropertyButton(Rect rect)
		{
			float size = rect.height;
			Rect buttonRect = new Rect(rect.xMax - size, rect.y, size, size);
			return Widgets.ButtonImage(buttonRect.ContractedBy(2), TexButton.Plus);
		}
	}
}
