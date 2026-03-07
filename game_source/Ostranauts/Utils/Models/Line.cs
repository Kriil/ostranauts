using System;

namespace Ostranauts.Utils.Models
{
	public class Line
	{
		public Line(ShipSitu shipSitu, float predictionTime)
		{
			this.A = new Point(shipSitu.vPosx, shipSitu.vPosy);
			this.B = shipSitu.GetPredictedPosition(predictionTime);
		}

		public Point A { get; private set; }

		public Point B { get; private set; }
	}
}
