using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Ostranauts;
using Ostranauts.Core;
using Ostranauts.Core.Models;
using Ostranauts.Events;
using Ostranauts.Objectives;
using Ostranauts.Ships.Comms;
using Ostranauts.Trading;
using Ostranauts.Utils.Models;
using UnityEngine;

// Runtime model of the current solar system or map region.
// It owns body orbits, ships, faction/company state, and message traffic,
// and can rebuild that world state from JsonStarSystemSave during loading.
public class StarSystem
{
	// Seeds the runtime registries and recreates the ship-comms event channel.
	public StarSystem()
	{
		StarSystem.fEpoch = 65627498854.02778;
		this.aBOs = new Dictionary<string, BodyOrbit>();
		this.dictShips = new Dictionary<string, Ship>();
		this.dictShipOwners = new Dictionary<string, string>();
		this.dictBOHierarchy = new Dictionary<BodyOrbit, BodyOrbit>();
		this.dictCompanies = new Dictionary<string, JsonCompany>();
		this.dictFactions = new Dictionary<string, JsonFaction>();
		this.aAutoFactions = new List<JsonFaction>();
		this._messagesEnRoute = new Dictionary<string, ShipMessage>();
		this.fGravAccelConstant = 2E-44f;
		if (StarSystem.OnNewShipCommsMessage != null)
		{
			StarSystem.OnNewShipCommsMessage.RemoveAllListeners();
		}
		StarSystem.OnNewShipCommsMessage = new OnNewShipCommsMessageEvent();
	}

	// Coroutine-based load entrypoint.
	// This restores orbital bodies, stations, derelicts, factions, and queued
	// ship messages while yielding to the loading screen between phases.
	public IEnumerator Init(JsonStarSystemSave objSystem, JsonShip[] aShips)
	{
		if (objSystem == null)
		{
			yield return this.Init(true, true);
		}
		StarSystem.fEpoch = objSystem.dfEpoch;
		if (objSystem.aFactions != null)
		{
			foreach (JsonFaction jf in objSystem.aFactions)
			{
				if (jf != null)
				{
					this.AddFaction(jf.Clone());
					yield return null;
				}
			}
		}
		if (objSystem.aShipMessages != null)
		{
			foreach (JsonShipMessage jsonShipMessage in objSystem.aShipMessages)
			{
				ShipMessage shipMessage = new ShipMessage
				{
					SenderRegId = jsonShipMessage.strSenderRegId,
					ReceiverRegId = jsonShipMessage.strRecieverRegId,
					AvailableTime = jsonShipMessage.dAvailableTime,
					Read = jsonShipMessage.bRead,
					Interaction = DataHandler.GetInteraction(jsonShipMessage.iaMessageInteraction.strName, jsonShipMessage.iaMessageInteraction, false)
				};
				this._messagesEnRoute.Add(shipMessage.ID, shipMessage);
			}
		}
		if (objSystem.dictShipOwners != null)
		{
			DataHandler.ConvertStringArrayToDict(objSystem.dictShipOwners, this.dictShipOwners);
		}
		LoadingScreen.SetProgressBar(LoadingScreen.GetProgress(), "Parse System Bodies");
		yield return null;
		Dictionary<string, BodyOrbit> dictBORefs = new Dictionary<string, BodyOrbit>();
		if (objSystem.aBOs != null)
		{
			foreach (JsonBodyOrbitSave jbo in objSystem.aBOs)
			{
				if (jbo.strName != null)
				{
					BodyOrbit bo = new BodyOrbit(jbo);
					this.aBOs.Add(bo.strName, bo);
					dictBORefs[jbo.strName] = bo;
					dictBORefs.TryGetValue("Sol", out this.boStar);
					if (this.boStar != null && this.boStar != bo)
					{
						bo.boParent = this.boStar;
					}
					yield return null;
				}
			}
		}
		LoadingScreen.SetProgressBar(LoadingScreen.GetProgress(), "Parse System Body Hierarchy");
		yield return null;
		if (objSystem.dictBOHierarchy != null)
		{
			foreach (string str in objSystem.dictBOHierarchy)
			{
				if (str != null)
				{
					string[] aSplit = str.Split(new char[]
					{
						'='
					});
					if (aSplit.Length >= 2)
					{
						BodyOrbit bo2 = null;
						if (dictBORefs.TryGetValue(aSplit[1], out bo2))
						{
							BodyOrbit boMoon = null;
							if (dictBORefs.TryGetValue(aSplit[0], out boMoon))
							{
								this.dictBOHierarchy[boMoon] = bo2;
								boMoon.boParent = bo2;
								if (boMoon.nDrawFlagsBody != 1)
								{
									if (bo2.boChildren == null)
									{
										bo2.boChildren = new List<BodyOrbit>();
									}
									bo2.boChildren.Add(boMoon);
								}
								yield return null;
							}
						}
					}
				}
			}
		}
		dictBORefs.Clear();
		dictBORefs = null;
		LoadingScreen.SetProgressBar(LoadingScreen.GetProgress() + 0.01f, "Spawning System Bodies");
		yield return null;
		if (objSystem.aSpawnBodies != null)
		{
			float fractionBody = 0.01f / (float)objSystem.aSpawnBodies.Length;
			foreach (JsonSpawnBodyOrbit jbo2 in objSystem.aSpawnBodies)
			{
				if (jbo2 != null)
				{
					if (jbo2.strNameParent == null || jbo2.strNameParent == string.Empty)
					{
						BodyOrbit bodyOrbit = this.AddStar(jbo2.strName, jbo2.fPeriapsisAU, jbo2.fApoapsisAU, jbo2.fDegreesCW, jbo2.fEccentricity, jbo2.fOrbitalPeriodYears, jbo2.fRadiusKM, jbo2.fMassKG, jbo2.fRotationPeriodDays);
						if (jbo2.strParallax != null)
						{
							bodyOrbit.strParallax = jbo2.strParallax;
							bodyOrbit.fParallaxRadius = Math.Max(1.0, jbo2.fParallaxRadiusKM) / 149597872.0;
							bodyOrbit.fGravParallaxRadius = Math.Max(1.0, jbo2.fGravParallaxRadiusKM) / 149597872.0;
						}
						if (jbo2.strGravParallax != null)
						{
							bodyOrbit.strGravParallax = jbo2.strGravParallax;
						}
						if (jbo2.aAtmosphericValues != null)
						{
							bodyOrbit.aAtmospheres = jbo2.aAtmosphericValues;
						}
						bodyOrbit.fVisibilityRangeMod = (double)((jbo2.fVisibilityRangeMod == 0f) ? 1f : jbo2.fVisibilityRangeMod);
						bodyOrbit.fVisibilityRangeModGrav = (double)((jbo2.fVisibilityRangeModGrav == 0f) ? 1f : jbo2.fVisibilityRangeModGrav);
					}
					else
					{
						BodyOrbit bo3 = this.GetBO(jbo2.strNameParent);
						BodyOrbit bodyOrbit2 = this.AddBody(jbo2.strName, jbo2.fDegreesCW, jbo2.fEccentricity, jbo2.fOrbitalPeriodYears, jbo2.fRadiusKM, jbo2.fMassKG, bo3, jbo2.fRotationPeriodDays);
						if (jbo2.strParallax != null)
						{
							bodyOrbit2.strParallax = jbo2.strParallax;
							bodyOrbit2.fParallaxRadius = Math.Max(1.0, jbo2.fParallaxRadiusKM) / 149597872.0;
							bodyOrbit2.fGravParallaxRadius = Math.Max(1.0, jbo2.fGravParallaxRadiusKM) / 149597872.0;
						}
						if (jbo2.strGravParallax != null)
						{
							bodyOrbit2.strGravParallax = jbo2.strGravParallax;
						}
						if (jbo2.aAtmosphericValues != null)
						{
							bodyOrbit2.aAtmospheres = jbo2.aAtmosphericValues;
						}
						bodyOrbit2.fVisibilityRangeMod = (double)((jbo2.fVisibilityRangeMod == 0f) ? 1f : jbo2.fVisibilityRangeMod);
						bodyOrbit2.fVisibilityRangeModGrav = (double)((jbo2.fVisibilityRangeModGrav == 0f) ? 1f : jbo2.fVisibilityRangeModGrav);
					}
					LoadingScreen.SetProgressBar(LoadingScreen.GetProgress() + fractionBody, "Spawning System Bodies");
					yield return null;
				}
			}
		}
		LoadingScreen.SetProgressBar(LoadingScreen.GetProgress(), "Spawning System Companies");
		yield return null;
		if (objSystem.aComps != null)
		{
			foreach (JsonCompany jc in objSystem.aComps)
			{
				if (jc != null)
				{
					this.dictCompanies[jc.strName] = jc.Clone();
					yield return null;
				}
			}
		}
		LoadingScreen.SetProgressBar(LoadingScreen.GetProgress(), "Spawning System Stations");
		yield return null;
		if (objSystem.aSpawnStations != null)
		{
			float fractionStation = 0.05f / (float)objSystem.aSpawnStations.Length;
			foreach (JsonSpawnStation jstn in objSystem.aSpawnStations)
			{
				this.SpawnStationFromJSON(jstn);
				LoadingScreen.SetProgressBar(LoadingScreen.GetProgress() + fractionStation, "Spawning Station: " + jstn.strName);
				yield return null;
			}
		}
		LoadingScreen.SetProgressBar(LoadingScreen.GetProgress(), "Spawning System Derelicts");
		yield return null;
		if (objSystem.aSpawnDerelictRings != null)
		{
			float fractionDereclicts = 0.01f / (float)objSystem.aSpawnDerelictRings.Length;
			foreach (JsonSpawnDerelict jsd in objSystem.aSpawnDerelictRings)
			{
				if (jsd != null)
				{
					this.AddDerelictRing(jsd);
					LoadingScreen.SetProgressBar(LoadingScreen.GetProgress() + fractionDereclicts, "Spawning System Derelicts");
					yield return null;
				}
			}
		}
		LoadingScreen.SetProgressBar(LoadingScreen.GetProgress() + 0.01f, "Spawning System Ships");
		yield return null;
		if (aShips != null)
		{
			float fractionShip = 0.01f / (float)aShips.Length;
			foreach (JsonShip jss in aShips)
			{
				Ship ship = this._SpawnShip(false, jss, Ship.Loaded.Shallow, this.GetShipOwner(jss.strRegID));
				if (ship != null)
				{
					ship.ToggleVis(false, true);
					ship.bNeedsGravSet = true;
					LoadingScreen.SetProgressBar(LoadingScreen.GetProgress() + fractionShip, "Spawning Ship: " + jss.publicName);
					yield return null;
				}
			}
		}
		CollisionManager.strBOClosestParallax = null;
		yield return true;
		yield break;
	}

	// Queues a ship-comms message for later delivery.
	public bool SendMessage(ShipMessage message)
	{
		if (this._messagesEnRoute.ContainsKey(message.ID))
		{
			return false;
		}
		this._messagesEnRoute.Add(message.ID, message);
		return true;
	}

