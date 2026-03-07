using System;
using System.Linq;
using Ostranauts.Ships.AIPilots.Interfaces;
using Ostranauts.Utils.Models;
using UnityEngine;

namespace Ostranauts.Ships.Commands
{
	public class FlyTo : BaseCommand
	{
		public FlyTo(IAICharacter pilot)
		{
			this._ai = pilot;
			base.ShipUs = pilot.ShipUs;
			this._flyToManual = new FlyToManual(pilot);
			this._flyToPath = new FlyToPath(pilot);
		}

		public override string DescriptionFriendly
		{
			get
			{
				return (base.ShipUs.shipScanTarget == null || base.ShipUs.fAIPauseTimer > StarSystem.fEpoch) ? "Calculating target coordinates" : ("Flying to " + base.ShipUs.shipScanTarget.strRegID);
			}
		}

		public override string[] SaveData
		{
			get
			{
				return this._flyToPath.SaveData;
			}
			set
			{
				if (value == null || value.Length == 0)
				{
					return;
				}
				this._flyToPath.SaveData = value;
			}
		}

		public override CommandCode ResolveInstantly()
		{
			if (base.ShipUs.shipSituTarget == null)
			{
				return CommandCode.Cancelled;
			}
			base.ShipUs.shipSituTarget.UpdateTime(StarSystem.fEpoch, false);
			if (base.ShipUs.shipScanTarget != null && base.ShipUs.IsAIShip && CrewSim.coPlayer.ship != null && base.ShipUs.shipScanTarget.strRegID == CrewSim.coPlayer.ship.strRegID)
			{
				base.ShipUs.shipScanTarget = null;
				base.ShipUs.shipSituTarget = null;
				return CommandCode.Skipped;
			}
			base.PlaceWithinDockingRange(base.ShipUs.shipSituTarget);
			base.ShipUs.objSS.ResetNavData();
			return CommandCode.Finished;
		}

		public override CommandCode RunCommand()
		{
			if (base.ShipUs.shipScanTarget != null && base.ShipUs.shipSituTarget != null)
			{
				base.ShipUs.shipSituTarget = base.ShipUs.shipScanTarget.objSS;
			}
			else if (base.ShipUs.shipScanTarget == null && base.ShipUs.shipSituTarget == null)
			{
				TargetData target = this._ai.GetTarget();
				if (target != null)
				{
					base.ShipUs.shipScanTarget = target.Ship;
					base.ShipUs.shipSituTarget = target.Situ;
					this.fuelLvlAtTheEnd = base.ShipUs.GetRCSRemain() - base.ShipUs.CalculateRCSFuelConsumption(AIShipManager.GetDeltaVNeededToTargetFullTrip(base.ShipUs, base.ShipUs.shipSituTarget, this._ai.MaxSpeed(null)));
				}
			}
			base.ShipUs.objSS.bIgnoreGrav = false;
			if (base.ShipUs.IsLocalAuthority && AIShipManager.ShowDebugLogs)
			{
				Debug.Log(string.Concat(new object[]
				{
					"#AI# ",
					base.ShipUs.strRegID,
					" RCSRemass: ",
					base.ShipUs.fShallowRCSRemass
				}));
			}
			if (base.ShipUs.objSS.HasNavData())
			{
				this.fuelLvlAtTheEnd = base.ShipUs.objSS.NavData.GetArrivalRCSFuel();
			}
			bool flag = base.ShipUs.shipSituTarget != null && base.ShipUs.objSS.GetDistance(base.ShipUs.shipSituTarget) <= 3.342293553032505E-08;
			if (base.ShipUs.fAIPauseTimer > StarSystem.fEpoch || (base.ShipUs.IsDocked() && !base.ShipUs.TowBraceSecured()))
			{
				base.ShipUs.objSS.ResetNavData();
				base.ShipUs.objSS.bIgnoreGrav = true;
				base.ShipUs.Maneuver(0f, 0f, 0f, 0, 1E-10f, Ship.EngineMode.RCS);
				return CommandCode.Ongoing;
			}
			if (!flag && !base.ShipUs.objSS.HasNavData() && this.IsOutOfFuelApproximation())
			{
				this.RequestHelp();
				base.ShipUs.objSS.ResetNavData();
				base.ShipUs.Maneuver(0f, 0f, 0f, 0, 1E-10f, Ship.EngineMode.RCS);
				base.ShipUs.fAIPauseTimer = StarSystem.fEpoch + 64.0;
				return CommandCode.Skipped;
			}
			if (base.ShipUs.objSS.bBOLocked)
			{
				return CommandCode.Skipped;
			}
			bool flag2 = this.HasMinDistanceForPathing();
			if (flag2 && this.HasValidPathTarget() && !AIShipManager.IsOnCollisionCourse(base.ShipUs, out this._p, 30f))
			{
				return this._flyToPath.RunCommand();
			}
			base.ShipUs.objSS.ResetNavData();
			return this._flyToManual.RunCommand();
		}

