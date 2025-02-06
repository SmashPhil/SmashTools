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
	public class AnimationParameter : IXmlExport
	{
		private const float ContractedBy = 2;

		internal int id; // cached HashCode of name
		private string name;

		private float value;
		internal ParamType type;

		private string inputBuffer;

		/// <summary>
		/// name edit event
		/// </summary>
		/// <remarks>params: (string)oldName, (string)newName</remarks>
		public event Action<string, string> OnNameChanged;

		public float Value => value;

		public ParamType Type => type;

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
				id = name.GetHashCodeSafe();
			}
		}

		public void DrawInput(Rect rect)
		{
			switch (type)
			{
				case ParamType.Float:
					DrawFloatInput(rect);
					break;
				case ParamType.Int:
					DrawIntInput(rect);
					break;
				case ParamType.Bool:
					DrawBoolInput(rect);
					break;
				case ParamType.Trigger:
					DrawTriggerInput(rect);
					break;
				default:
					throw new NotImplementedException(nameof(ParamType));
			}
		}

		private void DrawFloatInput(Rect rect)
		{
			Widgets.TextFieldNumeric(rect, ref value, ref inputBuffer, min: float.MinValue, max: float.MaxValue);
		}

		private void DrawIntInput(Rect rect)
		{
			Widgets.TextFieldNumeric(rect, ref value, ref inputBuffer, min: int.MinValue, max: int.MaxValue);
		}

		private void DrawBoolInput(Rect rect)
		{
			bool checkOn = value != 0;
			Widgets.Checkbox(rect.position, ref checkOn, size: rect.height - ContractedBy * 2);
			value = checkOn ? 1 : 0;
		}

		private void DrawTriggerInput(Rect rect)
		{
			bool checkOn = value != 0;
			Texture2D buttonTex = checkOn ? Widgets.RadioButOnTex : UIData.RadioButOffTex;
			Rect buttonRect = new Rect(rect.x, rect.y, rect.height, rect.height).ContractedBy(ContractedBy);

			Color color = GUI.color;
			if (!GUI.enabled)
			{
				GUI.color = Color.gray;
			}
			GUI.DrawTexture(buttonRect, buttonTex);
			if (Widgets.ButtonInvisible(buttonRect))
			{
				checkOn = !checkOn;
				value = checkOn ? 1 : 0;
				SoundDefOf.Tick_Tiny.PlayOneShotOnCamera(null);
			}
			GUI.color = color;
		}

		void IXmlExport.Export()
		{
			XmlExporter.WriteElement(nameof(name), name);
			XmlExporter.WriteObject(nameof(type), type);
			XmlExporter.WriteObject(nameof(value), value);
		}

		public void PostLoad()
		{
			id = name.GetHashCodeSafe();
		}

		public enum ParamType
		{
			Float,
			Int,
			Bool,
			Trigger,
		}
	}
}
