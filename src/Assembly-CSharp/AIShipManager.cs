using System;
using System.Collections.Generic;
using System.Linq;
using Ostranauts.Core.Models;
using Ostranauts.ShipGUIs.Utilities;
using Ostranauts.Ships;
using Ostranauts.Ships.AIPilots;
using Ostranauts.Ships.Commands;
using Ostranauts.Tools.ExtensionMethods;
using Ostranauts.Trading;
using Ostranauts.Utils.Models;
using UnityEngine;

// Global manager for non-player ship traffic in the current region.
// Likely updated once per frame from CrewSim to spawn haulers/scavs,
// restore AI pilots from saves, and keep ATC-linked regions populated.
public class AIShipManager
{
	// Set when the active ATC region changes and the manager has rebuilt local traffic.
	public static bool NewRegion { get; private set; }

	// Cached last ATC ship reference.
	// `strATCLast` is the persisted reg id stored in JsonAIShipManagerSave.
	public static Ship ShipATCLast
	{
		get
		{
			if ((AIShipManager._shipATCLast == null || AIShipManager._shipATCLast.bDestroyed) && !string.IsNullOrEmpty(AIShipManager.strATCLast))
			{
				AIShipManager._shipATCLast = CrewSim.system.GetShipByRegID(AIShipManager.strATCLast);
			}
			return AIShipManager._shipATCLast;
		}
	}

	// Resets runtime state, rebuilds region lookup tables, then reapplies saved scheduling data.
	public static void Init(JsonAIShipManagerSave ss = null)
	{
		AIShipManager.fPoliceIgnoreRange = 6.68458710606501E-08;
		AIShipManager.fTimeOfNextTransit = 0.0;
		AIShipManager.fTimeOfNextScav = 0.0;
		AIShipManager.fTimeOfNextHauler = 0.0;
		AIShipManager.nScavSpawnsRemaining = 0;
		AIShipManager.strATCLast = null;
		AIShipManager._shipATCLast = null;
		AIShipManager.dictAIs = new Dictionary<string, List<AIShip>>();
		AIShipManager.dictSpawnRegIDs = new Dictionary<string, Loot>();
		AIShipManager.dictPilotSrcRegIDs = new Dictionary<string, List<string>>();
		AIShipManager.dictFerries = new Dictionary<string, JsonFerryInfo>();
		AIShipManager.BuildRegionalStationDict();
		AIShipManager.ApplySaveData(ss);
	}

	// Rehydrates timers, active AI pilots, and ferry records from the save payload.
	private static void ApplySaveData(JsonAIShipManagerSave ss)
	{
		if (ss == null)
		{
			return;
		}
		AIShipManager.strATCLast = ss.strATCLast;
		AIShipManager.fTimeOfNextTransit = ss.fTimeOfNextTransit;
		AIShipManager.fTimeOfNextScav = ss.fTimeOfNextScav;
		AIShipManager.fTimeOfNextHauler = ss.fTimeOfNextHauler;
		AIShipManager.nScavSpawnsRemaining = ss.nScavSpawnsRemaining;
		AIShipManager.fTimeOfNextPassShip = ss.fTimeOfNextPassShip;
		AIShipManager.fTimeOfNextTradeCheck = ss.fTimeOfNextTradeCheck;
		AIShipManager.SetATCValues(AIShipManager.ShipATCLast);
		if (ss.aAIShips != null)
		{
			foreach (JsonAIShipSave jsonAIShipSave in ss.aAIShips)
			{
				if (jsonAIShipSave != null && !string.IsNullOrEmpty(jsonAIShipSave.strATCLast))
				{
					Ship shipByRegID = CrewSim.system.GetShipByRegID(jsonAIShipSave.strRegId);
					AIShipManager.AddAIToShip(shipByRegID, jsonAIShipSave.enumAIType, jsonAIShipSave.strATCLast, jsonAIShipSave);
				}
			}
		}
		if (ss.aFIs != null)
		{
			foreach (JsonFerryInfo jsonFerryInfo in ss.aFIs)
			{
				AIShipManager.dictFerries[jsonFerryInfo.strFerryCOID] = jsonFerryInfo.Clone();
			}
		}
	}

	// Serializes one live AI pilot back into the compact save DTO.
	public static JsonAIShipSave GetJSONAIShipSave(string regId)
	{
		foreach (KeyValuePair<string, List<AIShip>> keyValuePair in AIShipManager.dictAIs)
		{
			if (keyValuePair.Value != null)
			{
				foreach (AIShip aiship in keyValuePair.Value)
				{
					if (aiship != null && aiship.Ship != null && !(aiship.Ship.strRegID != regId))
					{
						return new JsonAIShipSave
						{
							strATCLast = keyValuePair.Key,
							strRegId = aiship.Ship.strRegID,
							strHomeStation = aiship.HomeStation,
							enumAIType = aiship.AIType,
							strActiveCommand = aiship.ActiveCommandName,
							strActiveCommandPayload = aiship.ActiveCommandSaveData
						};
					}
				}
			}
		}
		return null;
	}

	// Captures the entire AI traffic manager state for save/load.
	public static JsonAIShipManagerSave GetJSONSave()
	{
		JsonAIShipManagerSave jsonAIShipManagerSave = new JsonAIShipManagerSave();
		jsonAIShipManagerSave.strATCLast = AIShipManager.strATCLast;
		jsonAIShipManagerSave.fTimeOfNextTransit = AIShipManager.fTimeOfNextTransit;
		jsonAIShipManagerSave.fTimeOfNextScav = AIShipManager.fTimeOfNextScav;
		jsonAIShipManagerSave.nScavSpawnsRemaining = AIShipManager.nScavSpawnsRemaining;
		jsonAIShipManagerSave.fTimeOfNextPassShip = AIShipManager.fTimeOfNextPassShip;
		jsonAIShipManagerSave.fTimeOfNextTradeCheck = AIShipManager.fTimeOfNextTradeCheck;
		List<JsonAIShipSave> list = new List<JsonAIShipSave>();
		foreach (KeyValuePair<string, List<AIShip>> keyValuePair in AIShipManager.dictAIs)
		{
			foreach (AIShip aiship in keyValuePair.Value)
			{
				JsonAIShipSave item = new JsonAIShipSave
				{
					strATCLast = keyValuePair.Key,
					strRegId = aiship.Ship.strRegID,
					strHomeStation = aiship.HomeStation,
					enumAIType = aiship.AIType,
					strActiveCommand = aiship.ActiveCommandName,
					strActiveCommandPayload = aiship.ActiveCommandSaveData
				};
				list.Add(item);
			}
		}
		jsonAIShipManagerSave.aAIShips = list.ToArray();
		jsonAIShipManagerSave.aFIs = new JsonFerryInfo[AIShipManager.dictFerries.Count];
		AIShipManager.dictFerries.Values.CopyTo(jsonAIShipManagerSave.aFIs, 0);
		return jsonAIShipManagerSave;
	}

	// Fast-forward hook used when time is advanced in large steps.
	public static void FFWD(double timeDelta)
	{
		if (timeDelta < (double)GUIFFWD.FFWDTIMEUNIT)
		{
			return;
		}
		foreach (List<AIShip> list in AIShipManager.dictAIs.Values)
		{
			foreach (AIShip aiship in list.ToArray())
			{
				if (aiship.Ship != null && !aiship.Ship.bDestroyed)
				{
					aiship.FFWD();
				}
			}
		}
	}

	// Main simulation tick.
	// Likely called from the core gameplay loop to clean up region changes,
	// advance AI command queues, and schedule new ambient traffic.
	public static void Update()
	{
		AIShipManager.NewRegion = false;
		if (CollisionManager.strATCClosest != AIShipManager.strATCLast)
		{
			AIShipManager.RegionCleanup();
		}
		if (AIShipManager._shipATCLast != null && AIShipManager._shipATCLast.bDestroyed)
		{
			AIShipManager._shipATCLast = null;
		}
		if (AIShipManager.strATCLast != null && AIShipManager._shipATCLast == null)
		{
			AIShipManager._shipATCLast = CrewSim.system.GetShipByRegID(AIShipManager.strATCLast);
		}
		AIShipManager.RunAIQueue();
		AIShipManager.CheckTracking();
		if (AIShipManager.strATCLast == null || AIShipManager.IsTestCase)
		{
			return;
		}
		if (AIShipManager.NewRegion)
		{
			int nAmount = MathUtils.Rand(AIShipManager._scavs_Min, AIShipManager._scavs_Max, MathUtils.RandType.Flat, null);
			AIShipManager.SpawnScavs(nAmount, 0);
			AIShipManager.nScavSpawnsRemaining = 0;
		}
		if (StarSystem.fEpoch > AIShipManager.fTimeOfNextScav)
		{
			List<AIShip> shipsOfTypeForRegion = AIShipManager.GetShipsOfTypeForRegion(AIShipManager.strATCLast, AIType.Scav);
			int num = (shipsOfTypeForRegion == null) ? 0 : shipsOfTypeForRegion.Count;
			AIShipManager.nScavSpawnsRemaining = Math.Min(AIShipManager.nScavSpawnsRemaining, AIShipManager._scavs_Max - num);
			if (AIShipManager.nScavSpawnsRemaining < 0)
			{
				AIShipManager.nScavSpawnsRemaining = 0;
			}
			if (AIShipManager.nScavSpawnsRemaining > 0)
			{
				AIShipManager.nScavSpawnsRemaining--;
				AIShipManager.SpawnScavs(1, num);
			}
			else if (num < AIShipManager._scavs_Min)
			{
				AIShipManager.nScavSpawnsRemaining += MathUtils.Rand(1, AIShipManager._scavs_Min, MathUtils.RandType.Flat, null);
			}
			AIShipManager.fTimeOfNextScav = StarSystem.fEpoch + MathUtils.Rand(20.0, 120.0, MathUtils.RandType.Flat, null);
		}
		AIShipManager.SpawnHauler();
		AIShipManager.SpawnPassShip();
		if (StarSystem.fEpoch > AIShipManager.fTimeOfNextTradeCheck)
		{
			AIShipManager.fTimeOfNextTradeCheck = StarSystem.fEpoch + 7200.0;
			AIShipManager.CheckTradeRoutes();
		}
		if (StarSystem.fEpoch > AIShipManager.fTimeOfNextTransit)
		{
			AIShipManager.HandleDeads();
			AIShipManager.HandleTransits();
			AIShipManager.HandleMaint();
			AIShipManager.fTimeOfNextTransit = StarSystem.fEpoch + MathUtils.Rand(20.0, 120.0, MathUtils.RandType.Flat, null);
		}
		AIShipManager.HandleFerries();
	}

