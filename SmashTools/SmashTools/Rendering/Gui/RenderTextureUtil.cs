using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace SmashTools.Rendering;

public static class RenderTextureUtil
{
  public static RenderTexture CreateRenderTexture(int width, int height)
  {
    RenderTexture rt = new(width, height, 0, RenderTextureFormat.ARGBFloat.OrNextSupportedFormat());
    rt.Create();
    return rt;
  }

  public static RenderTextureFormat OrNextSupportedFormat(
    this RenderTextureFormat renderTextureFormat)
  {
    if (SystemInfo.SupportsRenderTextureFormat(renderTextureFormat))
      return renderTextureFormat;

    if (renderTextureFormat == RenderTextureFormat.R8 &&
      SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.RG16))
    {
      return RenderTextureFormat.RG16;
    }
    if ((renderTextureFormat is RenderTextureFormat.R8 or RenderTextureFormat.RG16) &&
      SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.ARGB32))
    {
      return RenderTextureFormat.ARGB32;
    }
    if ((renderTextureFormat is RenderTextureFormat.R8 or RenderTextureFormat.RHalf
        or RenderTextureFormat.RFloat) &&
      SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.RGFloat))
    {
      return RenderTextureFormat.RGFloat;
    }
    if ((renderTextureFormat is RenderTextureFormat.R8 or RenderTextureFormat.RHalf
        or RenderTextureFormat.RFloat or RenderTextureFormat.RGFloat) &&
      SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.ARGBFloat))
    {
      return RenderTextureFormat.ARGBFloat;
    }
    return renderTextureFormat;
  }
}