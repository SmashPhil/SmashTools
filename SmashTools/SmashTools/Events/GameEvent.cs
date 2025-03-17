using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmashTools;

public static class GameEvent
{
  // New game is being initialized.
  public static event Action onNewGame;

  // Existing save file is being loaded.
  public static event Action onLoadGame;

  // MainMenu scene has been loaded.
  public static event Action onMainMenu;

  // World and all maps are about to be unloaded.
  public static event Action onWorldUnloading;

  // World and all maps have finished being removed.
  public static event Action onWorldRemoved;

  internal static void OnNewGame()
  {
    onNewGame?.Invoke();
  }

  internal static void OnLoadGame()
  {
    onLoadGame?.Invoke();
  }

  internal static void OnMainMenu()
  {
    onMainMenu?.Invoke();
  }

  internal static void OnWorldUnloading()
  {
    onWorldUnloading?.Invoke();
  }

  internal static void OnWorldRemoved()
  {
    onWorldRemoved?.Invoke();
  }
}