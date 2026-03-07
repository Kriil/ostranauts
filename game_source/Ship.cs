using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Ostranauts.COCommands;
using Ostranauts.Core;
using Ostranauts.Core.Models;
using Ostranauts.Electrical;
using Ostranauts.Events;
using Ostranauts.Objectives;
using Ostranauts.Pledges;
using Ostranauts.ShipGUIs.ShipBroker;
using Ostranauts.ShipGUIs.Trade;
using Ostranauts.ShipGUIs.Utilities;
using Ostranauts.Ships;
using Ostranauts.Ships.AIPilots;
using Ostranauts.Ships.Comms;
using Ostranauts.Ships.Rooms;
using Ostranauts.Tools.ExtensionMethods;
using Ostranauts.Trading;
using Ostranauts.Utils.Models;
using UnityEngine;

// Runtime ship model that owns rooms, tiles, crew, and ship-level systems.
// This appears to be the live counterpart to JsonShip, using Json data from
// StreamingAssets/data/ships plus save payloads to construct a functional vessel.
public class Ship : IShip
{
	// Sets up empty runtime collections and caches commonly used CondTriggers.
	// Hardcoded `TIs...` ids here resolve in StreamingAssets/data/condtrigs.
	public Ship(GameObject go)
	{
		this.gameObject = go;
		this.goTiles = new GameObject("goTiles");
		this.goTiles.transform.SetParent(go.transform, false);
		this.tfBGs = new GameObject("goBGs").transform;
		this.tfBGs.SetParent(go.transform, false);
		this.aDocked = new List<Ship>();
		this.mapICOs = new Dictionary<string, CondOwner>();
		this.mapIDRemap = new Dictionary<string, string>();
		this.aTiles = new List<Tile>();
		this.aPwrTiles = new List<Tile>();
		this.aRooms = new List<Room>();
		this.aPeople = new List<PersonSpec>();
		this.aLocks = new List<CondOwner>();
		this.aWPs = new List<WaypointShip>();
		this.aNavs = new List<CondOwner>();
		this.aCores = new List<CondOwner>();
		this.nCols = 0;
		this.nRows = 0;
		this.FloorPlan = new List<Vector2>();
		this.vShipPos = Vector2.zero;
		this.objSS = new ShipSitu();
		this.mapZones = new Dictionary<string, JsonZone>();
		this.dictBGs = new Dictionary<string, List<Vector2>>();
		this.aProxCurrent = new List<string>();
		this.aProxIgnores = new List<string>();
		this.aTrackCurrent = new List<string>();
		this.aTrackIgnores = new List<string>();
		this.aFactions = new List<JsonFaction>();
		this.MarketConfigs = new Dictionary<string, string>();
		this.aRCSDistros = new List<CondOwner>();
		this.aRCSThrusters = new List<CondOwner>();
		this.aDocksys = new List<CondOwner>();
		this.aO2AirPumps = new List<CondOwner>();
		this.aActiveHeavyLiftRotors = new List<CondOwner>();
		if (Ship.ctRCSGasCans == null)
		{
			Ship.ctRCSGasCans = DataHandler.GetCondTrigger("TIsAirtightShipCan");
		}
		if (Ship.ctRCSGasInput == null)
		{
			Ship.ctRCSGasInput = DataHandler.GetCondTrigger("TIsRCSValidInput");
		}
		if (Ship.ctRCSClusterAudioEmitter == null)
		{
			Ship.ctRCSClusterAudioEmitter = DataHandler.GetCondTrigger("TIsRCSClusterAudioEmitter");
		}
		if (Ship.ctRCSDistroInstalled == null)
		{
			Ship.ctRCSDistroInstalled = DataHandler.GetCondTrigger("TIsRCSDistroInstalledOn");
		}
		if (Ship.ctDerelictSafe == null)
		{
			Ship.ctDerelictSafe = DataHandler.GetCondTrigger("TIsDerelictSalvage");
		}
		if (Ship.ctNavStationOn == null)
		{
			Ship.ctNavStationOn = DataHandler.GetCondTrigger("TIsStationNavOn");
		}
		if (Ship.ctXPDR == null)
		{
			Ship.ctXPDR = DataHandler.GetCondTrigger("TIsXPDRInstalled");
		}
		if (Ship.ctXPDRAntOn == null)
		{
			Ship.ctXPDRAntOn = DataHandler.GetCondTrigger("TIsXPDRAntOn");
		}
		if (Ship.ctDocksys == null)
		{
			Ship.ctDocksys = DataHandler.GetCondTrigger("TIsDockSysInstalled");
		}
		if (Ship.ctPortals == null)
		{
			Ship.ctPortals = DataHandler.GetCondTrigger("TIsPortalInstalled");
		}
		if (Ship.ctWearManeuver == null)
		{
			Ship.ctWearManeuver = DataHandler.GetCondTrigger("TIsDestructableManeuver");
		}
		if (Ship.ctWearManeuverTow == null)
		{
			Ship.ctWearManeuverTow = DataHandler.GetCondTrigger("TIsDestructableManeuverTow");
		}
		if (Ship.ctWearTime == null)
		{
			Ship.ctWearTime = DataHandler.GetCondTrigger("TIsDestructableWearTime");
		}
		if (Ship.ctSparkable == null)
		{
			Ship.ctSparkable = DataHandler.GetCondTrigger("TIsSparkable");
		}
		if (Ship.ctPilotSafe == null)
		{
			Ship.ctPilotSafe = DataHandler.GetCondTrigger("TIsHumanAwake");
		}
		if (Ship.ctFactionCO == null)
		{
			Ship.ctFactionCO = DataHandler.GetCondTrigger("TIsNotCarried");
		}
		if (Ship.ctPermitOKLG == null)
		{
			Ship.ctPermitOKLG = DataHandler.GetCondTrigger("TIsOKLGPermitValid");
		}
		if (Ship.ctTowBraceSecured == null)
		{
			Ship.ctTowBraceSecured = DataHandler.GetCondTrigger("TIsTowingBrace01InstalledSecure");
		}
		if (Ship.ctAirPump == null)
		{
			Ship.ctAirPump = DataHandler.GetCondTrigger("TIsAirPump02Installed");
		}
		if (Ship.ctO2Can == null)
		{
			Ship.ctO2Can = DataHandler.GetCondTrigger("TIsRTAO2Installed");
		}
		if (Ship.ctStabilizerActiveOn == null)
		{
			Ship.ctStabilizerActiveOn = DataHandler.GetCondTrigger("TIsStabilizerActive01NotOff");
		}
		if (Ship.ctHeavyLiftRotorsInstalled == null)
		{
			Ship.ctHeavyLiftRotorsInstalled = DataHandler.GetCondTrigger("TIsHeavyLiftRotorNotOff");
		}
		if (Ship.ctTutorialDerelict == null)
		{
			Ship.ctTutorialDerelict = DataHandler.GetCondTrigger("TIsTutorialDerelict");
		}
	}

	public CondOwner ShipCO { get; private set; }

	// Live kinematic state used by flight, docking, nav plotting, and saves.
	public ShipSitu objSS { get; private set; }

	// Likely per-market actor ids or config ids used by the trading subsystem.
	public Dictionary<string, string> MarketConfigs { get; set; }

	// Ship comms handler for ATC, docking clearance, and message traffic.
	public Comms Comms { get; private set; }

	// Helper that excludes unfinished ground stations from "full station" logic.
	public bool IsNotAFullStation
	{
		get
		{
			return this.Classification > Ship.TypeClassification.GroundStationUnfinished;
		}
	}

	// Transponder code; changing it marks the ship status dirty for UI/networking logic.
	public string strXPDR
	{
		get
		{
			return this._strXPDR;
		}
		set
		{
			if (value != this._strXPDR)
			{
				this.bChangedStatus = true;
			}
			this._strXPDR = value;
		}
	}

	// Cached room-stat trigger used by room/system scans.
	private CondTrigger CTRoomStats
	{
		get
		{
			CondTrigger result;
			if ((result = Ship._ctRoomStats) == null)
			{
				result = (Ship._ctRoomStats = DataHandler.GetCondTrigger("TIsRoomStat"));
			}
			return result;
		}
	}

	// Shared trigger for locating a usable reactor/nav IC.
	public static CondTrigger CTReactor
	{
		get
		{
			CondTrigger result;
			if ((result = Ship._ctReactor) == null)
			{
				result = (Ship._ctReactor = DataHandler.GetCondTrigger("TIsReactorICNAVUsable"));
			}
			return result;
		}
	}

	// Tears down the live ship and its attached systems.
	// Likely called on despawn, salvage cleanup, or when a temporary template ship is discarded.
	public void Destroy(bool isDespawning = true)
	{
		if (this.bDestroyed)
		{
			Debug.Log("Ship " + this.strRegID + " already destroyed. Aborting.");
			return;
		}
		if (!isDespawning && this.IsStation(false))
		{
			Debug.LogWarning(string.Concat(new object[]
			{
				StarSystem.fEpoch,
				" ",
				this.strRegID,
				" was destroyed!"
			}));
		}
		if (this.Comms != null)
		{
			this.Comms.Destroy();
		}
		this.Comms = null;
		this.aLog = null;
		this._isDespawning = isDespawning;
		Debug.Log("Destroying ship " + this.strRegID + ".");
		if (CrewSim.system != null)
		{
			CrewSim.system.RemoveShip(this);
		}
		AIShipManager.UnregisterShip(this);
		CrewSim.RemoveLoadedShip(this);
		this.bDestroyed = true;
		this.shipScanTarget = null;
		this.shipStationKeepingTarget = null;
		this.shipUndock = null;
		if (this.ShipCO != null)
		{
			this.ShipCO.Destroy();
		}
		this.ShipCO = null;
		this.objSS.destroy();
		this.objSS = null;
		this.aDocked.Clear();
		this.aProxCurrent.Clear();
		this.aProxCurrent = null;
		this.aProxIgnores.Clear();
		this.aProxIgnores = null;
		this.aTrackCurrent.Clear();
		this.aTrackCurrent = null;
		this.aTrackIgnores.Clear();
		this.aTrackIgnores = null;
		this.FloorPlan.Clear();
		this.FloorPlan = null;
		this.aFactions.Clear();
		this.aFactions = null;
		int num = 0;
		while (this.mapICOs.Count != 0)
		{
			foreach (CondOwner condOwner in this.mapICOs.Values)
			{
				if (!(condOwner.objCOParent != null))
				{
					condOwner.ValidateParent();
					CondOwner.CheckTrue(condOwner.ship == this, "CO in mapICOs but not on ship");
					this.RemoveCO(condOwner, false);
					CondOwner.CheckTrue(condOwner.ship == null, "Unable to remove CO during ship destroy");
					condOwner.ValidateParent();
					if (!isDespawning && condOwner.IsHumanOrRobot && !condOwner.bDestroyed)
					{
						condOwner.Kill = true;
					}
					condOwner.Destroy();
					condOwner.ValidateParent();
					break;
				}
				num++;
			}
			if (num > 0 && num == this.mapICOs.Values.Count)
			{
				Debug.LogWarning(string.Concat(new object[]
				{
					"WARNING: Unable to destroy ",
					this.mapICOs.Values.Count,
					" orphaned COs while destroying ship ",
					this.strRegID
				}));
				foreach (CondOwner condOwner2 in this.mapICOs.Values)
				{
					string text = " - ";
					if (condOwner2.objCOParent != null)
					{
						text += condOwner2.objCOParent.strID;
					}
					Debug.Log(string.Concat(new object[]
					{
						"Orphan: ",
						condOwner2.strName,
						" - ",
						condOwner2.strID,
						" had parent ",
						condOwner2.objCOParent,
						text
					}));
				}
				break;
			}
			num = 0;
		}
		foreach (WaypointShip waypointShip in this.aWPs)
		{
			waypointShip.Destroy();
		}
		this.aWPs.Clear();
		this.aWPs = null;
		foreach (Tile tile in this.aTiles)
		{
			tile.Destroy();
			if (tile != null)
			{
				UnityEngine.Object.Destroy(tile.gameObject);
			}
		}
		this.aTiles.Clear();
		this.aTiles = null;
		this.aPwrTiles.Clear();
		this.aPwrTiles = null;
		foreach (Room room in this.aRooms)
		{
			room.Destroy();
		}
		this.aRooms.Clear();
		this.aRooms = null;
		foreach (JsonZone jsonZone in this.mapZones.Values)
		{
			jsonZone.Destroy();
		}
		this.mapZones.Clear();
		this.mapZones = null;
		this.dictBGs.Clear();
		this.dictBGs = null;
		this.aRCSDistros.Clear();
		this.aRCSDistros = null;
		this.aRCSThrusters.Clear();
		this.aRCSThrusters = null;
		this.aActiveHeavyLiftRotors.Clear();
		this.aActiveHeavyLiftRotors = null;
		this.aDocksys.Clear();
		this.aDocksys = null;
		this.aO2AirPumps.Clear();
		this.aO2AirPumps = null;
		this.aPeople.Clear();
		this.aPeople = null;
		this.goTiles = null;
		this.tfBGs = null;
		this.mapICOs = null;
		this.vShipPos = Vector2.zero;
		this.nCols = 0;
		this.nRows = 0;
		UnityEngine.Object.DestroyImmediate(this.gameObject);
	}

	// Main ship load/build entrypoint.
	// Likely used for both template construction and save restoration:
	// create systems -> build rooms/tiles -> spawn CondOwners/items/crew -> finalize state.
	public void InitShip(bool bTemplateOnly, Ship.Loaded nLoad, string strRegIDNew = null)
	{
		if (nLoad <= this.nLoadState)
		{
			return;
		}
		if (this.json == null)
		{
			return;
		}
		this._bDoneLoading = false;
		if (this.Comms == null)
		{
			this.Comms = new Comms(this, this.json.commData);
		}
		this.bNoCollisions = this.json.bNoCollisions;
		this.dLastScanTime = this.json.dLastScanTime;
		this.bLocalAuthority = this.json.bLocalAuthority;
		this.bAIShip = this.json.bAIShip;
		if (nLoad > Ship.Loaded.Shallow)
		{
			CrewSim.bPoolVisUpdates = true;
		}
		List<CondOwner> list = new List<CondOwner>();
		Dictionary<string, CondOwner> dictionary = new Dictionary<string, CondOwner>();
		Dictionary<int, JsonRoom> dictionary2 = new Dictionary<int, JsonRoom>();
		List<JsonItem> list2 = new List<JsonItem>();
		if (this.nLoadState == Ship.Loaded.None)
		{
			if (nLoad == Ship.Loaded.Edit)
			{
				if (this.json.publicName != null)
				{
					this.publicName = this.json.publicName;
				}
				else
				{
					this.publicName = "$TEMPLATE";
				}
				if (this.json.origin != null)
				{
					this.origin = this.json.origin;
				}
				else
				{
					this.origin = "$TEMPLATE";
				}
				if (this.json.description != null)
				{
					this.description = this.json.description;
				}
			}
			else
			{
				if (this.json.origin != null)
				{
					this.origin = this.json.origin;
				}
				if (this.json.description != null)
				{
					this.description = this.json.description;
				}
				if (this.json.publicName == null || this.json.publicName == string.Empty || this.json.publicName == "$TEMPLATE")
				{
					this.publicName = DataHandler.GetShipName();
				}
				else
				{
					this.publicName = this.json.publicName;
				}
			}
			if (this.json.make != null)
			{
				this.make = this.json.make;
			}
			if (this.json.model != null)
			{
				this.model = this.json.model;
			}
			if (this.json.year != null)
			{
				this.year = this.json.year;
			}
			if (this.json.designation != null)
			{
				this.designation = this.json.designation;
			}
			if (this.json.dimensions != null)
			{
				this.dimensions = this.json.dimensions;
			}
			if (this.json.aRating != null)
			{
				this.rating = this.json.aRating;
			}
			if (this.json.aProxCurrent != null)
			{
				this.aProxCurrent = this.json.aProxCurrent.ToList<string>();
			}
			if (this.json.aProxIgnores != null)
			{
				this.aProxIgnores = this.json.aProxIgnores.ToList<string>();
			}
			if (this.json.aTrackCurrent != null)
			{
				this.aTrackCurrent = this.json.aTrackCurrent.ToList<string>();
			}
			if (this.json.aTrackIgnores != null)
			{
				this.aTrackIgnores = this.json.aTrackIgnores.ToList<string>();
			}
			if (this.json.aFactions != null)
			{
				this.aFactions = CrewSim.system.GetFactions(this.json.aFactions);
			}
			if (this.json.aMarketConfigs != null)
			{
				this.MarketConfigs = this.json.aMarketConfigs.CloneShallow<string, string>();
			}
			if (this.json.aLog != null)
			{
				this.aLog = new List<JsonShipLog>();
				foreach (JsonShipLog jsonShipLog in this.json.aLog)
				{
					if (jsonShipLog == null)
					{
						Debug.LogWarning("null ship aLog entry detected!");
					}
					else
					{
						this.aLog.Add(jsonShipLog.Clone());
					}
				}
			}
			this.Classification = this.json.ShipType;
			this.strLaw = this.json.strLaw;
			this.strParallax = this.json.strParallax;
			this.fShallowMass = this.json.fShallowMass;
			this.fShallowRCSRemass = this.json.fShallowRCSRemass;
			this.fShallowRCSRemassMax = this.json.fShallowRCSRemassMax;
			this.fShallowFusionRemain = this.json.fShallowFusionRemain;
			this.fFusionThrustMax = this.json.fFusionThrustMax;
			this.fFusionPelletMax = this.json.fFusionPelletMax;
			this.fEpochNextGrav = this.json.fEpochNextGrav;
			this.fLastQuotedPrice = this.json.fLastQuotedPrice;
			this.fBreakInMultiplier = this.json.fBreakInMultiplier;
			this.fRCSCount = this.json.nRCSCount;
			this.fShallowRotorStrength = this.json.fShallowRotorStrength;
			if (CrewSim.bSaveUsesOldDockCount)
			{
				this.nDockCount = 1;
			}
			else
			{
				this.nDockCount = this.json.nDockCount;
			}
			this.nRCSDistroCount = this.json.nRCSDistroCount;
			this.fAeroCoefficient = this.json.fAeroCoefficient;
			this.bFusionReactorRunning = this.json.bFusionTorch;
			this.strXPDR = this.json.strXPDR;
			this.bXPDRAntenna = this.json.bXPDRAntenna;
			this.bShipHidden = this.json.bShipHidden;
			this.bIsUnderConstruction = this.json.bIsUnderConstruction;
			if (this.json.nConstructionProgress > 0)
			{
				this.nConstructionProgress = this.json.nConstructionProgress;
			}
			this.strTemplateName = this.json.strTemplateName;
			this.nInitConstructionProgress = this.json.nInitConstructionProgress;
			if (nLoad >= Ship.Loaded.Shallow && this.json.aShallowPSpecs != null)
			{
				list2.AddRange(this.json.aShallowPSpecs);
			}
			if (bTemplateOnly)
			{
				if (strRegIDNew == null)
				{
					this.strRegID = Ship.GenerateID(null);
				}
				else
				{
					this.strRegID = strRegIDNew;
				}
				this.bPrefill = true;
				this.bResetLocks = true;
				if (this.json != null)
				{
					this.bBreakInUsed = this.json.bBreakInUsed;
				}
			}
			else
			{
				this.strRegID = this.json.strRegID;
				this.bPrefill = this.json.bPrefill;
				this.bBreakInUsed = this.json.bBreakInUsed;
			}
			if (this.json.origin == "$TEMPLATE")
			{
				Loot loot = DataHandler.GetLoot("TXTShipOrigin" + this.strRegID[0]);
				if (loot == null)
				{
					loot = DataHandler.GetLoot("TXTShipOrigin");
				}
				if (loot != null)
				{
					List<string> lootNames = loot.GetLootNames(null, false, null);
					if (lootNames != null && lootNames.Count > 0)
					{
						this.origin = loot.GetLootNames(null, false, null)[0];
					}
					else
					{
						this.origin = DataHandler.GetString("SHIP_ORIGIN_UNKNOWN", false);
					}
				}
			}
			this.gameObject.name = this.strRegID;
			this.DMGStatus = this.json.DMGStatus;
		}
		Debug.Log(string.Concat(new object[]
		{
			"#Info# Loading ship ",
			this.strRegID,
			"; Requesting: ",
			nLoad,
			"; Currently: ",
			this.nLoadState
		}));
		if (nLoad >= Ship.Loaded.Edit)
		{
			list2.AddRange(this.json.aItems);
			this.gameObject.SetActive(true);
			this.nRCSDistroCount = 0;
			this.fRCSCount = 0f;
			this.nDockCount = 0;
			this.LiftRotorsThrustStrength = -1f;
			this.aActiveHeavyLiftRotors.Clear();
			if (this.DMGStatus != Ship.Damage.Derelict)
			{
				this.fLastQuotedPrice = 0.0;
			}
			if (this.nConstructionProgress < 100 && this.fFirstVisit > 0.0)
			{
				List<JsonItem> list3 = this.Reconstruct();
				if (list3.Count > 0)
				{
					this.json.aRooms = null;
					this.SpawnItems(list3, true, nLoad, ref dictionary, ref list);
					if (list != null && list.Count > 0)
					{
						this.DoLootSpawners(list);
					}
				}
			}
		}
		if (this.json.aCrew != null && this.nLoadState == Ship.Loaded.None && (!bTemplateOnly || this.DMGStatus != Ship.Damage.Derelict))
		{
			list2.AddRange(this.json.aCrew);
			foreach (JsonItem jsonItem in this.json.aItems)
			{
				if (jsonItem.ForceLoad())
				{
					list2.Add(jsonItem);
				}
			}
		}
		if (this.json.aCOs != null)
		{
			foreach (JsonCondOwnerSave jsonCondOwnerSave in this.json.aCOs)
			{
				DataHandler.dictCOSaves[jsonCondOwnerSave.strID] = jsonCondOwnerSave;
			}
			this.json.aCOs = null;
		}
		if (this.ShipCO == null)
		{
			this.ShipCO = DataHandler.GetCondOwner("ShipCO", this.strRegID, null, false, null, this.json.shipCO, null, this.gameObject.transform);
			this.ShipCO.ship = this;
			this.ShipCO.ClaimShip(this.strRegID);
		}
		this.SpawnItems(list2, bTemplateOnly, nLoad, ref dictionary, ref list);
		if (!bTemplateOnly)
		{
			this.publicName = this.json.publicName;
			this.strRegID = this.json.strRegID;
			this.nCurrentWaypoint = this.json.nCurrentWaypoint;
			this.fTimeEngaged = this.json.fTimeEngaged;
			if (this.nLoadState != Ship.Loaded.Shallow)
			{
				this.objSS = new ShipSitu(this.json.objSS);
				if (this.objSS.NavData != null)
				{
					this.objSS.NavData.SetShip(this);
				}
				if (this.nLoadState == Ship.Loaded.None)
				{
					this.fWearManeuver = this.json.fWearManeuver;
					this.fWearAccrued = (double)this.json.fWearAccrued;
					this.fAIPauseTimer = this.json.fAIPauseTimer;
					this.fAIDockingExpire = this.json.fAIDockingExpire;
				}
			}
			this.fLastVisit = this.json.fLastVisit;
			this.fFirstVisit = this.json.fFirstVisit;
			if (this.json.aWPs != null)
			{
				for (int l = 0; l < this.json.aWPs.Length; l++)
				{
					this.aWPs.Add(new WaypointShip(new ShipSitu(this.json.aWPs[l]), this.json.aWPTimes[l]));
				}
			}
			if (this.json.aRooms != null)
			{
				for (int m = 0; m < this.json.aRooms.Length; m++)
				{
					if (this.json.aRooms[m].aTiles != null)
					{
						JsonRoom jsonRoom = this.json.aRooms[m];
						for (int n = 0; n < jsonRoom.aTiles.Length; n++)
						{
							dictionary2[jsonRoom.aTiles[n]] = jsonRoom;
						}
					}
				}
			}
			this.ApplyUniqueMapConditions();
		}
		else
		{
			this.ApplyUniqueMapConditions();
			List<CondOwner> cos = this.GetCOs(null, true, false, true);
			this.RectifyBrokenIDs(cos);
			List<string> list4 = this.MarketConfigs.Keys.ToList<string>();
			foreach (string text in list4)
			{
				string text2;
				if (this.mapIDRemap.TryGetValue(text, out text2))
				{
					string value = this.MarketConfigs[text];
					this.MarketConfigs.Remove(text);
					this.MarketConfigs[text2] = value;
					MarketManager.TraderIDUpdated(this.strRegID, text, text2);
				}
			}
		}
		if (nLoad > Ship.Loaded.Shallow)
		{
			if (!string.IsNullOrEmpty(this.strParallax))
			{
				CrewSim.system.SetParallax(this.strParallax);
			}
			this.SetZoneData(this.json.aZones);
			this.CreateRooms(dictionary2);
			TileUtils.GetPoweredTiles(this);
			if (this.json.aBGXs != null && this.json.aBGYs != null && this.json.aBGNames != null)
			{
				for (int num = 0; num < this.json.aBGNames.Length; num++)
				{
					if (num >= this.json.aBGYs.Length || num >= this.json.aBGXs.Length)
					{
						break;
					}
					if (this.json.aBGNames[num] != null && this.json.aBGXs[num] != null && this.json.aBGYs[num] != null)
					{
						for (int num2 = 0; num2 < this.json.aBGXs[num].Length; num2++)
						{
							float num3 = this.json.aBGXs[num][num2];
							float num4 = this.json.aBGYs[num][num2];
							if (this.BGItemFits(this.json.aBGNames[num], num3, num4))
							{
								Item background = DataHandler.GetBackground(this.json.aBGNames[num]);
								Vector3 position = new Vector3(this.tfBGs.position.x, this.tfBGs.position.y, background.TF.position.z);
								position.x += num3;
								position.y += num4;
								background.TF.position = position;
								this.BGItemAdd(background);
							}
						}
					}
				}
			}
		}
		if (this.json.objSS == null || !this.json.objSS.bIsBO)
		{
			this.FloorPlan = SilhouetteUtility.GetFloorVectors(this.json.aItems);
		}
		this.objSS.SetSize(SilhouetteUtility.GetSilhouetteLength(this.FloorPlan));
		CrewSim.system.AddShip(this, CrewSim.system.GetShipOwner(this.strRegID));
		if (bTemplateOnly)
		{
			if (nLoad == Ship.Loaded.Edit)
			{
				foreach (CondOwner condOwner in list)
				{
					LootSpawner component = condOwner.GetComponent<LootSpawner>();
					component.UpdateAppearance();
				}
			}
			else if (nLoad >= Ship.Loaded.Shallow)
			{
				this.DoLootSpawners(list);
			}
		}
		else
		{
			foreach (CondOwner condOwner2 in list)
			{
				LootSpawner component2 = condOwner2.GetComponent<LootSpawner>();
				component2.UpdateAppearance();
				condOwner2.Visible = false;
			}
			if (this.json.aPlaceholders != null)
			{
				foreach (JsonPlaceholder jsonPlaceholder in this.json.aPlaceholders)
				{
					if (dictionary.ContainsKey(jsonPlaceholder.strName))
					{
						CondOwner condOwner3 = dictionary[jsonPlaceholder.strName];
						string strID = condOwner3.strID;
						CondOwner condOwner4 = DataHandler.GetCondOwner(jsonPlaceholder.strActionCO);
						CondOwner condOwner5 = DataHandler.GetCondOwner(jsonPlaceholder.strInstalledCO);
						condOwner5.tf.position = condOwner3.tf.position;
						condOwner5.Item.fLastRotation = condOwner3.tf.rotation.eulerAngles.z;
						condOwner4.strPersistentCO = jsonPlaceholder.strPersistentCO;
						condOwner4.strPersistentCT = jsonPlaceholder.strPersistentCT;
						CondOwner coplaceholder = DataHandler.GetCOPlaceholder(condOwner5, condOwner4, jsonPlaceholder.strInstallIA);
						coplaceholder.jCOS = condOwner3.jCOS;
						condOwner3.jCOS = null;
						this.RemoveCO(condOwner3, false);
						condOwner3.Destroy();
						condOwner4.Destroy();
						condOwner5.Destroy();
						coplaceholder.strID = strID;
						this.AddCO(coplaceholder, true);
					}
				}
			}
		}
		list.Clear();
		list = null;
		dictionary.Clear();
		dictionary = null;
		if (this.bPrefill && nLoad >= Ship.Loaded.Edit)
		{
			this.PreFillRooms();
			if (Ship.ctTutorialDerelict.Triggered(this.ShipCO, null, true))
			{
				this.SetupTutorialDerelict();
				this.DamageAllCOs(this.fBreakInMultiplier, false, null);
				this.ShipCO.ZeroCondAmount("IsTutorialDerelict");
			}
			else if (this.DMGStatus == Ship.Damage.Derelict || this.DMGStatus == Ship.Damage.Damaged || (this.DMGStatus == Ship.Damage.Used && this.bBreakInUsed))
			{
				this.BreakIn();
				if (this.fLastQuotedPrice == 0.0)
				{
					this.SetDerelictValue(-1f);
				}
				this.bBreakInUsed = false;
			}
			else if (this.DMGStatus == Ship.Damage.Used)
			{
				this.DamageAllCOs(0.33f, false, null);
				if (this.ShipCO.HasCond("IsVendorShip", false))
				{
					this.ShipCO.ZeroCondAmount("IsVendorShip");
					if (this.Reactor != null)
					{
						FusionIC component3 = this.Reactor.GetComponent<FusionIC>();
						if (component3 != null)
						{
							component3.SetDerelict();
						}
					}
				}
			}
			this.bPrefill = false;
		}
		if (nLoad == Ship.Loaded.Full)
		{
			for (int num6 = this.aPeople.Count - 1; num6 >= 0; num6--)
			{
				PersonSpec personSpec = this.aPeople[num6];
				CondOwner condOwner6 = personSpec.MakeCondOwner(PersonSpec.StartShip.OLD, this);
				Pathfinder pathfinder = condOwner6.Pathfinder;
				Vector2 vector = new Vector2(condOwner6.tf.position.x, condOwner6.tf.position.y);
				pathfinder.tilCurrent = this.GetTileAtWorldCoords1(vector.x, vector.y, true, true);
				if (pathfinder.tilCurrent == null)
				{
					pathfinder.tilCurrent = this.GetCrewSpawnTile(condOwner6);
				}
				FaceAnim2.GetPNG(condOwner6);
				if (condOwner6.currentRoom == null && pathfinder.tilCurrent != null)
				{
					condOwner6.tf.position = pathfinder.tilCurrent.tf.position;
					condOwner6.gameObject.SetActive(true);
					condOwner6.Visible = true;
					if (condOwner6.HasTickers())
					{
						CrewSim.AddTicker(condOwner6);
					}
					List<CondOwner> cos2 = condOwner6.GetCOs(true, null);
					if (cos2 != null)
					{
						foreach (CondOwner condOwner7 in cos2)
						{
							if (condOwner7 != null && condOwner7.HasTickers())
							{
								CrewSim.AddTicker(condOwner7);
							}
						}
					}
					condOwner6.currentRoom = pathfinder.tilCurrent.room;
					if (condOwner6.currentRoom != null)
					{
						condOwner6.currentRoom.AddToRoom(condOwner6, true);
					}
				}
			}
			List<CondOwner> list5 = this.mapICOs.Values.ToList<CondOwner>();
			foreach (Room room in this.aRooms)
			{
				list5.Add(room.CO);
			}
			this.PostGameLoad(list5, nLoad);
		}
		else if (nLoad >= Ship.Loaded.Shallow)
		{
			List<CondOwner> aCOsLoaded = this.mapICOs.Values.ToList<CondOwner>();
			this.PostGameLoad(aCOsLoaded, nLoad);
		}
		this.nLoadState = nLoad;
		this.bCheckRooms = false;
		this.bCheckPower = false;
		this.bCheckTargets = true;
		this.CheckAccruedWear();
		this.UpdatePower();
		this.SilhouettePoints = SilhouetteUtility.GenerateVectorPoints(this.FloorPlan);
		string[] array2 = this.strRegID.Split(new char[]
		{
			'|'
		});
		if (array2.Length > 1)
		{
			this.HideFromSystem = true;
			this._subStation = true;
		}
		if (nLoad >= Ship.Loaded.Edit)
		{
			CrewSim.AddLoadedShip(this);
		}
		if (this.json.aDocked != null || (this.aDocked != null && this.aDocked.Count > 0))
		{
			List<Ship> list6 = new List<Ship>();
			if (this.json.aDocked != null)
			{
				foreach (string text3 in this.json.aDocked)
				{
					Ship shipByRegID = CrewSim.system.GetShipByRegID(text3);
					if (shipByRegID != null && list6.IndexOf(shipByRegID) < 0)
					{
						if (shipByRegID.LoadState >= Ship.Loaded.Edit)
						{
							list6.Insert(0, shipByRegID);
						}
						else
						{
							list6.Add(shipByRegID);
						}
					}
				}
			}
			if (this.aDocked != null)
			{
				foreach (Ship ship in this.aDocked)
				{
					if (ship != null && !ship.bDestroyed)
					{
						if (!list6.Contains(ship))
						{
							list6.Insert(0, ship);
						}
					}
				}
				this.aDocked.Clear();
			}
			if (this.DockCount < list6.Count)
			{
				list6.RemoveRange(this.DockCount, list6.Count - this.DockCount);
			}
			foreach (Ship ship2 in list6)
			{
				ship2.objSS.UpdateTime(StarSystem.fEpoch, false);
				if (nLoad == Ship.Loaded.Full)
				{
					CrewSim.DockShip(this, ship2.strRegID);
				}
				else
				{
					this.Dock(ship2, true);
					ship2.Dock(this, true);
				}
			}
		}
		if (nLoad == Ship.Loaded.Full)
		{
			if (bTemplateOnly)
			{
				this.SetFactions(this.aFactions, false);
			}
			CrewSim.objInstance.workManager.ShowShipTasks(this.strRegID);
			this.UpdateRating(null);
			Debug.Log("#Info# " + this.strRegID + this.GetRatingString());
			this.VisualizeOverlays(false);
		}
		this._bDoneLoading = true;
	}

