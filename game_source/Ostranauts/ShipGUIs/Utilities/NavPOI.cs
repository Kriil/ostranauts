using System;
using System.Collections.Generic;
using Ostranauts.Utils.Models;
using UnityEngine;

namespace Ostranauts.ShipGUIs.Utilities
{
	public class NavPOI
	{
		public NavPOI(double sx, double sy)
		{
			this.sx = sx;
			this.sy = sy;
		}

		public NavPOI(BodyOrbit bodyOrbit)
		{
			if (bodyOrbit == null)
			{
				Debug.Log("ERROR: No BO provided for BO NavPOI!");
				Debug.Break();
			}
			this.bodyOrbit = bodyOrbit;
		}

		public NavPOI(ShipDraw shipDraw, Ship playerShip, Dictionary<string, string> dictPropMap)
		{
			if (shipDraw == null)
			{
				Debug.Log("ERROR: No ship provided for ship NavPOI!");
				Debug.Break();
				return;
			}
			this.TargetShipInfo = ShipInfo.GetShipInfo(playerShip, shipDraw.ship, dictPropMap);
			this.Ship = shipDraw.ship;
		}

		public string name
		{
			get
			{
				if (this.bodyOrbit != null)
				{
					return this.bodyOrbit.strName;
				}
				if (this.Ship != null)
				{
					return this.Ship.strRegID;
				}
				return "None";
			}
		}

		public Ship Ship
		{
			get
			{
				if (this._ship != null && this._ship.bDestroyed)
				{
					this._ship = CrewSim.system.GetShipByRegID(this._ship.strRegID);
				}
				return this._ship;
			}
			private set
			{
				this._ship = value;
			}
		}

		public bool IsShipOrOrbit()
		{
			return (this.Ship != null && !this.Ship.bDestroyed) || this.bodyOrbit != null;
		}

		public Point GetSXY(out double sx, out double sy)
		{
			sx = this.sx + this.fOffsetSX;
			sy = this.sy + this.fOffsetSY;
			if (this.bodyOrbit != null)
			{
				if (this.fTargetFuture > 0.0)
				{
					this.bodyOrbit.UpdateTime(StarSystem.fEpoch + this.fTargetFuture, true, true);
					sx = this.bodyOrbit.dXReal + this.fOffsetSX;
					sy = this.bodyOrbit.dYReal + this.fOffsetSY;
					this.bodyOrbit.UpdateTime(StarSystem.fEpoch, true, true);
				}
				else
				{
					sx = this.bodyOrbit.dXReal + this.fOffsetSX;
					sy = this.bodyOrbit.dYReal + this.fOffsetSY;
				}
			}
			if (this.Ship != null && this.Ship.objSS != null)
			{
				ShipSitu objSS = this.Ship.objSS;
				if ((objSS.bBOLocked || objSS.bIsBO) && objSS.strBOPORShip != null)
				{
					BodyOrbit bo = CrewSim.system.GetBO(objSS.strBOPORShip);
					if (bo != null)
					{
						if (this.fTargetFuture > 0.0)
						{
							bo.UpdateTime(StarSystem.fEpoch + this.fTargetFuture, true, true);
							sx = bo.dXReal + objSS.vBOOffsetx + this.fOffsetSX;
							sy = bo.dYReal + objSS.vBOOffsety + this.fOffsetSY;
							bo.UpdateTime(StarSystem.fEpoch, true, true);
						}
						else
						{
							sx = bo.dXReal + objSS.vBOOffsetx + this.fOffsetSX;
							sy = bo.dYReal + objSS.vBOOffsety + this.fOffsetSY;
						}
					}
					return new Point(sx, sy);
				}
				if (this.fTargetFuture > 0.0)
				{
					Point predictedPosition = objSS.GetPredictedPosition((float)this.fTargetFuture);
					sx = predictedPosition.X + this.fOffsetSX;
					sy = predictedPosition.Y + this.fOffsetSY;
				}
				else
				{
					sx = objSS.vPosx + this.fOffsetSX;
					sy = objSS.vPosy + this.fOffsetSY;
				}
			}
			return new Point(sx, sy);
		}

		public void GetVSXY(out double svx, out double svy)
		{
			svx = 0.0;
			svy = 0.0;
			if (this.bodyOrbit != null)
			{
				svx = this.bodyOrbit.dVelX;
				svy = this.bodyOrbit.dVelY;
			}
			if (this.Ship != null && this.Ship.objSS != null)
			{
				svx = this.Ship.objSS.vVelX;
				svy = this.Ship.objSS.vVelY;
			}
		}

		public void GetParentSXY(double t, out double sx, out double sy)
		{
			sx = 0.0;
			sy = 0.0;
			if (this.bodyOrbit != null)
			{
				BodyOrbit boParent = this.bodyOrbit.boParent;
				if (boParent != null)
				{
					boParent.UpdateTime(t, true, true);
					sx = boParent.dXReal;
					sy = boParent.dYReal;
				}
			}
		}

		public void UpdateTime(double t)
		{
			if (this.bodyOrbit != null)
			{
				this.bodyOrbit.UpdateTime(t, true, true);
			}
			if (this.Ship != null && this.Ship.objSS != null)
			{
				this.Ship.objSS.UpdateTime(t, false);
			}
		}

		public double GetDistance(ShipSitu ss)
		{
			double xau;
			double yau;
			this.GetSXY(out xau, out yau);
			return ss.GetDistance(xau, yau);
		}

		private double sx;

		private double sy;

		public double fOffsetSX;

		public double fOffsetSY;

		public BodyOrbit bodyOrbit;

		public double fTargetFuture;

		private Ship _ship;

		public ShipInfo TargetShipInfo;
	}
}