	private static void SpawnHauler()
	{
		if (StarSystem.fEpoch < AIShipManager.fTimeOfNextHauler)
		{
			return;
		}
		bool flag = AIShipManager.fTimeOfNextHauler == 0.0;
		AIShipManager.fTimeOfNextHauler = StarSystem.fEpoch + (double)UnityEngine.Random.Range(2000, 3600);
		if (flag)
		{
			return;
		}
		PotentialSpawnStationsDTO haulerSpawnStations = AIShipManager.GetHaulerSpawnStations(AIShipManager.strATCLast);
		List<Ship> list = null;
		if (haulerSpawnStations.DerelictCollectorStations.Count > 0)
		{
			for (int i = haulerSpawnStations.DerelictCollectorStations.Count - 1; i >= 0; i--)
			{
				Tuple<Ship, int> tuple = haulerSpawnStations.DerelictCollectorStations[i];
				int num = 0;
				foreach (Ship ship in CrewSim.system.GetAllLoadedShipsWithin(tuple.Item1.objSS, ScavPilot.MaxTargetRange))
				{
					if (ship != null && !ship.bDestroyed && ship.DMGStatus == Ship.Damage.Derelict)
					{
						num++;
					}
				}
				if (num > tuple.Item2)
				{
					if (list == null)
					{
						list = new List<Ship>();
					}
					list.Add(tuple.Item1);
				}
			}
		}
		List<Ship> list2 = null;
		if (haulerSpawnStations.DerelictSpawnerStations.Count > 0)
		{
			for (int j = haulerSpawnStations.DerelictSpawnerStations.Count - 1; j >= 0; j--)
			{
				Tuple<Ship, int> tuple2 = haulerSpawnStations.DerelictSpawnerStations[j];
				int num2 = 0;
				foreach (Ship ship2 in CrewSim.system.GetAllLoadedShipsWithin(tuple2.Item1.objSS, ScavPilot.MaxTargetRange))
				{
					if (ship2 != null && !ship2.bDestroyed && ship2.DMGStatus == Ship.Damage.Derelict)
					{
						num2++;
					}
				}
				if (num2 < tuple2.Item2)
				{
					if (list2 == null)
					{
						list2 = new List<Ship>();
					}
					list2.Add(tuple2.Item1);
				}
			}
		}
		string stationID = string.Empty;
		AIType aitype;
		if (list != null && list2 != null)
		{
			aitype = ((UnityEngine.Random.Range(0, 10) >= 5) ? AIType.HaulerDeployer : AIType.HaulerRetriever);
			List<Ship> list3 = (aitype != AIType.HaulerRetriever) ? list2 : list;
			stationID = list3[UnityEngine.Random.Range(0, list3.Count)].strRegID;
		}
		else if (list != null && list2 == null)
		{
			aitype = AIType.HaulerRetriever;
			stationID = list[UnityEngine.Random.Range(0, list.Count)].strRegID;
		}
		else
		{
			if (list != null || list2 == null)
			{
				return;
			}
			aitype = AIType.HaulerDeployer;
			stationID = list2[UnityEngine.Random.Range(0, list2.Count)].strRegID;
		}
		AIShip aiship = new AIShip(stationID, aitype, null);
		if (aiship.Ship != null)
		{
			AIShipManager.AddAIToDict(aiship, null);
		}
	}

	private static void SpawnPassShip()
	{
		if (StarSystem.fEpoch < AIShipManager.fTimeOfNextPassShip)
		{
			return;
		}
		bool flag = AIShipManager.fTimeOfNextPassShip == 0.0;
		AIShipManager.fTimeOfNextPassShip = StarSystem.fEpoch + (double)UnityEngine.Random.Range(86400, 129600);
		if (flag)
		{
			return;
		}
		CondTrigger condTrigger = DataHandler.GetCondTrigger("TIsNPCPrunable");
		List<Tuple<double, Ship>> list = new List<Tuple<double, Ship>>();
		List<Ship> list2 = new List<Ship>();
		foreach (Ship ship in CrewSim.system.GetAllLoadedShips())
		{
			if (ship != null && !ship.bDestroyed && ship.IsStation(false) && ship.LoadState < Ship.Loaded.Edit)
			{
				int count = ship.GetPeople(false).Count;
				double maxPopulation = ship.MaxPopulation;
				if (maxPopulation != 0.0)
				{
					if ((double)(count + 4) < maxPopulation)
					{
						list2.Add(ship);
					}
					else if ((double)count > maxPopulation)
					{
						list.Add(new Tuple<double, Ship>((double)count - maxPopulation, ship));
					}
				}
			}
		}
		if (list.Count == 0)
		{
			return;
		}
		list = (from x in list
		orderby x.Item1 descending
		select x).ToList<Tuple<double, Ship>>();
		Ship item = list.First<Tuple<double, Ship>>().Item2;
		List<CondOwner> cos = item.GetCOs(condTrigger, false, false, false);
		if (list2.Count > 4)
		{
			Ship ship2 = list2[UnityEngine.Random.Range(0, list2.Count)];
			AIShip aiship = new AIShip(item.strRegID, AIType.HaulerCargo, "RandomPassShip");
			if (aiship.Ship == null)
			{
				Debug.LogWarning("PASS ship was null, exiting");
				return;
			}
			aiship.Ship.publicName = "PASS Transport";
			aiship.Ship.shipScanTarget = ship2;
			aiship.Ship.shipSituTarget = ship2.objSS;
			if (!AIShipManager.CanReachTarget(aiship.Ship, ship2))
			{
				CrewSim.DockAndDespawn(aiship.Ship, item, null);
				aiship.Ship.Destroy(false);
				return;
			}
			AIShipManager.AddAIToDict(aiship, "INTERREGIONAL");
			aiship.Ship.Comms.SendMessage("SHIPUnDockAI", item.strRegID, null);
			int num = 0;
			string optionalData = (ship2 == null) ? null : ship2.strRegID;
			foreach (CondOwner condOwner in cos)
			{
				if (num >= 4)
				{
					break;
				}
				condOwner.LogMove(condOwner.ship.strRegID, aiship.Ship.strRegID, MoveReason.PASS, optionalData);
				condOwner.UnclaimShip(condOwner.ship.strRegID);
				condOwner.ClaimShip(aiship.Ship.strRegID);
				condOwner.CatchUp();
				CrewSim.MoveCO(condOwner, aiship.Ship, false);
				if (!condOwner.HasQueuedInteraction("WanderSoon"))
				{
					Interaction interaction = DataHandler.GetInteraction("WanderSoon", null, false);
					condOwner.QueueInteraction(condOwner, interaction, false);
				}
				condOwner.ZeroCondAmount("IsEmbarkCommand");
				num++;
			}
			Debug.Log(string.Concat(new object[]
			{
				"#NPC# PASS Shipping ",
				item.strRegID,
				" transporting: ",
				num,
				" npcs to",
				ship2.strRegID
			}));
		}
		else
		{
			double maxPopulation2 = item.MaxPopulation;
			int count2 = item.GetPeople(false).Count;
			int num2 = count2;
			foreach (CondOwner condOwner2 in cos)
			{
				if ((double)num2 < maxPopulation2 / 2.0)
				{
					break;
				}
				condOwner2.RemoveFromCurrentHome(true);
				condOwner2.Destroy();
				num2--;
			}
			Debug.Log(string.Concat(new object[]
			{
				"#NPC# PASS Shipping ",
				item.strRegID,
				" removed: ",
				count2 - num2,
				" npcs"
			}));
		}
	}

	private static void SetATCValues(Ship atc)
	{
		if (atc == null || atc.ShipCO == null)
		{
			return;
		}
		AIShipManager._scavs_Min = (int)AIShipManager._shipATCLast.ShipCO.GetCondAmount("StationMinScav", false);
		AIShipManager._scavs_Max = (int)AIShipManager._shipATCLast.ShipCO.GetCondAmount("StationMaxScav", false);
		AIShipManager._pirates_Min = (int)AIShipManager._shipATCLast.ShipCO.GetCondAmount("StationMinPirate", false);
		AIShipManager._pirates_Max = (int)AIShipManager._shipATCLast.ShipCO.GetCondAmount("StationMaxPirate", false);
	}

	public static void CheckTradeRoutes()
	{
		List<TradeRouteDTO> tradeRoute = MarketManager.GetTradeRoute();
		if (tradeRoute == null || tradeRoute.Count == 0 || tradeRoute.FirstOrDefault<TradeRouteDTO>() == null)
		{
			return;
		}
		string cargoShipForRoute = AIShipManager.GetCargoShipForRoute(tradeRoute);
		if (string.IsNullOrEmpty(cargoShipForRoute))
		{
			return;
		}
		AIShip aiship = new AIShip(tradeRoute[0].OriginStation, AIType.HaulerCargo, cargoShipForRoute);
		if (aiship.Ship != null)
		{
			Ship shipByRegID = CrewSim.system.GetShipByRegID(tradeRoute[0].DestinationStation);
			if (shipByRegID != null)
			{
				aiship.Ship.shipScanTarget = shipByRegID;
				aiship.Ship.shipSituTarget = shipByRegID.objSS;
			}
			if (shipByRegID == null || !AIShipManager.CanReachTarget(aiship.Ship, shipByRegID))
			{
				Ship shipByRegID2 = CrewSim.system.GetShipByRegID(tradeRoute[0].OriginStation);
				CrewSim.DockAndDespawn(aiship.Ship, shipByRegID2, null);
				aiship.Ship.Destroy(false);
				return;
			}
			AIShipManager.AddAIToDict(aiship, "INTERREGIONAL");
			MarketManager.RegisterAICargoHauler(aiship.Ship, tradeRoute);
			aiship.Ship.Comms.SendMessage("SHIPUnDockAI", tradeRoute[0].OriginStation, null);
			if (MarketManager.ShowDebugLogs)
			{
				foreach (TradeRouteDTO tradeRouteDTO in tradeRoute)
				{
					Debug.LogWarning(string.Concat(new object[]
					{
						"#Market# <color=yellow>TRADE ROUTE FOUND! </color>",
						aiship.Ship.strRegID,
						" ",
						tradeRouteDTO.OriginStation,
						" to ",
						tradeRouteDTO.DestinationStation,
						" cargo: ",
						tradeRouteDTO.CoCollection.Name,
						" Amount: ",
						tradeRouteDTO.Amount,
						" Profit: ",
						tradeRouteDTO.RouteValue
					}));
				}
			}
		}
	}

