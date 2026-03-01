using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ostranauts.Pathing
{
	public class PathMemory
	{
		public PathResult HasResult(Tile origin, Tile destination)
		{
			if (Time.frameCount != this.FrameTime)
			{
				return null;
			}
			if (origin.Position.x != this.Origin.x || origin.Position.y != this.Origin.y)
			{
				return null;
			}
			PathResult result = null;
			if (this.SearchedPaths.TryGetValue(destination.Position, out result))
			{
				return result;
			}
			return null;
		}

		public void RememberResult(PathResult pr)
		{
			if (pr == null || pr.Origin == null || pr.Dest == null)
			{
				return;
			}
			if (Time.frameCount != this.FrameTime || pr.Origin.Position != this.Origin)
			{
				this.Reset();
			}
			this.Origin = pr.Origin.Position;
			this.SearchedPaths[pr.Dest.Position] = pr;
		}

		public void Reset()
		{
			this.FrameTime = Time.frameCount;
			this.SearchedPaths.Clear();
		}

		private int FrameTime;

		private Vector2Int Origin;

		private Dictionary<Vector2Int, PathResult> SearchedPaths = new Dictionary<Vector2Int, PathResult>();
	}
}