	// Delivers any queued messages whose availability time has been reached.
	private void DeliverMessages()
	{
		if (this._messagesEnRoute.Count == 0)
		{
			return;
		}
		List<Tuple<string, ShipMessage>> list = new List<Tuple<string, ShipMessage>>();
		foreach (KeyValuePair<string, ShipMessage> keyValuePair in this._messagesEnRoute)
		{
			ShipMessage value = keyValuePair.Value;
			if (value != null && StarSystem.fEpoch > value.AvailableTime)
			{
				list.Add(new Tuple<string, ShipMessage>(keyValuePair.Key, value));
			}
		}
		foreach (Tuple<string, ShipMessage> tuple in list)
		{
			this._messagesEnRoute.Remove(tuple.Item1);
			StarSystem.OnNewShipCommsMessage.Invoke(tuple.Item2);
		}
	}

	// Converts one JsonSpawnStation record into a live station/ship and applies
	// station-specific flags such as fees, law, factions, and starting conditions.
	public void SpawnStationFromJSON(JsonSpawnStation jstn)
	{
		if (jstn == null)
		{
			return;
		}
		switch (jstn.Orbit)
		{
		case JsonSpawnStation.OrbitType.GROUND:
			this.AddGroundStation(new SpawnGroundStationDTO(jstn, this.GetBO(jstn.strNameParent)));
			break;
		case JsonSpawnStation.OrbitType.ORBIT:
			this.AddOrbitStation(new SpawnStationDTO(jstn, this.GetBO(jstn.strNameParent)));
			break;
		case JsonSpawnStation.OrbitType.GEO:
			this.AddGeoStation(new SpawnGeoStationDTO(jstn, this.GetBO(jstn.strNameParent)));
			break;
		case JsonSpawnStation.OrbitType.EX:
			this.AddStationEx(new SpawnStationDTO(jstn, this.GetBO(jstn.strNameParent)));
			break;
		}
		Ship shipByRegID = this.GetShipByRegID(jstn.strName);
		if (shipByRegID != null)
		{
			shipByRegID.IsUnderConstruction = jstn.bIsUnderConstruction;
			shipByRegID.objSS.bIsRegion = jstn.bIsRegion;
			shipByRegID.objSS.bIsNoFees = jstn.bIsNoFees;
			shipByRegID.bNoCollisions = jstn.bNoCollisions;
			shipByRegID.fWearAccrued += (double)jstn.fDamageCap;
			shipByRegID.strLaw = jstn.strLaw;
			shipByRegID.strParallax = jstn.strParallax;
			if (jstn.strPublicName != null && jstn.strPublicName != string.Empty)
			{
				shipByRegID.publicName = jstn.strPublicName;
			}
			if (jstn.aFactions != null && jstn.aFactions.Length > 0)
			{
				shipByRegID.SetFactions(CrewSim.system.GetFactions(jstn.aFactions), false);
			}
			if (jstn.aStartingConds != null)
			{
				foreach (string strDef in jstn.aStartingConds)
				{
					shipByRegID.ShipCO.ParseCondEquation(strDef, 1.0, 0f);
				}
			}
		}
	}

	// Global UTC string derived from the current simulation epoch.
	public static string sUTCEpoch
	{
		get
		{
			return MathUtils.GetUTCFromS(StarSystem.fEpoch);
		}
	}

	// Convenience UTC hour lookup used by scheduling/UI systems.
	public static int nUTCHour
	{
		get
		{
			return MathUtils.GetHourFromS(StarSystem.fEpoch);
		}
	}

	private void CalculatePAFromET(ref double fPerihelion, ref double fAphelion, float fEccentricity, double fOrbitalPeriod, BodyOrbit boParent)
	{
		if (boParent == null || this.boStar == null)
		{
			Debug.LogError("ERROR: Trying to calculate periapsis/apoapsis for body with null parent/star");
			return;
		}
		double num = Math.Pow(boParent.fMass / this.boStar.fMass, 0.3333333333333333);
		double num2 = Math.Pow(Math.Abs(fOrbitalPeriod), 0.6666666666666666) * num;
		double num3 = num2 * Math.Sqrt((double)(1f - fEccentricity * fEccentricity));
		fPerihelion = num2 * (double)(1f - fEccentricity);
		fAphelion = num2 * (double)(1f + fEccentricity);
	}

	public double CalculatePeriodFromPAET(double fPerihelion, double fAphelion, double fMassParent)
	{
		if (fMassParent <= 0.0)
		{
			Debug.LogError("ERROR: Trying to calculate period for body with invalid parent mass");
			return 1.0;
		}
		double num = (fPerihelion + fAphelion) / 2.0;
		if (num <= 0.0)
		{
			Debug.LogError("ERROR: Trying to calculate period for body with invalid semi-major axis");
			return 1.0;
		}
		double num2 = 6.283185307179586 * Math.Sqrt(num * num * num / (double)this.fGravAccelConstant / fMassParent);
		return num2 / 31556926.0;
	}

	private BodyOrbit AddStar(string name, float fPerihelion, float fAphelion, float fW, float fEccentricity, float fOrbitalPeriod, float fRadius, float fMass, float fRotationPeriod = 0f)
	{
		Debug.Log("#Info# Adding star: " + name);
		BodyOrbit bodyOrbit = new BodyOrbit(name, (double)fPerihelion, (double)fAphelion, (double)fW, (double)fEccentricity, (double)fOrbitalPeriod, (double)fRadius, (double)fMass, (double)fRotationPeriod, null);
		bodyOrbit.nDrawFlagsTrack = 1;
		bodyOrbit.nDrawFlagsBody = 2;
		if (this.boStar == null)
		{
			this.boStar = bodyOrbit;
		}
		return this.AddBO(bodyOrbit, null);
	}

	private BodyOrbit AddBody(string name, float fW, float fEccentricity, float fOrbitalPeriod, float fRadius, float fMass, BodyOrbit boParent, float fRotationPeriod = 0f)
	{
		Debug.Log("#Info# Adding body: " + name);
		double fPerihelionAU = 0.0;
		double fAphelionAU = 0.0;
		this.CalculatePAFromET(ref fPerihelionAU, ref fAphelionAU, fEccentricity, (double)fOrbitalPeriod, boParent);
		BodyOrbit bodyOrbit = new BodyOrbit(name, fPerihelionAU, fAphelionAU, (double)fW, (double)fEccentricity, (double)fOrbitalPeriod, (double)fRadius, (double)fMass, (double)fRotationPeriod, boParent);
		this.AddBO(bodyOrbit, boParent);
		return bodyOrbit;
	}

	public static float RandomAngle()
	{
		return UnityEngine.Random.Range(0f, 360f);
	}

	private void AddDebugFactions()
	{
		JsonStarSystemSave jsonStarSystemSave;
		if (!DataHandler.dictStarSystems.TryGetValue("NewGame", out jsonStarSystemSave) || jsonStarSystemSave == null || jsonStarSystemSave.aFactions == null)
		{
			return;
		}
		foreach (JsonFaction jsonFaction in jsonStarSystemSave.aFactions)
		{
			this.AddFaction(jsonFaction.Clone());
		}
	}

