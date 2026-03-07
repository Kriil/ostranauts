using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using LitJson;
using Ostranauts.Core;
using Ostranauts.Ships.Commands;
using Ostranauts.Ships.Rooms;
using Ostranauts.Tools;
using Ostranauts.Tools.ExtensionMethods;
using Ostranauts.Trading;
using UnityEngine;

// Central data bootstrap and runtime registry for the decompiled game.
// This build loads core StreamingAssets/data first, then layers JSON data mods
// from the user-configured `Ostranauts_Data/Mods` path. `strModFolder` points at
// the Mods folder or its `loading_order.json`, while BepInEx/Harmony code mods
// live separately in `BepInEx/plugins`.
// Later-loaded JSON entries override earlier ids when the same `strName` is reused.
public static class DataHandler
{
	// Main startup entrypoint for data bootstrapping.
	// Likely called very early in Unity startup before save-load, UI, or world
	// generation asks for item, ship, interaction, or condition definitions.
	// Data flow: clear old runtime CondOwners -> read user settings -> resolve mod
	// load order -> load core/mod JSON registries -> emit logs -> fire InitComplete.
	public static void Init()
	{
		DataHandler.loadLog.Length = 0;
		DataHandler.loadLogError.Length = 0;
		DataHandler.loadLogWarning.Length = 0;
		if (DataHandler.bInitialised)
		{
			List<CondOwner> list = new List<CondOwner>(DataHandler.mapCOs.Values);
			foreach (CondOwner condOwner in list)
			{
				if (!(condOwner == null))
				{
					DataHandler.loadLogWarning.Append("Destroying leftover CO: ");
					DataHandler.loadLogWarning.Append(condOwner.strName);
					DataHandler.loadLogWarning.AppendLine();
					condOwner.Destroy();
				}
			}
			list.Clear();
			list = null;
			DataHandler.mapCOs.Clear();
			if (DataHandler.loadLogWarning.Length > 0)
			{
				Debug.LogWarning(DataHandler.loadLogWarning.ToString());
			}
			return;
		}
		DataHandler.strAssetPath = Application.streamingAssetsPath + "/";
		DataHandler.LoadBuildVersion();
		if (ObjReader.use)
		{
			ObjReader.use.scaleFactor = new Vector3(0.0625f, 0.0625f, 0.0625f);
			ObjReader.use.objRotation = new Vector3(90f, 0f, 180f);
		}
		DataHandler.SetupDicts();
		if (DataHandler._interactionObjectTracker == null)
		{
			DataHandler._interactionObjectTracker = new InteractionObjectTracker();
		}
		DataHandler.dictSettings["DefaultUserSettings"] = new JsonUserSettings();
		DataHandler.dictSettings["DefaultUserSettings"].Init();
		if (File.Exists(Application.persistentDataPath + "/settings.json"))
		{
			DataHandler.JsonToData<JsonUserSettings>(Application.persistentDataPath + "/settings.json", DataHandler.dictSettings);
		}
		else
		{
			DataHandler.loadLogWarning.Append("WARNING: settings.json not found. Resorting to default values.");
			DataHandler.loadLogWarning.AppendLine();
			DataHandler.ResetUserSettings();
		}
		if (!DataHandler.dictSettings.ContainsKey("UserSettings") || DataHandler.dictSettings["UserSettings"] == null)
		{
			DataHandler.loadLogError.Append("ERROR: Malformed settings.json. Resorting to default values.");
			DataHandler.loadLogError.AppendLine();
			DataHandler.ResetUserSettings();
		}
		DataHandler.dictSettings["DefaultUserSettings"].CopyTo(DataHandler.GetUserSettings());
		DataHandler.dictSettings.Remove("DefaultUserSettings");
		DataHandler.SaveUserSettings();
		bool flag = false;
		DataHandler.strModFolder = DataHandler.dictSettings["UserSettings"].strPathMods;
		if (DataHandler.strModFolder == null || DataHandler.strModFolder == string.Empty)
		{
			DataHandler.strModFolder = Path.Combine(Application.dataPath, "Mods/");
			DataHandler.loadLogWarning.Append("WARNING: Unrecognised mod folder. Setting mod path to ");
			DataHandler.loadLogWarning.Append(DataHandler.strModFolder);
			DataHandler.loadLogWarning.AppendLine();
		}
		string text = Path.GetDirectoryName(DataHandler.strModFolder);
		text = Path.Combine(text, "loading_order.json");
		JsonModInfo jsonModInfo = new JsonModInfo();
		jsonModInfo.strName = "Core";
		DataHandler.dictModInfos["core"] = jsonModInfo;
		bool flag2 = ConsoleToGUI.instance != null;
		if (flag2)
		{
			ConsoleToGUI.instance.LogInfo("Attempting to load " + text + "...");
		}
		if (File.Exists(text))
		{
			if (flag2)
			{
				ConsoleToGUI.instance.LogInfo("loading_order.json found. Beginning mod load.");
			}
			DataHandler.JsonToData<JsonModList>(text, DataHandler.dictModList);
			JsonModList jsonModList = null;
			if (DataHandler.dictModList.TryGetValue("Mod Loading Order", out jsonModList))
			{
				if (jsonModList.aIgnorePatterns != null)
				{
					for (int i = 0; i < jsonModList.aIgnorePatterns.Length; i++)
					{
						jsonModList.aIgnorePatterns[i] = DataHandler.PathSanitize(jsonModList.aIgnorePatterns[i]);
					}
				}
				foreach (string text2 in jsonModList.aLoadOrder)
				{
					flag = true;
					if (text2 == "core")
					{
						DataHandler.LoadMod(DataHandler.strAssetPath, jsonModList.aIgnorePatterns, jsonModInfo);
					}
					else if (text2 == null || text2 == string.Empty)
					{
						DataHandler.loadLogError.Append("ERROR: Invalid mod folder specified: ");
						DataHandler.loadLogError.Append(text2);
						DataHandler.loadLogError.Append("; Skipping...");
						DataHandler.loadLogError.AppendLine();
					}
					else
					{
						string text3 = text2.TrimStart(new char[]
						{
							Path.DirectorySeparatorChar
						});
						text3 = text2.TrimStart(new char[]
						{
							Path.AltDirectorySeparatorChar
						});
						text3 += "/";
						string text4 = Path.GetDirectoryName(DataHandler.strModFolder);
						text4 = Path.Combine(text4, text3);
						Dictionary<string, JsonModInfo> dictionary = new Dictionary<string, JsonModInfo>();
						string text5 = Path.Combine(text4, "mod_info.json");
						if (File.Exists(text5))
						{
							DataHandler.JsonToData<JsonModInfo>(text5, dictionary);
						}
						if (dictionary.Count < 1)
						{
							JsonModInfo jsonModInfo2 = new JsonModInfo();
							jsonModInfo2.strName = text2;
							dictionary[jsonModInfo2.strName] = jsonModInfo2;
							DataHandler.loadLogWarning.Append("WARNING: Missing mod_info.json in folder: ");
							DataHandler.loadLogWarning.Append(text2);
							DataHandler.loadLogWarning.Append("; Using default name: ");
							DataHandler.loadLogWarning.Append(jsonModInfo2.strName);
							DataHandler.loadLogWarning.AppendLine();
						}
						using (Dictionary<string, JsonModInfo>.ValueCollection.Enumerator enumerator2 = dictionary.Values.GetEnumerator())
						{
							if (enumerator2.MoveNext())
							{
								JsonModInfo value = enumerator2.Current;
								DataHandler.dictModInfos[text2] = value;
								if (flag2)
								{
									ConsoleToGUI.instance.LogInfo("Loading mod: " + DataHandler.dictModInfos[text2].strName + " from directory: " + text2);
								}
							}
						}
						DataHandler.LoadMod(text4, jsonModList.aIgnorePatterns, DataHandler.dictModInfos[text2]);
					}
				}
			}
		}
		if (!flag)
		{
			if (flag2)
			{
				ConsoleToGUI.instance.LogInfo("No loading_order.json found. Beginning default game data load from " + DataHandler.strAssetPath);
			}
			JsonModList jsonModList2 = new JsonModList();
			jsonModList2.strName = "Default";
			jsonModList2.aLoadOrder = new string[]
			{
				"core"
			};
			jsonModList2.aIgnorePatterns = new string[0];
			DataHandler.dictModList["Mod Loading Order"] = jsonModList2;
			DataHandler.LoadMod(DataHandler.strAssetPath, jsonModList2.aIgnorePatterns, jsonModInfo);
		}
		Application.SetStackTraceLogType(LogType.Log, StackTraceLogType.None);
		if (DataHandler.loadLog.Length > 0)
		{
			Debug.Log(DataHandler.loadLog.ToString());
		}
		Application.SetStackTraceLogType(LogType.Log, StackTraceLogType.ScriptOnly);
		if (DataHandler.loadLogWarning.Length > 0)
		{
			Debug.LogWarning(DataHandler.loadLogWarning.ToString());
		}
		if (DataHandler.loadLogError.Length > 0)
		{
			Debug.LogError(DataHandler.loadLogError.ToString());
		}
		DataHandler.bInitialised = true;
		if (DataHandler.InitComplete != null)
		{
			DataHandler.InitComplete();
		}
	}

	// Creates the in-memory registries that mirror the JSON folders under StreamingAssets/data.
	// Examples: `dictItemDefs` <= data/items, `dictInteractions` <= data/interactions,
	// `dictCOs` <= data/condowners, `dictCTs` <= data/condtrigs, `dictLoot` <= data/loot,
	// `dictShips` <= data/ships, `dictSlots` <= data/slots, and `dictRoomSpec` <= data/rooms.
	private static void SetupDicts()
	{
		DataHandler.dictImages = new Dictionary<string, Texture2D>();
		DataHandler.dictColors = new Dictionary<string, Color>();
		DataHandler.dictHTMLColors = new Dictionary<string, string>();
		DataHandler.dictJsonColors = new Dictionary<string, JsonColor>();
		DataHandler.dictLights = new Dictionary<string, JsonLight>();
		DataHandler.dictShips = new Dictionary<string, JsonShip>(256);
		DataHandler.dictShipImages = new Dictionary<string, Dictionary<string, Texture2D>>();
		DataHandler.dictConds = new Dictionary<string, JsonCond>(2048);
		DataHandler.dictItemDefs = new Dictionary<string, JsonItemDef>(1024);
		DataHandler.dictCTs = new Dictionary<string, CondTrigger>(4096);
		DataHandler.dictCOs = new Dictionary<string, JsonCondOwner>();
		DataHandler.dictDataCoCollections = new Dictionary<string, DataCoCollection>();
		DataHandler.dictCOSaves = new Dictionary<string, JsonCondOwnerSave>();
		DataHandler.dictInteractions = new Dictionary<string, JsonInteraction>(8192);
		DataHandler.dictLoot = new Dictionary<string, Loot>(8192);
		DataHandler.dictProductionMaps = new Dictionary<string, JsonProductionMap>();
		DataHandler.dictMarketConfigs = new Dictionary<string, JsonMarketActorConfig>();
		DataHandler.dictCargoSpecs = new Dictionary<string, JsonCargoSpec>();
		DataHandler.dictGasRespires = new Dictionary<string, JsonGasRespire>();
		DataHandler.dictPowerInfo = new Dictionary<string, JsonPowerInfo>();
		DataHandler.dictGUIPropMaps = new Dictionary<string, Dictionary<string, string>>();
		DataHandler.dictNamesFirst = new Dictionary<string, string>();
		DataHandler.dictNamesLast = new Dictionary<string, string>();
		DataHandler.dictNamesRobots = new Dictionary<string, string>();
		DataHandler.dictNamesFull = new Dictionary<string, string>();
		DataHandler.dictNamesShip = new Dictionary<string, string>();
		DataHandler.dictNamesShipAdjectives = new Dictionary<string, string>();
		DataHandler.dictNamesShipNouns = new Dictionary<string, string>();
		DataHandler.dictManPages = new Dictionary<string, string[]>();
		DataHandler.dictHomeworlds = new Dictionary<string, JsonHomeworld>();
		DataHandler.dictCareers = new Dictionary<string, JsonCareer>();
		DataHandler.dictLifeEvents = new Dictionary<string, JsonLifeEvent>();
		DataHandler.dictPersonSpecs = new Dictionary<string, JsonPersonSpec>();
		DataHandler.dictShipSpecs = new Dictionary<string, JsonShipSpec>();
		DataHandler.dictTraitScores = new Dictionary<string, int[]>();
		DataHandler.dictRoomSpec = new Dictionary<string, RoomSpec>();
		DataHandler.dictStrings = new Dictionary<string, string>();
		DataHandler.dictSlotEffects = new Dictionary<string, JsonSlotEffects>();
		DataHandler.dictSlots = new Dictionary<string, JsonSlot>();
		DataHandler.dictTickers = new Dictionary<string, JsonTicker>();
		DataHandler.dictCondRules = new Dictionary<string, CondRule>();
		DataHandler.dictMaterials = new Dictionary<string, Material>();
		DataHandler.dictAudioEmitters = new Dictionary<string, JsonAudioEmitter>(512);
		DataHandler.dictCrewSkins = new Dictionary<string, string>();
		DataHandler.dictAds = new Dictionary<string, JsonAd>();
		DataHandler.dictHeadlines = new Dictionary<string, JsonHeadline>();
		DataHandler.dictMusicTags = new Dictionary<string, List<string>>();
		DataHandler.dictMusic = new Dictionary<string, JsonMusic>();
		DataHandler.dictComputerEntries = new Dictionary<string, JsonComputerEntry>();
		DataHandler.dictCOOverlays = new Dictionary<string, JsonCOOverlay>(2048);
		DataHandler.dictDataCOs = new Dictionary<string, DataCO>();
		DataHandler.dictLedgerDefs = new Dictionary<string, JsonLedgerDef>();
		DataHandler.dictPledges = new Dictionary<string, JsonPledge>();
		DataHandler.dictJobitems = new Dictionary<string, JsonJobItems>();
		DataHandler.dictJobs = new Dictionary<string, JsonJob>();
		DataHandler.dictSettings = new Dictionary<string, JsonUserSettings>();
		DataHandler.dictModList = new Dictionary<string, JsonModList>();
		DataHandler.dictModInfos = new Dictionary<string, JsonModInfo>();
		DataHandler.aModPaths = new List<string>();
		DataHandler.dictInstallables2 = new Dictionary<string, JsonInstallable>(2048);
		DataHandler.dictAIPersonalities = new Dictionary<string, JsonAIPersonality>();
		DataHandler.dictTransit = new Dictionary<string, JsonTransit>();
		DataHandler.dictPlotManager = new Dictionary<string, JsonPlotManagerSettings>();
		DataHandler.dictStarSystems = new Dictionary<string, JsonStarSystemSave>();
		DataHandler.dictParallax = new Dictionary<string, JsonParallax>();
		DataHandler.dictContext = new Dictionary<string, JsonContext>();
		DataHandler.dictChargeProfiles = new Dictionary<string, JsonChargeProfile>();
		DataHandler.dictWounds = new Dictionary<string, JsonWound>();
		DataHandler.dictAModes = new Dictionary<string, JsonAttackMode>();
		DataHandler.dictPDAAppIcons = new Dictionary<string, JsonPDAAppIcon>();
		DataHandler.dictZoneTriggers = new Dictionary<string, JsonZoneTrigger>();
		DataHandler.dictTips = new Dictionary<string, JsonTip>();
		DataHandler.dictCrimes = new Dictionary<string, JsonCrime>();
		DataHandler.dictPlots = new Dictionary<string, JsonPlot>();
		DataHandler.dictPlotBeats = new Dictionary<string, JsonPlotBeat>();
		DataHandler.dictRaceTracks = new Dictionary<string, JsonRaceTrack>();
		DataHandler.dictRacingLeagues = new Dictionary<string, JsonRacingLeague>();
		DataHandler.dictInfoNodes = new Dictionary<string, JsonInfoNode>();
		DataHandler.dictInstallables = new Dictionary<string, JsonInstallable>(2048);
		DataHandler.dictIAOverrides = new Dictionary<string, JsonInteractionOverride>();
		DataHandler.dictPlotBeatOverrides = new Dictionary<string, JsonPlotBeatOverride>();
		DataHandler.dictJsonVerbs = new Dictionary<string, JsonVerbs>();
		DataHandler.dictVerbs = new Dictionary<string, string[]>();
		DataHandler.dictJsonTokens = new Dictionary<string, JsonCustomTokens>();
		DataHandler.listCustomTokens = new List<string>();
		DataHandler.dictSupersTemp = new Dictionary<string, JsonDCOCollection>();
		DataHandler.dictRoomSpecsTemp = new Dictionary<string, JsonRoomSpec>();
		DataHandler.dictSimple = new Dictionary<string, JsonSimple>();
		DataHandler.dictGUIPropMapUnparsed = new Dictionary<string, JsonGUIPropMap>();
		DataHandler.mapCOs = new Dictionary<string, CondOwner>();
	}

	public static void LoadBuildVersion()
	{
		try
		{
			TextAsset textAsset = (TextAsset)Resources.Load("version", typeof(TextAsset));
			DataHandler.strBuild = "Early Access Build: " + textAsset.text;
			DataHandler.loadLog.Append("#Info# Getting build info.");
			DataHandler.loadLog.AppendLine();
			DataHandler.loadLog.Append(DataHandler.strBuild);
			DataHandler.loadLog.AppendLine();
		}
		catch (Exception ex)
		{
			Debug.LogError(string.Concat(new string[]
			{
				DataHandler.loadLog.ToString(),
				"\n",
				ex.Message,
				"\n",
				ex.StackTrace.ToString()
			}));
		}
	}

