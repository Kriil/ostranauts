using System;
using System.Collections.Generic;
using System.Linq;
using Ostranauts.Ships.AIPilots.Interfaces;
using Ostranauts.Ships.Commands;

namespace Ostranauts.Ships.AIPilots
{
	public class HaulerCargoPilot : IAICharacter
	{
		public HaulerCargoPilot(string strStationID, Ship ship, string shipLoot)
		{
			this.ShipUs = (ship ?? this.CreateAIShip(strStationID, shipLoot));
			if (this.ShipUs == null)
			{
				return;
			}
			this._homeStation = strStationID;
			this.Commands = new List<ICommand>
			{
				new FlyTo(this),
				new DockCargoAndDespawn(this)
			};
		}

		public Ship ShipUs { get; private set; }

		public List<ICommand> Commands { get; set; }

		public AIType AIType
		{
			get
			{
				return AIType.HaulerCargo;
			}
		}

		public double MaxSpeed(bool? inAtmo = null)
		{
			return AIShip.CalculateMaxSpeed(this.ShipUs, null);
		}

		public ICommand FFWD(ICommand lastActiveCommand)
		{
			if (lastActiveCommand is FlyTo || lastActiveCommand is DockCargoAndDespawn)
			{
				if (this.ShipUs.objSS.HasNavData())
				{
					return lastActiveCommand;
				}
				ICommand command = this.Commands.First((ICommand x) => x is DockCargoAndDespawn);
				CommandCode commandCode = command.ResolveInstantly();
				if ((commandCode & CommandCode.ResultDone) == commandCode)
				{
					return this.Commands.First((ICommand x) => x is DockCargoAndDespawn);
				}
			}
			return lastActiveCommand;
		}

		private Ship CreateAIShip(string strStationID, string shipLoot)
		{
			Ship shipByRegID = CrewSim.system.GetShipByRegID(strStationID);
			if (!CrewSim.GetSelectedCrew().ship.objSS.bBOLocked && !CrewSim.GetSelectedCrew().ship.objSS.bIsBO && AIShip.IsPlayerClose(shipByRegID))
			{
				return null;
			}
			string faction = strStationID + "CargoHauler";
			Ship ship = AIShip.FindShipForFaction(faction);
			if (ship == null)
			{
				ship = AIShip.SpawnNewShip(faction, shipLoot, Ship.Damage.Used, null, shipByRegID);
				if (ship == null)
				{
					return null;
				}
				ship.origin = strStationID;
				ship.publicName = string.Concat(new object[]
				{
					strStationID,
					" Hauler ",
					MathUtils.Rand(1, 10, MathUtils.RandType.Flat, null),
					MathUtils.Rand(0, 10, MathUtils.RandType.Flat, null)
				});
			}
			AIShip.ResetShipState(ship);
			shipByRegID.objSS.TimeAdvance(0.0, false);
			ship.objSS.vVelX = shipByRegID.objSS.vVelX;
			ship.objSS.vVelY = shipByRegID.objSS.vVelY;
			ship.objSS.PlaceOrbitPosition(shipByRegID.objSS);
			AIShip.RandomizeSpawnPosition(ship, shipByRegID.objSS.strBOPORShip);
			AIShip.AddCrew("OKLGScavCrew", ship, strStationID, true);
			AIShipManager.NPCReport();
			ship.ToggleVis(false, true);
			ship.AIRefuel();
			return ship;
		}

		public TargetData GetTarget()
		{
			if (this.ShipUs.shipScanTarget != null)
			{
				return new TargetData(this.ShipUs.shipScanTarget);
			}
			if (string.IsNullOrEmpty(this._homeStation))
			{
				this._homeStation = CrewSim.system.GetNearestStationRegional(this.ShipUs.objSS.vPosx, this.ShipUs.objSS.vPosy).strRegID;
			}
			Ship shipByRegID = CrewSim.system.GetShipByRegID(this._homeStation);
			if (AIShipManager.BingoFuelCheck(this.ShipUs, shipByRegID, this.MaxSpeed(null)))
			{
				AIShipManager.UnregisterShip(this.ShipUs);
			}
			return new TargetData(shipByRegID);
		}

		private string _homeStation;
	}
}
