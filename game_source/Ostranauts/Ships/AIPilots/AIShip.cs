using System;
using System.Collections.Generic;
using System.Linq;
using Ostranauts.Core.Models;
using Ostranauts.Ships.AIPilots.Interfaces;
using Ostranauts.Ships.Commands;
using Ostranauts.Utils.Models;
using UnityEngine;

namespace Ostranauts.Ships.AIPilots
{
	public class AIShip
	{
		public AIShip(string stationID, AIType at, Ship ship = null)
		{
			if (string.IsNullOrEmpty(stationID))
			{
				return;
			}
			this.HomeStation = stationID;
			this.SetupAI(at, ship, null);
		}

		public AIShip(JsonAIShipSave json, Ship ship)
		{
			if (string.IsNullOrEmpty(json.strHomeStation) || ship == null)
			{
				return;
			}
			this.HomeStation = json.strHomeStation;
			this.SetupAI(json.enumAIType, ship, null);
		}

		public AIShip(string stationID, AIType at, string shipLoot)
		{
			this.HomeStation = stationID;
			this.SetupAI(at, null, shipLoot);
		}

		public Ship Ship
		{
			get
			{
				if (this._shipUs != null && this._shipUs.bDestroyed)
				{
					this._shipUs = CrewSim.system.GetShipByRegID(this._shipUs.strRegID);
				}
				return this._shipUs;
			}
			private set
			{
				this._shipUs = value;
			}
		}

		public string HomeStation { get; private set; }

		public IAICharacter AICharacter
		{
			get
			{
				return this._aiCharacter;
			}
		}

		public AIType AIType
		{
			get
			{
				return this._aiCharacter.AIType;
			}
		}

		public string ActiveCommandName
		{
			get
			{
				return (this._activeCommand != null) ? this._activeCommand.GetType().Name : string.Empty;
			}
		}

		public string ActiveCommandNameDescription
		{
			get
			{
				return (this._activeCommand != null) ? this._activeCommand.DescriptionFriendly : string.Empty;
			}
		}

		public string[] ActiveCommandSaveData
		{
			get
			{
				return (this._activeCommand != null) ? this._activeCommand.SaveData : null;
			}
		}

		private void SetupAI(AIType at, Ship ship, string shipLoot = null)
		{
			switch (at)
			{
			case AIType.Police:
				this._aiCharacter = new PolicePilot(this.HomeStation, ship);
				break;
			case AIType.Scav:
				this._aiCharacter = new ScavPilot(this.HomeStation, ship);
				break;
			default:
				if (at != AIType.Station)
				{
					if (at != AIType.Pirate)
					{
						if (at != AIType.HaulerCargo)
						{
							if (at != AIType.Auto)
							{
								return;
							}
							this._aiCharacter = new AutoPilot(this.HomeStation, ship);
						}
						else
						{
							this._aiCharacter = new HaulerCargoPilot(this.HomeStation, ship, shipLoot);
						}
					}
					else
					{
						this._aiCharacter = new PiratePilot(this.HomeStation, ship);
					}
				}
				else
				{
					this._aiCharacter = new StationPilot(ship);
				}
				break;
			case AIType.HaulerDeployer:
				this._aiCharacter = new HaulerDeployingPilot(this.HomeStation, ship);
				break;
			case AIType.HaulerRetriever:
				this._aiCharacter = new HaulerRetrievingPilot(this.HomeStation, ship);
				break;
			}
			this.Ship = this._aiCharacter.ShipUs;
			if (this.Ship == null)
			{
				return;
			}
			this._activeCommand = this._aiCharacter.Commands.FirstOrDefault<ICommand>();
			this._commands = this._aiCharacter.Commands;
		}

		private void NextCommand()
		{
			int num = -1;
			for (int i = 0; i < this._commands.Count; i++)
			{
				if (this._commands[i] == this._activeCommand || (num == -1 && this._commands[i].GetType().Name == this.ActiveCommandName))
				{
					num = i;
				}
			}
			if (num == -1 || num == this._commands.Count - 1)
			{
				this._activeCommand = this._commands.FirstOrDefault<ICommand>();
			}
			if (num + 1 < this._commands.Count)
			{
				this._activeCommand = this._commands[num + 1];
			}
			if (AIShipManager.ShowDebugLogs)
			{
				Debug.Log("#AI# " + this.Ship.strRegID + " Switching to Command: " + this._activeCommand.GetType().Name);
			}
		}

