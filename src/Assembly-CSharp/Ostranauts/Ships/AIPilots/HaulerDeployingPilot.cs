using System;
using System.Collections.Generic;
using System.Linq;
using Ostranauts.Ships.AIPilots.Interfaces;
using Ostranauts.Ships.Commands;

namespace Ostranauts.Ships.AIPilots
{
	public class HaulerDeployingPilot : IAICharacter
	{
		public HaulerDeployingPilot(string strStationID = null, Ship ship = null)
		{
			this.ShipUs = (ship ?? this.CreateAIShip(strStationID));
			if (this.ShipUs == null)
			{
				return;
			}
			if (ship == null)
			{
				this.SpawnDerelict(this.ShipUs, strStationID);
			}
			this.Commands = new List<ICommand>
			{
				new Dock(this),
				new HaulShip(this),
				new AnchorDockedShip(this),
				new FlyTo(this)
			};
			this.ShipUs.shipScanTarget = null;
			this.ShipUs.shipSituTarget = null;
		}

		public Ship ShipUs { get; private set; }

		public List<ICommand> Commands { get; set; }

		public AIType AIType
		{
			get
			{
				return AIType.HaulerDeployer;
			}
		}

		public double MaxSpeed(bool? inAtmo = null)
		{
			if (this.ShipUs.IsDocked())
			{
				return AIShip.CalculateMaxSpeed(this.ShipUs, inAtmo);
			}
			bool flag = (inAtmo == null) ? this.ShipUs.InAtmo : inAtmo.Value;
			return (!flag) ? 5.013440183831985E-09 : 2.5067200919159927E-09;
		}

		public ICommand FFWD(ICommand lastActiveCommand)
		{
			if (lastActiveCommand is FlyTo)
			{
				if (this.ShipUs.objSS.HasNavData())
				{
					return lastActiveCommand;
				}
				ICommand command = this.Commands.First((ICommand x) => x is Dock);
				CommandCode commandCode = command.ResolveInstantly();
				if ((commandCode & CommandCode.ResultDone) == commandCode)
				{
					TargetData target = this.GetTarget();
					if (target != null)
					{
						this.ShipUs.shipSituTarget = target.Situ;
						this.ShipUs.shipScanTarget = target.Ship;
					}
					return this.Commands.First((ICommand x) => x is FlyTo);
				}
			}
			else
			{
				if (lastActiveCommand is HaulShip && this.ShipUs.shipSituTarget != null)
				{
					ICommand command2 = this.Commands.First((ICommand x) => x is AnchorDockedShip);
					command2.ResolveInstantly();
					return command2;
				}
				lastActiveCommand.ResolveInstantly();
			}
			return lastActiveCommand;
		}

		private Ship CreateAIShip(string strStationID)
		{
			Ship shipByRegID = CrewSim.system.GetShipByRegID(strStationID);
			if (!CrewSim.GetSelectedCrew().ship.objSS.bBOLocked && !CrewSim.GetSelectedCrew().ship.objSS.bIsBO && AIShip.IsPlayerClose(shipByRegID))
			{
				return null;
			}
			string faction = strStationID + "Hauler";
			Ship ship = AIShip.FindShipForFaction(faction);
			if (ship == null)
			{
				ship = AIShip.SpawnNewShip(faction, "RandomHaulerShip", Ship.Damage.Used, null, shipByRegID);
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
			List<Ship> allDockedShips = this.ShipUs.GetAllDockedShips();
			if (allDockedShips.Count <= 0)
			{
				AIShip aishipByRegID = AIShipManager.GetAIShipByRegID(this.ShipUs.strRegID);
				if (aishipByRegID != null && aishipByRegID.HomeStation != null)
				{
					Ship shipByRegID = CrewSim.system.GetShipByRegID(aishipByRegID.HomeStation);
					if (shipByRegID != null)
					{
						return new TargetData(shipByRegID);
					}
				}
				return new TargetData(AIShipManager.ShipATCLast);
			}
			if (allDockedShips.First<Ship>().IsDerelict())
			{
				return new TargetData(this.CreateDerelictDestination());
			}
			Ship shipByRegID2 = CrewSim.system.GetShipByRegID(this.ShipUs.origin);
			if (shipByRegID2 != null)
			{
				return new TargetData(shipByRegID2);
			}
			return new TargetData(AIShipManager.ShipATCLast);
		}

		private void SpawnDerelict(Ship shipUs, string stationID)
		{
			string shipOwner = CrewSim.system.GetShipOwner(stationID);
			Ship ship = CrewSim.system.AddDerelict("RandomShip", shipOwner);
			if (ship == null)
			{
				ship = CrewSim.system.AddDerelict("RandomShip", shipOwner);
				if (ship == null)
				{
					return;
				}
			}
			ship.objSS.PlaceOrbitPosition(shipUs.objSS);
			ship.objSS.UnlockFromBO();
			ship.Dock(shipUs, false);
			ship.objSS.ssDockedHeavier = this.ShipUs.objSS;
			shipUs.objSS.ssDockedHeavier = null;
			ship.shipScanTarget = shipUs;
			shipUs.bTowBraceSecured = true;
			ship.bXPDRAntenna = false;
			ship.strXPDR = null;
			ship.objSS.bIsNoFees = true;
			ship.fBreakInMultiplier = (float)MathUtils.Rand(0.1, 0.99, MathUtils.RandType.Flat, null);
		}

		private ShipSitu CreateDerelictDestination()
		{
			ShipSitu shipSitu = new ShipSitu();
			BodyOrbit nearestBO = CrewSim.system.GetNearestBO(this.ShipUs.objSS, StarSystem.fEpoch, true);
			if (nearestBO == null)
			{
				return null;
			}
			nearestBO.UpdateTime(StarSystem.fEpoch, true, true);
			double num = MathUtils.Rand(0.0, 1.0, MathUtils.RandType.Low, null);
			num = nearestBO.fRadius * 149597872.0 + 90.0 + num * 310.0;
			CrewSim.system.SetSituToRandomSafeCoords(shipSitu, num / 149597872.0, (num + 30.0) / 149597872.0, nearestBO.dXReal, nearestBO.dYReal, MathUtils.RandType.Low);
			shipSitu.LockToBO(nearestBO, -1.0);
			return shipSitu;
		}
	}
}