	public IEnumerator Init(bool bLoadSystem, bool bLoadShips)
	{
		this.AddDebugFactions();
		Debug.Log("Initializing Star System");
		yield return null;
		BodyOrbit boSol = this.AddStar("Sol", 0.0001f, 0.0001f, 0f, 0f, 1f, 695510f, 1.9891E+30f, 24.47f);
		if (!bLoadSystem)
		{
			yield break;
		}
		Debug.Log("Loading Orbital Bodies!");
		LoadingScreen.SetProgressBar(LoadingScreen.GetProgress() + 0.005f, "Creating Oribtal Bodies");
		yield return null;
		BodyOrbit mercury = this.AddBody("Mercury", 48.3f, 0.206f, 0.241f, 2440f, 3.3E+23f, boSol, 59f);
		BodyOrbit venus = this.AddBody("Venus", 76.7f, 0.0067f, 0.615f, 6052f, 4.7E+24f, boSol, 243f);
		BodyOrbit earth = this.AddBody("Earth", -11.2f, 0.0167f, 1f, 6371f, 5.9724E+24f, boSol, 0.99f);
		BodyOrbit mars = this.AddBody("Mars", 49.6f, 0.094f, 1.88f, 3390f, 6.4E+23f, boSol, 1.02f);
		BodyOrbit ganymed = this.AddBody("1036 Ganymed", 215.556f, 0.5335f, 4.35f, 34.28f, 3.3E+16f, boSol, 0.416f);
		BodyOrbit ceres = this.AddBody("Ceres", 80.32f, 0.08f, 4.6f, 476f, 9.4E+20f, boSol, 0.375f);
		BodyOrbit vesta = this.AddBody("Vesta", 103.8f, 0.08874f, 3.63f, 262f, 2.59E+20f, boSol, 0.2226f);
		BodyOrbit pallas = this.AddBody("Pallas", 173.08f, 0.2305f, 4.62f, 256f, 2.1E+20f, boSol, 0.325f);
		BodyOrbit hygeia = this.AddBody("Hygeia", 283.2f, 0.1125f, 5.27f, 222f, 8.55E+19f, boSol, 1.15f);
		BodyOrbit jupiter = this.AddBody("Jupiter", 100.464f, 0.0489f, 11.862f, 69912f, 1.898E+27f, boSol, 0.414f);
		BodyOrbit saturn = this.AddBody("Saturn", 113.6f, 0.056f, 29.46f, 58233f, 5.7E+26f, boSol, 0f);
		BodyOrbit uranus = this.AddBody("Uranus", 74f, 0.047f, 84.02f, 25363f, 8.7E+25f, boSol, 0f);
		BodyOrbit neptune = this.AddBody("Neptune", 131.8f, 0.0113f, 164.8f, 24622f, 1.02413E+26f, boSol, 0f);
		bool addTrojans = true;
		if (addTrojans)
		{
			BodyOrbit bodyOrbit = this.AddBody("588 Achilles", 315.54f, 0.1463f, 11.862f, 65f, 2.6E+18f, boSol, 0.304f);
			BodyOrbit bodyOrbit2 = this.AddBody("617 Patroclus", 44.354f, 0.1382f, 11.862f, 70f, 1.36E+18f, boSol, 4.29f);
		}
		bool addTNO = true;
		if (addTNO)
		{
			this.AddBody("Pluto", 11.3f, 0.245f, 247.7f, 1184f, 1.3f * Mathf.Pow(10f, 22f), boSol, 0f);
			this.AddBody("Makemake", 79f, 0.156f, 309.1f, 716f, 8.3f * Mathf.Pow(10f, 21f), boSol, 0f);
			this.AddBody("Haumea", 122f, 0.191f, 284.1f, 620f, 4f * Mathf.Pow(10f, 21f), boSol, 0f);
			this.AddBody("Eris", 35.9f, 0.441f, 558f, 1164f, 1.7f * Mathf.Pow(10f, 22f), boSol, 0f);
			this.AddBody("Sedna", 144.5f, 0.855f, 11400f, 996f, 9.5f * Mathf.Pow(10f, 21f), boSol, 0f);
			this.AddBody("2007OR10", 337f, 0.506f, 546.6f, 1280f, 1.6f * Mathf.Pow(10f, 22f), boSol, 0f);
			this.AddBody("Quaoar", 189f, 0.039f, 286f, 1111f, 1.4f * Mathf.Pow(10f, 21f), boSol, 0f);
			this.AddBody("Orcus", 267f, 0.227f, 245.2f, 917f, 6.4f * Mathf.Pow(10f, 20f), boSol, 0f);
			this.AddBody("Ultima Thule", 158f, 0.041f, 298f, 16f, 1E+16f, boSol, 0f);
			BodyOrbit bodyOrbit3 = this.AddBody("Bowie", 131.8f, 0.467f, 15392f, 3637f, 2.1342E+23f, boSol, 0f);
		}
		BodyOrbit obj79 = this.AddBody("79au", 196.967f, 0f, 702.047f, 166f, 3.7056E+11f, boSol, 0f);
		BodyOrbit luna = this.AddBody("Luna", StarSystem.RandomAngle(), 0.0549f, 0.074794516f, 1737f, 7.342E+22f, earth, 27.32f);
		BodyOrbit phobos = this.AddBody("Phobos", StarSystem.RandomAngle(), 0.0151f, 0.0008739726f, 11.267f, 1.0659E+16f, mars, 0.3189f);
		BodyOrbit deimos = this.AddBody("Deimos", StarSystem.RandomAngle(), 0.0003f, 0.003460274f, 6.2f, 1.4762E+15f, mars, 1.263f);
		BodyOrbit ganymede = this.AddBody("Ganymede", StarSystem.RandomAngle(), 0.0013f, 0.0196f, 2634.1f, 1.4819E+23f, jupiter, 7.155f);
		BodyOrbit io = this.AddBody("Io", StarSystem.RandomAngle(), 0.0041f, 0.0048465757f, 1821.6f, 8.932E+22f, jupiter, 1.769f);
		BodyOrbit europa = this.AddBody("Europa", StarSystem.RandomAngle(), 0.009f, 0.009728768f, 1560.8f, 4.799E+22f, jupiter, 3.551f);
		this.AddBody("Amalthea", StarSystem.RandomAngle(), 0.003f, 0.0013643835f, 83.5f, 2.08E+18f, jupiter, 0.498f);
		this.AddBody("Himalia", StarSystem.RandomAngle(), 0.16f, 0.68646574f, 75f, 6.7E+18f, jupiter, 0.32425f);
		this.AddBody("Thebe", StarSystem.RandomAngle(), 0.0175f, 0.0018465754f, 49.3f, 4.3E+17f, jupiter, 0.675f);
		this.AddBody("Elara", StarSystem.RandomAngle(), 0.22f, 0.7113425f, 43f, 8.7E+17f, jupiter, 0.5f);
		this.AddBody("Pasiphae", StarSystem.RandomAngle(), 0.2953f, -2.09337f, 20f, 3E+17f, jupiter, 764.08f);
		this.AddBody("Metis", StarSystem.RandomAngle(), 0.0002f, 0.00079452054f, 21.5f, 3.6E+16f, jupiter, 0.295f);
		this.AddBody("Carme", StarSystem.RandomAngle(), 0.25f, -1.9240549f, 23f, 1.3E+17f, jupiter, 702.28f);
		this.AddBody("Sinope", StarSystem.RandomAngle(), 0.25f, -1.9838356f, 19f, 7.5E+16f, jupiter, 724.1f);
		this.AddBody("Lysithea", StarSystem.RandomAngle(), 0.11f, 0.710137f, 18f, 6.3E+16f, jupiter, 259.2f);
		this.AddBody("Ananke", StarSystem.RandomAngle(), 0.24f, -1.6724658f, 14f, 3E+16f, jupiter, 610.45f);
		this.AddBody("Callisto", StarSystem.RandomAngle(), 0.0074f, 0.045723286f, 2410.3f, 1.0759E+23f, jupiter, 16.689f);
		BodyOrbit titan = this.AddBody("Titan", StarSystem.RandomAngle(), 0.0288f, 0.04368493f, 2575f, 1.345E+23f, saturn, 15.945f);
		this.AddBody("Rhea", StarSystem.RandomAngle(), 0.00126f, 0.012378083f, 763.8f, 2.307E+21f, saturn, 4.518f);
		this.AddBody("Iapetus", StarSystem.RandomAngle(), 0.02768f, 0.21732055f, 734.5f, 1.806E+21f, saturn, 79.322f);
		this.AddBody("Dione", StarSystem.RandomAngle(), 0.0022f, 0.0074986303f, 561.4f, 1.095E+21f, saturn, 2.737f);
		this.AddBody("Tethys", StarSystem.RandomAngle(), 0.0001f, 0.005172603f, 531.1f, 6.174E+20f, saturn, 1.888f);
		this.AddBody("Enceladus", StarSystem.RandomAngle(), 0.0047f, 0.0037534246f, 252.1f, 1.08E+20f, saturn, 1.37f);
		this.AddBody("Mimas", StarSystem.RandomAngle(), 0.0196f, 0.0025808217f, 198.2f, 3.749E+19f, saturn, 0.942f);
		BodyOrbit triton = this.AddBody("Triton", StarSystem.RandomAngle(), 1.6E-05f, -0.015890412f, 14f, 3E+16f, neptune, 0f);
		Debug.Log("Loading Stations!");
		LoadingScreen.SetProgressBar(LoadingScreen.GetProgress() + 0.005f, "Creating Stations");
		yield return null;
		if (bLoadShips)
		{
			LoadingScreen.SetProgressBar(LoadingScreen.GetProgress(), "Creating Stations: Mercury");
			yield return null;
			this.AddGeoStation(new SpawnGeoStationDTO("HQCH", 0f, 0f, 1f, 100000f, "Station", mercury));
			LoadingScreen.SetProgressBar(LoadingScreen.GetProgress(), "Creating Stations: Venus");
			yield return null;
			this.AddGroundStation(new SpawnGroundStationDTO("VNCA", 0f, 1f, 100000f, "Station", venus));
			this.AddGroundStation(new SpawnGroundStationDTO("VCBR", 120f, 1f, 100000f, "Station", venus));
			this.AddGroundStation(new SpawnGroundStationDTO("VENC", 240f, 1f, 100000f, "Station", venus));
			LoadingScreen.SetProgressBar(LoadingScreen.GetProgress(), "Creating Stations: Luna");
			yield return null;
			this.AddGroundStation(new SpawnGroundStationDTO("EJDR", 0f, 1f, 1f * Mathf.Pow(10f, 5f), "Station", luna));
			LoadingScreen.SetProgressBar(LoadingScreen.GetProgress(), "Creating Stations: Mars");
			yield return null;
			this.AddGroundStation(new SpawnGroundStationDTO("MTRS", 0f, 1f, 1f * Mathf.Pow(10f, 5f), "Station", mars));
			this.GetShipByRegID("MTRS").objSS.bIsRegion = true;
			this.AddGroundStation(new SpawnGroundStationDTO("MLAB", 12f, 1f, 1f * Mathf.Pow(10f, 5f), "Station", mars));
			this.AddOrbitStation(new SpawnStationDTO("MHNG", 0f, 0f, 109.6f, 0.094f, 1.88f, 1f, 1f * Mathf.Pow(10f, 5f), "Station", this.boStar, 100, Ship.TypeClassification.OrbitalStation));
			LoadingScreen.SetProgressBar(LoadingScreen.GetProgress(), "Creating Stations: Deimos");
			yield return null;
			this.AddGroundStation(new SpawnGroundStationDTO("MVOL", 0f, 1f, 1f * Mathf.Pow(10f, 5f), "Station", deimos));
			LoadingScreen.SetProgressBar(LoadingScreen.GetProgress(), "Creating Stations: Ceres");
			yield return null;
			this.AddGroundStation(new SpawnGroundStationDTO("BCER", 0f, 1f, 1f * Mathf.Pow(10f, 5f), "Station", ceres));
			this.GetShipByRegID("BCER").objSS.bIsRegion = true;
			this.AddGeoStation(new SpawnGeoStationDTO("BCRS", 0f, 0f, 1f, 1f * Mathf.Pow(10f, 5f), "Station", ceres));
			LoadingScreen.SetProgressBar(LoadingScreen.GetProgress(), "Creating Stations: 1036 Ganymed");
			yield return null;
			this.AddGroundStation(new SpawnGroundStationDTO("OKLG", 0f, 1f, 1f * Mathf.Pow(10f, 5f), "OKLG Entrance", ganymed));
			this.GetShipByRegID("OKLG").objSS.bIsRegion = true;
			yield return null;
			this.AddGroundStation(new SpawnGroundStationDTO("OKLG_BIZ", 355f, 1f, 1f * Mathf.Pow(10f, 5f), "OKLG Bureaus", ganymed));
			yield return null;
			this.AddGroundStation(new SpawnGroundStationDTO("OKLG_MES", 5f, 1f, 1f * Mathf.Pow(10f, 5f), "OKLG Mescaform", ganymed));
			this.GetShipByRegID("OKLG_BIZ").bNoCollisions = true;
			this.GetShipByRegID("OKLG_MES").bNoCollisions = true;
			float fRadius = 1.0695339E-05f;
			yield return null;
			this.AddStationEx(new SpawnStationDTO("OKLG_FLOT", fRadius, fRadius, 312f, 0f, 500f, 1f, 1f * Mathf.Pow(10f, 5f), "Flotilla", ganymed, 100, Ship.TypeClassification.OrbitalStation));
			this.GetShipByRegID("OKLG_FLOT").objSS.bIsNoFees = true;
			fRadius = 2.6738348E-05f;
			yield return null;
			this.AddStationEx(new SpawnStationDTO("OKLG_ATC", fRadius, fRadius, 240f, 0f, 0.416f, 1f, 1f * Mathf.Pow(10f, 5f), "ATC 01", ganymed, 100, Ship.TypeClassification.Outpost));
			yield return null;
			this.AddStationEx(new SpawnStationDTO("OKLG_SEC", fRadius, fRadius, 120f, 0f, 0.416f, 1f, 1f * Mathf.Pow(10f, 5f), "Security Station", ganymed, 100, Ship.TypeClassification.Outpost));
			yield return null;
			this.AddStationEx(new SpawnStationDTO("OKLG_NAV0", fRadius, fRadius, 0f, 0f, 0.416f, 1f, 1f * Mathf.Pow(10f, 5f), "Mooring Buoy", ganymed, 100, Ship.TypeClassification.Buoy));
			yield return null;
			this.AddStationEx(new SpawnStationDTO("OKLG_NAV1", fRadius, fRadius, 60f, 0f, 0.416f, 1f, 1f * Mathf.Pow(10f, 5f), "Nav Buoy", ganymed, 100, Ship.TypeClassification.Buoy));
			yield return null;
			this.AddStationEx(new SpawnStationDTO("OKLG_NAV2", fRadius, fRadius, 180f, 0f, 0.416f, 1f, 1f * Mathf.Pow(10f, 5f), "Nav Buoy", ganymed, 100, Ship.TypeClassification.Buoy));
			yield return null;
			this.AddStationEx(new SpawnStationDTO("OKLG_NAV3", fRadius, fRadius, 300f, 0f, 0.416f, 1f, 1f * Mathf.Pow(10f, 5f), "Nav Buoy", ganymed, 100, Ship.TypeClassification.Buoy));
			yield return null;
			this.GetShipByRegID("OKLG_NAV1").publicName = "OKLG NAV1";
			this.GetShipByRegID("OKLG_NAV1").objSS.bIsNoFees = true;
			this.GetShipByRegID("OKLG_NAV2").publicName = "OKLG NAV2";
			this.GetShipByRegID("OKLG_NAV2").objSS.bIsNoFees = true;
			this.GetShipByRegID("OKLG_NAV3").publicName = "OKLG NAV3";
			this.GetShipByRegID("OKLG_NAV3").objSS.bIsNoFees = true;
			LoadingScreen.SetProgressBar(LoadingScreen.GetProgress(), "Creating Stations: Ganymede");
			yield return null;
			this.AddGroundStation(new SpawnGroundStationDTO("JFTS", 0f, 1f, 1f * Mathf.Pow(10f, 5f), "Station", ganymede));
			this.GetShipByRegID("JFTS").objSS.bIsRegion = true;
			LoadingScreen.SetProgressBar(LoadingScreen.GetProgress(), "Creating Stations: Europa");
			yield return null;
			this.AddGroundStation(new SpawnGroundStationDTO("JATL", 0f, 1f, 1f * Mathf.Pow(10f, 5f), "Station", europa));
			this.GetShipByRegID("JATL").objSS.bIsRegion = true;
			this.AddGeoStation(new SpawnGeoStationDTO("JPTN", 0f, 0f, 1f, 1f * Mathf.Pow(10f, 5f), "Station", europa));
			this.GetShipByRegID("JPTN").objSS.bIsRegion = true;
			LoadingScreen.SetProgressBar(LoadingScreen.GetProgress(), "Creating Stations: Titan");
			yield return null;
			this.AddGroundStation(new SpawnGroundStationDTO("SVIR", 0f, 1f, 1f * Mathf.Pow(10f, 5f), "Station", titan));
			this.GetShipByRegID("SVIR").objSS.bIsRegion = true;
		}
		Debug.Log("Loading Ships!");
		LoadingScreen.SetProgressBar(LoadingScreen.GetProgress() + 0.005f, "Init System: Creating Ships");
		yield return null;
		if (bLoadShips)
		{
			int nNum = MathUtils.Rand(2, 20, MathUtils.RandType.Mid, null);
			float fMaxVel = 3.3422936E-09f;
			for (int i = 0; i < nNum; i++)
			{
				Ship objShip = this.AddDerelict("RandomShip", "UNREGISTERED");
				if (objShip != null)
				{
					this.SetSituToRandomSafeCoords(objShip.objSS, 0.3100000023841858, 50.0, 0.0, 0.0, MathUtils.RandType.Low);
					objShip.objSS.fRot = MathUtils.Rand(0f, 6.2831855f, MathUtils.RandType.Flat, null);
					objShip.objSS.fW = MathUtils.Rand(0f, 1f, MathUtils.RandType.Low, null) - 0.5f;
					objShip.objSS.vVelX = (double)(MathUtils.Rand(0f, 2f * fMaxVel, MathUtils.RandType.Low, null) - fMaxVel);
					objShip.objSS.vVelY = (double)(MathUtils.Rand(0f, 2f * fMaxVel, MathUtils.RandType.Low, null) - fMaxVel);
					yield return null;
				}
			}
			nNum = MathUtils.Rand(40, 60, MathUtils.RandType.Mid, null);
			BodyOrbit bo = this.GetBO("1036 Ganymed");
			for (int j = 0; j < nNum; j++)
			{
				Ship objShip2 = this.AddDerelict("RandomDerelict", "UNREGISTERED");
				if (objShip2 != null)
				{
					bo.UpdateTime(StarSystem.fEpoch, true, true);
					double fRadiusMax = 1600.0;
					double fRadius2 = MathUtils.Rand(0.0, 1.0, MathUtils.RandType.Low, null);
					fRadius2 = 90.0 + fRadius2 * fRadius2 * fRadiusMax;
					if ((double)j < 0.05 * (double)nNum)
					{
						fRadius2 = MathUtils.Rand(1300.0, fRadiusMax, MathUtils.RandType.Flat, null);
					}
					this.SetSituToRandomSafeCoords(objShip2.objSS, fRadius2 / 149597872.0, fRadius2 / 149597872.0, bo.dXReal, bo.dYReal, MathUtils.RandType.Low);
					objShip2.objSS.fRot = MathUtils.Rand(0f, 6.2831855f, MathUtils.RandType.Flat, null);
					objShip2.objSS.LockToBO(bo, -1.0);
					objShip2.fBreakInMultiplier = (float)(1.0 - MathUtils.Rand((fRadius2 - 90.0) / fRadiusMax, 1.0, MathUtils.RandType.Low, null));
					yield return null;
				}
			}
		}
		yield return true;
		yield break;
	}

