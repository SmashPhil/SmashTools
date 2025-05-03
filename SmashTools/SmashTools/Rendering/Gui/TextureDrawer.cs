using System.Collections.Generic;
using UnityEngine;

namespace SmashTools.Rendering;

public static class TextureDrawer
{
  private static readonly List<RenderData> renderDatas = [];

  public static bool InUse { get; private set; }

  public static void Add(RenderData renderData)
  {
    renderDatas.Add(renderData);
  }

  public static void Open()
  {
    InUse = true;
    renderDatas.Clear();
  }

  public static void Close()
  {
    renderDatas.Clear();
    InUse = false;
  }

  public static void Draw(Rect rect, float scale = 1)
  {
    renderDatas.Sort();
    foreach (RenderData renderData in renderDatas)
    {
      UIElements.DrawTextureWithMaterialOnGUI(renderData.rect,
        renderData.mainTex, renderData.material, renderData.angle);
    }
  }
}