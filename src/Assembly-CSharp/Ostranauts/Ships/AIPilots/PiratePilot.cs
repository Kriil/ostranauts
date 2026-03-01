using System;
using System.Collections.Generic;
using System.Linq;
using Ostranauts.Ships.AIPilots.Interfaces;
using Ostranauts.Ships.Commands;

namespace Ostranauts.Ships.AIPilots
{
	public class PiratePilot : IAICharacter
	{
		public PiratePilot(string strStationID = null, Ship ship = null)
		{
			this.ShipUs = (ship ?? this.CreateAIShip(strStationID));
			if (this.ShipUs == null)
			{
				return;
			}
			this.Commands = new List<ICommand>
			{
				new Undock(this),
				new Lurk(this),
				new FlyTo(this),
				new Dock(this),
				new PirateShakeDown(this)
			};
		}

		public Ship ShipUs { get; private set; }

		public List<ICommand> Commands { get; set; }

		public AIType AIType
		{
			get
			{
				return AIType.Pirate;
			}
		}

		public double MaxSpeed(bool? inAtmo = false)
		{
			double num = AIShip.CalculateMaxSpeed(this.ShipUs, inAtmo);
			return num * 1.2;
		}

		public ICommand FFWD(ICommand lastActiveCommand)
		{
			if (lastActiveCommand is FlyTo || lastActiveCommand is Dock)
			{
				if (this.ShipUs.objSS.HasNavData())
				{
					return lastActiveCommand;
				}
				if (this.ShipUs.shipScanTarget == null || this.ShipUs.shipSituTarget != null)
				{
					ICommand command = this.Commands.First((ICommand x) => x is FlyTo);
					command.ResolveInstantly();
					return this.Commands.First((ICommand x) => x is Lurk);
				}
			}
			if (!(lastActiveCommand is Undock) && !(lastActiveCommand is PirateShakeDown))
			{
				return lastActiveCommand;
			}
			CommandCode commandCode = lastActiveCommand.ResolveInstantly();
			if (commandCode == CommandCode.Ongoing)
			{
				return lastActiveCommand;
			}
			if (lastActiveCommand is PirateShakeDown)
			{
				ICommand command2 = this.Commands.First((ICommand x) => x is Undock);
				command2.ResolveInstantly();
			}
			TargetData target = this.GetTarget();
			if (target.Ship == null)
			{
				ICommand command3 = this.Commands.First((ICommand x) => x is FlyTo);
				command3.ResolveInstantly();
				return this.Commands.First((ICommand x) => x is Lurk);
			}
			ICommand command4 = this.Commands.First((ICommand x) => x is Dock);
			CommandCode commandCode2 = command4.ResolveInstantly();
			return command4;
		}

		public TargetData GetTarget()
		{
			if (AIShipManager.LowFuel(this.ShipUs, this.MaxSpeed(new bool?(false))))
			{
				Ship nearestStationRegional = CrewSim.system.GetNearestStationRegional(this.ShipUs.objSS.vPosx, this.ShipUs.objSS.vPosy);
				if (nearestStationRegional != null)
				{
					return new TargetData(nearestStationRegional);
				}
			}
			ShipSitu shipSitu = new ShipSitu();
			BodyOrbit nearestBO = CrewSim.system.GetNearestBO(this.ShipUs.objSS, StarSystem.fEpoch, false);
			if (nearestBO == null)
			{
				return null;
			}
			nearestBO.UpdateTime(StarSystem.fEpoch, true, true);
			double num = MathUtils.Rand(0.0, 1.0, MathUtils.RandType.Low, null);
			num = nearestBO.fRadius * 149597872.0 + 150.0 + num * 310.0;
			CrewSim.system.SetSituToRandomSafeCoords(shipSitu, num / 149597872.0, (num + 30.0) / 149597872.0, nearestBO.dXReal, nearestBO.dYReal, MathUtils.RandType.Low);
			shipSitu.LockToBO(nearestBO, -1.0);
			return new TargetData(shipSitu);
		}

		private Ship CreateAIShip(string strStationID)
		{
			Ship shipByRegID = CrewSim.system.GetShipByRegID(strStationID);
			if (!CrewSim.GetSelectedCrew().ship.objSS.bBOLocked && !CrewSim.GetSelectedCrew().ship.objSS.bIsBO && AIShip.IsPlayerClose(shipByRegID))
			{
				return null;
			}
			string text = strStationID + "Pirates";
			Ship ship = AIShip.FindShipForFaction(text);
			if (ship == null)
			{
				ship = AIShip.SpawnNewShip(text, "RandomScavShip", Ship.Damage.Used, null, shipByRegID);
				if (ship == null)
				{
					return null;
				}
				ship.origin = strStationID;
			}
			AIShip.ResetShipState(ship);
			shipByRegID.objSS.TimeAdvance(0.0, false);
			ship.objSS.vVelX = shipByRegID.objSS.vVelX;
			ship.objSS.vVelY = shipByRegID.objSS.vVelY;
			ship.objSS.PlaceOrbitPosition(shipByRegID.objSS);
			AIShip.RandomizeSpawnPosition(ship, shipByRegID.objSS.strBOPORShip);
			CondOwner condOwner = AIShip.AddCrew("OKLGShipAIPirate", ship, "OKLG_UNK", false);
			AIShipManager.NPCReport();
			string strName = text + shipByRegID.strRegID;
			JsonCompany jsonCompany = CrewSim.system.GetCompany(strName);
			if (jsonCompany == null)
			{
				jsonCompany = new JsonCompany();
				jsonCompany.strName = strName;
				jsonCompany.strRegID = ship.strRegID;
			}
			condOwner.Company = jsonCompany;
			jsonCompany.mapRoster[condOwner.strID] = new JsonCompanyRules();
			condOwner.Company.SetPermissionAirlock(condOwner.strID, true);
			condOwner.Company.SetPermissionShore(condOwner.strID, true);
			condOwner.Company.SetPermissionRestore(condOwner.strID, true);
			int nUTCHour = StarSystem.nUTCHour;
			jsonCompany.mapRoster[condOwner.strID].StartWorkdayAt(nUTCHour);
			condOwner.ShiftChange(jsonCompany.GetShift(nUTCHour, condOwner), true);
			ship.ToggleVis(false, true);
			ship.AIRefuel();
			return ship;
		}
	}
}
