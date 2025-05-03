using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using Verse;

namespace SmashTools.Rendering;

[StaticConstructorOnStartup]
public static class RenderTextureDrawer
{
  private static readonly List<RenderData> renderDatas = [];
  private static readonly Mesh mesh;

  private static RenderTexture renderTexture;

  public static bool InUse => renderTexture != null;

  static RenderTextureDrawer()
  {
    // TODO - finish prop block stuff, need mesh for this
    //mesh = new Mesh
    //{
    //  vertices =
    //  [
    //    new Vector3(-0.5f, -0.5f, 0),
    //    new Vector3(0.5f, -0.5f, 0),
    //    new Vector3(0.5f, 0.5f, 0),
    //    new Vector3(-0.5f, 0.5f, 0)
    //  ],
    //  uv =
    //  [
    //    new Vector2(0, 0),
    //    new Vector2(1, 0),
    //    new Vector2(1, 1),
    //    new Vector2(0, 1)
    //  ],
    //  triangles = [0, 1, 2, 2, 3, 0]
    //};
  }

  public static void Add(RenderData renderData)
  {
    renderDatas.Add(renderData);
  }

  public static void Open(RenderTexture renderTexture)
  {
    Assert.IsFalse(InUse);
    RenderTextureDrawer.renderTexture = renderTexture;
    renderDatas.Clear();
  }

  public static void Close()
  {
    renderDatas.Clear();
    RenderTextureDrawer.renderTexture = null;
  }

  public static void Draw(Rect rect, float scale = 1)
  {
    renderDatas.Sort();

    Assert.IsNull(RenderTexture.active);
    RenderTexture.active = renderTexture;
    GL.PushMatrix();
    try
    {
      GL.LoadPixelMatrix(0, renderTexture.width, renderTexture.height, 0);
      GL.Clear(true, true, Color.clear);
      foreach (RenderData renderData in renderDatas)
      {
        DrawRenderData(rect, renderData, scale: scale);
      }
    }
    finally
    {
      GL.PopMatrix();
      RenderTexture.active = null;
    }
    return;

    static void DrawRenderData(Rect rect, in RenderData renderData, float scale = 1)
    {
      if (renderData.material != null && !renderData.material.SetPass(0))
        return;
      GL.PushMatrix();
      try
      {
        Rect normalizedRect = NormalizeRect(renderData.rect, rect);
        Vector3 size = normalizedRect.size * scale;
        Quaternion rotation = Quaternion.Euler(0f, 0f, renderData.angle);
        Matrix4x4 matrix = Matrix4x4.TRS(normalizedRect.center, rotation, size)
          * Matrix4x4.Translate(new Vector3(-0.5f, -0.5f, 0f));
        GL.MultMatrix(matrix);

        Graphics.DrawTexture(new Rect(0, 0, 1, 1), renderData.mainTex, renderData.material);

        // TODO - Switch to mesh based draw w/ property block
        //Graphics.DrawMesh(mesh, matrix, renderData.material, 0, null,
        //  0, renderData.propertyBlock);
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