	private static void BuildMarketDCOCollection(Dictionary<string, JsonDCOCollection> jCollections)
	{
		if (DataHandler.dictDataCoCollections == null)
		{
			DataHandler.dictDataCoCollections = new Dictionary<string, DataCoCollection>();
		}
		foreach (KeyValuePair<string, JsonCOOverlay> keyValuePair in DataHandler.dictCOOverlays)
		{
			DataHandler.BuildDataCO(keyValuePair.Key, keyValuePair.Value, null);
		}
		foreach (KeyValuePair<string, JsonCondOwner> keyValuePair2 in DataHandler.dictCOs)
		{
			DataHandler.BuildDataCO(keyValuePair2.Key, null, keyValuePair2.Value);
		}
		foreach (KeyValuePair<string, JsonDCOCollection> keyValuePair3 in jCollections)
		{
			DataCoCollection value = new DataCoCollection(keyValuePair3.Value);
			DataHandler.dictDataCoCollections.TryAdd(keyValuePair3.Key, value);
		}
		DataHandler.dictSupersTemp.Clear();
	}

	public static DataCoCollection GetDataCoCollection(string name)
	{
		DataCoCollection dataCoCollection;
		return (string.IsNullOrEmpty(name) || !DataHandler.dictDataCoCollections.TryGetValue(name, out dataCoCollection)) ? null : dataCoCollection;
	}

	public static DataCoCollection GetDataCoCollectionForCO(string coName)
	{
		foreach (KeyValuePair<string, DataCoCollection> keyValuePair in DataHandler.dictDataCoCollections)
		{
			if (keyValuePair.Value.IsPartOfCollection(coName))
			{
				return keyValuePair.Value;
			}
		}
		return new DataCoCollection(new JsonDCOCollection
		{
			strName = "NoCollection"
		});
	}

	public static void ResetUserSettings()
	{
		DataHandler.dictSettings["UserSettings"] = new JsonUserSettings();
		DataHandler.dictSettings["UserSettings"].Init();
	}

	public static void SaveUserSettings()
	{
		DataHandler.DataToJsonStreaming<JsonUserSettings>(DataHandler.dictSettings, "/settings.json", true, string.Empty);
	}

	public static JsonUserSettings GetUserSettings()
	{
		if (!DataHandler.dictSettings.ContainsKey("UserSettings") || DataHandler.dictSettings["UserSettings"] == null)
		{
			Debug.LogError("ERROR: UserSettings not found.");
			return null;
		}
		return DataHandler.dictSettings["UserSettings"];
	}

	// Loads one mod's `data/` tree into the shared registries.
	// Data flow: enumerate each known JSON folder -> schedule parsing into the
	// matching dictionary -> queue per-mod post-processing for simplified files,
	// GUI prop maps, strings, names, music, and related derived structures.
	private static void LoadMod(string strFolderPath, string[] aIgnorePatterns, JsonModInfo jmi)
	{
		ModLoader modLoader = new ModLoader
		{
			JsonModInfo = jmi
		};
		LoadManager.LoadingQueue.Add(modLoader);
		LoadManager.LastScheduledMod = modLoader;
		if (!Directory.Exists(strFolderPath + "data/"))
		{
			Debug.LogError("ERROR: Mod folder not found: " + strFolderPath + "data/");
			jmi.Status = GUIModRow.ModStatus.Missing;
			return;
		}
		bool flag = ConsoleToGUI.instance != null;
		int num = 0;
		if (flag)
		{
			num = ConsoleToGUI.instance.ErrorCount;
			ConsoleToGUI.instance.LogInfo("Begin loading data from: " + strFolderPath);
		}
		DataHandler.aModPaths.Insert(0, strFolderPath);
		strFolderPath += "data/";
		DataHandler.LoadModJsons<JsonShip>(strFolderPath + "ships/", DataHandler.dictShips, aIgnorePatterns);
		DataHandler.LoadModJsons<JsonAd>(strFolderPath + "ads/", DataHandler.dictAds, aIgnorePatterns);
		DataHandler.LoadModJsons<JsonAIPersonality>(strFolderPath + "ai_training/", DataHandler.dictAIPersonalities, aIgnorePatterns);
		DataHandler.LoadModJsons<JsonAttackMode>(strFolderPath + "attackmodes/", DataHandler.dictAModes, aIgnorePatterns);
		DataHandler.LoadModJsons<JsonAudioEmitter>(strFolderPath + "audioemitters/", DataHandler.dictAudioEmitters, aIgnorePatterns);
		DataHandler.LoadModJsons<JsonCareer>(strFolderPath + "careers/", DataHandler.dictCareers, aIgnorePatterns);
		DataHandler.LoadModJsons<JsonChargeProfile>(strFolderPath + "chargeprofiles/", DataHandler.dictChargeProfiles, aIgnorePatterns);
		DataHandler.LoadModJsons<JsonColor>(strFolderPath + "colors/", DataHandler.dictJsonColors, aIgnorePatterns);
		DataHandler.LoadModJsons<JsonCond>(strFolderPath + "conditions/", DataHandler.dictConds, aIgnorePatterns);
		Dictionary<string, JsonSimple> condsSimple = new Dictionary<string, JsonSimple>();
		DataHandler.LoadModJsons<JsonSimple>(strFolderPath + "conditions_simple/", condsSimple, aIgnorePatterns);
		DataHandler.LoadModJsons<JsonCondOwner>(strFolderPath + "condowners/", DataHandler.dictCOs, aIgnorePatterns);
		DataHandler.LoadModJsons<CondRule>(strFolderPath + "condrules/", DataHandler.dictCondRules, aIgnorePatterns);
		DataHandler.LoadModJsons<CondTrigger>(strFolderPath + "condtrigs/", DataHandler.dictCTs, aIgnorePatterns);
		DataHandler.LoadModJsons<JsonContext>(strFolderPath + "context/", DataHandler.dictContext, aIgnorePatterns);
		DataHandler.LoadModJsons<JsonCOOverlay>(strFolderPath + "cooverlays/", DataHandler.dictCOOverlays, aIgnorePatterns);
		Dictionary<string, JsonSimple> dictSimpleCrewSkins = new Dictionary<string, JsonSimple>();
		DataHandler.LoadModJsons<JsonSimple>(strFolderPath + "crewskins/", dictSimpleCrewSkins, aIgnorePatterns);
		DataHandler.LoadModJsons<JsonCrime>(strFolderPath + "crime/", DataHandler.dictCrimes, aIgnorePatterns);
		DataHandler.LoadModJsons<JsonGasRespire>(strFolderPath + "gasrespires/", DataHandler.dictGasRespires, aIgnorePatterns);
		DataHandler.LoadModJsons<JsonGUIPropMap>(strFolderPath + "guipropmaps/", DataHandler.dictGUIPropMapUnparsed, aIgnorePatterns);
		DataHandler.LoadModJsons<JsonHeadline>(strFolderPath + "headlines/", DataHandler.dictHeadlines, aIgnorePatterns);
		DataHandler.LoadModJsons<JsonHomeworld>(strFolderPath + "homeworlds/", DataHandler.dictHomeworlds, aIgnorePatterns);
		DataHandler.LoadModJsons<JsonInfoNode>(strFolderPath + "info/", DataHandler.dictInfoNodes, aIgnorePatterns);
		DataHandler.LoadModJsons<JsonInstallable>(strFolderPath + "installables/", DataHandler.dictInstallables, aIgnorePatterns);
		DataHandler.LoadModJsons<JsonInteractionOverride>(strFolderPath + "interaction_overrides/", DataHandler.dictIAOverrides, aIgnorePatterns);
		DataHandler.LoadModJsons<JsonInteraction>(strFolderPath + "interactions/", DataHandler.dictInteractions, aIgnorePatterns);
		DataHandler.LoadModJsons<JsonItemDef>(strFolderPath + "items/", DataHandler.dictItemDefs, aIgnorePatterns);
		DataHandler.LoadModJsons<JsonJobItems>(strFolderPath + "jobitems/", DataHandler.dictJobitems, aIgnorePatterns);
		DataHandler.LoadModJsons<JsonJob>(strFolderPath + "jobs/", DataHandler.dictJobs, aIgnorePatterns);
		DataHandler.LoadModJsons<JsonLedgerDef>(strFolderPath + "ledgerdefs/", DataHandler.dictLedgerDefs, aIgnorePatterns);
		DataHandler.LoadModJsons<JsonLifeEvent>(strFolderPath + "lifeevents/", DataHandler.dictLifeEvents, aIgnorePatterns);
		DataHandler.LoadModJsons<JsonLight>(strFolderPath + "lights/", DataHandler.dictLights, aIgnorePatterns);
		DataHandler.LoadModJsons<Loot>(strFolderPath + "loot/", DataHandler.dictLoot, aIgnorePatterns);
		Dictionary<string, JsonSimple> dictSimpleManPages = new Dictionary<string, JsonSimple>();
		DataHandler.LoadModJsons<JsonSimple>(strFolderPath + "manpages/", dictSimpleManPages, aIgnorePatterns);
		DataHandler.LoadModJsons<JsonMarketActorConfig>(strFolderPath + "market/Markets/", DataHandler.dictMarketConfigs, aIgnorePatterns);
		DataHandler.LoadModJsons<JsonDCOCollection>(strFolderPath + "market/CoCollections/", DataHandler.dictSupersTemp, aIgnorePatterns);
		DataHandler.LoadModJsons<JsonProductionMap>(strFolderPath + "market/Production/", DataHandler.dictProductionMaps, aIgnorePatterns);
		DataHandler.LoadModJsons<JsonCargoSpec>(strFolderPath + "market/CargoSpecs/", DataHandler.dictCargoSpecs, aIgnorePatterns);
		DataHandler.LoadModJsons<JsonMusic>(strFolderPath + "music/", DataHandler.dictMusic, aIgnorePatterns);
		Dictionary<string, JsonSimple> dictFirst = new Dictionary<string, JsonSimple>();
		DataHandler.LoadModJsons<JsonSimple>(strFolderPath + "names_first/", dictFirst, aIgnorePatterns);
		Dictionary<string, JsonSimple> dictFull = new Dictionary<string, JsonSimple>();
		DataHandler.LoadModJsons<JsonSimple>(strFolderPath + "names_full/", dictFull, aIgnorePatterns);
		Dictionary<string, JsonSimple> dictLast = new Dictionary<string, JsonSimple>();
		DataHandler.LoadModJsons<JsonSimple>(strFolderPath + "names_last/", dictLast, aIgnorePatterns);
		Dictionary<string, JsonSimple> dictRobots = new Dictionary<string, JsonSimple>();
		DataHandler.LoadModJsons<JsonSimple>(strFolderPath + "names_robots/", dictRobots, aIgnorePatterns);
		Dictionary<string, JsonSimple> dictShipNames = new Dictionary<string, JsonSimple>();
		DataHandler.LoadModJsons<JsonSimple>(strFolderPath + "names_ship/", dictShipNames, aIgnorePatterns);
		Dictionary<string, JsonSimple> dictShipAdjectives = new Dictionary<string, JsonSimple>();
		DataHandler.LoadModJsons<JsonSimple>(strFolderPath + "names_ship_adjectives/", dictShipAdjectives, aIgnorePatterns);
		Dictionary<string, JsonSimple> dictShipNouns = new Dictionary<string, JsonSimple>();
		DataHandler.LoadModJsons<JsonSimple>(strFolderPath + "names_ship_nouns/", dictShipNouns, aIgnorePatterns);
		DataHandler.LoadModJsons<JsonParallax>(strFolderPath + "parallax/", DataHandler.dictParallax, aIgnorePatterns);
		DataHandler.LoadModJsons<JsonPDAAppIcon>(strFolderPath + "pda_apps/", DataHandler.dictPDAAppIcons, aIgnorePatterns);
		DataHandler.LoadModJsons<JsonPersonSpec>(strFolderPath + "personspecs/", DataHandler.dictPersonSpecs, aIgnorePatterns);
		DataHandler.LoadModJsons<JsonPledge>(strFolderPath + "pledges/", DataHandler.dictPledges, aIgnorePatterns);
		DataHandler.LoadModJsons<JsonPlotBeatOverride>(strFolderPath + "plot_beat_overrides/", DataHandler.dictPlotBeatOverrides, aIgnorePatterns);
		DataHandler.LoadModJsons<JsonPlotBeat>(strFolderPath + "plot_beats/", DataHandler.dictPlotBeats, aIgnorePatterns);
		DataHandler.LoadModJsons<JsonPlotManagerSettings>(strFolderPath + "plot_manager/", DataHandler.dictPlotManager, aIgnorePatterns);
		DataHandler.LoadModJsons<JsonPlot>(strFolderPath + "plots/", DataHandler.dictPlots, aIgnorePatterns);
		DataHandler.LoadModJsons<JsonPowerInfo>(strFolderPath + "powerinfos/", DataHandler.dictPowerInfo, aIgnorePatterns);
		DataHandler.LoadModJsons<JsonRacingLeague>(strFolderPath + "racing/leagues/", DataHandler.dictRacingLeagues, aIgnorePatterns);
		DataHandler.LoadModJsons<JsonRaceTrack>(strFolderPath + "racing/tracks/", DataHandler.dictRaceTracks, aIgnorePatterns);
		DataHandler.LoadModJsons<JsonRoomSpec>(strFolderPath + "rooms/", DataHandler.dictRoomSpecsTemp, aIgnorePatterns);
		DataHandler.LoadModJsons<JsonShipSpec>(strFolderPath + "shipspecs/", DataHandler.dictShipSpecs, aIgnorePatterns);
		DataHandler.LoadModJsons<JsonSlotEffects>(strFolderPath + "slot_effects/", DataHandler.dictSlotEffects, aIgnorePatterns);
		DataHandler.LoadModJsons<JsonSlot>(strFolderPath + "slots/", DataHandler.dictSlots, aIgnorePatterns);
		DataHandler.LoadModJsons<JsonStarSystemSave>(strFolderPath + "star_systems/", DataHandler.dictStarSystems, aIgnorePatterns);
		Dictionary<string, JsonSimple> dictStringsTemp = new Dictionary<string, JsonSimple>();
		DataHandler.LoadModJsons<JsonSimple>(strFolderPath + "strings/", dictStringsTemp, aIgnorePatterns);
		DataHandler.LoadModJsons<JsonTicker>(strFolderPath + "tickers/", DataHandler.dictTickers, aIgnorePatterns);
		DataHandler.LoadModJsons<JsonTip>(strFolderPath + "tips/", DataHandler.dictTips, aIgnorePatterns);
		DataHandler.LoadModJsons<JsonCustomTokens>(strFolderPath + "tokens/", DataHandler.dictJsonTokens, aIgnorePatterns);
		Dictionary<string, JsonSimple> dictTraitsTemp = new Dictionary<string, JsonSimple>();
		DataHandler.LoadModJsons<JsonSimple>(strFolderPath + "traitscores/", dictTraitsTemp, aIgnorePatterns);
		DataHandler.LoadModJsons<JsonTransit>(strFolderPath + "transit/", DataHandler.dictTransit, aIgnorePatterns);
		DataHandler.LoadModJsons<JsonVerbs>(strFolderPath + "verbs/", DataHandler.dictJsonVerbs, aIgnorePatterns);
		DataHandler.LoadModJsons<JsonWound>(strFolderPath + "wounds/", DataHandler.dictWounds, aIgnorePatterns);
		DataHandler.LoadModJsons<JsonZoneTrigger>(strFolderPath + "zone_triggers/", DataHandler.dictZoneTriggers, aIgnorePatterns);
		modLoader.PerModPostLoadAsyncOkay.Add(delegate
		{
			DataHandler.ParseGUIPropMaps();
		});
		modLoader.PerModPostLoadAsyncOkay.Add(delegate
		{
			DataHandler.ParseConditionsSimple(condsSimple);
		});
		modLoader.PerModPostLoadAsyncOkay.Add(delegate
		{
			DataHandler.ParseTraitScores(dictTraitsTemp);
		});
		modLoader.PerModPostLoadAsyncOkay.Add(delegate
		{
			DataHandler.ParseSimpleIntoStringDict(dictStringsTemp, DataHandler.dictStrings);
		});
		modLoader.PerModPostLoadAsyncOkay.Add(delegate
		{
			DataHandler.ParseSimpleIntoStringDict(dictSimpleCrewSkins, DataHandler.dictCrewSkins);
		});
		modLoader.PerModPostLoadAsyncOkay.Add(delegate
		{
			DataHandler.ParseSimpleIntoStringDict(dictFirst, DataHandler.dictNamesFirst);
		});
		modLoader.PerModPostLoadAsyncOkay.Add(delegate
		{
			DataHandler.ParseSimpleIntoStringDict(dictFull, DataHandler.dictNamesFull);
		});
		modLoader.PerModPostLoadAsyncOkay.Add(delegate
		{
			DataHandler.ParseSimpleIntoStringDict(dictLast, DataHandler.dictNamesLast);
		});
		modLoader.PerModPostLoadAsyncOkay.Add(delegate
		{
			DataHandler.ParseSimpleIntoStringDict(dictRobots, DataHandler.dictNamesRobots);
		});
		modLoader.PerModPostLoadAsyncOkay.Add(delegate
		{
			DataHandler.ParseSimpleIntoStringDict(dictShipNames, DataHandler.dictNamesShip);
		});
		modLoader.PerModPostLoadAsyncOkay.Add(delegate
		{
			DataHandler.ParseSimpleIntoStringDict(dictShipNouns, DataHandler.dictNamesShipNouns);
		});
		modLoader.PerModPostLoadAsyncOkay.Add(delegate
		{
			DataHandler.ParseSimpleIntoStringDict(dictShipAdjectives, DataHandler.dictNamesShipAdjectives);
		});
		modLoader.PerModPostLoadAsyncOkay.Add(delegate
		{
			DataHandler.ParseManPages(dictSimpleManPages);
		});
		modLoader.PerModPostLoadAsyncOkay.Add(delegate
		{
			DataHandler.ParseMusic();
		});
		if (jmi.Status == GUIModRow.ModStatus.Missing)
		{
			jmi.Status = GUIModRow.ModStatus.Missing;
		}
		else if (ConsoleToGUI.instance && num < ConsoleToGUI.instance.ErrorCount)
		{
			jmi.Status = GUIModRow.ModStatus.Error;
		}
		else
		{
			jmi.Status = GUIModRow.ModStatus.Loaded;
		}
	}

