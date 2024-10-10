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

namespace SmashTools.Animations
{
	public abstract class AnimationParameter : IXmlExport
	{
		private string name;

		/// <summary>
		/// name edit event
		/// </summary>
		/// <remarks>params: (string)oldName, (string)newName</remarks>
		public event Action<string, string> OnNameChanged;

		public string Name
		{
			get
			{
				return name;
			}
			set
			{
				if (name == value) return;

				OnNameChanged?.Invoke(name, value);
				name = value;
			}
		}
		public abstract void DrawInput(Rect rect);

		void IXmlExport.Export()
		{
			XmlExporter.WriteElement(nameof(name), name);
		}
	}

	public class FloatParam : AnimationParameter
	{
		public const string ParamName = "Float";

		public float value;

		private string inputBuffer;

		public override void DrawInput(Rect rect)
		{
			Widgets.TextFieldNumeric(rect, ref value, ref inputBuffer, min: float.MinValue, max: float.MaxValue);
		}
	}

	public class IntParam : AnimationParameter
	{
		public const string ParamName = "Int";

		public int value;

		private string inputBuffer;

		public override void DrawInput(Rect rect)
		{
			Widgets.TextFieldNumeric(rect, ref value, ref inputBuffer, min: int.MinValue, max: int.MaxValue);
		}
	}

	public class BoolParam : AnimationParameter
	{
		public const string ParamName = "Bool";
		public const float ContractedBy = 2;

		public bool value;

		public override void DrawInput(Rect rect)
		{
			Widgets.Checkbox(rect.position, ref value, size: rect.height - ContractedBy * 2);
		}
	}

	public class TriggerParam : BoolParam
	{
		public new const string ParamName = "Trigger";

		public override void DrawInput(Rect rect)
		{
			Texture2D buttonTex = value ? Widgets.RadioButOnTex : UIData.RadioButOffTex;
			Rect buttonRect = new Rect(rect.x, rect.y, rect.height, rect.height).ContractedBy(ContractedBy);

			Color color = GUI.color;
			if (!GUI.enabled)
			{
				GUI.color = Color.gray;
			}
			GUI.DrawTexture(buttonRect, buttonTex);
			if (Widgets.ButtonInvisible(buttonRect))
			{
				value = !value;
				SoundDefOf.Tick_Tiny.PlayOneShotOnCamera(null);
			}
			GUI.color = color;
		}
	}
}
