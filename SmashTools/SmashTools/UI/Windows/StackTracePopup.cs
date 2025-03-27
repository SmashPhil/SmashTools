using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace SmashTools
{
  public class StackTracePopup : ModalWindow
  {
    private Vector2 size = new Vector2(500, 500);
    private string text;
    private string stackTrace;

    public StackTracePopup(string label, string text, IWindowDrawing customWindowDrawing = null)
      : base(label, customWindowDrawing)
    {
      this.text = text;
      this.stackTrace = StackTraceUtility.ExtractStackTrace();
      SetProperties();
    }

    public StackTracePopup(Vector2 size, string label, string text,
      IWindowDrawing customWindowDrawing = null)
      : this(label, text, customWindowDrawing)
    {
      this.size = size;
    }

    public override Vector2 InitialSize => size;

    private void SetProperties()
    {
      this.absorbInputAroundWindow = true;
      this.forcePause = true;
      this.forceCatchAcceptAndCancelEventEvenIfUnfocused = true;
      this.onlyDrawInDevMode = true;
      this.onlyOneOfTypeAllowed = true;
      this.preventCameraMotion = true;
      this.doCloseButton = false;
      this.doCloseX = true;
      this.closeOnAccept = false;
      this.closeOnCancel = false;
      this.closeOnClickedOutside = false;
      this.draggable = true;
      this.resizeable = true;
    }

    public override void PreClose()
    {
      SendToLog();
    }

    public void SendToLog()
    {
      Log.Error($"Assertion Failed! {text}\n\n{stackTrace}");
    }

    public override void DoWindowContents(Rect inRect)
    {
      float messageHeight;
      Rect messageRect;

      using (new TextBlock(GameFont.Small))
      {
        messageHeight = Text.CalcHeight(text, inRect.width);
        messageRect = new(inRect.x, inRect.y, inRect.width, messageHeight);

        Widgets.Label(messageRect, text);
      }

      using (new TextBlock(GameFont.Tiny))
      {
        float stackTraceHeight = Text.CalcHeight(stackTrace, inRect.width);
        Rect stackRect = new(inRect.x, messageRect.yMax, inRect.width, stackTraceHeight);
        Widgets.Label(stackRect, stackTrace);
      }
    }
  }
}