	// Schedules every `.json` file in one data subfolder for parsing.
	// Later files can overwrite existing ids because JsonToData keys by `strName`.
	public static void LoadModJsons<TJson>(string strFolderPath, Dictionary<string, TJson> dict, string[] aIgnorePatterns)
	{
		if (Directory.Exists(strFolderPath))
		{
			string[] files = Directory.GetFiles(strFolderPath, "*.json", SearchOption.AllDirectories);
			string[] array = files;
			for (int i = 0; i < array.Length; i++)
			{
				string strIn = array[i];
				string strFileTemp = DataHandler.PathSanitize(strIn);
				bool flag = false;
				if (aIgnorePatterns != null)
				{
					foreach (string value in aIgnorePatterns)
					{
						if (strFileTemp.IndexOf(value) >= 0)
						{
							flag = true;
							break;
						}
					}
				}
				if (flag)
				{
					Debug.LogWarning("Ignore Pattern match: " + strFileTemp + "; Skipping...");
				}
				else
				{
					FileLoader fileLoader;
					if (typeof(TJson) == typeof(JsonShip))
					{
						fileLoader = LoadManager.LastScheduledMod.AddShip(delegate
						{
							DataHandler.JsonToData<TJson>(strFileTemp, dict);
						});
					}
					else
					{
						fileLoader = LoadManager.LastScheduledMod.AddDelegate(delegate
						{
							DataHandler.JsonToData<TJson>(strFileTemp, dict);
						});
					}
					fileLoader.fileName = strFileTemp;
				}
			}
		}
	}

	// Final main-thread work after all files are parsed for the current session.
	// This builds derived registries that depend on multiple source folders.
	public static void PostModLoadMainThread()
	{
		DataHandler.BuildMarketDCOCollection(DataHandler.dictSupersTemp);
		DataHandler.ParseRoomSpecs(DataHandler.dictRoomSpecsTemp);
		foreach (KeyValuePair<string, JsonInstallable> keyValuePair in DataHandler.dictInstallables)
		{
			Installables.Create(keyValuePair.Value);
		}
		DataHandler.GenerateSkillChatter();
	}

	// Async-safe global post-processing once all mods have populated the raw registries.
	// This applies overrides, finalizes CondTriggers, and prepares derived text/token data.
	public static void AllPostLoadAsync()
	{
		foreach (KeyValuePair<string, JsonInteractionOverride> keyValuePair in DataHandler.dictIAOverrides)
		{
			keyValuePair.Value.Generate();
		}
		foreach (KeyValuePair<string, JsonPlotBeatOverride> keyValuePair2 in DataHandler.dictPlotBeatOverrides)
		{
			keyValuePair2.Value.Generate();
		}
		foreach (CondTrigger condTrigger in DataHandler.dictCTs.Values)
		{
			condTrigger.PostInit();
		}
		DataHandler.dictSocialStats = new Dictionary<string, SocialStats>();
		foreach (JsonInteraction jsonInteraction in DataHandler.dictInteractions.Values)
		{
			if (jsonInteraction.bSocial)
			{
				DataHandler.dictSocialStats[jsonInteraction.strName] = new SocialStats(jsonInteraction.strName);
			}
		}
		DataHandler.UnpackTokens();
		DataHandler.UnpackVerbs();
		DataHandler.PrepareReplacements();
		DataHandler.PrepareConditionDescriptions();
		DataHandler.PrepareInteractionInflections();
		JsonRaceTrack.GenerateWaypointIds(DataHandler.dictRaceTracks);
	}

	private static void GenerateSkillChatter()
	{
		JsonInteraction jsonInteraction = null;
		JsonInteraction jsonInteraction2 = null;
		CondTrigger condTrigger = null;
		DataHandler.dictInteractions.TryGetValue("SOCAskCareer", out jsonInteraction2);
		DataHandler.dictInteractions.TryGetValue("SOCTellSkill_TEMP", out jsonInteraction);
		DataHandler.dictCTs.TryGetValue("TIsSOCTalkSkillTEMPUs", out condTrigger);
		if (jsonInteraction != null && condTrigger != null && jsonInteraction2 != null)
		{
			List<string> list = new List<string>();
			List<string> lootNames = DataHandler.GetLoot("CONDSocialGUIFilterSkills").GetLootNames(null, false, null);
			foreach (string text in lootNames)
			{
				JsonInteraction jsonInteraction3 = jsonInteraction.Clone();
				CondTrigger condTrigger2 = condTrigger.Clone();
				global::Condition cond = DataHandler.GetCond(text);
				condTrigger2.strName = "TIsSOCTalk" + text + "Us";
				condTrigger2.aReqs = new string[]
				{
					text
				};
				jsonInteraction3.strName = "SOCTell" + text;
				jsonInteraction3.strTitle = cond.strNameFriendly;
				jsonInteraction3.strDesc = cond.strDesc;
				jsonInteraction3.CTTestUs = condTrigger2.strName;
				DataHandler.dictInteractions[jsonInteraction3.strName] = jsonInteraction3;
				DataHandler.dictCTs[condTrigger2.strName] = condTrigger2;
				list.Add(jsonInteraction3.strName);
			}
			foreach (string item in jsonInteraction2.aInverse)
			{
				list.Add(item);
			}
			jsonInteraction2.aInverse = list.ToArray();
		}
	}

	public static void ApplyOverride(object objOrig, string[] aValues)
	{
		if (objOrig == null || aValues == null || aValues.Length == 0)
		{
			return;
		}
		Type type = objOrig.GetType();
		foreach (string text in aValues)
		{
			if (!string.IsNullOrEmpty(text))
			{
				string[] array = text.Split(new char[]
				{
					'|'
				});
				if (array.Length == 2)
				{
					string message = text;
					PropertyInfo property = type.GetProperty(array[0]);
					try
					{
						if (array[1] == "null")
						{
							property.SetValue(objOrig, null, null);
						}
						else if (property.PropertyType == typeof(string[]))
						{
							property.SetValue(objOrig, Convert.ChangeType(new string[]
							{
								array[1]
							}, property.PropertyType), null);
						}
						else
						{
							property.SetValue(objOrig, Convert.ChangeType(array[1], property.PropertyType), null);
						}
					}
					catch (Exception ex)
					{
						Debug.Log(message);
						Debug.Log(ex.Message);
					}
				}
			}
		}
	}

	public static string PathSanitize(string strIn)
	{
		if (strIn == null)
		{
			return null;
		}
		strIn = strIn.Replace("\\", "/");
		strIn = strIn.Replace("//", "/");
		return strIn;
	}

	public static void ScheduleJsonLoad<TJson>(object o, string strFile, Dictionary<string, TJson> outputDictionary)
	{
		string json = File.ReadAllText(strFile, Encoding.UTF8);
		TJson[] array = JsonMapper.ToObject<TJson[]>(json);
	}

	// Core JSON array parser used by most data folders.
	// Assumes each JSON object exposes a `strName` property, which becomes the
	// registry key and the override point when mods define the same id later.
	public static void JsonToData<TJson>(string strFile, Dictionary<string, TJson> dict)
	{
		StringBuilder stringBuilder = new StringBuilder(70);
		stringBuilder.Length = 0;
		try
		{
			string json = File.ReadAllText(strFile, Encoding.UTF8);
			stringBuilder.AppendLine("Converting json into Array...");
			TJson[] array = JsonMapper.ToObject<TJson[]>(json);
			foreach (TJson tjson in array)
			{
				stringBuilder.Append("Getting key: ");
				Type type = tjson.GetType();
				PropertyInfo property = type.GetProperty("strName");
				if (property == null)
				{
					JsonLogger.ReportProblem("strName is missing", ReportTypes.FailingString);
				}
				object value = property.GetValue(tjson, null);
				string text = value.ToString();
				stringBuilder.AppendLine(text);
				object obj = DataHandler.dictWriteLock;
				lock (obj)
				{
					if (!dict.TryAdd(text, tjson))
					{
						dict[text] = tjson;
					}
				}
			}
		}
		catch (Exception ex)
		{
			object obj2 = new object();
			object obj3 = obj2;
			lock (obj3)
			{
				LoadManager.JsonLogErrorExceptions.Add(delegate
				{
					JsonLogger.ReportProblem(strFile, ReportTypes.SourceInfo);
				});
			}
			string text2;
			if (stringBuilder.Length > 1000)
			{
				text2 = stringBuilder.ToString(stringBuilder.Length - 1000, 1000);
			}
			else
			{
				text2 = stringBuilder.ToString();
			}
			Debug.LogError(string.Concat(new string[]
			{
				text2,
				"\n",
				ex.Message,
				"\n",
				ex.StackTrace.ToString()
			}));
		}
		if (strFile.IndexOf("osSGv1") >= 0)
		{
			Debug.Log(stringBuilder);
		}
	}

	// Byte-array variant used when loading from packaged saves or archives instead of loose files.
	public static void JsonToData<TJson>(string strFile, Dictionary<string, TJson> dict, Dictionary<string, byte[]> dictFiles)
	{
		Debug.Log("#Info# Loading json: " + strFile + " from byte array.");
		StringBuilder stringBuilder = new StringBuilder(70);
		try
		{
			byte[] bytes = dictFiles[strFile];
			string @string = Encoding.UTF8.GetString(bytes);
			stringBuilder.AppendLine("Converting json into Array...");
			TJson[] array = JsonMapper.ToObject<TJson[]>(@string);
			foreach (TJson tjson in array)
			{
				stringBuilder.Append("Getting key: ");
				Type type = tjson.GetType();
				PropertyInfo property = type.GetProperty("strName");
				if (property == null)
				{
					JsonLogger.ReportProblem("strName is missing", ReportTypes.FailingString);
				}
				object value = property.GetValue(tjson, null);
				string text = value.ToString();
				stringBuilder.AppendLine(text);
				if (dict.ContainsKey(text))
				{
					Debug.Log("Warning: Trying to add " + text + " twice.");
					dict[text] = tjson;
				}
				else
				{
					dict.Add(text, tjson);
				}
			}
		}
		catch (Exception ex)
		{
			JsonLogger.ReportProblem(strFile, ReportTypes.SourceInfo);
			string text2;
			if (stringBuilder.Length > 1000)
			{
				text2 = stringBuilder.ToString(stringBuilder.Length - 1000, 1000);
			}
			else
			{
				text2 = stringBuilder.ToString();
			}
			Debug.LogError(string.Concat(new string[]
			{
				text2,
				"\n",
				ex.Message,
				"\n",
				ex.StackTrace.ToString()
			}));
		}
		if (strFile.IndexOf("osSGv1") >= 0)
		{
			Debug.Log(stringBuilder);
		}
	}

	// Loads very simple comma-delimited text assets into a flat string list.
	// Likely used by older lightweight data packs where JSON would be overkill.
	public static void TxtToData(string strFile, List<string> aListOut)
	{
		Debug.Log("Loading text: " + strFile);
		string text = string.Empty;
		try
		{
			string text2 = File.ReadAllText(strFile);
			text += "Converting txt file into Array...\n";
			text2 = Regex.Replace(text2, "\\r\\n?|\\n", string.Empty);
			string[] array = text2.Split(new char[]
			{
				','
			});
			foreach (string item in array)
			{
				aListOut.Add(item);
			}
		}
		catch (Exception ex)
		{
			Debug.Log(string.Concat(new string[]
			{
				text,
				"\n",
				ex.Message,
				"\n",
				ex.StackTrace.ToString()
			}));
		}
	}

	public static string CreateJsonFromData<TJson>(Dictionary<string, TJson> dictData)
	{
		StringBuilder stringBuilder = new StringBuilder();
		JsonWriter jsonWriter = new JsonWriter(stringBuilder);
		jsonWriter.PrettyPrint = true;
		jsonWriter.IndentValue = 2;
		List<TJson> list = new List<TJson>();
		foreach (KeyValuePair<string, TJson> keyValuePair in dictData)
		{
			list.Add(keyValuePair.Value);
		}
		JsonMapper.ToJson(list, jsonWriter);
		string result = stringBuilder.ToString();
		list.Clear();
		list = null;
		stringBuilder = null;
		jsonWriter = null;
		return result;
	}

	// Serializes a registry dictionary back to JSON on disk.
	// Likely used by tooling/debug export rather than normal gameplay saves,
	// since it writes array-style registry data back under `data/` or persistent storage.
	public static Exception DataToJsonStreaming<TJson>(Dictionary<string, TJson> dictData, string strFilename, bool bPersistent, string persistenPath = "")
	{
		strFilename = DataHandler.ReplaceInvalidCharacters(strFilename, false);
		string text = DataHandler.strAssetPath + "data/" + strFilename;
		if (bPersistent)
		{
			strFilename = strFilename.TrimStart(new char[]
			{
				Path.DirectorySeparatorChar
			});
			strFilename = strFilename.TrimStart(new char[]
			{
				Path.AltDirectorySeparatorChar
			});
			if (!string.IsNullOrEmpty(persistenPath))
			{
				text = Path.Combine(persistenPath, strFilename);
			}
			else
			{
				text = Path.Combine(Application.persistentDataPath, strFilename);
			}
		}
		if (DataHandler.IllegalFileString(text))
		{
			return new IOException("Illegal path or file");
		}
		string directoryName = Path.GetDirectoryName(text);
		try
		{
			Directory.CreateDirectory(directoryName);
			using (StreamWriter streamWriter = new StreamWriter(text))
			{
				JsonWriter jsonWriter = new JsonWriter(streamWriter);
				jsonWriter.PrettyPrint = true;
				jsonWriter.IndentValue = 2;
				List<TJson> list = new List<TJson>();
				foreach (KeyValuePair<string, TJson> keyValuePair in dictData)
				{
					list.Add(keyValuePair.Value);
				}
				JsonMapper.ToJson(list, jsonWriter);
			}
		}
		catch (IOException result)
		{
			return result;
		}
		return null;
	}

	// Writes raw text into the StreamingAssets `data/` tree.
	// This is likely a developer/modding helper, not standard savegame output.
	public static void WriteFile(string strFilename, string strContents)
	{
		string text = DataHandler.strAssetPath + "data/" + strFilename;
		if (DataHandler.IllegalFileString(text))
		{
			return;
		}
		string directoryName = Path.GetDirectoryName(text);
		Directory.CreateDirectory(directoryName);
		File.WriteAllText(text, strContents);
	}

	// Writes raw text to persistent storage after path sanitization.
	public static void WriteFilePersistent(string strFilename, string strContents)
	{
		strFilename = DataHandler.ReplaceInvalidCharacters(strFilename, false);
		strFilename = strFilename.TrimStart(new char[]
		{
			Path.DirectorySeparatorChar
		});
		strFilename = strFilename.TrimStart(new char[]
		{
			Path.AltDirectorySeparatorChar
		});
		string text = Path.Combine(Application.persistentDataPath, strFilename);
		if (DataHandler.IllegalFileString(text))
		{
			return;
		}
		string directoryName = Path.GetDirectoryName(text);
		Directory.CreateDirectory(directoryName);
		File.WriteAllText(text, strContents);
	}

	public static void RemoveFile(string strFullPathFileName)
	{
		if (string.IsNullOrEmpty(strFullPathFileName))
		{
			return;
		}
		try
		{
			if (File.Exists(strFullPathFileName))
			{
				File.Delete(strFullPathFileName);
			}
		}
		catch (Exception ex)
		{
			Debug.LogWarning(string.Concat(new object[]
			{
				"Could not remove file ",
				strFullPathFileName,
				" Error: ",
				ex
			}));
		}
	}

	public static void CopyFile(string strFullPathFileName, string destinationFolder, string newFileName, bool overwrite = false)
	{
		if (string.IsNullOrEmpty(strFullPathFileName) || string.IsNullOrEmpty(destinationFolder))
		{
			return;
		}
		try
		{
			if (File.Exists(strFullPathFileName))
			{
				if (!Directory.Exists(destinationFolder))
				{
					Directory.CreateDirectory(destinationFolder);
				}
				File.Copy(strFullPathFileName, destinationFolder + newFileName, overwrite);
			}
		}
		catch (Exception ex)
		{
			Debug.LogWarning(string.Concat(new object[]
			{
				"Could not copy file ",
				strFullPathFileName,
				" Error: ",
				ex
			}));
		}
	}

	private static string ReplaceInvalidCharacters(string name, bool revert = false)
	{
		Dictionary<char, char> dictionary = new Dictionary<char, char>
		{
			{
				'|',
				'%'
			}
		};
		foreach (KeyValuePair<char, char> keyValuePair in dictionary)
		{
			char c = (!revert) ? keyValuePair.Key : keyValuePair.Value;
			char newChar = (!revert) ? keyValuePair.Value : keyValuePair.Key;
			if (name.Contains(c))
			{
				name = name.Replace(c, newChar);
			}
		}
		return name;
	}

	public static bool IllegalFileString(string strPathAndFile)
	{
		if (strPathAndFile.Contains("../") || strPathAndFile.Contains("..\\"))
		{
			Debug.LogError("ERROR: Cannot write file " + strPathAndFile + "\nFilename contains illegal characters.");
			return true;
		}
		return false;
	}

	public static void AddPNG(string strFileName, Texture2D bmp)
	{
		if (strFileName == null || bmp == null)
		{
			return;
		}
		DataHandler.dictImages[strFileName] = bmp;
		bmp.name = strFileName;
	}

