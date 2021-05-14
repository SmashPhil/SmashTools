using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace SmashTools
{
	public static class Ext_World
	{
		private static readonly Dictionary<Pair<int, int>, List<int>> tileNeighbors = new Dictionary<Pair<int, int>, List<int>>();

		/// <summary>
		/// Get neighbors of <paramref name="tile"/> on world map.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="offsets"></param>
		/// <param name="values"></param>
		/// <param name="tile"></param>
		/// <param name="outList"></param>
		public static List<int> GetTileNeighbors(int tile, int radius = 1)
		{
			if (radius == 1)
			{
				List<int> outList = new List<int>();
				Find.WorldGrid.GetTileNeighbors(tile, outList);
				return outList;
			}
			//if (tileNeighbors.TryGetValue(new Pair<int, int>(tile, radius), out outList))
			//{
			//	return;
			//}
			List<int> neighbors = new List<int>();
			Stack<int> stack = new Stack<int>();
			stack.Push(tile);
			List<int> newTilesSearch = new List<int>();
			HashSet<int> allSearchedTiles = new HashSet<int>() { tile };

			for (int i = 0; i < radius; i++)
			{
				newTilesSearch.Clear();
				int stackSize = stack.Count;
				for (int j = 0; j < stackSize; j++)
				{
					int searchTile = stack.Pop();
					Find.WorldGrid.GetTileNeighbors(searchTile, neighbors);
					foreach (int nTile in neighbors)
					{
						if (allSearchedTiles.Add(nTile))
						{
							newTilesSearch.Add(nTile);
						}
					}
				}
				stack = new Stack<int>(newTilesSearch);
			}
			return newTilesSearch;
		}
	}
}
