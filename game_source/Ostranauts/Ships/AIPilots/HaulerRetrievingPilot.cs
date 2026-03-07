using System;
using System.Collections.Generic;
using System.Linq;
using Ostranauts.Ships.AIPilots.Interfaces;
using Ostranauts.Ships.Commands;
using Ostranauts.Ships.Comms;
using UnityEngine;

namespace Ostranauts.Ships.AIPilots
{
	public class HaulerRetrievingPilot : IAICharacter
	{
		public HaulerRetrievingPilot(string strStationID = null, Ship ship = null)
		{
			this.ShipUs = (ship ?? this.CreateAIShip(strStationID));
			if (this.ShipUs == null)
			{
				return;
			}
			this.Commands = new List<ICommand>
			{
				new Undock(this),
				new FlyTo(this),
				new Dock(this),
				new HaulShip(this),
				new DockCargoAndDespawn(this)
			};
		}

		public Ship ShipUs { get; private set; }

		public List<ICommand> Commands { get; set; }

		public AIType AIType
		{
			get
			{
				return AIType.HaulerRetriever;
			}
		}

		public double MaxSpeed(bool? inAtmo = null)
		{
			if (this.ShipUs.IsDocked())
			{
				return AIShip.CalculateMaxSpeed(this.ShipUs, null);
			}
			bool flag = (inAtmo == null) ? this.ShipUs.InAtmo : inAtmo.Value;
			return (!flag) ? 5.013440183831985E-09 : 2.5067200919159927E-09;
		}

		public ICommand FFWD(ICommand lastActiveCommand)
		{
			if (lastActiveCommand is FlyTo || lastActiveCommand is Dock || lastActiveCommand is Undock)
			{
				if (this.ShipUs.objSS.HasNavData())
				{
					return lastActiveCommand;
				}
				ICommand command = this.Commands.First((ICommand x) => x is Dock);
				CommandCode commandCode = command.ResolveInstantly();
				if ((commandCode & CommandCode.ResultDone) == commandCode)
				{
					return this.Commands.First((ICommand x) => x is HaulShip);
				}
			}
			else if (lastActiveCommand is HaulShip && this.ShipUs.shipSituTarget != null)
			{
				if (this.ShipUs.shipSituTarget == null)
				{
					TargetData target = this.GetTarget();
					if (target != null)
					{
						this.ShipUs.shipSituTarget = target.Situ;
						this.ShipUs.shipScanTarget = target.Ship;
					}
				}
				this.ShipUs.shipSituTarget.UpdateTime(StarSystem.fEpoch, true);
				ICommand command2 = this.Commands.First((ICommand x) => x is DockCargoAndDespawn);
				command2.ResolveInstantly();
				return command2;
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
			if (allDockedShips.Count > 0)
			{
				if (allDockedShips.First<Ship>().IsDerelict())
				{
					Ship shipByRegID = CrewSim.system.GetShipByRegID(this.ShipUs.origin);
					if (shipByRegID != null)
					{
						return new TargetData(shipByRegID);
					}
				}
				return new TargetData(AIShipManager.ShipATCLast);
			}
			Ship ship = CrewSim.system.GetNearestStationRegional(this.ShipUs.objSS.vPosx, this.ShipUs.objSS.vPosy);
			if (ship == null)
			{
				ship = AIShipManager.ShipATCLast;
			}
			List<ShipMessage> messages = this.ShipUs.Comms.GetMessages(ship.strRegID);
			IOrderedEnumerable<ShipMessage> orderedEnumerable = from x in messages
			orderby x.AvailableTime descending
			select x;
			foreach (ShipMessage shipMessage in orderedEnumerable)
			{
				if (StarSystem.fEpoch - shipMessage.AvailableTime > 120.0)
				{
					break;
				}
				if (shipMessage.Interaction.obj3rd != null)
				{
					return new TargetData(shipMessage.Interaction.obj3rd.ship);
				}
			}
			return new TargetData(this.FindDerelictTarget(ship.strRegID));
		}

		private Ship FindDerelictTarget(string stationID)
		{
			string shipOwner = CrewSim.system.GetShipOwner(stationID);
			List<string> shipsForOwner = CrewSim.system.GetShipsForOwner(shipOwner);
			if (shipsForOwner == null || shipsForOwner.Count <= 1)
			{
				return null;
			}
			MathUtils.ShuffleList<string>(shipsForOwner);
			List<string> shipsInPlots = PlotManager.GetShipsInPlots();
			List<Ship> list = new List<Ship>();
			foreach (string text in shipsForOwner)
			{
				Ship shipByRegID = CrewSim.system.GetShipByRegID(text);
				if (shipByRegID != null && !shipByRegID.HideFromSystem && shipByRegID.DMGStatus == Ship.Damage.Derelict && shipByRegID.LoadState <= Ship.Loaded.Shallow && !shipsInPlots.Contains(text))
				{
					if (!shipByRegID.ShipCO.HasCond("IsTutorialDerelict"))
					{
						if (list.Count == 0)
						{
							list.Add(shipByRegID);
						}
						int num = 5;
						if (shipByRegID.fLastVisit < 0.0)
						{
							num++;
						}
						else if (StarSystem.fEpoch - shipByRegID.fLastVisit > 10000.0)
						{
							num += 2;
						}
						else if (shipByRegID.fLastVisit == 0.0)
						{
							num -= 2;
						}
						else
						{
							num--;
						}
						if (UnityEngine.Random.Range(0, 10) < num)
						{
							list.Add(shipByRegID);
						}
					}
				}
			}
			MathUtils.ShuffleList<Ship>(list);
			return list.FirstOrDefault<Ship>();
		}
	}
}