	public static bool CanReachTarget(Ship shipUs, Ship destination)
	{
		FlyToPath flyToPath = new FlyToPath(shipUs, false);
		NavDataPoint navOrigin = flyToPath.CreateNavPointStatic(shipUs.objSS, StarSystem.fEpoch, false, false);
		NavDataPoint navDestination = flyToPath.CreateNavPointStatic(destination.objSS, StarSystem.fEpoch, false, true);
		NavData navData = flyToPath.PlanTrip4(navOrigin, navDestination, 1f, 1f);
		return navData != null;
	}

	public static string GetCargoShipForRoute(List<TradeRouteDTO> tradeRoutes)
	{
		int num = 0;
		foreach (TradeRouteDTO tradeRouteDTO in tradeRoutes)
		{
			num = Mathf.CeilToInt((float)tradeRouteDTO.Amount / ((float)MarketManager.CARGOPOD_DEFAULTMASSCAPACITY / (float)tradeRouteDTO.CoCollection.GetAverageMass()));
		}
		Ship shipByRegID = CrewSim.system.GetShipByRegID(tradeRoutes.First<TradeRouteDTO>().OriginStation);
		if (shipByRegID == null || shipByRegID.objSS == null)
		{
			return null;
		}
		Ship nearestStationRegional = CrewSim.system.GetNearestStationRegional(shipByRegID.objSS.vPosx, shipByRegID.objSS.vPosy);
		if (nearestStationRegional == null)
		{
			return null;
		}
		List<string> allStationsInATCRegion = AIShipManager.GetAllStationsInATCRegion(nearestStationRegional.strRegID);
		string str = (!allStationsInATCRegion.Contains(tradeRoutes.First<TradeRouteDTO>().DestinationStation)) ? "Interregional" : "Regional";
		string result = str + "CargoShip36Pods";
		if (num <= 4)
		{
			result = str + "CargoShip4Pods";
		}
		else if (num <= 8)
		{
			result = str + "CargoShip8Pods";
		}
		else
		{
			result = str + "CargoShip36Pods";
		}
		return result;
	}

	public static AIShip DebugGetRandomShip()
	{
		List<AIShip> list = AIShipManager.dictAIs.Values.FirstOrDefault<List<AIShip>>();
		return (list == null) ? null : list.FirstOrDefault<AIShip>();
	}

	public static AIShip GetAIShipByRegID(string shipId)
	{
		foreach (List<AIShip> list in AIShipManager.dictAIs.Values)
		{
			if (list.Any((AIShip x) => x.Ship.strRegID == shipId))
			{
				return list.Find((AIShip x) => x.Ship.strRegID == shipId);
			}
		}
		return null;
	}

	private static void SpawnScavs(int nAmount, int existingScavs = 0)
	{
		for (int i = 0; i < nAmount; i++)
		{
			string randomStationInATCRegion = AIShipManager.GetRandomStationInATCRegion(AIShipManager.strATCLast);
			Ship shipByRegID = CrewSim.system.GetShipByRegID(randomStationInATCRegion);
			if (shipByRegID == null)
			{
				Debug.LogWarning("Station Not Found: Skipping Scav Spawn at " + randomStationInATCRegion);
			}
			else
			{
				int num = 0;
				foreach (Ship ship in CrewSim.system.GetAllLoadedShipsWithin(shipByRegID.objSS, ScavPilot.MaxTargetRange))
				{
					if (ship != null && ship.IsDerelict() && !ship.bDocked)
					{
						num++;
					}
				}
				if (num > 0 && existingScavs >= num)
				{
					return;
				}
				double num2 = MathUtils.Rand(0.0, 1.0, MathUtils.RandType.Low, null);
				num2 = 70.0 + num2 * num2 * 800.0;
				double dX = shipByRegID.objSS.vPosx;
				double dY = shipByRegID.objSS.vPosy;
				BodyOrbit bo = CrewSim.system.GetBO(shipByRegID.objSS.strBOPORShip);
				if (bo != null)
				{
					dX = bo.dXReal;
					dY = bo.dYReal;
				}
				ShipSitu shipSitu = new ShipSitu();
				CrewSim.system.SetSituToRandomSafeCoords(shipSitu, num2 / 149597872.0, num2 / 149597872.0, dX, dY, MathUtils.RandType.Low);
				AIShip aiship = AIShipManager.SpawnAI(AIType.Scav, shipSitu.vPosx, shipSitu.vPosy, true);
				if (AIShipManager.ShowDebugLogs && aiship != null)
				{
					Debug.Log("#AI# Spawned " + aiship.Ship.strRegID + " around station " + randomStationInATCRegion);
				}
			}
		}
		Debug.Log("#NPC# Spawned " + nAmount + " ScavAI ships.");
	}

	public static AIShip SpawnAI(AIType aiType, double worldX, double worldY, bool excludeOutposts)
	{
		Ship nearestStation = CrewSim.system.GetNearestStation(worldX, worldY, excludeOutposts);
		if (nearestStation == null)
		{
			Debug.LogWarning(aiType + " failed to spawn");
			return null;
		}
		string strRegID = nearestStation.strRegID;
		AIShip aiship = new AIShip(strRegID, aiType, null);
		if (aiship.Ship == null)
		{
			Debug.LogWarning(aiType + " failed to spawn");
			return null;
		}
		aiship.Ship.objSS.vPosx = worldX;
		aiship.Ship.objSS.vPosy = worldY;
		aiship.Ship.objSS.fRot = MathUtils.Rand(0f, 6.2831855f, MathUtils.RandType.Flat, null);
		string strRegID2 = CrewSim.system.GetNearestStationRegional(worldX, worldY).strRegID;
		AIShipManager.AddAIToDict(aiship, strRegID2);
		return aiship;
	}

	public static AIShip SpawnAI(AIType aiType, string atc = null)
	{
		if (atc == null)
		{
			atc = AIShipManager.strATCLast;
		}
		AIShip aiship = new AIShip(atc, aiType, null);
		if (aiship.Ship == null)
		{
			Debug.Log(aiType + " failed to spawn");
			return null;
		}
		AIShipManager.AddAIToDict(aiship, atc);
		return aiship;
	}

	private static void RegionCleanup()
	{
		Debug.Log("Leaving ATC Region: " + AIShipManager.strATCLast);
		if (AIShipManager.strATCLast != null && AIShipManager.dictAIs.ContainsKey(AIShipManager.strATCLast))
		{
			AIShip[] array = AIShipManager.dictAIs[AIShipManager.strATCLast].ToArray();
			Ship shipByRegID = CrewSim.system.GetShipByRegID(AIShipManager.strATCLast);
			foreach (AIShip aiShip in array)
			{
				AIShipManager.AIShipCleanup(aiShip, shipByRegID);
			}
			Debug.Log("Destroyed " + array.Length + " ships.");
			AIShipManager.DerelictCleanup(AIShipManager.strATCLast);
		}
		if (AIShipManager._aIQueue != null)
		{
			AIShipManager._aIQueue.Clear();
		}
		Debug.Log("Entering ATC Region: " + CollisionManager.strATCClosest);
		AIShipManager.strATCLast = CollisionManager.strATCClosest;
		AIShipManager._shipATCLast = CrewSim.system.GetShipByRegID(AIShipManager.strATCLast);
		AIShipManager.SetATCValues(AIShipManager._shipATCLast);
		AIShipManager.NewRegion = true;
	}

	private static void DerelictCleanup(string atcLast)
	{
		string shipOwner = CrewSim.system.GetShipOwner(atcLast);
		List<string> shipsForOwner = CrewSim.system.GetShipsForOwner(shipOwner);
		List<string> shipsInPlots = PlotManager.GetShipsInPlots();
		List<string> shipsForOwner2 = CrewSim.system.GetShipsForOwner(CrewSim.coPlayer.strID);
		int num = 0;
		foreach (string text in shipsForOwner)
		{
			Ship shipByRegID = CrewSim.system.GetShipByRegID(text);
			if (shipByRegID != null && !shipByRegID.bDestroyed && shipByRegID.objSS != null && !shipByRegID.objSS.bBOLocked && shipByRegID.IsDerelict() && !shipsInPlots.Contains(text))
			{
				bool flag = false;
				List<Ship> allDockedShips = shipByRegID.GetAllDockedShips();
				foreach (Ship ship in allDockedShips)
				{
					if (ship != null && !ship.bDestroyed)
					{
						if (shipsForOwner2.Contains(ship.strRegID))
						{
							flag = true;
							break;
						}
					}
				}
				if (!flag)
				{
					shipByRegID.Destroy(false);
					num++;
				}
			}
		}
		Debug.Log("Destroyed " + num + " derelicts.");
	}

	private static void AIShipCleanup(AIShip aiShip, Ship shipATC)
	{
		Ship ship = aiShip.Ship;
		if (ship.objSS.bIsBO)
		{
			AIShipManager.UnregisterShip(aiShip);
			aiShip.Ship.ToggleVis(false, true);
		}
		else
		{
			if (shipATC != null)
			{
				Ship shipByRegID = CrewSim.system.GetShipByRegID("OKLG_UNK");
				Ship ship2 = null;
				if (shipATC.MaxPopulation < (double)shipATC.GetPeople(false).Count)
				{
					List<string> allATCRecycleStations = AIShipManager.GetAllATCRecycleStations(shipATC.strRegID);
					if (allATCRecycleStations.Count > 1)
					{
						Ship shipByRegID2 = CrewSim.system.GetShipByRegID(allATCRecycleStations[UnityEngine.Random.Range(0, allATCRecycleStations.Count - 1)]);
						ship2 = (shipByRegID2 ?? shipATC);
					}
				}
				else
				{
					ship2 = shipATC;
				}
				List<CondOwner> people = ship.GetPeople(false);
				foreach (CondOwner condOwner in people)
				{
					if (aiShip.AIType == AIType.Pirate)
					{
						if (shipByRegID == null)
						{
							break;
						}
						ship2 = shipByRegID;
					}
					condOwner.LogMove(ship.strRegID, ship2.strRegID, MoveReason.REGIONCLEANUP, null);
					condOwner.UnclaimShip(ship.strRegID);
					condOwner.ClaimShip(ship2.strRegID);
					condOwner.CatchUp();
					CrewSim.MoveCO(condOwner, ship2, false);
					if (condOwner.Company != null)
					{
						condOwner.Company.SetPermissionAirlock(condOwner.strID, false);
						condOwner.Company.SetPermissionShore(condOwner.strID, false);
						condOwner.Company.SetPermissionRestore(condOwner.strID, false);
					}
					condOwner.ZeroCondAmount("IsShakedownModeActive");
					condOwner.ZeroCondAmount("IsLEOArresting");
					condOwner.ZeroCondAmount("IsLEOAttacking");
					if (!condOwner.HasQueuedInteraction("WanderSoon"))
					{
						Interaction interaction = DataHandler.GetInteraction("WanderSoon", null, false);
						condOwner.QueueInteraction(condOwner, interaction, false);
					}
				}
			}
			aiShip.Ship.ToggleVis(false, true);
			aiShip.Ship.Destroy(true);
		}
	}