	public void PostShipLoad()
	{
		foreach (Ship ship in this.dictShips.Values)
		{
			if (ship.json != null)
			{
				if (ship.json.strScanTargetID != null)
				{
					ship.shipScanTarget = CrewSim.system.GetShipByRegID(ship.json.strScanTargetID);
				}
			}
		}
	}

	private void SetSystemToEpoch(double fEpochWhen)
	{
		if (fEpochWhen == this._systemEpoch)
		{
			return;
		}
		this._systemEpoch = fEpochWhen;
		foreach (KeyValuePair<string, BodyOrbit> keyValuePair in this.aBOs)
		{
			keyValuePair.Value.UpdateTime(fEpochWhen, true, true);
		}
	}

	public void Update(double fTimeDelta)
	{
		int yearFromS = MathUtils.GetYearFromS(StarSystem.fEpoch);
		int monthFromS = MathUtils.GetMonthFromS(StarSystem.fEpoch);
		int dayOfYearFromS = MathUtils.GetDayOfYearFromS(StarSystem.fEpoch);
		int shiftFromS = MathUtils.GetShiftFromS(StarSystem.fEpoch);
		int nUTCHour = StarSystem.nUTCHour;
		StarSystem.fEpoch += fTimeDelta;
		int shiftFromS2 = MathUtils.GetShiftFromS(StarSystem.fEpoch);
		int dayOfYearFromS2 = MathUtils.GetDayOfYearFromS(StarSystem.fEpoch);
		if (!this.bWarnedShift && shiftFromS != MathUtils.GetShiftFromS(StarSystem.fEpoch + 300.0))
		{
			this.LedgerAlert();
		}
		if (shiftFromS2 != shiftFromS)
		{
			Ledger.Skip(StarSystem.fEpoch - fTimeDelta);
		}
		if (MathUtils.GetYearFromS(StarSystem.fEpoch) != yearFromS)
		{
			this.bWarnedShift = false;
			Ledger.ProcessRepeating(fTimeDelta, LedgerLI.Frequency.Yearly);
		}
		else if (MathUtils.GetMonthFromS(StarSystem.fEpoch) != monthFromS)
		{
			this.bWarnedShift = false;
			Ledger.ProcessRepeating(fTimeDelta, LedgerLI.Frequency.Monthly);
		}
		else if (MathUtils.GetDayOfYearFromS(StarSystem.fEpoch) != dayOfYearFromS)
		{
			this.bWarnedShift = false;
			Ledger.ProcessRepeating(fTimeDelta, LedgerLI.Frequency.Daily);
		}
		else if (MathUtils.GetShiftFromS(StarSystem.fEpoch) != shiftFromS)
		{
			this.bWarnedShift = false;
			Ledger.ProcessRepeating(fTimeDelta, LedgerLI.Frequency.Shiftly);
		}
		else if (MathUtils.GetHourFromS(StarSystem.fEpoch) != nUTCHour)
		{
			this.bWarnedShift = false;
			Ledger.ProcessRepeating(fTimeDelta, LedgerLI.Frequency.Hourly);
			if (StarSystem.nUTCHour == 24)
			{
				BeatManager.RunEncounter("ENCFirstUntime", false);
			}
		}
		foreach (BodyOrbit bodyOrbit in this.aBOs.Values)
		{
			bodyOrbit.UpdateTime(StarSystem.fEpoch, true, true);
		}
		this._shipsDockedToHeavies.Clear();
		foreach (Ship ship in this.dictShips.Values)
		{
			if (ship.bDestroyed)
			{
				this.temp_aDestroyed.Add(ship);
			}
			else
			{
				ship.fTimeEngaged += (float)fTimeDelta;
				this.UpdateShip(fTimeDelta, ship);
				if (ship.objSS.ssDockedHeavier != null)
				{
					this._shipsDockedToHeavies.Add(ship);
				}
			}
		}
		if (fTimeDelta > 0.0)
		{
			foreach (Ship ship2 in this._shipsDockedToHeavies)
			{
				if (ship2 != null && ship2.objSS != null && ship2.objSS.ssDockedHeavier != null)
				{
					ship2.objSS.PlaceOrbitPosition(ship2.objSS.ssDockedHeavier);
				}
			}
		}
		foreach (Ship ship3 in this.temp_aDestroyed)
		{
			Debug.LogWarning("WARNING: StarSystem performing cleanup on " + ship3.strRegID + "; Should be done in Ship.Destroy()");
			AIShipManager.UnregisterShip(ship3);
			MarketManager.UnregisterShip(ship3.strRegID);
			this.dictShips.Remove(ship3.strRegID);
			this.dictShipOwners.Remove(ship3.strRegID);
		}
		this.temp_aDestroyed.Clear();
		if (fTimeDelta > 0.0 && CrewSim.coPlayer != null)
		{
			AIShipManager.FFWD(fTimeDelta);
			CollisionManager.CheckCollisions(CrewSim.GetSelectedCrew().ship);
			foreach (Ship shipCheck in CrewSim.coPlayer.ship.GetAllDockedShips())
			{
				CollisionManager.CheckCollisions(shipCheck);
			}
			CollisionManager.RunQueue();
			this.DeliverMessages();
			AIShipManager.Update();
			MarketManager.Run();
		}
	}

	public void RemoveShip(Ship ship)
	{
		if (ship == null)
		{
			return;
		}
		this.RemoveShip(ship.strRegID);
	}

	public void RemoveShip(string shipRegID)
	{
		if (string.IsNullOrEmpty(shipRegID))
		{
			return;
		}
		if (this.dictShips.ContainsKey(shipRegID))
		{
			CollisionManager.UnregisterShip(this.dictShips[shipRegID]);
			this.dictShips.Remove(shipRegID);
		}
		if (this.dictShipOwners.ContainsKey(shipRegID))
		{
			this.dictShipOwners.Remove(shipRegID);
		}
	}

	public void Destroy()
	{
		CollisionManager.ClearQueue();
		this.aBOs.Clear();
		this.aBOs = null;
		this.dictBOHierarchy.Clear();
		this.dictBOHierarchy = null;
		this._messagesEnRoute = null;
		StarSystem.OnNewShipCommsMessage.RemoveAllListeners();
		Ship[] array = new Ship[this.dictShips.Values.Count];
		this.dictShips.Values.CopyTo(array, 0);
		foreach (Ship ship in array)
		{
			ship.Destroy(true);
		}
		this.dictShips.Clear();
		this.dictShips = null;
		foreach (JsonCompany jsonCompany in this.dictCompanies.Values)
		{
			jsonCompany.Destroy();
		}
		this.dictCompanies.Clear();
		this.dictCompanies = null;
		foreach (JsonFaction jsonFaction in this.dictFactions.Values)
		{
			jsonFaction.Destroy();
		}
		this.dictFactions.Clear();
		this.dictFactions = null;
		this.dictShipOwners.Clear();
		this.dictShipOwners = null;
	}

	private void LedgerAlert()
	{
		float unpaidAmount = Ledger.GetUnpaidAmount(null, CrewSim.coPlayer.strID, null);
		if (unpaidAmount <= 0f)
		{
			return;
		}
		this.bWarnedShift = true;
		AudioManager.am.PlayAudioEmitter("ShipUIBtnFinanceAcceptNeg", false, false);
		CrewSim.coPlayer.LogMessage(DataHandler.GetString("GUI_FINANCE_SHIFT_WARN", false), "Bad", CrewSim.coPlayer.strID);
		CrewSim.coPlayer.ZeroCondAmount("IsFinanceChecked");
		Objective objective = new Objective(CrewSim.coPlayer, "Payments Due", "TIsFinanceChecked");
		objective.strDisplayDesc = "Open the Cash UI to resolve any outstanding payments due.";
		MonoSingleton<ObjectiveTracker>.Instance.AddObjective(objective);
	}