	public static Texture2D LoadPNG(string strFileName, bool bNorm, bool alwaysLoadFreshInstance = false)
	{
		Texture2D texture2D = null;
		if (string.IsNullOrEmpty(strFileName))
		{
			strFileName = "null";
		}
		foreach (string str in DataHandler.aModPaths)
		{
			string path = str + "images/" + strFileName;
			if (!alwaysLoadFreshInstance && DataHandler.dictImages.TryGetValue(strFileName, out texture2D) && texture2D != null)
			{
				return texture2D;
			}
			if (File.Exists(path))
			{
				byte[] data = File.ReadAllBytes(path);
				texture2D = new Texture2D(2, 2, TextureFormat.ARGB32, false);
				texture2D.filterMode = FilterMode.Point;
				texture2D.wrapMode = TextureWrapMode.Clamp;
				texture2D.LoadImage(data);
				if (bNorm)
				{
					texture2D = ShaderSetup.NormalPNGtoDXTnm(texture2D);
				}
				DataHandler.dictImages[strFileName] = texture2D;
				texture2D.name = strFileName;
				return texture2D;
			}
		}
		if (!DataHandler.bSuppressGetErrors)
		{
			Debug.Log("Unable to load PNG: " + strFileName);
		}
		texture2D = (Resources.Load("Sprites/missing") as Texture2D);
		DataHandler.dictImages[strFileName] = texture2D;
		texture2D.name = "missing.png";
		return texture2D;
	}

	public static Dictionary<string, Texture2D> LoadPNGFolder(string directoryPath, bool bNorm)
	{
		Dictionary<string, Texture2D> dictionary = new Dictionary<string, Texture2D>();
		try
		{
			foreach (string str in DataHandler.aModPaths)
			{
				string path = str + "images/" + directoryPath;
				if (Directory.Exists(path))
				{
					List<string> list = Directory.GetFiles(path, "*.png", SearchOption.AllDirectories).ToList<string>();
					foreach (string path2 in list)
					{
						string fileName = Path.GetFileName(path2);
						dictionary.Add(Path.GetFileNameWithoutExtension(path2), DataHandler.LoadPNG(directoryPath + fileName, bNorm, false));
					}
				}
			}
		}
		catch (Exception ex)
		{
			Debug.LogWarning("Could not load png folder " + ex.Message);
		}
		return dictionary;
	}

	public static GameObject[] LoadOBJ(string strFileName)
	{
		string text = DataHandler.strAssetPath + "mesh/" + strFileName;
		if (!File.Exists(text))
		{
			Debug.Log("Unable to load mesh: " + text);
			return null;
		}
		return ObjReader.use.ConvertFile(text, true);
	}

	// Looks up a save path and logs if the file is missing.
	public static string SaveFileExists(string strFileName)
	{
		string text = strFileName;
		if (!File.Exists(strFileName))
		{
			if (!File.Exists(text))
			{
				Debug.Log("Error: Unable to find save file: " + text);
				return null;
			}
		}
		else
		{
			text = strFileName;
		}
		return text;
	}

	// Loads a loose save file into the top-level JsonGameSave payload.
	// JsonGameSave is still parsed through the generic `strName`-keyed loader, then
	// this returns the first payload entry from that array-style file.
	public static JsonGameSave LoadSaveFile(string strFileName)
	{
		string text = DataHandler.SaveFileExists(strFileName);
		if (text == null)
		{
			return null;
		}
		Dictionary<string, JsonGameSave> dictionary = new Dictionary<string, JsonGameSave>();
		DataHandler.JsonToData<JsonGameSave>(text, dictionary);
		using (Dictionary<string, JsonGameSave>.ValueCollection.Enumerator enumerator = dictionary.Values.GetEnumerator())
		{
			if (enumerator.MoveNext())
			{
				return enumerator.Current;
			}
		}
		Debug.Log("Error: Unable to parse save file: " + text);
		return null;
	}

	// Archive/byte-array variant for packaged saves.
	public static JsonGameSave LoadSaveFile(string strFileName, Dictionary<string, byte[]> dictFiles)
	{
		if (dictFiles == null)
		{
			return DataHandler.LoadSaveFile(strFileName);
		}
		if (string.IsNullOrEmpty(strFileName) || dictFiles.Count == 0)
		{
			Debug.Log("Error: Invalid save file: " + strFileName);
			return null;
		}
		Dictionary<string, JsonGameSave> dictionary = new Dictionary<string, JsonGameSave>();
		DataHandler.JsonToData<JsonGameSave>(strFileName, dictionary, dictFiles);
		using (Dictionary<string, JsonGameSave>.ValueCollection.Enumerator enumerator = dictionary.Values.GetEnumerator())
		{
			if (enumerator.MoveNext())
			{
				return enumerator.Current;
			}
		}
		Debug.Log("Error: Unable to parse save file: " + strFileName);
		return null;
	}

	// Sanitizes player-facing names into something safe for save filenames.
	public static string ConvertStringToFileSafe(string strIn)
	{
		if (strIn == null)
		{
			return null;
		}
		foreach (char oldChar in Path.GetInvalidFileNameChars())
		{
			strIn = strIn.Replace(oldChar, '-');
		}
		if (strIn.Length > 0 && strIn[strIn.Length - 1] == '.')
		{
			strIn = strIn.Replace(strIn[strIn.Length - 1], '-');
		}
		return strIn;
	}

	public static void CreateSimpleConditionFromString(string str)
	{
		JsonCond jsonCond = DataHandler.dictConds["Simple"];
		JsonCond jsonCond2 = new JsonCond();
		jsonCond2.strName = str;
		jsonCond2.strNameFriendly = str;
		jsonCond2.strDesc = str;
		jsonCond2.strColor = "Neutral";
		jsonCond2.nDisplaySelf = 2;
		jsonCond2.nDisplayOther = 2;
		DataHandler.dictConds.TryAdd(str, jsonCond2);
	}

	// Expands a `JsonSimple` table into lightweight condition definitions.
	// Likely used by a compact `conditions_simple` data source that reuses the
	// base `Simple` condition template and overrides display text/colors.
	private static void ParseConditionsSimple(Dictionary<string, JsonSimple> dictSimple)
	{
		foreach (KeyValuePair<string, JsonSimple> keyValuePair in dictSimple)
		{
			for (int i = 0; i < keyValuePair.Value.aValues.Length - 1; i += 7)
			{
				JsonCond jsonCond = DataHandler.dictConds["Simple"];
				JsonCond jsonCond2 = new JsonCond();
				jsonCond2.strName = keyValuePair.Value.aValues[i];
				jsonCond2.strNameFriendly = keyValuePair.Value.aValues[i + 1];
				jsonCond2.strDesc = keyValuePair.Value.aValues[i + 2];
				jsonCond2.aNext = jsonCond.aNext;
				jsonCond2.strColor = keyValuePair.Value.aValues[i + 5];
				jsonCond2.bResetTimer = jsonCond.bResetTimer;
				int num = 0;
				if (int.TryParse(keyValuePair.Value.aValues[i + 3], out num))
				{
					jsonCond2.nDisplaySelf = num;
				}
				num = 0;
				if (int.TryParse(keyValuePair.Value.aValues[i + 4], out num))
				{
					jsonCond2.nDisplayOther = num;
				}
				bool bInvert = false;
				if (bool.TryParse(keyValuePair.Value.aValues[i + 6], out bInvert))
				{
					jsonCond2.bInvert = bInvert;
				}
				jsonCond2.bFatal = jsonCond.bFatal;
				jsonCond2.bRemoveAll = jsonCond.bRemoveAll;
				jsonCond2.aNext = jsonCond.aNext;
				jsonCond2.fDuration = jsonCond.fDuration;
				DataHandler.dictConds[jsonCond2.strName] = jsonCond2;
			}
		}
	}

	// Unpacks a flat alternating key/value array from `JsonSimple` into a runtime dictionary.
	// This pattern is used by several small data tables such as strings or simple lookup maps.
	private static void ParseSimpleIntoStringDict(Dictionary<string, JsonSimple> dictSimple, Dictionary<string, string> dict)
	{
		if (dict == null)
		{
			Debug.LogError("ERROR: Trying to parse JsonSimple dictionary into null dictionary. Aborting.");
			return;
		}
		if (dictSimple == null)
		{
			Debug.LogError("ERROR: Trying to parse null dictionary into <string, string> dictionary. Aborting.");
			return;
		}
		using (Dictionary<string, JsonSimple>.Enumerator enumerator = dictSimple.GetEnumerator())
		{
			if (enumerator.MoveNext())
			{
				KeyValuePair<string, JsonSimple> keyValuePair = enumerator.Current;
				dict = DataHandler.ConvertStringArrayToDict(keyValuePair.Value.aValues, dict);
			}
		}
	}

	// Loads manual/help page entries where one `JsonSimple` record contains the page text array.
	private static void ParseManPages(Dictionary<string, JsonSimple> dict)
	{
		using (Dictionary<string, JsonSimple>.Enumerator enumerator = dict.GetEnumerator())
		{
			if (enumerator.MoveNext())
			{
				KeyValuePair<string, JsonSimple> keyValuePair = enumerator.Current;
				DataHandler.dictManPages[keyValuePair.Value.strName] = keyValuePair.Value.aValues;
			}
		}
		DataHandler.dictSimple.Clear();
	}

	// Converts simple `condition,min,max` rows into score ranges keyed by condition id.
	// Likely used by character generation, AI evaluation, or trait summary systems.
	private static void ParseTraitScores(Dictionary<string, JsonSimple> dict)
	{
		using (Dictionary<string, JsonSimple>.Enumerator enumerator = dict.GetEnumerator())
		{
			if (enumerator.MoveNext())
			{
				KeyValuePair<string, JsonSimple> keyValuePair = enumerator.Current;
				foreach (string text in keyValuePair.Value.aValues)
				{
					string[] array = text.Split(new char[]
					{
						','
					});
					if (!DataHandler.dictConds.ContainsKey(array[0]))
					{
						Debug.Log("Warning: Trait score " + array[0] + " refers to non-existent condition.");
					}
					else
					{
						DataHandler.dictTraitScores[array[0]] = new int[]
						{
							Convert.ToInt32(array[1]),
							Convert.ToInt32(array[2])
						};
					}
				}
			}
		}
	}

	// Finalizes GUI prop maps after JSON load by converting flat arrays into dictionaries.
	private static void ParseGUIPropMaps()
	{
		foreach (KeyValuePair<string, JsonGUIPropMap> keyValuePair in DataHandler.dictGUIPropMapUnparsed)
		{
			DataHandler.dictGUIPropMaps[keyValuePair.Key] = DataHandler.ConvertStringArrayToDict(keyValuePair.Value.dictGUIPropMap, null);
		}
	}

	// Builds reverse tag lookups so music can be queried by mood/context tag.
	public static void ParseMusic()
	{
		foreach (KeyValuePair<string, JsonMusic> keyValuePair in DataHandler.dictMusic)
		{
			string[] strTags = keyValuePair.Value.strTags;
			if (strTags != null && strTags.Length != 0)
			{
				foreach (string text in strTags)
				{
					if (text != null)
					{
						if (!DataHandler.dictMusicTags.ContainsKey(text))
						{
							DataHandler.dictMusicTags[text] = new List<string>();
						}
						DataHandler.dictMusicTags[text].Add(keyValuePair.Key);
					}
				}
			}
		}
	}

	// Converts temporary JSON room spec records into runtime RoomSpec instances.
	// These likely describe room templates loaded from `data/rooms`.
	private static void ParseRoomSpecs(Dictionary<string, JsonRoomSpec> jsonRoomSpecs)
	{
		foreach (KeyValuePair<string, JsonRoomSpec> keyValuePair in jsonRoomSpecs)
		{
			RoomSpec roomSpec = new RoomSpec(keyValuePair.Value);
			DataHandler.dictRoomSpec.Add(roomSpec.strName, roomSpec);
		}
		DataHandler.dictRoomSpecsTemp.Clear();
	}

	// Generic helper for the decompiler's flat string-array dictionary format:
	// `[key0, value0, key1, value1, ...]`.
	public static Dictionary<string, string> ConvertStringArrayToDict(string[] aStrings, Dictionary<string, string> dict = null)
	{
		if (dict == null)
		{
			dict = new Dictionary<string, string>();
		}
		if (aStrings != null)
		{
			for (int i = 0; i < aStrings.Length - 1; i += 2)
			{
				if (aStrings.Length <= i + 1)
				{
					dict[aStrings[i]] = string.Empty;
					break;
				}
				dict[aStrings[i]] = aStrings[i + 1];
			}
		}
		return dict;
	}

	// Double-valued variant of the flat string-array dictionary parser.
	public static Dictionary<string, double> ConvertStringArrayToDictDouble(string[] aStrings, Dictionary<string, double> dict = null)
	{
		if (dict == null)
		{
			dict = new Dictionary<string, double>();
		}
		if (aStrings != null)
		{
			for (int i = 0; i < aStrings.Length - 1; i += 2)
			{
				if (aStrings.Length <= i + 1)
				{
					dict[aStrings[i]] = 0.0;
					break;
				}
				double value = 0.0;
				double.TryParse(aStrings[i + 1], out value);
				dict[aStrings[i]] = value;
			}
		}
		return dict;
	}

	public static string[] ConvertDictToStringArray(Dictionary<string, string> dict)
	{
		List<string> list = new List<string>();
		if (dict != null)
		{
			foreach (string text in dict.Keys)
			{
				list.Add(text);
				list.Add(dict[text]);
			}
		}
		return list.ToArray();
	}

	public static string[] ConvertDictToStringArray(Dictionary<string, double> dict)
	{
		List<string> list = new List<string>();
		if (dict != null)
		{
			foreach (string text in dict.Keys)
			{
				list.Add(text);
				list.Add(dict[text].ToString());
			}
		}
		return list.ToArray();
	}

	public static void GetFullName(string strGender, out string strFirstName, out string strLastName)
	{
		if (DataHandler.dictNamesFull.Count > 0 && UnityEngine.Random.Range(0f, 1f) < DataHandler.fChanceFullname)
		{
			List<string> list = new List<string>(DataHandler.dictNamesFull.Keys);
			string text = list[UnityEngine.Random.Range(0, list.Count)];
			int num = 50;
			int num2 = 0;
			while (strGender != "IsNB" && DataHandler.dictNamesFull[text] != strGender)
			{
				text = list[UnityEngine.Random.Range(0, list.Count)];
				num2++;
				if (num2 >= num)
				{
					break;
				}
			}
			string[] array = text.Split(new char[]
			{
				'|'
			});
			strFirstName = array[0];
			strLastName = array[1];
			DataHandler.dictNamesFull.Remove(text);
			return;
		}
		strFirstName = DataHandler.GetName(true, strGender);
		strLastName = DataHandler.GetName(false, strGender);
	}

	public static string GetName(bool bFirst, string strGender)
	{
		string text = "NoName";
		if (strGender == "Robot")
		{
			text = DataHandler.GetRandomStringFrom(DataHandler.dictNamesRobots.Keys);
		}
		else if (bFirst)
		{
			if (DataHandler.dictNamesFirst.Count > 0)
			{
				text = string.Empty;
				int num = 1;
				int i = 0;
				if (UnityEngine.Random.Range(0, 100) < 15)
				{
					num++;
				}
				List<string> list = new List<string>(DataHandler.dictNamesFirst.Keys);
				string text2 = string.Empty;
				int num2 = 50;
				int num3 = 0;
				while (i < num)
				{
					text2 = list[UnityEngine.Random.Range(0, list.Count)];
					if (DataHandler.NameMatchesGender(text2, strGender))
					{
						if (text.Length > 0)
						{
							text += " ";
						}
						text += text2;
						i++;
					}
					num3++;
					if (num3 >= num2)
					{
						break;
					}
				}
			}
		}
		else
		{
			text = DataHandler.GetRandomStringFrom(DataHandler.dictNamesLast.Keys);
		}
		if (text == null)
		{
			text = string.Empty;
		}
		TextInfo textInfo = new CultureInfo("en-US", false).TextInfo;
		return textInfo.ToTitleCase(text.ToLower());
	}

	public static string GetRandomStringFrom(IEnumerable<string> aSource)
	{
		if (aSource == null)
		{
			return null;
		}
		List<string> list = aSource.ToList<string>();
		if (list.Count <= 0)
		{
			return null;
		}
		int index = UnityEngine.Random.Range(0, list.Count - 1);
		return list[index];
	}

	private static bool NameMatchesGender(string strName, string strGender)
	{
		return DataHandler.dictNamesFirst.ContainsKey(strName) && (strGender == "IsNB" || DataHandler.dictNamesFirst[strName] == strGender);
	}

	public static string EmbellishName(string strName, bool bFirst, string strGender)
	{
		float num = MathUtils.Rand(0f, 1f, MathUtils.RandType.Flat, null);
		if (bFirst)
		{
			if (num <= 0.5f)
			{
				strName = strName + " " + DataHandler.GetName(bFirst, strGender);
			}
			else if (num <= 1f)
			{
				strName = DataHandler.GetInitials(strName, ". ");
			}
		}
		else if (num <= 0.4f)
		{
			strName = DataHandler.GetName(bFirst, strGender) + "-" + strName;
		}
		else if (num <= 0.7f)
		{
			if (strName.IndexOf(DataHandler.GetString("NAME_EMBELLISH_SUFFIX1", false)) < 0)
			{
				strName = strName + " " + DataHandler.GetString("NAME_EMBELLISH_SUFFIX1", false);
			}
		}
		else if (num <= 1f && strName.IndexOf(DataHandler.GetString("NAME_EMBELLISH_SUFFIX2", false)) < 0)
		{
			strName = strName + " " + DataHandler.GetString("NAME_EMBELLISH_SUFFIX2", false);
		}
		return strName;
	}