	private static void CheckTracking()
	{
		if (CrewSim.coPlayer == null || CrewSim.coPlayer.ship == null)
		{
			return;
		}
		CrewSim.coPlayer.ship.aTrackCurrent.Clear();
		bool trackWarning = CrewSim.coPlayer.ship.trackWarning;
		bool flag = false;
		List<AIShip> shipsOfTypeForRegion = AIShipManager.GetShipsOfTypeForRegion(AIShipManager.strATCLast, AIType.Police);
		shipsOfTypeForRegion.AddRange(AIShipManager.GetShipsOfTypeForRegion(AIShipManager.strATCLast, AIType.Pirate));
		foreach (AIShip aiship in shipsOfTypeForRegion)
		{
			Ship ship = aiship.Ship;
			if (ship != null && !ship.bDestroyed && ship.shipScanTarget != null && !ship.shipScanTarget.bDestroyed)
			{
				if (!ship.bDocked && ship.shipScanTarget.IsPlayerShip())
				{
					if (ship.shipScanTarget.aTrackCurrent.IndexOf(ship.strRegID) < 0)
					{
						ship.shipScanTarget.aTrackCurrent.Add(ship.strRegID);
					}
					if (ship.shipScanTarget.aTrackIgnores.IndexOf(ship.strRegID) < 0)
					{
						flag = true;
					}
				}
				else
				{
					ship.shipScanTarget.aTrackIgnores.Remove(ship.strRegID);
				}
			}
		}
		CrewSim.coPlayer.ship.trackWarning = flag;
		if (flag && !trackWarning && (double)Time.timeScale > 1.0)
		{
			CrewSim.ResetTimeScale();
			BeatManager.ResetTensionTimer();
		}
	}

	private static void HandleDeads()
	{
		JsonPersonSpec personSpec = DataHandler.GetPersonSpec("RandomAdultDead");
		JsonShipSpec shipSpec = DataHandler.GetShipSpec("StationNotPlayerCurrentShip");
		Ship shipByRegID = CrewSim.system.GetShipByRegID("OKLG_UNK");
		if (shipByRegID == null)
		{
			return;
		}
		JsonPersonSpec jps = personSpec;
		global::Social soc = null;
		bool bForceUnrelated = false;
		JsonShipSpec jss = shipSpec;
		List<PersonSpec> persons = StarSystem.GetPersons(jps, soc, bForceUnrelated, null, jss);
		if (persons == null)
		{
			return;
		}
		foreach (PersonSpec personSpec2 in persons)
		{
			if (personSpec2 != null && !(personSpec2.GetCO() == null))
			{
				CondOwner co = personSpec2.GetCO();
				Debug.Log(string.Concat(new string[]
				{
					"#NPC# Transiting dead ",
					co.strName,
					" from ",
					co.ship.strRegID,
					" to ",
					shipByRegID.strRegID
				}));
				co.UnclaimShip(co.ship.strRegID);
				co.ClaimShip(shipByRegID.strRegID);
				CrewSim.MoveCO(co, shipByRegID, false);
			}
		}
	}

	private static void HandleTransits()
	{
		if (DataHandler.dictTransit == null || DataHandler.dictTransit.Count == 0)
		{
			return;
		}
		List<string> list = DataHandler.dictTransit.Keys.ToList<string>().Randomize<string>();
		CondOwner selectedCrew = CrewSim.GetSelectedCrew();
		int num = 3;
		foreach (string text in list)
		{
			Ship shipByRegID = CrewSim.system.GetShipByRegID(text);
			if (shipByRegID != null)
			{
				CondTrigger condTrigger = DataHandler.GetCondTrigger("TIsTrafficAI");
				List<CondOwner> cos = shipByRegID.GetCOs(condTrigger, false, false, false);
				if (cos.Count != 0)
				{
					CondOwner condOwner = cos[MathUtils.Rand(0, cos.Count, MathUtils.RandType.Flat, null)];
					List<Tuple<Ship, JsonTransitConnection>> list2 = new List<Tuple<Ship, JsonTransitConnection>>();
					JsonTransit transitConnections = DataHandler.GetTransitConnections(text);
					if (transitConnections != null && transitConnections.aConnections != null)
					{
						foreach (JsonTransitConnection jsonTransitConnection in transitConnections.aConnections)
						{
							if (jsonTransitConnection != null && !(jsonTransitConnection.strTargetRegID == shipByRegID.strRegID))
							{
								if (jsonTransitConnection.IsValidUser(condOwner))
								{
									Ship shipByRegID2 = CrewSim.system.GetShipByRegID(jsonTransitConnection.strTargetRegID);
									if (shipByRegID2 != null)
									{
										if ((double)shipByRegID2.Population <= shipByRegID2.MaxPopulation)
										{
											list2.Add(new Tuple<Ship, JsonTransitConnection>(shipByRegID2, jsonTransitConnection));
										}
									}
								}
							}
						}
					}
					if (list2.Count != 0)
					{
						Tuple<Ship, JsonTransitConnection> tuple = list2[MathUtils.Rand(0, list2.Count, MathUtils.RandType.Flat, null)];
						if (selectedCrew.ship != null && selectedCrew.ship.objSS.GetDistance(shipByRegID.objSS) > 2.005376018132665E-06)
						{
							if (num == 0)
							{
								continue;
							}
							num--;
						}
						if (shipByRegID.LoadState >= Ship.Loaded.Edit)
						{
							JsonPledge pledge = DataHandler.GetPledge("EmbarkCommand");
							Pledge2 pledge2 = PledgeFactory.Factory(condOwner, pledge, tuple.Item1.ShipCO);
							condOwner.AddPledge(pledge2);
							condOwner.AddCondAmount("IsEmbarkCommand", 1.0, 0.0, 0f);
							Debug.Log(string.Concat(new string[]
							{
								"Adding pledge to make ",
								condOwner.strName,
								" transit from ",
								condOwner.ship.strRegID,
								" to ",
								tuple.Item1.strRegID
							}));
						}
						else
						{
							Debug.Log(string.Concat(new string[]
							{
								"#NPC# Transiting ",
								condOwner.strName,
								" from ",
								condOwner.ship.strRegID,
								" to ",
								tuple.Item1.strRegID
							}));
							condOwner.UnclaimShip(condOwner.ship.strRegID);
							condOwner.ClaimShip(tuple.Item1.strRegID);
							condOwner.CatchUp();
							CrewSim.MoveCO(condOwner, tuple.Item1, false);
							if (!condOwner.HasQueuedInteraction("WanderSoon"))
							{
								Interaction interaction = DataHandler.GetInteraction("WanderSoon", null, false);
								condOwner.QueueInteraction(condOwner, interaction, false);
							}
							condOwner.ZeroCondAmount("IsEmbarkCommand");
						}
					}
				}
			}
		}
		AIShipManager.NPCReport();
	}

	private static void HandleMaint()
	{
		Dictionary<string, JsonTransit>.KeyCollection keys = DataHandler.dictTransit.Keys;
		foreach (string text in keys)
		{
			Ship shipByRegID = CrewSim.system.GetShipByRegID(text);
			if (shipByRegID != null)
			{
				PersonSpec personSpec = shipByRegID.GetPerson(DataHandler.GetPersonSpec("MaintenanceTechFind"), null, false, null);
				if (personSpec == null)
				{
					JsonPersonSpec personSpec2 = DataHandler.GetPersonSpec(text + "MaintenanceTech");
					if (personSpec2 != null)
					{
						personSpec = new PersonSpec(personSpec2, true);
						CondOwner condOwner = personSpec.MakeCondOwner(PersonSpec.StartShip.OLD, shipByRegID);
						if (condOwner.Company == null)
						{
							string strName = "Maintenance";
							JsonCompany jsonCompany = CrewSim.system.GetCompany(strName);
							if (jsonCompany == null)
							{
								jsonCompany = new JsonCompany();
								jsonCompany.strName = strName;
								jsonCompany.strRegID = shipByRegID.strRegID;
							}
							condOwner.Company = jsonCompany;
							jsonCompany.mapRoster[condOwner.strID] = new JsonCompanyRules();
							condOwner.Company.SetPermissionAirlock(condOwner.strID, true);
						}
					}
				}
			}
		}
	}

	public static List<AIShip> GetShipsOfTypeForRegion(string strRegion, AIType aiType)
	{
		List<AIShip> list = new List<AIShip>();
		List<AIShip> list2;
		if (strRegion != null && AIShipManager.dictAIs.TryGetValue(strRegion, out list2))
		{
			foreach (AIShip aiship in list2)
			{
				if ((aiType & aiship.AIType) == aiship.AIType)
				{
					list.Add(aiship);
				}
			}
		}
		return list;
	}

	public static List<AIShip> GetShipsOfTypeForRegion(AIType aiType)
	{
		return AIShipManager.GetShipsOfTypeForRegion(AIShipManager.strATCLast, aiType);
	}

	public static bool BingoFuelCheck(Ship ship, Ship shipHome, double maxSpeed)
	{
		return ship.DeltaVRemainingRCS <= 1.2 * AIShipManager.GetDeltaVNeededToTargetFullTrip(ship, shipHome.objSS, maxSpeed);
	}

	public static bool BingoFuelCheck(Ship ship, Ship shipTarget, Ship shipHome, double maxSpeed)
	{
		double num = 1.2 * AIShipManager.GetDeltaVNeededToTargetFullTrip(ship, shipTarget.objSS, maxSpeed);
		double num2 = 1.2 * AIShipManager.GetDeltaVNeededToTargetFullTrip(shipTarget, shipHome.objSS, maxSpeed);
		return ship.DeltaVRemainingRCS <= num + num2;
	}

	public static bool LowFuel(Ship ship, double maxSpeed)
	{
		double rcsremain = ship.GetRCSRemain();
		double rcsmax = ship.GetRCSMax();
		return rcsremain / rcsmax < 0.25 || ship.DeltaVRemainingRCS < 8.0 * maxSpeed;
	}

