using System.Diagnostics;

namespace SmashTools
{
  // Reimplementation of System.Diagnostics.Assert with IMGUI popup and RimWorld logger
  public static class Assert
  {
    [Conditional("DEBUG"), Conditional("ASSERT_ENABLED")]
    public static void IsTrue(bool condition, string message = null)
    {
      if (condition)
        return;
      Fail(message);
    }

    [Conditional("DEBUG"), Conditional("ASSERT_ENABLED")]
    public static void IsFalse(bool condition, string message = null)
    {
      if (!condition)
        return;
      Fail(message);
    }

    [Conditional("DEBUG"), Conditional("ASSERT_ENABLED")]
    public static void IsNull<T>(T obj, string message = null) where T : class
    {
      if (obj == null)
        return;
      Fail(message);
    }

    [Conditional("DEBUG"), Conditional("ASSERT_ENABLED")]
    public static void IsNotNull<T>(T obj, string message = null) where T : class
    {
      if (obj != null)
        return;
      Fail(message);
    }

    [Conditional("DEBUG"), Conditional("ASSERT_ENABLED")]
    public static void Fail(string message = null)
    {
      // NOTE - We don't need to insert the stack trace here, we'll be showing it in the assertion
      // popup and it will also be viewable via the message log window.
      if (Debugger.IsAttached) Debugger.Break();
      Debug.ShowStack("Assertion Failed!", message);
    }
  }
}