	private void ApplyUniqueMapConditions()
	{
		if (this.json.aUniques != null)
		{
			CondOwner condOwner = null;
			foreach (JsonShipUniques jsonShipUniques in this.json.aUniques)
			{
				if (this.mapIDRemap.ContainsKey(jsonShipUniques.strCOID))
				{
					if (DataHandler.mapCOs.TryGetValue(this.mapIDRemap[jsonShipUniques.strCOID], out condOwner))
					{
						if (!(condOwner.strName == "SysLootSpawner"))
						{
							if (jsonShipUniques.aConds != null)
							{
								for (int j = 0; j < jsonShipUniques.aConds.Length; j++)
								{
									DataHandler.CreateSimpleConditionFromString(jsonShipUniques.aConds[j]);
									bool bFreezeConds = condOwner.bFreezeConds;
									condOwner.bFreezeConds = false;
									condOwner.AddCondAmount(jsonShipUniques.aConds[j], 1.0, 0.0, 0f);
									condOwner.bFreezeConds = bFreezeConds;
									CrewSimTut.UniqueToStrID.TryAdd(jsonShipUniques.aConds[j], condOwner.strID);
								}
								jsonShipUniques.strCOID = condOwner.strID;
							}
						}
					}
				}
				else if (DataHandler.mapCOs.ContainsKey(jsonShipUniques.strCOID) && DataHandler.mapCOs.TryGetValue(jsonShipUniques.strCOID, out condOwner))
				{
					if (!(condOwner.strName == "SysLootSpawner"))
					{
						if (jsonShipUniques.aConds != null)
						{
							for (int k = 0; k < jsonShipUniques.aConds.Length; k++)
							{
								DataHandler.CreateSimpleConditionFromString(jsonShipUniques.aConds[k]);
								bool bFreezeConds2 = condOwner.bFreezeConds;
								condOwner.bFreezeConds = false;
								condOwner.AddCondAmount(jsonShipUniques.aConds[k], 1.0, 0.0, 0f);
								condOwner.bFreezeConds = bFreezeConds2;
								CrewSimTut.UniqueToStrID.TryAdd(jsonShipUniques.aConds[k], condOwner.strID);
							}
							jsonShipUniques.strCOID = condOwner.strID;
						}
					}
				}
			}
		}
	}

	private void DoLootSpawners(List<CondOwner> aLootSpawners)
	{
		foreach (CondOwner condOwner in aLootSpawners)
		{
			int num = 1;
			if (!condOwner.mapGUIPropMaps.ContainsKey("Panel A") || !condOwner.mapGUIPropMaps["Panel A"].ContainsKey("strCount"))
			{
				Debug.LogError(string.Concat(new object[]
				{
					"ERROR: Ship ",
					this.json.strName,
					" has bad loot spawner at ",
					condOwner.tf.position
				}));
			}
			else
			{
				int.TryParse(condOwner.mapGUIPropMaps["Panel A"]["strCount"], out num);
				condOwner.Visible = false;
				if (num != 0)
				{
					condOwner.GetComponent<LootSpawner>().DoLoot(this);
					condOwner.mapGUIPropMaps["Panel A"]["strCount"] = "-1";
					if (CrewSim.system.GetShipOwner(this.strRegID) == "UNREGISTERED" && this.aPeople.Count > 0)
					{
						CrewSim.system.RegisterShipOwner(this.strRegID, this.aPeople[0].FullName);
						JsonFaction faction = CrewSim.system.GetFaction(this.aPeople[0].FullName);
						this.SetFactions(new List<JsonFaction>
						{
							faction
						}, false);
					}
				}
			}
		}
	}

	private void SetupTutorialDerelict()
	{
		CondTrigger condTrigger = DataHandler.GetCondTrigger("TIsPermitOKLGSalvage");
		CondOwner cofirstOccurrence = this.GetCOFirstOccurrence(condTrigger, true, false, true);
		if (cofirstOccurrence != null)
		{
			cofirstOccurrence.SetCondAmount("IsHoursLeft", 2.0, 0.0);
		}
		CondTrigger condTrigger2 = DataHandler.GetCondTrigger("TIsRackInstalled");
		CondOwner cofirstOccurrence2 = this.GetCOFirstOccurrence(condTrigger2, true, false, true);
		if (cofirstOccurrence2 == null)
		{
			Debug.LogWarning("Tutorial: Could not spawn equipment, container missing");
			return;
		}
		if (!CrewSim.coPlayer)
		{
			return;
		}
		Ship ship = CrewSim.GetSelectedCrew().ship;
		CondTrigger condTrigger3 = DataHandler.GetCondTrigger("TIsNotInstalled");
		List<string> lootNames = DataHandler.GetLoot("TutorialDerelictEquipment").GetLootNames(null, false, null);
		foreach (CondOwner condOwner in ship.GetCOs(condTrigger3, true, false, false))
		{
			if (!(condOwner == null))
			{
				for (int i = lootNames.Count - 1; i >= 0; i--)
				{
					if (!(condOwner.strName != lootNames[i]))
					{
						lootNames.RemoveAt(i);
						break;
					}
				}
			}
		}
		foreach (string text in lootNames)
		{
			CondOwner condOwner2 = DataHandler.GetCondOwner(text);
			if (condOwner2 == null)
			{
				Debug.LogWarning("Tutorial: Could not spawn " + text);
			}
			else
			{
				cofirstOccurrence2.AddCO(condOwner2, false, true, true);
			}
		}
	}

	private void RectifyBrokenIDs(List<CondOwner> aCOs)
	{
		if (aCOs == null)
		{
			return;
		}
		foreach (CondOwner condOwner in aCOs)
		{
			if (!(condOwner == null))
			{
				if (condOwner.HasCond("IsRoom") && condOwner.gameObject != null)
				{
					this.RemoveCO(condOwner, false);
					UnityEngine.Object.Destroy(condOwner.gameObject);
				}
				else if (condOwner.mapGUIPropMaps != null)
				{
					foreach (Dictionary<string, string> dictionary in condOwner.mapGUIPropMaps.Values)
					{
						if (dictionary != null)
						{
							List<string> list = new List<string>();
							foreach (KeyValuePair<string, string> keyValuePair in dictionary)
							{
								if (keyValuePair.Value != null)
								{
									if (this.mapIDRemap.ContainsKey(keyValuePair.Value))
									{
										string value = keyValuePair.Value;
										string item = this.mapIDRemap[value];
										list.Add(keyValuePair.Key);
										list.Add(item);
									}
								}
							}
							for (int i = 0; i < list.Count; i += 2)
							{
								dictionary[list[i]] = list[i + 1];
							}
							list.Clear();
							list = null;
						}
					}
					Dictionary<string, string> dictionary2 = new Dictionary<string, string>();
					if (condOwner.mapGUIPropMaps.TryGetValue("Electrical", out dictionary2))
					{
						string text = null;
						if (dictionary2.TryGetValue("inputConnections", out text))
						{
							string[] array = text.Split(new char[]
							{
								','
							});
							text = string.Empty;
							bool flag = false;
							foreach (string text2 in array)
							{
								if (!(text2 == string.Empty) && text2 != null)
								{
									ElectricalConnection electricalConnection = ElectricalConnection.FromString(text2);
									if (this.mapIDRemap.ContainsKey(electricalConnection.originID))
									{
										electricalConnection.originID = this.mapIDRemap[electricalConnection.originID];
									}
									if (flag)
									{
										text += ",";
									}
									else
									{
										flag = true;
									}
									text += electricalConnection.ToString();
								}
							}
							dictionary2["inputConnections"] = text;
						}
						text = null;
						if (dictionary2.TryGetValue("outputConnections", out text))
						{
							string[] array3 = text.Split(new char[]
							{
								','
							});
							text = string.Empty;
							bool flag2 = false;
							foreach (string text3 in array3)
							{
								if (!(text3 == string.Empty) && text3 != null)
								{
									ElectricalConnection electricalConnection2 = ElectricalConnection.FromString(text3);
									if (this.mapIDRemap.ContainsKey(electricalConnection2.originID))
									{
										electricalConnection2.originID = this.mapIDRemap[electricalConnection2.originID];
									}
									if (flag2)
									{
										text += ",";
									}
									else
									{
										flag2 = true;
									}
									text += electricalConnection2.ToString();
								}
							}
							dictionary2["outputConnections"] = text;
						}
					}
				}
			}
		}
	}

	private void SpawnItems(List<JsonItem> aItemsPlusCrew, bool bTemplateOnly, Ship.Loaded nLoad, ref Dictionary<string, CondOwner> dictPlaceholders, ref List<CondOwner> aLootSpawners)
	{
		List<JsonItem> list = new List<JsonItem>();
		foreach (JsonItem jsonItem in aItemsPlusCrew)
		{
			if (!DataHandler.mapCOs.ContainsKey(jsonItem.strID))
			{
				if (!bTemplateOnly && !DataHandler.dictCOSaves.ContainsKey(jsonItem.strID))
				{
					Debug.LogError(string.Concat(new string[]
					{
						"ERROR: Trying to load a CO (",
						jsonItem.strName,
						") with missing save data for ship: ",
						this.strRegID,
						": ",
						jsonItem.strID,
						". Skipping."
					}));
				}
				else if (jsonItem.strParentID != null || jsonItem.strSlotParentID != null)
				{
					if (!bTemplateOnly || jsonItem.ForceLoad())
					{
						list.Add(jsonItem);
					}
				}
				else
				{
					string strIDTemp = jsonItem.strID;
					if (bTemplateOnly && !CrewSim.bShipEdit)
					{
						strIDTemp = null;
					}
					GameObject gameObject = this.CreatePart(jsonItem, strIDTemp, bTemplateOnly);
					if (!(gameObject == null))
					{
						CondOwner condOwner = gameObject.GetComponent<CondOwner>();
						if (bTemplateOnly)
						{
							this.mapIDRemap[jsonItem.strID] = condOwner.strID;
						}
						bool bTiles = nLoad > Ship.Loaded.Shallow;
						if (condOwner.mapGUIPropMaps != null && condOwner.mapGUIPropMaps.ContainsKey(GUITradeBase.ASYNCIDENTIFIER))
						{
							bTiles = false;
							condOwner.mapGUIPropMaps.Remove(GUITradeBase.ASYNCIDENTIFIER);
						}
						bool flag = condOwner.HasCond("IsPlaceholder");
						if (!flag || this.aTiles.Count > 0)
						{
							this.AddCO(condOwner, bTiles, flag);
						}
						if (condOwner.HasCond("IsLootSpawner"))
						{
							aLootSpawners.Add(condOwner);
							condOwner.ClaimShip(this.strRegID);
						}
						else if (flag)
						{
							dictPlaceholders[condOwner.strID] = condOwner;
						}
					}
				}
			}
		}
		int num = -1;
		int num2 = -1;
		while (list.Count > 0)
		{
			if (num2 < 0)
			{
				if (num == 0)
				{
					Debug.Log(string.Concat(new object[]
					{
						"WARNING: ",
						list.Count,
						" unprocessed sub items on ship ",
						this.strRegID
					}));
					break;
				}
				num2 = list.Count - 1;
				num = 0;
			}
			JsonItem jsonItem2 = list[num2];
			num2--;
			string text = jsonItem2.strParentID;
			if (text == null)
			{
				text = jsonItem2.strSlotParentID;
			}
			CondOwner condOwner;
			if (this.mapICOs.ContainsKey(text))
			{
				condOwner = this.mapICOs[text];
			}
			else
			{
				if (!this.mapIDRemap.ContainsKey(text))
				{
					continue;
				}
				condOwner = this.mapICOs[this.mapIDRemap[text]];
			}
			bool flag2 = jsonItem2.ForceLoad() || condOwner.pspec != null;
			string strIDTemp2 = jsonItem2.strID;
			if (bTemplateOnly && !flag2 && !CrewSim.bShipEdit)
			{
				strIDTemp2 = null;
			}
			GameObject gameObject = this.CreatePart(jsonItem2, strIDTemp2, bTemplateOnly);
			if (!(gameObject == null))
			{
				CondOwner component = gameObject.GetComponent<CondOwner>();
				if (bTemplateOnly)
				{
					this.mapIDRemap[jsonItem2.strID] = component.strID;
				}
				component.tf.localPosition = new Vector3(condOwner.tf.position.x, condOwner.tf.position.y, Container.fZSubOffset);
				bool flag3 = true;
				if (component.mapGUIPropMaps != null && component.mapGUIPropMaps.ContainsKey(GUITradeBase.ASYNCIDENTIFIER))
				{
					component.mapGUIPropMaps.Remove(GUITradeBase.ASYNCIDENTIFIER);
				}
				if (jsonItem2.strSlotParentID != null)
				{
					if (condOwner.compSlots == null)
					{
						Debug.LogError(string.Concat(new string[]
						{
							"ERROR: Attempting to slot ",
							component.strCODef,
							" - ",
							component.strID,
							" into parent with no slot: ",
							condOwner.strCODef,
							" - ",
							condOwner.strID
						}));
						flag3 = false;
					}
					else if (component.jCOS == null)
					{
						Debug.LogError(string.Concat(new string[]
						{
							"ERROR: Attempting to slot ",
							component.strCODef,
							" - ",
							component.strID,
							" but it has no jCOS data!"
						}));
						flag3 = false;
					}
					else if (!condOwner.compSlots.SlotItem(component.jCOS.strSlotName, component, true))
					{
						continue;
					}
				}
				else if (condOwner.objContainer != null && !condOwner.objContainer.Contains(component))
				{
					bool bAllowStacking = condOwner.objContainer.bAllowStacking;
					condOwner.objContainer.bAllowStacking = false;
					condOwner.objContainer.AddCOSimple(component, component.pairInventoryXY);
					condOwner.objContainer.bAllowStacking = bAllowStacking;
				}
				if (flag3)
				{
					this.mapICOs[component.strID] = component;
				}
				list.Remove(jsonItem2);
				num++;
			}
		}
	}

	public void SyncFuel()
	{
		double rcsremain = this.GetRCSRemain();
		if (rcsremain > this.fShallowRCSRemass)
		{
			this.RemoveGasMass(Mathf.Abs((float)this.fShallowRCSRemass - (float)rcsremain));
		}
	}

	private void AddMarketActorConfigToShip(CondOwner marketActorCO)
	{
		if (marketActorCO == null)
		{
			return;
		}
		string marketConfig = MarketActor.GetMarketConfig(marketActorCO);
		if (!string.IsNullOrEmpty(marketConfig))
		{
			this.MarketConfigs[marketActorCO.strID] = marketConfig;
		}
	}

	private void RemoveMarketActorConfigFromShip(CondOwner marketActorCO)
	{
		if (marketActorCO == null)
		{
			return;
		}
		if (this.MarketConfigs.ContainsKey(marketActorCO.strID))
		{
			this.MarketConfigs.Remove(marketActorCO.strID);
		}
	}

	public double CalculateRCSFuelConsumption(double dVdiff)
	{
		double num = dVdiff * (double)this.fRCSCount * 0.7279999852180481 / this.RCSAccelMax;
		return (num >= 0.01) ? num : 0.0;
	}

	public double CalculateTorchFuelConsumption(double dVdiff, float fLimiter)
	{
		double num = dVdiff / (double)this.GetMaxTorchThrust(fLimiter);
		return (num >= 0.01) ? num : 0.0;
	}

	protected void SetZoneData(JsonZone[] aZones)
	{
		if (aZones == null || this.aTiles.Count == 0)
		{
			return;
		}
		foreach (JsonZone jsonZone in aZones)
		{
			JsonZone jsonZone2 = jsonZone.Clone();
			jsonZone2.strRegID = this.strRegID;
			List<Tile> list = new List<Tile>();
			for (int j = 0; j < jsonZone2.aTiles.Length; j++)
			{
				int num = jsonZone2.aTiles[j];
				if (num >= this.aTiles.Count)
				{
					Debug.LogWarning("Zone tile index was bigger than available tiles on ship, Ship: " + this.strRegID + " Zonename: " + jsonZone2.strName);
					return;
				}
				if (this.aTiles[num] != null)
				{
					list.Add(this.aTiles[num]);
				}
			}
			foreach (Tile tile in list)
			{
				tile.SetZone(jsonZone2, null);
			}
			this.mapZones[jsonZone.strName] = jsonZone2;
		}
	}

	// Finalizes CondOwner state after all ship parts exist.
	// This unfreezes condition updates, restores shifts/appearance, and performs
	// load-level-specific cleanup once references between ship objects are valid.
	private void PostGameLoad(List<CondOwner> aCOsLoaded, Ship.Loaded nLoad)
	{
		foreach (CondOwner condOwner in aCOsLoaded)
		{
			condOwner.PostGameLoad(nLoad);
		}
		foreach (CondOwner condOwner2 in aCOsLoaded)
		{
			condOwner2.bFreezeConds = false;
			condOwner2.bFreezeCondRules = false;
			if (condOwner2.Company != null)
			{
				condOwner2.ShiftChange(JsonCompany.NullShift, true);
				condOwner2.ShiftChange(condOwner2.Company.GetShift(StarSystem.nUTCHour, condOwner2), true);
			}
			if (nLoad == Ship.Loaded.Full)
			{
				condOwner2.UpdateAppearance();
			}
		}
		if (this.ShipCO != null)
		{
			this.ShipCO.PostGameLoad(nLoad);
			this.ShipCO.bFreezeConds = false;
			this.ShipCO.bFreezeCondRules = false;
		}
		if (nLoad == Ship.Loaded.Full)
		{
			this.UpdateGravAndAtmo();
		}
		if (nLoad >= Ship.Loaded.Edit)
		{
			CondTrigger condTrigger = DataHandler.GetCondTrigger("TIsFitContainerSOC");
			List<CondOwner> cos = this.GetCOs(condTrigger, false, false, false);
			foreach (CondOwner condOwner3 in cos)
			{
				condOwner3.RemoveFromCurrentHome(true);
				condOwner3.Destroy();
			}
		}
	}

	public void PostUpdate()
	{
		if (this.nLoadState >= Ship.Loaded.Edit && (this.Reactor == null || !this.Reactor.HasCond("IsReadyFusion")))
		{
			if (this.bFusionReactorRunning)
			{
				this.bChangedStatus = true;
			}
			this.bFusionReactorRunning = false;
			this.SetThrust(0.0);
		}
	}

	// Likely recomputes environmental simulation after layout or orbit changes.
	// This appears to refresh gravity and atmosphere for rooms and occupants.
	public void UpdateGravAndAtmo()
	{
		Vector2 vector = default(Vector2);
		BodyOrbit boAtmo = null;
		BodyOrbit greatestGravBO = CrewSim.system.GetGreatestGravBO(this.objSS, StarSystem.fEpoch, ref vector, ref boAtmo);
		this.UpdateGravAndAtmo(greatestGravBO, boAtmo, Vector2.zero);
	}

	public void UpdateGravAndAtmo(BodyOrbit boGrav, BodyOrbit boAtmo, Vector2 ptPORGrav)
	{
		if (boGrav == null || StarSystem.fEpoch - this._lastAtmoUpdateTime < 0.33000001311302185)
		{
			return;
		}
		if (boAtmo == null)
		{
			boAtmo = boGrav;
		}
		this._lastAtmoUpdateTime = StarSystem.fEpoch;
		double num = (double)this.objSS.GetRadiusAU() + this.objSS.GetDistance(boAtmo.dXReal, boAtmo.dYReal);
		this.fVisibilityRangeMod = 1f;
		if (num <= boAtmo.GravRadius && num > boAtmo.fParallaxRadius)
		{
			float t = Mathf.InverseLerp((float)boAtmo.fParallaxRadius, (float)boAtmo.GravRadius, (float)num);
			this.fVisibilityRangeMod = Mathf.Lerp((float)boAtmo.fVisibilityRangeMod, (float)boAtmo.fVisibilityRangeModGrav, t);
		}
		else if (num <= boAtmo.fParallaxRadius)
		{
			this.fVisibilityRangeMod = (float)boAtmo.fVisibilityRangeMod;
		}
		if (this.LoadState != Ship.Loaded.Full || this.aTiles.Count == 0)
		{
			return;
		}
		double distanceToBO = (double)this.objSS.GetRadiusAU() + this.objSS.GetDistance(boGrav.dXReal, boGrav.dYReal);
		Tile.GravField gravField = Tile.SetTileGravitationalForces(boGrav, distanceToBO);
		if (gravField > Tile.GravField.None)
		{
			this.TileGravWarnPlayer(gravField);
		}
		JsonAtmosphere atmosphereAtDistance = boAtmo.GetAtmosphereAtDistance(num);
		foreach (Room room in this.aRooms)
		{
			room.SyncAtmoVoid(atmosphereAtDistance);
		}
		bool flag = ptPORGrav.x == 0f && ptPORGrav.y == 0f;
		if (atmosphereAtDistance.fMicrometeoroidChance > 0f && !flag)
		{
			float num2 = MathUtils.Rand(0f, 1f, MathUtils.RandType.Flat, null);
			Point point = boAtmo.vVel - this.objSS.vVel;
			double num3 = MathUtils.GetMagnitude(point.X, point.Y) / 5.013440183831985E-09;
			float fMult = Mathf.Max((float)num3, 0.5f);
			float fMicrometeoroidChance = atmosphereAtDistance.fMicrometeoroidChance;
			if (num2 < fMicrometeoroidChance)
			{
				StarSystem.SpawnMicroMeteoroid(this, fMult, true);
			}
		}
		this.CalculateLiftDrag(ptPORGrav);
		this.CheckRoomPressure();
		if (gravField == Tile.GravField.WeakToStrongTransition && this.aRooms != null)
		{
			TileUtils.DestroyAllEVACOs(this);
		}
	}

	private void CalculateLiftDrag(Vector2 ptPORGrav)
	{
		if (ptPORGrav.x == 0f && ptPORGrav.y == 0f && this.objSS.vAccLift.x == 0f && this.objSS.vAccLift.y == 0f)
		{
			return;
		}
		float num = (this.fAeroCoefficient != 0f) ? this.fAeroCoefficient : 1f;
		double liftCoefficient = (double)num / this.Mass;
		double num2 = (double)(this.nCols + this.nRows) * 0.32 / 2.0;
		float num3 = Mathf.Lerp(3f, 15f, (float)(num2 - 3.0) / 50f);
		double dragCoeffFront = num2 * (double)num3 / Math.Max(1.0, (double)(num / 100f));
		this.objSS.CalculateLiftDrag(liftCoefficient, dragCoeffFront, num2 * (double)num3, this.Mass, ptPORGrav);
	}

	private void TileGravWarnPlayer(Tile.GravField warningLvl)
	{
		if (this.IsStation(false) || this.IsStationHidden(false))
		{
			return;
		}
		CondOwner condOwner = this.aNavs.FirstOrDefault<CondOwner>();
		if (condOwner == null)
		{
			return;
		}
		string strDisplayName = string.Empty;
		if (warningLvl == Tile.GravField.NoneToWeakTransition)
		{
			strDisplayName = DataHandler.GetString("OBJV_GRAV_NONE_TO_WEAK", false);
		}
		else if (warningLvl == Tile.GravField.WeakToStrongTransition)
		{
			strDisplayName = DataHandler.GetString("OBJV_GRAV_WEAK_TO_STRONG", false);
		}
		else if (warningLvl == Tile.GravField.StrongToWeakTransition)
		{
			strDisplayName = DataHandler.GetString("OBJV_GRAV_STRONG_TO_WEAK", false);
		}
		else if (warningLvl == Tile.GravField.WeakToNoneTransition)
		{
			strDisplayName = DataHandler.GetString("OBJV_GRAV_WEAK_TO_NONE", false);
		}
		AlarmObjective objective = new AlarmObjective(AlarmType.nav_gravitation, condOwner, strDisplayName);
		MonoSingleton<ObjectiveTracker>.Instance.AddObjective(objective);
	}

	private void PreFillRooms()
	{
		foreach (Room room in this.aRooms)
		{
			GasContainer gasContainer = room.CO.GasContainer;
			double num = 297.0;
			double num2 = (!room.Void) ? room.CO.GetCondAmount("StatVolume") : 0.0;
			double value = 22.0 * num2 / 0.008314000442624092 / num;
			gasContainer.mapDGasMols["StatGasMolO2"] = value;
			if (gasContainer.mapGasMols1.ContainsKey("StatGasMolO2"))
			{
				Dictionary<string, double> mapDGasMols;
				(mapDGasMols = gasContainer.mapDGasMols)["StatGasMolO2"] = mapDGasMols["StatGasMolO2"] - gasContainer.mapGasMols1["StatGasMolO2"];
			}
			value = 80.0 * num2 / 0.008314000442624092 / num;
			gasContainer.mapDGasMols["StatGasMolN2"] = value;
			if (gasContainer.mapGasMols1.ContainsKey("StatGasMolN2"))
			{
				Dictionary<string, double> mapDGasMols;
				(mapDGasMols = gasContainer.mapDGasMols)["StatGasMolN2"] = mapDGasMols["StatGasMolN2"] - gasContainer.mapGasMols1["StatGasMolN2"];
			}
			if (room.Void)
			{
				gasContainer.fDGasTemp = 2.725480079650879 - gasContainer.fDGasTemp;
			}
			else
			{
				gasContainer.fDGasTemp = num - gasContainer.fDGasTemp;
			}
			if (double.IsNaN(gasContainer.fDGasTemp))
			{
				Debug.Log("fDGasTemp NaN");
			}
		}
	}

	// Ship log serialization helper used by UI/history and save data.
	public List<JsonShipLog> LogGet()
	{
		if (this.aLog == null)
		{
			this.aLog = new List<JsonShipLog>();
		}
		return this.aLog;
	}

	// Adds a new ship log entry for events such as docking, combat, or service changes.
	public void LogAdd(string strEntry, double fEpoch = 0.0, bool bShowEpoch = false)
	{
		if (!this._bDoneLoading || string.IsNullOrEmpty(strEntry))
		{
			return;
		}
		if (this.aLog == null)
		{
			this.aLog = new List<JsonShipLog>();
		}
		JsonShipLog jsonShipLog = new JsonShipLog();
		jsonShipLog.strEntry = strEntry;
		jsonShipLog.fEpoch = fEpoch;
		jsonShipLog.bShowEpoch = bShowEpoch;
		this.aLog.Add(jsonShipLog);
		int num = 1000;
		if (this.aLog.Count > num)
		{
			this.aLog.RemoveRange(0, this.aLog.Count - num);
		}
	}

	public List<JsonShipLog> LogGetHeader()
	{
		List<JsonShipLog> list = new List<JsonShipLog>();
		int num = 0;
		if (!int.TryParse(this.year, out num))
		{
			num = MathUtils.GetYearFromS(StarSystem.fEpoch) - 1;
			this.year = num.ToString();
		}
		double dfAmount = (double)((float)num * 31556926f) + MathUtils.Rand(0.0, 31556926.0, MathUtils.RandType.Flat, null);
		list.Add(JsonShipLog.Make("Vessel Name: " + this.publicName, 0.0, false));
		list.Add(JsonShipLog.Make("REGID: " + this.strRegID, 1.0, false));
		list.Add(JsonShipLog.Make("Date of Construction: " + MathUtils.GetUTCFromS(dfAmount), 2.0, false));
		list.Add(JsonShipLog.Make("Make: " + this.make, 3.0, false));
		list.Add(JsonShipLog.Make("Model: " + this.model, 4.0, false));
		list.Add(JsonShipLog.Make("Homeport: " + this.origin, 5.0, false));
		list.Add(JsonShipLog.Make("Designation: " + this.designation, 6.0, false));
		list.Add(JsonShipLog.Make("Total Mass: " + this.Mass.ToString("N0") + " kg", 7.0, false));
		list.Add(JsonShipLog.Make("-- -- --", 8.0, false));
		CondTrigger objCondTrig = new CondTrigger("PIN Locked Doors", new string[]
		{
			"IsLockPIN"
		}, new string[0], new string[0], new string[0]);
		List<CondOwner> icos = this.GetICOs1(objCondTrig, false, false, true);
		foreach (CondOwner condOwner in icos)
		{
			Dictionary<string, string> dictionary = null;
			condOwner.mapGUIPropMaps.TryGetValue("Panel A", out dictionary);
			if (dictionary != null && dictionary.ContainsKey("strPIN"))
			{
				string text = condOwner.strNameFriendly + DataHandler.GetString("GUI_NAV_LOGS_PIN", false);
				text += dictionary["strPIN"];
				list.Add(JsonShipLog.Make(text, 20.0, false));
			}
		}
		return list;
	}

	public void Sparks()
	{
		if (this.nLoadState < Ship.Loaded.Edit || CrewSim.Paused)
		{
			return;
		}
		if (DataHandler.GetUserSettings().nFlickerAmount < 0)
		{
			return;
		}
		float num = 0.02f;
		num += num * CrewSim.TimeElapsedScaled();
		num *= (float)this.mapICOs.Count / 1200f;
		if (MathUtils.Rand(0.0, 1.0, MathUtils.RandType.Flat, null) > (double)num)
		{
			return;
		}
		Ship.ctSparkable.logReason = false;
		if (this.aSparkables == null || this.aSparkables.Count == 0)
		{
			this.aSparkables = this.BuildSparkables();
		}
		while (this.aSparkables.Count > 0)
		{
			CondOwner condOwner = this.aSparkables[0];
			this.aSparkables.RemoveAt(0);
			if (!(condOwner == null) && Ship.ctSparkable.Triggered(condOwner, null, true))
			{
				Tile tileAtWorldCoords = this.GetTileAtWorldCoords1(condOwner.tf.position.x, condOwner.tf.position.y, false, true);
				if (tileAtWorldCoords == null || tileAtWorldCoords.aConnectedPowerCOs.Count == 0)
				{
					break;
				}
				double num2 = 1.0 - condOwner.GetDamageState();
				if (num2 < 0.5)
				{
					break;
				}
				CrewSim.vfxSparks.AddSparkAt(condOwner.tf.position);
				float num3 = 400f;
				num2 = (double)Mathf.Min(0.01f, Convert.ToSingle(condOwner.GetCondAmount("StatDamageMax")) / (num3 * 2f));
				condOwner.AddCondAmount("StatDamage", num2, 0.0, 0f);
				break;
			}
		}
	}

	private List<CondOwner> BuildSparkables()
	{
		List<CondOwner> cos = this.GetCOs(Ship.ctSparkable, false, false, false);
		List<CondOwner> list = new List<CondOwner>();
		for (int i = 0; i < cos.Count; i++)
		{
			int index = UnityEngine.Random.Range(0, cos.Count);
			list.Add(cos[index]);
		}
		return list;
	}

