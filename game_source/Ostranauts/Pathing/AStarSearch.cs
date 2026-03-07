using System;
using System.Collections.Generic;
using Priority_Queue;
using UnityEngine;

namespace Ostranauts.Pathing
{
	public class AStarSearch : IPathSearchProvider
	{
		public CondOwner coUs { get; set; }

		public Pathfinder pf { get; set; }

		public CondOwner coDest { get; set; }

		public PathResult GetPath(Tile destination, bool bAllowAirlocks, Tile origin)
		{
			bool flag = false;
			bool flag2 = false;
			bool flag3 = false;
			SimplePriorityQueue<Tile> simplePriorityQueue = new SimplePriorityQueue<Tile>();
			this.tilesSearched.Clear();
			this.tileCosts.Clear();
			simplePriorityQueue.Enqueue(origin, 0f);
			this.tileCosts.Add(origin, 0f);
			Tile[] array = new Tile[8];
			string b = (!(destination.coProps != null) || destination.coProps.ship == null) ? string.Empty : destination.coProps.ship.strRegID;
			for (int i = 0; i < 3333; i++)
			{
				bool flag4 = i == 3332 || simplePriorityQueue.Count == 0;
				if (flag4)
				{
					if (i > 1511)
					{
						Debug.Log(string.Concat(new object[]
						{
							this.coUs.strName,
							" pathfinder searched ",
							i,
							" tiles."
						}));
					}
					PathResult pathResult = new PathResult(origin, destination);
					pathResult.SetTiles(null);
					pathResult.bGravBlocked = flag;
					pathResult.bForbidZoneBlocked = flag2;
					pathResult.bAirlockBlocked = flag3;
					return pathResult;
				}
				Tile tile = simplePriorityQueue.Dequeue();
				if (tile == destination)
				{
					break;
				}
				bool forceExact = tile.coProps != null && tile.coProps.ship != null && tile.coProps.ship.strRegID != b;
				TileUtils.GetSurroundingTiles(ref array, tile, forceExact);
				int num = 0;
				for (int j = 0; j < 8; j++)
				{
					Tile tile2 = array[j];
					if (!(tile2 == null))
					{
						float num2 = 1f;
						if (j > 3)
						{
							num2 = 1.4142135f;
						}
						if (j != 4 || (num & 3) == 0)
						{
							if (j != 5 || (num & 6) == 0)
							{
								if (j != 6 || (num & 9) == 0)
								{
									if (j != 7 || (num & 12) == 0)
									{
										if (tile2.IsPortal && tile2.IsWall)
										{
											num2 += 2f;
											if (Pathfinder.CheckPressure(tile2.tf.position, tile2.coProps.ship, tile2.room) && !bAllowAirlocks)
											{
												num |= 1 << j;
												flag3 = true;
												goto IL_3A6;
											}
										}
										float num3 = this.tileCosts[tile] + num2;
										float num4 = 100000000f;
										if (!this.tileCosts.TryGetValue(tile2, out num4))
										{
											num4 = 100000000f;
										}
										if (j < 3 || num4 > num3)
										{
											if (tile2.IsForbidden(this.coUs))
											{
												flag2 = true;
												num |= 1 << j;
											}
											else if (!tile2.bPassable && (!tile2.IsPortal || tile2.coProps.HasCond("IsPortalStuck", false)))
											{
												num |= 1 << j;
											}
											else if (tile2.IsEvaTileWithGravitation())
											{
												num |= 1 << j;
												flag = true;
											}
											else if (num4 > num3)
											{
												this.tileCosts[tile2] = num3;
												float priority = num3 + this.Heuristic(destination, tile2);
												simplePriorityQueue.Enqueue(tile2, priority);
												this.tilesSearched[tile2] = tile;
											}
										}
									}
								}
							}
						}
					}
					IL_3A6:;
				}
			}
			List<Tile> list = Pathfinder.BuildListFromTilesSearched(this.tilesSearched, origin, destination);
			PathResult pathResult2 = new PathResult(origin, destination);
			pathResult2.SetTiles(list);
			if (list == null)
			{
				pathResult2.bAirlockBlocked = flag3;
				pathResult2.bGravBlocked = flag;
				pathResult2.bForbidZoneBlocked = flag2;
			}
			return pathResult2;
		}

		private float Heuristic(Tile a, Tile b)
		{
			float num = a.tf.position.x - b.tf.position.x;
			float num2 = a.tf.position.y - b.tf.position.y;
			return Mathf.Sqrt(num * num + num2 * num2);
		}

		private Dictionary<Tile, Tile> tilesSearched = new Dictionary<Tile, Tile>();

		private Dictionary<Tile, float> tileCosts = new Dictionary<Tile, float>();
	}
}
