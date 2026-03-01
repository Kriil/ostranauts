using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ostranauts.Pathing
{
	// Pathfinder backend implementing Jump Point Search over the ship tile grid.
	// Used by Pathfinder to build efficient tile paths across docked ships.
	public class JumpPointSearch : IPathSearchProvider
	{
		public CondOwner coUs { get; set; }

		public Pathfinder pf { get; set; }

		public CondOwner coDest { get; set; }

		// Builds a walkability grid large enough to cover the current ship and all
		// loaded docked ships, then stores the coordinate offsets.
		private bool?[,] GetGridSize()
		{
			Vector2 vShipPos = this.coUs.ship.vShipPos;
			this._gridXMax = this.coUs.ship.nCols;
			this._gridYMax = this.coUs.ship.nRows;
			float num = vShipPos.x;
			float num2 = vShipPos.y - (float)this.coUs.ship.nRows;
			float num3 = vShipPos.x + (float)this.coUs.ship.nCols;
			float num4 = vShipPos.y;
			foreach (Ship ship in this.coUs.ship.GetAllDockedShips())
			{
				if (ship.LoadState > Ship.Loaded.Shallow)
				{
					num = Math.Min(num, ship.vShipPos.x);
					num2 = Math.Min(num2, ship.vShipPos.y - (float)ship.nRows);
					num3 = Math.Max(num3, ship.vShipPos.x + (float)ship.nCols);
					num4 = Math.Max(num4, ship.vShipPos.y);
				}
			}
			vShipPos = new Vector2(num, num4);
			this._gridXMax = (int)(num3 - num);
			this._gridYMax = (int)(num4 - num2);
			this._gridOffsetX = (int)(0f - vShipPos.x);
			this._gridOffsetY = (int)((float)this._gridYMax - vShipPos.y);
			this._gridXMax++;
			this._gridYMax++;
			return new bool?[this._gridXMax, this._gridYMax];
		}

		// Entry point used by Pathfinder: runs JPS from the current tile to the
		// destination tile and converts the result into a PathResult.
		public PathResult GetPath(Tile destination, bool bAllowAirlocks, Tile tilCurrent)
		{
			this._pr = new PathResult(bAllowAirlocks);
			if (this.coUs == null || this.coUs.ship == null)
			{
				return this._pr;
			}
			this._walkableGrid = this.GetGridSize();
			List<Vector2Int> list = this.FindPath(tilCurrent.Position, destination.Position);
			if (list == null)
			{
				return this._pr;
			}
			this._pr.Origin = tilCurrent;
			this._pr.Dest = destination;
			float num = 0f;
			Vector2Int a = new Vector2Int(tilCurrent.Position.x, tilCurrent.Position.y);
			Ship ship = tilCurrent.coProps.ship;
			this._pr.CheckDisembarkSimple();
			foreach (Vector2Int vector2Int in list)
			{
				num += Vector2Int.Distance(a, vector2Int);
				a = vector2Int;
				Tile tileAtWorldCoords = this.coUs.ship.GetTileAtWorldCoords1((float)vector2Int.x, (float)vector2Int.y, true, false);
				if (!this._pr.Disembark && tileAtWorldCoords.coProps != null)
				{
					this._pr.Disembark = (ship != tileAtWorldCoords.coProps.ship);
				}
				this._pr.AddTile(tileAtWorldCoords);
			}
			this._pr.PathLength = num;
			return this._pr;
		}

		// Standard JPS/A*-style search over jump points.
		private List<Vector2Int> FindPath(Vector2Int start, Vector2Int target)
		{
			if (!this.IsWalkable(target.x, target.y))
			{
				return null;
			}
			JPSPriorityQueue<Node> jpspriorityQueue = new JPSPriorityQueue<Node>();
			HashSet<Vector2Int> hashSet = new HashSet<Vector2Int>();
			Dictionary<Vector2Int, Node> dictionary = new Dictionary<Vector2Int, Node>();
			Node node = new Node(start);
			node.CostStartToCurrent = 0f;
			node.CostCurrentToGoal = this.Heuristic(start, target);
			jpspriorityQueue.Enqueue(node);
			dictionary[start] = node;
			while (jpspriorityQueue.Count > 0)
			{
				Node node2 = jpspriorityQueue.Dequeue();
				if (node2.Position == target)
				{
					return this.ReconstructPath(node2);
				}
				hashSet.Add(node2.Position);
				this.FindSuccessors(node2, target, jpspriorityQueue, hashSet, dictionary);
			}
			return null;
		}

		// Expands the jump-point successors from the current node.
		private void FindSuccessors(Node current, Vector2Int target, JPSPriorityQueue<Node> openSet, HashSet<Vector2Int> closedSet, Dictionary<Vector2Int, Node> nodeMap)
		{
			this._neighbors.Clear();
			this.FindNeighbors(current, ref this._neighbors);
			foreach (Vector2Int vector2Int in this._neighbors)
			{
				Vector2Int vector2Int2 = this.Jump(current.Position, new Vector2Int(vector2Int.x - current.Position.x, vector2Int.y - current.Position.y), target);
				if (!(vector2Int2 == JumpPointSearch.INVALID_POINT) && !closedSet.Contains(vector2Int2))
				{
					float num = Vector2.Distance(new Vector2((float)current.Position.x, (float)current.Position.y), new Vector2((float)vector2Int2.x, (float)vector2Int2.y));
					float num2 = current.CostStartToCurrent + num;
					Node node = null;
					if (!nodeMap.TryGetValue(vector2Int2, out node))
					{
						node = new Node(vector2Int2);
						nodeMap[vector2Int2] = node;
						node.CostCurrentToGoal = this.Heuristic(vector2Int2, target);
					}
					else if (num2 >= node.CostStartToCurrent)
					{
						continue;
					}
					node.Parent = current;
					node.CostStartToCurrent = num2;
					if (!openSet.Contains(node))
					{
						openSet.Enqueue(node);
					}
					else
					{
						openSet.UpdatePriority(node);
					}
				}
			}
		}

		// Applies the JPS neighbor-pruning rules based on the parent direction.
		private void FindNeighbors(Node node, ref List<Vector2Int> neighbors)
		{
			Vector2Int position = node.Position;
			if (node.Parent == null)
			{
				for (int i = 0; i < 8; i++)
				{
					Vector2Int item = new Vector2Int(position.x + JumpPointSearch.Directions[i].x, position.y + JumpPointSearch.Directions[i].y);
					if (this.IsWalkable(item.x, item.y))
					{
						neighbors.Add(item);
					}
				}
				return;
			}
			Vector2Int vector2Int = new Vector2Int(Math.Sign(position.x - node.Parent.Position.x), Math.Sign(position.y - node.Parent.Position.y));
			if (vector2Int.x != 0 && vector2Int.y != 0)
			{
				if (this.IsWalkable(position.x, position.y + vector2Int.y))
				{
					neighbors.Add(new Vector2Int(position.x, position.y + vector2Int.y));
				}
				if (this.IsWalkable(position.x + vector2Int.x, position.y))
				{
					neighbors.Add(new Vector2Int(position.x + vector2Int.x, position.y));
				}
				if (this.IsWalkable(position.x + vector2Int.x, position.y + vector2Int.y))
				{
					neighbors.Add(new Vector2Int(position.x + vector2Int.x, position.y + vector2Int.y));
				}
				if (!this.IsWalkable(position.x - vector2Int.x, position.y) && this.IsWalkable(position.x - vector2Int.x, position.y + vector2Int.y))
				{
					neighbors.Add(new Vector2Int(position.x - vector2Int.x, position.y + vector2Int.y));
				}
				if (!this.IsWalkable(position.x, position.y - vector2Int.y) && this.IsWalkable(position.x + vector2Int.x, position.y - vector2Int.y))
				{
					neighbors.Add(new Vector2Int(position.x + vector2Int.x, position.y - vector2Int.y));
				}
			}
			else if (vector2Int.x != 0)
			{
				if (this.IsWalkable(position.x + vector2Int.x, position.y))
				{
					neighbors.Add(new Vector2Int(position.x + vector2Int.x, position.y));
				}
				if (!this.IsWalkable(position.x, position.y + 1) && this.IsWalkable(position.x + vector2Int.x, position.y + 1))
				{
					neighbors.Add(new Vector2Int(position.x + vector2Int.x, position.y + 1));
				}
				if (!this.IsWalkable(position.x, position.y - 1) && this.IsWalkable(position.x + vector2Int.x, position.y - 1))
				{
					neighbors.Add(new Vector2Int(position.x + vector2Int.x, position.y - 1));
				}
			}
			else
			{
				if (this.IsWalkable(position.x, position.y + vector2Int.y))
				{
					neighbors.Add(new Vector2Int(position.x, position.y + vector2Int.y));
				}
				if (!this.IsWalkable(position.x + 1, position.y) && this.IsWalkable(position.x + 1, position.y + vector2Int.y))
				{
					neighbors.Add(new Vector2Int(position.x + 1, position.y + vector2Int.y));
				}
				if (!this.IsWalkable(position.x - 1, position.y) && this.IsWalkable(position.x - 1, position.y + vector2Int.y))
				{
					neighbors.Add(new Vector2Int(position.x - 1, position.y + vector2Int.y));
				}
			}
		}

		private Vector2Int Jump(Vector2Int current, Vector2Int direction, Vector2Int target)
		{
			Vector2Int vector2Int = new Vector2Int(current.x + direction.x, current.y + direction.y);
			if (!this.IsWalkable(vector2Int.x, vector2Int.y))
			{
				return JumpPointSearch.INVALID_POINT;
			}
			if (Tile.IsDoor((float)vector2Int.x, (float)vector2Int.y, this.coUs))
			{
				return vector2Int;
			}
			if (direction.x != 0 && direction.y != 0)
			{
				bool flag = this.IsWalkable(vector2Int.x - direction.x, vector2Int.y + direction.y);
				bool flag2 = this.IsWalkable(vector2Int.x - direction.x, vector2Int.y);
				bool flag3 = this.IsWalkable(vector2Int.x + direction.x, vector2Int.y - direction.y);
				bool flag4 = this.IsWalkable(vector2Int.x, vector2Int.y - direction.y);
				if (!flag2 && !flag4)
				{
					return JumpPointSearch.INVALID_POINT;
				}
				if ((flag && !flag2) || (flag3 && !flag4))
				{
					return vector2Int;
				}
				Vector2Int a = this.Jump(vector2Int, new Vector2Int(direction.x, 0), target);
				Vector2Int a2 = this.Jump(vector2Int, new Vector2Int(0, direction.y), target);
				if (a != JumpPointSearch.INVALID_POINT || a2 != JumpPointSearch.INVALID_POINT)
				{
					return vector2Int;
				}
			}
			else if (direction.x != 0)
			{
				if ((this.IsWalkable(vector2Int.x + direction.x, vector2Int.y + 1) && !this.IsWalkable(vector2Int.x, vector2Int.y + 1)) || (this.IsWalkable(vector2Int.x + direction.x, vector2Int.y - 1) && !this.IsWalkable(vector2Int.x, vector2Int.y - 1)))
				{
					return vector2Int;
				}
			}
			else if (direction.y != 0 && ((this.IsWalkable(vector2Int.x + 1, vector2Int.y + direction.y) && !this.IsWalkable(vector2Int.x + 1, vector2Int.y)) || (this.IsWalkable(vector2Int.x - 1, vector2Int.y + direction.y) && !this.IsWalkable(vector2Int.x - 1, vector2Int.y))))
			{
				return vector2Int;
			}
			if (vector2Int == target)
			{
				return vector2Int;
			}
			return this.Jump(vector2Int, direction, target);
		}

		private List<Vector2Int> ReconstructPath(Node end)
		{
			List<Vector2Int> list = new List<Vector2Int>();
			for (Node node = end; node != null; node = node.Parent)
			{
				list.Add(node.Position);
			}
			list.Reverse();
			return list;
		}

		private float Heuristic(Vector2Int a, Vector2Int b)
		{
			int num = a.x - b.x;
			int num2 = a.y - b.y;
			return Mathf.Sqrt((float)(num * num + num2 * num2));
		}

		private bool IsWalkable(int x, int y)
		{
			int num = x + this._gridOffsetX;
			int num2 = y + this._gridOffsetY;
			if (num < 0 || num >= this._gridXMax || num2 < 0 || num2 >= this._gridYMax)
			{
				return false;
			}
			bool? flag = this._walkableGrid[num, num2];
			if (flag != null)
			{
				bool? flag2 = this._walkableGrid[num, num2];
				return flag2.Value;
			}
			Tile tileAtWorldCoords = this.coUs.ship.GetTileAtWorldCoords1((float)x, (float)y, true, true);
			bool flag3 = tileAtWorldCoords != null && tileAtWorldCoords.IsWalkable(this.coUs, this._pr);
			this._walkableGrid[num, num2] = new bool?(flag3);
			return flag3;
		}

		private static readonly Vector2Int INVALID_POINT = new Vector2Int(int.MinValue, int.MinValue);

		private static readonly Vector2Int[] Directions = new Vector2Int[]
		{
			new Vector2Int(0, 1),
			new Vector2Int(1, 1),
			new Vector2Int(1, 0),
			new Vector2Int(1, -1),
			new Vector2Int(0, -1),
			new Vector2Int(-1, -1),
			new Vector2Int(-1, 0),
			new Vector2Int(-1, 1)
		};

		private bool?[,] _walkableGrid;

		private int _gridXMax;

		private int _gridYMax;

		private int _gridOffsetX;

		private int _gridOffsetY;

		private PathResult _pr;

		private List<Vector2Int> _neighbors = new List<Vector2Int>();
	}
}
