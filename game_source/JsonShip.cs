using System;
using System.Collections.Generic;
using System.Linq;
using Ostranauts.Tools.ExtensionMethods;
using UnityEngine;

// Save/template payload for a ship and its attached runtime state.
// Likely loaded from StreamingAssets/data/ships for archetypes, then reused for
// save/load snapshots after items, crew, rooms, and CondOwners are attached.
[Serializable]
public class JsonShip
{
	// `strName` is the ship definition id; `strRegID` looks like the runtime registry id.
	public string strName { get; set; }

	public string strRegID { get; set; }

	public int nCurrentWaypoint { get; set; }

	public float fTimeEngaged { get; set; }

	public float fWearManeuver { get; set; }

	public float fWearAccrued { get; set; }

	public string[] aDocked { get; set; }

	public string[] aProxCurrent { get; set; }

	public string[] aProxIgnores { get; set; }

	public string[] aTrackCurrent { get; set; }

	public string[] aTrackIgnores { get; set; }

	public string[] aFactions { get; set; }

	public float[] aWPTimes { get; set; }

	// Serialized ship-owned CondOwners plus the ship's own CondOwner wrapper.
	public JsonCondOwnerSave[] aCOs { get; set; }

	public JsonCondOwnerSave shipCO { get; set; }

	public JsonItem[] aItems { get; set; }

	public JsonItem[] aCrew { get; set; }

	public JsonItem[] aShallowPSpecs { get; set; }

	public JsonPlaceholder[] aPlaceholders { get; set; }

	public Vector2 vShipPos { get; set; }

	public JsonShipSitu objSS { get; set; }

	public JsonShipSitu[] aWPs { get; set; }

	// Room and zone layouts likely correspond to data/rooms and ship layout data.
	public JsonRoom[] aRooms { get; set; }

	public JsonZone[] aZones { get; set; }

	public float[][] aBGXs { get; set; }

	public float[][] aBGYs { get; set; }

	public string[] aBGNames { get; set; }

	public Ship.Damage DMGStatus { get; set; }

	public double fLastVisit { get; set; }

	public double fFirstVisit { get; set; }

	public double fAIDockingExpire { get; set; }

	public double fAIPauseTimer { get; set; }

	public bool bPrefill { get; set; }

	public bool bBreakInUsed { get; set; }

	public bool bNoCollisions { get; set; }

	public double dLastScanTime { get; set; }

	public string strScanTargetID { get; set; }

	public string strStationKeepingTargetID { get; set; }

	public JsonShipSitu objSituScanTarget { get; set; }

	public string strUndockID { get; set; }

	public bool bLocalAuthority { get; set; }

	public bool bAIShip { get; set; }

	public string strLaw { get; set; }

	public string strParallax { get; set; }

	public string make { get; set; }

	public string model { get; set; }

	public string year { get; set; }

	public string origin { get; set; }

	public string description { get; set; }

	public string designation { get; set; }

	public string publicName { get; set; }

	public string dimensions { get; set; }

	public string[] aRating { get; set; }

	public Dictionary<string, string> aMarketConfigs { get; set; }

	public double fShallowMass { get; set; }

	public double fShallowRCSRemass { get; set; }

	public double fShallowRCSRemassMax { get; set; }

	public double fShallowFusionRemain { get; set; }

	public double fFusionThrustMax { get; set; }

	public double fFusionPelletMax { get; set; }

	public double fLastQuotedPrice { get; set; }

	public double fEpochNextGrav { get; set; }

	public float fBreakInMultiplier { get; set; }

	public float nRCSCount { get; set; }

	public float fShallowRotorStrength { get; set; }

	public int nRCSDistroCount { get; set; }

	public float fAeroCoefficient { get; set; }

	public int nDockCount { get; set; }

	public bool bFusionTorch { get; set; }

	public string strXPDR { get; set; }

	public bool bXPDRAntenna { get; set; }

	public bool bShipHidden { get; set; }

	public bool bIsUnderConstruction { get; set; }

	public int nO2PumpCount { get; set; }

	public JsonCommData commData { get; set; }

	public Ship.TypeClassification ShipType { get; set; }

	public int nConstructionProgress { get; set; }