	public void DamageOverTime()
	{
		if (this.fLastWearEpoch == 0.0)
		{
			this.fLastWearEpoch = StarSystem.fEpoch;
			return;
		}
		if (StarSystem.fEpoch - this.fLastWearEpoch < 300.0)
		{
			return;
		}
		double num = 1.5844382307706396E-09 * (StarSystem.fEpoch - this.fLastWearEpoch);
		if (this.nLoadState < Ship.Loaded.Edit)
		{
			this.AccrueWear((float)num);
		}
		else
		{
			this.DamageAllCOs((float)num, true, null);
		}
		this.fLastWearEpoch = StarSystem.fEpoch;
	}

	private void UpdateConstructionProgress()
	{
		int num = (int)((StarSystem.fEpoch - this.fFirstVisit) / 86400.0);
		this.nConstructionProgress = this.nInitConstructionProgress + num;
		if (this.nConstructionProgress > 100)
		{
			this.nConstructionProgress = 100;
		}
		this.json.nConstructionProgress = this.nConstructionProgress;
	}

	private List<JsonItem> Reconstruct()
	{
		if (this.json == null || this.nConstructionProgress <= 0 || this.nConstructionProgress >= 100)
		{
			return new List<JsonItem>();
		}
		int nProgress = DataHandler.GetShipConstructionTemplate(this.json).nProgress;
		this.UpdateConstructionProgress();
		JsonShipConstructionTemplate shipConstructionTemplate = DataHandler.GetShipConstructionTemplate(this.json);
		if (shipConstructionTemplate.nProgress == nProgress)
		{
			return new List<JsonItem>();
		}
		Dictionary<string, bool> dictionary = new Dictionary<string, bool>();
		List<JsonItem> list = new List<JsonItem>();
		JsonItem jsonItem = null;
		List<JsonItem> list2 = shipConstructionTemplate.aItems.ToList<JsonItem>();
		if (shipConstructionTemplate.aShallowPSpecs != null)
		{
			list2.AddRange(shipConstructionTemplate.aShallowPSpecs);
		}
		foreach (JsonItem jsonItem2 in list2)
		{
			bool flag = false;
			if (!dictionary.TryGetValue(jsonItem2.strName, out flag))
			{
				DataCO dataCO = DataHandler.GetDataCO(jsonItem2.strName);
				if (dataCO == null)
				{
					continue;
				}
				flag = (dataCO.HasCond("IsInstalled") || dataCO.HasCond("IsLootSpawner"));
				dictionary[jsonItem2.strName] = flag;
				if (jsonItem == null && dataCO.HasCond("IsDockSys"))
				{
					jsonItem = jsonItem2;
				}
			}
			if (flag)
			{
				list.Add(jsonItem2.Clone());
			}
		}
		if (jsonItem == null)
		{
			Debug.Log("Could not find docksys on template, aborting reconstruction of " + this.strRegID);
			return new List<JsonItem>();
		}
		float num = 0f;
		float num2 = 0f;
		int rotateCWCount = 0;
		foreach (JsonItem jsonItem3 in this.json.aItems)
		{
			if (!(jsonItem3.strName != jsonItem.strName))
			{
				if ((int)jsonItem3.fRotation != (int)jsonItem.fRotation)
				{
					int num3 = (int)jsonItem.fRotation;
					num = jsonItem.fX;
					num2 = jsonItem.fY;
					int num4 = 1;
					while (num4 < 4 && num3 != (int)jsonItem3.fRotation)
					{
						num3 += 90;
						num3 %= 360;
						rotateCWCount = num4;
						float num5 = num2;
						float num6 = -num;
						num = num5;
						num2 = num6;
						num4++;
					}
				}
				num = jsonItem3.fX - num;
				num2 = jsonItem3.fY - num2;
				break;
			}
		}
		List<JsonItem> list3 = new List<JsonItem>();
		foreach (JsonItem item in this.json.aItems)
		{
			list3.Add(item);
		}
		List<JsonItem> list4 = new List<JsonItem>();
		foreach (JsonItem jsonItem4 in list)
		{
			bool flag2 = false;
			jsonItem4.Translate(num, num2, rotateCWCount);
			foreach (JsonItem jsonItem5 in list3)
			{
				if (jsonItem5.Matches(jsonItem4))
				{
					flag2 = true;
					list3.Remove(jsonItem5);
					break;
				}
			}
			if (!flag2)
			{
				list4.Add(jsonItem4);
			}
		}
		return list4;
	}

	public void DebugBreakIn()
	{
		this.BreakIn();
		this.VisualizeOverlays(false);
	}

	private void BreakIn()
	{
		bool bIgnoreCOTrans = AudioManager.bIgnoreCOTrans;
		AudioManager.bIgnoreCOTrans = true;
		if (CrewSim.coPlayer != null && CrewSim.coPlayer.HasCond("IsDueBonusDerelict"))
		{
			float num = (float)(CrewSim.coPlayer.GetCondAmount("IsDueBonusDerelict") + 1.0);
			this.fBreakInMultiplier /= num;
			CrewSim.coPlayer.ZeroCondAmount("IsDueBonusDerelict");
		}
		List<CondOwner> list = new List<CondOwner>();
		CondTrigger condTrigger = DataHandler.GetCondTrigger("TIsDerelictSafe");
		float fChance = Ship.ctDerelictSafe.fChance;
		condTrigger.fChance = Mathf.Lerp(0.5f, 1f, fChance * this.fBreakInMultiplier);
		int num2 = 0;
		while ((float)num2 < this.fBreakInMultiplier * 10f)
		{
			Tile randomTile = this.GetRandomTile1(true, false);
			int num3 = Mathf.RoundToInt(MathUtils.Rand(this.fBreakInMultiplier * 2.5f, this.fBreakInMultiplier * 5f, MathUtils.RandType.Flat, null));
			if (num3 < 1)
			{
				num3 = 1;
			}
			list = this.GetCOsInZone(TileUtils.GetZoneFromTileRadius(this, randomTile.tf.position, num3, false, true), condTrigger, true, false);
			foreach (CondOwner condOwner in list)
			{
				if (condOwner.ship == this)
				{
					condOwner.RemoveFromCurrentHome(false);
					condOwner.Destroy();
				}
			}
			num2++;
		}
		condTrigger.fChance = fChance;
		fChance = Ship.ctDerelictSafe.fChance;
		Ship.ctDerelictSafe.fChance = Mathf.Lerp(0.5f, 1f, fChance * this.fBreakInMultiplier);
		list = this.GetCOs(Ship.ctDerelictSafe, true, false, true);
		Ship.ctDerelictSafe.fChance = fChance;
		foreach (CondOwner condOwner2 in list)
		{
			if (condOwner2.ship == this)
			{
				condOwner2.RemoveFromCurrentHome(false);
				condOwner2.Destroy();
			}
		}
		list.Clear();
		CondTrigger condTrigger2 = DataHandler.GetCondTrigger("TIsSwitch01On");
		foreach (CondOwner condOwner3 in this.mapICOs.Values)
		{
			if (condOwner3.HasCond("IsAirtight"))
			{
				foreach (string strName in FusionIC.aReactantNames)
				{
					if (condOwner3.HasCond(strName))
					{
						condOwner3.AddCondAmount(strName, -condOwner3.GetCondAmount(strName) * (double)MathUtils.Rand(0f, 1f, MathUtils.RandType.Flat, null), 0.0, 0f);
					}
				}
			}
			else if (condOwner3.HasCond("StatPower"))
			{
				condOwner3.SetCondAmount("StatPower", MathUtils.Rand(0.0, condOwner3.GetCondAmount("StatPower"), MathUtils.RandType.Flat, null), 0.0);
			}
			else if (condOwner3.HasCond("IsDoor01"))
			{
				list.Add(condOwner3);
			}
			else if (condOwner3.HasCond("IsPowerConduit") && condOwner3.HasCond("IsCollectiveElectric"))
			{
				list.Add(condOwner3);
			}
			else if (this.DMGStatus == Ship.Damage.Derelict)
			{
				if (condOwner3.HasCond("IsReactorIC"))
				{
					condOwner3.GetComponent<FusionIC>().SetDerelict();
				}
				else if (condTrigger2.Triggered(condOwner3, null, true))
				{
					list.Add(condOwner3);
				}
			}
		}
		Loot loot = DataHandler.GetLoot("ItmRandomDerelictConduit");
		Loot loot2 = DataHandler.GetLoot("ItmRandomDerelictDoor");
		Loot loot3 = DataHandler.GetLoot("ItmSwitch01Off");
		foreach (CondOwner condOwner4 in list)
		{
			if (condOwner4.HasCond("IsDoor01"))
			{
				List<CondOwner> coloot = loot2.GetCOLoot(null, false, null);
				if (coloot.Count > 0)
				{
					condOwner4.ModeSwitch(coloot[0], condOwner4.tf.position);
				}
			}
			else if (condOwner4.HasCond("IsPowerConduit") && condOwner4.HasCond("IsCollectiveElectric"))
			{
				List<CondOwner> coloot2 = loot.GetCOLoot(null, false, null);
				if (coloot2.Count > 0)
				{
					condOwner4.ModeSwitch(coloot2[0], condOwner4.tf.position);
				}
			}
			else if (condTrigger2.Triggered(condOwner4, null, true))
			{
				List<CondOwner> coloot3 = loot3.GetCOLoot(null, false, null);
				if (coloot3.Count > 0)
				{
					condOwner4.ModeSwitch(coloot3[0], condOwner4.tf.position);
				}
			}
		}
		float num4 = Mathf.Lerp(0.33f, 1.1f, this.fBreakInMultiplier);
		this.DamageAllCOs(num4, true, null);
		this.DamageAllCOs(num4 * 3f, true, Ship.ctDerelictSafe);
		int num5 = MathUtils.Rand((int)(this.fBreakInMultiplier * 4f), (int)(this.fBreakInMultiplier * 10f), MathUtils.RandType.Flat, null);
		if (num5 < 1)
		{
			num5 = 1;
		}
		JsonAttackMode attackMode = DataHandler.GetAttackMode("AModeDerelictBreakIn");
		for (int j = 0; j < num5; j++)
		{
			if (attackMode == null)
			{
				break;
			}
			this.DamageRayRandom(attackMode, this.fBreakInMultiplier, null, false);
		}
		if (CrewSim.coPlayer != null)
		{
			if (CrewSim.coPlayer.GetCondAmount("StatNoMeat") >= 1.0)
			{
				CrewSim.coPlayer.AddCondAmount("StatNoMeat", -1.0, 0.0, 0f);
			}
			else if (CrewSim.coPlayer.HasCond("IsDueMeatProgression"))
			{
				CrewSim.coPlayer.ZeroCondAmount("IsDueMeatProgression");
				CondTrigger condTrigger3 = DataHandler.GetCondTrigger("CTPLOT_Meat_Inert_Ready");
				CondTrigger condTrigger4 = DataHandler.GetCondTrigger("CTPLOT_Meat_Easy_Ready");
				CondTrigger condTrigger5 = DataHandler.GetCondTrigger("CTPLOT_Meat_Med_Ready");
				CondTrigger condTrigger6 = DataHandler.GetCondTrigger("CTPLOT_Meat_Hard_Ready");
				CondTrigger condTrigger7 = DataHandler.GetCondTrigger("CTPLOT_Meat_Awaken");
				float fFuel = (float)CrewSim.coPlayer.GetCondAmount("StatPlotMeat");
				if (CrewSim.eMeatState == MeatState.Dormant && condTrigger7.Triggered(CrewSim.coPlayer, null, true))
				{
					CrewSim.eMeatState = MeatState.Spread;
				}
				if (condTrigger6.Triggered(CrewSim.coPlayer, null, true))
				{
					CrewSim.GetSelectedCrew().LogMessage(CrewSim.GetSelectedCrew().strName + " is suddenly more aware of their own meat and bone.", "Meat", CrewSim.GetSelectedCrew().strID);
					for (int k = 0; k < UnityEngine.Random.Range(3, 9); k++)
					{
						this.SpawnMeat(fFuel);
					}
				}
				else if (condTrigger5.Triggered(CrewSim.coPlayer, null, true))
				{
					CrewSim.GetSelectedCrew().LogMessage(CrewSim.GetSelectedCrew().strName + " has a meaty lump in their throat.", "Meat", CrewSim.GetSelectedCrew().strID);
					this.SpawnMeat(fFuel);
				}
				else if (condTrigger4.Triggered(CrewSim.coPlayer, null, true))
				{
					CrewSim.GetSelectedCrew().LogMessage(CrewSim.GetSelectedCrew().strName + " feels like a meaty smell lingers.", "Meat", CrewSim.GetSelectedCrew().strID);
					for (int l = 0; l < UnityEngine.Random.Range(3, 9); l++)
					{
						this.SpawnRandom("CTPLOT_Meat_Spawnable", "ItmDocumentMeatPackaging");
					}
					for (int m = 0; m < UnityEngine.Random.Range(3, 9); m++)
					{
						this.SpawnRandom("CTPLOT_Meat_Spawnable", "LiquidBloodMeat");
					}
				}
				else if (condTrigger3.Triggered(CrewSim.coPlayer, null, true))
				{
					CrewSim.GetSelectedCrew().LogMessage(CrewSim.GetSelectedCrew().strName + " is craving something meaty.", "Meat", CrewSim.GetSelectedCrew().strID);
				}
				double num6 = CrewSim.coPlayer.GetCondAmount("StatMeatRate");
				if (num6 == 0.0)
				{
					num6 = 1.0;
				}
				CrewSim.coPlayer.AddCondAmount("StatNoMeat", num6, 0.0, 0f);
			}
		}
		list = this.GetCOs(Ship.ctRCSGasCans, true, false, true);
		foreach (CondOwner condOwner5 in list)
		{
			GasContainer gasContainer = condOwner5.GasContainer;
			if (gasContainer != null)
			{
				gasContainer.BreakIn();
			}
		}
		this.CreateRooms(null);
		TileUtils.GetPoweredTiles(this);
		AudioManager.bIgnoreCOTrans = bIgnoreCOTrans;
	}

	public void SpawnMeat(float fFuel)
	{
		if (CrewSim.GetSelectedCrew() != null && CrewSim.GetSelectedCrew().GetCondAmount("StatMeatBlobMax") > 0.0)
		{
			fFuel = Mathf.Min(fFuel, (float)CrewSim.GetSelectedCrew().GetCondAmount("StatMeatBlobMax"));
		}
		else
		{
			fFuel = Mathf.Min(fFuel, 50f);
		}
		CondTrigger condTrigger = DataHandler.GetCondTrigger("CTPLOT_Meat_Spawnable");
		Tile randomTile = this.GetRandomTile1(true, false);
		Tuple<Vector2, Vector2> airlockBounds = TileUtils.GetAirlockBounds(this);
		for (int i = 0; i < 10; i++)
		{
			if (randomTile != null && TileUtils.IsTileAboveAirlock(randomTile, airlockBounds) && condTrigger.Triggered(randomTile.coProps, null, true))
			{
				break;
			}
			randomTile = this.GetRandomTile1(true, false);
		}
		if (randomTile == null || !TileUtils.IsTileAboveAirlock(randomTile, airlockBounds))
		{
			return;
		}
		List<CondOwner> list = new List<CondOwner>();
		list.AddRange(DataHandler.GetLoot("ItmMeat01").GetCOLoot(null, false, null));
		foreach (CondOwner condOwner in list)
		{
			condOwner.tf.position = new Vector3(randomTile.tf.position.x, randomTile.tf.position.y, -2.5f);
			this.AddCO(condOwner, true);
			Meat meat = condOwner.GetComponent<Meat>();
			if (meat == null)
			{
				meat = condOwner.gameObject.AddComponent<Meat>();
			}
			meat.SpreadFast((int)fFuel);
		}
	}

	public void SpawnRandom(string strCT, string strLoot)
	{
		CondTrigger condTrigger = DataHandler.GetCondTrigger(strCT);
		Tile randomTile = this.GetRandomTile1(true, false);
		for (int i = 0; i < 10; i++)
		{
			if (condTrigger.Triggered(randomTile.coProps, null, true))
			{
				break;
			}
			randomTile = this.GetRandomTile1(true, false);
		}
		if (randomTile == null)
		{
			Debug.LogWarning("No tile in SpawnRandom!");
			return;
		}
		List<CondOwner> list = new List<CondOwner>();
		list.AddRange(DataHandler.GetLoot(strLoot).GetCOLoot(null, false, null));
		if (list == null || list.Count == 0)
		{
			Debug.LogWarning("No loot in SpawnRandom!");
			return;
		}
		foreach (CondOwner condOwner in list)
		{
			condOwner.tf.position = new Vector3(randomTile.tf.position.x, randomTile.tf.position.y, -2.5f);
			this.AddCO(condOwner, true);
		}
	}

	public void DamageAllCOs(float fMaxAmount, bool allowMultiple = false, CondTrigger ct = null)
	{
		if (ct == null)
		{
			ct = DataHandler.GetCondTrigger("Blank");
		}
		List<CondOwner> cos = this.GetCOs(ct, true, false, true);
		CondOwner selectedCrew = CrewSim.GetSelectedCrew();
		foreach (CondOwner condOwner in cos)
		{
			condOwner.BreakIn(fMaxAmount, allowMultiple, 0.0);
			if (selectedCrew != null && selectedCrew.ship == condOwner.ship && condOwner.GetDamageState() <= 0.0)
			{
				BeatManager.ResetTensionTimer();
			}
		}
	}

	public void DamageAllCOsTrend(float fMaxAmount, CondTrigger ct = null, double fRepairTarget = 0.0)
	{
		if (ct == null)
		{
			ct = DataHandler.GetCondTrigger("Blank");
		}
		List<CondOwner> cos = this.GetCOs(ct, true, false, true);
		CondOwner selectedCrew = CrewSim.GetSelectedCrew();
		foreach (CondOwner condOwner in cos)
		{
			condOwner.BreakIn(fMaxAmount, true, fRepairTarget);
			if (selectedCrew != null && selectedCrew.ship == condOwner.ship && condOwner.GetDamageState() <= 0.0)
			{
				BeatManager.ResetTensionTimer();
			}
		}
	}

	public void CheckLocks()
	{
		CondOwner[] array = new CondOwner[this.aLocks.Count];
		this.aLocks.CopyTo(array);
		foreach (CondOwner condOwner in array)
		{
			if (condOwner.HasCond("IsLockPIN"))
			{
				Dictionary<string, string> dictionary = null;
				condOwner.mapGUIPropMaps.TryGetValue("Panel A", out dictionary);
				if (this.bResetLocks || (dictionary != null && !dictionary.ContainsKey("strPIN")))
				{
					StringBuilder stringBuilder = new StringBuilder();
					stringBuilder.Append(MathUtils.Rand(0, 10, MathUtils.RandType.Flat, null));
					stringBuilder.Append(MathUtils.Rand(0, 10, MathUtils.RandType.Flat, null));
					stringBuilder.Append(MathUtils.Rand(0, 10, MathUtils.RandType.Flat, null));
					stringBuilder.Append(MathUtils.Rand(0, 10, MathUtils.RandType.Flat, null));
					dictionary["strPIN"] = stringBuilder.ToString();
				}
			}
		}
		this.aLocks.Clear();
		this.bCheckLocks = false;
		this.bResetLocks = false;
	}

	public void CheckTargets()
	{
		if (this.json.strScanTargetID != null)
		{
			this.shipScanTarget = CrewSim.system.GetShipByRegID(this.json.strScanTargetID);
		}
		if (this.json.strStationKeepingTargetID != null)
		{
			this.shipStationKeepingTarget = CrewSim.system.GetShipByRegID(this.json.strStationKeepingTargetID);
		}
		if (this.json.strUndockID != null)
		{
			this.shipUndock = CrewSim.system.GetShipByRegID(this.json.strUndockID);
		}
		if (this.json.objSituScanTarget != null)
		{
			this.shipSituTarget = new ShipSitu(this.json.objSituScanTarget);
		}
		this.bCheckTargets = false;
	}

	public PersonSpec GetPerson(JsonPersonSpec jps, global::Social soc, bool bForceUnrelated, List<string> aForbids = null)
	{
		if (jps == null)
		{
			return null;
		}
		CondTrigger condTrigger = DataHandler.GetCondTrigger(jps.strCT);
		PersonSpec personSpec = null;
		if (soc != null)
		{
			CondOwner component = soc.GetComponent<CondOwner>();
			personSpec = component.pspec;
		}
		if (this.nLoadState < Ship.Loaded.Shallow || this.aPeople == null)
		{
			return null;
		}
		List<PersonSpec> list = null;
		foreach (PersonSpec personSpec2 in this.aPeople)
		{
			if (personSpec2 != null && personSpec != personSpec2)
			{
				if (aForbids == null || !aForbids.Contains(personSpec2.FullName))
				{
					if (bForceUnrelated && soc != null)
					{
						string strCTRelFind = jps.strCTRelFind;
						jps.strCTRelFind = "TRELStranger";
						bool flag = false;
						if (soc.HasPerson(personSpec2))
						{
							flag = true;
						}
						else if (personSpec != null && !personSpec.IsCOMyMother(jps, personSpec2.GetCO()))
						{
							flag = true;
						}
						jps.strCTRelFind = strCTRelFind;
						if (flag)
						{
							continue;
						}
					}
					else if (personSpec != null)
					{
						if (!personSpec.IsCOMyMother(jps, personSpec2.GetCO()))
						{
							continue;
						}
					}
					else if (!jps.Matches(personSpec2.GetCO()))
					{
						continue;
					}
					if (list == null)
					{
						list = new List<PersonSpec>();
					}
					list.Add(personSpec2);
				}
			}
		}
		if (list == null)
		{
			return null;
		}
		return list[MathUtils.Rand(0, list.Count - 1, MathUtils.RandType.Flat, null)];
	}

	public List<CondOwner> GetPeopleInRoom(Room room, CondTrigger ct = null)
	{
		List<CondOwner> list = new List<CondOwner>();
		if (room != null)
		{
			for (int i = this.aPeople.Count - 1; i >= 0; i--)
			{
				PersonSpec personSpec = this.aPeople[i];
				CondOwner condOwner = personSpec.MakeCondOwner(PersonSpec.StartShip.OLD, this);
				if (condOwner.currentRoom == room)
				{
					if (ct == null || ct.Triggered(condOwner, null, true))
					{
						if (list.IndexOf(condOwner) < 0)
						{
							list.Add(condOwner);
						}
					}
				}
			}
		}
		return list;
	}

	public List<CondOwner> GetPeople(bool bAllowDocked)
	{
		List<CondOwner> list = new List<CondOwner>();
		if (this.aPeople != null)
		{
			for (int i = this.aPeople.Count - 1; i >= 0; i--)
			{
				PersonSpec personSpec = this.aPeople[i];
				list.Add(personSpec.MakeCondOwner(PersonSpec.StartShip.OLD, this));
			}
		}
		if (bAllowDocked && this.aDocked != null)
		{
			foreach (Ship ship in this.aDocked)
			{
				if (ship != null)
				{
					list.AddRange(ship.GetPeople(false));
				}
			}
		}
		return list;
	}

	public int Population
	{
		get
		{
			if (this.aPeople != null)
			{
				return this.aPeople.Count;
			}
			return 0;
		}
	}

	public double MaxPopulation
	{
		get
		{
			return this.ShipCO.GetCondAmount("StationMaxPopulation", false);
		}
	}

	public static string GenerateID(string strColony = null)
	{
		string text = string.Empty;
		string text2 = "HVEMBOJS";
		string text3 = "ABCDEFGHJKLMNPQRSTUVWXYZ0123456789";
		if (strColony == null)
		{
			text += text2.Substring(UnityEngine.Random.Range(0, text2.Length - 1), 1);
		}
		else
		{
			text += strColony;
		}
		text += "-";
		text += text3.Substring(UnityEngine.Random.Range(0, text3.Length - 1), 1);
		text += text3.Substring(UnityEngine.Random.Range(0, text3.Length - 1), 1);
		text += text3.Substring(UnityEngine.Random.Range(0, text3.Length - 1), 1);
		if (MathUtils.Rand(0.0, 1.0, MathUtils.RandType.Flat, null) < 0.5)
		{
			text += text3.Substring(UnityEngine.Random.Range(0, text3.Length - 1), 1);
		}
		if (MathUtils.Rand(0.0, 1.0, MathUtils.RandType.Flat, null) < 0.1)
		{
			text += text3.Substring(UnityEngine.Random.Range(0, text3.Length - 1), 1);
		}
		if (CrewSim.system != null && CrewSim.system.dictShips.ContainsKey(text))
		{
			return Ship.GenerateID(null);
		}
		return text;
	}

	public GameObject CreatePart(JsonItem objItem, string strIDTemp, bool bLoot)
	{
		if (objItem == null)
		{
			return null;
		}
		CondOwner condOwner = null;
		if (strIDTemp != null)
		{
			if (this.mapICOs.ContainsKey(strIDTemp) && this.mapICOs[strIDTemp] != null)
			{
				condOwner = this.mapICOs[strIDTemp];
			}
			else if (DataHandler.mapCOs.ContainsKey(strIDTemp) && DataHandler.mapCOs[strIDTemp] != null)
			{
				condOwner = DataHandler.mapCOs[strIDTemp];
			}
		}
		if (condOwner == null)
		{
			condOwner = DataHandler.GetCondOwner(objItem.strName, strIDTemp, null, bLoot, null, null, null, null);
			if (condOwner != null && strIDTemp != null)
			{
				condOwner.strID = strIDTemp;
			}
		}
		if (condOwner == null)
		{
			return null;
		}
		Item item = condOwner.Item;
		GameObject result = condOwner.gameObject;
		condOwner.tf.position = new Vector3(objItem.fX, objItem.fY, condOwner.tf.position.z);
		if (item != null)
		{
			item.fLastRotation = objItem.fRotation;
		}
		else
		{
			condOwner.tf.rotation = Quaternion.Euler(0f, 0f, objItem.fRotation);
		}
		if (objItem.aGPMSettings != null)
		{
			for (int i = 0; i < objItem.aGPMSettings.Length; i++)
			{
				string strName = objItem.aGPMSettings[i].strName;
				Dictionary<string, string> dictionary = DataHandler.ConvertStringArrayToDict(objItem.aGPMSettings[i].dictGUIPropMap, null);
				foreach (KeyValuePair<string, string> keyValuePair in dictionary)
				{
					if (!condOwner.mapGUIPropMaps.ContainsKey(strName))
					{
						condOwner.mapGUIPropMaps[strName] = new Dictionary<string, string>();
					}
					condOwner.mapGUIPropMaps[strName][keyValuePair.Key] = keyValuePair.Value;
				}
				if (strName == "Overrides" && condOwner.mapGUIPropMaps[strName].ContainsKey("strName"))
				{
					condOwner.strName = condOwner.mapGUIPropMaps[strName]["strName"];
				}
			}
			condOwner.CheckForRename();
		}
		return result;
	}

	protected bool UpdateTiles(CondOwner objICO, bool bRemove, bool skipRepositioning = false)
	{
		if (objICO == null)
		{
			return false;
		}
		Item item = objICO.Item;
		if (item == null)
		{
			return false;
		}
		Pathfinder pathfinder = objICO.Pathfinder;
		if (pathfinder != null)
		{
			return false;
		}
		Vector2 zero = new Vector2(-1f, 1f);
		if (objICO.HasCond("IsRoom"))
		{
			zero = Vector2.zero;
		}
		Vector3 vector = objICO.tf.position;
		Vector3 vector2 = vector;
		if (vector.x % 1f == 0f || vector.y % 1f == 0f)
		{
			Vector3? closestCrewMemberPosition = this.GetClosestCrewMemberPosition(objICO.tf.position);
			vector2 = ((closestCrewMemberPosition == null) ? vector2 : closestCrewMemberPosition.Value);
		}
		if (item.nWidthInTiles % 2 != 0)
		{
			vector.x = (float)MathUtils.RoundToInt(vector.x);
		}
		else if (vector.x % 1f == 0f)
		{
			Vector3 closestPosition = MathUtils.GetClosestPosition(new Vector3[]
			{
				new Vector3(vector.x + 0.5f, vector.y, vector.z),
				new Vector3(vector.x - 0.5f, vector.y, vector.z)
			}, vector2);
			vector = closestPosition;
		}
		if (item.nHeightInTiles % 2 != 0)
		{
			vector.y = (float)MathUtils.RoundToInt(vector.y);
		}
		else if (vector.y % 1f == 0f)
		{
			Vector3 closestPosition2 = MathUtils.GetClosestPosition(new Vector3[]
			{
				new Vector3(vector.x, vector.y + 0.5f, vector.z),
				new Vector3(vector.x, vector.y - 0.5f, vector.z)
			}, vector2);
			vector = closestPosition2;
		}
		if (!skipRepositioning && vector != objICO.tf.position)
		{
			Debug.LogWarning(string.Concat(new object[]
			{
				"Correcting item ",
				objICO.strNameFriendly,
				" pos from ",
				objICO.tf.position,
				" to ",
				vector
			}));
			objICO.tf.position = vector;
		}
		Vector2 tltileCoords = objICO.TLTileCoords;
		Vector2 vector3 = objICO.TLTileCoords + zero;
		int num = (int)(-(int)zero.x);
		int num2 = (int)zero.y;
		int nRight = item.nWidthInTiles - (int)zero.x;
		int nBottom = item.nHeightInTiles + (int)zero.y;
		if (this.aTiles.Count > 0)
		{
			num = MathUtils.RoundToInt(this.vShipPos.x - vector3.x);
			num2 = -MathUtils.RoundToInt(this.vShipPos.y - vector3.y);
			nRight = Mathf.Max(0, item.nWidthInTiles - 2 * (int)zero.x - num - this.nCols);
			nBottom = Mathf.Max(0, item.nHeightInTiles + 2 * (int)zero.y - num2 - this.nRows);
			num = Mathf.Max(0, num);
			num2 = Mathf.Max(0, num2);
		}
		else
		{
			this.vShipPos = tltileCoords;
		}
		bool result = false;
		if (!bRemove)
		{
			result = TileUtils.PadTilemap(this, this.goTiles, num, nRight, num2, nBottom);
		}
		else if (this.aTiles != null && this.aTiles.Count > 0)
		{
			result = TileUtils.PadTilemap(this, this.goTiles, num, nRight, num2, nBottom);
		}
		Tile tile = null;
		bool flag = false;
		List<Tile> list = null;
		if (item.ctSpriteSheet != null && (item.nHeightInTiles > 1 || item.nWidthInTiles > 1))
		{
			list = new List<Tile>();
		}
		for (int i = 0; i < item.nHeightInTiles; i++)
		{
			for (int j = 0; j < item.nWidthInTiles; j++)
			{
				tile = null;
				int num3 = i * item.nWidthInTiles + j;
				if (item.aSocketAdds.Count < num3 - 1)
				{
					flag = true;
					break;
				}
				bool activeInHierarchy = TileUtils.goPartTiles.activeInHierarchy;
				TileUtils.goPartTiles.SetActive(true);
				Ray ray = new Ray(new Vector3(tltileCoords.x + (float)j, tltileCoords.y - (float)i, -10f), Vector3.forward);
				RaycastHit[] array = Physics.RaycastAll(ray, 100f, 256);
				TileUtils.goPartTiles.SetActive(activeInHierarchy);
				foreach (RaycastHit raycastHit in array)
				{
					tile = raycastHit.transform.GetComponent<Tile>();
					if (tile != null && tile.coProps != null && tile.coProps.ship == this)
					{
						List<string> lootNames = item.aSocketAdds[num3].GetLootNames(null, false, null);
						CondOwner coProps = tile.coProps;
						foreach (string strName in lootNames)
						{
							if (bRemove)
							{
								coProps.AddCondAmount(strName, -1.0, 0.0, 0f);
							}
							else
							{
								coProps.AddCondAmount(strName, 1.0, 0.0, 0f);
							}
						}
						tile.UpdateFlags();
						if (list != null)
						{
							list.Add(tile);
						}
						break;
					}
				}
			}
			if (flag)
			{
				break;
			}
		}
		if (item.ctSpriteSheet != null)
		{
			if (list == null)
			{
				Tile[] surroundingTiles = TileUtils.GetSurroundingTiles(tile, true, false);
				item.SetSpriteSheetIndex(surroundingTiles);
				List<CondOwner> list2 = new List<CondOwner>();
				foreach (Tile tile2 in surroundingTiles)
				{
					if (!(tile2 == null))
					{
						if (item.ctSpriteSheet.Triggered(tile2.coProps, null, true))
						{
							this.GetCOsAtWorldCoords1(tile2.tf.position, null, false, true, list2);
							foreach (CondOwner condOwner in list2)
							{
								Item item2 = condOwner.Item;
								if (!(item2 == null) && item2.ctSpriteSheet != null && !(item.ctSpriteSheet.strName != item2.ctSpriteSheet.strName))
								{
									item2.SetSpriteSheetIndex(TileUtils.GetSurroundingTiles(tile2, true, false));
									break;
								}
							}
							list2.Clear();
						}
					}
				}
			}
			else
			{
				foreach (Tile tilCenter in list)
				{
					Tile[] surroundingTiles2 = TileUtils.GetSurroundingTiles(tilCenter, true, false);
					List<CondOwner> list3 = new List<CondOwner>();
					foreach (Tile tile3 in surroundingTiles2)
					{
						if (!(tile3 == null) && !list.Contains(tile3))
						{
							if (item.ctSpriteSheet.Triggered(tile3.coProps, null, true))
							{
								this.GetCOsAtWorldCoords1(tile3.tf.position, null, false, true, list3);
								foreach (CondOwner condOwner2 in list3)
								{
									Item item3 = condOwner2.Item;
									if (!(item3 == null) && item3.ctSpriteSheet != null && !(item.ctSpriteSheet.strName != item3.ctSpriteSheet.strName))
									{
										item3.SetSpriteSheetIndex(TileUtils.GetSurroundingTiles(tile3, true, false));
										break;
									}
								}
								list3.Clear();
							}
						}
					}
				}
			}
		}
		return result;
	}