	public Ship AddDerelict(string strLoot, string strOwner)
	{
		Loot loot = DataHandler.GetLoot(strLoot);
		if (loot == null)
		{
			Debug.LogError("ERROR: Trying to spawn derelict with null loot type:" + strLoot);
			return null;
		}
		List<string> lootNames = loot.GetLootNames(null, false, null);
		if (lootNames.Count <= 0)
		{
			return null;
		}
		string strJson = lootNames[0];
		Ship ship = this.SpawnShip(strJson, null, Ship.Loaded.Shallow, Ship.Damage.Derelict, strOwner, 100, false);
		if (ship == null)
		{
			return null;
		}
		ship.ToggleVis(false, true);
		return ship;
	}

	private void AddStationEx(SpawnStationDTO ssDTO)
	{
		JsonSpawnStation jsonSpawnStation = ssDTO.JsonSpawnStation;
		if (ssDTO.BoParent == null)
		{
			Debug.LogError("ERROR: Trying to spawn ex station with null parent:" + jsonSpawnStation.strName);
			return;
		}
		BodyOrbit bodyOrbit = this.GetBO(jsonSpawnStation.strName);
		if (bodyOrbit == null)
		{
			double fRotationPeriod = ssDTO.BoParent.fRotationPeriod;
			if (jsonSpawnStation.fOrbitalPeriodYears == 0.0)
			{
				jsonSpawnStation.fOrbitalPeriodYears = this.CalculatePeriodFromPAET(jsonSpawnStation.fPeriapsisAU, jsonSpawnStation.fApoapsisAU, (ssDTO.BoParent == null) ? 0.0 : ssDTO.BoParent.fMass);
			}
			bodyOrbit = new BodyOrbit(jsonSpawnStation.strName, jsonSpawnStation.fPeriapsisAU, jsonSpawnStation.fApoapsisAU, (double)jsonSpawnStation.fDegreesCW, (double)jsonSpawnStation.fEccentricity, jsonSpawnStation.fOrbitalPeriodYears, (double)jsonSpawnStation.fRadiusKM, (double)jsonSpawnStation.fMassKG, fRotationPeriod, ssDTO.BoParent);
			bodyOrbit.nDrawFlagsBody = 1;
			if (jsonSpawnStation.bDrawTrack)
			{
				bodyOrbit.nDrawFlagsTrack = 2;
			}
			else
			{
				bodyOrbit.nDrawFlagsTrack = 1;
			}
			this.AddBO(bodyOrbit, ssDTO.BoParent);
		}
		Ship ship = this.SpawnStation(jsonSpawnStation.strShipType, jsonSpawnStation.strName, bodyOrbit, jsonSpawnStation.strOwner, jsonSpawnStation.nConstructionProgress, jsonSpawnStation.Classification);
		if (ship == null)
		{
			return;
		}
		ship.ToggleVis(false, true);
	}

	private void AddOrbitStation(SpawnStationDTO ssDTO)
	{
		if (ssDTO.BoParent == null)
		{
			Debug.LogError("ERROR: Trying to spawn orbit station with null parent:" + ssDTO.JsonSpawnStation.strName);
			return;
		}
		double fPeriapsisAU = 0.0;
		double fApoapsisAU = 0.0;
		this.CalculatePAFromET(ref fPeriapsisAU, ref fApoapsisAU, ssDTO.JsonSpawnStation.fEccentricity, ssDTO.JsonSpawnStation.fOrbitalPeriodYears, ssDTO.BoParent);
		ssDTO.JsonSpawnStation.fApoapsisAU = fApoapsisAU;
		ssDTO.JsonSpawnStation.fPeriapsisAU = fPeriapsisAU;
		this.AddStationEx(ssDTO);
	}

	private void AddGeoStation(SpawnStationDTO ssDTO)
	{
		if (ssDTO.BoParent == null)
		{
			Debug.LogError("ERROR: Trying to spawn geo station with null parent:" + ssDTO.JsonSpawnStation.strName);
			return;
		}
		ssDTO.JsonSpawnStation.fOrbitalPeriodYears = ssDTO.BoParent.fRotationPeriod / 31556926.0;
		this.AddOrbitStation(ssDTO);
	}

	private void AddGroundStation(SpawnStationDTO ssDTO)
	{
		BodyOrbit boParent = ssDTO.BoParent;
		if (boParent == null)
		{
			Debug.LogError("ERROR: Trying to spawn ground station with null parent:" + ssDTO.JsonSpawnStation.strName);
			return;
		}
		float num = (float)ShipSitu.MINSTATIONSIZE * 6.684587E-12f;
		ssDTO.JsonSpawnStation.fPeriapsisAU = boParent.fRadius + (double)num;
		ssDTO.JsonSpawnStation.fApoapsisAU = boParent.fRadius + (double)num;
		ssDTO.JsonSpawnStation.fOrbitalPeriodYears = boParent.fRotationPeriod / 31556926.0;
		this.AddStationEx(ssDTO);
	}

	private void AddDerelictRing(JsonSpawnDerelict jsd)
	{
		int num = MathUtils.Rand(jsd.nSpawnCountMin, jsd.nSpawnCountMax, MathUtils.RandType.Mid, null);
		float num2 = jsd.fVelMaxKMpS / 149597870f;
		BodyOrbit bo = this.GetBO(jsd.strSpawnAroundBody);
		double dX = 0.0;
		double dY = 0.0;
		if (bo != null)
		{
			bo.UpdateTime(StarSystem.fEpoch, true, true);
			dX = bo.dXReal;
			dY = bo.dYReal;
		}
		for (int i = 0; i < num; i++)
		{
			Ship ship = this.AddDerelict(jsd.strLootShipType, jsd.strOwner);
			if (ship != null)
			{
				bo.UpdateTime(StarSystem.fEpoch, true, true);
				double fSpawnRadiusMaxKM = jsd.fSpawnRadiusMaxKM;
				double num3 = MathUtils.Rand(0.0, 1.0, MathUtils.RandType.Low, null);
				num3 = jsd.fSpawnRadiusMinKM + num3 * num3 * fSpawnRadiusMaxKM;
				if ((double)i < 0.05 * (double)num)
				{
					num3 = MathUtils.Rand(jsd.fSpawnRadiusMinKM, fSpawnRadiusMaxKM, MathUtils.RandType.High, null);
				}
				this.SetSituToRandomSafeCoords(ship.objSS, num3 / 149597872.0, num3 / 149597872.0, dX, dY, MathUtils.RandType.Low);
				ship.objSS.fRot = MathUtils.Rand(0f, 6.2831855f, MathUtils.RandType.Flat, null);
				if (jsd.bIsBodyLocked)
				{
					ship.objSS.LockToBO(bo, -1.0);
				}
				else
				{
					ship.objSS.fW = MathUtils.Rand(0f, 1f, MathUtils.RandType.Low, null) - 0.5f;
					ship.objSS.vVelX = (double)(MathUtils.Rand(0f, 2f * num2, MathUtils.RandType.Low, null) - num2);
					ship.objSS.vVelY = (double)(MathUtils.Rand(0f, 2f * num2, MathUtils.RandType.Low, null) - num2);
				}
				ship.bXPDRAntenna = false;
				ship.strXPDR = null;
				ship.objSS.bIsNoFees = true;
				ship.fBreakInMultiplier = (float)(1.0 - MathUtils.Rand((num3 - jsd.fSpawnRadiusMinKM) / fSpawnRadiusMaxKM, 1.0, MathUtils.RandType.Low, null));
				if (jsd.aFactions != null && jsd.aFactions.Length > 0)
				{
					ship.SetFactions(CrewSim.system.GetFactions(jsd.aFactions), false);
				}
			}
		}
	}

	public bool CheckShipSpawned(string strRegID)
	{
		return strRegID != null && this.dictShips.ContainsKey(strRegID);
	}

	public Ship SpawnShip(string strRegID, Ship.Loaded nLoad)
	{
		Ship ship = null;
		if (strRegID != null && this.dictShips.TryGetValue(strRegID, out ship))
		{
			ship.InitShip(ship.IsTemplateShip && this.bAllowTemplates, nLoad, null);
			return ship;
		}
		return null;
	}

	public Ship SpawnShip(string strJson, string strRegID, Ship.Loaded nLoad, Ship.Damage nDamage, string strOwner, int constructionProgress = 100, bool isStation = false)
	{
		Ship ship = this.SpawnShip(strRegID, nLoad);
		if (ship != null)
		{
			return ship;
		}
		if (strJson == null)
		{
			return null;
		}
		JsonShip jsonShip = DataHandler.GetShip(strJson);
		if (jsonShip == null)
		{
			return null;
		}
		jsonShip = jsonShip.Clone();
		jsonShip.nConstructionProgress = constructionProgress;
		if (constructionProgress < 100)
		{
			JsonShipConstructionTemplate shipConstructionTemplate = DataHandler.GetShipConstructionTemplate(jsonShip);
			jsonShip.aItems = shipConstructionTemplate.aItems;
			jsonShip.aShallowPSpecs = shipConstructionTemplate.aShallowPSpecs;
		}
		jsonShip.DMGStatus = nDamage;
		jsonShip.strRegID = strRegID;
		if (isStation && jsonShip.objSS != null)
		{
			jsonShip.objSS.bIsBO = isStation;
		}
		return this._SpawnShip(true, jsonShip, nLoad, strOwner);
	}

	private Ship _SpawnShip(bool bTemplate, JsonShip json, Ship.Loaded nLoad, string strOwner)
	{
		GameObject go = new GameObject();
		Ship ship = new Ship(go);
		ship.json = json;
		this.RegisterShipOwner(json.strRegID, strOwner);
		ship.InitShip(bTemplate, nLoad, json.strRegID);
		this.AddShip(ship, strOwner);
		return ship;
	}

	public bool SetSituToRandomSafeCoords(ShipSitu objSS, double fRadMin, double fRadMax, double dX, double dY, MathUtils.RandType randType = MathUtils.RandType.Low)
	{
		bool flag = false;
		int num = 20;
		do
		{
			flag = false;
			num--;
			double num2 = MathUtils.Rand(fRadMin, fRadMax, randType, null);
			double num3 = MathUtils.Rand(0.0, 6.2831854820251465, MathUtils.RandType.Flat, null);
			objSS.vPosx = dX + Math.Sin(num3) * num2;
			objSS.vPosy = dY + Math.Cos(num3) * num2;
			BodyOrbit nearestBO = this.GetNearestBO(objSS, StarSystem.fEpoch, false);
			double num4 = Math.Sqrt((nearestBO.dXReal - objSS.vPosx) * (nearestBO.dXReal - objSS.vPosx) + (nearestBO.dYReal - objSS.vPosy) * (nearestBO.dYReal - objSS.vPosy));
			if (num4 <= nearestBO.fRadius)
			{
				flag = true;
			}
			else
			{
				foreach (Ship ship in this.dictShips.Values)
				{
					if (ship.objSS != objSS)
					{
						double num5 = ship.objSS.GetRangeTo(objSS);
						num5 -= (double)(ship.objSS.GetRadiusAU() + objSS.GetRadiusAU());
						if (num5 <= 0.0)
						{
							flag = true;
							break;
						}
					}
				}
			}
		}
		while (flag && num > 0);
		if (num <= 0)
		{
			Debug.LogWarning("Could not find safe coordinates to spawn object");
			return false;
		}
		return true;
	}

