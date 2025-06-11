using System;
using JetBrains.Annotations;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SmashTools.Rendering;

/// <summary>
/// Double buffer implementation for seemless read / write commands
/// </summary>
[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public class RenderTextureBuffer : IDisposable
{
  private RenderTexture rtA;
  private RenderTexture rtB;

  public RenderTextureBuffer(RenderTexture rtA, RenderTexture rtB)
  {
    this.rtA = rtA;
    this.rtB = rtB;
    Read = rtA;
  }

  /// <summary>
  /// Get current read target without swapping
  /// </summary>
  public RenderTexture Read { get; private set; }

  public RenderTexture Write => Read == rtA ? rtB : rtA;

  /// <summary>
  /// Get current write target and swap
  /// </summary>
  public RenderTexture GetWrite()
  {
    Read = Write;
    return Read;
  }

  public void Dispose()
  {
    rtA.Release();
    rtB.Release();
    Object.Destroy(rtA);
    Object.Destroy(rtB);
    GC.SuppressFinalize(this);
  }
}