	public static double GetDeltaVNeededToTargetFullTrip(Ship shipUs, ShipSitu shipSituTarget, double maxShipSpeed)
	{
		double num = maxShipSpeed * 2.0;
		if (shipUs == null || shipUs.objSS == null || shipSituTarget == null)
		{
			return num;
		}
		double dX = shipSituTarget.vVelX - shipUs.objSS.vVelX;
		double dY = shipSituTarget.vVelY - shipUs.objSS.vVelY;
		double magnitude = MathUtils.GetMagnitude(dX, dY);
		return num + magnitude;
	}

	public static void ValidateCrew(CondOwner co)
	{
		if (co == null)
		{
			return;
		}
		AIShipManager.ValidateCrew(co.ship);
	}

	public static void ValidateCrew(Ship ship)
	{
		if (ship == null || ship.bDestroyed || ship.objSS == null || ship.IsStation(false) || ship.IsStationHidden(false))
		{
			return;
		}
		AIShip aiship = AIShipManager.TryGetAIShip(ship, null);
		if (aiship == null)
		{
			return;
		}
		List<CondOwner> people = aiship.Ship.GetPeople(false);
		List<CondOwner> list = new List<CondOwner>();
		JsonCompany jsonCompany = (!(CrewSim.coPlayer != null)) ? null : CrewSim.coPlayer.Company;
		if (jsonCompany != null)
		{
			list = jsonCompany.GetCrewMembers(null);
		}
		foreach (CondOwner condOwner in people)
		{
			if (!(condOwner == null) && condOwner.bAlive && !list.Contains(condOwner))
			{
				return;
			}
		}
		if (AIShipManager.ShowDebugLogs)
		{
			Debug.Log(string.Concat(new object[]
			{
				"Dead Captain! Unregistered ",
				aiship.AIType,
				" ",
				aiship.Ship.strRegID
			}));
		}
		AIShipManager.UnregisterShip(aiship);
	}

	public static void UnregisterShip(Ship ship)
	{
		if (ship == null)
		{
			return;
		}
		AIShip aishipByRegID = AIShipManager.GetAIShipByRegID(ship.strRegID);
		AIShipManager.UnregisterShip(aishipByRegID);
	}

	private static void UnregisterShip(AIShip aiShip)
	{
		if (aiShip == null || aiShip.Ship == null || AIShipManager.dictAIs == null)
		{
			return;
		}
		Ship ship = aiShip.Ship;
		ship.Maneuver(0f, 0f, 0f, 0, 1E-10f, Ship.EngineMode.RCS);
		ship.shipScanTarget = null;
		ship.shipSituTarget = null;
		foreach (List<AIShip> list in AIShipManager.dictAIs.Values)
		{
			for (int i = list.Count - 1; i >= 0; i--)
			{
				if (list[i].Ship == ship || list[i].Ship.strRegID == ship.strRegID)
				{
					list.RemoveAt(i);
					AIShipManager._aIQueue.UnregisterShip(aiShip);
				}
			}
		}
	}

	public static AIShip AddAIToShip(Ship ship, AIType at, string atcLast = null, JsonAIShipSave jSave = null)
	{
		if (ship == null || at == AIType.NA)
		{
			return null;
		}
		if (atcLast == null)
		{
			atcLast = AIShipManager.strATCLast;
		}
		AIShip aiship;
		if (jSave != null)
		{
			aiship = new AIShip(jSave, ship);
		}
		else
		{
			aiship = AIShipManager.TryGetAIShip(ship, atcLast);
			if (aiship == null)
			{
				aiship = new AIShip(atcLast, at, ship);
			}
		}
		if (aiship.Ship == null)
		{
			Debug.LogWarning("Unable to create AI");
			return null;
		}
		if (jSave != null)
		{
			aiship.AddCommandLoot(jSave.strActiveCommand, jSave.strActiveCommandPayload);
		}
		AIShipManager.AddAIToDict(aiship, atcLast);
		return aiship;
	}

	private static AIShip TryGetAIShip(Ship ship, string atcLast = null)
	{
		if (ship == null)
		{
			return null;
		}
		if (atcLast == null)
		{
			atcLast = AIShipManager.strATCLast;
		}
		if (!string.IsNullOrEmpty(atcLast) && AIShipManager.dictAIs.ContainsKey(atcLast))
		{
			foreach (AIShip aiship in AIShipManager.dictAIs[atcLast])
			{
				if (aiship.Ship == ship)
				{
					return aiship;
				}
			}
		}
		return null;
	}

	private static void AddAIToDict(AIShip ship, string atc = null)
	{
		if (atc == null)
		{
			atc = AIShipManager.strATCLast;
		}
		if (!AIShipManager.dictAIs.ContainsKey(atc))
		{
			AIShipManager.dictAIs[atc] = new List<AIShip>();
		}
		if (AIShipManager.TryGetAIShip(ship.Ship, atc) == null)
		{
			AIShipManager.dictAIs[atc].Add(ship);
		}
		AIShipManager._aIQueue.Enqueue(ship);
	}

	public static void NPCReport()
	{
		if (CrewSim.system == null)
		{
			return;
		}
		int num = 0;
		int num2 = 0;
		Dictionary<string, int> dictionary = new Dictionary<string, int>();
		foreach (Ship ship in CrewSim.system.GetAllLoadedShips())
		{
			foreach (CondOwner condOwner in ship.GetPeople(false))
			{
				num++;
				if (!condOwner.bAlive)
				{
					num2++;
				}
				if (dictionary.ContainsKey(ship.strRegID))
				{
					Dictionary<string, int> dictionary2;
					string strRegID;
					(dictionary2 = dictionary)[strRegID = ship.strRegID] = dictionary2[strRegID] + 1;
				}
				else
				{
					dictionary[ship.strRegID] = 1;
				}
			}
		}
		using (Dictionary<string, int>.Enumerator enumerator3 = dictionary.GetEnumerator())
		{
			if (enumerator3.MoveNext())
			{
				KeyValuePair<string, int> keyValuePair = enumerator3.Current;
			}
		}
		Debug.Log(string.Concat(new object[]
		{
			"#NPC# NPCs: ",
			num,
			"; Dead: ",
			num2
		}));
	}

	public static AIType GetShipType(Ship ship)
	{
		if (ship == null)
		{
			return AIType.NA;
		}
		foreach (List<AIShip> source in AIShipManager.dictAIs.Values)
		{
			using (IEnumerator<AIShip> enumerator2 = (from ai in source
			where ai.Ship == ship || ai.Ship.strRegID == ship.strRegID
			select ai).GetEnumerator())
			{
				if (enumerator2.MoveNext())
				{
					AIShip aiship = enumerator2.Current;
					return aiship.AIType;
				}
			}
		}
		return AIType.NA;
	}

	public static int PiratesMin
	{
		get
		{
			return AIShipManager._pirates_Min;
		}
	}

	public static int PiratesMax
	{
		get
		{
			return AIShipManager._pirates_Max;
		}
	}

	private static void BuildRegionalStationDict()
	{
		foreach (Ship ship in CrewSim.system.dictShips.Values)
		{
			if (ship != null && ship.objSS != null && ship.objSS.bIsRegion)
			{
				Loot loot = DataHandler.GetLoot("TXT" + ship.strRegID + "SpawnStations");
				if (loot != null && !(loot.strName == "Blank"))
				{
					AIShipManager.dictSpawnRegIDs[ship.strRegID] = loot;
					loot = DataHandler.GetLoot("TXT" + ship.strRegID + "RecycleStations");
					if (loot != null && !(loot.strName == "Blank"))
					{
						AIShipManager.dictPilotSrcRegIDs[ship.strRegID] = loot.GetLootNames(null, false, null);
					}
				}
			}
		}
	}

	public static List<string> GetAllATCRecycleStations(string atc)
	{
		List<string> result = null;
		if (!AIShipManager.dictPilotSrcRegIDs.TryGetValue(atc, out result))
		{
			foreach (KeyValuePair<string, List<string>> keyValuePair in AIShipManager.dictPilotSrcRegIDs)
			{
				if (keyValuePair.Value != null && keyValuePair.Value.Count != 0)
				{
					if (keyValuePair.Value.Contains(atc))
					{
						return AIShipManager.GetAllStationsInATCRegion(keyValuePair.Key);
					}
				}
			}
			return new List<string>();
		}
		return result;
	}

	public static List<string> GetAllStationsInATCRegion(string atc)
	{
		List<string> list = new List<string>();
		Loot loot;
		if (AIShipManager.dictSpawnRegIDs.TryGetValue(atc, out loot))
		{
			list = loot.GetAllLootNames();
		}
		if (list.Count == 0)
		{
			list.Add(atc);
		}
		return list;
	}

	private static PotentialSpawnStationsDTO GetHaulerSpawnStations(string atc)
	{
		List<string> allStationsInATCRegion = AIShipManager.GetAllStationsInATCRegion(atc);
		CondTrigger condTrigger = DataHandler.GetCondTrigger("TIsDerelictCollector");
		CondTrigger condTrigger2 = DataHandler.GetCondTrigger("TIsDerelictSpawner");
		PotentialSpawnStationsDTO potentialSpawnStationsDTO = new PotentialSpawnStationsDTO();
		foreach (string strRegID in allStationsInATCRegion)
		{
			Ship shipByRegID = CrewSim.system.GetShipByRegID(strRegID);
			if (shipByRegID != null && !shipByRegID.bDestroyed)
			{
				if (condTrigger.Triggered(shipByRegID.ShipCO, null, true))
				{
					potentialSpawnStationsDTO.DerelictCollectorStations.Add(new Tuple<Ship, int>(shipByRegID, (int)shipByRegID.ShipCO.GetCondAmount("StationIsDerelictCollector")));
				}
				if (condTrigger2.Triggered(shipByRegID.ShipCO, null, true))
				{
					potentialSpawnStationsDTO.DerelictSpawnerStations.Add(new Tuple<Ship, int>(shipByRegID, (int)shipByRegID.ShipCO.GetCondAmount("StationIsDerelictSpawner")));
				}
			}
		}
		return potentialSpawnStationsDTO;
	}

	public static string GetRandomStationInATCRegion(string atc)
	{
		string text = atc;
		Loot loot;
		if (AIShipManager.dictSpawnRegIDs.TryGetValue(atc, out loot))
		{
			text = loot.GetLootNameSingle(null);
			if (string.IsNullOrEmpty(text))
			{
				text = atc;
			}
		}
		return text;
	}