	public void SetSituToSafeCoords(ShipSitu objSS)
	{
		bool flag = false;
		int num = 0;
		BodyOrbit nearestBO = this.GetNearestBO(objSS, StarSystem.fEpoch, false);
		do
		{
			flag = false;
			if (num != 0)
			{
				double num2 = (double)MathUtils.Rand(objSS.GetRadiusAU(), objSS.GetRadiusAU() + 4.679211E-08f, MathUtils.RandType.Low, null);
				double num3 = MathUtils.Rand(0.0, 6.2831854820251465, MathUtils.RandType.Flat, null);
				objSS.vPosx += Math.Sin(num3) * num2;
				objSS.vPosy += Math.Cos(num3) * num2;
			}
			num++;
			double distance = objSS.GetDistance(nearestBO.dXReal, nearestBO.dYReal);
			double num4 = CollisionManager.GetCollisionDistanceAU(objSS, nearestBO) + 3.3422935320898146E-09;
			if (distance < num4)
			{
				Point normalized = (objSS.vPos - nearestBO.vPos).normalized;
				Point point = objSS.vPos + (num4 - distance) * normalized;
				objSS.vPosx = point.X;
				objSS.vPosy = point.Y;
			}
			foreach (Ship ship in this.dictShips.Values)
			{
				if (ship.objSS != objSS && !ship.bNoCollisions)
				{
					double num5 = ship.objSS.GetRangeTo(objSS);
					num5 -= (double)(ship.objSS.GetRadiusAU() + objSS.GetRadiusAU());
					if (num5 <= 0.0)
					{
						flag = true;
						break;
					}
				}
			}
		}
		while (flag && num < 20);
		if (num > 0)
		{
			return;
		}
		Debug.LogWarning("Could not find safe coordinates to spawn object");
	}

	private Ship SpawnStation(string strJson, string strRegID, BodyOrbit bo, string strOwner, int constructionProgress, Ship.TypeClassification classification)
	{
		Ship ship = this.SpawnShip(strJson, strRegID, Ship.Loaded.Shallow, Ship.Damage.New, strOwner, constructionProgress, true);
		if (ship == null)
		{
			Debug.LogError("ERROR: Unable to spawn station: " + strJson + "; Aborting.");
			return null;
		}
		ship.strXPDR = strRegID;
		ship.bXPDRAntenna = true;
		ship.Classification = classification;
		ship.objSS.bIsBO = true;
		ship.objSS.strBOPORShip = bo.strName;
		ship.json.objSS.bIsBO = true;
		ship.json.objSS.boPORShip = bo.strName;
		return ship;
	}

	public void AddShip(Ship objShip, string strOwner)
	{
		if (objShip == null)
		{
			return;
		}
		if (string.IsNullOrEmpty(strOwner))
		{
			strOwner = this.GetShipOwner(objShip.strRegID);
		}
		if (!this.dictShips.ContainsValue(objShip))
		{
			this.dictShips[objShip.strRegID] = objShip;
		}
		else
		{
			string[] array = new string[this.dictShips.Keys.Count];
			this.dictShips.Keys.CopyTo(array, 0);
			foreach (string text in array)
			{
				if (this.dictShips[text] == objShip)
				{
					this.dictShips.Remove(text);
					if (this.dictShipOwners.ContainsKey(text) && text != objShip.strRegID)
					{
						this.dictShipOwners.Remove(text);
					}
				}
			}
			this.dictShips[objShip.strRegID] = objShip;
		}
		this.RegisterShipOwner(objShip.strRegID, strOwner);
		objShip.gameObject.transform.SetParent(GameObject.Find("PlayState").transform, false);
		CollisionManager.RegisterShip(objShip);
	}

	private void UpdateShip(double fTimeDelta, Ship ship)
	{
		ship.DamageOverTime();
		ship.Sparks();
		if (!CrewSim.bPoolShipUpdates)
		{
			if (ship.bCheckRooms)
			{
				ship.CreateRooms(null);
			}
			if (ship.bCheckPower)
			{
				ship.UpdatePower();
			}
			if (ship.bCheckLocks)
			{
				ship.CheckLocks();
			}
		}
		if (ship.bCheckTargets)
		{
			ship.CheckTargets();
		}
		int nCurrentWaypoint = ship.nCurrentWaypoint;
		int currentWaypoint = FlightCPU.GetCurrentWaypoint(ship.fTimeEngaged, ship.aWPs, ship.objSS);
		if (currentWaypoint >= 0 && currentWaypoint != nCurrentWaypoint && FlightCPU.MatchSitu(ship, ship.aWPs[currentWaypoint].objSS))
		{
			ship.nCurrentWaypoint = currentWaypoint;
		}
		if (!ship.objSS.bIsBO && !ship.objSS.bBOLocked && !ship.objSS.bOrbitLocked)
		{
			this.temp_ptPORGrav = Vector2.zero;
			this.temp_boAtmo = null;
			this.temp_boGrav = this.GetGreatestGravBOFast(ship.objSS, ref this.temp_ptPORGrav, ref this.temp_boAtmo);
			if (this.temp_boGrav == null)
			{
				this.temp_boGrav = this.aBOs.FirstOrDefault<KeyValuePair<string, BodyOrbit>>().Value;
			}
			ship.objSS.strBOPORShip = this.temp_boGrav.strName;
			ship.objSS.vAccEx += this.temp_ptPORGrav;
			ship.objSS.TimeAdvance(fTimeDelta, false);
			double gravity = (double)((ship.objSS.vAccLift + ship.objSS.vAccIn + ship.objSS.vAccRCS + ship.objSS.vAccDrag).magnitude / 6.684587E-12f / 9.81f);
			ship.objSS.vAccEx -= this.temp_ptPORGrav;
			if (!CrewSim.bShipEdit && (fTimeDelta != 0.0 || ship.bNeedsGravSet) && (ship.objSS.vPosx != 0.0 || ship.objSS.vPosy != 0.0))
			{
				ship.Gravity = gravity;
				ship.UpdateGravAndAtmo(this.temp_boGrav, this.temp_boAtmo, this.temp_ptPORGrav);
				ship.bNeedsGravSet = false;
			}
		}
		else
		{
			ship.objSS.TimeAdvance(fTimeDelta, false);
			if (fTimeDelta != 0.0 && (ship.objSS.vPosx != 0.0 || ship.objSS.vPosy != 0.0))
			{
				this.temp_ptPORGrav = Vector2.zero;
				this.temp_boAtmo = null;
				this.temp_boGrav = this.GetGreatestGravBOFast(ship.objSS, ref this.temp_ptPORGrav, ref this.temp_boAtmo);
				bool flag = ship.IsGroundStation();
				if (!flag)
				{
					foreach (Ship ship2 in ship.GetAllDockedShipsFull())
					{
						if (ship2.IsGroundStation())
						{
							flag = true;
							break;
						}
					}
				}
				if (flag)
				{
					ship.objSS.vAccEx += this.temp_ptPORGrav;
					ship.Gravity = (double)(ship.objSS.vAccEx.magnitude / 6.684587E-12f / 9.81f);
					ship.objSS.vAccEx -= this.temp_ptPORGrav;
				}
				else
				{
					ship.Gravity = 0.0;
				}
				ship.UpdateGravAndAtmo(this.temp_boGrav, this.temp_boAtmo, Vector2.zero);
			}
		}
		if (ship.objSS.aPathRecent.Count == 0 || StarSystem.fEpoch - ship.objSS.aPathRecent[0].Item1 >= 2.0)
		{
			ship.objSS.LogPath();
		}
		ship.GravApplyCrew();
		ship.PostUpdate();
	}

	public IEnumerable<Ship> GetAllLoadedShips()
	{
		return (this.dictShips != null) ? this.dictShips.Values : null;
	}

	public IEnumerable<Ship> GetAllLoadedShipsWithin(ShipSitu center, double range)
	{
		if (this.dictShips == null)
		{
			return null;
		}
		List<Ship> list = new List<Ship>();
		foreach (KeyValuePair<string, Ship> keyValuePair in this.dictShips)
		{
			Ship value = keyValuePair.Value;
			if (value != null && value.objSS != null)
			{
				if (value.objSS.GetRangeTo(center) < range)
				{
					list.Add(value);
				}
			}
		}
		return list;
	}

	public bool IsWithinNoWakeRangeOfAnyStation(ShipSitu objSS, double fEpochCheck)
	{
		foreach (Ship ship in CrewSim.system.dictShips.Values)
		{
			if (ship.IsStation(false) && !ship.IsNotAFullStation)
			{
				ship.objSS.UpdateTime(fEpochCheck, false);
				double distance = ship.objSS.GetDistance(objSS);
				if (distance < 2.005376018132665E-06)
				{
					return true;
				}
			}
		}
		return false;
	}

	public List<Ship> GetStations(bool includeHidden = false)
	{
		List<Ship> list = new List<Ship>();
		foreach (KeyValuePair<string, Ship> keyValuePair in this.dictShips)
		{
			Ship value = keyValuePair.Value;
			if (value != null && !value.bDestroyed)
			{
				if (value.IsStation(false) || (includeHidden && value.IsStationHidden(false)))
				{
					list.Add(value);
				}
			}
		}
		return list;
	}

	public Ship GetNearestStation(double sx, double sy, bool excludeOutposts = false)
	{
		Ship result = null;
		double num = 1E+20;
		foreach (Ship ship in this.dictShips.Values)
		{
			if (ship.IsStation(false) && (!excludeOutposts || (!ship.IsNotAFullStation && !ship.IsSubStation())))
			{
				double distance = ship.objSS.GetDistance(sx, sy);
				if (num > distance)
				{
					num = distance;
					result = ship;
				}
			}
		}
		return result;
	}

	public Ship GetStationClosestToRegionalBorder(ShipSitu origin, ShipSitu destination, double fEpoch)
	{
		Ship result = null;
		double num = 0.0;
		BodyOrbit nearestBO = this.GetNearestBO(origin, fEpoch, false);
		foreach (KeyValuePair<string, Ship> keyValuePair in this.dictShips)
		{
			Ship value = keyValuePair.Value;
			if (value != null && !value.bDestroyed && value.IsStation(false) && !value.IsNotAFullStation)
			{
				BodyOrbit nearestBO2 = this.GetNearestBO(value.objSS, fEpoch, false);
				if (!(nearestBO2.strName != nearestBO.strName))
				{
					value.objSS.UpdateTime(fEpoch, false);
					Point[] array = MathUtils.FindCircleLineIntersections(value.objSS.vPos.X, value.objSS.vPos.Y, 2.005376018132665E-06, origin.vPos, destination.vPos);
					if (array != null && array.Length != 0)
					{
						foreach (Point p in array)
						{
							bool flag = p.X >= Math.Min(origin.vPos.X, destination.vPos.X) && p.X <= Math.Max(origin.vPos.X, destination.vPos.X);
							bool flag2 = p.Y >= Math.Min(origin.vPos.Y, destination.vPos.Y) && p.Y <= Math.Max(origin.vPos.Y, destination.vPos.Y);
							if (flag && flag2)
							{
								double magnitude = MathUtils.GetMagnitude(p, origin.vPos);
								if (magnitude >= num)
								{
									result = value;
									num = magnitude;
								}
							}
						}
					}
				}
			}
		}
		return result;
	}

