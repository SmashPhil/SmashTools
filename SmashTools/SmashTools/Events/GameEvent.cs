using System;

namespace SmashTools;

public static class GameEvent
{
  // New game is being initialized.
  public static event Action OnNewGame;

  // Existing save file is being loaded.
  public static event Action OnLoadGame;

  // MainMenu scene has been loaded.
  public static event Action OnMainMenu;

  // World and all maps are about to be unloaded.
  public static event Action OnWorldUnloading;

  // World and all maps have finished being removed.
  public static event Action OnWorldRemoved;

  // DefDatabase implied defs are currently in the pre-resolve stage.
  // Now is the time to add any implied defs from mods.
  public static event Action<bool> OnGenerateImpliedDefs;

  internal static void RaiseOnNewGame()
  {
    OnNewGame?.Invoke();
  }

  internal static void RaiseOnLoadGame()
  {
    OnLoadGame?.Invoke();
  }

  internal static void RaiseOnMainMenu()
  {
    OnMainMenu?.Invoke();
  }

  internal static void RaiseOnWorldUnloading()
  {
    OnWorldUnloading?.Invoke();
  }

  internal static void RaiseOnWorldRemoved()
  {
    OnWorldRemoved?.Invoke();
  }

  internal static void RaiseOnGenerateImpliedDefs(bool hotReload)
  {
    OnGenerateImpliedDefs?.Invoke(hotReload);
  }
}