	public static string GetInitials(string names, string separator)
	{
		Regex regex = new Regex("\\s*([^\\s])[^\\s]*\\s*");
		return regex.Replace(names, "$1" + separator).ToUpper();
	}

	public static string GetShipName()
	{
		float num = UnityEngine.Random.Range(0f, 1f);
		string text;
		if (num > 0.6f)
		{
			text = DataHandler.GetRandomStringFrom(DataHandler.dictNamesShip.Keys);
		}
		else
		{
			text = DataHandler.GetRandomStringFrom(DataHandler.dictNamesShipAdjectives.Keys);
			text = text + " " + DataHandler.GetRandomStringFrom(DataHandler.dictNamesShipNouns.Keys);
		}
		return text;
	}

	public static string GetColorHTML(string strName)
	{
		string text = null;
		if (!DataHandler.dictHTMLColors.TryGetValue(strName, out text))
		{
			text = ColorUtility.ToHtmlStringRGB(DataHandler.GetColor(strName));
			DataHandler.dictHTMLColors[strName] = text;
		}
		return text;
	}

	public static Color GetColor(string strName)
	{
		if (DataHandler.dictColors.ContainsKey(strName))
		{
			return DataHandler.dictColors[strName];
		}
		if (!DataHandler.dictJsonColors.ContainsKey(strName))
		{
			Debug.Log("Color not found: " + strName);
			return Color.magenta;
		}
		DataHandler.dictColors[strName] = DataHandler.dictJsonColors[strName].GetColor();
		return DataHandler.dictColors[strName];
	}

	public static string GetString(string strName, bool allowEmpty = false)
	{
		if (string.IsNullOrEmpty(strName))
		{
			return (!allowEmpty) ? "UNKNOWN_STRING" : string.Empty;
		}
		if (!DataHandler.dictStrings.ContainsKey(strName))
		{
			Debug.Log("Unable to load string: " + strName);
			return (!allowEmpty) ? "UNKNOWN_STRING" : string.Empty;
		}
		return DataHandler.dictStrings[strName];
	}

	public static Loot GetLoot(string strName)
	{
		if (strName == null)
		{
			return new Loot();
		}
		if (!DataHandler.dictLoot.ContainsKey(strName))
		{
			if (strName != string.Empty && strName != null)
			{
				Debug.Log("Unable to load Loot: " + strName);
			}
			return new Loot();
		}
		return DataHandler.dictLoot[strName];
	}

	public static global::Condition GetCond(string strName)
	{
		if (strName == null)
		{
			return null;
		}
		JsonCond jsonCond;
		if (DataHandler.dictConds.TryGetValue(strName, out jsonCond) && jsonCond != null)
		{
			return new global::Condition(DataHandler.dictConds[strName]);
		}
		return null;
	}

	public static JsonLight GetLight(string strName)
	{
		if (strName != null && DataHandler.dictLights.ContainsKey(strName) && DataHandler.dictLights[strName] != null)
		{
			return DataHandler.dictLights[strName];
		}
		return null;
	}

	public static JsonItemDef GetItemDef(string strName)
	{
		if (strName != null && DataHandler.dictItemDefs.ContainsKey(strName))
		{
			return DataHandler.dictItemDefs[strName];
		}
		Debug.Log("Unable to load Item Def: " + strName);
		return null;
	}

	public static Producer GetProductionMap(string name)
	{
		JsonProductionMap jsonProductionMap;
		if (DataHandler.dictProductionMaps.TryGetValue(name, out jsonProductionMap) && jsonProductionMap != null)
		{
			return new Producer(jsonProductionMap);
		}
		return null;
	}

	public static MarketActorConfig GetMarketConfig(string jConfigName)
	{
		if (string.IsNullOrEmpty(jConfigName))
		{
			return null;
		}
		JsonMarketActorConfig jActorConfig;
		if (DataHandler.dictMarketConfigs.TryGetValue(jConfigName, out jActorConfig))
		{
			return new MarketActorConfig(jActorConfig);
		}
		Debug.LogError("MarketConfig not found: " + jConfigName);
		return null;
	}

	public static List<JsonCargoSpec> GetCargoRequirements(string[] names)
	{
		if (names == null || names.Length == 0 || DataHandler.dictCargoSpecs == null)
		{
			return null;
		}
		List<JsonCargoSpec> list = null;
		foreach (string key in names)
		{
			JsonCargoSpec item;
			if (DataHandler.dictCargoSpecs.TryGetValue(key, out item))
			{
				if (list == null)
				{
					list = new List<JsonCargoSpec>();
				}
				list.Add(item);
			}
		}
		return list;
	}

	public static CondOwner GetCondOwner(string strCO)
	{
		return DataHandler.GetCondOwner(strCO, null, null, true, null, null, null, null);
	}

	public static CondOwner GetCondOwner(string strCO, string strIDOld = null)
	{
		return DataHandler.GetCondOwner(strCO, null, null, true, null, null, strIDOld, null);
	}

	// Main CondOwner factory.
	// This resolves either a base condowner id or a cooverlay id, instantiates the
	// correct Unity object type (item, crew, robot, ship), applies save data, adds
	// the synthetic UniqueID condition, and registers the live object in `mapCOs`.
	public static CondOwner GetCondOwner(string strCO, string strName, string strPortraitImg, bool bLoot, string strPrefab = null, JsonCondOwnerSave jcos = null, string strIDOld = null, Transform parent = null)
	{
		if (strCO == null && strName == null)
		{
			Debug.Log("Unable to load null CO.");
			return null;
		}
		if (strName != null && jcos == null)
		{
			if (DataHandler.mapCOs.ContainsKey(strName))
			{
				return DataHandler.mapCOs[strName];
			}
			if (DataHandler.dictCOSaves.ContainsKey(strName))
			{
				return DataHandler.GetCondOwner(DataHandler.dictCOSaves[strName].strCODef, strName, null, false, null, DataHandler.dictCOSaves[strName], null, null);
			}
		}
		if (strCO == null)
		{
			Debug.Log("Unable to load null CO.");
			return null;
		}
		JsonCOOverlay jsonCOOverlay = null;
		if (!DataHandler.dictCOs.ContainsKey(strCO))
		{
			if (!DataHandler.dictCOOverlays.ContainsKey(strCO))
			{
				Debug.Log("Unable to load CO: " + strCO);
				return null;
			}
			jsonCOOverlay = DataHandler.dictCOOverlays[strCO];
			strCO = jsonCOOverlay.strCOBase;
		}
		CondOwner condOwner = null;
		string text = DataHandler.GetNextID();
		if (strName == null)
		{
			strName = strCO + text;
		}
		if (strIDOld != null)
		{
			text = strIDOld;
			if (jcos != null)
			{
				strName = jcos.strCondID;
			}
		}
		else if (jcos != null)
		{
			text = jcos.strID;
			strName = jcos.strCondID;
		}
		JsonCondOwner jsonCondOwner = null;
		if (!DataHandler.dictCOs.TryGetValue(strCO, out jsonCondOwner))
		{
			string text2 = "Unable to load base CO: " + strCO;
			if (jsonCOOverlay != null)
			{
				text2 = text2 + " for COOverlay " + jsonCOOverlay.strName;
			}
			Debug.Log(text2);
			return null;
		}
		if (jsonCondOwner.strType.ToLower() == "item")
		{
			if (strPrefab == null)
			{
				strPrefab = "prefabQuad";
			}
			GameObject gameObject = DataHandler.GetMesh(strPrefab, parent);
			Item item = gameObject.AddComponent<Item>();
			item.SetData(jsonCondOwner.strItemDef, 0f, 0f);
			condOwner = gameObject.AddComponent<CondOwner>();
			condOwner.strID = text;
			condOwner.SetData(jsonCondOwner, bLoot, jcos);
			if (strPortraitImg != null)
			{
				condOwner.strPortraitImg = strPortraitImg;
			}
			condOwner.Item.VisualizeOverlays(false);
		}
		else if (jsonCondOwner.strType.ToLower() == "crew")
		{
			GameObject gameObject = DataHandler.GetMesh("prefabCrew2", null);
			condOwner = gameObject.AddComponent<CondOwner>();
			condOwner.strID = strName;
			condOwner.SetData(jsonCondOwner, bLoot, jcos);
			if (strPortraitImg != null)
			{
				condOwner.strPortraitImg = strPortraitImg;
			}
			condOwner.strName = strName;
			condOwner.strNameFriendly = strName;
		}
		else if (jsonCondOwner.strType.ToLower().Contains("robot"))
		{
			GameObject gameObject = DataHandler.GetMesh("prefab" + jsonCondOwner.strType, null);
			condOwner = gameObject.AddComponent<CondOwner>();
			condOwner.strID = strName;
			condOwner.SetData(jsonCondOwner, bLoot, jcos);
			if (strPortraitImg != null)
			{
				condOwner.strPortraitImg = strPortraitImg;
			}
			condOwner.strName = strName;
			condOwner.strNameFriendly = strName;
		}
		else if (jsonCondOwner.strType.ToLower() == "ship")
		{
			GameObject gameObject = new GameObject(strName);
			if (parent != null)
			{
				gameObject.transform.SetParent(parent);
			}
			condOwner = gameObject.AddComponent<CondOwner>();
			condOwner.SetData(jsonCondOwner, bLoot, jcos);
			condOwner.strID = "CO-" + strName;
			condOwner.strName = "CO-" + strName;
		}
		condOwner.gameObject.name = condOwner.ToString();
		JsonCond jsonIn = DataHandler.dictConds["UniqueID"];
		condOwner.objCondID = new global::Condition(jsonIn);
		condOwner.objCondID.strName = strName;
		if (jcos != null)
		{
			condOwner.objCondID.strName = jcos.strCondID;
		}
		condOwner.objCondID.fCount = 1.0;
		condOwner.mapConds[condOwner.objCondID.strName] = condOwner.objCondID;
		DataHandler.mapCOs[condOwner.strID] = condOwner;
		if (jsonCOOverlay != null)
		{
			COOverlay cooverlay = condOwner.gameObject.AddComponent<COOverlay>();
			cooverlay.Init(jsonCOOverlay.strName);
		}
		DataHandler.debugCOCount++;
		return condOwner;
	}

	// Builds a combined data-view object that merges a base CondOwner with an optional overlay.
	private static void BuildDataCO(string strCO, JsonCOOverlay jcoo, JsonCondOwner jco)
	{
		string key = strCO;
		if (jcoo != null)
		{
			key = jcoo.strName;
			strCO = jcoo.strCOBase;
		}
		if (jco == null && !DataHandler.dictCOs.TryGetValue(strCO, out jco))
		{
			return;
		}
		DataHandler.dictDataCOs[key] = new DataCO(jco, jcoo);
	}

	// Returns a cached merged view for UI/inspection systems that need base CO + overlay data together.
	public static DataCO GetDataCO(string strCO)
	{
		if (string.IsNullOrEmpty(strCO))
		{
			return null;
		}
		DataCO dataCO;
		if (DataHandler.dictDataCOs.TryGetValue(strCO, out dataCO))
		{
			return dataCO;
		}
		JsonCOOverlay jsonCOOverlay;
		if (DataHandler.dictCOOverlays.TryGetValue(strCO, out jsonCOOverlay))
		{
			strCO = jsonCOOverlay.strCOBase;
		}
		JsonCondOwner jco;
		if (!DataHandler.dictCOs.TryGetValue(strCO, out jco))
		{
			return null;
		}
		dataCO = new DataCO(jco, jsonCOOverlay);
		DataHandler.dictDataCOs[strCO] = dataCO;
		return dataCO;
	}

	public static CondOwner GetCOPlaceholder(CondOwner coCursor, CondOwner coTarget, string strInstallIA)
	{
		JsonItemDef jsonItemDef = null;
		if (coCursor == null)
		{
			Debug.Log("Unable to load COPlaceholder: Cursor is null.");
			return null;
		}
		if (coTarget == null || !DataHandler.dictItemDefs.TryGetValue(coCursor.strItemDef, out jsonItemDef))
		{
			Debug.Log(string.Concat(new object[]
			{
				"Unable to load COPlaceholder: ",
				coCursor.strItemDef,
				" with target ",
				coTarget
			}));
			return null;
		}
		CondOwner condOwner = null;
		string nextID = DataHandler.GetNextID();
		JsonCondOwner jid = DataHandler.dictCOs["Placeholder"];
		GameObject mesh = DataHandler.GetMesh("prefabQuadPlaceholder", null);
		JsonCOOverlay jsonCOOverlay = null;
		DataHandler.dictCOOverlays.TryGetValue(coCursor.strName, out jsonCOOverlay);
		condOwner = mesh.AddComponent<CondOwner>();
		condOwner.strID = nextID;
		condOwner.SetData(jid, false, null);
		condOwner.strName = coCursor.strName + "_Placeholder";
		condOwner.strNameFriendly = coCursor.FriendlyName + " Placeholder";
		condOwner.strPortraitImg = jsonItemDef.strImg;
		if (jsonCOOverlay != null)
		{
			condOwner.strPortraitImg = jsonCOOverlay.strPortraitImg;
		}
		foreach (KeyValuePair<string, Vector2> keyValuePair in coCursor.mapPoints)
		{
			condOwner.mapPoints[keyValuePair.Key] = keyValuePair.Value;
		}
		Item item = mesh.AddComponent<Item>();
		item.bPlaceholder = true;
		item.SetData(coCursor.strItemDef, 0f, 0f);
		condOwner.gameObject.name = condOwner.ToString();
		if (jsonCOOverlay != null)
		{
			item.SetAlt(jsonCOOverlay.strImg, jsonCOOverlay.strImgNorm, jsonCOOverlay.strImgDamaged, jsonCOOverlay.strDmgColor, null);
		}
		condOwner.transform.position = coCursor.transform.position;
		item.fLastRotation = coCursor.transform.rotation.eulerAngles.z;
		Placeholder placeholder = condOwner.gameObject.AddComponent<Placeholder>();
		placeholder.Init(coCursor, coTarget, strInstallIA);
		Destructable component = coTarget.GetComponent<Destructable>();
		if (component != null)
		{
			Destructable destructable = condOwner.gameObject.GetComponent<Destructable>();
			if (destructable == null)
			{
				destructable = condOwner.gameObject.AddComponent<Destructable>();
			}
			else
			{
				destructable.ClearChecks();
			}
			destructable.CopyFrom(component);
			foreach (DestCheck destCheck in component.aChecks)
			{
				condOwner.SetCondAmount(destCheck.strDamageCond, coTarget.GetCondAmount(destCheck.strDamageCond), 0.0);
				condOwner.SetCondAmount(destCheck.strDamageCondMax, coTarget.GetCondAmount(destCheck.strDamageCondMax), 0.0);
				if (!condOwner.aDestructableConds.Contains(destCheck.strDamageCond))
				{
					condOwner.aDestructableConds.Add(destCheck.strDamageCond);
				}
			}
		}
		COOverlay component2 = coTarget.GetComponent<COOverlay>();
		if (component2 != null)
		{
			COOverlay cooverlay = condOwner.gameObject.AddComponent<COOverlay>();
			cooverlay.strName = component2.strName;
		}
		JsonCond jsonIn = DataHandler.dictConds["UniqueID"];
		condOwner.objCondID = new global::Condition(jsonIn);
		condOwner.objCondID.strName = condOwner.strName;
		condOwner.mapConds[condOwner.objCondID.strName] = condOwner.objCondID;
		DataHandler.mapCOs[condOwner.strID] = condOwner;
		if (condOwner.Item != null)
		{
			condOwner.Item.VisualizeOverlays(false);
		}
		return condOwner;
	}

	public static Item GetBackground(string strCOName)
	{
		if (strCOName == null)
		{
			Debug.Log("Unable to load null background.");
			return null;
		}
		JsonCondOwner jsonCondOwner = null;
		JsonCOOverlay jsonCOOverlay = null;
		if (DataHandler.dictCOOverlays.TryGetValue(strCOName, out jsonCOOverlay))
		{
			DataHandler.dictCOs.TryGetValue(jsonCOOverlay.strCOBase, out jsonCondOwner);
		}
		if (jsonCondOwner == null && !DataHandler.dictCOs.TryGetValue(strCOName, out jsonCondOwner))
		{
			string text = "Unable to load base CO: " + strCOName;
			if (jsonCOOverlay != null)
			{
				text = text + " for COOverlay " + jsonCOOverlay.strName;
			}
			Debug.Log(text);
			return null;
		}
		JsonItemDef jsonItemDef = null;
		if (!DataHandler.dictItemDefs.TryGetValue(jsonCondOwner.strItemDef, out jsonItemDef))
		{
			Debug.Log("Unable to load background: " + strCOName);
			return null;
		}
		string strType = "prefabQuad";
		GameObject mesh = DataHandler.GetMesh(strType, null);
		Item item = mesh.AddComponent<Item>();
		item.SetData(jsonItemDef.strName, 0f, 0f);
		if (jsonCOOverlay != null)
		{
			item.SetAlt(jsonCOOverlay.strImg, jsonCOOverlay.strImgNorm, jsonCOOverlay.strImgDamaged, jsonCOOverlay.strDmgColor, null);
		}
		mesh.name = strCOName;
		return item;
	}

	public static RoomSpec GetRoomDef(string strName)
	{
		if (string.IsNullOrEmpty(strName))
		{
			return null;
		}
		RoomSpec result = null;
		DataHandler.dictRoomSpec.TryGetValue(strName, out result);
		return result;
	}

	public static CondTrigger GetCondTrigger(string strName)
	{
		CondTrigger condTrigger;
		if (strName != null && DataHandler.dictCTs.TryGetValue(strName, out condTrigger))
		{
			return condTrigger.Clone();
		}
		if (!string.IsNullOrEmpty(strName))
		{
			Debug.Log("No such CT: " + strName);
		}
		if (DataHandler._blankCTBackup == null && DataHandler.dictCTs["Blank"] != null)
		{
			DataHandler._blankCTBackup = DataHandler.dictCTs["Blank"].Clone();
		}
		if (DataHandler.dictCTs["Blank"] == null && DataHandler._blankCTBackup != null)
		{
			DataHandler.dictCTs["Blank"] = DataHandler._blankCTBackup.Clone();
		}
		return DataHandler.dictCTs["Blank"];
	}