	public int nInitConstructionProgress { get; set; }

	public JsonShipConstructionTemplate[] aConstructionTemplates { get; set; }

	public string strTemplateName { get; set; }

	public JsonShipLog[] aLog { get; set; }

	public JsonShipUniques[] aUniques { get; set; }

	// Shallow-clones most arrays and deep-clones a few nested records used by ship state.
	// Likely used during spawning, prefab/template duplication, or save staging.
	public JsonShip Clone()
	{
		JsonShip jsonShip = new JsonShip();
		jsonShip.strName = this.strName;
		jsonShip.strRegID = this.strRegID;
		jsonShip.nCurrentWaypoint = this.nCurrentWaypoint;
		jsonShip.fTimeEngaged = this.fTimeEngaged;
		jsonShip.fWearManeuver = this.fWearManeuver;
		jsonShip.fWearAccrued = this.fWearAccrued;
		if (this.aDocked != null)
		{
			jsonShip.aDocked = (string[])this.aDocked.Clone();
		}
		if (this.aProxCurrent != null)
		{
			jsonShip.aProxCurrent = (string[])this.aProxCurrent.Clone();
		}
		if (this.aProxIgnores != null)
		{
			jsonShip.aProxIgnores = (string[])this.aProxIgnores.Clone();
		}
		if (this.aTrackIgnores != null)
		{
			jsonShip.aTrackIgnores = (string[])this.aTrackIgnores.Clone();
		}
		if (this.aTrackCurrent != null)
		{
			jsonShip.aTrackCurrent = (string[])this.aTrackCurrent.Clone();
		}
		if (this.aFactions != null)
		{
			jsonShip.aFactions = (string[])this.aFactions.Clone();
		}
		if (this.aWPTimes != null)
		{
			jsonShip.aWPTimes = (float[])this.aWPTimes.Clone();
		}
		if (this.aCOs != null)
		{
			jsonShip.aCOs = (JsonCondOwnerSave[])this.aCOs.Clone();
		}
		if (this.aItems != null)
		{
			jsonShip.aItems = (JsonItem[])this.aItems.Clone();
		}
		if (this.aCrew != null)
		{
			jsonShip.aCrew = (JsonItem[])this.aCrew.Clone();
		}
		if (this.aShallowPSpecs != null)
		{
			jsonShip.aShallowPSpecs = (JsonItem[])this.aShallowPSpecs.Clone();
		}
		if (this.aPlaceholders != null)
		{
			jsonShip.aPlaceholders = (JsonPlaceholder[])this.aPlaceholders.Clone();
		}
		if (this.aMarketConfigs != null)
		{
			jsonShip.aMarketConfigs = this.aMarketConfigs.CloneShallow<string, string>();
		}
		jsonShip.vShipPos = this.vShipPos;
		jsonShip.objSS = this.objSS;
		jsonShip.shipCO = this.shipCO;
		if (this.aWPs != null)
		{
			jsonShip.aWPs = (JsonShipSitu[])this.aWPs.Clone();
		}
		if (this.aRooms != null)
		{
			jsonShip.aRooms = (JsonRoom[])this.aRooms.Clone();
		}
		if (this.aZones != null)
		{
			jsonShip.aZones = (JsonZone[])this.aZones.Clone();
		}
		if (this.aBGXs != null)
		{
			jsonShip.aBGXs = (float[][])this.aBGXs.Clone();
		}
		if (this.aBGYs != null)
		{
			jsonShip.aBGYs = (float[][])this.aBGYs.Clone();
		}
		if (this.aBGNames != null)
		{
			jsonShip.aBGNames = (string[])this.aBGNames.Clone();
		}
		if (this.commData != null)
		{
			jsonShip.commData = this.commData.Clone();
		}
		if (this.aLog != null)
		{
			jsonShip.aLog = new JsonShipLog[this.aLog.Length];
			for (int i = 0; i < this.aLog.Length; i++)
			{
				jsonShip.aLog[i] = this.aLog[i].Clone();
			}
		}
		jsonShip.ShipType = this.ShipType;
		jsonShip.DMGStatus = this.DMGStatus;
		jsonShip.fLastVisit = this.fLastVisit;
		jsonShip.fAIPauseTimer = this.fAIPauseTimer;
		jsonShip.fAIDockingExpire = this.fAIDockingExpire;
		jsonShip.bPrefill = this.bPrefill;
		jsonShip.bBreakInUsed = this.bBreakInUsed;
		jsonShip.bNoCollisions = this.bNoCollisions;
		jsonShip.dLastScanTime = this.dLastScanTime;
		jsonShip.strScanTargetID = this.strScanTargetID;
		jsonShip.objSituScanTarget = this.objSituScanTarget;
		jsonShip.strUndockID = this.strUndockID;
		jsonShip.bLocalAuthority = this.bLocalAuthority;
		jsonShip.bAIShip = this.bAIShip;
		jsonShip.strLaw = this.strLaw;
		jsonShip.make = this.make;
		jsonShip.model = this.model;
		jsonShip.year = this.year;
		jsonShip.origin = this.origin;
		jsonShip.description = this.description;
		jsonShip.designation = this.designation;
		jsonShip.publicName = this.publicName;
		jsonShip.dimensions = this.dimensions;
		if (this.aRating != null)
		{
			jsonShip.aRating = (string[])this.aRating.Clone();
		}
		jsonShip.fShallowMass = this.fShallowMass;
		jsonShip.fShallowRCSRemass = this.fShallowRCSRemass;
		jsonShip.fShallowRCSRemassMax = this.fShallowRCSRemassMax;
		jsonShip.fShallowFusionRemain = this.fShallowFusionRemain;
		jsonShip.fFusionThrustMax = this.fFusionThrustMax;
		jsonShip.fFusionPelletMax = this.fFusionPelletMax;
		jsonShip.fEpochNextGrav = this.fEpochNextGrav;
		jsonShip.fLastQuotedPrice = this.fLastQuotedPrice;
		jsonShip.fBreakInMultiplier = this.fBreakInMultiplier;
		jsonShip.nRCSCount = this.nRCSCount;
		jsonShip.fShallowRotorStrength = this.fShallowRotorStrength;
		jsonShip.nRCSDistroCount = this.nRCSDistroCount;
		jsonShip.fAeroCoefficient = this.fAeroCoefficient;
		jsonShip.nDockCount = this.nDockCount;
		jsonShip.bFusionTorch = this.bFusionTorch;
		jsonShip.strXPDR = this.strXPDR;
		jsonShip.bXPDRAntenna = this.bXPDRAntenna;
		jsonShip.bShipHidden = this.bShipHidden;
		jsonShip.nO2PumpCount = this.nO2PumpCount;
		if (this.aConstructionTemplates != null)
		{
			jsonShip.aConstructionTemplates = (JsonShipConstructionTemplate[])this.aConstructionTemplates.Clone();
		}
		jsonShip.strTemplateName = this.strTemplateName;
		jsonShip.nConstructionProgress = this.nConstructionProgress;
		jsonShip.nInitConstructionProgress = this.nInitConstructionProgress;
		if (this.aUniques != null)
		{
			jsonShip.aUniques = (from u in this.aUniques
			select u.Clone()).ToArray<JsonShipUniques>();
		}
		return jsonShip;
	}

	// Chooses the best construction-stage snapshot for the current build progress.
	// Possibly used by shipyards or install/build systems when a hull is incomplete.
	public JsonShipConstructionTemplate GetCurrentConstructionTemplate(int currentProgress)
	{
		if (this.aConstructionTemplates == null || this.aConstructionTemplates.Length == 0 || currentProgress == 100)
		{
			return new JsonShipConstructionTemplate(this, 100);
		}
		IOrderedEnumerable<JsonShipConstructionTemplate> orderedEnumerable = from x in this.aConstructionTemplates
		orderby x.nProgress descending
		select x;
		foreach (JsonShipConstructionTemplate jsonShipConstructionTemplate in orderedEnumerable)
		{
			if (currentProgress >= jsonShipConstructionTemplate.nProgress)
			{
				return jsonShipConstructionTemplate.Clone();
			}
		}
		return new JsonShipConstructionTemplate(this, 100);
	}

	public override string ToString()
	{
		return this.strName;
	}
}
