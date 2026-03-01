using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Ostranauts.ShipGUIs.Utilities
{
	public class FlightPlan
	{
		public void CopyFrom(FlightPlan rhs)
		{
			this.dEscapeAngle = rhs.dEscapeAngle;
			this.dEscapeT = rhs.dEscapeT;
			this.dApproachAngle = rhs.dApproachAngle;
			this.dApproachT = rhs.dApproachT;
			this.dStartT = rhs.dStartT;
			this.dest = rhs.dest;
		}

		public void DebugLog()
		{
			Debug.Log(string.Concat(new object[]
			{
				"escape:",
				this.dEscapeAngle,
				"*",
				this.dEscapeT,
				"s"
			}));
			Debug.Log(string.Concat(new object[]
			{
				"approach:",
				this.dApproachAngle,
				"*",
				this.dApproachT,
				"s"
			}));
			double num;
			double num2;
			double num3;
			double num4;
			this.GetDestXY(this.dCurrentT, out num, out num2, out num3, out num4);
			Debug.Log(string.Concat(new object[]
			{
				"VX:",
				this.dCurrentVX,
				"destvx:",
				num3
			}));
		}

		public void Reset(ShipDraw sd, double t0)
		{
			this.dStartX = sd.ship.objSS.vPosx;
			this.dStartY = sd.ship.objSS.vPosy;
			this.dStartVX = sd.ship.objSS.vVelX;
			this.dStartVY = sd.ship.objSS.vVelY;
			this.dStartT = t0;
			this.dSunError = 0.0;
			this.SetStart();
		}

		public void SetDestination(NavPOI poi)
		{
			this.dest = poi;
		}

		private void SetStart()
		{
			this.dCurrentX = this.dStartX;
			this.dCurrentY = this.dStartY;
			this.dCurrentVX = this.dStartVX;
			this.dCurrentVY = this.dStartVY;
			this.dCurrentT = this.dStartT;
		}

		public void AppendDense(List<double> dense)
		{
			if (dense != null)
			{
				dense.Add(this.dCurrentX);
				dense.Add(this.dCurrentY);
			}
		}

		public void RunPhase(StarSystem objSystem, double angle, double thrust, double phaseTime, List<double> dense)
		{
			this.dAccelX = Math.Cos(angle) * thrust;
			this.dAccelY = Math.Sin(angle) * thrust;
			float num = 28800f;
			if (thrust == 0.0 && phaseTime < 6000.0)
			{
				num = 60f;
			}
			int num2 = (int)Mathf.Ceil((float)(phaseTime / (double)num / 2.0));
			num2 = num2 * 2 + 1;
			double deltaT = phaseTime / (double)num2;
			for (int i = 0; i < num2; i++)
			{
				this.AppendDense(dense);
				this.Advance(objSystem, deltaT);
			}
			this.AppendDense(dense);
		}

		public void FullAdvance(StarSystem objSystem, List<double> dense)
		{
			float num = this.fThrustMax;
			this.RunPhase(objSystem, this.dEscapeAngle, (double)num, this.dEscapeT, dense);
			this.RunPhase(objSystem, this.dApproachAngle, (double)num, this.dApproachT, dense);
		}

		public double GetDuration()
		{
			return this.dEscapeT + this.dApproachT;
		}

		private void Advance(StarSystem objSystem, double deltaT)
		{
			this.dCurrentT += deltaT;
			double num = this.dAccelX;
			double num2 = this.dAccelY;
			Vector2 gravAccelPoint = objSystem.GetGravAccelPoint(objSystem.aBOs.First<KeyValuePair<string, BodyOrbit>>().Value, this.dCurrentX, this.dCurrentY);
			num += (double)gravAccelPoint.x;
			num2 += (double)gravAccelPoint.y;
			double num3 = 0.5 * deltaT * deltaT;
			this.dCurrentX += this.dCurrentVX * deltaT + num * num3;
			this.dCurrentY += this.dCurrentVY * deltaT + num2 * num3;
			this.dCurrentVX += num * deltaT;
			this.dCurrentVY += num2 * deltaT;
			double num4 = 1.0 - (this.dCurrentX * this.dCurrentX + this.dCurrentY * this.dCurrentY) * 500.0;
			if (num4 > 0.0)
			{
				this.dSunError += num4;
			}
		}

		public void GetDestXY(double t, out double x, out double y, out double vx, out double vy)
		{
			this.dest.UpdateTime(t);
			this.dest.GetSXY(out x, out y);
			this.dest.GetVSXY(out vx, out vy);
		}

		public double CalcError()
		{
			double num = 0.0;
			double num2;
			double num3;
			double num4;
			double num5;
			this.GetDestXY(this.dCurrentT, out num2, out num3, out num4, out num5);
			num2 -= this.dCurrentX;
			num3 -= this.dCurrentY;
			num += num2 * num2 + num3 * num3;
			num4 -= this.dCurrentVX;
			num5 -= this.dCurrentVY;
			double num6 = num4 * num4 + num5 * num5;
			num += num6 * 10000000000.0;
			num += (this.dEscapeT + this.dApproachT) / 86400.0 / 100.0;
			return num + this.dSunError * 10000000.0;
		}

		public double dEscapeAngle;

		public double dEscapeT;

		public double dApproachAngle;

		public double dApproachT;

		public double dStartX;

		public double dStartY;

		private double dStartVX;

		private double dStartVY;

		private double dStartT;

		private double dAccelX;

		private double dAccelY;

		public double dCurrentX;

		public double dCurrentY;

		private double dCurrentVX;

		private double dCurrentVY;

		private double dCurrentT;

		private NavPOI dest;

		public double dDestT;

		public double dSunError;

		public float fThrustMax;
	}
}