	public static JsonCondOwner GetCondOwnerDef(string strName)
	{
		if (strName != null && DataHandler.dictCOs.ContainsKey(strName))
		{
			return DataHandler.dictCOs[strName];
		}
		JsonCOOverlay jsonCOOverlay = null;
		if (strName != null)
		{
			DataHandler.dictCOOverlays.TryGetValue(strName, out jsonCOOverlay);
		}
		if (jsonCOOverlay != null && jsonCOOverlay.strCOBase != null && DataHandler.dictCOs.ContainsKey(jsonCOOverlay.strCOBase))
		{
			return DataHandler.dictCOs[jsonCOOverlay.strCOBase];
		}
		Debug.Log("No such CO: " + strName);
		return null;
	}

	public static JsonLedgerDef GetLedgerDef(string strName)
	{
		if (strName == null || !DataHandler.dictLedgerDefs.ContainsKey(strName))
		{
			Debug.Log("No such LedgerDef: " + strName);
			return null;
		}
		return DataHandler.dictLedgerDefs[strName];
	}

	public static Type GetCommand(string name)
	{
		name = "Ostranauts.Ships.Commands." + name;
		Type type = Type.GetType(name);
		if (type != null && type.IsClass && type.GetInterfaces().Contains(typeof(ICommand)))
		{
			return type;
		}
		return null;
	}

	public static JsonPledge GetPledge(string strName)
	{
		if (strName == null || !DataHandler.dictPledges.ContainsKey(strName))
		{
			Debug.Log("No such Pledge: " + strName);
			return null;
		}
		return DataHandler.dictPledges[strName];
	}

	public static JsonParallax GetParallax(string strName)
	{
		if (strName == null || !DataHandler.dictParallax.ContainsKey(strName))
		{
			Debug.Log("No such Parallax: " + strName);
			return null;
		}
		return DataHandler.dictParallax[strName];
	}

	public static JsonZoneTrigger GetZoneTrigger(string strName)
	{
		if (strName == null || !DataHandler.dictZoneTriggers.ContainsKey(strName))
		{
			if (!string.IsNullOrEmpty(strName))
			{
				Debug.Log("No such zone trigger: " + strName);
			}
			return null;
		}
		return DataHandler.dictZoneTriggers[strName];
	}

	public static JsonChargeProfile GetChargeProfile(string strName)
	{
		if (strName == null || !DataHandler.dictChargeProfiles.ContainsKey(strName))
		{
			Debug.Log("No such Chargeprofile: " + strName);
			return null;
		}
		return DataHandler.dictChargeProfiles[strName];
	}

	public static JsonWound GetWound(string strName)
	{
		if (strName == null || !DataHandler.dictWounds.ContainsKey(strName))
		{
			Debug.Log("No such Wound: " + strName);
			return null;
		}
		return DataHandler.dictWounds[strName];
	}

	public static JsonAttackMode GetAttackMode(string strName)
	{
		if (strName == null || !DataHandler.dictAModes.ContainsKey(strName))
		{
			Debug.Log("No such AttackMode: " + strName);
			return DataHandler.dictAModes["AModeBlank"];
		}
		return DataHandler.dictAModes[strName];
	}

	public static JsonContext GetContext(string strName)
	{
		if (strName == null || !DataHandler.dictContext.ContainsKey(strName))
		{
			Debug.Log("No such Context: " + strName);
			return null;
		}
		return DataHandler.dictContext[strName];
	}

	public static JsonJob GetJob(string strName)
	{
		if (strName == null || !DataHandler.dictJobs.ContainsKey(strName))
		{
			Debug.Log("No such Job: " + strName);
			return null;
		}
		return DataHandler.dictJobs[strName];
	}

	public static JsonTransit GetTransitConnections(string shipOrigin)
	{
		if (DataHandler.dictTransit == null || shipOrigin == null)
		{
			return null;
		}
		if (shipOrigin.Contains("|"))
		{
			shipOrigin = shipOrigin.Substring(0, shipOrigin.IndexOf("|") + 1);
		}
		JsonTransit result = null;
		DataHandler.dictTransit.TryGetValue(shipOrigin, out result);
		return result;
	}

	public static JsonCrime GetCrime(string strName)
	{
		if (strName == null || !DataHandler.dictCrimes.ContainsKey(strName))
		{
			Debug.Log("No such Crime: " + strName);
			return null;
		}
		return DataHandler.dictCrimes[strName];
	}

	public static JsonPlot GetPlot(string strName)
	{
		if (strName == null || !DataHandler.dictPlots.ContainsKey(strName))
		{
			Debug.Log("No such Plot: " + strName);
			return null;
		}
		return DataHandler.dictPlots[strName];
	}

	public static JsonPlotBeat GetPlotBeat(string strName)
	{
		if (strName == null || !DataHandler.dictPlotBeats.ContainsKey(strName))
		{
			Debug.Log("No such PlotBeat: " + strName);
			return null;
		}
		return DataHandler.dictPlotBeats[strName];
	}

	public static JsonRaceTrack GetRaceTrack(string strName)
	{
		if (strName == null || !DataHandler.dictRaceTracks.ContainsKey(strName))
		{
			Debug.Log("No such Racetrack: " + strName);
			return null;
		}
		return DataHandler.dictRaceTracks[strName];
	}

	public static JsonRacingLeague GetRaceLeague(string strName)
	{
		if (strName == null || !DataHandler.dictRacingLeagues.ContainsKey(strName))
		{
			Debug.Log("No such League: " + strName);
			return null;
		}
		return DataHandler.dictRacingLeagues[strName];
	}

	public static JsonJobItems GetJobItems(string strName)
	{
		if (strName == null || !DataHandler.dictJobitems.ContainsKey(strName))
		{
			Debug.Log("No such JobItems: " + strName);
			return null;
		}
		return DataHandler.dictJobitems[strName];
	}

	public static string GetCOShortName(string strCOName)
	{
		if (strCOName == null)
		{
			return null;
		}
		string result = strCOName;
		JsonCOOverlay jsonCOOverlay = null;
		DataHandler.dictCOOverlays.TryGetValue(strCOName, out jsonCOOverlay);
		if (jsonCOOverlay != null && jsonCOOverlay.strNameShort != null)
		{
			result = jsonCOOverlay.strNameShort;
		}
		else
		{
			JsonCondOwner condOwnerDef = DataHandler.GetCondOwnerDef(strCOName);
			if (condOwnerDef != null && condOwnerDef.strNameShort != null)
			{
				result = condOwnerDef.strNameShort;
			}
		}
		return result;
	}

	public static string GetCOFriendlyName(string strCOName)
	{
		if (strCOName == null)
		{
			return null;
		}
		string result = strCOName;
		JsonCOOverlay jsonCOOverlay = null;
		DataHandler.dictCOOverlays.TryGetValue(strCOName, out jsonCOOverlay);
		if (jsonCOOverlay != null && jsonCOOverlay.strNameFriendly != null)
		{
			result = jsonCOOverlay.strNameFriendly;
		}
		else
		{
			JsonCondOwner condOwnerDef = DataHandler.GetCondOwnerDef(strCOName);
			if (condOwnerDef != null && condOwnerDef.strNameFriendly != null)
			{
				result = condOwnerDef.strNameFriendly;
			}
		}
		return result;
	}

	public static string GetCondFriendlyName(string strCondName)
	{
		if (strCondName == null)
		{
			return null;
		}
		string result = strCondName;
		if (DataHandler.dictConds.ContainsKey(strCondName))
		{
			JsonCond jsonCond = DataHandler.dictConds[strCondName];
			if (jsonCond != null && jsonCond.strNameFriendly != null)
			{
				result = jsonCond.strNameFriendly;
			}
		}
		return result;
	}

	public static Interaction GetInteraction(string strName, JsonInteractionSave jis = null, bool getTrackedObject = false)
	{
		if (strName == null || !DataHandler.dictInteractions.ContainsKey(strName))
		{
			if (strName != string.Empty && strName != null)
			{
				Debug.Log("No such Interaction: " + strName);
			}
			return null;
		}
		return (!getTrackedObject || DataHandler._interactionObjectTracker == null) ? new Interaction(DataHandler.dictInteractions[strName], jis) : DataHandler._interactionObjectTracker.GetObject(DataHandler.dictInteractions[strName], jis);
	}

	private static void UnpackTokens()
	{
		foreach (KeyValuePair<string, JsonCustomTokens> keyValuePair in DataHandler.dictJsonTokens)
		{
			DataHandler.listCustomTokens.AddRange(keyValuePair.Value.tokens);
		}
	}

	private static void UnpackVerbs()
	{
		foreach (JsonVerbs jsonVerbs in DataHandler.dictJsonVerbs.Values)
		{
			if (jsonVerbs.verbs != null)
			{
				foreach (string[] array in jsonVerbs.verbs)
				{
					if (!string.IsNullOrEmpty(array[0]))
					{
						string[] array2;
						if (array.Length == 1)
						{
							array2 = new string[]
							{
								array[0],
								array[0].Remove(array[0].Length - 1)
							};
						}
						else
						{
							array2 = array;
						}
						if (!DataHandler.dictVerbs.TryAdd(array2[0], array2))
						{
							DataHandler.dictVerbs[array2[0]] = array2;
						}
					}
				}
			}
		}
	}

	private static void PrepareReplacements()
	{
		if (DataHandler.replacementDict.Count > 0)
		{
			return;
		}
		DataHandler.replacementDict.Add("us", new InflectedTokenData
		{
			replacementType = GrammarUtils.ReplacementType.Name,
			LUTIndex = GrammarUtils.GrammarLUTIndex.Subjective,
			usThem3Rd = GrammarUtils.UsThem3rd.Us
		});
		DataHandler.replacementDict.Add("us-subj", new InflectedTokenData
		{
			replacementType = GrammarUtils.ReplacementType.FromLUT,
			LUTIndex = GrammarUtils.GrammarLUTIndex.Subjective,
			usThem3Rd = GrammarUtils.UsThem3rd.Us
		});
		DataHandler.replacementDict.Add("us-obj", new InflectedTokenData
		{
			replacementType = GrammarUtils.ReplacementType.FromLUT,
			LUTIndex = GrammarUtils.GrammarLUTIndex.Objective,
			usThem3Rd = GrammarUtils.UsThem3rd.Us
		});
		DataHandler.replacementDict.Add("us-pos", new InflectedTokenData
		{
			replacementType = GrammarUtils.ReplacementType.FromLUT,
			LUTIndex = GrammarUtils.GrammarLUTIndex.Possessive,
			usThem3Rd = GrammarUtils.UsThem3rd.Us
		});
		DataHandler.replacementDict.Add("us-reflexive", new InflectedTokenData
		{
			replacementType = GrammarUtils.ReplacementType.FromLUT,
			LUTIndex = GrammarUtils.GrammarLUTIndex.Reflexive,
			usThem3Rd = GrammarUtils.UsThem3rd.Us
		});
		DataHandler.replacementDict.Add("us-contractIs", new InflectedTokenData
		{
			replacementType = GrammarUtils.ReplacementType.FromLUT,
			usThem3Rd = GrammarUtils.UsThem3rd.Us,
			LUTIndex = GrammarUtils.GrammarLUTIndex.ContractIs
		});
		DataHandler.replacementDict.Add("us-contractHas", new InflectedTokenData
		{
			replacementType = GrammarUtils.ReplacementType.FromLUT,
			usThem3Rd = GrammarUtils.UsThem3rd.Us,
			LUTIndex = GrammarUtils.GrammarLUTIndex.ContractHas
		});
		DataHandler.replacementDict.Add("us-contractWill", new InflectedTokenData
		{
			replacementType = GrammarUtils.ReplacementType.FromLUT,
			usThem3Rd = GrammarUtils.UsThem3rd.Us,
			LUTIndex = GrammarUtils.GrammarLUTIndex.ContractWill
		});
		DataHandler.replacementDict.Add("us-fullName", new InflectedTokenData
		{
			replacementType = GrammarUtils.ReplacementType.FromLUT,
			usThem3Rd = GrammarUtils.UsThem3rd.Us,
			LUTIndex = GrammarUtils.GrammarLUTIndex.FullName
		});
		DataHandler.replacementDict.Add("them", new InflectedTokenData
		{
			replacementType = GrammarUtils.ReplacementType.Name,
			LUTIndex = GrammarUtils.GrammarLUTIndex.Subjective,
			usThem3Rd = GrammarUtils.UsThem3rd.Them
		});
		DataHandler.replacementDict.Add("them-subj", new InflectedTokenData
		{
			replacementType = GrammarUtils.ReplacementType.FromLUT,
			LUTIndex = GrammarUtils.GrammarLUTIndex.Subjective,
			usThem3Rd = GrammarUtils.UsThem3rd.Them
		});
		DataHandler.replacementDict.Add("them-obj", new InflectedTokenData
		{
			replacementType = GrammarUtils.ReplacementType.FromLUT,
			LUTIndex = GrammarUtils.GrammarLUTIndex.Objective,
			usThem3Rd = GrammarUtils.UsThem3rd.Them
		});
		DataHandler.replacementDict.Add("them-pos", new InflectedTokenData
		{
			replacementType = GrammarUtils.ReplacementType.FromLUT,
			LUTIndex = GrammarUtils.GrammarLUTIndex.Possessive,
			usThem3Rd = GrammarUtils.UsThem3rd.Them
		});
		DataHandler.replacementDict.Add("them-reflexive", new InflectedTokenData
		{
			replacementType = GrammarUtils.ReplacementType.FromLUT,
			LUTIndex = GrammarUtils.GrammarLUTIndex.Reflexive,
			usThem3Rd = GrammarUtils.UsThem3rd.Them
		});
		DataHandler.replacementDict.Add("them-contractIs", new InflectedTokenData
		{
			replacementType = GrammarUtils.ReplacementType.FromLUT,
			usThem3Rd = GrammarUtils.UsThem3rd.Them,
			LUTIndex = GrammarUtils.GrammarLUTIndex.ContractIs
		});
		DataHandler.replacementDict.Add("them-contractHas", new InflectedTokenData
		{
			replacementType = GrammarUtils.ReplacementType.FromLUT,
			usThem3Rd = GrammarUtils.UsThem3rd.Them,
			LUTIndex = GrammarUtils.GrammarLUTIndex.ContractHas
		});
		DataHandler.replacementDict.Add("them-contractWill", new InflectedTokenData
		{
			replacementType = GrammarUtils.ReplacementType.FromLUT,
			usThem3Rd = GrammarUtils.UsThem3rd.Them,
			LUTIndex = GrammarUtils.GrammarLUTIndex.ContractWill
		});
		DataHandler.replacementDict.Add("them-fullName", new InflectedTokenData
		{
			replacementType = GrammarUtils.ReplacementType.FromLUT,
			usThem3Rd = GrammarUtils.UsThem3rd.Us,
			LUTIndex = GrammarUtils.GrammarLUTIndex.FullName
		});
		DataHandler.replacementDict.Add("3rd", new InflectedTokenData
		{
			replacementType = GrammarUtils.ReplacementType.Name,
			LUTIndex = GrammarUtils.GrammarLUTIndex.Subjective,
			usThem3Rd = GrammarUtils.UsThem3rd.Third
		});
		DataHandler.replacementDict.Add("3rd-subj", new InflectedTokenData
		{
			replacementType = GrammarUtils.ReplacementType.FromLUT,
			LUTIndex = GrammarUtils.GrammarLUTIndex.Subjective,
			usThem3Rd = GrammarUtils.UsThem3rd.Third
		});
		DataHandler.replacementDict.Add("3rd-obj", new InflectedTokenData
		{
			replacementType = GrammarUtils.ReplacementType.FromLUT,
			LUTIndex = GrammarUtils.GrammarLUTIndex.Objective,
			usThem3Rd = GrammarUtils.UsThem3rd.Third
		});
		DataHandler.replacementDict.Add("3rd-pos", new InflectedTokenData
		{
			replacementType = GrammarUtils.ReplacementType.FromLUT,
			LUTIndex = GrammarUtils.GrammarLUTIndex.Possessive,
			usThem3Rd = GrammarUtils.UsThem3rd.Third
		});
		DataHandler.replacementDict.Add("3rd-reflexive", new InflectedTokenData
		{
			replacementType = GrammarUtils.ReplacementType.FromLUT,
			LUTIndex = GrammarUtils.GrammarLUTIndex.Reflexive,
			usThem3Rd = GrammarUtils.UsThem3rd.Third
		});
		DataHandler.replacementDict.Add("3rd-contractIs", new InflectedTokenData
		{
			replacementType = GrammarUtils.ReplacementType.FromLUT,
			usThem3Rd = GrammarUtils.UsThem3rd.Third,
			LUTIndex = GrammarUtils.GrammarLUTIndex.ContractIs
		});
		DataHandler.replacementDict.Add("3rd-contractHas", new InflectedTokenData
		{
			replacementType = GrammarUtils.ReplacementType.FromLUT,
			usThem3Rd = GrammarUtils.UsThem3rd.Third,
			LUTIndex = GrammarUtils.GrammarLUTIndex.ContractHas
		});
		DataHandler.replacementDict.Add("3rd-contractWill", new InflectedTokenData
		{
			replacementType = GrammarUtils.ReplacementType.FromLUT,
			usThem3Rd = GrammarUtils.UsThem3rd.Third,
			LUTIndex = GrammarUtils.GrammarLUTIndex.ContractWill
		});
		DataHandler.replacementDict.Add("3rd-fullName", new InflectedTokenData
		{
			replacementType = GrammarUtils.ReplacementType.FromLUT,
			usThem3Rd = GrammarUtils.UsThem3rd.Us,
			LUTIndex = GrammarUtils.GrammarLUTIndex.FullName
		});
	}

