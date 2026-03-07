using System;

namespace Ostranauts.Pathing
{
	public interface IPathSearchProvider
	{
		CondOwner coUs { get; set; }

		Pathfinder pf { get; set; }

		CondOwner coDest { get; set; }

		PathResult GetPath(Tile destination, bool bAllowAirlocks, Tile tilCurrent);
	}
}
