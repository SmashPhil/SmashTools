using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace SmashTools.Rendering;

public static class TextureDrawer
{
	private static readonly List<RenderData> RenderDatas = [];

	public static bool InUse { get; private set; }

	public static void Add(RenderData renderData)
	{
		RenderDatas.Add(renderData);
	}

	public static void Open()
	{
		InUse = true;
		RenderDatas.Clear();
	}

	public static void Close()
	{
		RenderDatas.Clear();
		InUse = false;
	}

	public static void Draw(Rect rect, float scale = 1, bool forceCentering = false)
	{
		GUI.BeginClip(rect);
		try
		{
			RenderDatas.Sort();
			foreach (RenderData renderData in RenderDatas)
			{
				Vector2 drawPos = renderData.rect.position - rect.position;
				Rect drawRect = new(drawPos, renderData.rect.size);

				if (forceCentering)
					drawRect.center = rect.center;

				if (!Mathf.Approximately(scale, 1))
				{
					Vector2 expandSize = renderData.rect.size * (scale - 1);
					drawRect = scale > 1 ?
						drawRect.ExpandedBy(expandSize.x, expandSize.y) :
						drawRect.ContractedBy(expandSize.x, expandSize.y);
				}
				UIElements.DrawTextureWithMaterialOnGUI(drawRect, renderData.mainTex, renderData.material, renderData.angle);
			}
		}
		finally
		{
			GUI.EndClip();
		}
	}
}