		private bool IsOutOfFuelApproximation()
		{
			ShipSitu shipSitu = base.ShipUs.shipSituTarget;
			if (shipSitu == null && base.ShipUs.shipScanTarget != null)
			{
				shipSitu = base.ShipUs.shipScanTarget.objSS;
			}
			if (shipSitu == null)
			{
				return false;
			}
			double deltaVRemainingRCS = base.ShipUs.DeltaVRemainingRCS;
			double rcsremain = base.ShipUs.GetRCSRemain();
			if (this.fuelLvlAtTheEnd < 0.0 || (this.fuelLvlAtTheEnd > 0.0 && rcsremain <= 1.0))
			{
				return true;
			}
			double deltaVNeededToTargetFullTrip = AIShipManager.GetDeltaVNeededToTargetFullTrip(base.ShipUs, shipSitu, this._ai.MaxSpeed(null));
			if (deltaVNeededToTargetFullTrip < deltaVRemainingRCS)
			{
				return false;
			}
			if ((double.IsNaN(deltaVRemainingRCS) || deltaVRemainingRCS < deltaVNeededToTargetFullTrip) && base.ShipUs.LiftRotorsThrustStrength > 0f)
			{
				return false;
			}
			bool flag = FlyToPath.IsInterregionalPath(base.ShipUs.objSS, shipSitu);
			if (flag && base.ShipUs.bFusionReactorRunning)
			{
				return false;
			}
			double magnitude = (shipSitu.vVel - base.ShipUs.objSS.vVel).magnitude;
			return deltaVRemainingRCS <= magnitude;
		}

		private void RequestHelp()
		{
			Ship ship = CrewSim.coPlayer.ship.GetAllDockedShips().FirstOrDefault<Ship>();
			bool flag = ship != null && ship.IsStation(false);
			if (this._ai.AIType == AIType.HaulerRetriever || this._ai.AIType == AIType.HaulerDeployer || this._ai.AIType == AIType.Pirate)
			{
				AIShipManager.UnregisterShip(base.ShipUs);
			}
			if (this._ai.AIType == AIType.Scav && !flag && base.ShipUs.GetRangeTo(CrewSim.coPlayer.ship) * 149597872.0 < (double)this.GetRequestDistance())
			{
				base.ShipUs.Comms.SendMessage("SHIPSosAskPlayer", CrewSim.coPlayer.ship.strRegID, null);
				BeatManager.ResetTensionTimer();
			}
			else if (this._ai.AIType != AIType.HaulerRetriever && this._ai.AIType != AIType.HaulerDeployer)
			{
				if (base.ShipUs.shipSituTarget != null && base.ShipUs.objSS.GetRangeTo(base.ShipUs.shipSituTarget) < 3.342293553032505E-08)
				{
					return;
				}
				Ship nearestStationRegional = CrewSim.system.GetNearestStationRegional(base.ShipUs.objSS.vPosx, base.ShipUs.objSS.vPosy);
				string targetRegID = (nearestStationRegional == null) ? AIShipManager.strATCLast : nearestStationRegional.strRegID;
				base.ShipUs.Comms.SendMessage("SHIPSosStranded", targetRegID, null);
				AIShipManager.UnregisterShip(base.ShipUs);
			}
			if (AIShipManager.ShowDebugLogs)
			{
				Debug.Log("#AI# " + base.ShipUs.strRegID + ": DeltaVRemaining <= 0");
			}
		}

		private int GetRequestDistance()
		{
			JsonPersonSpec personSpec = DataHandler.GetPersonSpec("RELClose");
			CondOwner condOwner;
			base.ShipUs.Comms.GetCaptain(out condOwner);
			if (condOwner == null || condOwner.pspec == null)
			{
				return 120;
			}
			return (!CrewSim.coPlayer.pspec.IsCOMyMother(personSpec, condOwner)) ? 40 : 120;
		}

		private bool HasValidPathTarget()
		{
			return base.ShipUs.shipSituTarget != null && (base.ShipUs.shipSituTarget.bIsBO || base.ShipUs.shipSituTarget.bBOLocked);
		}

		private bool HasMinDistanceForPathing()
		{
			if (base.ShipUs.shipSituTarget != null && base.ShipUs.objSS.NavData == null)
			{
				double rangeTo = base.ShipUs.objSS.GetRangeTo(base.ShipUs.shipSituTarget);
				return rangeTo > 3.342293553032505E-08;
			}
			return true;
		}

		private readonly IAICharacter _ai;

		private ICommand _flyToManual;

		private ICommand _flyToPath;

		private Point _p;

		private double fuelLvlAtTheEnd;
	}
}
