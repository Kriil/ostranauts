using System;
using Ostranauts.Utils.Models;

namespace Ostranauts.Core.Models
{
	public class BezierPoints
	{
		public Point vPos
		{
			get
			{
				return this.situ.vPos;
			}
		}

		public ShipSitu situ;

		public bool IsRegionalBorder;

		public bool isEdge;

		public double angleBasedSpeedFactor = 1.0;

		public double atmoBasedAccelerationFactor = 1.0;
	}
}