	public static void SpawnMicroMeteoroid(Ship ship, float fMult, bool resetTimeScale)
	{
		if (ship == null || ship.LoadState < Ship.Loaded.Full)
		{
			return;
		}
		JsonAttackMode attackMode = DataHandler.GetAttackMode("AModeMicrometeoroid");
		bool bAudio = Wound.bAudio;
		Wound.bAudio = true;
		if (attackMode != null)
		{
			ship.DamageRayRandom(attackMode, fMult, null, false);
		}
		Wound.bAudio = bAudio;
		Loot loot = DataHandler.GetLoot("TXTRandomBangAudio");
		if (loot != null)
		{
			CrewSim.objInstance.CamShake(0.2f);
			AudioManager.am.PlayAudioEmitter(loot.GetLootNameSingle(null), false, false);
		}
		if (resetTimeScale)
		{
			CrewSim.ResetTimeScale();
		}
	}

	public Ship GetNearestStationRegional(double sx, double sy)
	{
		Ship result = null;
		double num = 1E+20;
		foreach (Ship ship in this.dictShips.Values)
		{
			if (ship.IsStation(false) && ship.objSS.bIsRegion)
			{
				double distance = ship.objSS.GetDistance(sx, sy);
				if (num > distance)
				{
					num = distance;
					result = ship;
				}
			}
		}
		return result;
	}

	public Vector2 GetGravAccel(BodyOrbit bo, ShipSitu objSS)
	{
		if (bo == null || objSS == null)
		{
			return default(Vector2);
		}
		float num = Convert.ToSingle(bo.dXReal - objSS.vPosx);
		float num2 = Convert.ToSingle(bo.dYReal - objSS.vPosy);
		float num3 = num * num + num2 * num2;
		if (num3 == 0f)
		{
			return default(Vector2);
		}
		float num4 = (float)((double)this.fGravAccelConstant * bo.fMass * Math.Pow((double)num3, -1.5));
		return new Vector2(num * num4, num2 * num4);
	}

	public Vector2 GetGravAccelPoint(BodyOrbit bo, double x, double y)
	{
		if (bo == null)
		{
			return default(Vector2);
		}
		float num = Convert.ToSingle(bo.dXReal - x);
		float num2 = Convert.ToSingle(bo.dYReal - y);
		float num3 = num * num + num2 * num2;
		if (num3 == 0f)
		{
			return default(Vector2);
		}
		float num4 = (float)((double)this.fGravAccelConstant * bo.fMass * (double)Mathf.Pow(num3, -1.5f));
		return new Vector2(num * num4, num2 * num4);
	}

	public double GetGravAccelScalar(BodyOrbit bo, ShipSitu objSS, ref double fDistSquared)
	{
		double num = bo.dXReal - objSS.vPosx;
		double num2 = bo.dYReal - objSS.vPosy;
		fDistSquared = num * num + num2 * num2;
		if (fDistSquared == 0.0)
		{
			fDistSquared = 1.6711467660449074E-07;
		}
		return (double)this.fGravAccelConstant * bo.fMass / fDistSquared;
	}

	public static bool IsLOSBlockedByBO(BodyOrbit bo, Ship shipA, Ship shipB)
	{
		double num = MathUtils.FirstLineSegmentCircleIntersect(shipA.objSS.vPosx, shipA.objSS.vPosy, shipB.objSS.vPosx, shipB.objSS.vPosy, bo.dXReal, bo.dYReal, bo.fRadius);
		return num < 1.0;
	}

	public BodyOrbit GetGreatestGravBO(ShipSitu objSS, double dfEpoch, ref Vector2 ptGrav, ref BodyOrbit boClosest)
	{
		double systemToEpoch = StarSystem.fEpoch;
		BodyOrbit bodyOrbit = null;
		ptGrav.Set(0f, 0f);
		double num = 0.0;
		double num2 = double.PositiveInfinity;
		this.SetSystemToEpoch(dfEpoch);
		if (this._filteredBoCache == null)
		{
			this._filteredBoCache = (from x in this.aBOs.Values
			where x != null && x.nDrawFlagsBody != 1
			select x).ToList<BodyOrbit>();
			if (this._filteredBoCache == null)
			{
				this._filteredBoCache = new List<BodyOrbit>();
				Debug.LogWarning("BO Cache is null!");
			}
		}
		foreach (BodyOrbit bodyOrbit2 in this._filteredBoCache)
		{
			double num3 = 0.0;
			double gravAccelScalar = this.GetGravAccelScalar(bodyOrbit2, objSS, ref num3);
			if (gravAccelScalar > num)
			{
				num = gravAccelScalar;
				bodyOrbit = bodyOrbit2;
			}
			if (bodyOrbit2.nDrawFlagsBody != 1 && num3 < num2)
			{
				boClosest = bodyOrbit2;
				num2 = num3;
			}
		}
		Vector2 gravAccel = this.GetGravAccel(bodyOrbit, objSS);
		ptGrav.Set(gravAccel.x, gravAccel.y);
		this.SetSystemToEpoch(systemToEpoch);
		return bodyOrbit;
	}

	private BodyOrbit GetGreatestGravBOFast(ShipSitu objSS, ref Vector2 ptGrav, ref BodyOrbit boClosest)
	{
		ptGrav.Set(0f, 0f);
		BodyOrbit bodyOrbit = this.boStar;
		BodyOrbit bodyOrbit2 = bodyOrbit;
		double num = 0.0;
		double num2 = this.GetGravAccelScalar(bodyOrbit, objSS, ref num);
		double num3 = num;
		bool flag = false;
		while (bodyOrbit.boChildren != null)
		{
			flag = false;
			foreach (BodyOrbit bodyOrbit3 in bodyOrbit.boChildren)
			{
				double gravAccelScalar = this.GetGravAccelScalar(bodyOrbit3, objSS, ref num);
				if (gravAccelScalar > num2)
				{
					num2 = gravAccelScalar;
					bodyOrbit2 = bodyOrbit3;
					flag = true;
				}
				if (num < num3)
				{
					boClosest = bodyOrbit3;
					num3 = num;
					flag = true;
				}
			}
			if (!flag)
			{
				break;
			}
			if (bodyOrbit == bodyOrbit2 && boClosest != bodyOrbit)
			{
				bodyOrbit = boClosest;
			}
			else
			{
				bodyOrbit = bodyOrbit2;
			}
		}
		Vector2 gravAccel = this.GetGravAccel(bodyOrbit2, objSS);
		ptGrav.Set(gravAccel.x, gravAccel.y);
		return bodyOrbit2;
	}

	public BodyOrbit GetNearestBO(ShipSitu objSS, double dfEpoch, bool bIncludePlaceholders)
	{
		double systemToEpoch = StarSystem.fEpoch;
		BodyOrbit result = null;
		double num = 1E+20;
		this.SetSystemToEpoch(dfEpoch);
		foreach (KeyValuePair<string, BodyOrbit> keyValuePair in this.aBOs)
		{
			if ((bIncludePlaceholders || !keyValuePair.Value.IsPlaceholder()) && !keyValuePair.Value.IsShipOrbit())
			{
				double collisionDistanceAU = CollisionManager.GetCollisionDistanceAU(objSS, keyValuePair.Value);
				double distance = objSS.GetDistance(keyValuePair.Value.dXReal, keyValuePair.Value.dYReal);
				if (distance - collisionDistanceAU < num)
				{
					num = distance;
					result = keyValuePair.Value;
				}
			}
		}
		this.SetSystemToEpoch(systemToEpoch);
		return result;
	}

	public BodyOrbit GetCloserBO(ShipSitu objSS, double dfEpoch, BodyOrbit boA, BodyOrbit boB)
	{
		if (boA == null || boB == null)
		{
			return null;
		}
		double systemToEpoch = StarSystem.fEpoch;
		BodyOrbit result = null;
		double num = 1E+20;
		this.SetSystemToEpoch(dfEpoch);
		double collisionDistanceAU = CollisionManager.GetCollisionDistanceAU(objSS, boA);
		double distance = objSS.GetDistance(boA.dXReal, boA.dYReal);
		if (distance - collisionDistanceAU < num)
		{
			num = distance;
			result = boA;
		}
		collisionDistanceAU = CollisionManager.GetCollisionDistanceAU(objSS, boB);
		distance = objSS.GetDistance(boB.dXReal, boB.dYReal);
		if (distance - collisionDistanceAU < num)
		{
			result = boB;
		}
		this.SetSystemToEpoch(systemToEpoch);
		return result;
	}

	public BodyOrbit AddBO(BodyOrbit bo, BodyOrbit boParent = null)
	{
		if (boParent != null)
		{
			Debug.Log("#Info# Add " + boParent.strName + "." + bo.strName);
			if (!this.aBOs.ContainsKey(boParent.strName))
			{
				Debug.Log("Panic!");
			}
			this.dictBOHierarchy[bo] = boParent;
			if (bo.nDrawFlagsBody != 1)
			{
				if (boParent.boChildren == null)
				{
					boParent.boChildren = new List<BodyOrbit>();
				}
				boParent.boChildren.Add(bo);
			}
		}
		if (this.aBOs.ContainsKey(bo.strName))
		{
			Debug.Log("Tried to add same BodyOrbit twice");
		}
		this.aBOs.Add(bo.strName, bo);
		return bo;
	}

	public void RemoveBO(BodyOrbit bo)
	{
		if (bo == null)
		{
			return;
		}
		this.dictBOHierarchy.Remove(bo);
		if (bo.boParent != null)
		{
			if (bo.boParent.boChildren != null)
			{
				bo.boParent.boChildren.Remove(bo);
			}
			if (bo.boChildren != null)
			{
				if (bo.boParent.boChildren == null)
				{
					bo.boParent.boChildren = new List<BodyOrbit>();
				}
				bo.boParent.boChildren.AddRange(bo.boChildren);
			}
		}
		GUIOrbitDraw.RemoveBODraw(bo.strName);
		this.aBOs.Remove(bo.strName);
	}

	public BodyOrbit GetBO(string strName)
	{
		BodyOrbit result = null;
		if (!string.IsNullOrEmpty(strName))
		{
			this.aBOs.TryGetValue(strName, out result);
		}
		return result;
	}

	public List<BodyOrbit> GetSubSystem(BodyOrbit boSub)
	{
		List<BodyOrbit> list = new List<BodyOrbit>();
		list.Add(boSub);
		foreach (KeyValuePair<string, BodyOrbit> keyValuePair in this.aBOs)
		{
			if (this.CheckOrbitalParent(keyValuePair.Value, boSub))
			{
				list.Add(boSub);
			}
		}
		return list;
	}

	public void AddCompany(JsonCompany jc)
	{
		if (jc == null || jc.strName == null)
		{
			return;
		}
		this.dictCompanies[jc.strName] = jc;
	}

	public JsonCompany GetCompany(string strName)
	{
		if (strName == null || this.dictCompanies == null)
		{
			return null;
		}
		JsonCompany result = null;
		this.dictCompanies.TryGetValue(strName, out result);
		return result;
	}