		public void RunAI()
		{
			CommandCode commandCode = this._activeCommand.RunCommand();
			if (AIShipManager.ShowDebugLogs && commandCode != CommandCode.Ongoing)
			{
				Debug.Log(string.Concat(new object[]
				{
					"#AI# ",
					this.Ship.strRegID,
					" Command: ",
					this._activeCommand.GetType().Name,
					" result: ",
					commandCode
				}));
			}
			if ((commandCode & CommandCode.ResultDone) == commandCode)
			{
				this.NextCommand();
			}
		}

		public void FFWD()
		{
			this._activeCommand = this._aiCharacter.FFWD(this._activeCommand);
		}

		public void AddCommandLoot(string commandName, string[] commandSaveData = null)
		{
			if (string.IsNullOrEmpty(commandName))
			{
				return;
			}
			object[] args = new object[]
			{
				this._aiCharacter
			};
			Type command = DataHandler.GetCommand(commandName);
			if (command == null)
			{
				return;
			}
			object obj = Activator.CreateInstance(command, args);
			this._activeCommand = (ICommand)obj;
			if (commandSaveData != null)
			{
				this._activeCommand.SaveData = commandSaveData;
			}
		}

		public static bool IsPlayerClose(Ship station)
		{
			double distance = station.objSS.GetDistance(CrewSim.GetSelectedCrew().ship.objSS);
			if (distance <= AIShipManager.fPoliceIgnoreRange)
			{
				Debug.Log("AI Spawn Aborted: Player too close to station.");
				return true;
			}
			return false;
		}

		public static bool IsDockingAreaClear(Ship station, double range)
		{
			List<AIShip> shipsOfTypeForRegion = AIShipManager.GetShipsOfTypeForRegion(AIType.All);
			foreach (AIShip aiship in shipsOfTypeForRegion)
			{
				if (aiship != null && aiship.Ship != null && !aiship.Ship.HideFromSystem && !aiship.Ship.objSS.bIsBO)
				{
					if (aiship.Ship.GetRangeTo(station) < range)
					{
						return false;
					}
				}
			}
			return true;
		}

		public static void ResetShipState(Ship ship)
		{
			ship.fAIPauseTimer = 0.0;
			ship.fAIDockingExpire = double.NegativeInfinity;
			ship.HideFromSystem = false;
			ship.IsAIShip = true;
			ship.strXPDR = ship.strRegID;
			ship.bXPDRAntenna = true;
			ship.objSS.bIsNoFees = true;
			ship.objSS.bIgnoreGrav = false;
		}

		public static Ship FindShipForFaction(string faction)
		{
			foreach (string strRegID in CrewSim.system.GetShipsForOwner(faction))
			{
				Ship shipByRegID = CrewSim.system.GetShipByRegID(strRegID);
				if (shipByRegID != null && !shipByRegID.objSS.bIsBO && shipByRegID.HideFromSystem)
				{
					return shipByRegID;
				}
			}
			return null;
		}

		public static Ship SpawnNewShip(string faction, string shipLoot, Ship.Damage damage, string regId = null, Ship spawnStation = null)
		{
			if (spawnStation != null)
			{
				if (DataHandler.dictLoot.ContainsKey(shipLoot + spawnStation.strRegID))
				{
					shipLoot += spawnStation.strRegID;
				}
				else
				{
					Ship nearestStationRegional = CrewSim.system.GetNearestStationRegional(spawnStation.objSS.vPosx, spawnStation.objSS.vPosy);
					if (nearestStationRegional != null && DataHandler.dictLoot.ContainsKey(shipLoot + nearestStationRegional.strRegID))
					{
						shipLoot += nearestStationRegional.strRegID;
					}
				}
			}
			Loot loot = DataHandler.GetLoot(shipLoot);
			List<string> lootNames = loot.GetLootNames(null, false, null);
			if (lootNames.Count > 0)
			{
				return CrewSim.system.SpawnShip(lootNames[0], regId ?? Ship.GenerateID("O"), Ship.Loaded.Shallow, damage, faction, 100, false);
			}
			Debug.Log("Unable to spawn new AI ship!");
			return null;
		}