	public void UpdatePower()
	{
		TileUtils.GetPoweredTiles(this);
		this.bCheckPower = false;
	}

	public bool BGItemFits(Item itm)
	{
		if (itm == null)
		{
			return false;
		}
		Vector3 localPosition = itm.TF.localPosition;
		return this.BGItemFits(itm.ToString(), localPosition.x, localPosition.y);
	}

	private bool BGItemFits(string strBGName, float fX, float fY)
	{
		if (strBGName == null)
		{
			return false;
		}
		if (this.dictBGs.ContainsKey(strBGName))
		{
			List<Vector2> list = this.dictBGs[strBGName];
			if (list == null)
			{
				return true;
			}
			foreach (Vector2 vector in list)
			{
				if (Mathf.Abs(fX - vector.x) < 0.5f && Mathf.Abs(fY - vector.y) < 0.5f)
				{
					return false;
				}
			}
			return true;
		}
		return true;
	}

	public void BGItemAdd(Item itm)
	{
		if (itm == null)
		{
			return;
		}
		Vector3 localPosition = itm.TF.localPosition;
		string key = itm.ToString();
		if (!this.dictBGs.ContainsKey(key))
		{
			this.dictBGs[key] = new List<Vector2>();
		}
		this.dictBGs[key].Add(localPosition);
		itm.TF.SetParent(this.tfBGs, true);
		CrewSim.objInstance.ShowBlocksAndLights(itm, true);
		if (!CrewSim.bShipEdit)
		{
			BoxCollider component = itm.GetComponent<BoxCollider>();
			component.center = new Vector3(component.center.x, component.center.y, component.center.z + 125f);
		}
	}

	public void BGItemRemove(Item itm)
	{
		if (itm == null)
		{
			return;
		}
		Vector3 localPosition = itm.TF.localPosition;
		if (this.dictBGs.ContainsKey(itm.ToString()))
		{
			List<Vector2> list = this.dictBGs[itm.ToString()];
			if (list != null)
			{
				int num = -1;
				for (int i = 0; i < list.Count; i++)
				{
					Vector2 vector = list[i];
					if (Mathf.Abs(localPosition.x - vector.x) < 0.5f && Mathf.Abs(localPosition.y - vector.y) < 0.5f)
					{
						num = i;
						break;
					}
				}
				if (num >= 0)
				{
					list.RemoveAt(num);
				}
				if (list.Count == 0)
				{
					this.dictBGs.Remove(itm.ToString());
				}
			}
		}
		CrewSim.objInstance.ShowBlocksAndLights(itm, false);
		UnityEngine.Object.Destroy(itm.gameObject);
	}

	public void AddCO(CondOwner objICO, bool bTiles)
	{
		this.AddCO(objICO, bTiles, false);
	}

	private void AddCO(CondOwner objICO, bool bTiles, bool skipRepositioning)
	{
		if (objICO == null)
		{
			return;
		}
		if (this.mapICOs == null)
		{
			Debug.LogError(string.Concat(new object[]
			{
				"Adding CO ",
				objICO,
				" to null ship: ",
				this.strRegID
			}));
			return;
		}
		if (objICO == null || !objICO.HasCond("IsModeSwitching", false))
		{
			this.ResetMass();
			this.SilhouettePoints = null;
		}
		CrewSim.objInstance.coDicts.AddCO(objICO);
		bool flag = false;
		if (bTiles)
		{
			flag = this.UpdateTiles(objICO, false, skipRepositioning);
			CrewSim.objInstance.ShowBlocksAndLights(objICO, true);
		}
		List<CondOwner> list = new List<CondOwner>();
		foreach (CondOwner condOwner in objICO.aStack)
		{
			list.Add(condOwner);
			CondOwner.NullSafeAddRange(ref list, condOwner.GetCOs(true, null));
		}
		if (objICO.objContainer != null)
		{
			list.AddRange(objICO.objContainer.GetCOs(true, null));
		}
		Slots compSlots = objICO.compSlots;
		if (compSlots != null)
		{
			list.AddRange(compSlots.GetCOs(null, false, null));
		}
		Tile tileAtWorldCoords = this.GetTileAtWorldCoords1(objICO.tf.position.x, objICO.tf.position.y, true, true);
		foreach (CondOwner condOwner2 in list)
		{
			if (condOwner2 == null)
			{
				Debug.LogError("Adding CO with null sub-co: " + objICO);
			}
			else
			{
				this.mapICOs[condOwner2.strID] = condOwner2;
				condOwner2.ship = this;
				Tile.AddToRoom(tileAtWorldCoords, condOwner2, true);
				if (condOwner2.tf.parent == null && condOwner2.slotNow != null)
				{
					condOwner2.tf.SetParent(this.gameObject.transform, true);
				}
				if (condOwner2.HasCond("IsSocial") && this.aPeople.IndexOf(condOwner2.pspec) < 0)
				{
					if (condOwner2.pspec == null)
					{
						Debug.LogError("Adding null pspec: " + condOwner2.strName);
					}
					this.aPeople.Add(condOwner2.pspec);
				}
				if (this.gameObject.activeInHierarchy && condOwner2.HasTickers())
				{
					CrewSim.AddTicker(condOwner2);
				}
				if (!CrewSim.bShipEdit && GUILocks.IsLock(condOwner2))
				{
					this.bCheckLocks = true;
					this.aLocks.Add(condOwner2);
				}
			}
		}
		this.mapICOs[objICO.strID] = objICO;
		objICO.ship = this;
		if (objICO.objCOParent == null)
		{
			objICO.tf.SetParent(this.gameObject.transform, true);
		}
		Tile.AddToRoom(tileAtWorldCoords, objICO, true);
		if (objICO.HasCond("IsSocial") && this.aPeople.IndexOf(objICO.pspec) < 0)
		{
			if (objICO.pspec == null)
			{
				Debug.LogError("Adding null pspec: " + objICO.strName);
			}
			this.aPeople.Add(objICO.pspec);
			Debug.Log("#Info# Adding " + objICO.strName + " to " + this.strRegID);
		}
		if (this.gameObject.activeInHierarchy && objICO.HasTickers())
		{
			CrewSim.AddTicker(objICO);
		}
		Pathfinder pathfinder = objICO.Pathfinder;
		if (pathfinder != null)
		{
			pathfinder.tilCurrent = tileAtWorldCoords;
		}
		objICO.gameObject.SetActive(this.gameObject.activeInHierarchy);
		if (objICO.objCOParent == null)
		{
			objICO.Visible = this.gameObject.activeInHierarchy;
		}
		if (objICO.HasCond("IsTraderNPC") || objICO.HasCond("IsMarketActor"))
		{
			this.AddMarketActorConfigToShip(objICO);
			if (this.nLoadState == Ship.Loaded.Full)
			{
				MarketManager.AddMarketActorToShip(this, objICO.strID);
			}
		}
		if (objICO.HasCond("IsPowerRecalc"))
		{
			this.bCheckPower = true;
		}
		if (flag || objICO.HasCond("IsCheckRoom"))
		{
			this.bCheckRooms = true;
		}
		if (objICO.HasCond("IsShipSpecialItem"))
		{
			if (!CrewSim.bShipEdit && GUILocks.IsLock(objICO))
			{
				this.bCheckLocks = true;
				this.aLocks.Add(objICO);
			}
			if (Ship.ctRCSClusterAudioEmitter.Triggered(objICO, null, true) && !this.aRCSThrusters.Contains(objICO))
			{
				this.aRCSThrusters.Add(objICO);
				if (objICO.HasCond("StatThrustStrength"))
				{
					this.fRCSCount += (float)objICO.GetCondAmount("StatThrustStrength");
				}
				else
				{
					this.fRCSCount += 1f;
				}
			}
			if (Ship.ctNavStationOn.Triggered(objICO, null, true) && this.aNavs.IndexOf(objICO) < 0)
			{
				this.aNavs.Add(objICO);
			}
			if (Ship.CTReactor.Triggered(objICO, null, true) && this.aCores.IndexOf(objICO) < 0)
			{
				this.aCores.Add(objICO);
			}
			if (Ship.ctRCSDistroInstalled.Triggered(objICO, null, true) && !this.aRCSDistros.Contains(objICO))
			{
				this.aRCSDistros.Add(objICO);
				this.nRCSDistroCount++;
			}
			if (Ship.ctDocksys.Triggered(objICO, null, true) && !this.aDocksys.Contains(objICO))
			{
				this.aDocksys.Add(objICO);
				this.nDockCount++;
			}
			if (objICO.HasCond("IsTransponder"))
			{
				if (objICO.HasCond("IsDamaged"))
				{
					if (CrewSim.shipCurrentLoaded == this && CrewSim.coPlayer != null && CrewSim.coPlayer.HasCond("TutorialXPDRReplaceShow"))
					{
						Objective objective = new Objective(CrewSim.coPlayer, "Replace Broken Transponder", "TIsTutorialXPDRReplaceComplete");
						objective.strDisplayDesc = "Visit the ship broker on OKLG Commercial to replace your broken transponder.";
						objective.strDisplayDescComplete = "Replacement Transponder Purchased";
						objective.bTutorial = true;
						MonoSingleton<ObjectiveTracker>.Instance.AddObjective(objective);
						CrewSim.coPlayer.ZeroCondAmount("TutorialXPDRReplaceShow");
						CrewSim.coPlayer.AddCondAmount("TutorialXPDRReplaceWaiting", 1.0, 0.0, 0f);
					}
				}
				else if (objICO.HasCond("IsInstalled"))
				{
					if (objICO.HasCond("IsReadyTransponderReset"))
					{
						objICO.ApplyGPMChanges(new string[]
						{
							"Data,strRegID," + this.strRegID
						});
						objICO.ZeroCondAmount("IsReadyTransponderReset");
					}
					if (!objICO.HasCond("IsOff"))
					{
						this.strXPDR = objICO.GetGPMInfo("Data", "strRegID");
					}
				}
			}
			if (Ship.ctXPDRAntOn.Triggered(objICO, null, true))
			{
				this.bXPDRAntenna = true;
			}
			if (Ship.ctTowBraceSecured.Triggered(objICO, null, true))
			{
				this.bTowBraceSecured = true;
			}
			if (Ship.ctAirPump.Triggered(objICO, null, true) && !this.aO2AirPumps.Contains(objICO))
			{
				Tuple<double, double> o2UnderPump = ShipStatus.GetO2UnderPump(objICO, Ship.ctO2Can);
				if (o2UnderPump.Item2 > 0.0)
				{
					this.aO2AirPumps.Add(objICO);
				}
			}
			if (Ship.ctO2Can.Triggered(objICO, null, true))
			{
				foreach (CondOwner condOwner3 in this.GetCOs(Ship.ctAirPump, false, false, false))
				{
					if (!this.aO2AirPumps.Contains(condOwner3))
					{
						Tile tileAtWorldCoords2 = this.GetTileAtWorldCoords1(objICO.tf.position.x, objICO.tf.position.y, false, true);
						Vector2 pos = condOwner3.GetPos("GasInput", false);
						if (this.GetTileAtWorldCoords1(pos.x, pos.y, false, true) == tileAtWorldCoords2)
						{
							this.aO2AirPumps.Add(condOwner3);
							break;
						}
					}
				}
			}
			if (objICO.HasCond("IsFusionCoreModule"))
			{
				this.bCheckFusion = true;
			}
			if (Ship.ctHeavyLiftRotorsInstalled.Triggered(objICO, null, true))
			{
				this.LiftRotorsThrustStrength = -1f;
				if (!this.aActiveHeavyLiftRotors.Contains(objICO))
				{
					this.aActiveHeavyLiftRotors.Add(objICO);
				}
			}
			if (Ship.ctStabilizerActiveOn.Triggered(objICO, null, true))
			{
				this.nActiveStabilizers++;
			}
			if (this.objSS != null && objICO.HasCond("StatAeroLift"))
			{
				this.fAeroCoefficient += (float)objICO.GetCondAmount("StatAeroLift");
			}
		}
	}

	public CondOwner DropCO(CondOwner objCO, Vector2 nearPosition)
	{
		JsonZone zoneFromTileRadius = TileUtils.GetZoneFromTileRadius(this, nearPosition, 2, true, false);
		Tile tileAtWorldCoords = this.GetTileAtWorldCoords1(nearPosition.x, nearPosition.y, true, true);
		if (tileAtWorldCoords != null && tileAtWorldCoords.jZone != null)
		{
			HashSet<int> hashSet = new HashSet<int>();
			foreach (int item in tileAtWorldCoords.jZone.aTiles)
			{
				hashSet.Add(item);
			}
			zoneFromTileRadius.aTiles = hashSet.ToArray<int>();
		}
		objCO.RemoveFromCurrentHome(false);
		CondTrigger condTrigger = DataHandler.GetCondTrigger("TIsLootSpawnOK");
		List<CondOwner> cosInZone = this.GetCOsInZone(zoneFromTileRadius, condTrigger, false, true);
		cosInZone.Remove(objCO);
		bool flag = true;
		if (flag)
		{
			HashSet<int> hashSet2 = new HashSet<int>();
			foreach (int num in zoneFromTileRadius.aTiles)
			{
				Vector2 worldCoordsAtTileIndex = this.GetWorldCoordsAtTileIndex1(num);
				worldCoordsAtTileIndex.x += 0.5f;
				worldCoordsAtTileIndex.y += 0.5f;
				if (Visibility.IsCondOwnerLOSVisibleBlocks(objCO, worldCoordsAtTileIndex, false, false))
				{
					hashSet2.Add(num);
				}
			}
			zoneFromTileRadius.aTiles = hashSet2.ToArray<int>();
			for (int k = cosInZone.Count - 1; k >= 0; k--)
			{
				if (!Visibility.IsCondOwnerLOSVisibleBlocks(cosInZone[k], nearPosition, false, false))
				{
					cosInZone.RemoveAt(k);
				}
			}
		}
		List<CondOwner> list = TileUtils.DropCOsNearby(new List<CondOwner>
		{
			objCO
		}, this, zoneFromTileRadius, cosInZone, condTrigger, false, true);
		CondOwner result = null;
		if (list.Count > 0)
		{
			result = list[0];
		}
		return result;
	}

	private void RemoveInternalFromRooms(Tile tile, CondOwner objCO)
	{
		if (this.bDestroyed || tile == null)
		{
			return;
		}
		if (tile.room == null)
		{
			Vector2 pos = objCO.GetPos("use", false);
			Tile tileAtWorldCoords = this.GetTileAtWorldCoords1(pos.x, pos.y, false, true);
			if (tileAtWorldCoords != null && tileAtWorldCoords.room != null)
			{
				tileAtWorldCoords.room.RemoveFromRoom(objCO);
			}
		}
		else
		{
			tile.room.RemoveFromRoom(objCO);
		}
	}

	private void RemoveInternalSocial(CondOwner objCO)
	{
		this.aPeople.Remove(objCO.pspec);
		if (this.LoadState <= Ship.Loaded.Shallow && this.json != null && this.json.aCrew != null)
		{
			List<JsonItem> list = new List<JsonItem>();
			foreach (JsonItem jsonItem in this.json.aCrew)
			{
				if (jsonItem.strID != objCO.strID)
				{
					list.Add(jsonItem);
				}
			}
			this.json.aCrew = list.ToArray();
		}
		Debug.Log("#Info# Removing " + objCO.strName + " from " + this.strRegID);
	}

	private void RemoveInternalItems(List<CondOwner> items)
	{
		if (items.Count == 0)
		{
			return;
		}
		Dictionary<string, bool> dictionary = new Dictionary<string, bool>(items.Count);
		foreach (CondOwner condOwner in items)
		{
			dictionary[condOwner.strID] = true;
		}
		JsonItem[] aItems = this.json.aItems;
		int num = aItems.Length;
		int num2 = 0;
		for (int i = 0; i < num; i++)
		{
			if (!dictionary.ContainsKey(aItems[i].strID))
			{
				aItems[num2] = aItems[i];
				num2++;
			}
		}
		if (num2 < num)
		{
			JsonItem[] array = new JsonItem[num2];
			Array.Copy(aItems, 0, array, 0, num2);
			this.json.aItems = array;
		}
	}

	protected void RemoveInternal(Tile tile, CondOwner objCO)
	{
		this.mapICOs.Remove(objCO.strID);
		CrewSim.RemoveTicker(objCO);
		this.RemoveInternalFromRooms(tile, objCO);
		if (objCO.HasCond("IsSocial"))
		{
			this.RemoveInternalSocial(objCO);
		}
		else if (this.LoadState <= Ship.Loaded.Shallow && this.json != null && this.json.aItems != null)
		{
			int num = -1;
			int num2 = 0;
			foreach (JsonItem jsonItem in this.json.aItems)
			{
				if (jsonItem != null && jsonItem.strID == objCO.strID)
				{
					num = num2;
					break;
				}
				num2++;
			}
			if (num != -1)
			{
				List<JsonItem> list = new List<JsonItem>(this.json.aItems);
				list.RemoveAt(num);
				this.json.aItems = list.ToArray();
			}
		}
		objCO.ship = null;
		if (objCO.tf != null && this.gameObject != null && objCO.tf.parent == this.gameObject.transform)
		{
			objCO.tf.SetParent(null, true);
		}
	}

	protected void RemoveInternalLot(Tile tile, List<CondOwner> cosToRemove)
	{
		List<CondOwner> list = new List<CondOwner>();
		foreach (CondOwner condOwner in cosToRemove)
		{
			this.mapICOs.Remove(condOwner.strID);
			CrewSim.RemoveTicker(condOwner);
			this.RemoveInternalFromRooms(tile, condOwner);
			if (condOwner.HasCond("IsSocial"))
			{
				this.RemoveInternalSocial(condOwner);
			}
			else
			{
				list.Add(condOwner);
			}
			condOwner.ship = null;
			if (condOwner.tf != null && this.gameObject != null && condOwner.tf.parent == this.gameObject.transform)
			{
				condOwner.tf.SetParent(null, true);
			}
		}
		if (this.LoadState > Ship.Loaded.Shallow || this.json == null || this.json.aItems == null)
		{
			return;
		}
		this.RemoveInternalItems(list);
	}

	public bool HasRating()
	{
		return this.rating != null && this.rating.Length >= 1 && this.rating[0] != null;
	}

	public string GetRatingString()
	{
		if (!this.HasRating())
		{
			return "None";
		}
		string text = string.Empty;
		for (int i = 1; i < this.rating.Length; i++)
		{
			if (!string.IsNullOrEmpty(this.rating[i]))
			{
				if (i == 1)
				{
					text = this.rating[i];
				}
				else
				{
					text = text + "-" + this.rating[i];
				}
			}
		}
		return text;
	}

	public string[] CalculateRating()
	{
		float num = 0f;
		List<CondOwner> cos = this.GetCOs(DataHandler.GetCondTrigger("TIsInstalled"), true, false, true);
		foreach (CondOwner condOwner in cos)
		{
			if (!condOwner.HasCond("IsDamaged"))
			{
				float damage = condOwner.GetDamage();
				num += Mathf.Clamp01(1f - damage);
			}
		}
		num /= (float)cos.Count;
		string text;
		if (num >= 0f && (double)num <= 0.5)
		{
			text = "E";
		}
		else if ((double)num > 0.5 && (double)num <= 0.8)
		{
			text = "D";
		}
		else if ((double)num > 0.8 && (double)num <= 0.95)
		{
			text = "C";
		}
		else if ((double)num > 0.95 && (double)num <= 0.99)
		{
			text = "B";
		}
		else
		{
			text = "A";
		}
		List<RoomSpec> roomSpecs = this.GetRoomSpecs();
		string text2 = "0";
		if (roomSpecs != null)
		{
			text2 = roomSpecs.Count.ToString();
		}
		double num2 = (this.fRCSCount != 0f) ? (this.Mass / (double)this.fRCSCount) : 0.0;
		string text3 = string.Empty;
		if (num2 <= 0.0)
		{
			text3 = "O";
		}
		else if (num2 > 0.0 && num2 < 300.0)
		{
			text3 = "A";
		}
		else if (num2 >= 300.0 && num2 < 500.0)
		{
			text3 = "B";
		}
		else if (num2 >= 500.0 && num2 < 750.0)
		{
			text3 = "C";
		}
		else if (num2 >= 750.0 && num2 < 1500.0)
		{
			text3 = "D";
		}
		else
		{
			text3 = "E";
		}
		int num3 = this.nCols * this.nRows;
		string text4 = string.Empty;
		if (num3 <= 0)
		{
			text4 = string.Empty;
		}
		else if (num3 < 250)
		{
			text4 = "Small";
		}
		else if (num3 < 900)
		{
			text4 = "Medium";
		}
		else if (num3 < 1600)
		{
			text4 = "Lunamax";
		}
		else if (num3 < 2300)
		{
			text4 = "Ceresmax";
		}
		else if (num3 < 3000)
		{
			text4 = "Titanmax";
		}
		else if (num3 < 3700)
		{
			text4 = "Very Large";
		}
		else
		{
			text4 = "Ultra Large";
		}
		string text5 = string.Empty;
		if (this.rating != null && this.rating.Length == 6)
		{
			text5 = this.rating[5];
		}
		string text6 = StarSystem.fEpoch.ToString();
		return new string[]
		{
			text6,
			text,
			text2,
			text3,
			text4,
			text5
		};
	}

	public void UpdateRating(string[] newRating = null)
	{
		this.rating = ((newRating == null) ? this.CalculateRating() : newRating);
	}

	public void RemoveCO(CondOwner objCO, bool bForce = false)
	{
		if (objCO == null || !objCO.HasCond("IsModeSwitching", false))
		{
			this.ResetMass();
			this.SilhouettePoints = null;
		}
		if (objCO == null)
		{
			if (objCO != null && objCO.strID != null)
			{
				this.mapICOs.Remove(objCO.strID);
			}
			this.aDocksys.Remove(objCO);
			return;
		}
		objCO.ValidateParent();
		CrewSim.objInstance.coDicts.RemoveCO(objCO);
		if (objCO.objCOParent != null && objCO.coStackHead == null)
		{
			if (objCO.slotNow != null)
			{
				Slots compSlots = objCO.objCOParent.compSlots;
				if (compSlots != null)
				{
					compSlots.UnSlotItem(objCO, bForce);
					objCO.ValidateParent();
					CondOwner.CheckTrue(objCO.objCOParent == null, "Unslotted but still have parent...");
				}
			}
		}
		else if (objCO.coStackHead != null)
		{
			CondOwner coStackHead = objCO.coStackHead;
			coStackHead.aStack.Remove(objCO);
			objCO.coStackHead = null;
			Item item = objCO.Item;
			item.fLastRotation = coStackHead.tf.rotation.eulerAngles.z;
			objCO.tf.position = new Vector3(coStackHead.tf.position.x, coStackHead.tf.position.y, coStackHead.tf.position.z);
			coStackHead.UpdateAppearance();
			objCO.UpdateAppearance();
		}
		else
		{
			this.UpdateTiles(objCO, true, false);
			CrewSim.objInstance.ShowBlocksAndLights(objCO, false);
		}
		Tile tileAtWorldCoords = this.GetTileAtWorldCoords1(objCO.tf.position.x, objCO.tf.position.y, true, true);
		List<CondOwner> list = new List<CondOwner>();
		list.AddRange(objCO.aStack);
		if (objCO.objContainer != null)
		{
			list.AddRange(objCO.objContainer.GetCOs(true, null));
		}
		Slots compSlots2 = objCO.compSlots;
		if (compSlots2 != null)
		{
			list.AddRange(compSlots2.GetCOs(null, true, null));
		}
		list.AddRange(objCO.aLot);
		this.RemoveInternalLot(tileAtWorldCoords, list);
		this.RemoveInternal(tileAtWorldCoords, objCO);
		this.bCheckPower |= objCO.HasCond("IsPowerRecalc");
		this.bCheckRooms |= objCO.HasCond("IsCheckRoom");
		CrewSim.inventoryGUI.RemoveAndDestroy(objCO.strID);
		if (objCO.HasCond("IsShipSpecialItem"))
		{
			if (Ship.ctRCSClusterAudioEmitter.Triggered(objCO, null, true) && this.aRCSThrusters.Contains(objCO))
			{
				this.aRCSThrusters.Remove(objCO);
				if (objCO.HasCond("StatThrustStrength"))
				{
					this.fRCSCount -= (float)objCO.GetCondAmount("StatThrustStrength");
				}
				else
				{
					this.fRCSCount -= 1f;
				}
			}
			if (Ship.ctHeavyLiftRotorsInstalled.Triggered(objCO, null, true))
			{
				this.LiftRotorsThrustStrength = -1f;
				if (this.aActiveHeavyLiftRotors.Contains(objCO))
				{
					this.aActiveHeavyLiftRotors.Remove(objCO);
				}
			}
			if (Ship.ctNavStationOn.Triggered(objCO, null, true))
			{
				this.aNavs.Remove(objCO);
			}
			if (Ship.CTReactor.Triggered(objCO, null, true))
			{
				this.aCores.Remove(objCO);
			}
			if (Ship.ctRCSDistroInstalled.Triggered(objCO, null, true) && this.aRCSDistros.Contains(objCO))
			{
				this.aRCSDistros.Remove(objCO);
				this.nRCSDistroCount--;
			}
			if (Ship.ctDocksys.Triggered(objCO, null, true))
			{
				this.aDocksys.Remove(objCO);
				this.nDockCount--;
			}
			if (Ship.ctXPDR.Triggered(objCO, null, true) && this.GetCOs(Ship.ctXPDR, false, false, false).Count == 0)
			{
				this.strXPDR = null;
			}
			if (Ship.ctXPDRAntOn.Triggered(objCO, null, true) && this.GetCOs(Ship.ctXPDRAntOn, false, false, false).Count == 0)
			{
				this.bXPDRAntenna = false;
			}
			if (Ship.ctTowBraceSecured.Triggered(objCO, null, true) && this.GetCOs(Ship.ctTowBraceSecured, false, false, false).Count == 0)
			{
				this.bTowBraceSecured = false;
			}
			if (Ship.ctAirPump.Triggered(objCO, null, true))
			{
				this.aO2AirPumps.Remove(objCO);
			}
			if (Ship.ctO2Can.Triggered(objCO, null, true))
			{
				foreach (CondOwner condOwner in this.GetCOs(Ship.ctAirPump, false, false, false))
				{
					if (this.aO2AirPumps.Contains(condOwner))
					{
						Tile tileAtWorldCoords2 = this.GetTileAtWorldCoords1(condOwner.tf.position.x, condOwner.tf.position.y, false, true);
						Vector2 pos = condOwner.GetPos("GasInput", false);
						if (this.GetTileAtWorldCoords1(pos.x, pos.y, false, true) == tileAtWorldCoords2)
						{
							this.aO2AirPumps.Remove(condOwner);
							break;
						}
					}
				}
			}
			if (objCO.HasCond("IsFusionCoreModule"))
			{
				this.bCheckFusion = true;
			}
			if (Ship.ctStabilizerActiveOn.Triggered(objCO, null, true))
			{
				this.nActiveStabilizers--;
			}
			if (this.objSS != null && objCO.HasCond("StatAeroLift"))
			{
				this.fAeroCoefficient -= (float)objCO.GetCondAmount("StatAeroLift");
			}
		}
		if (!this._isDespawning && (objCO.HasCond("IsTraderNPC") || objCO.HasCond("IsMarketActor")))
		{
			this.RemoveMarketActorConfigFromShip(objCO);
			List<MarketItem> itemsToDrop = MarketManager.RemoveMarketActorFromShip(this, objCO.strID);
			this.DropCargoPodContent(objCO, itemsToDrop);
		}
		objCO.ValidateParent();
	}

	private void DropCargoPodContent(CondOwner podCO, List<MarketItem> itemsToDrop)
	{
		if (itemsToDrop == null || itemsToDrop.Count <= 0)
		{
			return;
		}
		List<CondOwner> list = new List<CondOwner>();
		int num = UnityEngine.Random.Range(0, itemsToDrop.Count / 4);
		if (MarketManager.ShowDebugLogs)
		{
			Debug.LogWarning("#Market# Dropping pod contents, trying: #" + num);
		}
		for (int i = 0; i < num; i++)
		{
			MarketItem marketItem = itemsToDrop[i];
			CondOwner condOwner = DataHandler.GetCondOwner(marketItem.COName);
			if (!(condOwner == null))
			{
				float num2 = (float)(2 + Mathf.Min(list.Count, 3));
				CondOwner condOwner2 = podCO.DropCO(condOwner, false, this, num2, num2, true, null);
				if (condOwner2 != null)
				{
					list.Add(condOwner2);
				}
			}
		}
		foreach (CondOwner condOwner3 in list)
		{
			if (!(condOwner3 == null))
			{
				condOwner3.Destroy();
			}
		}
	}

	public void ResetMass()
	{
		this._mass = 0.0;
	}

	public void VisitCOs(CondTrigger objCondTrig, bool bSubObjects, bool bAllowDocked, bool bAllowLocked, Action<CondOwner> visitor)
	{
		CondOwnerVisitorAddToHashSet condOwnerVisitorAddToHashSet = new CondOwnerVisitorAddToHashSet();
		CondOwnerVisitor visitor2 = CondOwnerVisitorCondTrigger.WrapVisitor(condOwnerVisitorAddToHashSet, objCondTrig);
		this.VisitCOs(visitor2, bSubObjects, bAllowDocked, bAllowLocked);
		foreach (CondOwner obj in condOwnerVisitorAddToHashSet.aHashSet)
		{
			visitor(obj);
		}
	}

	public void VisitCOs(CondOwnerVisitor visitor, bool bSubObjects, bool bAllowDocked, bool bAllowLocked)
	{
		if (this.mapICOs == null)
		{
			return;
		}
		foreach (CondOwner condOwner in this.mapICOs.Values.ToArray<CondOwner>())
		{
			if (!(condOwner.objCOParent != null))
			{
				if (bSubObjects)
				{
					condOwner.VisitCOs(visitor, bAllowLocked);
				}
				visitor.Visit(condOwner);
			}
		}
		foreach (Room room in this.aRooms)
		{
			visitor.Visit(room.CO);
		}
		if (bAllowDocked)
		{
			foreach (Ship ship in this.aDocked)
			{
				if (ship != null)
				{
					ship.VisitCOs(visitor, bSubObjects, false, bAllowLocked);
				}
			}
		}
	}

	public List<CondOwner> GetCOs(CondTrigger objCondTrig, bool bSubObjects, bool bAllowDocked, bool bAllowLocked)
	{
		CondOwnerVisitorAddToHashSet condOwnerVisitorAddToHashSet = new CondOwnerVisitorAddToHashSet();
		CondOwnerVisitor visitor = CondOwnerVisitorCondTrigger.WrapVisitor(condOwnerVisitorAddToHashSet, objCondTrig);
		this.VisitCOs(visitor, bSubObjects, bAllowDocked, bAllowLocked);
		return new List<CondOwner>(condOwnerVisitorAddToHashSet.aHashSet);
	}

