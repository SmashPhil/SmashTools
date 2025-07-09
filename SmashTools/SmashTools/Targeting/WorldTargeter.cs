using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace SmashTools.Targeting;

public class WorldTargeter<TPayload> : Targeter<GlobalTargetInfo> where TPayload : ITargetOption
{
  private readonly IWorldTargeterSource<TPayload> source;

  private GlobalTargetInfo curTarget;
  private WorldTargetResult curResult;

  public WorldTargeter(IWorldTargeterSource<TPayload> source) : base(null)
  {
    this.source = source;
  }

  public WorldTargeter(IWorldTargeterSource<TPayload> source, ITargeterUpdate<GlobalTargetInfo> updater) : base(updater)
  {
    this.source = source;
  }

  public Texture2D TargetTexture { get; init; }

  protected override TargeterResult PrimaryClick()
  {
    if (targetData.targets.Count > 0 && curTarget == targetData.targets[^1])
      return TargeterResult.Submit;

    TargeterResult result = source.Select(curTarget);
    if (result.action is TargeterAction.Accept or TargeterAction.Submit)
      targetData.targets.Add(curTarget);
    return result;
  }

  protected override void Submit(ITargetOption option)
  {
    source.OnTargetingFinished(targetData, (TPayload)option);
  }

  public override void OnGUI()
  {
    base.OnGUI();
    if (!curResult.isValid)
      return;

    const float OffsetFromMouse = 8f;
    const float MouseIconSize = 32f;

    string tooltip = curResult.Tooltip;
    if (!tooltip.NullOrEmpty())
    {
      Vector2 mousePosition = Event.current.mousePosition;
      Rect iconRect = new(mousePosition.x + OffsetFromMouse, mousePosition.y + OffsetFromMouse,
        MouseIconSize, MouseIconSize);
      if (TargetTexture)
      {
        GUI.DrawTexture(iconRect, TargetTexture);
      }
      Vector2 labelGetterText = Text.CalcSize(tooltip);
      Rect rect = new(iconRect.xMax, iconRect.y, 9999f, 100f);
      Rect bgRect = new(rect.x - labelGetterText.x * 0.1f, rect.y, labelGetterText.x * 1.2f,
        labelGetterText.y);
      GUI.DrawTexture(bgRect, TexUI.GrayTextBG);
      Widgets.Label(rect, tooltip);
    }
  }

  public override void Update()
  {
    base.Update();
    UpdateTargetUnderMouse();
  }

  private void UpdateTargetUnderMouse()
  {
    curTarget = GlobalTargetInfo.Invalid;
    curResult = WorldTargetResult.Failed;

    List<WorldObject> objects = GenWorldUI.WorldObjectsUnderMouse(UI.MousePositionOnUI);
    if (objects.Count > 0)
    {
      foreach (WorldObject obj in objects)
      {
        WorldTargetResult targetResult = source.CanTarget(obj);
        if (targetResult.isValid)
        {
          curTarget = obj;
          curResult = targetResult;
          return;
        }
      }
    }
    PlanetTile tile = GenWorld.MouseTile();
    if (!tile.Valid)
      return;

    curTarget = new GlobalTargetInfo(tile);
    curResult = source.CanTarget(curTarget);
  }
}