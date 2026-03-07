using System;
using Ostranauts.ShipGUIs.Utilities;

namespace Ostranauts.Core.Models
{
	public class WaypointWrapper
	{
		public WaypointWrapper(NavDataPoint navDataPoint, bool isRegionalBorderPoint = false)
		{
			this.NavDataPoint = navDataPoint;
			this.IsRegionalBorderPoint = isRegionalBorderPoint;
		}

		public NavDataPoint NavDataPoint { get; private set; }

		public bool IsRegionalBorderPoint;
	}
}
