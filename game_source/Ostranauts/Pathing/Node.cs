using System;

namespace Ostranauts.Pathing
{
	public class Node
	{
		public Node(Vector2Int pos)
		{
			this.Position = pos;
		}

		public float TotalCost
		{
			get
			{
				return this.CostStartToCurrent + this.CostCurrentToGoal;
			}
		}

		public Vector2Int Position;

		public Node Parent;

		public float CostStartToCurrent;

		public float CostCurrentToGoal;
	}
}