	public static void LEOCheckIllegalUndock(Ship shipTarget, Ship shipPolice)
	{
		if (AIShipManager.GetShipType(shipPolice) != AIType.Police)
		{
			return;
		}
		bool flag = false;
		bool flag2 = false;
		Loot loot = DataHandler.GetLoot("CONDPoliceShakedownQuarryEscapingPlayerShip");
		CondTrigger condTrigger = DataHandler.GetCondTrigger("TIsShakedownDone");
		foreach (CondOwner condOwner in shipTarget.GetPeople(false))
		{
			if (condTrigger.Triggered(condOwner, null, true))
			{
				flag = true;
			}
			if (condOwner.HasCond("IsShakedownModeActive"))
			{
				loot.ApplyCondLoot(condOwner, 1f, null, 0f);
				flag2 = true;
			}
		}
		if (flag && !flag2)
		{
			return;
		}
		loot = DataHandler.GetLoot("CONDPoliceShakedownQuarryEscapingLEOShip");
		bool flag3 = false;
		foreach (CondOwner condOwner2 in shipPolice.GetPeople(false))
		{
			if (condOwner2.bAlive && !condOwner2.HasCond("Unconscious"))
			{
				if (condOwner2.HasCond("IsShakedownModeActive"))
				{
					loot.ApplyCondLoot(condOwner2, 1f, null, 0f);
					flag3 = true;
				}
			}
		}
		if (!flag3)
		{
			return;
		}
		if (shipPolice.strLaw != null)
		{
			loot = DataHandler.GetLoot("Crime" + shipPolice.strLaw + "Arrest");
			foreach (CondOwner condOwner3 in shipTarget.GetPeople(false))
			{
				if (!condOwner3.HasCond("CareerLEOfficer"))
				{
					loot.ApplyCondLoot(condOwner3, 1f, null, 0f);
				}
			}
		}
	}

	public static bool CheckLocalAuthorityScenario()
	{
		if (CrewSim.coPlayer.HasCond("IsInChargen") || CrewSim.coPlayer.ship.objSS.bIsBO)
		{
			return false;
		}
		if (AIShipManager.strATCLast != "OKLG")
		{
			return false;
		}
		Ship shipATCLast = AIShipManager.ShipATCLast;
		if (shipATCLast.GetRangeTo(CrewSim.coPlayer.ship) > 2.673834764710392E-05)
		{
			return false;
		}
		List<AIShip> shipsOfTypeForRegion = AIShipManager.GetShipsOfTypeForRegion(shipATCLast.strRegID, AIType.Police);
		bool flag = false;
		int num = 0;
		while (!flag && num < shipsOfTypeForRegion.Count)
		{
			flag = (shipATCLast.GetRangeTo(shipsOfTypeForRegion[num].Ship) <= AIShipManager.fPoliceIgnoreRange);
			num++;
		}
		bool result = false;
		if (!flag && shipsOfTypeForRegion.Count < 4)
		{
			Debug.Log("GenerateTension() spawning police.");
			AIShipManager.SpawnLeo();
			result = true;
		}
		else if (StarSystem.fEpoch - CrewSim.shipCurrentLoaded.dLastScanTime > 21600.0)
		{
			for (int i = shipsOfTypeForRegion.Count - 1; i >= 0; i--)
			{
				Ship ship = shipsOfTypeForRegion[i].Ship;
				if (ship.shipScanTarget == null && !AIShipManager.LEOForbiddenTarget(CrewSim.shipCurrentLoaded))
				{
					List<CondOwner> people = ship.GetPeople(false);
					if (people.Count != 0)
					{
						if (people.Any((CondOwner x) => x.HasFaction("OKLGLEO")))
						{
							if (ship.HideFromSystem)
							{
								ship.HideFromSystem = false;
								ship.ToggleVis(true, true);
								ship.AIRefuel();
							}
							Debug.Log("GenerateTension() redirecting police to player.");
							ship.shipScanTarget = CrewSim.shipCurrentLoaded;
							AIShipManager.NotifyTarget(ship, ship.shipScanTarget);
							result = true;
							break;
						}
					}
					AIShipManager.UnregisterShip(shipsOfTypeForRegion[i]);
					if (ship.HideFromSystem)
					{
						ship.Destroy(true);
					}
				}
			}
		}
		return result;
	}

	public static bool LEOForbiddenTarget(Ship ship)
	{
		if (ship == null)
		{
			return true;
		}
		bool bOrbitLocked = ship.objSS.bOrbitLocked;
		List<CondOwner> people = ship.GetPeople(true);
		if (bOrbitLocked && people.Count == 0)
		{
			return true;
		}
		foreach (CondOwner condOwner in people)
		{
			CondTrigger condTrigger = DataHandler.GetCondTrigger("TIsShakedownDone");
			if (condTrigger.Triggered(condOwner, null, true) || condOwner.HasCond("TutorialNoDockedDerelictYet"))
			{
				return true;
			}
		}
		if (AIShipManager.ShipATCLast != null)
		{
			foreach (AIShip aiship in AIShipManager.GetShipsOfTypeForRegion(AIShipManager.ShipATCLast.strRegID, AIType.Police))
			{
				if (aiship.Ship.shipScanTarget == ship)
				{
					return true;
				}
			}
		}
		return false;
	}

	public static void NotifyTarget(Ship shipAI, Ship targetShip)
	{
		if (shipAI == null || targetShip == null)
		{
			return;
		}
		if (targetShip.LoadState < Ship.Loaded.Edit || targetShip.NavCount == 0)
		{
			return;
		}
		if (!shipAI.NavAIManned)
		{
			return;
		}
		bool flag = targetShip.IsDocked();
		bool flag2 = targetShip.IsFlyingDark() || targetShip.strXPDR != targetShip.strRegID;
		bool flag3 = targetShip.strXPDR != targetShip.strRegID;
		if (!flag3)
		{
			string shipOwner = CrewSim.system.GetShipOwner(targetShip.strRegID);
			flag3 = (shipOwner != CrewSim.coPlayer.strID && CrewSim.coPlayer.ship == targetShip && CrewSim.coPlayer.OwnsShip(targetShip.strRegID));
		}
		if (flag3)
		{
			CrewSim.coPlayer.SetCondAmount("Crime" + shipAI.strLaw + "StolenShip", 1.0, 0.0);
		}
		string ia = "SHIPLeoPrepareBoarding";
		if (targetShip.IsDocked())
		{
			foreach (CondOwner condOwner in shipAI.GetPeople(false))
			{
				if (flag)
				{
					condOwner.SetCondAmount("IsAccusingIllegalSalvage", 1.0, 0.0);
				}
				if (flag2)
				{
					condOwner.SetCondAmount("IsAccusingTransponder", 1.0, 0.0);
				}
				if (flag3)
				{
					condOwner.SetCondAmount("IsAccusingStolenShip", 1.0, 0.0);
				}
			}
			ia = "SHIPLeoUndock";
		}
		shipAI.Comms.SendMessage(ia, targetShip.strRegID, null);
		AudioManager.am.SuggestMusic("Explore", false);
		BeatManager.ResetTensionTimer();
	}

	public static void SpawnLeo()
	{
		AIShip aiship = AIShipManager.SpawnAI(AIType.Police, AIShipManager.strATCLast);
		if (aiship != null)
		{
			CrewSim.coPlayer.LogMessage(AIShipManager.strATCLast + " Local Authority ship scan activity detected.", "Bad", "Game");
			AudioManager.am.SuggestMusic("Explore", false);
		}
	}

	public static void PrioritizeShip(Ship ship)
	{
		AIShip aishipByRegID = AIShipManager.GetAIShipByRegID(ship.strRegID);
		AIShipManager.PrioritizeShip(aishipByRegID);
	}

	private static void PrioritizeShip(AIShip ship)
	{
		AIShipManager._aIQueue.PrioritizeShip(ship);
	}

	private static void RunAIQueue()
	{
		if (AIShipManager._aIQueue.Count() == 0)
		{
			List<AIShip> shipsOfTypeForRegion = AIShipManager.GetShipsOfTypeForRegion(AIShipManager.strATCLast, AIType.All);
			shipsOfTypeForRegion.AddRange(AIShipManager.GetShipsOfTypeForRegion("INTERREGIONAL", AIType.All));
			AIShipManager._aIQueue.Fill(shipsOfTypeForRegion);
		}
		IEnumerable<AIShip> enumerable = AIShipManager._aIQueue.Dequeue();
		if (enumerable == null)
		{
			return;
		}
		foreach (AIShip aiship in enumerable)
		{
			if (aiship.Ship == null || aiship.Ship.bDestroyed)
			{
				AIShipManager.UnregisterShip(aiship);
			}
			else
			{
				aiship.RunAI();
			}
		}
	}

	public static void AIIntercept(Ship shipInterceptor, double chaseX, double chaseY, double chaseVX, double chaseVY)
	{
		double num = 0.0;
		double num2 = 0.0;
		double num3 = 0.0;
		float num4 = Mathf.Cos(shipInterceptor.objSS.fRot);
		float num5 = Mathf.Sin(shipInterceptor.objSS.fRot);
		double vPosx = shipInterceptor.objSS.vPosx;
		double vPosy = shipInterceptor.objSS.vPosy;
		double num6 = chaseVX - shipInterceptor.objSS.vVelX;
		double num7 = chaseVY - shipInterceptor.objSS.vVelY;
		float num8 = (float)(chaseX - vPosx);
		float num9 = (float)(chaseY - vPosy);
		float num10 = 5f;
		float num11 = (float)(chaseX - vPosx + num6 * (double)num10);
		float num12 = (float)(chaseY - vPosy + num7 * (double)num10);
		float num13 = 0f;
		num13 += Mathf.Atan2(num11 * num4 + num12 * num5, -num11 * num5 + num12 * num4) * -0.5f;
		num13 -= shipInterceptor.objSS.fW * 0.5f;
		float num14 = 1f;
		num3 = (double)MathUtils.Clamp(num13, -num14, num14);
		num += (double)((num8 * num4 + num9 * num5) * 1000000f);
		num2 += (double)((num8 * -(double)num5 + num9 * num4) * 1000000f);
		num += (double)((float)((num6 * (double)num4 + num7 * (double)num5) * 20000000.0));
		num2 += (double)((float)((num6 * (double)(-(double)num5) + num7 * (double)num4) * 20000000.0));
		foreach (KeyValuePair<string, BodyOrbit> keyValuePair in CrewSim.system.aBOs)
		{
			double num15 = vPosx - keyValuePair.Value.dXReal;
			double num16 = vPosy - keyValuePair.Value.dYReal;
			double num17 = keyValuePair.Value.fRadius * 1.2999999523162842 - (double)((float)Math.Sqrt(num15 * num15 + num16 * num16));
			if (num17 >= 0.0)
			{
				double num18 = num17 * 100000000376832.0;
				num += (num15 * (double)num4 + num16 * (double)num5) * num18;
				num2 += (num15 * (double)(-(double)num5) + num16 * (double)num4) * num18;
			}
		}
		foreach (Ship ship in CrewSim.system.dictShips.Values)
		{
			if (ship != shipInterceptor)
			{
				double num19 = vPosx - ship.objSS.vPosx;
				double num20 = vPosy - ship.objSS.vPosy;
				float num21 = Mathf.Pow((float)(num19 * num19 + num20 * num20 + 1.0000000031710769E-30), -1f);
				float num22 = num21 * 3E-10f;
				num += (double)((float)(num19 * (double)num4 + num20 * (double)num5) * num22);
				num2 += (double)((float)(num19 * (double)(-(double)num5) + num20 * (double)num4) * num22);
			}
		}
		num = MathUtils.Clamp(num, -1.0, 1.0);
		num2 = MathUtils.Clamp(num2, -1.0, 1.0);
		num3 = MathUtils.Clamp(num3, -1.0, 1.0);
		float fDeltaTime = Mathf.Min(CrewSim.TimeElapsedScaled(), 0.125f);
		shipInterceptor.Maneuver((float)num, (float)num2, (float)num3, 0, fDeltaTime, Ship.EngineMode.RCS);
	}