		public static CondOwner AddCrew(string pspec, Ship ship, string stationId, bool bIncludeRegionals)
		{
			JsonPersonSpec personSpec = DataHandler.GetPersonSpec(pspec);
			List<string> list = null;
			if (bIncludeRegionals)
			{
				list = AIShipManager.GetAllATCRecycleStations(stationId);
			}
			if (list == null || list.Count == 0)
			{
				list = new List<string>
				{
					stationId
				};
			}
			PersonSpec personSpec2 = StarSystem.GetPerson(personSpec, null, false, AIShip.GetPilotForbids(list), null);
			bool flag = AIShip.NotEnoughNPCsOnStation(stationId);
			bool flag2 = false;
			if (personSpec2 == null || flag)
			{
				flag2 = true;
				personSpec2 = new PersonSpec(personSpec, false);
				Debug.Log(string.Concat(new object[]
				{
					"#NPC# Creating new NPC ",
					personSpec2.FullName,
					"; NewRegion: ",
					AIShipManager.NewRegion,
					"; ps: ",
					personSpec2,
					"; bNotEnough: ",
					flag
				}));
			}
			else
			{
				Debug.Log("#NPC# Reusing NPC " + personSpec2.FullName);
			}
			CondOwner condOwner = personSpec2.MakeCondOwner(PersonSpec.StartShip.OLD, null);
			string originRegId = stationId;
			if (condOwner.ship != ship)
			{
				if (condOwner.ship != null)
				{
					originRegId = condOwner.ship.strRegID;
				}
				CrewSim.MoveCO(condOwner, ship, false);
			}
			condOwner.LogMove(originRegId, ship.strRegID, (!flag2) ? MoveReason.ADDCREW : MoveReason.ADDNEWCREW, null);
			condOwner.ClaimShip(ship.strRegID);
			return condOwner;
		}

		public static void ClearShipTarget(Ship ship)
		{
			ship.shipScanTarget = null;
			ship.shipSituTarget = null;
		}

		private static bool NotEnoughNPCsOnStation(string stationId)
		{
			Ship shipByRegID = CrewSim.system.GetShipByRegID(stationId);
			if (shipByRegID == null)
			{
				return true;
			}
			if (stationId.IndexOf("_UNK") == stationId.Length - 4)
			{
				return false;
			}
			List<CondOwner> people = shipByRegID.GetPeople(false);
			return shipByRegID.Mass > 10000.0 && people.Count <= 3;
		}

		private static List<string> GetPilotForbids(List<string> aLimitToRegIDs = null)
		{
			List<string> list = new List<string>();
			Dictionary<string, Ship>.ValueCollection values = CrewSim.system.dictShips.Values;
			foreach (Ship ship in values)
			{
				if (ship != null && !ship.bDestroyed)
				{
					if (ship.LoadState < Ship.Loaded.Edit)
					{
						if (aLimitToRegIDs == null)
						{
							if (ship.objSS.bIsBO)
							{
								continue;
							}
						}
						else if (aLimitToRegIDs.IndexOf(ship.strRegID) >= 0)
						{
							continue;
						}
					}
					List<CondOwner> people = ship.GetPeople(false);
					foreach (CondOwner condOwner in people)
					{
						list.Add(condOwner.strName);
					}
				}
			}
			if (CrewSim.coPlayer != null && CrewSim.coPlayer.Company != null && CrewSim.coPlayer.Company.mapRoster != null)
			{
				list.AddRange(CrewSim.coPlayer.Company.mapRoster.Keys);
			}
			return list;
		}

		public static double CalculateMaxSpeed(Ship shipUs, bool? inAtmo = null)
		{
			double num = 1000.0;
			bool flag = (inAtmo == null) ? shipUs.InAtmo : inAtmo.Value;
			float num2;
			double num3;
			if (flag)
			{
				if (shipUs.LiftRotorsThrustStrength > 0f)
				{
					num2 = (float)((double)shipUs.LiftRotorsThrustStrength / shipUs.Mass) * 100f;
				}
				else
				{
					num2 = (float)shipUs.RCSAccelMax / 6.684587E-12f;
				}
				num3 = 500.0;
			}
			else
			{
				num2 = (float)(shipUs.RCSAccelMax * 2.0) / 6.684587E-12f;
				num3 = 760.0;
				num = 200.0;
			}
			float num4 = (float)(num3 * (Math.Log((double)(num2 + 1f)) / Math.Log((double)num2 + num)));
			float num5 = Math.Max(1.002688E-09f, num4 * 6.684587E-12f);
			return (double)num5;
		}

		public static void RandomizeSpawnPosition(Ship ship, string strBOPORShip)
		{
			BodyOrbit bo = CrewSim.system.GetBO(strBOPORShip);
			if (bo != null && bo.boParent != null)
			{
				Point normalized = (bo.dPosReal - bo.boParent.dPosReal).normalized;
				float num = UnityEngine.Random.Range(2f, 6f) / 149597870f;
				ship.objSS.vPosx += (double)num * normalized.X;
				ship.objSS.vPosy += (double)num * normalized.Y;
			}
		}

		private Ship _shipUs;

		private IAICharacter _aiCharacter;

		private ICommand _activeCommand;

		private List<ICommand> _commands;
	}
}
