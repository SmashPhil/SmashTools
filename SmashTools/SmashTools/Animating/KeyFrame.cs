using System;
using System.Globalization;
using SmashTools.Xml;
using UnityEngine;
using Verse;
using static SmashTools.Debug;

namespace SmashTools.Animations
{
	public readonly struct KeyFrame : IXmlExport, IComparable<KeyFrame>
	{
		public readonly int frame;
		public readonly float value;
		public readonly float inTangent;
		public readonly float outTangent;
		public readonly float inWeight;
		public readonly float outWeight;
		public readonly WeightedMode weightedMode;

		public KeyFrame(int frame, float value)
		{
			this.frame = frame;
			this.value = value;
			inTangent = 0;
			outTangent = 0;
			inWeight = 0.333f;
			outWeight = 0.333f;
			weightedMode = WeightedMode.None;
		}

		public KeyFrame(int frame, float value, float inTangent, float outTangent)
		{
			this.frame = frame;
			this.value = value;
			this.inTangent = inTangent;
			this.outTangent = outTangent;
			inWeight = 0.333f;
			outWeight = 0.333f;
			weightedMode = WeightedMode.None;
		}

		public KeyFrame(int frame, float value, float inTangent, float outTangent, float inWeight, float outWeight)
		{
			this.frame = frame;
			this.value = value;
			this.inTangent = inTangent;
			this.outTangent = outTangent;
			this.inWeight = inWeight;
			this.outWeight = outWeight;
			weightedMode = WeightedMode.Both;
		}

		public static KeyFrame Invalid => new KeyFrame(-1, 0);

		readonly void IXmlExport.Export()
		{
			XmlExporter.WriteString(ToString());
		}

		public override readonly string ToString()
		{
			return $"({frame},{Ext_Math.RoundTo(value, 0.0001f)}," +
				$"{Ext_Math.RoundTo(inTangent, 0.0001f)},{Ext_Math.RoundTo(outTangent, 0.0001f)}" +
				$"{Ext_Math.RoundTo(inWeight, 0.0001f)},{Ext_Math.RoundTo(outWeight, 0.0001f)})";
		}

		readonly int IComparable<KeyFrame>.CompareTo(KeyFrame other)
		{
			return frame.CompareTo(other.frame);
		}

		public static KeyFrame FromString(string entry)
		{
			entry = entry.Replace("(", "");
			entry = entry.Replace(")", "");
			string[] array = entry.Split(',');

			if (array.Length == 6)
			{
				CultureInfo invariantCulture = CultureInfo.InvariantCulture;
				int frame = Convert.ToInt32(array[0], invariantCulture);
				float value = Convert.ToSingle(array[1], invariantCulture);
				float inTangent = Convert.ToSingle(array[2], invariantCulture);
				float outTangent = Convert.ToSingle(array[3], invariantCulture);
				float inWeight = Convert.ToSingle(array[4], invariantCulture);
				float outWeight = Convert.ToSingle(array[5], invariantCulture);
				return new KeyFrame(frame, value, inTangent, outTangent, inWeight, outWeight);
			}
			Log.Error($"Unable to parse AnimationCurve.KeyFrame. Invalid format: {entry}.");
			return Invalid;
		}
	}
}
