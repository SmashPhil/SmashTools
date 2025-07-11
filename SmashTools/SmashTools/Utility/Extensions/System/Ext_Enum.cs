using System;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace SmashTools;

/// <summary>
/// Utility methods for <see cref="Enum"/> types.
/// </summary>
[PublicAPI]
public static class Ext_Enum
{
  /// <summary>
  /// Returns the lesser of two enum values using the default comparer.
  /// </summary>
  /// <typeparam name="T">The enum type being compared.</typeparam>
  /// <param name="a">The first value to compare.</param>
  /// <param name="b">The second value to compare.</param>
  /// <returns>
  /// <paramref name="a"/> if it is less than or equal to <paramref name="b"/>; otherwise <paramref name="b"/>.
  /// </returns>
  public static T Min<T>(T a, T b) where T : Enum
  {
    return Comparer<T>.Default.Compare(a, b) <= 0 ? a : b;
  }

  /// <summary>
  /// Returns the greater of two enum values using the default comparer.
  /// </summary>
  /// <typeparam name="T">The enum type being compared.</typeparam>
  /// <param name="a">The first value to compare.</param>
  /// <param name="b">The second value to compare.</param>
  /// <returns>
  /// <paramref name="a"/> if it is greater than or equal to <paramref name="b"/>; otherwise <paramref name="b"/>.
  /// </returns>
  public static T Max<T>(T a, T b) where T : Enum
  {
    return Comparer<T>.Default.Compare(a, b) >= 0 ? a : b;
  }
}