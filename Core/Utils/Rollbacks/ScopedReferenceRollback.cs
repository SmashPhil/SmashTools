using System;
using System.Runtime.InteropServices;
using JetBrains.Annotations;

namespace CoreLib;

/// <summary>
/// Reference wrapper in place of ref fields which are not supported in .Net Framework 4.8
/// </summary>
public class Ref<T>
{
  public T Value { get; set; }

  public static implicit operator T(Ref<T> reference)
  {
    return reference.Value;
  }
}

/// <summary>
/// Records the current value of a class object and rolls it back when this object goes out of scope.
/// </summary>
/// <typeparam name="T">Type of the object being rolled back.</typeparam>
[PublicAPI]
public readonly struct ScopedReferenceRollback<T> : IDisposable
{
  private readonly Ref<T> reference;
  private readonly T value;

  /// <summary>
  /// Create temporary value state that rolls back to previous value when this struct goes out of scope.
  /// </summary>
  /// <param name="reference">Reference to rollback when this struct goes out of scope.</param>
  public ScopedReferenceRollback(Ref<T> reference)
  {
    this.reference = reference;
    value = reference.Value;
  }

  /// <summary>
  /// Create temporary value state that rolls back to previous value when this struct goes out of scope.
  /// </summary>
  /// <param name="reference">Reference to rollback when this struct goes out of scope.</param>
  /// <param name="value">Value to assign temporarily.</param>
  public ScopedReferenceRollback(Ref<T> reference, T value)
  {
    this.reference = reference;
    this.value = reference.Value;
    reference.Value = value;
  }

  public void Dispose()
  {
    reference.Value = value;
  }
}