	public CondOwner GetCOFirstOccurrence(JsonPersonSpec jps, PersonSpec psSearcher, bool bAllowDocked)
	{
		if (this.mapICOs == null)
		{
			return null;
		}
		System.Random randomObjectContainer = new System.Random();
		List<PersonSpec> list = new List<PersonSpec>(this.aPeople);
		if (bAllowDocked)
		{
			foreach (Ship ship in this.aDocked)
			{
				if (ship != null)
				{
					list.AddRange(ship.aPeople);
				}
			}
		}
		IOrderedEnumerable<PersonSpec> orderedEnumerable = from x in list
		orderby randomObjectContainer.Next()
		select x;
		foreach (PersonSpec personSpec in orderedEnumerable)
		{
			if (personSpec != null)
			{
				if (psSearcher == null)
				{
					if (jps == null || jps.Matches(personSpec.GetCO()))
					{
						return personSpec.GetCO();
					}
				}
				else if (psSearcher.IsCOMyMother(jps, personSpec.GetCO()))
				{
					return personSpec.GetCO();
				}
			}
		}
		return null;
	}

	public CondOwner GetCOFirstOccurrence(CondTrigger objCondTrig, bool bSubObjects, bool bAllowDocked, bool bAllowLocked)
	{
		if (this.mapICOs == null)
		{
			return null;
		}
		System.Random randomObjectContainer = new System.Random();
		if (objCondTrig.RequiresHumans)
		{
			List<PersonSpec> list = new List<PersonSpec>(this.aPeople);
			if (bAllowDocked)
			{
				foreach (Ship ship in this.aDocked)
				{
					if (ship != null)
					{
						list.AddRange(ship.aPeople);
					}
				}
			}
			IOrderedEnumerable<PersonSpec> orderedEnumerable = from x in list
			orderby randomObjectContainer.Next()
			select x;
			foreach (PersonSpec personSpec in orderedEnumerable)
			{
				if (personSpec != null)
				{
					if (objCondTrig.Triggered(personSpec.GetCO(), null, true))
					{
						return personSpec.GetCO();
					}
				}
			}
			return null;
		}
		System.Random random = new System.Random();
		CondOwner[] array = this.mapICOs.Values.ToArray<CondOwner>();
		for (int i = array.Length - 1; i > 0; i--)
		{
			int num = random.Next(i + 1);
			CondOwner condOwner = array[i];
			array[i] = array[num];
			array[num] = condOwner;
		}
		List<CondOwner> list2 = new List<CondOwner>();
		foreach (CondOwner condOwner2 in array)
		{
			if (!(condOwner2 == null) && !(condOwner2.objCOParent != null))
			{
				if (objCondTrig.Triggered(condOwner2, null, true))
				{
					if (!bAllowDocked)
					{
						return condOwner2;
					}
					list2.Add(condOwner2);
					break;
				}
				else if (bSubObjects)
				{
					List<CondOwner> cos = condOwner2.GetCOs(bAllowLocked, objCondTrig);
					if (cos != null && cos.Count != 0)
					{
						IOrderedEnumerable<CondOwner> source = from x in cos
						orderby randomObjectContainer.Next()
						select x;
						if (bAllowDocked)
						{
							list2.Add(source.FirstOrDefault<CondOwner>());
							break;
						}
						return source.FirstOrDefault<CondOwner>();
					}
				}
			}
		}
		IOrderedEnumerable<Room> orderedEnumerable2 = from x in this.aRooms
		orderby randomObjectContainer.Next()
		select x;
		foreach (Room room in orderedEnumerable2)
		{
			if (objCondTrig.Triggered(room.CO, null, true))
			{
				if (!bAllowDocked)
				{
					return room.CO;
				}
				list2.Add(room.CO);
				break;
			}
		}
		if (bAllowDocked)
		{
			foreach (Ship ship2 in this.aDocked)
			{
				if (ship2 != null)
				{
					CondOwner cofirstOccurrence = ship2.GetCOFirstOccurrence(objCondTrig, bSubObjects, false, bAllowLocked);
					if (cofirstOccurrence != null)
					{
						list2.Add(cofirstOccurrence);
						break;
					}
				}
			}
		}
		if (list2.Count > 0)
		{
			IOrderedEnumerable<CondOwner> source2 = from x in list2
			orderby randomObjectContainer.Next()
			select x;
			return source2.FirstOrDefault<CondOwner>();
		}
		return null;
	}

	public List<CondOwner> GetICOs1(CondTrigger objCondTrig, bool bSubObjects, bool bAllowDocked, bool bAllowLocked)
	{
		return this.GetCOs(objCondTrig, bSubObjects, bAllowDocked, bAllowLocked);
	}

	public CondOwner GetCOByID(string strID)
	{
		if (strID == null)
		{
			return null;
		}
		CondOwner result = null;
		this.mapICOs.TryGetValue(strID, out result);
		return result;
	}

	public Room GetRoomByID(string strID)
	{
		if (strID == null)
		{
			return null;
		}
		foreach (Room room in this.aRooms)
		{
			if (room.CO != null && room.CO.strID == strID)
			{
				return room;
			}
		}
		return null;
	}

	public void GetCOsAtWorldCoords1(Vector2 vPos, CondTrigger ct, bool bAllowDocked, bool bAllowLocked, List<CondOwner> aOut)
	{
		if (false || aOut == null)
		{
			return;
		}
		Ray ray = new Ray(new Vector3(vPos.x, vPos.y, -20f), Vector3.forward);
		RaycastHit[] array = Physics.RaycastAll(ray, 100f);
		Room roomAtWorldCoords = this.GetRoomAtWorldCoords1(vPos, bAllowDocked);
		CondOwner condOwner = null;
		CondOwner condOwner2 = null;
		if (roomAtWorldCoords != null)
		{
			condOwner = roomAtWorldCoords.CO;
		}
		foreach (RaycastHit raycastHit in array)
		{
			CondOwner component = raycastHit.transform.GetComponent<CondOwner>();
			if (!(condOwner == component))
			{
				if (component != null && component.ship != null)
				{
					if (component.ship != this)
					{
						if (!bAllowDocked)
						{
							goto IL_16A;
						}
						if (condOwner2 == null)
						{
							Room roomAtWorldCoords2 = component.ship.GetRoomAtWorldCoords1(vPos, false);
							if (roomAtWorldCoords2 != null && (ct == null || ct.Triggered(roomAtWorldCoords2.CO, null, false)))
							{
								condOwner2 = roomAtWorldCoords2.CO;
							}
						}
					}
					bool flag = false;
					if (ct == null)
					{
						flag = true;
					}
					else if ((ct.strCondName == null || ct.strCondName == string.Empty) && ct.Triggered(component, null, false))
					{
						flag = true;
					}
					if (flag && aOut.IndexOf(component) < 0)
					{
						aOut.Add(component);
					}
				}
			}
			IL_16A:;
		}
		if (condOwner != null && (ct == null || ct.Triggered(condOwner, null, false)) && aOut.IndexOf(condOwner) < 0)
		{
			aOut.Add(condOwner);
		}
		if (bAllowDocked && condOwner2 != null && aOut.IndexOf(condOwner2) < 0)
		{
			aOut.Add(condOwner2);
		}
	}

	public bool TileIndexValid(int nTileIndex)
	{
		return 0 <= nTileIndex && nTileIndex < this.aTiles.Count;
	}

	public int GetTileIndexAtWorldCoords1(Vector2 vPos)
	{
		return this.GetTileIndexAtWorldCoords(vPos.x, vPos.y);
	}

	public int GetTileIndexAtWorldCoords(float fX, float fY)
	{
		int num = MathUtils.RoundToInt((fX - this.vShipPos.x) / 1f);
		int num2 = -MathUtils.RoundToInt((fY - this.vShipPos.y) / 1f);
		if (num < 0 || this.nCols <= num)
		{
			return -1;
		}
		if (num2 < 0 || this.nRows <= num2)
		{
			return -1;
		}
		return num + num2 * this.nCols;
	}

	public Vector2 GetWorldCoordsAtTileIndex1(int nIndex)
	{
		if (nIndex < 0 || nIndex > this.aTiles.Count - 1)
		{
			return Vector2.zero;
		}
		Vector2 result = new Vector2(this.vShipPos.x, this.vShipPos.y);
		result.x += (float)(nIndex % this.nCols);
		result.y -= (float)(nIndex / this.nCols);
		return result;
	}

	public Tile GetTileAtWorldCoords1(float fX, float fY, bool bAllowDocked, bool checkIfShipTile = true)
	{
		Tile tile = null;
		int tileIndexAtWorldCoords = this.GetTileIndexAtWorldCoords(fX, fY);
		if (tileIndexAtWorldCoords >= 0 && tileIndexAtWorldCoords < this.aTiles.Count)
		{
			tile = this.aTiles[tileIndexAtWorldCoords];
			if (checkIfShipTile && TileUtils.CTShipTileOrSub.Triggered(tile.coProps, null, false))
			{
				return tile;
			}
		}
		if (bAllowDocked && this.aDocked != null)
		{
			foreach (Ship ship in this.aDocked)
			{
				if (ship != null)
				{
					Tile tileAtWorldCoords = ship.GetTileAtWorldCoords1(fX, fY, false, false);
					if (tileAtWorldCoords != null)
					{
						if (TileUtils.CTShipTileOrSub.Triggered(tileAtWorldCoords.coProps, null, true))
						{
							return tileAtWorldCoords;
						}
						if (tile == null)
						{
							tile = tileAtWorldCoords;
						}
					}
				}
			}
			return tile;
		}
		return tile;
	}

	public Room GetRandomRoom(bool bAllowDocked)
	{
		Room result = null;
		if (!bAllowDocked)
		{
			if (this.aRooms.Count > 0)
			{
				int index = UnityEngine.Random.Range(0, this.aRooms.Count);
				return this.aRooms[index];
			}
		}
		else
		{
			List<Room> list = new List<Room>();
			list.AddRange(this.aRooms);
			foreach (Ship ship in this.aDocked)
			{
				if (ship != null)
				{
					list.AddRange(ship.aRooms);
				}
			}
			int index2 = UnityEngine.Random.Range(0, list.Count);
			if (list.Count > 0)
			{
				return list[index2];
			}
		}
		return result;
	}

	public Tile GetRandomTile1(bool bWalkable = true, bool bAllowDocked = false)
	{
		List<Tile> allDockedTiles;
		if (!bAllowDocked)
		{
			allDockedTiles = this.aTiles;
		}
		else
		{
			allDockedTiles = this.GetAllDockedTiles();
		}
		if (allDockedTiles.Count > 0)
		{
			for (int i = 0; i < 50; i++)
			{
				int index = UnityEngine.Random.Range(0, allDockedTiles.Count);
				if (!bWalkable || allDockedTiles[index].bPassable)
				{
					return allDockedTiles[index];
				}
			}
			return allDockedTiles[0];
		}
		return null;
	}

	public Tile GetRandomAtmoTile(bool bSafeCO2 = true)
	{
		if (this.bDestroyed || this.aRooms == null)
		{
			return null;
		}
		System.Random randomObjectContainer = new System.Random();
		IOrderedEnumerable<Room> orderedEnumerable = from x in this.aRooms
		orderby randomObjectContainer.Next()
		select x;
		foreach (Room room in orderedEnumerable)
		{
			if (room != null && !room.Void)
			{
				if (PledgeSurviveO2.CoHasO2(room.CO) && (!bSafeCO2 || PledgeSurviveCO2.CoHasSafeCO2Lvl(room.CO)))
				{
					Tile randomWalkableTile = room.GetRandomWalkableTile();
					if (randomWalkableTile != null)
					{
						return randomWalkableTile;
					}
				}
			}
		}
		if (bSafeCO2)
		{
			return this.GetRandomAtmoTile(false);
		}
		return null;
	}

	public Tile GetCrewSpawnTile(CondOwner objCO)
	{
		Tile tile = null;
		List<CondOwner> validCrewSpawnersForCO = this.GetValidCrewSpawnersForCO(objCO);
		using (List<CondOwner>.Enumerator enumerator = validCrewSpawnersForCO.GetEnumerator())
		{
			if (enumerator.MoveNext())
			{
				CondOwner condOwner = enumerator.Current;
				tile = condOwner.GetComponent<LootSpawner>().GetSpawnTile(this);
			}
		}
		if (tile == null)
		{
			tile = this.GetRandomAtmoTile(true);
			if (tile == null)
			{
				tile = this.GetRandomTile1(true, false);
			}
		}
		return tile;
	}

	public Vector3 GetCrewSpawnPosition(CondOwner objCO)
	{
		List<CondOwner> validCrewSpawnersForCO = this.GetValidCrewSpawnersForCO(objCO);
		List<CondOwner> list = validCrewSpawnersForCO.Randomize<CondOwner>();
		foreach (CondOwner condOwner in list)
		{
			if (!(condOwner == null))
			{
				return condOwner.GetComponent<LootSpawner>().GetSpawnPosition(this);
			}
		}
		return Vector3.zero;
	}

	private List<CondOwner> GetValidCrewSpawnersForCO(CondOwner objCO)
	{
		List<CondOwner> cos = this.GetCOs(new CondTrigger
		{
			strName = "TIsLootSpawner",
			aReqs = new string[]
			{
				"IsLootSpawner"
			}
		}, false, false, true);
		List<CondOwner> list = new List<CondOwner>();
		for (int i = cos.Count - 1; i >= 0; i--)
		{
			string a = cos[i].mapGUIPropMaps["Panel A"]["strType"];
			if (a == "Loot")
			{
				cos.RemoveAt(i);
			}
			else if (!(a == "Pspec Loot"))
			{
				if (LootSpawner.ShipMatch(this, cos[i]))
				{
					string strName = cos[i].mapGUIPropMaps["Panel A"]["strLoot"];
					JsonPersonSpec personSpec = DataHandler.GetPersonSpec(strName);
					if (personSpec != null && !personSpec.Matches(objCO))
					{
						cos.RemoveAt(i);
					}
					else
					{
						int num = 1;
						int.TryParse(cos[i].mapGUIPropMaps["Panel A"]["strCount"], out num);
						if (num >= 0)
						{
							if (num == 0)
							{
								list.Add(cos[i]);
							}
							else
							{
								list.Insert(0, cos[i]);
							}
						}
					}
				}
			}
		}
		return list;
	}

	public Room GetRoomAtWorldCoords1(Vector2 vPos, bool bAllowDocked)
	{
		Tile tileAtWorldCoords = this.GetTileAtWorldCoords1(vPos.x, vPos.y, bAllowDocked, true);
		if (tileAtWorldCoords != null)
		{
			return tileAtWorldCoords.room;
		}
		return null;
	}

	public void ShiftTorwardsClosestCrewMember(CondOwner co, float shiftDistance)
	{
		if (co == null)
		{
			return;
		}
		Vector3? closestCrewMemberPosition = this.GetClosestCrewMemberPosition(co.tf.position);
		if (closestCrewMemberPosition == null)
		{
			return;
		}
		Vector2 v = Vector2.MoveTowards(co.tf.position.ToVector2(), closestCrewMemberPosition.Value.ToVector2(), shiftDistance);
		co.tf.position = v.ToVector3(co.tf.position.z);
	}

	private Vector3? GetClosestCrewMemberPosition(Vector3 originPosition)
	{
		List<CondOwner> people = this.GetPeople(false);
		if (people == null)
		{
			return null;
		}
		return new Vector3?(MathUtils.GetClosestPosition((from x in people
		select x.tf.position).ToArray<Vector3>(), originPosition));
	}

	public List<JsonZone> GetZones(string strZoneCond, CondOwner coTest, bool bAllowDocked, bool includeShallowLoaded = false)
	{
		List<JsonZone> list = new List<JsonZone>();
		if (this.mapZones == null)
		{
			return list;
		}
		List<JsonZone> list2 = this.mapZones.Values.ToList<JsonZone>();
		if (includeShallowLoaded && this.json != null && this.json.aZones != null)
		{
			JsonZone[] aZones = this.json.aZones;
			for (int i = 0; i < aZones.Length; i++)
			{
				JsonZone jZone = aZones[i];
				if (!list2.Any((JsonZone x) => x.strName == jZone.strName))
				{
					list2.Add(jZone);
				}
			}
		}
		foreach (JsonZone jsonZone in list2)
		{
			if (jsonZone.aTileConds == null)
			{
				Debug.LogWarning("Warning: null aTileConds on zone " + jsonZone.strName + " in ship " + this.strRegID);
			}
			else if (strZoneCond == null || Array.FindIndex<string>(jsonZone.aTileConds, (string str) => str == strZoneCond) >= 0)
			{
				if (jsonZone.Matches(coTest, false))
				{
					list.Add(jsonZone);
				}
			}
		}
		if (bAllowDocked)
		{
			foreach (Ship ship in this.aDocked)
			{
				if (ship != null)
				{
					list.AddRange(ship.GetZones(strZoneCond, coTest, false, includeShallowLoaded));
				}
			}
		}
		return list;
	}

	public List<CondOwner> GetCOsInZone(string strZone, CondTrigger ct, bool bAllowLocked)
	{
		List<CondOwner> result = new List<CondOwner>();
		JsonZone jz = null;
		if (!this.mapZones.TryGetValue(strZone, out jz))
		{
			return result;
		}
		return this.GetCOsInZone(jz, ct, bAllowLocked, true);
	}

	public List<CondOwner> GetCOsInZone(JsonZone jz, CondTrigger ct, bool bAllowLocked, bool bAllowDocked = true)
	{
		List<CondOwner> list = new List<CondOwner>();
		if (jz == null)
		{
			return list;
		}
		foreach (int index in jz.aTiles)
		{
			List<CondOwner> list2 = new List<CondOwner>();
			this.GetCOsAtWorldCoords1(this.aTiles[index].tf.position, ct, bAllowDocked, bAllowLocked, list2);
			foreach (CondOwner condOwner in list2)
			{
				if (!(condOwner.objCOParent != null))
				{
					if (list.IndexOf(condOwner) < 0)
					{
						list.Add(condOwner);
					}
				}
			}
		}
		return list;
	}

	public List<CondOwner> GetCOsInLocalZone(JsonZone jz, CondTrigger ct)
	{
		List<CondOwner> list = new List<CondOwner>();
		if (jz == null)
		{
			return list;
		}
		foreach (int index in jz.aTiles)
		{
			List<CondOwner> list2 = new List<CondOwner>();
			this.GetCOsAtLocalCoords(this.aTiles[index].tf.position, ct, list2);
			foreach (CondOwner condOwner in list2)
			{
				if (!(condOwner.objCOParent != null))
				{
					if (list.IndexOf(condOwner) < 0)
					{
						list.Add(condOwner);
					}
				}
			}
		}
		return list;
	}

	private void GetCOsAtLocalCoords(Vector2 vPos, CondTrigger ct, List<CondOwner> aOut)
	{
		Ray ray = new Ray(new Vector3(vPos.x, vPos.y, -20f), Vector3.forward);
		int num = Physics.RaycastNonAlloc(ray, Ship.aHitsGetCOs, 100f);
		for (int i = num; i < Ship.aHitsGetCOs.Length; i++)
		{
			Ship.aHitsGetCOs[i].distance = float.PositiveInfinity;
		}
		Array.Sort<RaycastHit>(Ship.aHitsGetCOs, (RaycastHit x, RaycastHit y) => x.distance.CompareTo(y.distance));
		Room roomAtWorldCoords = this.GetRoomAtWorldCoords1(vPos, false);
		CondOwner condOwner = null;
		if (roomAtWorldCoords != null)
		{
			condOwner = roomAtWorldCoords.CO;
		}
		for (int j = 0; j < num; j++)
		{
			RaycastHit raycastHit = Ship.aHitsGetCOs[j];
			if (!(raycastHit.transform == null))
			{
				CondOwner component = raycastHit.transform.GetComponent<CondOwner>();
				if (!(condOwner == component))
				{
					if (component != null && component.ship != null && component.ship == this)
					{
						bool flag = false;
						if (ct == null)
						{
							flag = true;
						}
						else if ((ct.strCondName == null || ct.strCondName == string.Empty) && ct.Triggered(component, null, true))
						{
							flag = true;
						}
						if (flag && aOut.IndexOf(component) < 0)
						{
							aOut.Add(component);
						}
					}
				}
			}
		}
		if (condOwner != null && (ct == null || ct.Triggered(condOwner, null, true)) && aOut.IndexOf(condOwner) < 0)
		{
			aOut.Add(condOwner);
		}
	}

	public bool ChangeCOID(CondOwner co, string strIDNew)
	{
		bool result = false;
		if (this.mapICOs.ContainsKey(co.strID))
		{
			this.mapICOs.Remove(co.strID);
			this.mapICOs[strIDNew] = co;
			result = true;
		}
		else
		{
			foreach (Ship ship in this.aDocked)
			{
				if (ship != null)
				{
					if (ship.ChangeCOID(co, strIDNew))
					{
						result = true;
						break;
					}
				}
			}
		}
		return result;
	}

	public bool CheckCOIDProblem(CondOwner co)
	{
		if (this.mapICOs == null || co == null)
		{
			return true;
		}
		CondOwner x = null;
		CondOwner y = null;
		if (this.mapICOs.ContainsKey(co.strID))
		{
			x = this.mapICOs[co.strID];
		}
		if (this.mapICOs.Values.Contains(co))
		{
			y = co;
		}
		return x != y;
	}

	public bool CheckCOIDProblem(string strCOID)
	{
		if (this.mapICOs == null || strCOID == null)
		{
			return true;
		}
		CondOwner condOwner = null;
		if (this.mapICOs.ContainsKey(strCOID))
		{
			condOwner = this.mapICOs[strCOID];
		}
		return !(condOwner != null) || condOwner.ship != this;
	}

	public bool CanBeDockedWith()
	{
		return this.DockCount > this.GetAllDockedShipsFull().Count;
	}

	public void Dock(Ship objShip, bool bSyncOnly = false)
	{
		if (objShip == null || objShip == this || this.bDestroyed)
		{
			return;
		}
		if (this.aDocked == null)
		{
			this.aDocked = new List<Ship>();
		}
		int count = this.aDocked.Count;
		List<string> list = new List<string>
		{
			objShip.strRegID
		};
		foreach (Ship ship in this.aDocked)
		{
			if (ship != null && !list.Contains(ship.strRegID))
			{
				list.Add(ship.strRegID);
			}
		}
		if (this.json != null && this.json.aDocked != null)
		{
			foreach (string item in this.json.aDocked)
			{
				if (!list.Contains(item))
				{
					list.Add(item);
				}
			}
		}
		this.aDocked.Clear();
		foreach (string text in list)
		{
			Ship shipByRegID = CrewSim.system.GetShipByRegID(text);
			if (shipByRegID != null)
			{
				this.aDocked.Add(shipByRegID);
			}
			else
			{
				Debug.Log("Found null ship in docked list. Removing from dict: " + text);
				CrewSim.system.RemoveShip(text);
			}
		}
		if (this.json != null)
		{
			this.json.aDocked = list.ToArray();
		}
		bool flag = this.Mass < objShip.Mass;
		float fRot = this.objSS.fRot;
		if (flag)
		{
			this.objSS.ssDockedHeavier = objShip.objSS;
			fRot = objShip.objSS.fRot;
		}
		if (bSyncOnly || this.aDocked.Count == count)
		{
			return;
		}
		if (CrewSim.coPlayer != null && this.aPeople.IndexOf(CrewSim.coPlayer.pspec) >= 0)
		{
			AudioManager.am.PlayAudioEmitter("ShipDockClamp", false, false);
		}
		this.objSS.CopyFrom(objShip.objSS, true);
		if (flag)
		{
			this.objSS.fRot = 3.1415927f + fRot;
		}
		else
		{
			this.objSS.fRot = fRot;
		}
		if (!this.objSS.bIsBO)
		{
			this.objSS.UnlockFromBO();
		}
		this.UnlockFromOrbit(true);
		List<Ship> list2 = this.aDocked.ToList<Ship>();
		foreach (Ship ship2 in list2)
		{
			if (ship2 != null)
			{
				ship2.Dock(this, false);
				if (ship2.objSS.bIsBO)
				{
					this.objSS.bBOLocked = true;
					this.objSS.strBOPORShip = ship2.objSS.strBOPORShip;
				}
			}
		}
		if (objShip.objSS.bIsBO && !objShip.objSS.bIsNoFees && CrewSim.coPlayer != null && CrewSim.coPlayer.ship == this)
		{
			string strPayee = AIShipManager.strATCLast + DataHandler.GetString("GUI_REFUEL_PORT_SUFFIX", false);
			Ledger.AddLI(new LedgerLI(strPayee, CrewSim.coPlayer.strID, GUIStationRefuel.dictPrices["rowFuelConnect"], DataHandler.GetString("GUI_REFUEL_SERVICE_FUEL_CONNECT", false), "$", StarSystem.fEpoch, false, LedgerLI.Frequency.OneTime));
			Ledger.AddLI(new LedgerLI(strPayee, CrewSim.coPlayer.strID, GUIStationRefuel.dictPrices["rowLifeExchange"], DataHandler.GetString("GUI_REFUEL_SERVICE_LIFE_EXCHANGE", false), "$", StarSystem.fEpoch, false, LedgerLI.Frequency.OneTime));
			Ledger.AddLI(new LedgerLI(strPayee, CrewSim.coPlayer.strID, GUIStationRefuel.dictPrices["rowPowerHookup"], DataHandler.GetString("GUI_REFUEL_SERVICE_POWER_HOOKUP", false), "$", StarSystem.fEpoch, false, LedgerLI.Frequency.OneTime));
			float num = (float)this.nCols * 0.32f + (float)this.nRows * 0.32f;
			Ledger.AddLI(new LedgerLI(strPayee, CrewSim.coPlayer.strID, GUIStationRefuel.dictPrices["rowDock"] * num, DataHandler.GetString("GUI_REFUEL_SERVICE_DOCK", false), "$", StarSystem.fEpoch, false, LedgerLI.Frequency.Hourly));
			Ledger.AddLI(new LedgerLI(strPayee, CrewSim.coPlayer.strID, GUIStationRefuel.dictPrices["rowDockFacilities"], DataHandler.GetString("GUI_REFUEL_SERVICE_FACILITIES", false), "$", StarSystem.fEpoch, false, LedgerLI.Frequency.Hourly));
		}
		this.LogAdd(DataHandler.GetString("NAV_LOG_DOCK", false) + objShip.strRegID + DataHandler.GetString("NAV_LOG_TERMINATOR", false), StarSystem.fEpoch, true);
		this.CheckAccruedWear();
		this.fWearManeuver = 0f;
		Ship.OnDock.Invoke(this, objShip);
	}

	public void Undock(Ship objShip)
	{
		if (objShip == null || objShip.bDestroyed || objShip == this)
		{
			return;
		}
		if (this.LoadState == Ship.Loaded.Full)
		{
			this.UpdateRating(null);
		}
		if (this.RemoveDockedShip(objShip))
		{
			objShip.Undock(this);
			if (this.aPeople.IndexOf(CrewSim.coPlayer.pspec) >= 0)
			{
				AudioManager.am.PlayAudioEmitter("ShipDockUnclamp", false, false);
			}
			if (this.objSS.ssDockedHeavier == objShip.objSS || this.aDocked.Count == 0)
			{
				this.objSS.ssDockedHeavier = null;
			}
			if (!this.objSS.bIsBO)
			{
				float fRot = this.objSS.fRot;
				this.objSS.CopyFrom(objShip.objSS, true);
				this.objSS.vAccEx = Vector2.zero;
				this.objSS.fRot = fRot;
				BodyOrbit nearestBO = CrewSim.system.GetNearestBO(this.objSS, StarSystem.fEpoch, false);
				bool flag = false;
				if (this.DMGStatus == Ship.Damage.Derelict && nearestBO != null && !this.NavAIManned && !this.NavPlayerManned)
				{
					nearestBO.UpdateTime(StarSystem.fEpoch, true, true);
					double dX = nearestBO.dVelX - this.objSS.vVelX;
					double dY = nearestBO.dVelY - this.objSS.vVelY;
					double magnitude = MathUtils.GetMagnitude(dX, dY);
					if (magnitude / 6.6845869117759804E-12 <= CollisionManager.dMaxSafeV)
					{
						this.objSS.LockToBO(nearestBO, -1.0);
						flag = true;
					}
				}
				if (!flag)
				{
					Vector2 vector = default(Vector2);
					BodyOrbit bodyOrbit = null;
					BodyOrbit greatestGravBO = CrewSim.system.GetGreatestGravBO(this.objSS, StarSystem.fEpoch, ref vector, ref bodyOrbit);
					if (greatestGravBO != null)
					{
						this.objSS.strBOPORShip = greatestGravBO.strName;
					}
					else
					{
						this.objSS.strBOPORShip = null;
					}
					this.objSS.bBOLocked = false;
				}
			}
			this.UnlockFromOrbit(true);
			foreach (Ship ship in this.aDocked)
			{
				this.Undock(objShip);
				if (ship.objSS.bIsBO)
				{
					this.objSS.bBOLocked = true;
					this.objSS.strBOPORShip = ship.objSS.strBOPORShip;
				}
			}
			this.fWearManeuver = 0f;
			this.RemoveDockingFees(objShip);
			this.LogAdd(DataHandler.GetString("NAV_LOG_UNDOCK", false) + objShip.strRegID + DataHandler.GetString("NAV_LOG_TERMINATOR", false), StarSystem.fEpoch, true);
			return;
		}
	}

	private bool RemoveDockedShip(Ship objShip)
	{
		bool result = false;
		if (this.aDocked != null)
		{
			result = this.aDocked.Remove(objShip);
		}
		if (this.json != null && this.json.aDocked != null && this.json.aDocked.Contains(objShip.strRegID))
		{
			List<string> list = new List<string>();
			foreach (string text in this.json.aDocked)
			{
				if (!(text == objShip.strRegID))
				{
					list.Add(text);
				}
			}
			this.json.aDocked = list.ToArray();
			result = true;
		}
		return result;
	}

	private void RemoveDockingFees(Ship objShip)
	{
		if (objShip == null)
		{
			return;
		}
		if (objShip.objSS.bIsBO && CrewSim.coPlayer != null && (CrewSim.system.GetShipOwner(this.strRegID) == CrewSim.coPlayer.strID || CrewSim.coPlayer.ship == this))
		{
			string strPayee = AIShipManager.strATCLast + DataHandler.GetString("GUI_REFUEL_PORT_SUFFIX", false);
			List<LedgerLI> unpaidLIs = Ledger.GetUnpaidLIs(strPayee, CrewSim.coPlayer.strID, DataHandler.GetString("GUI_REFUEL_SERVICE_DOCK", false), true, false);
			unpaidLIs.AddRange(Ledger.GetUnpaidLIs(strPayee, CrewSim.coPlayer.strID, DataHandler.GetString("GUI_REFUEL_SERVICE_FACILITIES", false), true, false));
			strPayee = objShip.strRegID + DataHandler.GetString("GUI_REFUEL_PORT_SUFFIX", false);
			unpaidLIs.AddRange(Ledger.GetUnpaidLIs(strPayee, CrewSim.coPlayer.strID, DataHandler.GetString("GUI_REFUEL_SERVICE_DOCK", false), true, false));
			foreach (LedgerLI ledgerLI in unpaidLIs)
			{
				ledgerLI.Repeats = LedgerLI.Frequency.Hourly;
				Ledger.RemoveLI(ledgerLI);
			}
		}
	}

	public void ToggleVis(bool bShow, bool affectDocked = true)
	{
		if (this.gameObject == null || this.gameObject.activeInHierarchy == bShow)
		{
			return;
		}
		foreach (CondOwner condOwner in this.mapICOs.Values)
		{
			if (condOwner == null || !DataHandler.mapCOs.ContainsKey(condOwner.strID))
			{
				if (condOwner != null)
				{
					Debug.Log("ERROR: Bogus co found: " + condOwner.strID);
				}
				else
				{
					Debug.Log("ERROR: Bogus co found: ");
				}
				Debug.Break();
			}
			else
			{
				Pathfinder pathfinder = condOwner.Pathfinder;
				if (pathfinder != null)
				{
					pathfinder.HideFootprints();
				}
				CrewSim.objInstance.ShowBlocksAndLights(condOwner, bShow);
				if (condOwner.HasTickers())
				{
					if (bShow)
					{
						CrewSim.AddTicker(condOwner);
					}
					else
					{
						CrewSim.RemoveTicker(condOwner);
					}
				}
			}
		}
		IEnumerator enumerator2 = this.tfBGs.GetEnumerator();
		try
		{
			while (enumerator2.MoveNext())
			{
				object obj = enumerator2.Current;
				Transform transform = (Transform)obj;
				CrewSim.objInstance.ShowBlocksAndLights(transform.GetComponent<Item>(), bShow);
			}
		}
		finally
		{
			IDisposable disposable;
			if ((disposable = (enumerator2 as IDisposable)) != null)
			{
				disposable.Dispose();
			}
		}
		this.gameObject.SetActive(bShow);
		this.ShowRoomIDs(CrewSim.bDebugShow && bShow);
		if (affectDocked)
		{
			foreach (Ship ship in this.aDocked)
			{
				ship.ToggleVis(bShow, true);
			}
		}
	}

