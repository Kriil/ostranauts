using System;

namespace Ostranauts.Ships
{
	public class TargetData
	{
		public TargetData(ShipSitu situ)
		{
			if (situ == null)
			{
				return;
			}
			this.Situ = situ;
		}

		public TargetData(Ship ship)
		{
			if (ship == null)
			{
				return;
			}
			this.Ship = ship;
			this.Situ = ship.objSS;
		}

		public ShipSitu Situ;

		public Ship Ship;
	}
}
