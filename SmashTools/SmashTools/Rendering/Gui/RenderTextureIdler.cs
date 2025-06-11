using System;
using SmashTools.Performance;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SmashTools.Rendering;

/// <summary>
/// Wrapper class for binding the lifetime of a <see cref="RenderTexture"/> or <see cref="RenderTextureBuffer"/> to a timer.
/// <para/>
/// Each time the render texture or buffer is read from, the timer will reset to 0. If the timer reaches the expiry
/// threshold — meaning the resources acquired haven't been accessed for that amount of time — all resources will be
/// freed and its timer will stop.
/// </summary>
public class RenderTextureIdler : IDisposable
{
  private readonly RenderTexture renderTexture;
  private readonly RenderTextureBuffer buffer;

  private readonly float expiryTime;
  private float timeSinceRead;

  private RenderTextureIdler(float expiryTime)
  {
    this.expiryTime = expiryTime;
    UnityThread.StartUpdate(Update);
  }

  /// <param name="renderTexture">RenderTexture used in this wrapper. Will be freed when timer expires.</param>
  /// <param name="expiryTime">Time till resources contained in this wrapper class are destroyed. Time will reset
  /// every time a resource is read.</param>
  public RenderTextureIdler(RenderTexture renderTexture, float expiryTime) : this(expiryTime)
  {
    this.renderTexture = renderTexture;
  }

  /// <param name="buffer">RenderTextureBuffer used in this wrapper. Will be freed when timer expires.</param>
  /// <param name="expiryTime">Time till resources contained in this wrapper class are destroyed. Time will reset
  /// every time a resource is read.</param>
  public RenderTextureIdler(RenderTextureBuffer buffer, float expiryTime) : this(expiryTime)
  {
    this.buffer = buffer;
  }

  /// <summary>
  /// UnitTest hook for OnUpdate function reference.
  /// </summary>
  internal UnityThread.OnUpdate UpdateLoop => Update;

  public bool Disposed => renderTexture == null && buffer == null;

  public RenderTexture Read
  {
    get
    {
      timeSinceRead = 0;
      return renderTexture ?? buffer.Read;
    }
  }

  public RenderTexture Write
  {
    get
    {
      timeSinceRead = 0;
      return renderTexture ?? buffer.Write;
    }
  }

  public RenderTexture GetWrite()
  {
    timeSinceRead = 0;
    return renderTexture ?? buffer.GetWrite();
  }

  internal void SetTimeDirect(float timeSinceRead)
  {
    this.timeSinceRead = timeSinceRead;
  }

  private bool Update()
  {
    timeSinceRead += Time.deltaTime;

    if (timeSinceRead < expiryTime)
      return true;

    Dispose();
    return false; // Dequeues from Update loop
  }

  public void Dispose()
  {
    if (renderTexture != null)
    {
      renderTexture.Release();
      Object.Destroy(renderTexture);
    }
    buffer?.Dispose();
    GC.SuppressFinalize(this);
  }
}