	private static void PrepareConditionDescriptions()
	{
		List<string> list = new List<string>();
		foreach (KeyValuePair<string, JsonCond> keyValuePair in DataHandler.dictConds)
		{
			if (keyValuePair.Value.strDesc != null)
			{
				GrammarUtils.UsThem3rd usThem3Rd = GrammarUtils.UsThem3rd.None;
				int num = keyValuePair.Value.strDesc.IndexOf('[');
				int num2 = keyValuePair.Value.strDesc.IndexOf(']');
				int num3 = 0;
				while (num != -1 && num2 != -1)
				{
					num3++;
					if (num3 > 100)
					{
						Debug.Log("There's a fucked up bracket in " + keyValuePair.Key);
						break;
					}
					string text = keyValuePair.Value.strDesc.Substring(num + 1, num2 - num - 1);
					bool flag = DataHandler.listCustomTokens.Contains(text);
					bool flag2 = false;
					if (!flag)
					{
						flag2 = DataHandler.dictVerbs.ContainsKey(text);
					}
					if (!flag2 && !flag)
					{
						if (!list.Contains(text))
						{
							list.Add(text);
						}
						num = keyValuePair.Value.strDesc.IndexOf('[', num2);
						num2 = keyValuePair.Value.strDesc.IndexOf(']', num2 + 1);
					}
					else
					{
						InflectedTokenData item = new InflectedTokenData
						{
							start = num,
							end = num2,
							usThem3Rd = usThem3Rd
						};
						if (flag2)
						{
							item.replacementType = GrammarUtils.ReplacementType.FromVerbList;
							item.verbForm = GrammarUtils.VerbForm.Singular;
							item.verbForms = DataHandler.dictVerbs[text];
							item.usThem3Rd = usThem3Rd;
						}
						else if (DataHandler.replacementDict.TryGetValue(text, out item))
						{
							item.start = num;
							item.end = num2;
						}
						keyValuePair.Value.replacementValues.Add(item);
						usThem3Rd = item.usThem3Rd;
						num = keyValuePair.Value.strDesc.IndexOf('[', num2);
						num2 = keyValuePair.Value.strDesc.IndexOf(']', num2 + 1);
					}
				}
				if (!GrammarUtils.inflectedStrings.TryAdd(keyValuePair.Value.strDesc, new InflectedString
				{
					tokens = keyValuePair.Value.replacementValues
				}))
				{
				}
			}
		}
	}

	private static void PrepareInteractionInflections()
	{
		foreach (KeyValuePair<string, JsonInteraction> keyValuePair in DataHandler.dictInteractions)
		{
			if (!string.IsNullOrEmpty(keyValuePair.Value.strTooltip))
			{
				DataHandler.PrepareInflectedString(keyValuePair.Value.strTooltip);
			}
			if (!string.IsNullOrEmpty(keyValuePair.Value.strDesc))
			{
				DataHandler.PrepareInflectedString(keyValuePair.Value.strDesc);
			}
		}
	}

	private static void PrepareInflectedString(string s)
	{
		List<string> list = new List<string>();
		GrammarUtils.UsThem3rd usThem3Rd = GrammarUtils.UsThem3rd.None;
		int num = s.IndexOf('[');
		int num2 = s.IndexOf(']');
		int num3 = 0;
		List<InflectedTokenData> list2 = new List<InflectedTokenData>();
		while (num != -1 && num2 != -1)
		{
			num3++;
			if (num3 > 100)
			{
				Debug.Log("There's a fucked up bracket in " + s);
				break;
			}
			string text = s.Substring(num + 1, num2 - num - 1);
			bool flag = DataHandler.listCustomTokens.Contains(text);
			bool flag2 = false;
			if (!flag)
			{
				flag2 = DataHandler.dictVerbs.ContainsKey(text);
			}
			if (!flag2 && !flag)
			{
				if (!text.Contains('-'))
				{
					if (!list.Contains(text))
					{
						Debug.Log(text + " " + s);
						list.Add(text);
					}
					num = s.IndexOf('[', num2);
					num2 = s.IndexOf(']', num2 + 1);
					continue;
				}
				string[] array = text.Split(new char[]
				{
					'-'
				});
				if (DataHandler.dictVerbs.ContainsKey(array[1]))
				{
					flag2 = true;
					text = array[1];
					string text2 = array[0];
					if (text2 != null)
					{
						if (!(text2 == "us"))
						{
							if (!(text2 == "them"))
							{
								if (text2 == "3rd")
								{
									usThem3Rd = GrammarUtils.UsThem3rd.Third;
								}
							}
							else
							{
								usThem3Rd = GrammarUtils.UsThem3rd.Them;
							}
						}
						else
						{
							usThem3Rd = GrammarUtils.UsThem3rd.Us;
						}
					}
				}
			}
			InflectedTokenData item = new InflectedTokenData
			{
				start = num,
				end = num2,
				usThem3Rd = usThem3Rd
			};
			if (flag2)
			{
				item.replacementType = GrammarUtils.ReplacementType.FromVerbList;
				item.verbForm = GrammarUtils.VerbForm.Singular;
				item.verbForms = DataHandler.dictVerbs[text];
				item.usThem3Rd = usThem3Rd;
			}
			else if (DataHandler.replacementDict.ContainsKey(text))
			{
				DataHandler.replacementDict.TryGetValue(text, out item);
				item.start = num;
				item.end = num2;
			}
			else
			{
				item.start = num;
				item.end = num2;
				item.replacementType = GrammarUtils.ReplacementType.Other;
				if (text.Contains("regID"))
				{
					item.replacementOther = GrammarUtils.ReplacementOther.RegID;
				}
				if (text.Contains("shipfriendly"))
				{
					item.replacementOther = GrammarUtils.ReplacementOther.ShipFriendlyName;
				}
				if (text.Contains("captain"))
				{
					item.replacementOther = GrammarUtils.ReplacementOther.Captain;
				}
				if (text.Contains("data"))
				{
					item.replacementOther = GrammarUtils.ReplacementOther.Data;
				}
				if (text.Contains("us"))
				{
					item.usThem3Rd = GrammarUtils.UsThem3rd.Us;
				}
				if (text.Contains("them"))
				{
					item.usThem3Rd = GrammarUtils.UsThem3rd.Them;
				}
				if (text.Contains("3rd"))
				{
					item.usThem3Rd = GrammarUtils.UsThem3rd.Third;
				}
			}
			list2.Add(item);
			usThem3Rd = item.usThem3Rd;
			num = s.IndexOf('[', num2);
			num2 = s.IndexOf(']', num2 + 1);
		}
		if (list2.Count <= 0 || !GrammarUtils.inflectedStrings.TryAdd(s, new InflectedString
		{
			tokens = list2
		}))
		{
		}
	}

	public static void ReleaseTrackedInteraction(Interaction trackedInteraction)
	{
		if (DataHandler._interactionObjectTracker == null)
		{
			return;
		}
		DataHandler._interactionObjectTracker.ReleaseObject(trackedInteraction);
	}

	public static void KeepInteraction(Interaction trackedInteraction)
	{
		if (DataHandler._interactionObjectTracker == null)
		{
			return;
		}
		DataHandler._interactionObjectTracker.UntrackObject(trackedInteraction);
	}

	public static JsonGasRespire GetGasRespire(string strName)
	{
		if (strName == null || !DataHandler.dictGasRespires.ContainsKey(strName))
		{
			Debug.Log("No such GasRespire: " + strName);
			return null;
		}
		return DataHandler.dictGasRespires[strName];
	}

	public static JsonPowerInfo GetPowerInfo(string strName)
	{
		if (strName == null || !DataHandler.dictPowerInfo.ContainsKey(strName))
		{
			Debug.Log("No such PowerInfo: " + strName);
			return null;
		}
		return DataHandler.dictPowerInfo[strName];
	}

	public static Dictionary<string, string> GetGUIPropMap(string strName)
	{
		if (strName == null || !DataHandler.dictGUIPropMaps.ContainsKey(strName))
		{
			Debug.Log("No such GUIPropMap: " + strName);
			return null;
		}
		Dictionary<string, string> dictionary = DataHandler.dictGUIPropMaps[strName];
		Dictionary<string, string> dictionary2 = new Dictionary<string, string>();
		foreach (KeyValuePair<string, string> keyValuePair in dictionary)
		{
			dictionary2[keyValuePair.Key] = keyValuePair.Value;
		}
		return dictionary2;
	}

	public static JsonLifeEvent GetLifeEvent(string strName)
	{
		if (strName == null || !DataHandler.dictLifeEvents.ContainsKey(strName))
		{
			Debug.Log("Unable to load Life Event: " + strName);
			return null;
		}
		return DataHandler.dictLifeEvents[strName];
	}

	public static JsonCareer GetCareer(string strName)
	{
		if (strName == null || !DataHandler.dictCareers.ContainsKey(strName))
		{
			Debug.Log("Unable to load Career: " + strName);
			return null;
		}
		return DataHandler.dictCareers[strName];
	}

	public static JsonHomeworld GetHomeworld(string strName)
	{
		if (strName == null || !DataHandler.dictHomeworlds.ContainsKey(strName))
		{
			Debug.Log("Unable to load Homeworld: " + strName);
			return null;
		}
		return DataHandler.dictHomeworlds[strName];
	}

	public static JsonPersonSpec GetPersonSpec(string strName)
	{
		if (strName == null || !DataHandler.dictPersonSpecs.ContainsKey(strName))
		{
			if (strName != string.Empty && strName != null)
			{
				Debug.Log("Unable to load Person Spec: " + strName);
			}
			return null;
		}
		return DataHandler.dictPersonSpecs[strName];
	}

	public static JsonShipSpec GetShipSpec(string strName)
	{
		if (strName == null || !DataHandler.dictShipSpecs.ContainsKey(strName))
		{
			if (strName != string.Empty && strName != null)
			{
				Debug.Log("Unable to load Ship Spec: " + strName);
			}
			return null;
		}
		return DataHandler.dictShipSpecs[strName];
	}

	public static JsonSlot GetSlot(string strName)
	{
		if (strName == null || !DataHandler.dictSlots.ContainsKey(strName))
		{
			Debug.Log("Unable to load Slot: " + strName);
			return null;
		}
		return DataHandler.dictSlots[strName];
	}

	public static JsonSlotEffects GetSlotEffect(string strName)
	{
		if (strName == null || !DataHandler.dictSlotEffects.ContainsKey(strName))
		{
			Debug.Log("Unable to load Slot_Effect: " + strName);
			return null;
		}
		return DataHandler.dictSlotEffects[strName].Clone();
	}

	public static JsonTicker GetTicker(string strName)
	{
		if (strName == null || !DataHandler.dictTickers.ContainsKey(strName))
		{
			Debug.Log("Unable to load Ticker: " + strName);
			return null;
		}
		JsonTicker jsonTicker = DataHandler.dictTickers[strName].Clone();
		jsonTicker.fEpochStart = StarSystem.fEpoch;
		return jsonTicker;
	}

	public static JsonShip GetShip(string strName)
	{
		if (strName == null || !DataHandler.dictShips.ContainsKey(strName))
		{
			Debug.Log("Unable to load Ship: " + strName);
			return null;
		}
		return DataHandler.dictShips[strName];
	}

	public static JsonShipConstructionTemplate GetShipConstructionTemplate(JsonShip jShip)
	{
		if (string.IsNullOrEmpty(jShip.strTemplateName))
		{
			return new JsonShipConstructionTemplate(jShip, 100);
		}
		JsonShip ship = DataHandler.GetShip(jShip.strTemplateName);
		return ship.GetCurrentConstructionTemplate(jShip.nConstructionProgress);
	}

	public static Dictionary<string, byte[]> GetShipImageByteArrays(string strName)
	{
		if (string.IsNullOrEmpty(strName))
		{
			return null;
		}
		Dictionary<string, Texture2D> dictionary;
		if (!DataHandler.dictShipImages.TryGetValue(strName, out dictionary))
		{
			return null;
		}
		if (dictionary == null)
		{
			return null;
		}
		Dictionary<string, byte[]> dictionary2 = new Dictionary<string, byte[]>();
		foreach (KeyValuePair<string, Texture2D> keyValuePair in dictionary)
		{
			if (!(keyValuePair.Value == null))
			{
				dictionary2.Add(keyValuePair.Key, keyValuePair.Value.EncodeToPNG());
			}
		}
		return dictionary2;
	}

	public static CondRule GetCondRule(string strName)
	{
		if (strName == null || !DataHandler.dictCondRules.ContainsKey(strName))
		{
			Debug.Log("Unable to load CondRule: " + strName);
			return null;
		}
		return DataHandler.dictCondRules[strName].Clone();
	}

	public static JsonAudioEmitter GetAudioEmitter(string strName)
	{
		if (strName == null || !DataHandler.dictAudioEmitters.ContainsKey(strName))
		{
			Debug.Log("Unable to load AudioEmitter: " + strName);
			return null;
		}
		return DataHandler.dictAudioEmitters[strName];
	}

	public static string GetCrewSkin(string strName)
	{
		if (strName == null || !DataHandler.dictCrewSkins.ContainsKey(strName))
		{
			Debug.Log("Unable to load Crew Skin: " + strName);
			return "01";
		}
		return DataHandler.dictCrewSkins[strName];
	}

	public static JsonCOOverlay GetCOOverlay(string strName)
	{
		if (strName == null || !DataHandler.dictCOOverlays.ContainsKey(strName))
		{
			Debug.Log("Unable to load COOverlay: " + strName);
			return null;
		}
		return DataHandler.dictCOOverlays[strName];
	}

	public static JsonAd GetAd()
	{
		int num = DataHandler.dictAds.Keys.Count;
		if (num == 0)
		{
			Debug.Log("No ads found.");
			return new JsonAd
			{
				strName = "blank",
				strDesc = "blank"
			};
		}
		JsonAd[] array = new JsonAd[num];
		DataHandler.dictAds.Values.CopyTo(array, 0);
		num = MathUtils.Rand(0, array.Length, MathUtils.RandType.Flat, null);
		return array[num];
	}

	public static JsonHeadline GetHeadline()
	{
		int num = DataHandler.dictHeadlines.Keys.Count;
		if (num == 0)
		{
			Debug.Log("No ads found.");
			return new JsonHeadline
			{
				strName = "blank",
				strDesc = "blank",
				strRegion = "blank"
			};
		}
		JsonHeadline[] array = new JsonHeadline[num];
		DataHandler.dictHeadlines.Values.CopyTo(array, 0);
		num = MathUtils.Rand(0, array.Length, MathUtils.RandType.Flat, null);
		return array[num];
	}

	public static string GetTrackForTag(string strTag)
	{
		if (strTag == null || !DataHandler.dictMusicTags.ContainsKey(strTag))
		{
			Debug.Log("No such music tag found: " + strTag);
			return null;
		}
		if (DataHandler.dictMusicTags[strTag].Count == 0)
		{
			Debug.Log("No music found for tag: " + strTag);
			return null;
		}
		int index = MathUtils.Rand(0, DataHandler.dictMusicTags[strTag].Count, MathUtils.RandType.Flat, strTag);
		return DataHandler.dictMusicTags[strTag][index];
	}

	public static GameObject GetMesh(string strType, Transform parent = null)
	{
		if (parent == null)
		{
			GameObject gameObject = UnityEngine.Object.Instantiate(Resources.Load(strType)) as GameObject;
			if (gameObject != null)
			{
				return gameObject;
			}
			return UnityEngine.Object.Instantiate(Resources.Load("prefabQuad")) as GameObject;
		}
		else
		{
			GameObject gameObject2 = UnityEngine.Object.Instantiate(Resources.Load(strType), parent) as GameObject;
			if (gameObject2 != null)
			{
				return gameObject2;
			}
			return UnityEngine.Object.Instantiate(Resources.Load("prefabQuad"), parent) as GameObject;
		}
	}

	public static Material GetMaterial(Renderer rend, string strImg, string strImgNorm = "blank", string strImgDamaged = "blank", string strDmgColor = "blank")
	{
		string text = strImg + strImgNorm + strImgDamaged + strDmgColor;
		Material material;
		if (DataHandler.dictMaterials.TryGetValue(text, out material))
		{
			return material;
		}
		material = new Material(rend.material);
		material.SetTexture("_MainTex", DataHandler.LoadPNG(strImg + ".png", false, false));
		material.mainTextureScale = new Vector2(1f, 1f);
		material.mainTextureOffset = default(Vector2);
		if (strImgNorm != "blank")
		{
			material.SetTexture("_BumpMap", DataHandler.LoadPNG(strImgNorm + ".png", true, false));
			material.SetTextureScale("_BumpMap", new Vector2(1f, 1f));
			material.SetTextureOffset("_BumpMap", default(Vector2));
		}
		if (strImgDamaged != "blank")
		{
			if (strImgDamaged == string.Empty)
			{
				material.SetFloat("_DmgPresent", 0f);
			}
			else
			{
				material.SetTexture("_DmgTex", DataHandler.LoadPNG(strImgDamaged + ".png", false, false));
				material.SetTextureScale("_DmgTex", new Vector2(1f, 1f));
				material.SetTextureOffset("_DmgTex", default(Vector2));
				material.SetFloat("_DmgPresent", 1f);
			}
		}
		else
		{
			material.SetFloat("_DmgPresent", 0f);
		}
		material.SetVector("_WearCol", Item.GetWearColor(strDmgColor, strImgDamaged));
		material.name = text;
		DataHandler.dictMaterials[text] = material;
		return material;
	}