	public bool RenameCompany(string strNameOld, JsonCompany jc)
	{
		if (strNameOld == null || jc == null || this.dictCompanies == null)
		{
			return false;
		}
		if (this.dictCompanies.ContainsKey(jc.strName))
		{
			return false;
		}
		JsonCompany jsonCompany = null;
		if (this.dictCompanies.TryGetValue(strNameOld, out jsonCompany))
		{
			this.dictCompanies.Remove(strNameOld);
		}
		this.dictCompanies[jc.strName] = jc;
		return true;
	}

	public void AddFaction(JsonFaction jf)
	{
		if (jf == null || jf.strName == null)
		{
			return;
		}
		this.dictFactions[jf.strName] = jf;
		if (jf.bAutoAdd && this.aAutoFactions.IndexOf(jf) < 0)
		{
			this.aAutoFactions.Add(jf);
		}
	}

	public JsonFaction GetFaction(string strName)
	{
		if (strName == null || this.dictFactions == null)
		{
			return null;
		}
		JsonFaction result = null;
		this.dictFactions.TryGetValue(strName, out result);
		return result;
	}

	public List<JsonFaction> GetFactions(string[] aFactions)
	{
		if (aFactions == null || this.dictFactions == null)
		{
			return new List<JsonFaction>();
		}
		List<JsonFaction> list = new List<JsonFaction>();
		foreach (string strName in aFactions)
		{
			JsonFaction faction = CrewSim.system.GetFaction(strName);
			if (faction != null)
			{
				list.Add(faction);
			}
		}
		return list;
	}

	public void RenameFaction(string strNameNew, JsonFaction jf)
	{
		if (strNameNew == null || jf == null || this.dictFactions == null)
		{
			return;
		}
		List<CondOwner> list = new List<CondOwner>();
		foreach (string key in jf.aMembers)
		{
			CondOwner item = null;
			if (DataHandler.mapCOs.TryGetValue(key, out item))
			{
				list.Add(item);
			}
		}
		foreach (CondOwner condOwner in list)
		{
			condOwner.RemoveFaction(jf);
		}
		this.dictFactions.Remove(jf.strName);
		jf.strName = strNameNew;
		this.dictFactions[jf.strName] = jf;
		foreach (CondOwner condOwner2 in list)
		{
			condOwner2.AddFaction(jf);
		}
	}

	public void AddAutoFactions(CondOwner co)
	{
		if (co == null)
		{
			return;
		}
		foreach (JsonFaction jsonFaction in this.aAutoFactions)
		{
			if (jsonFaction.Triggered(co))
			{
				co.AddFaction(jsonFaction);
			}
		}
	}

	public static PersonSpec GetPerson(JsonPersonSpec jps, global::Social soc, bool bForceUnrelated, List<string> aForbids = null, JsonShipSpec jss = null)
	{
		if (jps == null)
		{
			return null;
		}
		if (bForceUnrelated || jps.strCTRelFind == null || jps.strCTRelFind == string.Empty)
		{
			List<PersonSpec> persons = StarSystem.GetPersons(jps, soc, bForceUnrelated, aForbids, jss);
			if (persons == null)
			{
				return null;
			}
			return persons[MathUtils.Rand(0, persons.Count - 1, MathUtils.RandType.Flat, null)];
		}
		else
		{
			if (soc == null)
			{
				return null;
			}
			string matchingRelation = soc.GetMatchingRelation(jps, aForbids, jss);
			if (matchingRelation != null)
			{
				return soc.GetPSpec(matchingRelation);
			}
			return null;
		}
	}

	public static List<PersonSpec> GetPersons(JsonPersonSpec jps, global::Social soc, bool bForceUnrelated, List<string> aForbids = null, JsonShipSpec jss = null)
	{
		if (jps == null)
		{
			return null;
		}
		CondOwner coUs = null;
		if (soc != null)
		{
			coUs = soc.GetComponent<CondOwner>();
		}
		List<PersonSpec> list = null;
		foreach (Ship ship in CrewSim.system.dictShips.Values)
		{
			if (ship != null)
			{
				if (jss == null || jss.Matches(ship, coUs))
				{
					PersonSpec person = ship.GetPerson(jps, soc, bForceUnrelated, aForbids);
					if (person != null)
					{
						if (list == null)
						{
							list = new List<PersonSpec>();
						}
						list.Add(person);
					}
				}
			}
		}
		return list;
	}

	public bool CheckOrbitalParent(BodyOrbit boChild, BodyOrbit boParent)
	{
		return this.dictBOHierarchy.ContainsKey(boChild) && (this.dictBOHierarchy[boChild] == boParent || this.CheckOrbitalParent(this.dictBOHierarchy[boChild], boParent));
	}

	public void SetParallax(string strParallax)
	{
		JsonParallax parallax = DataHandler.GetParallax(strParallax);
		ParallaxController component = CanvasManager.instance.goCanvasParallax.GetComponent<ParallaxController>();
		component.SetData(parallax);
	}

	public int GetYear()
	{
		return Mathf.FloorToInt(Convert.ToSingle(StarSystem.fEpoch / 31556926.0));
	}

	public JsonStarSystemSave GetJSONSave()
	{
		JsonStarSystemSave jsonStarSystemSave = new JsonStarSystemSave();
		jsonStarSystemSave.dfEpoch = StarSystem.fEpoch;
		List<JsonBodyOrbitSave> list = new List<JsonBodyOrbitSave>();
		foreach (KeyValuePair<string, BodyOrbit> keyValuePair in this.aBOs)
		{
			list.Add(keyValuePair.Value.GetJSONSave());
		}
		jsonStarSystemSave.aBOs = list.ToArray();
		list.Clear();
		List<string> list2 = new List<string>();
		foreach (string item in this.dictShips.Keys)
		{
			list2.Add(item);
		}
		jsonStarSystemSave.dictShips = list2.ToArray();
		list2.Clear();
		foreach (BodyOrbit bodyOrbit in this.dictBOHierarchy.Keys)
		{
			list2.Add(bodyOrbit.strName + "=" + this.dictBOHierarchy[bodyOrbit].strName);
		}
		jsonStarSystemSave.dictBOHierarchy = list2.ToArray();
		list2.Clear();
		List<JsonCompany> list3 = new List<JsonCompany>();
		foreach (JsonCompany jsonCompany in this.dictCompanies.Values)
		{
			if (jsonCompany != null)
			{
				list3.Add(jsonCompany.Clone());
			}
		}
		jsonStarSystemSave.aComps = list3.ToArray();
		list3.Clear();
		list3 = null;
		if (this._messagesEnRoute.Count > 0)
		{
			List<JsonShipMessage> list4 = new List<JsonShipMessage>();
			foreach (ShipMessage shipMessage in this._messagesEnRoute.Values)
			{
				list4.Add(shipMessage.GetJson());
			}
			jsonStarSystemSave.aShipMessages = list4.ToArray();
		}
		List<JsonFaction> list5 = new List<JsonFaction>();
		foreach (JsonFaction jsonFaction in this.dictFactions.Values)
		{
			if (jsonFaction != null)
			{
				list5.Add(jsonFaction.Clone());
			}
		}
		jsonStarSystemSave.aFactions = list5.ToArray();
		list5.Clear();
		list5 = null;
		jsonStarSystemSave.dictShipOwners = DataHandler.ConvertDictToStringArray(this.dictShipOwners);
		return jsonStarSystemSave;
	}

	public Ship GetShipByRegID(string strRegID)
	{
		if (strRegID == null)
		{
			return null;
		}
		Ship result;
		this.dictShips.TryGetValue(strRegID, out result);
		return result;
	}

	public List<Ship> GetShipsBySubString(string strRegID)
	{
		List<Ship> list = new List<Ship>();
		if (string.IsNullOrEmpty(strRegID))
		{
			return list;
		}
		foreach (KeyValuePair<string, Ship> keyValuePair in this.dictShips)
		{
			if (keyValuePair.Value != null)
			{
				if (keyValuePair.Key.Contains(strRegID))
				{
					list.Add(keyValuePair.Value);
				}
			}
		}
		return list;
	}

	public string GetShipOwner(string strRegID)
	{
		string result = "UNREGISTERED";
		if (strRegID == null || !this.dictShipOwners.TryGetValue(strRegID, out result))
		{
			return "UNREGISTERED";
		}
		return result;
	}

	public void RegisterShipOwner(string strRegID, string strOwner)
	{
		if (string.IsNullOrEmpty(strRegID) || string.IsNullOrEmpty(strOwner))
		{
			return;
		}
		string a = null;
		if (!this.dictShipOwners.TryGetValue(strRegID, out a) || a != strOwner)
		{
			JsonCondOwner jsonCondOwner = null;
			string str = strOwner;
			if (strOwner != null && DataHandler.dictCOs.TryGetValue(strOwner, out jsonCondOwner) && !string.IsNullOrEmpty(jsonCondOwner.strNameFriendly))
			{
				str = jsonCondOwner.strNameFriendly;
			}
			Ship ship = null;
			if (this.dictShips.TryGetValue(strRegID, out ship))
			{
				ship.LogAdd(DataHandler.GetString("NAV_LOG_OWNER_CHANGED", false) + str + DataHandler.GetString("NAV_LOG_TERMINATOR", false), StarSystem.fEpoch, true);
			}
		}
		this.dictShipOwners[strRegID] = strOwner;
	}

	public List<string> GetShipsForOwner(string strOwner)
	{
		if (string.IsNullOrEmpty(strOwner))
		{
			return new List<string>();
		}
		List<string> list = new List<string>();
		foreach (KeyValuePair<string, string> keyValuePair in this.dictShipOwners)
		{
			if (keyValuePair.Value == strOwner)
			{
				list.Add(keyValuePair.Key);
			}
		}
		return list;
	}

	public bool IsOwnerWanted(string strXPDRID, Point position)
	{
		Ship nearestStation = CrewSim.system.GetNearestStation(position.X, position.Y, false);
		if (nearestStation == null || string.IsNullOrEmpty(nearestStation.strLaw))
		{
			return false;
		}
		string shipOwner = this.GetShipOwner(strXPDRID);
		CondOwner objOwner = null;
		if (DataHandler.mapCOs.TryGetValue(shipOwner, out objOwner))
		{
			CondTrigger condTrigger = DataHandler.GetCondTrigger("TIsWanted" + nearestStation.strLaw);
			return !condTrigger.IsBlank() && condTrigger.Triggered(objOwner, null, true);
		}
		return false;
	}

	public static OnNewShipCommsMessageEvent OnNewShipCommsMessage;

	private Dictionary<string, ShipMessage> _messagesEnRoute;

	public static double fEpoch;

	public Dictionary<string, BodyOrbit> aBOs;

	public Dictionary<string, Ship> dictShips;

	private Dictionary<string, string> dictShipOwners;

	public Dictionary<BodyOrbit, BodyOrbit> dictBOHierarchy;

	private Dictionary<string, JsonCompany> dictCompanies;

	private Dictionary<string, JsonFaction> dictFactions;

	private List<JsonFaction> aAutoFactions;

	public bool bAllowTemplates = true;

	private bool bWarnedShift;

	private List<string> _outpostStations;

	public BodyOrbit boStar;

	private float fGravAccelConstant;

	public const string OWNER_NONE = "UNREGISTERED";

	private List<BodyOrbit> _filteredBoCache;

	private List<Ship> temp_aDestroyed = new List<Ship>();

	private Vector2 temp_ptPORGrav = default(Vector2);

	private BodyOrbit temp_boAtmo;

	private BodyOrbit temp_boGrav;

	private double _systemEpoch = -1.0;

	private readonly List<Ship> _shipsDockedToHeavies = new List<Ship>();
}
