using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using SmashTools.Xml;
using UnityEngine;
using Verse;
using Verse.Sound;
using static SmashTools.Debug;

namespace SmashTools.Animations
{
	public class AnimationCondition : IXmlExport
	{
		private const float UIDropdownPadding = 5;

		public string parameter;
		public ComparisonType comparison;
		public float value;

		private string inputBuffer;

		private AnimationParameter cachedParameter;

		public AnimationTransition Transition { get; internal set; }

		public AnimationParameter Parameter
		{
			get
			{
				if (cachedParameter == null)
				{
					ResolveParameter();
				}
				return cachedParameter;
			}
			internal set
			{
				if (cachedParameter == value) return;

				cachedParameter = value;
				parameter = Parameter?.Name ?? string.Empty;
			}
		}

		public void DrawConditionInput(Rect rect)
		{
			Rect dragHandleRect = new Rect(rect.x, rect.y, rect.height, rect.height);
			rect.xMin += dragHandleRect.width;

			List<AnimationParameter> parameters = Transition.FromState.Layer.Controller.parameters;

			Type paramType = Parameter.GetType();
			if (paramType == typeof(FloatParam))
			{
				Rect[] rects = rect.SplitVertically(3, new float[] { 0.4f, 0.3f, 0.3f }, 5);
				Rect parameterRect = rects[0];
				Rect comparisonRect = rects[1];
				Rect inputRect = rects[2];

				if (AnimationEditor.Dropdown(parameterRect, Parameter?.Name ?? string.Empty, null))
				{
					if (!parameters.NullOrEmpty())
					{
						List<FloatMenuOption> options = new List<FloatMenuOption>();
						foreach (AnimationParameter parameter in parameters)
						{
							options.Add(new FloatMenuOption(parameter.Name, delegate ()
							{
								Parameter = parameter;
							}));
						}
						Find.WindowStack.Add(new FloatMenu(options));
					}
					else
					{
						SoundDefOf.ClickReject.PlayOneShotOnCamera();
					}
				}
				if (AnimationEditor.Dropdown(comparisonRect, comparison.ToString() ?? string.Empty, null))
				{
					List<FloatMenuOption> options = new List<FloatMenuOption>
					{
						new FloatMenuOption(ComparisonType.LessThan.ToString(), () => comparison = ComparisonType.LessThan),
						new FloatMenuOption(ComparisonType.GreaterThan.ToString(), () => comparison = ComparisonType.GreaterThan)
					};
					Find.WindowStack.Add(new FloatMenu(options));
				}
				Widgets.TextFieldNumeric(parameterRect, ref value, ref inputBuffer, min: float.MinValue, float.MaxValue);
			}
			else if (paramType == typeof(IntParam))
			{

			}
			else if (paramType == typeof(BoolParam))
			{

			}
			else if (paramType == typeof(TriggerParam))
			{

			}
			else
			{
				throw new ArgumentException(paramType?.FullName);
			}
		}

		public void ResolveParameter()
		{
			if (!parameter.NullOrEmpty())
			{
				Parameter = Transition.FromState.Layer.Controller.parameters.FirstOrDefault(param => param.Name == parameter);
				if (Parameter == null)
				{
					Log.Error($"Unable to resolve parameter \"{parameter}\" in condition. Removing...");
					Transition.conditions.Remove(this);
				}
			}
		}

		public void PostLoad()
		{
			ResolveParameter();
		}

		public void Export()
		{
			XmlExporter.WriteObject(nameof(parameter), parameter);
			XmlExporter.WriteObject(nameof(comparison), comparison);
		}
	}
}
