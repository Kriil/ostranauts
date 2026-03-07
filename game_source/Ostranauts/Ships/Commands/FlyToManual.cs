using System;
using Ostranauts.Ships.AIPilots.Interfaces;
using Ostranauts.Utils.Models;
using UnityEngine;

namespace Ostranauts.Ships.Commands
{
	public class FlyToManual : BaseCommand
	{
		public FlyToManual(IAICharacter pilot)
		{
			this._ai = pilot;
			base.ShipUs = pilot.ShipUs;
		}

		public override string DescriptionFriendly
		{
			get
			{
				return (base.ShipUs.shipScanTarget == null || base.ShipUs.fAIPauseTimer > StarSystem.fEpoch) ? "Calculating target coordinates" : ("Flying to " + base.ShipUs.shipScanTarget.strRegID);
			}
		}

		public override CommandCode RunCommand()
		{
			if (base.ShipUs.shipScanTarget == null && base.ShipUs.shipSituTarget == null)
			{
				TargetData target = this._ai.GetTarget();
				if (target != null)
				{
					base.ShipUs.shipScanTarget = target.Ship;
					base.ShipUs.shipSituTarget = target.Situ;
				}
			}
			if (base.ShipUs.shipScanTarget != null && base.ShipUs.shipScanTarget.bDestroyed)
			{
				base.ShipUs.shipScanTarget = CrewSim.system.GetShipByRegID(base.ShipUs.shipScanTarget.strRegID);
				if (base.ShipUs.shipScanTarget == null)
				{
					base.ShipUs.shipSituTarget = null;
				}
				return CommandCode.Skipped;
			}
			if (base.ShipUs.shipSituTarget == null && (base.ShipUs.shipScanTarget == null || base.ShipUs.shipScanTarget.bDestroyed))
			{
				base.ShipUs.shipScanTarget = null;
				return CommandCode.Skipped;
			}
			if (base.ShipUs.shipSituTarget != null)
			{
				base.ShipUs.shipSituTarget.UpdateTime(StarSystem.fEpoch, false);
			}
			if (base.ShipUs.fAIPauseTimer > StarSystem.fEpoch)
			{
				if (AIShipManager.ShowDebugLogs)
				{
					Debug.Log("#AI# " + base.ShipUs.strRegID + ": FlyTo-PauseTimer active!");
				}
				base.ShipUs.Maneuver(0f, 0f, 0f, 0, 1E-10f, Ship.EngineMode.RCS);
				return CommandCode.Ongoing;
			}
			ShipSitu shipSitu = (base.ShipUs.shipScanTarget == null) ? base.ShipUs.shipSituTarget : base.ShipUs.shipScanTarget.objSS;
			double vPosx = shipSitu.vPosx;
			double vPosy = shipSitu.vPosy;
			float num = CollisionManager.GetCollisionDistanceAU(base.ShipUs.objSS, shipSitu);
			float num2 = base.GetHandOffDistance(num);
			bool flag = base.ShipUs.shipScanTarget != null && base.ShipUs.shipScanTarget.IsDocked() && !base.ShipUs.shipScanTarget.IsStation(false);
			if (flag)
			{
				num = Dock.TargetDockedHandoffDistance;
				num2 = Dock.TargetDockedHandoffDistance;
			}
			BodyOrbit nearestBO = CrewSim.system.GetNearestBO(base.ShipUs.objSS, StarSystem.fEpoch, false);
			bool dockingNavPoint = FlightCPU.GetDockingNavPoint(base.ShipUs.objSS, nearestBO, ref vPosx, ref vPosy);
			double dX = shipSitu.vVelX - base.ShipUs.objSS.vVelX;
			double dY = shipSitu.vVelY - base.ShipUs.objSS.vVelY;
			double magnitude = MathUtils.GetMagnitude(dX, dY);
			double num3 = magnitude * 149597872.0 * 1000.0;
			double rangeTo = base.ShipUs.objSS.GetRangeTo(shipSitu);
			if (rangeTo < 1.0026880659097515E-07)
			{
				AIShipManager.PrioritizeShip(base.ShipUs);
			}
			if (rangeTo < (double)num2 && num3 < (double)((!flag) ? 100 : 20))
			{
				base.ShipUs.Maneuver(0f, 0f, 0f, 0, 1E-10f, Ship.EngineMode.RCS);
				return CommandCode.Finished;
			}
			if (!dockingNavPoint)
			{
				num = 0f;
			}
			double distance = (double)base.ShipUs.objSS.GetRadiusAU() + base.ShipUs.objSS.GetDistance(nearestBO.dXReal, nearestBO.dYReal);
			JsonAtmosphere atmosphereAtDistance = nearestBO.GetAtmosphereAtDistance(distance);
			bool value = atmosphereAtDistance.GetTotalKPA() > BodyOrbit.AtmoKPaThreshold;
			Point point;
			if (AIShipManager.IsOnCollisionCourse(base.ShipUs, out point, 30f))
			{
				if (AIShipManager.ShowDebugLogs)
				{
					Debug.Log("#AI# " + base.ShipUs.strRegID + " Evading!");
				}
				base.ShipUs.Comms.AIAnnounceEvasion();
				AIShipManager.PrioritizeShip(base.ShipUs);
				AIShipManager.AIIntercept2(base.ShipUs, point.X, point.Y, base.ShipUs.objSS.vVelX, base.ShipUs.objSS.vVelY, this._ai.MaxSpeed(new bool?(value)) * 2.0, null, 0.0, 0.0);
				return CommandCode.Ongoing;
			}
			AIShipManager.AIIntercept2(base.ShipUs, vPosx, vPosy, shipSitu.vVelX, shipSitu.vVelY, (double)num, shipSitu, this._ai.MaxSpeed(new bool?(value)), 0.0);
			return CommandCode.Ongoing;
		}

		private readonly IAICharacter _ai;
	}
}