	public void ToggleDockedVis(bool show)
	{
		foreach (Ship ship in this.GetAllDockedShips())
		{
			if (ship != null)
			{
				ship.ToggleVis(show, false);
			}
		}
	}

	public void ToggleCrewVisibility(bool show)
	{
		foreach (CondOwner condOwner in this.GetPeople(false))
		{
			if (!(condOwner.Crew == null))
			{
				condOwner.Crew.ToggleVisibility(show);
			}
		}
	}

	public void RotateCW()
	{
		Transform transform = this.gameObject.transform;
		foreach (KeyValuePair<string, CondOwner> keyValuePair in this.mapICOs)
		{
			try
			{
				CondOwner value = keyValuePair.Value;
				if (value == null)
				{
					Debug.LogWarning(string.Concat(new string[]
					{
						"WARNING: Null co ",
						keyValuePair.Key,
						" found on ship ",
						this.strRegID,
						"'s mapICOs."
					}));
				}
				else
				{
					if (value.tf == null)
					{
						value.tf = value.transform;
					}
					if (!(value.tf.parent != transform))
					{
						if (value.Item != null)
						{
							value.Item.RotateCW();
						}
						if (value != null && value.transform != null)
						{
							Vector3 position = value.transform.position;
							value.transform.position = new Vector3(position.y, -position.x, position.z);
						}
					}
				}
			}
			catch (Exception ex)
			{
				string text = (!(keyValuePair.Value != null) || keyValuePair.Value.strName == null) ? "null co" : keyValuePair.Value.strName;
				Debug.LogWarning(string.Concat(new string[]
				{
					"Caught Exception from ",
					text,
					" ",
					keyValuePair.Key,
					" M: ",
					ex.Message
				}));
			}
		}
		Tile[] array = new Tile[this.aTiles.Count];
		this.aTiles.CopyTo(array);
		this.aTiles = TileUtils.RotateTilesCW<Tile>(this.aTiles, this.nCols);
		int num = this.nCols;
		this.nCols = this.nRows;
		this.nRows = num;
		this.vShipPos.Set(this.vShipPos.y - (float)(this.nCols - 1), -this.vShipPos.x);
		foreach (Room room in this.aRooms)
		{
			if (room == null)
			{
				Debug.LogWarning("WARNING: Null room found in ship " + this.strRegID + "'s aRooms.");
			}
			else
			{
				CondOwner co = room.CO;
				Tile tile = room.aTiles.FirstOrDefault<Tile>();
				if (co != null && co.transform != null && tile != null && tile.transform != null)
				{
					co.transform.position = tile.transform.position;
				}
				Dictionary<string, int> dictionary = new Dictionary<string, int>();
				foreach (KeyValuePair<string, int> keyValuePair2 in room.dictDoors)
				{
					int value2 = keyValuePair2.Value;
					int value3 = this.aTiles.IndexOf(array[value2]);
					dictionary[keyValuePair2.Key] = value3;
				}
				room.dictDoors = dictionary;
			}
		}
		foreach (CondOwner condOwner in this.mapICOs.Values)
		{
			if (!(condOwner == null) && !(condOwner.Item == null) && condOwner.Item.aBlocks != null && !(condOwner.tf == null))
			{
				foreach (Block block in condOwner.Item.aBlocks)
				{
					block.UpdateStats();
				}
				if (condOwner.Item.bHasSpriteSheet)
				{
					Tile[] surroundingTiles = TileUtils.GetSurroundingTiles(this.GetTileAtWorldCoords1(condOwner.tf.position.x, condOwner.tf.position.y, false, true), true, false);
					condOwner.Item.SetSpriteSheetIndex(surroundingTiles);
				}
			}
		}
		foreach (JsonZone jsonZone in this.mapZones.Values)
		{
			if (jsonZone == null)
			{
				Debug.LogWarning("WARNING: Null zone found in ship " + this.strRegID + "'s mapZones.");
			}
			else
			{
				for (int i = 0; i < jsonZone.aTiles.Length; i++)
				{
					int num2 = jsonZone.aTiles[i];
					if (array.Length <= num2 || num2 < 0)
					{
						Debug.LogWarning(string.Concat(new object[]
						{
							"Trying to copy unavailable index: ",
							array.Length,
							"/",
							jsonZone.aTiles.Length,
							"/",
							this.aTiles.Count
						}));
					}
					else
					{
						jsonZone.aTiles[i] = this.aTiles.IndexOf(array[num2]);
					}
				}
			}
		}
		CrewSim.objInstance.workManager.RefreshTileIDs(this.strRegID, new List<Tile>(array));
		if (this.tfBGs != null)
		{
			this.tfBGs.Rotate(Vector3.back, 90f);
			this.tfBGs.position = new Vector3(this.tfBGs.position.y, -this.tfBGs.position.x, this.tfBGs.position.z);
		}
		CrewSim.bPoolVisUpdates = true;
	}

	public void MoveShip(Vector2 ptOffset)
	{
		Transform transform = this.gameObject.transform;
		Vector2 vector = new Vector2(transform.position.x, transform.position.y);
		transform.position = new Vector3(vector.x + ptOffset.x, vector.y + ptOffset.y, transform.position.z);
		foreach (CondOwner condOwner in this.mapICOs.Values)
		{
			Item item = condOwner.Item;
			if (!(item == null))
			{
				foreach (Block block in item.aBlocks)
				{
					block.UpdateStats();
				}
			}
		}
		ptOffset.x += this.vShipPos.x;
		ptOffset.y += this.vShipPos.y;
		this.vShipPos.Set(ptOffset.x, ptOffset.y);
		foreach (Room room in this.aRooms)
		{
			room.CO.tf.position = room.aTiles[0].tf.position;
		}
	}

	public List<Ship> GetAllDockedShips()
	{
		List<Ship> list = new List<Ship>();
		if (this.aDocked != null)
		{
			list.AddRange(this.aDocked);
		}
		else if (this.json != null && this.json.aDocked != null && this.json.aDocked.Length > 0)
		{
			foreach (string text in this.json.aDocked)
			{
				Ship shipByRegID = CrewSim.system.GetShipByRegID(text);
				if (text != null)
				{
					list.Add(shipByRegID);
				}
			}
		}
		return list;
	}

	public List<Ship> GetAllDockedShipsFull()
	{
		return this.GetAllDockedShips();
	}

	public List<Tile> GetAllDockedTiles()
	{
		List<Tile> list = new List<Tile>();
		list.AddRange(this.aTiles);
		foreach (Ship ship in this.aDocked)
		{
			if (ship != null)
			{
				list.AddRange(ship.aTiles);
			}
		}
		return list;
	}

	public static List<string> GetOwnedDockedShips(CondOwner coSelf, CondOwner coThem)
	{
		return Ship.GetOwnedDockedShips(coSelf, coThem.ship, false);
	}

	public static List<string> GetOwnedDockedShips(CondOwner coSelf, Ship otherShip, bool includeShipsWithZonesWeOwn = false)
	{
		List<string> list = new List<string>();
		if (coSelf == null)
		{
			return list;
		}
		List<string> shipsForOwner = CrewSim.system.GetShipsForOwner(coSelf.strID);
		Ship ship = otherShip;
		if (ship != null)
		{
			ship = CrewSim.system.GetNearestStation(ship.objSS.vPosx, ship.objSS.vPosy, true);
		}
		if (ship == null || shipsForOwner == null || ship.GetRangeTo(otherShip) > 1.336917421213002E-07)
		{
			return list;
		}
		List<Ship> shipsBySubString = CrewSim.system.GetShipsBySubString(ship.strRegID);
		List<Ship> list2 = new List<Ship>();
		foreach (Ship ship2 in shipsBySubString)
		{
			if (ship2 != null && !ship2.bDestroyed)
			{
				list2.AddRange(ship2.GetAllDockedShipsFull());
			}
		}
		foreach (Ship ship3 in list2)
		{
			if (ship3 != null)
			{
				bool flag = false;
				foreach (string text in shipsForOwner)
				{
					if (!(ship3.strRegID != text))
					{
						list.Add(text);
						flag = true;
					}
				}
				if (includeShipsWithZonesWeOwn && !flag)
				{
					List<JsonZone> zones = ship3.GetZones("IsZoneBarter", coSelf, false, ship3.LoadState == Ship.Loaded.Shallow);
					if (zones.Count > 0)
					{
						list.Add(ship3.strRegID);
					}
				}
			}
		}
		return list;
	}

	public double GetCondAmount(string strStatName, bool bAllowDocked)
	{
		double fAmount = 0.0;
		this.VisitCOs(new CondTrigger
		{
			aReqs = new string[]
			{
				strStatName
			}
		}, false, bAllowDocked, true, delegate(CondOwner co)
		{
			fAmount += co.GetCondAmount(strStatName);
		});
		return fAmount;
	}

	public string GetReactorGPMValue(string strKey)
	{
		if (this.Reactor != null)
		{
			return this.Reactor.GetGPMInfo("Panel A", strKey);
		}
		return string.Empty;
	}

	public void SetReactorGPMValue(string strName, string strValue)
	{
		if (this.Reactor != null)
		{
			this.Reactor.ApplyGPMChanges(new string[]
			{
				"Panel A," + strName + "," + strValue
			});
		}
	}

	public CondOwner Reactor
	{
		get
		{
			if (this.aCores.Count > 0)
			{
				return this.aCores[0];
			}
			return null;
		}
	}

	public void SetThrust(double fAmount)
	{
		bool flag = this.nLoadState >= Ship.Loaded.Edit;
		double num = fAmount / this.Mass;
		bool flag2 = this.bTorchDriveThrusting;
		this.IsUsingTorchDrive = (fAmount > 0.0);
		bool flag3 = flag2 != this.bTorchDriveThrusting || this.bTorchDriveThrusting;
		bool flag4 = false;
		if (this.IsDocked())
		{
			foreach (Ship ship in this.GetAllDockedShips())
			{
				if (ship.IsUsingTorchDrive)
				{
					flag4 = true;
					break;
				}
			}
		}
		float num2 = 0f;
		if (fAmount == 0.0)
		{
			this.aWPs.Clear();
			if (!flag4)
			{
				this.objSS.vAccIn.Set(0f, 0f);
			}
		}
		else
		{
			num2 = Mathf.Min(1f, (float)num / 9.81f / 7f);
			num2 = 0.25f + 0.75f * num2;
			num = num / 149597872.0 / 1000.0;
			float num3 = Mathf.Cos(this.objSS.fRot);
			float num4 = Mathf.Sin(this.objSS.fRot);
			float newX = -(float)num * num4;
			float newY = (float)num * num3;
			this.objSS.vAccIn.Set(newX, newY);
		}
		if (this.bTorchDriveThrusting && !flag4)
		{
			this.UpdateDockedShips(this.objSS, this.Gravity);
		}
		if (flag && flag3)
		{
			AudioManager.am.PlayAudioEmitter("ShipTorchLow", true, false);
			AudioManager.am.TweakAudioEmitter("ShipTorchLow", 1f, num2);
		}
	}

	private void WearManeuver(CondTrigger ctDamage, bool bPlayerNotice, float fDmgModifier)
	{
		if (this.fWearManeuver <= 30f || this.LoadState < Ship.Loaded.Edit || this.bNeedsGravSet)
		{
			return;
		}
		float num = 0.3f;
		float num2 = (this.nActiveStabilizers <= 0) ? 1f : (num + 1f / Mathf.Pow(2f, (float)this.nActiveStabilizers));
		fDmgModifier *= num2;
		List<CondOwner> cos = this.GetCOs(ctDamage, false, true, false);
		foreach (CondOwner co in cos)
		{
			this.ManeuverDamagePart(co, bPlayerNotice, fDmgModifier);
		}
		this.fWearManeuver = 0f;
	}

	public float RemoveGasMass(float fMassNeeded)
	{
		if (fMassNeeded <= 0f)
		{
			return 0f;
		}
		if (this.LoadState <= Ship.Loaded.Shallow)
		{
			if (this.IsAIShip)
			{
				fMassNeeded *= 0.75f;
			}
			float num = Mathf.Min((float)this.fShallowRCSRemass, fMassNeeded);
			this.fShallowRCSRemass -= (double)num;
			return num;
		}
		float num2 = 0f;
		List<CondOwner> list = new List<CondOwner>();
		foreach (CondOwner condOwner in this.aRCSDistros)
		{
			foreach (KeyValuePair<string, Vector2> keyValuePair in condOwner.mapPoints)
			{
				if (keyValuePair.Key.IndexOf("GasInput") >= 0)
				{
					list.Clear();
					condOwner.ship.GetCOsAtWorldCoords1(condOwner.GetPos(keyValuePair.Key, false), Ship.ctRCSGasInput, true, false, list);
					foreach (CondOwner condOwner2 in list)
					{
						GasContainer gasContainer = condOwner2.GasContainer;
						if (gasContainer != null)
						{
							num2 += (float)gasContainer.RemoveGasMass((double)(fMassNeeded - num2));
						}
						if (fMassNeeded <= num2)
						{
							break;
						}
					}
					if (fMassNeeded <= num2)
					{
						break;
					}
				}
			}
			if (fMassNeeded <= num2)
			{
				break;
			}
		}
		return num2;
	}

	public float CurrentRotorEfficiency
	{
		get
		{
			if (this.aRooms == null)
			{
				return 0f;
			}
			Room room = (from x in this.aRooms
			where x.Void
			select x).FirstOrDefault<Room>();
			if (room != null)
			{
				float num = (float)room.CO.GetCondAmount("StatGasPressure");
				return Mathf.Clamp(num / 100f, 0f, 1.5f);
			}
			return 0f;
		}
	}

	public void Maneuver(float fX, float fY, float fR, int nNoiseOnly, float fDeltaTime, Ship.EngineMode engineMode = Ship.EngineMode.RCS)
	{
		if (fDeltaTime <= 0f || this.objSS == null)
		{
			return;
		}
		bool flag = this.nLoadState >= Ship.Loaded.Edit;
		float num = Math.Abs(fX);
		num += Math.Abs(fY);
		num += Math.Abs(fR);
		bool flag2 = nNoiseOnly > 0 && num <= 1E-05f;
		num += (float)nNoiseOnly;
		float num2 = num;
		float currentRotorEfficiency = this.CurrentRotorEfficiency;
		if (engineMode == Ship.EngineMode.AUTO)
		{
			if (this.LiftRotorsThrustStrength > 0f && currentRotorEfficiency > 0f)
			{
				engineMode = Ship.EngineMode.MIXED;
			}
			else
			{
				engineMode = Ship.EngineMode.RCS;
			}
		}
		bool flag3 = (this.fRCSCount == 0f || this.nRCSDistroCount == 0) && engineMode == Ship.EngineMode.RCS;
		if (this.objSS.bIsBO || num <= 1E-05f || flag3)
		{
			this.StopManeuver(flag, flag2);
			return;
		}
		if (flag)
		{
			float num3 = 1f;
			int num4 = MathUtils.Rand(0, this.aRCSThrusters.Count, MathUtils.RandType.Flat, null);
			Item item = null;
			if (num4 < this.aRCSThrusters.Count)
			{
				CondOwner condOwner = this.aRCSThrusters[num4];
				item = condOwner.Item;
				num3 = 1f - (float)condOwner.GetDamageState();
			}
			double num5 = 0.5;
			if (!CrewSim.Paused && item != null && MathUtils.Rand(0.0, 1.0, MathUtils.RandType.Flat, null) < 0.4 && ((double)num3 >= num5 || (double)item.fFlickerAmount < 1.0))
			{
				double num6 = (double)(1f - num3 * num3) / (1.0 - num5 * num5);
				if (num6 > 1.0)
				{
					num6 = 1.0;
				}
				if (item.fFlickerAmount == 0f)
				{
					if (MathUtils.Rand(0.0, 1.0, MathUtils.RandType.Flat, null) < num6)
					{
						num *= (float)num6;
					}
				}
				else if (MathUtils.Rand(0.0, 1.0, MathUtils.RandType.Flat, null) >= num6)
				{
					num = 0f;
				}
			}
		}
		float num7 = 1f;
		if (engineMode == Ship.EngineMode.RCS || engineMode == Ship.EngineMode.MIXED)
		{
			float fMassNeeded = 0.728f * this.fRCSCount * num * fDeltaTime;
			if (!flag2)
			{
				num7 = this.RemoveGasMass(fMassNeeded);
			}
			if (num7 <= 0f && engineMode == Ship.EngineMode.RCS)
			{
				this.StopManeuver(flag, flag2);
				return;
			}
		}
		if (!flag2)
		{
			float num8 = num7 / num;
			num8 *= 100f;
			float num9 = (float)this.Mass;
			foreach (Ship ship in this.aDocked)
			{
				if (ship != null)
				{
					num9 += (float)ship.Mass;
				}
			}
			if (num9 <= 0f)
			{
				num9 = 0.01f;
			}
			float num10 = Mathf.Cos(this.objSS.fRot);
			float num11 = Mathf.Sin(this.objSS.fRot);
			float num12 = 0f;
			float num13 = 0f;
			if (engineMode == Ship.EngineMode.ROTOR || engineMode == Ship.EngineMode.MIXED)
			{
				float num14 = this.LiftRotorsThrustStrength * currentRotorEfficiency;
				float num15 = (fX * num10 - fY * num11) * num14 / 149597870f;
				float num16 = (fX * num11 + fY * num10) * num14 / 149597870f;
				fR += this.GetRotorMomentum();
				num12 = num15 / num9;
				num13 = num16 / num9;
			}
			if (engineMode == Ship.EngineMode.RCS || engineMode == Ship.EngineMode.MIXED)
			{
				float num15 = (fX * num10 - fY * num11) * num8 * 5.26077E-09f;
				float num16 = (fX * num11 + fY * num10) * num8 * 5.26077E-09f;
				num12 += num15 / num9 / fDeltaTime;
				num13 += num16 / num9 / fDeltaTime;
			}
			this.objSS.vAccRCS.x = num12;
			this.objSS.vAccRCS.y = num13;
			float num17 = fR * fDeltaTime;
			bool flag4 = (double)Mathf.Abs(this.objSS.fW) > 1E-07;
			if (flag4 && this.objSS.fW * (this.objSS.fW + num17) < 0f)
			{
				this.objSS.fW = 0f;
			}
			else
			{
				this.objSS.fW += num17;
				this.objSS.fA = num17 / fDeltaTime;
			}
			this.UpdateDockedShips(this.objSS, this.Gravity);
		}
		if (flag)
		{
			if (Ship.OnManeuver == null)
			{
				Ship.OnManeuver = new ManeuverEvent();
			}
			if (engineMode == Ship.EngineMode.ROTOR || engineMode == Ship.EngineMode.MIXED)
			{
				Ship.OnManeuver.Invoke(this.strRegID, num > 0f);
			}
			if (num7 > 0f && num > 0f && engineMode != Ship.EngineMode.ROTOR)
			{
				AudioManager.am.PlayAudioEmitter("ShipRCSFwd", true, false);
			}
			else
			{
				AudioManager.am.StopAudioEmitter("ShipRCSFwd");
			}
			if (num7 > 0f && num > 1f && engineMode != Ship.EngineMode.ROTOR)
			{
				AudioManager.am.PlayAudioEmitter("ShipRCSRot", true, false);
			}
			else
			{
				AudioManager.am.StopAudioEmitter("ShipRCSRot");
			}
			if (num7 > 0f && num > 2f && engineMode != Ship.EngineMode.ROTOR)
			{
				AudioManager.am.PlayAudioEmitter("ShipRCSSide", true, false);
			}
			else
			{
				AudioManager.am.StopAudioEmitter("ShipRCSSide");
			}
		}
		this.fLastRCSCount = num2;
	}

	public void UnlockFromOrbit(bool destroyBO = true)
	{
		if (this.objSS == null || !this.objSS.bOrbitLocked)
		{
			return;
		}
		this.objSS.UpdateTime(StarSystem.fEpoch, false);
		BodyOrbit bo = CrewSim.system.GetBO(this.objSS.strBOPORShip);
		if (!this.objSS.bBOLocked)
		{
			this.objSS.strBOPORShip = null;
		}
		this.objSS.bOrbitLocked = false;
		if (destroyBO && bo != null && bo.IsShipOrbit())
		{
			CrewSim.system.RemoveBO(bo);
		}
	}

	public BodyOrbit LockToOrbit()
	{
		if (this.objSS == null || this.objSS.IsAccelerating)
		{
			return null;
		}
		BodyOrbit bodyOrbit = CrewSim.system.GetBO(this.strRegID);
		if (bodyOrbit == null)
		{
			BodyOrbit bodyOrbit2 = null;
			Vector2 zero = Vector2.zero;
			BodyOrbit bodyOrbit3 = CrewSim.system.GetGreatestGravBO(this.objSS, StarSystem.fEpoch, ref zero, ref bodyOrbit2);
			bodyOrbit3.UpdateTime(StarSystem.fEpoch, true, true);
			int num = 15;
			while (num > 0 && bodyOrbit3.IsEscaping(this))
			{
				num--;
				if (bodyOrbit3.boParent == null || bodyOrbit3 == bodyOrbit3.boParent)
				{
					return null;
				}
				bodyOrbit3 = bodyOrbit3.boParent;
			}
			if (bodyOrbit3.RadiusAtmo > 0.0 && MathUtils.GetDistance(bodyOrbit3.dXReal, bodyOrbit3.dXReal, this.objSS.vPosx, this.objSS.vPosy) < bodyOrbit3.RadiusAtmo)
			{
				return null;
			}
			bodyOrbit = BodyOrbit.CreateBOFromShip(this, bodyOrbit3);
			if (bodyOrbit == null)
			{
				return null;
			}
			CrewSim.system.AddBO(bodyOrbit, bodyOrbit.boParent);
		}
		this.objSS.LockToOrbit(bodyOrbit, -1.0);
		return bodyOrbit;
	}

	private float GetRotorMomentum()
	{
		if (this.aActiveHeavyLiftRotors.Count == 0)
		{
			return 0f;
		}
		float num = 0f;
		foreach (CondOwner condOwner in this.aActiveHeavyLiftRotors)
		{
			if (!(condOwner == null))
			{
				Rotor component = condOwner.GetComponent<Rotor>();
				if (!(component == null))
				{
					num += component.Momentum;
				}
			}
		}
		return num;
	}

	public void StopManeuver(bool bPlayerNotice, bool bNoiseOnly)
	{
		if (bPlayerNotice)
		{
			AudioManager.am.StopAudioEmitter("ShipRCSFwd");
			AudioManager.am.StopAudioEmitter("ShipRCSRot");
			AudioManager.am.StopAudioEmitter("ShipRCSSide");
			Ship.OnManeuver.Invoke(this.strRegID, false);
		}
		if (!bNoiseOnly)
		{
			this.objSS.vAccRCS = Vector2.zero;
			this.objSS.fA = 0f;
			this.UpdateDockedShips(this.objSS, this.Gravity);
		}
		this.fLastRCSCount = 0f;
	}

	private void PlayCreakAudio(string strLoot)
	{
		Loot loot = DataHandler.GetLoot(strLoot);
		if (loot != null)
		{
			string lootNameSingle = loot.GetLootNameSingle("SHIP_CREAK");
			AudioManager.am.PlayAudioEmitter(lootNameSingle, false, true);
			AudioManager.am.TweakAudioEmitter(lootNameSingle, 0.85f - MathUtils.Rand(0f, 0.15f, MathUtils.RandType.Flat, null), MathUtils.Rand(0.5f, 1f, MathUtils.RandType.Flat, null));
		}
	}

	private void ManeuverDamagePart(CondOwner co, bool bPlayerNotice, float fModifier)
	{
		double num = (double)MathUtils.Rand(0f, 0.00167f, MathUtils.RandType.Flat, null);
		num *= co.GetCondAmount("StatDamageMax");
		num /= 4000.0;
		num *= (double)fModifier;
		co.AddCondAmount("StatDamage", num, 0.0, 0f);
		if (bPlayerNotice && co.GetDamageState() <= 0.0)
		{
			BeatManager.ResetTensionTimer();
		}
	}

	private void UpdateDockedShips(ShipSitu currentSitu, double gravity)
	{
		foreach (Ship ship in this.aDocked)
		{
			if (ship != null && !ship.bDestroyed)
			{
				ship.objSS.vAccRCS.x = currentSitu.vAccRCS.x;
				ship.objSS.vAccRCS.y = currentSitu.vAccRCS.y;
				ship.objSS.fW = currentSitu.fW;
				ship.objSS.fA = currentSitu.fA;
				ship.Gravity = gravity;
				ship.objSS.vAccIn.x = currentSitu.vAccIn.x;
				ship.objSS.vAccIn.y = currentSitu.vAccIn.y;
			}
		}
	}

	public double GetRCSRemain()
	{
		if (this.LoadState <= Ship.Loaded.Shallow)
		{
			return this.fShallowRCSRemass;
		}
		double num = 0.0;
		List<CondOwner> list = new List<CondOwner>();
		foreach (CondOwner condOwner in this.aRCSDistros)
		{
			foreach (KeyValuePair<string, Vector2> keyValuePair in condOwner.mapPoints)
			{
				if (keyValuePair.Key.IndexOf("GasInput") >= 0)
				{
					list.Clear();
					condOwner.ship.GetCOsAtWorldCoords1(condOwner.GetPos(keyValuePair.Key, false), Ship.ctRCSGasInput, true, false, list);
					foreach (CondOwner condOwner2 in list)
					{
						GasContainer gasContainer = condOwner2.GasContainer;
						if (gasContainer != null)
						{
							num += gasContainer.Mass;
						}
					}
				}
			}
		}
		return num;
	}

	public double GetRCSMax()
	{
		if (this.LoadState <= Ship.Loaded.Shallow)
		{
			if (this.fShallowRCSRemassMax == 0.0)
			{
				this.fShallowRCSRemassMax = this.fShallowRCSRemass;
			}
			return this.fShallowRCSRemassMax;
		}
		double num = 0.0;
		List<CondOwner> list = new List<CondOwner>();
		foreach (CondOwner condOwner in this.aRCSDistros)
		{
			foreach (KeyValuePair<string, Vector2> keyValuePair in condOwner.mapPoints)
			{
				if (keyValuePair.Key.IndexOf("GasInput") >= 0)
				{
					list.Clear();
					condOwner.ship.GetCOsAtWorldCoords1(condOwner.GetPos(keyValuePair.Key, false), Ship.ctRCSGasInput, true, false, list);
					foreach (CondOwner condOwner2 in list)
					{
						double fMols = condOwner2.GetCondAmount("StatGasPressureMax") * condOwner2.GetCondAmount("StatVolume") / 293.0 / 0.008314000442624092;
						num += GasContainer.GetGasMass("N2", fMols);
					}
				}
			}
		}
		return num;
	}

	public string GetCurrentAICommandDescription
	{
		get
		{
			AIShip aishipByRegID = AIShipManager.GetAIShipByRegID(this.strRegID);
			return (aishipByRegID == null) ? "Idle" : aishipByRegID.ActiveCommandNameDescription;
		}
	}

	public string GetMarketStatus
	{
		get
		{
			return MarketManager.GetStatusForShip(this.strRegID);
		}
	}

	public double GetRCSMinimumFuelAmount
	{
		get
		{
			double num = 1.2 * AIShipManager.GetDeltaVNeededToTargetFullTrip(this, AIShipManager.ShipATCLast.objSS, AIShip.CalculateMaxSpeed(this, null));
			return num * (double)this.fRCSCount * 0.7279999852180481 / this.RCSAccelMaxUndocked;
		}
	}

	public List<CondOwner> GetRCSCans()
	{
		List<CondOwner> list = new List<CondOwner>();
		if (this.LoadState <= Ship.Loaded.Shallow)
		{
			return list;
		}
		List<CondOwner> list2 = new List<CondOwner>();
		foreach (CondOwner condOwner in this.aRCSDistros)
		{
			foreach (KeyValuePair<string, Vector2> keyValuePair in condOwner.mapPoints)
			{
				if (keyValuePair.Key.IndexOf("GasInput") >= 0)
				{
					list2.Clear();
					condOwner.ship.GetCOsAtWorldCoords1(condOwner.GetPos(keyValuePair.Key, false), Ship.ctRCSGasInput, true, false, list2);
					foreach (CondOwner item in list2)
					{
						list.Add(item);
					}
				}
			}
		}
		return list;
	}

	public void AIRefuel()
	{
		this.fShallowRCSRemass = this.GetRCSMax();
	}

	public float RefuelRCS(float amount)
	{
		if (this.LoadState > Ship.Loaded.Shallow)
		{
			List<CondOwner> rcscans = this.GetRCSCans();
			double gasMass = GasContainer.GetGasMass("N2", 1.0);
			foreach (CondOwner condOwner in rcscans)
			{
				GasContainer gasContainer = condOwner.GasContainer;
				if (!(gasContainer == null))
				{
					float num = Convert.ToSingle(condOwner.GetCondAmount("StatVolume") * condOwner.GetCondAmount("StatGasPressureMax") / 0.008314000442624092 / condOwner.GetCondAmount("StatGasTemp") * gasMass);
					float num2 = (float)(condOwner.GetCondAmount("StatGasPressure") / condOwner.GetCondAmount("StatGasPressureMax") * (double)num);
					float num3 = Mathf.Min(num - num2, amount);
					gasContainer.AddGasMols("N2", (double)num3 / gasMass, true);
					amount -= num3;
					if (amount <= 0f)
					{
						amount = 0f;
						break;
					}
				}
			}
			return amount;
		}
		double rcsmax = this.GetRCSMax();
		if (this.fShallowMass + (double)amount < rcsmax)
		{
			this.fShallowMass += (double)amount;
			return 0f;
		}
		this.fShallowMass = rcsmax;
		return (float)(this.fShallowMass + (double)amount - rcsmax);
	}

	public void DamageRayRandom(JsonAttackMode jam, float fMult, CondOwner coAttacker, bool bAllowDocked)
	{
		float angle = MathUtils.Rand(0f, 360f, MathUtils.RandType.Flat, null);
		Vector3 b = new Vector3((float)this.nCols / 2f, (float)(-(float)this.nRows) / 2f, 0f);
		float magnitude = b.magnitude;
		Vector3 vStart = new Vector3(this.vShipPos.x, this.vShipPos.y) + b + Quaternion.AngleAxis(angle, Vector3.forward) * Vector3.up * magnitude;
		this.DamageRay(vStart, -vStart.normalized, magnitude * 2f, fMult, jam, coAttacker, bAllowDocked);
	}

