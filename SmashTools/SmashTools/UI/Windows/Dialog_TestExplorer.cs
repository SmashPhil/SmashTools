using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace SmashTools.UnitTesting;

internal class Dialog_TestExplorer : Window
{
  private const float LineHeight = 30;
  private const float HeaderPadding = 30;
  private const float ResultIconSize = 24;
  private const float ExpandBtnSize = 20;
  private const float TabSize = ExpandBtnSize + ResultIconSize / 2;

  private readonly List<TestBatch> results;
  private readonly bool[] expanded;

  private Vector2 scrollPos;

  public Dialog_TestExplorer(List<TestBatch> results)
  {
    // copy results over, result list will be cleared from test manager when test enabler terminates.
    this.results = results;
    expanded = new bool[results.Count];
    RecacheHeight();
  }

  public override Vector2 InitialSize => new(UI.screenWidth, UI.screenHeight);

  private float Height { get; set; }

  private void RecacheHeight()
  {
    using TextBlock tb = new(GameFont.Small);
    Height = 0;
    for (int i = 0; i < results.Count; i++)
    {
      Height += LineHeight;
      if (expanded[i])
      {
        Height += LineHeight * results[i].results.Count;
      }
    }
  }

  public override void DoWindowContents(Rect inRect)
  {
    using (new TextBlock(GameFont.Medium, TextAnchor.UpperCenter))
    {
      Widgets.Label(inRect, "Test Explorer");
      inRect.yMin += HeaderPadding;
    }

    using TextBlock resultText = new(GameFont.Small, TextAnchor.MiddleLeft);

    Rect outRect = inRect;
    Rect viewRect = outRect.AtZero() with { width = outRect.width - 16, height = Height };
    Widgets.BeginScrollView(outRect, ref scrollPos, viewRect);
    float curY = 0;
    for (int i = 0; i < results.Count; i++)
    {
      Rect rect = new(viewRect.x, curY, viewRect.width, LineHeight);
      Rect expandBtnRect = (rect with { size = new Vector2(LineHeight, LineHeight) })
       .ContractedBy((LineHeight - ExpandBtnSize) / 2);
      Rect testChkRect = (expandBtnRect with
      {
        x = expandBtnRect.xMax,
        size = new Vector2(LineHeight, LineHeight)
      }).ContractedBy((LineHeight - ResultIconSize) / 2);
      Rect labelRect = testChkRect with
      {
        x = testChkRect.xMax,
        width = rect.width - testChkRect.width - expandBtnRect.width
      };
      labelRect.xMin += 5; // +5 to pad a bit between the label and result tick mark

      bool expand = expanded[i];
      TestBatch batch = results[i];
      if (UIElements.CollapseButton(expandBtnRect, ref expand))
      {
        expanded[i] = expand;
        if (expand)
          SoundDefOf.TabOpen.PlayOneShotOnCamera();
        else
          SoundDefOf.TabClose.PlayOneShotOnCamera();
        RecacheHeight();
      }
      UIElements.CheckboxDraw(testChkRect, !batch.Failed, false);
      Widgets.Label(labelRect, batch.unitTest.Name);
      Widgets.DrawBoxSolid(expandBtnRect, Color.red);
      Widgets.DrawBoxSolid(testChkRect, Color.green);
      if (expand)
      {
        rect.x += TabSize;
        expandBtnRect.x += TabSize;
        testChkRect.x += TabSize;
        foreach (UTResult resultGroup in batch.results)
        {
          foreach ((string name, UTResult.Result result) in resultGroup.Tests)
          {
            curY += LineHeight;
            testChkRect.y = curY;
            labelRect.y = curY;
            UIElements.CheckboxDraw(testChkRect, !batch.Failed, false);
            Widgets.Label(labelRect, name);
            Widgets.DrawBoxSolid(testChkRect, Color.cyan);
          }
        }
      }
      curY += LineHeight;
    }
    Widgets.EndScrollView();
  }
}