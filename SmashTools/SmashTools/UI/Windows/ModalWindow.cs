using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace SmashTools
{
#if DEBUG
  [StaticConstructorOnModInit]
#endif
  public abstract class ModalWindow : Window
  {
    private static readonly MethodInfo innerWindowGUIMethod;
    private static readonly FieldInfo resizerField;
    private static readonly FieldInfo resizeLaterField;
    private static readonly FieldInfo resizeLaterRectField;

    protected IWindowDrawing windowDrawing;
    protected GUI.WindowFunction innerWindowOnGUICached;

    static ModalWindow()
    {
      innerWindowGUIMethod = AccessTools.Method(typeof(Window), "InnerWindowOnGUI");
      resizerField = AccessTools.Field(typeof(Window), "resizer");
      resizeLaterField = AccessTools.Field(typeof(Window), "resizeLater");
      resizeLaterRectField = AccessTools.Field(typeof(Window), "resizeLaterRect");

      // Validate fields at startup. If modal window throws
      // exceptions, it could block application input.
      Assert.IsNotNull(innerWindowGUIMethod);
      Assert.IsNotNull(resizerField);
      Assert.IsNotNull(resizeLaterField);
      Assert.IsNotNull(resizeLaterRectField);
    }

    public ModalWindow(string label, IWindowDrawing customWindowDrawing = null) : base(customWindowDrawing)
    {
      optionalTitle = label;

      Action<int> windowFunc = (Action<int>)Delegate.CreateDelegate(typeof(Action<int>), this, innerWindowGUIMethod);
      innerWindowOnGUICached = new GUI.WindowFunction(windowFunc);
      windowDrawing = customWindowDrawing ?? new DefaultWindowDrawing();
    }

    public override void PreOpen()
    {
      base.PreOpen();
      object resizer = resizerField.GetValue(this);
      if (resizer == null) resizerField.SetValue(this, new WindowResizer());
    }

    public override void WindowOnGUI()
    {
      if (!drawInScreenshotMode && Find.UIRoot.screenshotMode.Active) return;
      if (onlyDrawInDevMode && !Prefs.DevMode) return;

      if (resizeable)
      {
        if ((bool)resizeLaterField.GetValue(this))
        {
          resizeLaterField.SetValue(this, false);
          windowRect = (Rect)resizeLaterRectField.GetValue(this);
        }
      }
      windowRect = windowRect.Rounded();
      windowRect = GUI.ModalWindow(ID, windowRect, innerWindowOnGUICached, string.Empty, windowDrawing.EmptyStyle);
    }
  }
}
