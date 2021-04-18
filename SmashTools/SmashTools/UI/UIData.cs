using UnityEngine;
using Verse;

namespace SmashTools
{
	[StaticConstructorOnStartup]
	public static class UIData
	{
		public static readonly Texture2D FillableBarTexture = SolidColorMaterials.NewSolidColorTexture(0.5f, 0.5f, 0.5f, 0.5f);
		public static readonly Texture2D ClearBarTexture = BaseContent.ClearTex;
	}
}