	public static void AIIntercept2(Ship shipInterceptor, double chaseX, double chaseY, double chaseVX, double chaseVY, double fTrim = 0.0, ShipSitu shipATC = null, double maxSpeed = 0.0, double timeDiff = 0.0)
	{
		if (maxSpeed == 0.0)
		{
			maxSpeed = 5.013440183831985E-09;
		}
		float num = Mathf.Cos(shipInterceptor.objSS.fRot);
		float num2 = Mathf.Sin(shipInterceptor.objSS.fRot);
		double num3 = chaseVX - shipInterceptor.objSS.vVelX;
		double num4 = chaseVY - shipInterceptor.objSS.vVelY;
		float num5 = (float)(chaseX - shipInterceptor.objSS.vPosx);
		float num6 = (float)(chaseY - shipInterceptor.objSS.vPosy);
		float num7 = num5;
		float num8 = num6;
		MathUtils.SetLength(ref num7, ref num8, (float)fTrim);
		num5 -= num7;
		num6 -= num8;
		double num9 = (double)num5;
		double num10 = (double)num6;
		MathUtils.SetLength(ref num9, ref num10, 1.0);
		double num11 = num9 * num4 - num10 * num3;
		double num12 = 3.34229345588799E-11;
		if (num11 > num12)
		{
			MathUtils.RotateAngleCCW(ref num9, ref num10, 45.0);
		}
		else if (num11 < -num12)
		{
			MathUtils.RotateAngleCW(ref num9, ref num10, 45.0);
		}
		else
		{
			num10 = (num9 = 0.0);
		}
		float num13 = 0.5f;
		double magnitude = MathUtils.GetMagnitude(num3, num4);
		float magnitude2 = MathUtils.GetMagnitude(num5, num6);
		double num14 = MathUtils.Min(shipInterceptor.RCSAccelMax, 1.9016982118751132E-10);
		num14 = (double)num13 * num14;
		double stoppingDistance = MathUtils.GetStoppingDistance(magnitude, num14);
		double num15 = magnitude * timeDiff * 1.25;
		if (magnitude > 6.68458691177598E-11 && (double)magnitude2 < stoppingDistance + num15)
		{
			num5 = (float)num3;
			num6 = (float)num4;
			num9 = 0.0;
			num10 = 0.0;
			if (AIShipManager.ShowDebugLogs)
			{
				Debug.Log(string.Concat(new object[]
				{
					"#AI# ",
					shipInterceptor.strRegID,
					" emergency braking! fRange: ",
					magnitude2 / 6.684587E-12f,
					"; fStopRange: ",
					stoppingDistance / 6.6845869117759804E-12
				}));
			}
		}
		else if (shipATC != null)
		{
			double num16 = shipATC.vVelX - shipInterceptor.objSS.vVelX;
			double num17 = shipATC.vVelY - shipInterceptor.objSS.vVelY;
			double magnitude3 = MathUtils.GetMagnitude(num16, num17);
			if (magnitude3 > maxSpeed)
			{
				Vector2 lhs = MathUtils.NormalizeVector(new Vector2((float)num16 * 149597870f, (float)num17 * 149597870f));
				Vector2 rhs = MathUtils.NormalizeVector(new Vector2(num5 * 149597870f, num6 * 149597870f));
				float num18 = Vector2.Dot(lhs, rhs);
				if (num18 < 0f)
				{
					if (magnitude3 > maxSpeed * 1.25)
					{
						num5 = (float)num3;
						num6 = (float)num4;
					}
					else if ((double)Math.Abs(num18 + 1f) < 0.0001)
					{
						num6 = (num5 = 0f);
						num9 = 0.0;
						num10 = 0.0;
					}
					else
					{
						num6 = (num5 = 0f);
					}
				}
			}
		}
		float num19 = (float)(num9 * (double)num + num10 * (double)num2);
		float num20 = (float)(num9 * (double)(-(double)num2) + num10 * (double)num);
		num9 = (double)num19;
		num10 = (double)num20;
		num19 = num5 * num + num6 * num2 + (float)num9;
		num20 = num5 * -num2 + num6 * num + (float)num10;
		float num21 = (float)(num14 / shipInterceptor.RCSAccelMax);
		if ((double)magnitude2 < stoppingDistance && num21 < 1f)
		{
			num21 = Mathf.Min(1f, num21 / num13);
			if (AIShipManager.ShowDebugLogs)
			{
				Debug.LogWarning(string.Concat(new object[]
				{
					"#AI# ",
					shipInterceptor.strRegID,
					" more power! Increasing thrust factor to: ",
					num21
				}));
			}
		}
		MathUtils.SetLength(ref num19, ref num20, num21);
		if (AIShipManager.ShowDebugLogs)
		{
			Debug.Log(string.Concat(new object[]
			{
				"#AI# ",
				shipInterceptor.strRegID,
				" Maneuver: ",
				num19,
				" + ",
				num9,
				", ",
				num20,
				" + ",
				num10
			}));
		}
		float num22 = Mathf.Atan2(num5 * num + num6 * num2, -num5 * num2 + num6 * num) * (-0.5f / Time.timeScale);
		float num23 = 1f + (Time.timeScale - 1f) * 0.25f;
		num22 -= shipInterceptor.objSS.fW * num23;
		float num24 = 1f / Time.timeScale;
		float fR = MathUtils.Clamp(num22, -num24, num24);
		float fDeltaTime = Mathf.Min(CrewSim.TimeElapsedScaled(), 0.125f);
		shipInterceptor.Maneuver(num19, num20, fR, 0, fDeltaTime, Ship.EngineMode.RCS);
	}

	private static Point CalculateEvasionTarget(ShipSitu ship, ShipSitu collisionSitu)
	{
		double x = ship.vPosx - (ship.vPosx - collisionSitu.vPosx) * -1.0;
		double y = ship.vPosy - (ship.vPosy - collisionSitu.vPosy) * -1.0;
		return new Point(x, y);
	}

	public static bool IsOnCollisionCourse(Ship shipUs, out Point escapePoint, float predictionTime = 30f)
	{
		if (shipUs == null)
		{
			escapePoint = default(Point);
			return false;
		}
		Line us = new Line(shipUs.objSS, predictionTime);
		double fAccel = MathUtils.Min(shipUs.RCSAccelMax, 1.9016982118751132E-10);
		Ship nearestStationRegional = CrewSim.system.GetNearestStationRegional(shipUs.objSS.vPosx, shipUs.objSS.vPosy);
		PredictionRectangle predictionRectangle = new PredictionRectangle(shipUs, nearestStationRegional);
		foreach (Ship ship in CrewSim.system.GetAllLoadedShips())
		{
			if (ship != shipUs && ship != shipUs.shipScanTarget && ship.shipScanTarget != shipUs && !ship.objSS.bBOLocked && !ship.objSS.bIsBO && !ship.HideFromSystem && !ship.GetAllDockedShips().Contains(shipUs.shipScanTarget) && !ship.IsDockedWith(shipUs))
			{
				double distance = shipUs.objSS.GetDistance(ship.objSS);
				double stoppingDistance = MathUtils.GetStoppingDistance(MathUtils.GetMagnitude(shipUs.objSS.vVel, ship.objSS.vVel), fAccel);
				if (distance <= stoppingDistance * 1.5)
				{
					if (predictionRectangle.HasIntersectingdVel(shipUs.objSS.vPos, ship, nearestStationRegional) || predictionRectangle.IsInsideRectangle(ship.objSS.vPos))
					{
						Line them = new Line(ship.objSS, predictionTime);
						if (AIShipManager.ShowDebugLogs)
						{
							Debug.Log("#AI# " + shipUs.strRegID + " on collision course with " + ship.strRegID);
						}
						escapePoint = AIShipManager.GetEscapePoint2(us, them, distance);
						ShipSitu shipSitu = new ShipSitu();
						shipSitu.vPosx = escapePoint.X;
						shipSitu.vPosy = escapePoint.Y;
						shipSitu.vVelX = shipUs.objSS.vVelX;
						shipSitu.vVelY = shipUs.objSS.vVelY;
						return true;
					}
				}
			}
		}
		escapePoint = default(Point);
		return false;
	}

	private static Point GetPerpendicularEscapePoint(Line us, Line them, double distanceToThem)
	{
		Point a = us.A;
		Point b = us.B;
		Point a2 = them.A;
		Point b2 = them.B;
		Vector2 vector = MathUtils.NormalizeVector(a.GetVector2(a2));
		Vector2 from = MathUtils.NormalizeVector(a.GetVector2(b));
		Vector2 vector2 = MathUtils.NormalizeVector(a.GetVector2(b2));
		float num = Vector2.Angle(from, vector);
		float num2 = Vector2.Angle(from, vector2);
		Vector2 vector3 = Vector2.zero;
		Vector2 to = Vector2.zero;
		if (num <= num2 && distanceToThem > 3.342293553032505E-08)
		{
			vector3 = vector;
			to = vector2;
		}
		else
		{
			vector3 = vector2;
			to = vector;
		}
		Vector2 from2 = new Vector2(-vector3.y, vector3.x);
		Vector2 from3 = new Vector2(vector3.y, -vector3.x);
		if (Vector2.Angle(from2, to) > Vector2.Angle(from3, to))
		{
			return new Point(a.X + (double)(from3.x * 3.3422936E-08f), a.Y + (double)(from3.y * 3.3422936E-08f));
		}
		return new Point(a.X + (double)(from2.x * 3.3422936E-08f), a.Y + (double)(from2.y * 3.3422936E-08f));
	}

