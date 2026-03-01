using System;
using System.Collections.Generic;
using System.Linq;
using Ostranauts.Ships.AIPilots.Interfaces;
using Ostranauts.Utils.Models;
using UnityEngine;

namespace Ostranauts.Ships.Commands
{
	public class HaulShip : BaseCommand
	{
		public HaulShip(IAICharacter pilot)
		{
			this._ai = pilot;
			base.ShipUs = pilot.ShipUs;
		}

		public override string DescriptionFriendly
		{
			get
			{
				List<Ship> allDockedShips = base.ShipUs.GetAllDockedShips();
				string str = (allDockedShips.Count < 1) ? string.Empty : allDockedShips.First<Ship>().strRegID;
				return "Hauling " + str + " to " + ((base.ShipUs.shipScanTarget == null) ? (" a position around " + AIShipManager.strATCLast) : base.ShipUs.shipScanTarget.strRegID);
			}
		}

		public override CommandCode RunCommand()
		{
			if (base.ShipUs.shipSituTarget == null && base.ShipUs.shipScanTarget == null)
			{
				TargetData target = this._ai.GetTarget();
				base.ShipUs.shipScanTarget = target.Ship;
				base.ShipUs.shipSituTarget = target.Situ;
			}
			if (base.ShipUs.shipScanTarget != null && base.ShipUs.shipScanTarget.bDestroyed)
			{
				base.ShipUs.shipScanTarget = CrewSim.system.GetShipByRegID(base.ShipUs.shipScanTarget.strRegID);
			}
			base.ShipUs.objSS.bIgnoreGrav = false;
			List<Ship> allDockedShips = base.ShipUs.GetAllDockedShips();
			if ((base.ShipUs.shipScanTarget == null && base.ShipUs.shipSituTarget == null) || allDockedShips.Count == 0)
			{
				this._lastUpdate = 0.0;
				return CommandCode.Skipped;
			}
			double num = 0.0;
			foreach (Ship ship in allDockedShips)
			{
				num += (double)(2f * ship.objSS.GetRadiusAU());
				if (ship.objSS.bBOLocked)
				{
					ship.objSS.bBOLocked = false;
				}
			}
			if (base.ShipUs.fAIPauseTimer > StarSystem.fEpoch)
			{
				base.ShipUs.objSS.bIgnoreGrav = true;
				this._lastUpdate = 0.0;
				base.ShipUs.Maneuver(0f, 0f, 0f, 0, 1E-10f, Ship.EngineMode.RCS);
				return CommandCode.Ongoing;
			}
			base.ShipUs.bTowBraceSecured = true;
			if (base.ShipUs.shipScanTarget != null && !base.ShipUs.shipScanTarget.CanBeDockedWith() && !base.ShipUs.shipScanTarget.IsStation(false))
			{
				this._lastUpdate = 0.0;
				return CommandCode.Cancelled;
			}
			if (base.ShipUs.DeltaVRemainingRCS <= 0.0)
			{
				AIShipManager.UnregisterShip(base.ShipUs);
				base.ShipUs.strDebugInfo = base.ShipUs.strRegID + ": DeltaVRemaining <= 0";
				if (AIShipManager.ShowDebugLogs)
				{
					Debug.Log("#AI# " + base.ShipUs.strDebugInfo);
				}
				this._lastUpdate = 0.0;
				return CommandCode.Cancelled;
			}
			ShipSitu shipSitu = (base.ShipUs.shipScanTarget == null) ? base.ShipUs.shipSituTarget : base.ShipUs.shipScanTarget.objSS;
			shipSitu.UpdateTime(StarSystem.fEpoch, false);
			double vPosx = shipSitu.vPosx;
			double vPosy = shipSitu.vPosy;
			double vVelX = shipSitu.vVelX;
			double vVelY = shipSitu.vVelY;
			double fTrim = num + (double)CollisionManager.GetCollisionDistanceAU(base.ShipUs.objSS, shipSitu);
			if (base.ShipUs.shipScanTarget == null)
			{
				fTrim = (double)(2f * base.ShipUs.objSS.GetRadiusAU());
			}
			BodyOrbit nearestBO = CrewSim.system.GetNearestBO(base.ShipUs.objSS, StarSystem.fEpoch, false);
			bool dockingNavPoint = FlightCPU.GetDockingNavPoint(base.ShipUs.objSS, nearestBO, ref vPosx, ref vPosy);
			double dX = vVelX - base.ShipUs.objSS.vVelX;
			double dY = vVelY - base.ShipUs.objSS.vVelY;
			double magnitude = MathUtils.GetMagnitude(dX, dY);
			double num2 = magnitude * 149597872.0 * 1000.0;
			float num3 = (float)base.ShipUs.objSS.GetRangeTo(shipSitu);
			if ((double)num3 < Math.Min(1.0026880659097515E-07, 3.342293553032505E-08 + num) && num2 <= 100.0)
			{
				base.ShipUs.Maneuver(0f, 0f, 0f, 0, 1E-10f, Ship.EngineMode.RCS);
				if (base.ShipUs.shipScanTarget != null)
				{
					base.ShipUs.objSS.vVelX = shipSitu.vVelX;
					base.ShipUs.objSS.vVelY = shipSitu.vVelY;
				}
				this._lastUpdate = 0.0;
				return CommandCode.Finished;
			}
			if (!dockingNavPoint)
			{
				fTrim = 0.0;
			}
			Point point;
			if (AIShipManager.IsOnCollisionCourse(base.ShipUs, out point, 30f))
			{
				base.ShipUs.Comms.AIAnnounceEvasion();
				AIShipManager.PrioritizeShip(base.ShipUs);
				AIShipManager.AIIntercept2(base.ShipUs, point.X, point.Y, base.ShipUs.objSS.vVelX, base.ShipUs.objSS.vVelY, 0.0, null, 0.0, 0.0);
				return CommandCode.Ongoing;
			}
			double distance = (double)base.ShipUs.objSS.GetRadiusAU() + base.ShipUs.objSS.GetDistance(nearestBO.dXReal, nearestBO.dYReal);
			JsonAtmosphere atmosphereAtDistance = nearestBO.GetAtmosphereAtDistance(distance);
			bool value = atmosphereAtDistance.GetTotalKPA() > BodyOrbit.AtmoKPaThreshold;
			double num4 = (this._lastUpdate <= 0.0) ? 0.0 : (StarSystem.fEpoch - this._lastUpdate);
			if (num4 > 20.0)
			{
				num4 = 0.0;
			}
			this._lastUpdate = StarSystem.fEpoch;
			AIShipManager.AIIntercept2(base.ShipUs, vPosx, vPosy, vVelX, vVelY, fTrim, base.ShipUs.shipSituTarget, this._ai.MaxSpeed(new bool?(value)), num4);
			return CommandCode.Ongoing;
		}

		public override string[] SaveData
		{
			get
			{
				if (base.ShipUs == null || base.ShipUs.bDestroyed)
				{
					return null;
				}
				List<Ship> allDockedShips = base.ShipUs.GetAllDockedShips();
				string[] result;
				if (allDockedShips.Count > 0)
				{
					(result = new string[1])[0] = allDockedShips.First<Ship>().strRegID;
				}
				else
				{
					result = null;
				}
				return result;
			}
			set
			{
				if (value == null || value.Length == 0)
				{
					return;
				}
				string text = value.FirstOrDefault<string>();
				if (string.IsNullOrEmpty(text))
				{
					return;
				}
				Ship shipByRegID = CrewSim.system.GetShipByRegID(text);
				if (base.ShipUs.IsDockedWith(shipByRegID))
				{
					return;
				}
				if (base.ShipUs.GetRangeTo(shipByRegID) < 3.342293553032505E-08)
				{
					base.ShipUs.Dock(shipByRegID, false);
				}
			}
		}

		private readonly IAICharacter _ai;

		private double _lastUpdate;
	}
}