	public static Material GetMaterialSheet(Renderer rend, string strImg, int nIndex, string strImgNorm = "blank", string strImgDamaged = "blank", string strDmgColor = "blank", int tileWidth = 1, int tileHeight = 1)
	{
		string text = string.Concat(new object[]
		{
			strImg,
			"Sheet",
			nIndex,
			strImgNorm,
			strImgDamaged,
			strDmgColor
		});
		Material material;
		if (DataHandler.dictMaterials.TryGetValue(text, out material))
		{
			return material;
		}
		material = new Material(rend.material);
		material.name = text;
		material.SetTexture("_MainTex", DataHandler.LoadPNG(strImg + ".png", false, false));
		float num = 1f * (float)tileWidth * 16f / (float)material.GetTexture("_MainTex").width;
		float num2 = 1f * (float)tileHeight * 16f / (float)material.GetTexture("_MainTex").height;
		material.mainTextureScale = new Vector2(num, num2);
		int num3 = MathUtils.RoundToInt(1f / num);
		int num4 = MathUtils.RoundToInt(1f / num2);
		int num5 = Mathf.FloorToInt((float)(nIndex / num3));
		int num6 = Mathf.FloorToInt((float)(nIndex % num3));
		material.mainTextureOffset = new Vector2(num * (float)num6, num2 * (float)num5);
		if (strImgNorm != "blank")
		{
			material.SetTexture("_BumpMap", DataHandler.LoadPNG(strImgNorm + ".png", true, false));
			material.SetTextureScale("_BumpMap", new Vector2(num, num2));
			material.SetTextureOffset("_BumpMap", new Vector2(num * (float)num6, num2 * (float)num5));
		}
		if (strImgDamaged != "blank")
		{
			if (strImgDamaged == string.Empty)
			{
				material.SetFloat("_DmgPresent", 0f);
			}
			else
			{
				material.SetTexture("_DmgTex", DataHandler.LoadPNG(strImgDamaged + ".png", false, false));
				material.SetTextureScale("_DmgTex", new Vector2(num, num2));
				material.SetTextureOffset("_DmgTex", new Vector2(num * (float)num6, num2 * (float)num5));
				material.SetFloat("_DmgPresent", 1f);
				material.SetFloat("_Rows", (float)num4);
				material.SetFloat("_Columns", (float)num3);
			}
		}
		else
		{
			material.SetFloat("_DmgPresent", 0f);
		}
		material.SetVector("_WearCol", Item.GetWearColor(strDmgColor, strImgDamaged));
		DataHandler.dictMaterials[text] = material;
		return material;
	}

	public static IEnumerable GetAudio(string strPath)
	{
		strPath = "file:///" + DataHandler.strAssetPath + "/audio/sfx/AlarmMaster01.ogg";
		WWW www = new WWW(strPath);
		yield return www;
		if (www.error != null)
		{
			Debug.Log(www.error);
		}
		AudioClip clip = www.GetAudioClip(false, true);
		if (clip != null && clip.isReadyToPlay)
		{
			AudioSource audioSource = new AudioSource();
			audioSource.clip = clip;
			audioSource.transform.SetParent(CrewSim.objInstance.transform);
			audioSource.Play();
		}
		yield break;
	}

	public static string GetNextID()
	{
		return Guid.NewGuid().ToString();
	}

	public static JsonComputerEntry GetEntry(string name)
	{
		return DataHandler.dictComputerEntries[name];
	}

	public static void WordCount()
	{
		File.Delete("words.txt");
		File.AppendAllText("words.txt", DataHandler.AppendDictWords<JsonShip>(new string[]
		{
			"make",
			"model",
			"designation",
			"dimensions"
		}, DataHandler.dictShips));
		File.AppendAllText("words.txt", DataHandler.AppendDictWords<JsonCond>(new string[]
		{
			"strNameFriendly",
			"strDesc"
		}, DataHandler.dictConds));
		File.AppendAllText("words.txt", DataHandler.AppendDictWords<JsonCondOwner>(new string[]
		{
			"strNameFriendly",
			"strDesc"
		}, DataHandler.dictCOs));
		File.AppendAllText("words.txt", DataHandler.AppendDictWords<JsonInteraction>(new string[]
		{
			"strTitle",
			"strDesc"
		}, DataHandler.dictInteractions));
		File.AppendAllText("words.txt", DataHandler.AppendDictWords<JsonHomeworld>(new string[]
		{
			"strColonyName"
		}, DataHandler.dictHomeworlds));
		File.AppendAllText("words.txt", DataHandler.AppendDictWords<JsonCareer>(new string[]
		{
			"strNameFriendly"
		}, DataHandler.dictCareers));
		File.AppendAllText("words.txt", DataHandler.AppendDictWords<JsonAd>(new string[]
		{
			"strDesc"
		}, DataHandler.dictAds));
		File.AppendAllText("words.txt", DataHandler.AppendDictWords<JsonHeadline>(new string[]
		{
			"strName",
			"strDesc",
			"strRegion"
		}, DataHandler.dictHeadlines));
		File.AppendAllText("words.txt", DataHandler.AppendDictWords<JsonCOOverlay>(new string[]
		{
			"strNameFriendly"
		}, DataHandler.dictCOOverlays));
		File.AppendAllText("words.txt", DataHandler.AppendDictWords<JsonLedgerDef>(new string[]
		{
			"strDesc"
		}, DataHandler.dictLedgerDefs));
		File.AppendAllText("words.txt", DataHandler.AppendDictWords<JsonJobItems>(new string[]
		{
			"strFriendlyName"
		}, DataHandler.dictJobitems));
		StringBuilder stringBuilder = new StringBuilder();
		foreach (Dictionary<string, string> dictionary in DataHandler.dictGUIPropMaps.Values)
		{
			if (dictionary.ContainsKey("strFriendlyName"))
			{
				stringBuilder.AppendLine(dictionary["strFriendlyName"]);
			}
			if (dictionary.ContainsKey("strTitle"))
			{
				stringBuilder.AppendLine(dictionary["strTitle"]);
			}
			if (dictionary.ContainsKey("strBrand"))
			{
				stringBuilder.AppendLine(dictionary["strBrand"]);
			}
			if (dictionary.ContainsKey("strBrandSub"))
			{
				stringBuilder.AppendLine(dictionary["strBrandSub"]);
			}
		}
		foreach (string value in DataHandler.dictNamesFirst.Keys)
		{
			stringBuilder.AppendLine(value);
		}
		foreach (string value2 in DataHandler.dictNamesLast.Keys)
		{
			stringBuilder.AppendLine(value2);
		}
		foreach (string value3 in DataHandler.dictNamesFull.Keys)
		{
			stringBuilder.AppendLine(value3);
		}
		foreach (string value4 in DataHandler.dictNamesShip.Keys)
		{
			stringBuilder.AppendLine(value4);
		}
		foreach (string value5 in DataHandler.dictNamesShipAdjectives.Keys)
		{
			stringBuilder.AppendLine(value5);
		}
		foreach (string value6 in DataHandler.dictNamesShipNouns.Keys)
		{
			stringBuilder.AppendLine(value6);
		}
		foreach (string value7 in DataHandler.dictManPages["Manual Pages"])
		{
			stringBuilder.AppendLine(value7);
		}
		foreach (string value8 in DataHandler.dictStrings.Values)
		{
			stringBuilder.AppendLine(value8);
		}
		File.AppendAllText("words.txt", stringBuilder.ToString());
	}

	private static string AppendDictWords<TJson>(string[] aFields, Dictionary<string, TJson> dict)
	{
		StringBuilder stringBuilder = new StringBuilder();
		foreach (TJson tjson in dict.Values)
		{
			foreach (string name in aFields)
			{
				Type type = tjson.GetType();
				PropertyInfo property = type.GetProperty(name);
				if (property != null)
				{
					object value = property.GetValue(tjson, null);
					if (value != null)
					{
						string text = value.ToString();
						if (text != null && text != string.Empty)
						{
							stringBuilder.AppendLine(text);
						}
					}
				}
			}
		}
		return stringBuilder.ToString();
	}

	public static bool IsNameRegistered(KeyValuePair<string, IEnumerable> kvp)
	{
		IDictionary[] array = new IDictionary[]
		{
			DataHandler.dictCTs,
			DataHandler.dictLoot,
			DataHandler.dictInteractions,
			DataHandler.dictConds,
			DataHandler.dictCOs,
			DataHandler.dictPersonSpecs,
			DataHandler.dictCondRules,
			DataHandler.dictItemDefs,
			DataHandler.dictTickers,
			DataHandler.dictShips,
			DataHandler.dictLifeEvents,
			DataHandler.dictCOOverlays,
			DataHandler.dictShipSpecs
		};
		for (int i = 0; i < array.Length; i++)
		{
			Type[] genericArguments = array[i].GetType().GetGenericArguments();
			if (kvp.Value == null || genericArguments[1] == kvp.Value)
			{
				if (array[i].Contains(kvp.Key))
				{
					return true;
				}
			}
			else
			{
				IEnumerator enumerator = kvp.Value.GetEnumerator();
				try
				{
					while (enumerator.MoveNext())
					{
						object obj = enumerator.Current;
						if (genericArguments[1].Equals(obj) && array[i].Contains(kvp.Key))
						{
							return true;
						}
					}
				}
				finally
				{
					IDisposable disposable;
					if ((disposable = (enumerator as IDisposable)) != null)
					{
						disposable.Dispose();
					}
				}
			}
		}
		return false;
	}

	public static void ScanDictionaries()
	{
		if (DataHandler.dictCondRulesLookup == null)
		{
			DataHandler.dictCondRulesLookup = new Dictionary<string, string>();
			foreach (CondRule condRule in DataHandler.dictCondRules.Values)
			{
				if (!string.IsNullOrEmpty(condRule.strName) && !string.IsNullOrEmpty(condRule.strCond))
				{
					DataHandler.dictCondRulesLookup[condRule.strCond] = condRule.strName;
				}
			}
		}
		DataHandler.dictLoot.Verify<Loot>();
		DataHandler.dictInteractions.Verify<JsonInteraction>();
		DataHandler.dictConds.Verify<JsonCond>();
		DataHandler.dictCTs.Verify<CondTrigger>();
		DataHandler.dictCOs.Verify<JsonCondOwner>();
	}

	public static JsonTip GetTip()
	{
		int num = DataHandler.dictAds.Keys.Count;
		if (num == 0)
		{
			return null;
		}
		JsonTip[] array = new JsonTip[num];
		DataHandler.dictTips.Values.CopyTo(array, 0);
		num = MathUtils.Rand(0, array.Length, MathUtils.RandType.Flat, "LoadTip");
		return array[num];
	}

	public static string strAssetPath;

	public const string strImagePath = "images/";

	public const string strManualsPath = "manuals/";

	public const string strDataPath = "data/";

	public const string strMeshPath = "mesh/";

	public const string strImgMissing = "missing.png";

	public const string str32 = " (32)";

	public const string str64 = " (64)";

	public static string strModFolder = string.Empty;

	public const string strModListName = "Mod Loading Order";

	public static Action InitComplete;

	public static Action LoadComplete;

	public static string strBuild;

	public static List<string> aModPaths;

	public static Dictionary<string, JsonCond> dictConds;

	public static Dictionary<string, JsonCondOwner> dictCOs;

	public static Dictionary<string, CondTrigger> dictCTs;

	public static Dictionary<string, JsonInteraction> dictInteractions;

	public static Dictionary<string, Loot> dictLoot;

	public static Dictionary<string, JsonShip> dictShips;

	public static Dictionary<string, JsonSimple> dictSimple;

	public static Dictionary<string, CondOwner> mapCOs;

	public static Dictionary<string, JsonAd> dictAds;

	public static Dictionary<string, JsonAIPersonality> dictAIPersonalities;

	public static Dictionary<string, JsonAttackMode> dictAModes;

	public static Dictionary<string, JsonAudioEmitter> dictAudioEmitters;

	public static Dictionary<string, JsonCareer> dictCareers;

	public static Dictionary<string, JsonCargoSpec> dictCargoSpecs;

	public static Dictionary<string, JsonChargeProfile> dictChargeProfiles;

	public static Dictionary<string, Color> dictColors;

	public static Dictionary<string, JsonComputerEntry> dictComputerEntries;

	public static Dictionary<string, CondRule> dictCondRules;

	public static Dictionary<string, string> dictCondRulesLookup;

	public static Dictionary<string, JsonContext> dictContext;

	public static Dictionary<string, JsonCOOverlay> dictCOOverlays;

	public static Dictionary<string, JsonCondOwnerSave> dictCOSaves;

	public static Dictionary<string, string> dictCrewSkins;

	public static Dictionary<string, JsonCrime> dictCrimes;

	public static Dictionary<string, DataCoCollection> dictDataCoCollections;

	public static Dictionary<string, DataCO> dictDataCOs;

	public static Dictionary<string, JsonGasRespire> dictGasRespires;

	public static Dictionary<string, Dictionary<string, string>> dictGUIPropMaps;

	public static Dictionary<string, JsonGUIPropMap> dictGUIPropMapUnparsed;

	public static Dictionary<string, JsonHeadline> dictHeadlines;

	public static Dictionary<string, JsonHomeworld> dictHomeworlds;

	public static Dictionary<string, string> dictHTMLColors;

	public static Dictionary<string, JsonInteractionOverride> dictIAOverrides;

	public static Dictionary<string, JsonInstallable> dictInstallables;

	public static Dictionary<string, JsonInstallable> dictInstallables2;

	public static Dictionary<string, JsonItemDef> dictItemDefs;

	public static Dictionary<string, Texture2D> dictImages;

	public static Dictionary<string, JsonInfoNode> dictInfoNodes;

	public static Dictionary<string, JsonJobItems> dictJobitems;

	public static Dictionary<string, JsonJob> dictJobs;

	public static Dictionary<string, JsonColor> dictJsonColors;

	public static Dictionary<string, JsonCustomTokens> dictJsonTokens;

	public static Dictionary<string, JsonVerbs> dictJsonVerbs;

	public static Dictionary<string, JsonLedgerDef> dictLedgerDefs;

	public static Dictionary<string, JsonLifeEvent> dictLifeEvents;

	public static Dictionary<string, JsonLight> dictLights;

	public static Dictionary<string, string[]> dictManPages;

	public static Dictionary<string, JsonMarketActorConfig> dictMarketConfigs;

	public static Dictionary<string, Material> dictMaterials;

	public static Dictionary<string, JsonModInfo> dictModInfos;

	public static Dictionary<string, JsonModList> dictModList;

	public static Dictionary<string, JsonMusic> dictMusic;

	public static Dictionary<string, List<string>> dictMusicTags;

	public static Dictionary<string, string> dictNamesFirst;

	public static Dictionary<string, string> dictNamesFull;

	public static Dictionary<string, string> dictNamesLast;

	public static Dictionary<string, string> dictNamesRobots;

	public static Dictionary<string, string> dictNamesShip;

	public static Dictionary<string, string> dictNamesShipAdjectives;

	public static Dictionary<string, string> dictNamesShipNouns;

	public static Dictionary<string, JsonParallax> dictParallax;

	public static Dictionary<string, JsonPDAAppIcon> dictPDAAppIcons;

	public static Dictionary<string, JsonPersonSpec> dictPersonSpecs;

	public static Dictionary<string, JsonPledge> dictPledges;

	public static Dictionary<string, JsonPlotBeatOverride> dictPlotBeatOverrides;

	public static Dictionary<string, JsonPlotBeat> dictPlotBeats;

	public static Dictionary<string, JsonPlotManagerSettings> dictPlotManager;

	public static Dictionary<string, JsonPlot> dictPlots;

	public static Dictionary<string, JsonPowerInfo> dictPowerInfo;

	public static Dictionary<string, JsonProductionMap> dictProductionMaps;

	public static Dictionary<string, JsonRaceTrack> dictRaceTracks;

	public static Dictionary<string, JsonRacingLeague> dictRacingLeagues;

	public static Dictionary<string, RoomSpec> dictRoomSpec;

	public static Dictionary<string, JsonRoomSpec> dictRoomSpecsTemp;

	public static Dictionary<string, JsonUserSettings> dictSettings;

	public static Dictionary<string, Dictionary<string, Texture2D>> dictShipImages;

	public static Dictionary<string, JsonShipSpec> dictShipSpecs;

	public static Dictionary<string, JsonSlotEffects> dictSlotEffects;

	public static Dictionary<string, JsonSlot> dictSlots;

	public static Dictionary<string, SocialStats> dictSocialStats;

	public static Dictionary<string, JsonStarSystemSave> dictStarSystems;

	public static Dictionary<string, string> dictStrings;

	public static Dictionary<string, JsonDCOCollection> dictSupersTemp;

	public static Dictionary<string, JsonTicker> dictTickers;

	public static Dictionary<string, JsonTip> dictTips;

	public static Dictionary<string, int[]> dictTraitScores;

	public static Dictionary<string, JsonTransit> dictTransit;

	public static Dictionary<string, string[]> dictVerbs;

	public static Dictionary<string, JsonWound> dictWounds;

	public static Dictionary<string, JsonZoneTrigger> dictZoneTriggers;

	public static List<string> listCustomTokens;

	private static InteractionObjectTracker _interactionObjectTracker;

	private static CondTrigger _blankCTBackup;

	public static bool bInitialised = false;

	public static bool bLoaded = false;

	public static bool bAsyncLoaded = false;

	public static float fChanceFullname = 0.001f;

	public static int debugCOCount = 0;

	public static bool bSuppressGetErrors = false;

	public static StringBuilder loadLog = new StringBuilder();

	public static StringBuilder loadLogError = new StringBuilder();

	public static StringBuilder loadLogWarning = new StringBuilder();

	public static object dictWriteLock = new object();

	private static Dictionary<string, InflectedTokenData> replacementDict = new Dictionary<string, InflectedTokenData>();
}