	public void DamageRay(Vector3 vStart, Vector3 vDir, float fRadius, float fMult, JsonAttackMode jam, CondOwner coAttacker, bool bAllowDocked)
	{
		if (jam == null || this.gameObject == null || !this.gameObject.activeInHierarchy)
		{
			return;
		}
		int num = 256;
		num |= 4;
		num = ~num;
		Ray ray = new Ray(vStart, vDir);
		RaycastHit[] array = Physics.RaycastAll(ray, fRadius, num);
		array = (from go in array
		orderby go.distance
		select go).ToArray<RaycastHit>();
		double num2 = (double)jam.fDmgEnv * jam.GetDmgAmount(coAttacker) * (double)fMult;
		double num3 = (double)jam.fDmgBlunt * jam.GetDmgAmount(coAttacker) * (double)fMult;
		double num4 = (double)jam.fDmgCut * jam.GetDmgAmount(coAttacker) * (double)fMult;
		if (CrewSim.BulletTrail != null)
		{
			TrailRenderer trail = UnityEngine.Object.Instantiate<TrailRenderer>(CrewSim.BulletTrail, vStart, Quaternion.identity);
			Vector3 vEnd = vStart + vDir * fRadius;
			if (array.Length > 0)
			{
				vEnd = array[array.Length - 1].point;
			}
			CrewSim.objInstance.StartCoroutine(CrewSim.objInstance.SpawnTrail(trail, vEnd));
		}
		foreach (RaycastHit raycastHit in array)
		{
			Destructable component = raycastHit.transform.GetComponent<Destructable>();
			if (component != null)
			{
				if (num2 <= 0.0)
				{
					break;
				}
				if (bAllowDocked || component.CO.ship == this)
				{
					double num5 = component.DmgLeft("StatDamage");
					if (num5 > 0.0)
					{
						if (num2 < num5)
						{
							num5 = num2;
						}
						component.CO.AddCondAmount("StatDamage", num5, 0.0, 0f);
						component.DamageCheck();
						component.CO.EndTurn();
						num3 *= (num2 - num5) / num2;
						num4 *= (num2 - num5) / num2;
						num2 -= num5;
						if (num2 < 0.0)
						{
							num2 = 0.0;
						}
						if (num4 < 0.0)
						{
							num4 = 0.0;
						}
						if (num3 < 0.0)
						{
							num3 = 0.0;
						}
						if (coAttacker != null)
						{
							component.CO.ApplyFactionReps(coAttacker, -(float)num5);
						}
					}
				}
			}
			else
			{
				if (num2 <= 0.0 || num3 + num4 <= 0.0)
				{
					break;
				}
				CondOwner component2 = raycastHit.transform.GetComponent<CondOwner>();
				if (!(component2 == null))
				{
					if (bAllowDocked || component2.ship == this)
					{
						bool bBlunt = (double)jam.fDmgBlunt > 0.5;
						bool bCut = (double)jam.fDmgCut > 0.5;
						Wound woundLocation = component2.GetWoundLocation(bBlunt, bCut);
						if (!(woundLocation == null))
						{
							if (woundLocation.DamageLeft() > 0.0)
							{
								double num6 = woundLocation.DamageLeft();
								double num7 = 1.0;
								if (num3 * num7 > num6)
								{
									num7 = num6 / (num3 * num7);
								}
								if (num4 * num7 > num6)
								{
									num7 = num6 / (num4 * num7);
								}
								woundLocation.Damage(num3 * num7, num4 * num7, (double)jam.fPenetration, coAttacker, jam.strNameFriendly, true, null, false);
								num2 -= num6 * num7;
								num3 -= num6 * num7;
								num4 -= num6 * num7;
								if (num2 < 0.0)
								{
									num2 = 0.0;
								}
								if (num4 < 0.0)
								{
									num4 = 0.0;
								}
								if (num3 < 0.0)
								{
									num3 = 0.0;
								}
								component2.PlayHitAnim(num3, num4);
								if (coAttacker != null)
								{
									component2.ApplyFactionReps(coAttacker, -(float)(num6 * num7));
								}
							}
						}
					}
				}
			}
		}
	}

	public void DamageRadius(Vector3 vStart, float fRadius, float fMult, JsonAttackMode jam, CondOwner coAttacker, bool bAllowDocked)
	{
		if (jam == null || this.gameObject == null || !this.gameObject.activeInHierarchy)
		{
			return;
		}
		List<CondOwner> list = new List<CondOwner>();
		JsonZone zoneFromTileRadius = TileUtils.GetZoneFromTileRadius(this, vStart, Mathf.RoundToInt(fRadius), true, false);
		list = this.GetCOsInZone(zoneFromTileRadius, DataHandler.GetCondTrigger("TIsExplosionTarget"), false, true);
		double num = 0.0;
		double num2 = 0.0;
		foreach (CondOwner condOwner in list)
		{
			double num3 = (double)jam.fDmgEnv * jam.GetDmgAmount(coAttacker) * (double)fMult;
			num = (double)jam.fDmgBlunt * jam.GetDmgAmount(coAttacker) * (double)fMult;
			num2 = (double)jam.fDmgCut * jam.GetDmgAmount(coAttacker) * (double)fMult;
			Destructable component = condOwner.GetComponent<Destructable>();
			if (component != null)
			{
				if (num3 <= 0.0)
				{
					break;
				}
				if (bAllowDocked || component.CO.ship == this)
				{
					double num4 = component.DmgLeft("StatDamage");
					if (num4 > 0.0)
					{
						if (coAttacker != null)
						{
							component.CO.ApplyFactionReps(coAttacker, -(float)num4);
						}
						if (num3 >= 2.0 * num4)
						{
							Debug.Log(string.Concat(new object[]
							{
								"Annihilating ",
								condOwner.strNameShort,
								": ",
								num4
							}));
							condOwner.RemoveFromCurrentHome(true);
							condOwner.Destroy();
						}
						else
						{
							if (num4 > num3)
							{
								num4 = num3;
							}
							Debug.Log(string.Concat(new object[]
							{
								"Damaging ",
								condOwner.strNameShort,
								": ",
								num4
							}));
							component.CO.AddCondAmount("StatDamage", num4, 0.0, 0f);
							component.DamageCheck();
							component.CO.EndTurn();
						}
					}
				}
			}
			else
			{
				if (num3 <= 0.0 || num + num2 <= 0.0)
				{
					break;
				}
				if (!(condOwner == null))
				{
					if (bAllowDocked || condOwner.ship == this)
					{
						bool flag = (double)jam.fDmgBlunt > 0.5;
						bool flag2 = (double)jam.fDmgCut > 0.5;
						List<Wound> allWounds = condOwner.GetAllWounds();
						double num5 = 0.0;
						foreach (Wound wound in allWounds)
						{
							if (!(wound == null))
							{
								if (wound.DamageLeft() > 0.0)
								{
									double num6 = wound.DamageLeft();
									double num7 = 1.0;
									if (num * num7 > num6)
									{
										num7 = num6 / (num * num7);
									}
									if (num2 * num7 > num6)
									{
										num7 = num6 / (num2 * num7);
									}
									wound.Damage(num * num7, num2 * num7, (double)jam.fPenetration, coAttacker, jam.strNameFriendly, true, null, false);
									num5 += num6 * num7;
								}
							}
						}
						condOwner.PlayHitAnim(num * (double)allWounds.Count, num2 * (double)allWounds.Count);
						if (coAttacker != null)
						{
							condOwner.ApplyFactionReps(coAttacker, -(float)num5);
						}
					}
				}
			}
		}
	}

	public double GetTotalCOPrice()
	{
		List<CondOwner> cos = this.GetCOs(null, true, false, true);
		double num = 0.0;
		foreach (CondOwner condOwner in cos)
		{
			if (condOwner.HasCond("IsInstalled"))
			{
				num += condOwner.GetCondAmount("StatBasePrice") * 100.0;
			}
		}
		return num;
	}

	public double GetRangeTo(Ship other)
	{
		if (other == null)
		{
			return double.PositiveInfinity;
		}
		return this.objSS.GetRangeTo(other.objSS);
	}

	public bool bDocked
	{
		get
		{
			return this.IsDocked();
		}
	}

	public bool IsDocked()
	{
		if (this.aDocked != null)
		{
			return this.aDocked.Count > 0;
		}
		return this.json != null && this.json.aDocked != null && this.json.aDocked.Length > 0;
	}

	public bool IsDockedFull()
	{
		return this.IsDocked();
	}

	public bool IsDerelict()
	{
		return this.DMGStatus == Ship.Damage.Derelict;
	}

	public bool IsFlyingDark()
	{
		return !this.bXPDRAntenna || this.strXPDR == "?" || string.IsNullOrEmpty(this.strXPDR);
	}

	public bool HasOKLGSalvageLicense()
	{
		List<CondOwner> cos = this.GetCOs(Ship.ctPermitOKLG, true, true, true);
		return cos.Count > 0;
	}

	public bool TowBraceSecured()
	{
		if (!this.bTowBraceSecured && this.aDocked != null)
		{
			foreach (Ship ship in this.aDocked)
			{
				if (ship != null)
				{
					if (ship.bTowBraceSecured)
					{
						return true;
					}
				}
			}
		}
		return this.bTowBraceSecured;
	}

	public bool IsActive()
	{
		return !this.IsDocked();
	}

	public bool IsStation(bool bIgnoreDocks = false)
	{
		return (bIgnoreDocks || this.DockCount > 0) && this.objSS.bIsBO;
	}

	public bool IsSubStation()
	{
		return this._subStation;
	}

	public bool IsStationHidden(bool bIgnoreDocks = false)
	{
		return (bIgnoreDocks || this.DockCount <= 0) && this.objSS != null && this.objSS.bIsBO && this.Classification != Ship.TypeClassification.Waypoint;
	}

	public bool IsGroundStation()
	{
		return this.Classification == Ship.TypeClassification.GroundStation || this.Classification == Ship.TypeClassification.GroundStationUnfinished;
	}

	public bool IsPlayerShip()
	{
		return this == CrewSim.GetSelectedCrew().ship;
	}

	public Ship GetDockedShip(string strRegID)
	{
		if (this.LoadState >= Ship.Loaded.Edit)
		{
			foreach (Ship ship in this.aDocked)
			{
				if (ship != null)
				{
					if (ship.strRegID == strRegID)
					{
						return ship;
					}
				}
			}
		}
		else if (this.LoadState == Ship.Loaded.Shallow && this.json != null && this.json.aDocked != null)
		{
			foreach (string a in this.json.aDocked)
			{
				if (a == strRegID)
				{
					return CrewSim.system.GetShipByRegID(strRegID);
				}
			}
		}
		return null;
	}

	public bool IsDockedWith(string regId)
	{
		Ship shipByRegID = CrewSim.system.GetShipByRegID(regId);
		return shipByRegID != null && this.IsDockedWith(shipByRegID);
	}

	public bool IsDockedWith(Ship ship)
	{
		return ship != null && ((this.aDocked != null && this.aDocked.IndexOf(ship) >= 0) || (this.LoadState == Ship.Loaded.Shallow && this.json != null && this.json.aDocked != null && Array.IndexOf<string>(this.json.aDocked, ship.strRegID) >= 0));
	}

	public void ShowRoomIDs(bool bShow)
	{
		foreach (Room room in this.aRooms)
		{
			room.ShowID(bShow);
		}
	}

	public JsonItem GetJsonItem(CondOwner co)
	{
		if (co == null || co.tf == null)
		{
			Debug.Log("ERROR: Trying to get save info for null CO: " + co);
			return null;
		}
		List<JsonGUIPropMap> list = new List<JsonGUIPropMap>();
		JsonItem jsonItem = new JsonItem();
		jsonItem.strName = co.strCODef;
		jsonItem.fRotation = co.tf.rotation.eulerAngles.z;
		Item item = co.Item;
		if (item != null)
		{
			jsonItem.fRotation = item.fLastRotation;
		}
		jsonItem.fX = co.tf.position.x;
		jsonItem.fY = co.tf.position.y;
		jsonItem.strID = co.strID;
		if (co.AlwaysLoad)
		{
			jsonItem.bForceLoad = new bool?(co.AlwaysLoad);
		}
		if (co.coStackHead != null)
		{
			jsonItem.strParentID = co.coStackHead.strID;
		}
		else if (co.objCOParent != null)
		{
			if (co.slotNow != null)
			{
				jsonItem.strSlotParentID = co.objCOParent.strID;
			}
			else
			{
				jsonItem.strParentID = co.objCOParent.strID;
			}
		}
		else
		{
			jsonItem.strParentID = null;
		}
		foreach (string text in co.mapGUIPropMaps.Keys)
		{
			list.Add(new JsonGUIPropMap
			{
				strName = text,
				dictGUIPropMap = DataHandler.ConvertDictToStringArray(co.mapGUIPropMaps[text])
			});
		}
		jsonItem.aGPMSettings = list.ToArray();
		return jsonItem;
	}

	public JsonShip GetJSON(string strName, bool bSaveGame, List<CondOwner> allCos = null)
	{
		int count = this.aTiles.Count;
		JsonShip jsonShip = null;
		Debug.Log("Getting JSON for ship: " + this.strRegID + " - " + this.model);
		if (this.nLoadState >= Ship.Loaded.Edit)
		{
			this.MarketConfigs.Clear();
			jsonShip = new JsonShip();
			jsonShip.aItems = new JsonItem[0];
			jsonShip.aCrew = new JsonItem[0];
			if (this.nRows > 0 && this.nCols > 0)
			{
				this.dimensions = ((float)this.nCols * 0.32f).ToString("#.00") + "m x " + ((float)this.nRows * 0.32f).ToString("#.00") + "m";
				bool activeInHierarchy = this.gameObject.activeInHierarchy;
				this.gameObject.SetActive(true);
				TileUtils.TrimTiles(this);
				this.RotateCW();
				TileUtils.TrimTiles(this);
				this.RotateCW();
				TileUtils.TrimTiles(this);
				this.RotateCW();
				TileUtils.TrimTiles(this);
				this.RotateCW();
				if (count != this.aTiles.Count)
				{
					Debug.Log(string.Concat(new object[]
					{
						"Ship ",
						this.strRegID,
						" changed tiles from ",
						count,
						" to ",
						this.aTiles.Count,
						", recalcing rooms."
					}));
					this.CreateRooms(null);
					if (allCos != null)
					{
						foreach (Room room in this.aRooms)
						{
							if (room != null && room.CO != null && !allCos.Contains(room.CO))
							{
								allCos.Add(room.CO);
							}
						}
					}
				}
				this.gameObject.SetActive(activeInHierarchy);
			}
		}
		else
		{
			jsonShip = this.json.Clone();
			if (jsonShip.aItems == null)
			{
				jsonShip.aItems = new JsonItem[0];
			}
			if (jsonShip.aCrew == null)
			{
				jsonShip.aCrew = new JsonItem[0];
			}
		}
		this.SaveCOs(bSaveGame, jsonShip, allCos);
		jsonShip.shipCO = this.ShipCO.GetJSONSave();
		jsonShip.commData = this.Comms.GetJson();
		jsonShip.strName = strName;
		jsonShip.strRegID = this.strRegID;
		jsonShip.nCurrentWaypoint = this.nCurrentWaypoint;
		jsonShip.fTimeEngaged = this.fTimeEngaged;
		jsonShip.fWearManeuver = this.fWearManeuver;
		jsonShip.fWearAccrued = (float)this.fWearAccrued;
		jsonShip.vShipPos = this.vShipPos;
		jsonShip.bNoCollisions = this.bNoCollisions;
		jsonShip.bLocalAuthority = this.bLocalAuthority;
		jsonShip.bAIShip = this.bAIShip;
		jsonShip.dLastScanTime = this.dLastScanTime;
		jsonShip.objSS = this.objSS.GetJSON();
		jsonShip.DMGStatus = this.DMGStatus;
		jsonShip.fLastVisit = this.fLastVisit;
		jsonShip.fFirstVisit = this.fFirstVisit;
		jsonShip.fAIDockingExpire = this.fAIDockingExpire;
		jsonShip.fAIPauseTimer = this.fAIPauseTimer;
		jsonShip.bPrefill = this.bPrefill;
		jsonShip.bBreakInUsed = this.bBreakInUsed;
		if (this.shipUndock != null)
		{
			jsonShip.strUndockID = this.shipUndock.strRegID;
		}
		if (this.shipScanTarget != null)
		{
			jsonShip.strScanTargetID = this.shipScanTarget.strRegID;
		}
		if (this.shipStationKeepingTarget != null)
		{
			jsonShip.strStationKeepingTargetID = this.shipStationKeepingTarget.strRegID;
		}
		if (this.shipSituTarget != null)
		{
			jsonShip.objSituScanTarget = this.shipSituTarget.GetJSON();
		}
		if (this.json != null && this.json.aConstructionTemplates != null)
		{
			jsonShip.aConstructionTemplates = (JsonShipConstructionTemplate[])this.json.aConstructionTemplates.Clone();
		}
		if (this.MarketConfigs != null)
		{
			jsonShip.aMarketConfigs = this.MarketConfigs.CloneShallow<string, string>();
		}
		if (this.aLog != null)
		{
			jsonShip.aLog = new JsonShipLog[this.aLog.Count];
			for (int i = 0; i < this.aLog.Count; i++)
			{
				jsonShip.aLog[i] = this.aLog[i].Clone();
			}
		}
		jsonShip.strLaw = this.strLaw;
		jsonShip.strParallax = this.strParallax;
		jsonShip.make = this.make;
		jsonShip.model = this.model;
		jsonShip.year = this.year;
		jsonShip.designation = this.designation;
		if (this.rating != null)
		{
			jsonShip.aRating = this.rating;
		}
		jsonShip.dimensions = this.dimensions;
		jsonShip.publicName = this.publicName;
		jsonShip.origin = this.origin;
		jsonShip.description = this.description;
		jsonShip.fShallowMass = this.Mass;
		jsonShip.fShallowRCSRemass = this.GetRCSRemain();
		jsonShip.fShallowRCSRemassMax = this.GetRCSMax();
		jsonShip.fLastQuotedPrice = this.fLastQuotedPrice;
		jsonShip.nRCSCount = this.fRCSCount;
		jsonShip.fShallowRotorStrength = this.fShallowRotorStrength;
		jsonShip.nRCSDistroCount = this.nRCSDistroCount;
		jsonShip.nDockCount = this.nDockCount;
		if (this.LoadState >= Ship.Loaded.Edit)
		{
			jsonShip.nO2PumpCount = this.aO2AirPumps.Count;
			jsonShip.bXPDRAntenna = this.bXPDRAntenna;
		}
		jsonShip.bFusionTorch = this.bFusionReactorRunning;
		jsonShip.strXPDR = this.strXPDR;
		jsonShip.bShipHidden = this.bShipHidden;
		jsonShip.bIsUnderConstruction = this.bIsUnderConstruction;
		jsonShip.strTemplateName = this.strTemplateName;
		jsonShip.nConstructionProgress = this.nConstructionProgress;
		jsonShip.nInitConstructionProgress = this.nInitConstructionProgress;
		jsonShip.fShallowFusionRemain = this.fShallowFusionRemain;
		jsonShip.fFusionThrustMax = this.fFusionThrustMax;
		jsonShip.fFusionPelletMax = this.fFusionPelletMax;
		jsonShip.fEpochNextGrav = this.fEpochNextGrav;
		jsonShip.fBreakInMultiplier = this.fBreakInMultiplier;
		jsonShip.ShipType = this.Classification;
		List<string> list = new List<string>();
		foreach (JsonFaction jsonFaction in this.aFactions)
		{
			list.Add(jsonFaction.strName);
		}
		jsonShip.aFactions = list.ToArray();
		list.Clear();
		if (this.aRooms.Count > 0)
		{
			List<JsonRoom> list2 = new List<JsonRoom>();
			foreach (Room room2 in this.aRooms)
			{
				list2.Add(room2.GetJSONSave());
			}
			jsonShip.aRooms = list2.ToArray();
		}
		if (this.nLoadState >= Ship.Loaded.Edit)
		{
			foreach (Ship ship in this.aDocked)
			{
				if (ship != null)
				{
					list.Add(ship.strRegID);
				}
			}
			jsonShip.aDocked = list.ToArray();
			list.Clear();
			foreach (string item in this.aProxCurrent)
			{
				list.Add(item);
			}
			jsonShip.aProxCurrent = list.ToArray();
			list.Clear();
			foreach (string item2 in this.aProxIgnores)
			{
				list.Add(item2);
			}
			jsonShip.aProxIgnores = list.ToArray();
			list.Clear();
			foreach (string item3 in this.aTrackCurrent)
			{
				list.Add(item3);
			}
			jsonShip.aTrackCurrent = list.ToArray();
			list.Clear();
			foreach (string item4 in this.aTrackIgnores)
			{
				list.Add(item4);
			}
			jsonShip.aTrackIgnores = list.ToArray();
			list.Clear();
			List<JsonShipSitu> list3 = new List<JsonShipSitu>();
			List<float> list4 = new List<float>();
			foreach (WaypointShip waypointShip in this.aWPs)
			{
				list3.Add(waypointShip.objSS.GetJSON());
				list4.Add(waypointShip.fTime);
			}
			jsonShip.aWPs = list3.ToArray();
			jsonShip.aWPTimes = list4.ToArray();
			list3.Clear();
			list4.Clear();
			jsonShip.aZones = new JsonZone[this.mapZones.Values.Count];
			int num = 0;
			foreach (JsonZone jsonZone in this.mapZones.Values)
			{
				jsonShip.aZones[num] = jsonZone.Clone();
				num++;
			}
			this.fShallowRotorStrength = -1f;
			this.fShallowRotorStrength = this.LiftRotorsThrustStrength;
			jsonShip.fShallowRotorStrength = this.fShallowRotorStrength;
			List<float[]> list5 = new List<float[]>();
			List<float[]> list6 = new List<float[]>();
			List<string> list7 = new List<string>();
			foreach (KeyValuePair<string, List<Vector2>> keyValuePair in this.dictBGs)
			{
				list7.Add(keyValuePair.Key);
				List<float> list8 = new List<float>();
				List<float> list9 = new List<float>();
				foreach (Vector2 vector in keyValuePair.Value)
				{
					list8.Add(vector.x);
					list9.Add(vector.y);
				}
				list5.Add(list8.ToArray());
				list6.Add(list9.ToArray());
			}
			jsonShip.aBGXs = list5.ToArray();
			jsonShip.aBGYs = list6.ToArray();
			jsonShip.aBGNames = list7.ToArray();
			jsonShip.aUniques = this.json.aUniques;
		}
		return jsonShip;
	}

	public void SaveCOs(bool bSaveGame, JsonShip jShip, List<CondOwner> aICOs = null)
	{
		List<CondOwner> list = new List<CondOwner>();
		list = (aICOs ?? this.GetCOs(null, true, false, true));
		if (jShip.aItems == null)
		{
			jShip.aItems = new JsonItem[0];
		}
		if (jShip.aCrew == null)
		{
			jShip.aCrew = new JsonItem[0];
		}
		List<JsonItem> list2 = new List<JsonItem>(jShip.aItems);
		List<JsonItem> list3 = new List<JsonItem>(jShip.aCrew);
		List<JsonItem> list4 = new List<JsonItem>();
		List<JsonPlaceholder> list5 = new List<JsonPlaceholder>();
		if (jShip.aPlaceholders != null)
		{
			list5.AddRange(jShip.aPlaceholders);
		}
		foreach (CondOwner condOwner in list)
		{
			if (condOwner == null)
			{
				Debug.Log("Skipping null objICO");
			}
			else
			{
				bool flag = condOwner.Crew != null;
				bool flag2 = false;
				if (condOwner.HasCond("IsLootSpawner"))
				{
					string text = condOwner.mapGUIPropMaps["Panel A"]["strType"];
					if (text.IndexOf("Pspec") >= 0)
					{
						flag2 = true;
					}
				}
				if (!flag || bSaveGame)
				{
					bool flag3 = false;
					for (int i = 0; i < jShip.aItems.Length; i++)
					{
						if (jShip.aItems[i].strID == condOwner.strID)
						{
							JsonItem jsonItem = this.GetJsonItem(condOwner);
							if (jsonItem != null)
							{
								jShip.aItems[i] = jsonItem;
								flag3 = true;
							}
							else
							{
								Debug.LogError("ERROR: Failed to get JsonItem for " + condOwner.strID + " - " + condOwner.strCODef);
							}
							break;
						}
					}
					if (flag2)
					{
						JsonItem jsonItem2 = this.GetJsonItem(condOwner);
						if (jsonItem2 != null)
						{
							list4.Add(jsonItem2);
						}
						else
						{
							Debug.LogError("ERROR: Failed to get JsonItem for " + condOwner.strID + " - " + condOwner.strCODef);
						}
					}
					else if (condOwner.HasCond("IsPlaceholder"))
					{
						Placeholder component = condOwner.GetComponent<Placeholder>();
						if (component != null)
						{
							JsonItem jsonItem3 = this.GetJsonItem(condOwner);
							if (jsonItem3 != null)
							{
								list5.Add(new JsonPlaceholder
								{
									strName = condOwner.strID,
									strInstalledCO = component.strInstalledCO,
									strActionCO = component.strActionCO,
									strPersistentCO = component.strPersistentCO,
									strPersistentCT = component.strPersistentCT,
									strInstallIA = component.strInstallIA
								});
								list2.Add(jsonItem3);
							}
							else
							{
								Debug.LogError("ERROR: Failed to get JsonItem for " + condOwner.strID + " - " + condOwner.strCODef);
							}
						}
					}
					else
					{
						if (flag3)
						{
							continue;
						}
						if (flag)
						{
							int num = -1;
							for (int j = 0; j < list3.Count; j++)
							{
								JsonItem jsonItem4 = list3[j];
								if (jsonItem4.strID == condOwner.strID)
								{
									num = j;
									break;
								}
							}
							if (num < 0)
							{
								JsonItem jsonItem5 = this.GetJsonItem(condOwner);
								if (jsonItem5 != null)
								{
									list3.Add(jsonItem5);
								}
								else
								{
									Debug.LogError("ERROR: Failed to get JsonItem for " + condOwner.strID + " - " + condOwner.strCODef);
								}
							}
						}
						else
						{
							if (condOwner.HasCond("IsTraderNPC") || condOwner.HasCond("IsMarketActor"))
							{
								this.AddMarketActorConfigToShip(condOwner);
							}
							JsonItem jsonItem6 = this.GetJsonItem(condOwner);
							if (jsonItem6 != null)
							{
								list2.Add(jsonItem6);
							}
							else
							{
								Debug.LogError("ERROR: Failed to get JsonItem for " + condOwner.strID + " - " + condOwner.strCODef);
							}
						}
					}
					if (condOwner.aLot != null)
					{
						foreach (CondOwner co in condOwner.GetLotCOs(true))
						{
							JsonItem jsonItem7 = this.GetJsonItem(co);
							if (jsonItem7 == null)
							{
								Debug.LogError("ERROR: Failed to get JsonItem for " + condOwner.strID + " - " + condOwner.strCODef);
							}
							else
							{
								list2.Add(jsonItem7);
							}
						}
					}
				}
			}
		}
		jShip.aItems = list2.ToArray();
		jShip.aCrew = list3.ToArray();
		jShip.aShallowPSpecs = list4.ToArray();
		jShip.aPlaceholders = list5.ToArray();
	}

	public override string ToString()
	{
		return string.Concat(new object[]
		{
			this.strRegID,
			"; ",
			this.model,
			"; Load:",
			this.nLoadState,
			"; Docked: ",
			this.bDocked
		});
	}

	public bool IsLocalAuthority
	{
		get
		{
			return this.bLocalAuthority;
		}
		set
		{
			this.bLocalAuthority = value;
			if (this.json != null)
			{
				this.json.bLocalAuthority = value;
			}
		}
	}

	public bool IsAIShip
	{
		get
		{
			return this.bAIShip;
		}
		set
		{
			this.bAIShip = value;
			if (this.json != null)
			{
				this.json.bAIShip = value;
			}
		}
	}

	public Ship.Loaded LoadState
	{
		get
		{
			return this.nLoadState;
		}
	}

	public bool IsTemplateShip
	{
		get
		{
			return this.fLastVisit == 0.0;
		}
	}

	public double Gravity
	{
		get
		{
			return this.fGravity;
		}
		set
		{
			bool flag = this.nLoadState >= Ship.Loaded.Edit;
			this.fGravity = value;
			float num = Mathf.Abs((float)(this.fGravity - this.fGravAmountLast));
			if (this.bNeedsGravSet || GUIFFWD.Active)
			{
				num = 0f;
			}
			this.fWearManeuver += num;
			CondTrigger ctDamage = Ship.ctWearManeuver;
			float fDmgModifier = 1f;
			bool flag2 = false;
			if (num / 9.81f > 8f)
			{
				fDmgModifier = MathUtils.Rand(0f, 288000f, MathUtils.RandType.Mid, null);
				flag2 = true;
			}
			if (this.IsDocked() && !this.TowBraceSecured())
			{
				this.fWearManeuver += num * 4f;
				ctDamage = Ship.ctWearManeuverTow;
				fDmgModifier = MathUtils.Rand(0f, 288000f, MathUtils.RandType.Mid, null);
				flag2 = true;
			}
			this.WearManeuver(ctDamage, flag, fDmgModifier);
			if (flag && !this.bNeedsGravSet && !GUIFFWD.Active)
			{
				float num2 = Mathf.Min(1f, num / 4f);
				if (num2 > 0.05f)
				{
					CrewSim.objInstance.CamShake(num2);
				}
				if ((double)num >= 0.3)
				{
					if (flag2)
					{
						AudioManager.am.PlayCreakAudio("TXTRandomCreakMedAudio");
					}
					else
					{
						AudioManager.am.PlayCreakAudio("TXTRandomCreakSmAudio");
					}
				}
			}
			this.fGravAmountLast = this.fGravity;
			foreach (PersonSpec personSpec in this.aPeople)
			{
				if (!flag)
				{
					break;
				}
				CondOwner co = personSpec.GetCO();
				if (!(co == null))
				{
					if (co.GetCondAmount("IsHuman") > 0.0)
					{
						co.SetCondAmount("StatEncumbrance", co.GetCondAmount("StatMass") * this.fGravity, 0.0);
						co.UpdateGravity();
						co.SetCondAmount("StatGrav", this.fGravity, 0.0);
					}
					if (num > 1f)
					{
						this.KnockDownCrew(co);
					}
				}
			}
		}
	}

	public void KnockDownCrew(CondOwner co)
	{
		if (co == null || this.LoadState < Ship.Loaded.Edit || this.bNeedsGravSet || GUIFFWD.Active)
		{
			return;
		}
		double condAmount = co.GetCondAmount("StatDefense");
		double num = 50.0 - condAmount;
		num = MathUtils.Clamp(num, 0.05, 1.0);
		double num2 = MathUtils.Rand(0.0, 1.0, MathUtils.RandType.Flat, null);
		if (num2 <= num)
		{
			Interaction interaction = DataHandler.GetInteraction("ACTShipKnockDown", null, false);
			if (interaction != null)
			{
				Interaction interaction2 = interaction;
				interaction.objThem = co;
				interaction2.objUs = co;
				if (interaction.Triggered(false, true, false))
				{
					co.QueueInteraction(co, interaction, true);
				}
			}
		}
	}

	public void GravApplyCrew()
	{
		if (StarSystem.fEpoch < this.fEpochNextGrav)
		{
			return;
		}
		bool flag = false;
		foreach (PersonSpec personSpec in this.aPeople)
		{
			CondOwner co = personSpec.GetCO();
			if (!(co == null))
			{
				if (co.GetCondAmount("IsHuman") > 0.0 && co.bAlive)
				{
					CondRule condRule = co.GetCondRule("StatGrav");
					if (condRule != null)
					{
						CondRuleThresh currentThresh = condRule.GetCurrentThresh(co);
						if (currentThresh != null)
						{
							int num = Array.IndexOf<CondRuleThresh>(condRule.aThresholds, currentThresh);
							if (num >= 3)
							{
								double num2 = 10.0;
								num2 /= (double)(condRule.aThresholds[3].fMax - condRule.aThresholds[3].fMin);
								if (double.IsNaN(num2))
								{
									num2 = 1.0;
								}
								Wound woundLocation = co.GetWoundLocation(true, false);
								if (!(woundLocation == null))
								{
									double num3 = num2 * (this.fGravity - (double)condRule.aThresholds[3].fMin);
									if (woundLocation.DamageLeft() < num3)
									{
										num3 = woundLocation.DamageLeft();
									}
									bool bAudio = Wound.bAudio;
									Wound.bAudio = (this.LoadState >= Ship.Loaded.Edit);
									woundLocation.Damage(num3, 0.0, 0.0, null, string.Empty, true, null, true);
									Wound.bAudio = bAudio;
									if (this.LoadState >= Ship.Loaded.Edit)
									{
										flag = true;
									}
								}
							}
						}
					}
				}
			}
		}
		this.fEpochNextGrav = StarSystem.fEpoch + MathUtils.Rand(0.9, 1.1, MathUtils.RandType.Flat, null);
		if (flag)
		{
			BeatManager.ResetTensionTimer();
		}
	}

	public double RCSAccelMax
	{
		get
		{
			double num = (double)(100f * (0.728f * this.RCSCount) * 5.26077E-09f);
			double num2 = this.Mass + this.GetAllDockedShipsFull().Sum((Ship dockedShip) => dockedShip.Mass);
			return num / num2;
		}
	}

	public double RCSAccelMaxUndocked
	{
		get
		{
			double num = (double)(100f * (0.728f * this.RCSCount) * 5.26077E-09f);
			return num / this.Mass;
		}
	}

