using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Object = UnityEngine.Object;

namespace SmashTools.Rendering;

/// <summary>
/// Caches rotated copies of textures so a sprite can be drawn UN-rotated (which IMGUI's rectangular clipping handles
/// correctly) while still appearing rotated. Rotating <see cref="GUI.matrix"/> instead would break clipping — it both
/// mis-positions the draw and lets the GUI clip mask discard it.
/// </summary>
public static class RotatedTextureCache
{
  private static readonly Dictionary<(Texture, int), Texture2D> QuarterTurnCache = [];

  private static readonly Dictionary<(Texture, int), Texture2D> ArbitraryCache = [];

  /// <summary>
  /// Rotated copy for a 90-degree step. Uses an exact pixel remap (no resampling), keeping the texture crisp.
  /// </summary>
  /// <param name="quarterTurns">Number of 90-degree steps (any integer; reduced mod 4).</param>
  public static Texture2D GetRotated(Texture src, int quarterTurns)
  {
    quarterTurns = ((quarterTurns % 4) + 4) % 4;
    if (src == null)
      return null;
    if (quarterTurns == 0)
      return src as Texture2D;

    (Texture, int) key = (src, quarterTurns);
    if (QuarterTurnCache.TryGetValue(key, out Texture2D cached) && cached)
      return cached;

    Texture2D readable = AsReadable(src, out bool temporary);
    if (readable == null)
      return src as Texture2D;
    Texture2D rotated = RotateQuarterTurns(readable, quarterTurns);
    if (temporary)
      Object.Destroy(readable);
    QuarterTurnCache[key] = rotated;
    return rotated;
  }

  /// <summary>
  /// Rotated copy for an arbitrary angle (bilinear sampled into a diagonal-sized canvas so corners aren't clipped).
  /// Matches <see cref="GetRotated"/> exactly at the cardinal angles. Cached to the nearest degree.
  /// </summary>
  public static Texture2D GetRotatedArbitrary(Texture src, float angle)
  {
    if (src == null)
      return null;
    int key = ((Mathf.RoundToInt(angle) % 360) + 360) % 360;
    if (key == 0)
      return src as Texture2D;

    if (ArbitraryCache.TryGetValue((src, key), out Texture2D cached) && cached)
      return cached;

    Texture2D readable = AsReadable(src, out bool temporary);
    if (readable == null)
      return src as Texture2D;
    Texture2D rotated = RotateArbitrary(readable, key);
    if (temporary)
      Object.Destroy(readable);
    ArbitraryCache[(src, key)] = rotated;
    return rotated;
  }

  /// <summary>
  /// Returns a CPU-readable copy of <paramref name="src"/>, blitting through a <see cref="RenderTexture"/> when the
  /// source isn't import-flagged readable (the common case for mod textures).
  /// </summary>
  private static Texture2D AsReadable(Texture src, out bool temporary)
  {
    temporary = false;
    if (src is Texture2D { isReadable: true } direct)
      return direct;
    try
    {
      RenderTexture rt = RenderTexture.GetTemporary(src.width, src.height, 0, RenderTextureFormat.ARGB32);
      RenderTexture previous = RenderTexture.active;
      Graphics.Blit(src, rt);
      RenderTexture.active = rt;
      Texture2D readable = new(src.width, src.height, TextureFormat.RGBA32, false);
      readable.ReadPixels(new Rect(0, 0, src.width, src.height), 0, 0);
      readable.Apply();
      RenderTexture.active = previous;
      RenderTexture.ReleaseTemporary(rt);
      temporary = true;
      return readable;
    }
    catch (Exception ex)
    {
      Log.Warning($"[SmashTools] RotatedTextureCache GPU readback failed for '{src?.name}': {ex.Message}");
      return null;
    }
  }

  private static Texture2D RotateQuarterTurns(Texture2D src, int quarterTurns)
  {
    Color32[] pixels = src.GetPixels32();
    int width = src.width;
    int height = src.height;
    bool swap = quarterTurns is 1 or 3;
    int newWidth = swap ? height : width;
    int newHeight = swap ? width : height;
    Color32[] result = new Color32[pixels.Length];
    for (int y = 0; y < height; y++)
    {
      for (int x = 0; x < width; x++)
      {
        Color32 color = pixels[y * width + x];
        int nx;
        int ny;
        switch (quarterTurns)
        {
          case 1:
            nx = y;
            ny = width - 1 - x;
            break;
          case 2:
            nx = width - 1 - x;
            ny = height - 1 - y;
            break;
          case 3:
            nx = height - 1 - y;
            ny = x;
            break;
          default:
            nx = x;
            ny = y;
            break;
        }
        result[ny * newWidth + nx] = color;
      }
    }
    return BuildTexture(newWidth, newHeight, result, src.filterMode);
  }

  private static Texture2D RotateArbitrary(Texture2D src, float angle)
  {
    Color32[] pixels = src.GetPixels32();
    int width = src.width;
    int height = src.height;
    // Diagonal canvas guarantees the rotated source always fits, for any angle.
    int size = Mathf.CeilToInt(Mathf.Sqrt((float)width * width + (float)height * height));
    Color32[] result = new Color32[size * size];

    float radians = angle * Mathf.Deg2Rad;
    float cos = Mathf.Cos(radians);
    float sin = Mathf.Sin(radians);
    float canvasCenter = size / 2f;
    float centerX = width / 2f;
    float centerY = height / 2f;

    // For each destination pixel, sample the source at the inverse-rotated position. The sign matches the
    // quarter-turn remap at the cardinal angles, so the two paths are seamless.
    for (int ny = 0; ny < size; ny++)
    {
      for (int nx = 0; nx < size; nx++)
      {
        float dx = nx - canvasCenter;
        float dy = ny - canvasCenter;
        float sx = cos * dx - sin * dy + centerX;
        float sy = sin * dx + cos * dy + centerY;
        result[ny * size + nx] = SampleBilinear(pixels, width, height, sx, sy);
      }
    }
    return BuildTexture(size, size, result, src.filterMode);
  }

  private static Color32 SampleBilinear(Color32[] pixels, int width, int height, float x, float y)
  {
    if (x < 0f || y < 0f || x > width - 1 || y > height - 1)
      return new Color32(0, 0, 0, 0);

    int x0 = (int)x;
    int y0 = (int)y;
    int x1 = Mathf.Min(x0 + 1, width - 1);
    int y1 = Mathf.Min(y0 + 1, height - 1);
    float fx = x - x0;
    float fy = y - y0;

    Color32 top = Color32.Lerp(pixels[y0 * width + x0], pixels[y0 * width + x1], fx);
    Color32 bottom = Color32.Lerp(pixels[y1 * width + x0], pixels[y1 * width + x1], fx);
    return Color32.Lerp(top, bottom, fy);
  }

  private static Texture2D BuildTexture(int width, int height, Color32[] pixels, FilterMode filterMode)
  {
    Texture2D texture = new(width, height, TextureFormat.RGBA32, false)
    {
      filterMode = filterMode,
      wrapMode = TextureWrapMode.Clamp,
    };
    texture.SetPixels32(pixels);
    texture.Apply();
    return texture;
  }
}
