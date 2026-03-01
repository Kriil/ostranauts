using System;
using System.Collections.Generic;
using Ostranauts.Ships.AIPilots.Interfaces;
using Ostranauts.Utils.Models;
using UnityEngine;

namespace Ostranauts.Ships.Commands
{
	public class Undock : BaseCommand
	{
		public Undock(IAICharacter pilot)
		{
			base.ShipUs = pilot.ShipUs;
			this._ai = pilot;
		}

		public override string DescriptionFriendly
		{
			get
			{
				return "Undocking";
			}
		}

		public override CommandCode RunCommand()
		{
			if (base.ShipUs.HideFromSystem)
			{
				base.ShipUs.HideFromSystem = false;
			}
			if (base.ShipUs.IsDockedFull())
			{
				List<Ship> allDockedShipsFull = base.ShipUs.GetAllDockedShipsFull();
				foreach (Ship ship in allDockedShipsFull)
				{
					if (ship != null && !ship.bDestroyed)
					{
						base.ShipUs.bTowBraceSecured = false;
						base.ShipUs.shipUndock = ship;
						base.ShipUs.fAIDockingExpire = 10.0;
						if (base.ShipUs.LoadState > Ship.Loaded.Shallow)
						{
							if (!base.ShipUs.NavAIManned || base.ShipUs.NavPlayerManned)
							{
								return CommandCode.Ongoing;
							}
							CrewSim.UndockShip(ship, base.ShipUs, true, false);
							base.ShipUs.objSS.Pushback(base.ShipUs, ship);
						}
						else
						{
							base.ShipUs.Undock(ship);
							base.ShipUs.objSS.Pushback(base.ShipUs, ship);
						}
					}
				}
				return CommandCode.Ongoing;
			}
			if (AIShipManager.ShipATCLast != null && MathUtils.GetDistance(base.ShipUs.objSS, AIShipManager.ShipATCLast.objSS) < 6.68458710606501E-08)
			{
				AIShipManager.PrioritizeShip(base.ShipUs);
				double num = base.ShipUs.objSS.vPosx - AIShipManager.ShipATCLast.objSS.vPosx;
				double num2 = base.ShipUs.objSS.vPosy - AIShipManager.ShipATCLast.objSS.vPosy;
				MathUtils.SetLength(ref num, ref num2, 1.0026880659097515E-07);
				Vector2 vector = default(Vector2);
				BodyOrbit bodyOrbit = null;
				CrewSim.system.GetGreatestGravBO(base.ShipUs.objSS, StarSystem.fEpoch, ref vector, ref bodyOrbit);
				double distance = (double)base.ShipUs.objSS.GetRadiusAU() + base.ShipUs.objSS.GetDistance(bodyOrbit.dXReal, bodyOrbit.dYReal);
				JsonAtmosphere atmosphereAtDistance = bodyOrbit.GetAtmosphereAtDistance(distance);
				bool value = atmosphereAtDistance.GetTotalKPA() > BodyOrbit.AtmoKPaThreshold;
				AIShipManager.AIIntercept2(base.ShipUs, AIShipManager.ShipATCLast.objSS.vPosx + num, AIShipManager.ShipATCLast.objSS.vPosy + num2, AIShipManager.ShipATCLast.objSS.vVelX, AIShipManager.ShipATCLast.objSS.vVelY, 0.0, AIShipManager.ShipATCLast.objSS, this._ai.MaxSpeed(new bool?(value)), 0.0);
				return CommandCode.Ongoing;
			}
			if (base.ShipUs.fAIDockingExpire > 0.0 && base.ShipUs.shipUndock != null)
			{
				if (AIShipManager.ShowDebugLogs)
				{
					Debug.Log(base.ShipUs.strRegID + " undock pushback. T-" + base.ShipUs.fAIDockingExpire);
				}
				AIShipManager.PrioritizeShip(base.ShipUs);
				if (base.ShipUs.shipUndock.bDestroyed || base.ShipUs.shipUndock.objSS == null)
				{
					base.ShipUs.shipUndock = CrewSim.system.GetShipByRegID(base.ShipUs.shipUndock.strRegID);
					if (base.ShipUs.shipUndock == null || base.ShipUs.shipUndock.objSS == null || base.ShipUs.shipUndock.bDestroyed)
					{
						base.ShipUs.shipUndock = null;
						return CommandCode.Ongoing;
					}
				}
				Point point = new Point(base.ShipUs.objSS.vPosx - base.ShipUs.shipUndock.objSS.vPosx, base.ShipUs.objSS.vPosy - base.ShipUs.shipUndock.objSS.vPosy);
				double num3 = MathUtils.GetMagnitude(point.X, point.Y);
				float num4 = CollisionManager.GetCollisionDistanceAU(base.ShipUs, base.ShipUs.shipUndock) * 3f;
				num3 = Math.Max(num3 * 2.0, (double)num4);
				point = MathUtils.SetLength(point, num3);
				point += new Point(base.ShipUs.shipUndock.objSS.vPosx, base.ShipUs.shipUndock.objSS.vPosy);
				AIShipManager.AIIntercept2(base.ShipUs, point.X, point.Y, base.ShipUs.shipUndock.objSS.vVelX, base.ShipUs.shipUndock.objSS.vVelY, 0.0, base.ShipUs.shipUndock.objSS, this._ai.MaxSpeed(null), 0.0);
				base.ShipUs.fAIDockingExpire -= (double)CrewSim.TimeElapsedScaled();
				return CommandCode.Ongoing;
			}
			if (AIShipManager.ShowDebugLogs && base.ShipUs.shipUndock != null && !base.ShipUs.shipUndock.bDestroyed)
			{
				double rangeTo = base.ShipUs.GetRangeTo(base.ShipUs.shipUndock);
				float num5 = CollisionManager.GetCollisionDistanceAU(base.ShipUs, base.ShipUs.shipUndock) * 3f;
				Debug.Log(string.Concat(new object[]
				{
					"#AI# ",
					base.ShipUs.strRegID,
					" undock finished with timer: ",
					base.ShipUs.fAIDockingExpire,
					" range: ",
					rangeTo,
					" vs undockDist: ",
					num5
				}));
			}
			this.Cleanup();
			return CommandCode.Finished;
		}

		public override CommandCode ResolveInstantly()
		{
			this.Cleanup();
			return CommandCode.Finished;
		}

		private void Cleanup()
		{
			base.ShipUs.fAIDockingExpire = double.NegativeInfinity;
			base.ShipUs.shipSituTarget = null;
			base.ShipUs.shipScanTarget = null;
			foreach (CondOwner condOwner in base.ShipUs.GetPeople(false))
			{
				condOwner.SetCondAmount("IsPledgeUndockDone", 1.0, 0.0);
			}
		}

		private IAICharacter _ai;
	}
}