	public double DeltaVRemainingRCS
	{
		get
		{
			return this.RCSAccelMax * this.GetRCSRemain() / 0.7279999852180481 / (double)this.fRCSCount;
		}
	}

	public double DeltaVMaxRCS
	{
		get
		{
			return this.RCSAccelMax * this.GetRCSMax() / 0.7279999852180481 / (double)this.fRCSCount;
		}
	}

	public double DeltaVRemainingFusion(float fLimiter)
	{
		return (double)this.GetMaxTorchThrust(fLimiter) * this.fShallowFusionRemain;
	}

	public bool InAtmo
	{
		get
		{
			BodyOrbit nearestBO = CrewSim.system.GetNearestBO(this.objSS, StarSystem.fEpoch, false);
			if (nearestBO == null)
			{
				return false;
			}
			double distance = (double)this.objSS.GetRadiusAU() + this.objSS.GetDistance(nearestBO.dXReal, nearestBO.dYReal);
			JsonAtmosphere atmosphereAtDistance = nearestBO.GetAtmosphereAtDistance(distance);
			return atmosphereAtDistance.GetTotalKPA() > BodyOrbit.AtmoKPaThreshold;
		}
	}

	public double Mass
	{
		get
		{
			if (this.LoadState <= Ship.Loaded.Shallow)
			{
				this._mass = this.fShallowMass + MarketManager.GetCargoMassForShip(this);
				return this._mass;
			}
			if (this._mass == 0.0)
			{
				this._mass = this.GetCondAmount("StatMass", false) + MarketManager.GetCargoMassForShip(this);
			}
			return this._mass;
		}
	}

	public int NavCount
	{
		get
		{
			return this.aNavs.Count;
		}
	}

	public bool NavAIManned
	{
		get
		{
			List<CondOwner> list = new List<CondOwner>();
			if (this.aPeople == null)
			{
				return false;
			}
			foreach (PersonSpec personSpec in this.aPeople)
			{
				if (personSpec != null)
				{
					if (Ship.ctPilotSafe.Triggered(personSpec.GetCO(), null, true))
					{
						list.Add(personSpec.GetCO());
					}
				}
			}
			if (list.Count == 0)
			{
				return false;
			}
			if (this.LoadState <= Ship.Loaded.Shallow)
			{
				return true;
			}
			if (this.aNavs.Count == 0)
			{
				return false;
			}
			foreach (CondOwner condOwner in list)
			{
				Interaction interactionCurrent = condOwner.GetInteractionCurrent();
				if (interactionCurrent != null)
				{
					if (interactionCurrent.objThem != null && this.aNavs.IndexOf(interactionCurrent.objThem) >= 0)
					{
						return true;
					}
				}
			}
			return false;
		}
	}

	public bool NavPlayerManned
	{
		get
		{
			if (this.LoadState <= Ship.Loaded.Shallow)
			{
				return false;
			}
			if (this.aNavs.Count == 0)
			{
				return false;
			}
			CondOwner selectedCrew = CrewSim.GetSelectedCrew();
			if (selectedCrew == null)
			{
				return false;
			}
			Interaction interactionCurrent = selectedCrew.GetInteractionCurrent();
			return interactionCurrent != null && this.aNavs.IndexOf(interactionCurrent.objThem) >= 0;
		}
	}

	public float RCSCount
	{
		get
		{
			return this.fRCSCount;
		}
	}

	public int DockCount
	{
		get
		{
			return this.nDockCount;
		}
	}

	public bool proximityWarning
	{
		get
		{
			return this._proximityWarning;
		}
		set
		{
			bool proximityWarning = this._proximityWarning;
			this._proximityWarning = value;
			if (this.LoadState >= Ship.Loaded.Edit)
			{
				if (this._proximityWarning && this.aNavs.Count > 0)
				{
					bool flag = false;
					foreach (CondOwner condOwner in this.aNavs)
					{
						condOwner.SetCondAmount("IsProxAlarm", 1.0, 0.0);
						if (!condOwner.HasCond("IsProxMuted"))
						{
							AlarmObjective objective = new AlarmObjective(AlarmType.nav_proximity, condOwner, DataHandler.GetString("OBJV_NAV_PROX_TITLE", false), "TIsNavStationProxClear", this.strRegID, DataHandler.GetString("OBJV_NAV_PROX_DESC", false));
							MonoSingleton<ObjectiveTracker>.Instance.AddObjective(objective);
							flag = true;
						}
					}
					if (flag)
					{
						AudioManager.am.PlayAudioEmitter("ShipProxAlarm", true, false);
						if (!proximityWarning)
						{
							BeatManager.ResetTensionTimer();
							if ((double)Time.timeScale > 1.0)
							{
								CrewSim.ResetTimeScale();
							}
						}
					}
				}
				else
				{
					AudioManager.am.StopAudioEmitter("ShipProxAlarm");
					foreach (CondOwner condOwner2 in this.aNavs)
					{
						condOwner2.ZeroCondAmount("IsProxAlarm");
						MonoSingleton<ObjectiveTracker>.Instance.CheckObjective(condOwner2.strID);
					}
				}
			}
		}
	}

	public float proximityDistanceScaled
	{
		get
		{
			return this._proximityDistanceScaled;
		}
		set
		{
			if (this._proximityDistanceScaled == value)
			{
				return;
			}
			this._proximityDistanceScaled = value;
			if (this._proximityWarning && this.LoadState >= Ship.Loaded.Edit)
			{
				AudioManager.am.TweakAudioEmitter("ShipProxAlarm", 1f - this._proximityDistanceScaled + 0.5f, 1f);
			}
		}
	}

	public bool trackWarning
	{
		get
		{
			return this._trackWarning;
		}
		set
		{
			this._trackWarning = value;
			if (this.LoadState >= Ship.Loaded.Edit)
			{
				if (this._trackWarning && this.aNavs.Count > 0)
				{
					AudioManager.am.PlayAudioEmitter("ShipTrackAlarm", true, false);
					foreach (CondOwner condOwner in this.aNavs)
					{
						condOwner.SetCondAmount("IsTrackAlarm", 1.0, 0.0);
					}
				}
				else
				{
					AudioManager.am.StopAudioEmitter("ShipTrackAlarm");
					foreach (CondOwner condOwner2 in this.aNavs)
					{
						condOwner2.ZeroCondAmount("IsTrackAlarm");
						MonoSingleton<ObjectiveTracker>.Instance.CheckObjective(condOwner2.strID);
					}
				}
			}
		}
	}

	public bool CheckAccruedWear()
	{
		if (this.fWearAccrued <= 0.0)
		{
			return false;
		}
		if (this.nLoadState > Ship.Loaded.Shallow)
		{
			this.DamageAllCOsTrend((float)this.fWearAccrued, Ship.ctWearTime, this.ShipCO.GetCondAmount("StationMaintLvl"));
			this.fWearAccrued = 0.0;
			return true;
		}
		return false;
	}

	public bool HideFromSystem
	{
		get
		{
			return this.bShipHidden;
		}
		set
		{
			this.bShipHidden = value;
			this.bNoCollisions = value;
			this.bChangedStatus = true;
		}
	}

	public double PelletMax
	{
		get
		{
			if (this.Reactor != null)
			{
				this.fFusionPelletMax = this.Reactor.GetCondAmount("StatICPellMax");
			}
			if (this.fFusionPelletMax <= 0.0)
			{
				this.fFusionPelletMax = 1.0;
			}
			return this.fFusionPelletMax;
		}
	}

	public float GetMaxTorchThrust(float fAmount)
	{
		float num = Mathf.Lerp(1f, (float)this.PelletMax, fAmount) / (float)this.PelletMax * fAmount;
		return (float)((double)num * this.fFusionThrustMax / this.Mass * 6.6845869117759804E-12);
	}

	public bool IsUnderConstruction
	{
		get
		{
			return this.bIsUnderConstruction;
		}
		set
		{
			this.bIsUnderConstruction = value;
		}
	}

	public bool IsUsingTorchDrive
	{
		get
		{
			if (this.LoadState == Ship.Loaded.Full)
			{
				return this.bTorchDriveThrusting;
			}
			return this.objSS != null && this.objSS.HasNavData() && this.objSS.NavData.IsTorching;
		}
		set
		{
			if (value != this.bTorchDriveThrusting)
			{
				GUIOrbitDraw.TriggerShipRedraw(this.strRegID);
			}
			this.bTorchDriveThrusting = value;
		}
	}

	public float LiftRotorsThrustStrength
	{
		get
		{
			if (this.fShallowRotorStrength >= 0f)
			{
				return this.fShallowRotorStrength;
			}
			this.fShallowRotorStrength = 0f;
			foreach (CondOwner condOwner in this.aActiveHeavyLiftRotors)
			{
				if (!(condOwner == null))
				{
					this.fShallowRotorStrength += Rotor.ThrustStrength(condOwner);
				}
			}
			return this.fShallowRotorStrength;
		}
		set
		{
			this.fShallowRotorStrength = value;
		}
	}

	public void AccrueWear(float wear)
	{
		this.fWearAccrued += (double)wear;
	}

	public void VisualizeOverlays(bool force = false)
	{
		CondTrigger condTrigger = DataHandler.GetCondTrigger("Blank");
		List<CondOwner> cos = this.GetCOs(condTrigger, true, false, true);
		foreach (CondOwner condOwner in cos)
		{
			if (condOwner.Item != null)
			{
				condOwner.Item.VisualizeOverlays(force);
			}
			else if (condOwner.Crew != null)
			{
				condOwner.Crew.VisualizeDamage(force);
			}
		}
	}

	public bool COIsFactionProperty(CondOwner co)
	{
		return !(co == null) && !(co.socUs != null) && Ship.ctFactionCO.Triggered(co, null, true);
	}

	public void SetFactions(List<JsonFaction> aJFs, bool bRemoveOld)
	{
		foreach (CondOwner condOwner in this.GetCOs(DataHandler.GetCondTrigger("Blank"), true, false, true))
		{
			if (this.COIsFactionProperty(condOwner))
			{
				condOwner.SetFactions(aJFs, bRemoveOld);
			}
		}
		if (bRemoveOld)
		{
			this.aFactions.Clear();
		}
		foreach (JsonFaction item in aJFs)
		{
			if (this.aFactions.IndexOf(item) < 0)
			{
				this.aFactions.Add(item);
			}
		}
	}

	public List<JsonFaction> GetShipFactions()
	{
		return this.aFactions ?? new List<JsonFaction>();
	}

	public bool HasFaction(string strFaction)
	{
		if (string.IsNullOrEmpty(strFaction) || this.aFactions == null)
		{
			return false;
		}
		foreach (JsonFaction jsonFaction in this.aFactions)
		{
			if (jsonFaction.strName == strFaction)
			{
				return true;
			}
		}
		return false;
	}

	public double GetShipValue()
	{
		double num = 0.0;
		double num2 = 1.0;
		if (this.nLoadState <= Ship.Loaded.Shallow)
		{
			if (this.json.aRooms != null)
			{
				foreach (JsonRoom jsonRoom in this.json.aRooms)
				{
					num += jsonRoom.roomValue;
				}
			}
			else
			{
				Debug.LogWarning("No rooms on json " + this.json.designation);
			}
			if (this.json.nO2PumpCount > 0)
			{
				num2 += 2.0;
			}
		}
		else
		{
			foreach (Room room in this.aRooms)
			{
				num += room.RoomValue;
			}
			if (this.aO2AirPumps.Count > 0)
			{
				num2 += 2.0;
			}
		}
		return num * num2;
	}

	private void SetDerelictValue(float randomPriceMod = -1f)
	{
		if (this.DMGStatus != Ship.Damage.Derelict)
		{
			return;
		}
		float num = randomPriceMod;
		if (num < 0f)
		{
			int randomNr = DerelictShipEntry.HashIdIntoNumber(this.strRegID);
			num = DerelictShipEntry.GetRandomPriceModifier(randomNr);
		}
		float num2 = 1.1f - this.fBreakInMultiplier;
		if (num2 <= 0f)
		{
			num2 = 0.1f;
		}
		this.fLastQuotedPrice = this.GetShipValue() * (double)num2 * (double)num;
	}

	public List<RoomSpec> GetRoomSpecs()
	{
		List<RoomSpec> list = new List<RoomSpec>();
		if (this.nLoadState <= Ship.Loaded.Shallow)
		{
			if (this.json.aRooms != null)
			{
				foreach (JsonRoom jsonRoom in this.json.aRooms)
				{
					RoomSpec roomDef = DataHandler.GetRoomDef(jsonRoom.roomSpec);
					if (roomDef != null && !roomDef.IsBlank)
					{
						list.Add(roomDef);
					}
				}
			}
			else
			{
				Debug.LogWarning("No rooms on json " + this.json.designation);
			}
		}
		else
		{
			foreach (Room room in this.aRooms)
			{
				RoomSpec roomSpec = room.GetRoomSpec();
				if (!roomSpec.IsBlank)
				{
					list.Add(roomSpec);
				}
			}
		}
		return list;
	}

	public void CreateRooms(Dictionary<int, JsonRoom> mapTileRooms = null)
	{
		List<Room> range = this.aRooms.GetRange(0, this.aRooms.Count);
		this.aRooms.Clear();
		if (this._cachedRoomDividerDTOs != null)
		{
			this._cachedRoomDividerDTOs.Clear();
		}
		Dictionary<Room, List<Room>> dictionary = new Dictionary<Room, List<Room>>();
		foreach (Tile tile in this.aTiles)
		{
			if (tile.room != null && tile.room.CO == null)
			{
				tile.room = null;
			}
			tile.bPathChecked = false;
		}
		List<Tile> list = new List<Tile>();
		HashSet<Tile> hashSet = new HashSet<Tile>();
		Dictionary<string, int> mapRoomCount = new Dictionary<string, int>();
		int count = this.aTiles.Count;
		for (int i = 0; i < count; i++)
		{
			Tile tile2 = this.aTiles[i];
			CondOwner coProps = tile2.coProps;
			if (!tile2.bPathChecked)
			{
				if (coProps.HasCond("IsPortal"))
				{
					if (!hashSet.Contains(tile2))
					{
						hashSet.Add(tile2);
					}
				}
				else if (coProps.HasCond("IsWall"))
				{
					if (!coProps.HasCond("IsPortal"))
					{
						tile2.room = null;
					}
				}
				else
				{
					Room room = null;
					if (mapTileRooms != null && mapTileRooms.ContainsKey(tile2.Index))
					{
						string strRoomID = mapTileRooms[tile2.Index].strID;
						CondOwner cobyID = this.GetCOByID(strRoomID);
						if (cobyID != null)
						{
							Debug.Log("Generating new room with old CO: " + strRoomID);
							room = new Room(cobyID);
							this.RemoveCO(cobyID, false);
						}
						else
						{
							room = this.aRooms.FirstOrDefault((Room x) => x.CO != null && x.CO.strID == strRoomID);
							if (room == null)
							{
								Debug.Log(string.Concat(new object[]
								{
									"Generating new room with old ID: ",
									strRoomID,
									" for Tile: ",
									tile2.Index
								}));
								room = new Room(DataHandler.GetCondOwner("Compartment", mapTileRooms[tile2.Index].strID, null, false, null, null, null, null));
							}
							else
							{
								Debug.Log(string.Concat(new object[]
								{
									"Tile ",
									tile2.Index,
									" requests room that already exists! Assigning existing one ",
									strRoomID
								}));
							}
						}
						this.LogRoom(mapRoomCount, room);
						room.CO.AddCondAmount("StatVolume", -room.CO.GetCondAmount("StatVolume"), 0.0, 0f);
					}
					if (room == null)
					{
						room = new Room(null);
						Debug.Log("Generating completely new room: " + room.CO.strID);
						this.LogRoom(mapRoomCount, room);
					}
					if (!this.aRooms.Contains(room))
					{
						this.aRooms.Add(room);
					}
					room.CO.tf.SetParent(this.gameObject.transform);
					room.CO.ship = this;
					room.bOuter = false;
					if (!dictionary.ContainsKey(room))
					{
						dictionary[room] = new List<Room>();
					}
					HashSet<Tile> hashSet2 = new HashSet<Tile>
					{
						tile2
					};
					list.Add(tile2);
					for (int j = 0; j < list.Count; j++)
					{
						tile2 = list[j];
						if (tile2.room != null)
						{
							if (!tile2.coProps.HasCond("IsPortal"))
							{
								if (!dictionary[room].Contains(tile2.room))
								{
									dictionary[room].Add(tile2.room);
								}
							}
							else if (!hashSet.Contains(tile2))
							{
								hashSet.Add(tile2);
							}
						}
						tile2.room = room;
						room.aTiles.Add(tile2);
						tile2.bPathChecked = true;
						tile2.strDebug = tile2.Index.ToString();
						coProps = tile2.coProps;
						bool flag = coProps.HasCond("IsPortal");
						room.CO.tf.position = tile2.tf.position;
						if (!room.Void && !coProps.HasCond("IsFloorSealed"))
						{
							room.Void = true;
						}
						Tile[] array;
						if (!flag)
						{
							array = TileUtils.GetSurroundingTiles(tile2, true, false);
						}
						else
						{
							if (!hashSet.Contains(tile2))
							{
								hashSet.Add(tile2);
							}
							array = new Tile[0];
						}
						for (int k = 1; k < array.Length; k++)
						{
							if (k != 2 && k != 5 && k != 7)
							{
								tile2 = array[k];
								if (tile2 != null)
								{
									coProps = tile2.coProps;
									if (!tile2.bPathChecked && !coProps.HasCond("IsWall") && !flag)
									{
										if (!hashSet2.Contains(tile2))
										{
											list.Add(tile2);
											hashSet2.Add(tile2);
										}
									}
								}
								else
								{
									room.Void = true;
									room.bOuter = true;
								}
							}
						}
					}
					list.Clear();
				}
			}
		}
		foreach (Tile tile3 in hashSet)
		{
			List<CondOwner> list2 = new List<CondOwner>();
			this.GetCOsAtWorldCoords1(tile3.tf.position, Ship.ctPortals, false, false, list2);
			if (list2.Count > 0)
			{
				Room roomAtWorldCoords = this.GetRoomAtWorldCoords1(list2[0].GetPos("RoomA", false), false);
				Room roomAtWorldCoords2 = this.GetRoomAtWorldCoords1(list2[0].GetPos("RoomB", false), false);
				if (roomAtWorldCoords != null && !roomAtWorldCoords.Void)
				{
					tile3.room = roomAtWorldCoords;
				}
				else if (roomAtWorldCoords2 != null && !roomAtWorldCoords2.Void)
				{
					tile3.room = roomAtWorldCoords2;
				}
				else if (roomAtWorldCoords != null)
				{
					tile3.room = roomAtWorldCoords;
				}
				else if (roomAtWorldCoords2 != null)
				{
					tile3.room = roomAtWorldCoords2;
				}
				if (tile3.room != null)
				{
					tile3.room.aTiles.Add(tile3);
				}
				if (roomAtWorldCoords != null)
				{
					roomAtWorldCoords.dictDoors[list2[0].strID] = tile3.Index;
				}
				if (roomAtWorldCoords2 != null)
				{
					roomAtWorldCoords2.dictDoors[list2[0].strID] = tile3.Index;
				}
			}
		}
		foreach (Room room2 in this.aRooms)
		{
			room2.ResetBorderCOCache();
			if (!room2.Void)
			{
				room2.CO.AddCondAmount("StatVolume", (double)(0.25599998f * (float)room2.aTiles.Count) - room2.CO.GetCondAmount("StatVolume"), 0.0, 0f);
			}
			List<string> list3 = new List<string>();
			foreach (Condition condition in room2.CO.mapConds.Values)
			{
				if (condition.bRoom)
				{
					list3.Add(condition.strName);
				}
			}
			foreach (string strName in list3)
			{
				room2.CO.ZeroCondAmount(strName);
			}
		}
		List<CondOwner> cos = this.GetCOs(DataHandler.GetCondTrigger("TIsInstalled"), true, false, true);
		foreach (CondOwner condOwner in cos)
		{
			Tile tileAtWorldCoords = this.GetTileAtWorldCoords1(condOwner.tf.position.x, condOwner.tf.position.y, false, true);
			Tile.AddToRoom(tileAtWorldCoords, condOwner, false);
		}
		foreach (Room room3 in this.aRooms)
		{
			room3.CreateRoomSpecs();
		}
		foreach (CondOwner condOwner2 in this.GetCOs(this.CTRoomStats, true, false, true))
		{
			Room roomAtWorldCoords3 = this.GetRoomAtWorldCoords1(condOwner2.tf.position, true);
			if (roomAtWorldCoords3 != null)
			{
				roomAtWorldCoords3.AddToRoom(condOwner2, true);
			}
		}
		foreach (KeyValuePair<Room, List<Room>> keyValuePair in dictionary)
		{
			int count2 = keyValuePair.Value.Count;
			if (!keyValuePair.Key.Void && count2 > 0)
			{
				CondOwner co = keyValuePair.Key.CO;
				GasContainer gasContainer = co.GasContainer;
				double condAmount = co.GetCondAmount("StatVolume");
				double statGasTemp = GasExchange.GetStatGasTemp(co, true);
				double num = 0.0;
				double num2 = 0.0;
				foreach (Room room4 in keyValuePair.Value)
				{
					CondOwner co2 = room4.CO;
					double condAmount2 = co2.GetCondAmount("StatVolume");
					double statGasTemp2 = GasExchange.GetStatGasTemp(co2, false);
					double num3 = Math.Min(condAmount / condAmount2, 1.0);
					GasContainer gasContainer2 = co2.GasContainer;
					foreach (string text in gasContainer2.mapGasMols1.Keys)
					{
						if (!(text == "StatGasMolTotal"))
						{
							double num4 = gasContainer2.mapGasMols1[text] * num3;
							if (num4 > 0.0)
							{
								num += num4;
								float num5 = 1f;
								num2 += num4 * statGasTemp2 * (double)num5;
								MathUtils.DictionaryKeyPlus(ref gasContainer.mapDGasMols, text, num4);
							}
						}
					}
				}
				gasContainer.fDGasTemp = num2 / (num + 1.0000000031710769E-30) - statGasTemp;
				co.GasChanged = true;
				gasContainer.Run(false);
			}
		}
		foreach (Room room5 in range)
		{
			if (room5.CO == null)
			{
				Debug.LogWarning("Warning: Destroying old room that is already null: " + room5);
			}
			else
			{
				room5.CO.tf.SetParent(null);
				room5.CO.ship = null;
			}
			room5.Destroy();
		}
		this.CleanupCompartments();
		this.bCheckRooms = false;
		this.ShowRoomIDs(CrewSim.bDebugShow);
	}

	private void CleanupCompartments()
	{
		if (this.aRooms == null || this.mapICOs == null)
		{
			return;
		}
		List<string> list = null;
		using (Dictionary<string, CondOwner>.Enumerator enumerator = this.mapICOs.GetEnumerator())
		{
			while (enumerator.MoveNext())
			{
				KeyValuePair<string, CondOwner> kvp = enumerator.Current;
				if (!(kvp.Value == null) && kvp.Value.HasCond("IsRoom"))
				{
					if (!this.aRooms.Any((Room r) => r.CO.strID == kvp.Key))
					{
						Debug.LogWarning("Removing ghost room: " + kvp.Key);
						if (list == null)
						{
							list = new List<string>();
						}
						list.Add(kvp.Key);
					}
				}
			}
		}
		if (list == null)
		{
			return;
		}
		foreach (string key in list)
		{
			CondOwner condOwner;
			if (this.mapICOs.TryGetValue(key, out condOwner))
			{
				if (condOwner != null)
				{
					condOwner.Destroy();
				}
				this.mapICOs.Remove(key);
			}
		}
	}

	private void CheckRoomPressure()
	{
		if (this._cachedRoomDividerDTOs != null && this._cachedRoomDividerDTOs.Count > 0)
		{
			foreach (RoomDividerDTO roomDividerDTO in this._cachedRoomDividerDTOs)
			{
				if (roomDividerDTO != null && !roomDividerDTO.HasNullValues())
				{
					GasContainer.CheckPressureDifference(roomDividerDTO.RoomA.CO.GasContainer, roomDividerDTO.RoomB.CO.GasContainer, roomDividerDTO.Position, null);
				}
			}
		}
		else
		{
			this._cachedRoomDividerDTOs = new List<RoomDividerDTO>();
			foreach (Tile tile in this.aTiles)
			{
				if (tile.room == null)
				{
					Vector2 vector = tile.tf.position.ToVector2();
					Room room = null;
					foreach (Vector2 b in this._directionVectors)
					{
						Vector2 vector2 = vector + b;
						Tile tileAtWorldCoords = this.GetTileAtWorldCoords1(vector2.x, vector2.y, false, false);
						if (!(tileAtWorldCoords == null) && tileAtWorldCoords.room != null)
						{
							if (room == null)
							{
								room = tileAtWorldCoords.room;
							}
							else if (room != tileAtWorldCoords.room)
							{
								this._cachedRoomDividerDTOs.Add(new RoomDividerDTO(vector, room, tileAtWorldCoords.room));
								GasContainer.CheckPressureDifference(room.CO.GasContainer, tileAtWorldCoords.room.CO.GasContainer, vector, null);
								break;
							}
						}
					}
				}
			}
		}
	}

	private void LogRoom(Dictionary<string, int> mapRoomCount, Room objRoom)
	{
		if (!mapRoomCount.ContainsKey(objRoom.CO.strID))
		{
			mapRoomCount[objRoom.CO.strID] = 1;
		}
		else
		{
			string strID;
			mapRoomCount[strID = objRoom.CO.strID] = mapRoomCount[strID] + 1;
			Debug.LogError(string.Concat(new object[]
			{
				"ERROR: Ship ",
				this.strRegID,
				" has ",
				mapRoomCount[objRoom.CO.strID],
				" rooms with ID ",
				objRoom.CO.strID
			}));
			Debug.Break();
		}
	}

	public static ManeuverEvent OnManeuver = new ManeuverEvent();

	public static DockEvent OnDock = new DockEvent();

	public const float RCS_EXHAUST_V_AU = 5.26077E-09f;

	public const float RCS_MASS_FLOW = 0.728f;

	public const float RCS_FUDGE = 100f;

	private const float USED_SHIP_DMG = 0.33f;

	public const double DMG_PER_SEC = 1.5844382307706396E-09;

	public string strRegID;

	public string strLaw;

	public string strParallax;

	public JsonShip json;

	public int nCols;

	public int nRows;

	public List<Vector2> FloorPlan;

	public List<Vector2> SilhouettePoints;

	public int nCurrentWaypoint = -1;

	public float fTimeEngaged;

	private double fGravity = 0.3;

	private double _mass;

	public Vector2 vShipPos;

	public GameObject gameObject;

	private GameObject goTiles;

	public Transform tfBGs;

	public bool bDestroyed;

	private bool _proximityWarning;

	private float _proximityDistanceScaled = 1f;

	private bool _trackWarning;

	public List<string> aProxCurrent;

	public List<string> aProxIgnores;

	public List<string> aTrackCurrent;

	public List<string> aTrackIgnores;

	public float fVisibilityRangeMod = 1f;

	private float fAeroCoefficient = 1f;

	private List<Ship> aDocked;

	public List<WaypointShip> aWPs;

	public List<Tile> aTiles;

	public List<Tile> aPwrTiles;

	public List<Room> aRooms;

	protected Dictionary<string, CondOwner> mapICOs;

	public Dictionary<string, string> mapIDRemap;

	private List<PersonSpec> aPeople;

	private List<CondOwner> aLocks;

	private Ship.Loaded nLoadState;

	public Ship.Damage DMGStatus;

	public Dictionary<string, JsonZone> mapZones;

	public Dictionary<string, List<Vector2>> dictBGs;

	private List<CondOwner> aRCSDistros;

	private List<CondOwner> aRCSThrusters;

	public List<CondOwner> aDocksys;

	public List<CondOwner> aO2AirPumps;

	public List<CondOwner> aCores;

	public bool bCheckRooms;

	public bool bCheckPower;

	public bool bCheckLocks;

	public bool bCheckTargets;

	public bool bCheckFusion;

	public bool bChangedStatus;

	public bool bResetLocks;

	public bool bPrefill;

	public bool bBreakInUsed;

	public bool bNoCollisions = true;

	public bool bNeedsGravSet;

	protected float fRCSCount;

	public List<CondOwner> aActiveHeavyLiftRotors;

	private int nRCSDistroCount;

	private int nDockCount;

	public List<CondOwner> aNavs;

	public double fLastVisit;

	public double fFirstVisit;

	public int nInitConstructionProgress;

	private string strTemplateName;

	public int nConstructionProgress = 100;

	private bool bIsUnderConstruction;

	private static readonly RaycastHit[] aHitsGetCOs = new RaycastHit[25];

	private static CondTrigger ctRCSGasCans;

	private static CondTrigger ctRCSGasInput;

	private static CondTrigger ctRCSClusterAudioEmitter;

	private static CondTrigger ctRCSDistroInstalled;

	private static CondTrigger ctDerelictSafe;

	public static CondTrigger ctNavStationOn;

	public static CondTrigger ctXPDR;

	public static CondTrigger ctXPDRAntOn;

	private static CondTrigger ctDocksys;

	private static CondTrigger ctPortals;

	private static CondTrigger ctWearManeuver;

	private static CondTrigger ctWearManeuverTow;

	private static CondTrigger ctWearTime;

	public static CondTrigger ctSparkable;

	private static CondTrigger ctPilotSafe;

	private static CondTrigger ctFactionCO;

	private static CondTrigger ctPermitOKLG;

	private static CondTrigger ctTowBraceSecured;

	private static CondTrigger ctO2Can;

	private static CondTrigger ctAirPump;

	private static CondTrigger ctStabilizerActiveOn;

	private static CondTrigger ctHeavyLiftRotorsInstalled;

	private static CondTrigger ctTutorialDerelict;

	private float fWearManeuver;

	public double fLastWearEpoch;

	public double fWearAccrued;

	private int nActiveStabilizers;

	private float fLastRCSCount;

	private List<JsonFaction> aFactions;

	public float fBreakInMultiplier = 1f;

	public string make = string.Empty;

	public string model = string.Empty;

	public string year = string.Empty;

	public string origin = string.Empty;

	public string description = string.Empty;

	public string designation = string.Empty;

	public string publicName = string.Empty;

	public string dimensions = string.Empty;

	public double fLastQuotedPrice;

	private string[] rating;

	protected double fShallowMass;

	public double fShallowRCSRemass;

	private double fShallowRCSRemassMax;

	public double fShallowFusionRemain;

	public double fFusionThrustMax;

	public double fFusionPelletMax;

	public double fEpochNextGrav;

	public bool bFusionReactorRunning;

	private bool bTorchDriveThrusting;

	private bool _isDespawning;

	private string _strXPDR;

	private List<JsonShipLog> aLog;

	private bool _bDoneLoading;

	public Ship.TypeClassification Classification;

	public bool bXPDRAntenna;

	private bool bShipHidden;

	public bool bTowBraceSecured;

	private static CondTrigger _ctRoomStats;

	private static CondTrigger _ctReactor;

	public Ship shipScanTarget;

	public Ship shipStationKeepingTarget;

	public TargetData TargetData;

	public ShipSitu shipSituTarget;

	public Ship shipUndock;

	public double dLastScanTime;

	public double fAIDockingExpire;

	public double fAIPauseTimer;

	private bool bLocalAuthority;

	private bool bAIShip;

	private List<CondOwner> aSparkables;

	public string strDebugInfo;

	private JsonShipUniques[] JsonShipUniques;

	private double _lastAtmoUpdateTime;

	private bool _subStation;

	private double fGravAmountLast;

	private float fShallowRotorStrength = -1f;

	private Vector2[] _directionVectors = new Vector2[]
	{
		Vector2.down,
		Vector2.up,
		Vector2.left,
		Vector2.right
	};

	private List<RoomDividerDTO> _cachedRoomDividerDTOs;

	public enum Loaded
	{
		None,
		Shallow,
		Edit,
		Full
	}

	public enum Damage
	{
		New,
		Used,
		Damaged,
		Derelict
	}

	public enum Audio
	{
		RCS,
		CollisionBig,
		CollisionMed,
		CollisionSmall
	}

	public enum TypeClassification
	{
		None,
		OrbitalStation,
		OrbitalStationUnfinished,
		GroundStation,
		GroundStationUnfinished,
		Buoy,
		Outpost,
		Waypoint,
		Ship
	}

	public enum EngineMode
	{
		AUTO,
		RCS,
		MIXED,
		ROTOR
	}
}
