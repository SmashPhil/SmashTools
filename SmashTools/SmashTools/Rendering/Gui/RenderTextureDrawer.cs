using System.Collections.Generic;
using CoreLib;
using UnityEngine;
using UnityEngine.Assertions;
using Verse;

namespace SmashTools.Rendering;

[StaticConstructorOnStartup]
public static class RenderTextureDrawer
{
  private static readonly List<RenderData> RenderDatas = [];

  private static RenderTexture renderTexture;

  public static bool InUse => renderTexture;

  public static void Add(RenderData renderData)
  {
    RenderDatas.Add(renderData);
  }

  public static void Open(RenderTexture renderTexture)
  {
    Assert.IsFalse(InUse);
    Assert.IsTrue(RenderDatas.Count == 0);
    RenderTextureDrawer.renderTexture = renderTexture;
    RenderDatas.Clear();
  }

  public static void Close()
  {
    RenderDatas.Clear();
    renderTexture = null;
  }

  /// <summary>
  /// Finalize RenderTexture rendering with added render data.
  /// </summary>
  /// <param name="rect">Outer rect containing all the graphics being drawn.</param>
  /// <param name="scale">Zoom factor on all drawn graphics, scaled from the center of the rect.</param>
  /// <param name="center">Set rect position of all render data to center of outer rect. Use for 'icon' images that need all offsets erased.</param>
  public static void Draw(Rect rect, float scale = 1, bool center = false)
  {
    if (!renderTexture || !renderTexture.IsCreated())
    {
      Trace.Fail("Trying to blit with null render texture.");
      return;
    }
    RenderDatas.Sort();

    // Save/restore the active target rather than asserting it is null, so a blit can run mid-OnGUI
    // (where IMGUI owns the render target) without orphaning it.
    RenderTexture prevActive = RenderTexture.active;
    RenderTexture.active = renderTexture;

    try
    {
      GL.PushMatrix();
      GL.Viewport(new Rect(0, 0, renderTexture.width, renderTexture.height));
      GL.LoadPixelMatrix(0, renderTexture.width, renderTexture.height, 0);
      GL.Clear(true, true, Color.clear);

      foreach (RenderData renderData in RenderDatas)
      {
        DrawRenderData(rect, renderData, scale: scale, center: center);
      }
    }
    finally
    {
      GL.PopMatrix();
      GL.Flush();
      RenderDatas.Clear();
      RenderTexture.active = prevActive;
    }
    return;

    static void DrawRenderData(Rect rect, in RenderData renderData, float scale, bool center)
    {
      Material material = renderData.material;
      if (!material)
        return;
      if (renderData.mainTex)
        material.mainTexture = renderData.mainTex;
      if (!material.SetPass(0))
        return;

      GL.PushMatrix();
      GL.LoadIdentity();
      try
      {
        Rect input = center ? renderData.rect with { center = rect.center } : renderData.rect;
        Rect normalizedRect = NormalizeRect(input, rect);
        Vector3 size = normalizedRect.size * scale;
        Quaternion rotation = Quaternion.Euler(0f, 0f, renderData.angle);
        Matrix4x4 matrix = Matrix4x4.TRS(normalizedRect.center, rotation, size)
          * Matrix4x4.Translate(new Vector3(-0.5f, -0.5f, 0f));
        GL.MultMatrix(matrix);

        // Raw GL quad rather than Graphics.DrawTexture: the latter is IMGUI-aware and re-applies the
        // surrounding GUI clip's viewport mid-blit, cropping the draw inside nested groups / scroll views.
        GL.Begin(GL.QUADS);
        GL.Color(Color.white);
        GL.TexCoord2(0f, 1f);
        GL.Vertex3(0f, 0f, 0f);
        GL.TexCoord2(1f, 1f);
        GL.Vertex3(1f, 0f, 0f);
        GL.TexCoord2(1f, 0f);
        GL.Vertex3(1f, 1f, 0f);
        GL.TexCoord2(0f, 0f);
        GL.Vertex3(0f, 1f, 0f);
        GL.End();
      }
      finally
      {
        GL.PopMatrix();
      }
      return;

      static Rect NormalizeRect(Rect input, Rect rect)
      {
        float scaleX = renderTexture.width / rect.width;
        float scaleY = renderTexture.height / rect.height;

        // Render data rects are in the outer rect's (absolute) coordinate space; rebase to the rect
        // origin before scaling into RT-pixel space, otherwise the draw lands outside the texture.
        return new Rect(
          (input.x - rect.x) * scaleX,
          (input.y - rect.y) * scaleY,
          input.width * scaleX,
          input.height * scaleY
        );
      }
    }
  }
}