	private static Point GetEscapePoint2(Line us, Line them, double distanceToThem)
	{
		Vector2 b = MathUtils.NormalizeVector(us.A.GetVector2(them.A));
		Vector2 a = MathUtils.NormalizeVector(us.B.GetVector2(them.B));
		Vector2 to = MathUtils.NormalizeVector(a - b);
		Vector2 from = new Vector2(-b.y, b.x);
		Vector2 from2 = new Vector2(b.y, -b.x);
		float num = Vector2.Angle(from, to);
		float num2 = Vector2.Angle(from2, to);
		if (num2 > num)
		{
			return new Point(us.A.X + (double)(from2.x * 3.3422936E-08f), us.A.Y + (double)(from2.y * 3.3422936E-08f));
		}
		return new Point(us.A.X + (double)(from.x * 3.3422936E-08f), us.A.Y + (double)(from.y * 3.3422936E-08f));
	}

	private static void HandleFerries()
	{
		List<JsonFerryInfo> list = new List<JsonFerryInfo>(AIShipManager.dictFerries.Values);
		foreach (JsonFerryInfo jsonFerryInfo in list)
		{
			if (jsonFerryInfo.GetFerryState() == AIShipManager.FerryState.ARRIVED)
			{
				CondOwner condOwner = null;
				if (string.IsNullOrEmpty(jsonFerryInfo.strFerryCOID))
				{
					AIShipManager.FerryClear(jsonFerryInfo.strFerryCOID);
				}
				else if (DataHandler.mapCOs.TryGetValue(jsonFerryInfo.strFerryCOID, out condOwner))
				{
					if (condOwner.ship.strRegID == jsonFerryInfo.strFerryDestination)
					{
						condOwner.ZeroCondAmount("IsBoarding");
						AIShipManager.FerryClear(jsonFerryInfo.strFerryCOID);
					}
					else if (condOwner.HasCond("IsFerryCancelled"))
					{
						condOwner.ZeroCondAmount("IsFerryCancelled");
						AIShipManager.FerryClear(jsonFerryInfo.strFerryCOID);
					}
					else if (!condOwner.HasQueuedInteraction("ENCFerryPickupStart") && !condOwner.HasQueuedInteraction("ENCFerryPickupAllow") && !condOwner.HasQueuedInteraction("ENCFerryPickupDeny") && !condOwner.HasQueuedInteraction("ENCFerryPickupCancel") && !CrewSim.bRaiseUI)
					{
						jsonFerryInfo.fTimeOfFerryArrival = StarSystem.fEpoch + 5.0;
						jsonFerryInfo.SetFerryState(AIShipManager.FerryState.COMING);
					}
				}
				else
				{
					AIShipManager.FerryClear(jsonFerryInfo.strFerryCOID);
				}
			}
			else if (jsonFerryInfo.GetFerryState() == AIShipManager.FerryState.COMING)
			{
				double num = jsonFerryInfo.fTimeOfFerryArrival - StarSystem.fEpoch;
				if (num <= 0.0)
				{
					CondOwner condOwner2 = null;
					if (string.IsNullOrEmpty(jsonFerryInfo.strFerryCOID) || string.IsNullOrEmpty(jsonFerryInfo.strFerryDestination))
					{
						AIShipManager.FerryClear(jsonFerryInfo.strFerryCOID);
					}
					else if (DataHandler.mapCOs.TryGetValue(jsonFerryInfo.strFerryCOID, out condOwner2))
					{
						condOwner2.LogMessage("Ferry service has arrived!", "Neutral", condOwner2.strID);
						AudioManager.am.PlayAudioEmitter("ShipUIBtnPDAChime", false, false);
						if (condOwner2 == CrewSim.GetSelectedCrew())
						{
							if (!BeatManager.RunEncounter("ENCFerryPickupStart", false))
							{
								AIShipManager.FerryClear(jsonFerryInfo.strFerryCOID);
								continue;
							}
							AIShipManager.ChargeFerryPrice(condOwner2, jsonFerryInfo);
						}
						else
						{
							condOwner2.AddCondAmount("IsBoarding", 1.0, 0.0, 0f);
							Ship objShipNew = CrewSim.system.SpawnShip(jsonFerryInfo.strFerryDestination, Ship.Loaded.Shallow);
							CrewSim.MoveCO(condOwner2, objShipNew, false);
							AIShipManager.ChargeFerryPrice(condOwner2, jsonFerryInfo);
						}
						jsonFerryInfo.SetFerryState(AIShipManager.FerryState.ARRIVED);
					}
					else
					{
						AIShipManager.FerryClear(jsonFerryInfo.strFerryCOID);
					}
				}
			}
		}
	}

	private static void ChargeFerryPrice(CondOwner coRider, JsonFerryInfo jfi)
	{
		if (jfi == null || jfi.bUserCharged)
		{
			return;
		}
		jfi.bUserCharged = true;
		string strDesc = string.Concat(new string[]
		{
			"PASS Tender Service from ",
			coRider.ship.strRegID,
			" to ",
			jfi.strFerryDestination,
			"."
		});
		LedgerLI li = new LedgerLI("PASS", coRider.strID, (float)jfi.fPricePaid, strDesc, Ledger.CURRENCY, StarSystem.fEpoch, false, LedgerLI.Frequency.OneTime);
		Ledger.AddLI(li);
	}

	private static void FerryClear(string strCOID)
	{
		if (string.IsNullOrEmpty(strCOID) || !AIShipManager.dictFerries.ContainsKey(strCOID))
		{
			return;
		}
		AIShipManager.dictFerries.Remove(strCOID);
	}

	public static void RequestFerry(string strCOID, string strDestRegID, double fETA, double fPrice)
	{
		if (fETA < 0.0 || string.IsNullOrEmpty(strCOID) || string.IsNullOrEmpty(strDestRegID))
		{
			Debug.Log(string.Concat(new object[]
			{
				"Warning: ",
				strCOID,
				" Requested Ferry to ",
				strDestRegID,
				" with ETA ",
				fETA,
				". Aborting."
			}));
			return;
		}
		JsonFerryInfo jsonFerryInfo = new JsonFerryInfo();
		jsonFerryInfo.strFerryCOID = strCOID;
		jsonFerryInfo.strFerryDestination = strDestRegID;
		jsonFerryInfo.fTimeOfFerryArrival = StarSystem.fEpoch + fETA;
		jsonFerryInfo.fPricePaid = fPrice;
		jsonFerryInfo.SetFerryState(AIShipManager.FerryState.COMING);
		AIShipManager.dictFerries[strCOID] = jsonFerryInfo;
	}

	public static void CancelFerry(string strCOID)
	{
		if (string.IsNullOrEmpty(strCOID))
		{
			return;
		}
		JsonFerryInfo jsonFerryInfo = null;
		if (AIShipManager.dictFerries.TryGetValue(strCOID, out jsonFerryInfo))
		{
			AIShipManager.FerryClear(jsonFerryInfo.strFerryCOID);
			string strDesc = "CANCELLATION FEE: PASS Tender Service to " + jsonFerryInfo.strFerryDestination + ".";
			LedgerLI li = new LedgerLI("PASS", strCOID, 400f, strDesc, Ledger.CURRENCY, StarSystem.fEpoch, false, LedgerLI.Frequency.OneTime);
			Ledger.AddLI(li);
		}
	}

	public static double CalcFerryETA(Ship shipFrom, Ship shipTo)
	{
		if (shipFrom == null || shipTo == null || string.IsNullOrEmpty(CollisionManager.strATCClosest))
		{
			return 0.0;
		}
		double distance = MathUtils.GetDistance(shipFrom.objSS, shipTo.objSS);
		return distance / 3.34229345588799E-09;
	}

	public static double FerryETA(string strCOID)
	{
		if (string.IsNullOrEmpty(strCOID))
		{
			return 0.0;
		}
		JsonFerryInfo jsonFerryInfo = null;
		if (AIShipManager.dictFerries.TryGetValue(strCOID, out jsonFerryInfo))
		{
			return jsonFerryInfo.fTimeOfFerryArrival;
		}
		return 0.0;
	}

	public static bool FerryComingForCO(string strCOID)
	{
		return !string.IsNullOrEmpty(strCOID) && AIShipManager.dictFerries.ContainsKey(strCOID);
	}

	public static string FerryDestForCO(string strCOID)
	{
		if (string.IsNullOrEmpty(strCOID))
		{
			return null;
		}
		JsonFerryInfo jsonFerryInfo = null;
		if (AIShipManager.dictFerries.TryGetValue(strCOID, out jsonFerryInfo))
		{
			return jsonFerryInfo.strFerryDestination;
		}
		return null;
	}

	public static double FerryPriceFromETA(double fETAIn)
	{
		double num = fETAIn / 3.6;
		num = Math.Round(num, 2, MidpointRounding.AwayFromZero);
		return num + 400.0;
	}

	public const string INTERREGIONAL_ATC = "INTERREGIONAL";

	public const double POLICE_SCAN_COOLDOWN = 21600.0;

	public static Dictionary<string, List<AIShip>> dictAIs;

	private static Dictionary<string, Loot> dictSpawnRegIDs;

	private static Dictionary<string, List<string>> dictPilotSrcRegIDs;

	private static readonly AIShipQueue _aIQueue = new AIShipQueue();

	public static string strATCLast = null;

	public static double fPoliceIgnoreRange;

	private static Ship _shipATCLast = null;

	private static double fTimeOfNextTransit = 0.0;

	private static double fTimeOfNextScav = 0.0;

	private static double fTimeOfNextHauler = 0.0;

	private static double fTimeOfNextTradeCheck = 0.0;

	public static double fTimeOfNextPassShip = 0.0;

	private static int nScavSpawnsRemaining = 0;

	private static Dictionary<string, JsonFerryInfo> dictFerries;

	public const bool bDebugOutput = false;

	public const double TIMER_RESET = double.NegativeInfinity;

	public const double PUSHBACK_TIMER = 10.0;

	public const string TELEPORT_FERRY = "FERRY";

	public const double FERRY_FEE_MINIMUM = 400.0;

	private static int _scavs_Min;

	private static int _scavs_Max;

	private static int _pirates_Min;

	private static int _pirates_Max;

	public static bool ShowDebugLogs = false;

	public static bool IsTestCase = false;

	public enum FerryState
	{
		OFF,
		COMING,
		ARRIVED
	}
}
