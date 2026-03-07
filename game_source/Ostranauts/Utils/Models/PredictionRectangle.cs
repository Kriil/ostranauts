using System;

namespace Ostranauts.Utils.Models
{
	public class PredictionRectangle
	{
		public PredictionRectangle(Ship shipUs, Ship atc)
		{
			double fAccel = MathUtils.Min(shipUs.RCSAccelMax, 1.9016982118751132E-10);
			Point point = shipUs.objSS.vVel - atc.objSS.vVel;
			float num = (!shipUs.IsDockedFull()) ? 1f : 2f;
			double scalar = MathUtils.GetStoppingDistance(point.magnitude, fAccel) * (double)num;
			float num2 = shipUs.objSS.GetRadiusAU() * 6f;
			Point v = new Point(point.Y, -point.X);
			this.B = shipUs.objSS.vPos + v.normalized * (double)num2;
			this.A = shipUs.objSS.vPos + (v * -1.0).normalized * (double)num2;
			this.ab = this.B - this.A;
			this.bc = point.normalized * scalar;
			this.C = this.bc + this.B;
			this.D = this.A + this.bc;
		}

		public bool IsInsideRectangle(Point pos)
		{
			Point b = pos - this.A;
			double num = this.ab.Dot(b);
			double num2 = this.ab.Dot(this.ab);
			if (0.0 <= num && num <= num2)
			{
				Point b2 = pos - this.B;
				double num3 = this.bc.Dot(b2);
				double num4 = this.bc.Dot(this.bc);
				return 0.0 <= num3 && num3 <= num4;
			}
			return false;
		}

		public bool HasIntersectingdVel(Point posUs, Ship shipThem, Ship atc)
		{
			double fAccel = MathUtils.Min(shipThem.RCSAccelMax, 1.9016982118751132E-10);
			Point point = shipThem.objSS.vVel - atc.objSS.vVel;
			double scalar = MathUtils.GetStoppingDistance(point.magnitude, fAccel) * 0.5;
			return MathUtils.AreLinesIntersecting(posUs, posUs + this.bc, shipThem.objSS.vPos, shipThem.objSS.vPos + point.normalized * scalar);
		}

		private void DebugVisualize(Point a, Point b, Point c, Point d)
		{
			ShipSitu shipSitu = new ShipSitu(CrewSim.coPlayer.ship.objSS);
			shipSitu.vPosx = a.X;
			shipSitu.vPosy = a.Y;
			shipSitu.LockToBO(-1.0, false);
			GUIOrbitDraw.AddDebugDraw("A", shipSitu, false, null);
			ShipSitu shipSitu2 = new ShipSitu(CrewSim.coPlayer.ship.objSS);
			shipSitu2.vPosx = b.X;
			shipSitu2.vPosy = b.Y;
			shipSitu2.LockToBO(-1.0, false);
			GUIOrbitDraw.AddDebugDraw("B", shipSitu2, false, null);
			ShipSitu shipSitu3 = new ShipSitu(CrewSim.coPlayer.ship.objSS);
			shipSitu3.vPosx = c.X;
			shipSitu3.vPosy = c.Y;
			shipSitu3.LockToBO(-1.0, false);
			GUIOrbitDraw.AddDebugDraw("C", shipSitu3, false, null);
			ShipSitu shipSitu4 = new ShipSitu(CrewSim.coPlayer.ship.objSS);
			shipSitu4.vPosx = d.X;
			shipSitu4.vPosy = d.Y;
			shipSitu4.LockToBO(-1.0, false);
			GUIOrbitDraw.AddDebugDraw("D", shipSitu4, false, null);
		}

		public Point A;

		public Point B;

		public Point C;

		public Point D;

		private Point ab;

		private Point bc;
	}
}
