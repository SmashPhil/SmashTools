using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using Verse;

namespace SmashTools.Rendering;

[StaticConstructorOnStartup]
public static class RenderTextureDrawer
{
  private static readonly List<RenderData> RenderDatas = [];

  private static RenderTexture renderTexture;
  private static Mesh mesh;

  static RenderTextureDrawer()
  {
    mesh = new Mesh()
    {
      vertices =
      [
        new Vector3(0, 0, 0),
        new Vector3(1, 0, 0),
        new Vector3(1, 1, 0),
        new Vector3(0, 1, 0),
      ],
      uv =
      [
        new Vector2(0, 0),
        new Vector2(1, 0),
        new Vector2(1, 1),
        new Vector2(0, 1),
      ],
      triangles = [0, 1, 2, 2, 3, 0]
    };
    mesh.RecalculateNormals();
    mesh.RecalculateBounds();
  }

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
  /// Finalize RenderTexture rendering with added render datas.
  /// </summary>
  /// <param name="rect">Outer rect containing all of the graphics being drawn.</param>
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

    Assert.IsNull(RenderTexture.active);
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
      RenderTexture.active = null;
    }
    return;

    static void DrawRenderData(Rect rect, in RenderData renderData, float scale, bool center)
    {
      if (renderData.material && !renderData.material.SetPass(0))
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
        //GL.MultMatrix(matrix);
        Log.Message($"Drawing at {normalizedRect.center} at {rect}");
        //Graphics.DrawTexture(new Rect(0, 0, 1, 1), renderData.mainTex, renderData.material);
        renderData.material.SetPass(0);
        Graphics.DrawMeshNow(mesh, matrix);

        //Vector2 scaleT = normalizedRect.size / Mathf.Min(normalizedRect.size.x, normalizedRect.size.y);
        //Graphics.Blit(renderData.mainTex, renderTexture, scaleT, Vector2.zero);
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

        return new Rect(
          input.x * scaleX,
          input.y * scaleY,
          input.width * scaleX,
          input.height * scaleY
        );
      }
    }
  }
}