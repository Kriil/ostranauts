using FFU_Beyond_Reach;
using Ostranauts.Core;
using System.Collections.Generic;
using System.IO;
using System;
using MonoMod;
using UnityEngine;
using LitJson;
using Ostranauts.Tools;
using System.Reflection;
using System.Text;
using System.Linq;
using System.Text.RegularExpressions;
using System.Collections;

public partial class patch_JsonModInfo : JsonModInfo {
    public Dictionary<string, string[]> removeIds { get; set; }
    public Dictionary<string, Dictionary<string, string[]>> changesMap { get; set; }
}

public static partial class patch_DataHandler {
    public static string strModsPath = string.Empty;
    public static Dictionary<string, Dictionary<string, List<string>>> dictChangesMap;
    public static List<string> listLockedCOs = [];

    // Internal References
    [MonoModIgnore] public static Dictionary<string, patch_JsonModInfo> dictModInfos;

    [MonoModReplace] public static void Init() {
        DataHandler.loadLog.Length = 0;
        DataHandler.loadLogError.Length = 0;
        DataHandler.loadLogWarning.Length = 0;

        // Reload Cleanup
        if (DataHandler.bInitialised) {
            List<CondOwner> listMapCOs = new(DataHandler.mapCOs.Values);
            foreach (CondOwner item in listMapCOs) {
                if (!(item == null)) {
                    DataHandler.loadLogWarning.Append("Destroying leftover CO: ");
                    DataHandler.loadLogWarning.Append(item.strName);
                    DataHandler.loadLogWarning.AppendLine();
                    item.Destroy();
                }
            }
            listMapCOs.Clear();
            listMapCOs = null;
            DataHandler.mapCOs.Clear();
            if (DataHandler.loadLogWarning.Length > 0) {
                Debug.LogWarning(DataHandler.loadLogWarning.ToString());
            }
            return;
        }

        // Build + Config Loading
        DataHandler.strAssetPath = Application.streamingAssetsPath + "/";
        DataHandler.LoadBuildVersion();
        FFU_BR_Defs.GameVersion = new(((TextAsset)Resources.Load("version", typeof(TextAsset)))?.text ?? "0.0.0.0");
        Debug.Log($"Early Access Build: v{FFU_BR_Defs.GameVersion}");
        FFU_BR_Defs.InitConfig();
        if ((bool)ObjReader.use) {
            ObjReader.use.scaleFactor = new Vector3(0.0625f, 0.0625f, 0.0625f);
            ObjReader.use.objRotation = new Vector3(90f, 0f, 180f);
        }

        // Dictionary Initialization
        dictChangesMap = [];
        DataHandler.SetupDicts();
        DataHandler._interactionObjectTracker ??= new InteractionObjectTracker();

        // User Settings Initialization
        DataHandler.dictSettings["DefaultUserSettings"] = new JsonUserSettings();
        DataHandler.dictSettings["DefaultUserSettings"].Init();
        if (File.Exists(Application.persistentDataPath + "/settings.json")) {
            DataHandler.JsonToData(Application.persistentDataPath + "/settings.json", DataHandler.dictSettings);
        } else {
            DataHandler.loadLogWarning.Append("WARNING: settings.json not found. Resorting to default values.");
            DataHandler.loadLogWarning.AppendLine();
            DataHandler.dictSettings["UserSettings"] = new JsonUserSettings();
            DataHandler.dictSettings["UserSettings"].Init();
        }
        if (!DataHandler.dictSettings.ContainsKey("UserSettings") || DataHandler.dictSettings["UserSettings"] == null) {
            DataHandler.loadLogError.Append("ERROR: Malformed settings.json. Resorting to default values.");
            DataHandler.loadLogError.AppendLine();
            DataHandler.dictSettings["UserSettings"] = new JsonUserSettings();
            DataHandler.dictSettings["UserSettings"].Init();
        }
        DataHandler.dictSettings["DefaultUserSettings"].CopyTo(DataHandler.GetUserSettings());
        DataHandler.dictSettings.Remove("DefaultUserSettings");
        DataHandler.SaveUserSettings();

        // Mod List Initialization
        bool isModded = false;
        DataHandler.strModFolder = DataHandler.dictSettings["UserSettings"].strPathMods;
        if (DataHandler.strModFolder == null || DataHandler.strModFolder == string.Empty) {
            DataHandler.strModFolder = Path.Combine(Application.dataPath, "Mods/");
            DataHandler.loadLogWarning.Append("WARNING: Unrecognized mod folder. Setting mod path to ");
            DataHandler.loadLogWarning.Append(DataHandler.strModFolder);
            DataHandler.loadLogWarning.AppendLine();
        }
        strModsPath = DataHandler.strModFolder.Replace("loading_order.json", string.Empty);
        string strModFolderName = Path.GetDirectoryName(DataHandler.strModFolder);
        strModFolderName = Path.Combine(strModFolderName, "loading_order.json");

        // Creating Mod Placeholder
        JsonModInfo coreModInfo = new();
        coreModInfo.strName = "Core";
        DataHandler.dictModInfos["core"] = coreModInfo;

        // Mod List Loading Routine
        bool isConsoleExists = ConsoleToGUI.instance != null;
        if (isConsoleExists) ConsoleToGUI.instance.LogInfo("Attempting to load " + strModFolderName + "...");

        // Proceed With Mod List Loading
        if (File.Exists(strModFolderName)) {
            if (isConsoleExists) ConsoleToGUI.instance.LogInfo("loading_order.json found. Beginning mod load.");
            DataHandler.JsonToData(strModFolderName, DataHandler.dictModList);

            // Process Mod List
            JsonModList newModList = null;
            if (DataHandler.dictModList.TryGetValue("Mod Loading Order", out newModList)) {
                if (newModList.aIgnorePatterns != null) {
                    for (int i = 0; i < newModList.aIgnorePatterns.Length; i++) {
                        newModList.aIgnorePatterns[i] = DataHandler.PathSanitize(newModList.aIgnorePatterns[i]);
                    }
                }
                string[] aLoadOrder = newModList.aLoadOrder;

                // Go Through Each Mod Entry
                foreach (string aLoadEntry in aLoadOrder) {
                    isModded = true;

                    // Handle Dedicated/Invalid Settings
                    if (aLoadEntry.IsCoreEntry()) {
                        string strDedicatedMsg = $"Loading core files: {DataHandler.strAssetPath}";
                        DataHandler.loadLog.Append(strDedicatedMsg);
                        DataHandler.loadLog.AppendLine();
                        Debug.Log($"#Info# {strDedicatedMsg}");
                        continue;
                    }
                    if (aLoadEntry == null || aLoadEntry == string.Empty) {
                        DataHandler.loadLogError.Append("ERROR! Invalid mod folder specified: ");
                        DataHandler.loadLogError.Append(aLoadEntry);
                        DataHandler.loadLogError.Append("; Skipping...");
                        DataHandler.loadLogError.AppendLine();
                        continue;
                    }

                    // Prepare Mod Information
                    string aLoadPath = aLoadEntry.TrimStart(Path.DirectorySeparatorChar);
                    aLoadPath = aLoadEntry.TrimStart(Path.AltDirectorySeparatorChar) + "/";
                    string modFolderPath = Path.GetDirectoryName(DataHandler.strModFolder);
                    modFolderPath = Path.Combine(modFolderPath, aLoadPath);
                    Dictionary<string, JsonModInfo> modInfoJson = [];
                    string modInfoPath = Path.Combine(modFolderPath, "mod_info.json");

                    // Start Mod Loading Routine
                    if (File.Exists(modInfoPath)) {
                        DataHandler.JsonToData(modInfoPath, modInfoJson);
                    }
                    if (modInfoJson.Count < 1) {
                        JsonModInfo altModInfo = new();
                        altModInfo.strName = aLoadEntry;
                        modInfoJson[altModInfo.strName] = altModInfo;
                        DataHandler.loadLogWarning.Append("WARNING: Missing mod_info.json in folder: ");
                        DataHandler.loadLogWarning.Append(aLoadEntry);
                        DataHandler.loadLogWarning.Append("; Using default name: ");
                        DataHandler.loadLogWarning.Append(altModInfo.strName);
                        DataHandler.loadLogWarning.AppendLine();
                    }
                    using (Dictionary<string, JsonModInfo>.ValueCollection.Enumerator modEnum = modInfoJson.Values.GetEnumerator()) {
                        if (modEnum.MoveNext()) {
                            JsonModInfo modCurrent = modEnum.Current;
                            DataHandler.dictModInfos[aLoadEntry] = modCurrent;
                            string strFoundModMsg = $"Loading mod '{DataHandler.dictModInfos[aLoadEntry].strName}' from directory: {aLoadEntry}";
                            if (isConsoleExists) ConsoleToGUI.instance.LogInfo(strFoundModMsg);
                            else Debug.Log($"#Info# {strFoundModMsg}");
                        }
                    }
                }
            }

            // Sync Load All Mod Data
            SyncLoadMods(newModList.aIgnorePatterns);
        }

        // Default Non-Modded Loading
        if (!isModded) {
            string strLoadCleanMsg = $"No loading_order.json found. Loading default game data from {DataHandler.strAssetPath}";
            if (isConsoleExists) ConsoleToGUI.instance.LogInfo(strLoadCleanMsg);
            Debug.Log($"#Info# {strLoadCleanMsg}");

            // Creating Clean Placeholder
            JsonModList cleanModList = new();
            cleanModList.strName = "Default";
            cleanModList.aLoadOrder = ["core"];
            cleanModList.aIgnorePatterns = [];
            DataHandler.dictModList["Mod Loading Order"] = cleanModList;

            // Sync Load Core Data Only
            SyncLoadMods(cleanModList.aIgnorePatterns);
        }

        // Validate Mapped COs In Ship Templates
        if (FFU_BR_Defs.ModSyncLoading) {
            foreach (var dictShip in DataHandler.dictShips) {
                SwitchSlottedItems(dictShip.Value, true);
                RecoverMissingItems(dictShip.Value);
            }
        }

        // Write Logged Information
        Application.SetStackTraceLogType(LogType.Log, StackTraceLogType.None);
        if (DataHandler.loadLog.Length > 0) {
            Debug.Log(DataHandler.loadLog.ToString());
        }
        Application.SetStackTraceLogType(LogType.Log, StackTraceLogType.ScriptOnly);
        if (DataHandler.loadLogWarning.Length > 0) {
            Debug.LogWarning(DataHandler.loadLogWarning.ToString());
        }
        if (DataHandler.loadLogError.Length > 0) {
            Debug.LogError(DataHandler.loadLogError.ToString());
        }

        // Loaded Data Post-Processing
        DataHandler.bInitialised = true;
        if (DataHandler.InitComplete != null) {
            DataHandler.InitComplete();
        }
    }

    private static void SyncLoadMods(string[] aIgnorePatterns) {
        // Validate Mod Paths
        foreach (var modInfo in dictModInfos) {
            if (modInfo.Key.IsCoreEntry()) continue;
            string modPath = Path.Combine(strModsPath, modInfo.Key);
            if (Directory.Exists(Path.Combine(modPath, "data/"))) {
                Debug.Log($"Data Mod Queued: {modInfo.Key} => {modPath}");
            } else if (Directory.GetDirectories(modPath).Length > 0) {
                Debug.Log($"Asset Mod Queued: {modInfo.Key} => {modPath}");
            } else if (File.Exists(Path.Combine(modPath, "mod_info.json"))) {
                Debug.Log($"Patch Mod Queued: {modInfo.Key} => {modPath}");
            } else {
                Debug.LogWarning($"Attempted to queue invalid mod: {modInfo.Key}");
            }
        }

        // List Valid Data Paths
        bool isConsoleExists = ConsoleToGUI.instance != null;
        int numConsoleErrors = 0;
        if (isConsoleExists) {
            numConsoleErrors = ConsoleToGUI.instance.ErrorCount;
            ConsoleToGUI.instance.LogInfo("Begin loading data from these paths:");
            foreach (var modInfo in dictModInfos) {
                if (modInfo.Key.IsCoreEntry()) continue;
                string modPath = Path.Combine(strModsPath, modInfo.Key);
                string dataPath = Path.Combine(modPath, "data/");
                if (Directory.Exists(dataPath)) ConsoleToGUI.instance.LogInfo(dataPath);
            }
        }

        // Create CO Changes Map
        foreach (var modInfo in dictModInfos) {
            if (modInfo.Key.IsCoreEntry()) continue;
            if (modInfo.Value?.changesMap != null) {
                foreach (var changeMap in modInfo.Value.changesMap) {
                    if (!dictChangesMap.ContainsKey(changeMap.Key))
                        dictChangesMap[changeMap.Key] = [];
                    if (changeMap.Value != null) {
                        foreach (var subMap in changeMap.Value) {
                            bool IsInverse = subMap.Key.StartsWith(FFU_BR_Defs.SYM_INV);
                            string subMapKey = IsInverse? subMap.Key.Substring(1) : subMap.Key;
                            if (subMapKey != FFU_BR_Defs.OPT_DEL) {
                                if (!dictChangesMap[changeMap.Key].ContainsKey(subMapKey))
                                    dictChangesMap[changeMap.Key][subMapKey] = [];
                                if (IsInverse && !dictChangesMap[changeMap.Key][subMapKey].Contains(FFU_BR_Defs.FLAG_INVERSE))
                                    dictChangesMap[changeMap.Key][subMapKey].Add(FFU_BR_Defs.FLAG_INVERSE);
                                if (subMap.Value != null && subMap.Value.Length > 0) {
                                    if (subMap.Value[0] != FFU_BR_Defs.OPT_DEL) {
                                        foreach (var subMapEntry in subMap.Value) {
                                            if (subMapEntry.StartsWith(FFU_BR_Defs.OPT_REM)) {
                                                string cleanEntry = subMapEntry.Substring(1);
                                                string targetEntry = dictChangesMap[changeMap.Key][subMapKey]
                                                    .Find(x => x.StartsWith(cleanEntry));
                                                if (!string.IsNullOrEmpty(targetEntry)) {
                                                    dictChangesMap[changeMap.Key][subMapKey].Remove(targetEntry);
                                                    continue;
                                                }
                                            } else if (subMapEntry.StartsWith(FFU_BR_Defs.OPT_MOD)) {
                                                string cleanEntry = subMapEntry.Substring(1);
                                                string lookupKey = cleanEntry.Contains(FFU_BR_Defs.SYM_DIV) ? cleanEntry.Split(FFU_BR_Defs.SYM_DIV[0])[0] :
                                                    cleanEntry.Contains(FFU_BR_Defs.SYM_EQU) ? cleanEntry.Split(FFU_BR_Defs.SYM_EQU[0])[0] : cleanEntry;
                                                string targetEntry = dictChangesMap[changeMap.Key][subMapKey]
                                                    .Find(x => x.StartsWith(lookupKey));
                                                if (!string.IsNullOrEmpty(targetEntry)) {
                                                    int targetIdx = dictChangesMap[changeMap.Key][subMapKey].IndexOf(targetEntry);
                                                    dictChangesMap[changeMap.Key][subMapKey][targetIdx] = cleanEntry;
                                                    continue;
                                                }
                                            } else dictChangesMap[changeMap.Key][subMapKey].Add(subMapEntry);
                                        }
                                    }
                                    else dictChangesMap[changeMap.Key].Remove(subMapKey);
                                }
                            } else {
                                dictChangesMap.Remove(changeMap.Key);
                                if (changeMap.Value.Count > 1)
                                    dictChangesMap[changeMap.Key] = [];
                                else break;
                            }
                        }
                    }
                }
            }
        }
        if (FFU_BR_Defs.SyncLogging >= FFU_BR_Defs.SyncLogs.ModdedDump) Debug.Log($"Dynamic Changes Map (Dump): {JsonMapper.ToJson(dictChangesMap)}");

        // Sync Mod Paths
        foreach (var modInfo in dictModInfos) {
            if (modInfo.Key.IsCoreEntry()) DataHandler.aModPaths.Insert(0, DataHandler.strAssetPath);
            else DataHandler.aModPaths.Insert(0, Path.Combine(strModsPath, modInfo.Key) + "/");
        }

        // Prepare Simple Arrays
        Dictionary<string, JsonSimple> condsSimple = [];
        Dictionary<string, JsonSimple> dictCrewSkins = [];
        Dictionary<string, JsonSimple> dictManualPages = [];
        Dictionary<string, JsonSimple> dictFirstNames = [];
        Dictionary<string, JsonSimple> dictFullNames = [];
        Dictionary<string, JsonSimple> dictLastNames = [];
        Dictionary<string, JsonSimple> dictRobotNames = [];
        Dictionary<string, JsonSimple> dictShipNames = [];
        Dictionary<string, JsonSimple> dictShipAdjectives = [];
        Dictionary<string, JsonSimple> dictShipNouns = [];
        Dictionary<string, JsonSimple> dictStrings = [];
        Dictionary<string, JsonSimple> dictTraits = [];

        // Sync Load Mods Data
        foreach (var modInfo in dictModInfos) SyncLoadJSONs(modInfo, "ships/", DataHandler.dictShips, aIgnorePatterns);
        foreach (var modInfo in dictModInfos) SyncLoadJSONs(modInfo, "ads/", DataHandler.dictAds, aIgnorePatterns);
        foreach (var modInfo in dictModInfos) SyncLoadJSONs(modInfo, "ai_training/", DataHandler.dictAIPersonalities, aIgnorePatterns);
        foreach (var modInfo in dictModInfos) SyncLoadJSONs(modInfo, "attackmodes/", DataHandler.dictAModes, aIgnorePatterns);
        foreach (var modInfo in dictModInfos) SyncLoadJSONs(modInfo, "audioemitters/", DataHandler.dictAudioEmitters, aIgnorePatterns);
        foreach (var modInfo in dictModInfos) SyncLoadJSONs(modInfo, "careers/", DataHandler.dictCareers, aIgnorePatterns);
        foreach (var modInfo in dictModInfos) SyncLoadJSONs(modInfo, "chargeprofiles/", DataHandler.dictChargeProfiles, aIgnorePatterns);
        foreach (var modInfo in dictModInfos) SyncLoadJSONs(modInfo, "colors/", DataHandler.dictJsonColors, aIgnorePatterns);
        foreach (var modInfo in dictModInfos) SyncLoadJSONs(modInfo, "conditions/", DataHandler.dictConds, aIgnorePatterns);
        foreach (var modInfo in dictModInfos) SyncLoadJSONs(modInfo, "conditions_simple/", condsSimple, aIgnorePatterns);
        foreach (var modInfo in dictModInfos) SyncLoadJSONs(modInfo, "condowners/", DataHandler.dictCOs, aIgnorePatterns);
        foreach (var modInfo in dictModInfos) SyncLoadJSONs(modInfo, "condrules/", DataHandler.dictCondRules, aIgnorePatterns);
        foreach (var modInfo in dictModInfos) SyncLoadJSONs(modInfo, "condtrigs/", DataHandler.dictCTs, aIgnorePatterns);
        foreach (var modInfo in dictModInfos) SyncLoadJSONs(modInfo, "context/", DataHandler.dictContext, aIgnorePatterns);
        foreach (var modInfo in dictModInfos) SyncLoadJSONs(modInfo, "cooverlays/", DataHandler.dictCOOverlays, aIgnorePatterns);
        foreach (var modInfo in dictModInfos) SyncLoadJSONs(modInfo, "crewskins/", dictCrewSkins, aIgnorePatterns);
        foreach (var modInfo in dictModInfos) SyncLoadJSONs(modInfo, "crime/", DataHandler.dictCrimes, aIgnorePatterns);
        foreach (var modInfo in dictModInfos) SyncLoadJSONs(modInfo, "gasrespires/", DataHandler.dictGasRespires, aIgnorePatterns);
        foreach (var modInfo in dictModInfos) SyncLoadJSONs(modInfo, "guipropmaps/", DataHandler.dictGUIPropMapUnparsed, aIgnorePatterns);
        foreach (var modInfo in dictModInfos) SyncLoadJSONs(modInfo, "headlines/", DataHandler.dictHeadlines, aIgnorePatterns);
        foreach (var modInfo in dictModInfos) SyncLoadJSONs(modInfo, "homeworlds/", DataHandler.dictHomeworlds, aIgnorePatterns);
        foreach (var modInfo in dictModInfos) SyncLoadJSONs(modInfo, "info/", DataHandler.dictInfoNodes, aIgnorePatterns);
        foreach (var modInfo in dictModInfos) SyncLoadJSONs(modInfo, "installables/", DataHandler.dictInstallables, aIgnorePatterns);
        foreach (var modInfo in dictModInfos) SyncLoadJSONs(modInfo, "interaction_overrides/", DataHandler.dictIAOverrides, aIgnorePatterns);
        foreach (var modInfo in dictModInfos) SyncLoadJSONs(modInfo, "interactions/", DataHandler.dictInteractions, aIgnorePatterns);
        foreach (var modInfo in dictModInfos) SyncLoadJSONs(modInfo, "items/", DataHandler.dictItemDefs, aIgnorePatterns);
        foreach (var modInfo in dictModInfos) SyncLoadJSONs(modInfo, "jobitems/", DataHandler.dictJobitems, aIgnorePatterns);
        foreach (var modInfo in dictModInfos) SyncLoadJSONs(modInfo, "jobs/", DataHandler.dictJobs, aIgnorePatterns);
        foreach (var modInfo in dictModInfos) SyncLoadJSONs(modInfo, "ledgerdefs/", DataHandler.dictLedgerDefs, aIgnorePatterns);
        foreach (var modInfo in dictModInfos) SyncLoadJSONs(modInfo, "lifeevents/", DataHandler.dictLifeEvents, aIgnorePatterns);
        foreach (var modInfo in dictModInfos) SyncLoadJSONs(modInfo, "lights/", DataHandler.dictLights, aIgnorePatterns);
        foreach (var modInfo in dictModInfos) SyncLoadJSONs(modInfo, "loot/", DataHandler.dictLoot, aIgnorePatterns);
        foreach (var modInfo in dictModInfos) SyncLoadJSONs(modInfo, "manpages/", dictManualPages, aIgnorePatterns);
        foreach (var modInfo in dictModInfos) SyncLoadJSONs(modInfo, "market/Markets/", DataHandler.dictMarketConfigs, aIgnorePatterns);
        foreach (var modInfo in dictModInfos) SyncLoadJSONs(modInfo, "market/CoCollections/", DataHandler.dictSupersTemp, aIgnorePatterns);
        foreach (var modInfo in dictModInfos) SyncLoadJSONs(modInfo, "market/Production/", DataHandler.dictProductionMaps, aIgnorePatterns);
        foreach (var modInfo in dictModInfos) SyncLoadJSONs(modInfo, "market/CargoSpecs/", DataHandler.dictCargoSpecs, aIgnorePatterns);
        foreach (var modInfo in dictModInfos) SyncLoadJSONs(modInfo, "music/", DataHandler.dictMusic, aIgnorePatterns);
        foreach (var modInfo in dictModInfos) SyncLoadJSONs(modInfo, "names_first/", dictFirstNames, aIgnorePatterns);
        foreach (var modInfo in dictModInfos) SyncLoadJSONs(modInfo, "names_full/", dictFullNames, aIgnorePatterns);
        foreach (var modInfo in dictModInfos) SyncLoadJSONs(modInfo, "names_last/", dictLastNames, aIgnorePatterns);
        foreach (var modInfo in dictModInfos) SyncLoadJSONs(modInfo, "names_robots/", dictRobotNames, aIgnorePatterns);
        foreach (var modInfo in dictModInfos) SyncLoadJSONs(modInfo, "names_ship/", dictShipNames, aIgnorePatterns);
        foreach (var modInfo in dictModInfos) SyncLoadJSONs(modInfo, "names_ship_adjectives/", dictShipAdjectives, aIgnorePatterns);
        foreach (var modInfo in dictModInfos) SyncLoadJSONs(modInfo, "names_ship_nouns/", dictShipNouns, aIgnorePatterns);
        foreach (var modInfo in dictModInfos) SyncLoadJSONs(modInfo, "parallax/", DataHandler.dictParallax, aIgnorePatterns);
        foreach (var modInfo in dictModInfos) SyncLoadJSONs(modInfo, "pda_apps/", DataHandler.dictPDAAppIcons, aIgnorePatterns);
        foreach (var modInfo in dictModInfos) SyncLoadJSONs(modInfo, "personspecs/", DataHandler.dictPersonSpecs, aIgnorePatterns);
        foreach (var modInfo in dictModInfos) SyncLoadJSONs(modInfo, "pledges/", DataHandler.dictPledges, aIgnorePatterns);
        foreach (var modInfo in dictModInfos) SyncLoadJSONs(modInfo, "plot_beat_overrides/", DataHandler.dictPlotBeatOverrides, aIgnorePatterns);
        foreach (var modInfo in dictModInfos) SyncLoadJSONs(modInfo, "plot_beats/", DataHandler.dictPlotBeats, aIgnorePatterns);
        foreach (var modInfo in dictModInfos) SyncLoadJSONs(modInfo, "plot_manager/", DataHandler.dictPlotManager, aIgnorePatterns);
        foreach (var modInfo in dictModInfos) SyncLoadJSONs(modInfo, "plots/", DataHandler.dictPlots, aIgnorePatterns);
        foreach (var modInfo in dictModInfos) SyncLoadJSONs(modInfo, "powerinfos/", DataHandler.dictPowerInfo, aIgnorePatterns);
        foreach (var modInfo in dictModInfos) SyncLoadJSONs(modInfo, "racing/leagues/", DataHandler.dictRacingLeagues, aIgnorePatterns);
        foreach (var modInfo in dictModInfos) SyncLoadJSONs(modInfo, "racing/tracks/", DataHandler.dictRaceTracks, aIgnorePatterns);
        foreach (var modInfo in dictModInfos) SyncLoadJSONs(modInfo, "rooms/", DataHandler.dictRoomSpecsTemp, aIgnorePatterns);
        foreach (var modInfo in dictModInfos) SyncLoadJSONs(modInfo, "shipspecs/", DataHandler.dictShipSpecs, aIgnorePatterns);
        foreach (var modInfo in dictModInfos) SyncLoadJSONs(modInfo, "slot_effects/", DataHandler.dictSlotEffects, aIgnorePatterns);
        foreach (var modInfo in dictModInfos) SyncLoadJSONs(modInfo, "slots/", DataHandler.dictSlots, aIgnorePatterns);
        foreach (var modInfo in dictModInfos) SyncLoadJSONs(modInfo, "star_systems/", DataHandler.dictStarSystems, aIgnorePatterns);
        foreach (var modInfo in dictModInfos) SyncLoadJSONs(modInfo, "strings/", dictStrings, aIgnorePatterns);
        foreach (var modInfo in dictModInfos) SyncLoadJSONs(modInfo, "tickers/", DataHandler.dictTickers, aIgnorePatterns);
        foreach (var modInfo in dictModInfos) SyncLoadJSONs(modInfo, "tips/", DataHandler.dictTips, aIgnorePatterns);
        foreach (var modInfo in dictModInfos) SyncLoadJSONs(modInfo, "tokens/", DataHandler.dictJsonTokens, aIgnorePatterns);
        foreach (var modInfo in dictModInfos) SyncLoadJSONs(modInfo, "traitscores/", dictTraits, aIgnorePatterns);
        foreach (var modInfo in dictModInfos) SyncLoadJSONs(modInfo, "transit/", DataHandler.dictTransit, aIgnorePatterns);
        foreach (var modInfo in dictModInfos) SyncLoadJSONs(modInfo, "verbs/", DataHandler.dictJsonVerbs, aIgnorePatterns);
        foreach (var modInfo in dictModInfos) SyncLoadJSONs(modInfo, "wounds/", DataHandler.dictWounds, aIgnorePatterns);
        foreach (var modInfo in dictModInfos) SyncLoadJSONs(modInfo, "zone_triggers/", DataHandler.dictZoneTriggers, aIgnorePatterns);

        // Process Loaded Data
        DataHandler.ParseGUIPropMaps();
        DataHandler.ParseConditionsSimple(condsSimple);
        DataHandler.ParseTraitScores(dictTraits);
        DataHandler.ParseSimpleIntoStringDict(dictStrings, DataHandler.dictStrings);
        DataHandler.ParseSimpleIntoStringDict(dictCrewSkins, DataHandler.dictCrewSkins);
        DataHandler.ParseSimpleIntoStringDict(dictFirstNames, DataHandler.dictNamesFirst);
        DataHandler.ParseSimpleIntoStringDict(dictFullNames, DataHandler.dictNamesFull);
        DataHandler.ParseSimpleIntoStringDict(dictLastNames, DataHandler.dictNamesLast);
        DataHandler.ParseSimpleIntoStringDict(dictRobotNames, DataHandler.dictNamesRobots);
        DataHandler.ParseSimpleIntoStringDict(dictShipNames, DataHandler.dictNamesShip);
        DataHandler.ParseSimpleIntoStringDict(dictShipNouns, DataHandler.dictNamesShipNouns);
        DataHandler.ParseSimpleIntoStringDict(dictShipAdjectives, DataHandler.dictNamesShipAdjectives);
        DataHandler.ParseManPages(dictManualPages);
        DataHandler.ParseMusic();

        // Create Fast List of Locked COs
        foreach (var dictCO in DataHandler.dictCOs) {
            if (dictCO.Value.bSlotLocked) patch_DataHandler.listLockedCOs.Add(dictCO.Value.strName);
        }

        // Finalize Mod Load Status
        foreach (var modInfo in DataHandler.dictModInfos) {
            if (modInfo.Value.Status == GUIModRow.ModStatus.Missing) {
                modInfo.Value.Status = GUIModRow.ModStatus.Missing;
            } else if ((bool)ConsoleToGUI.instance && 
                numConsoleErrors < ConsoleToGUI.instance.ErrorCount) {
                modInfo.Value.Status = GUIModRow.ModStatus.Error;
            } else {
                modInfo.Value.Status = GUIModRow.ModStatus.Loaded;
            }
        }
    }

    private static void SyncLoadJSONs<TJson>(KeyValuePair<string, patch_JsonModInfo> refModInfo, string subFolder, Dictionary<string, TJson> dataDict, string[] aIgnorePatterns) {
        // Prepare Reference Data
        bool isMod = !refModInfo.Key.IsCoreEntry();
        string fileType = subFolder.Remove(subFolder.Length - 1);
        string dataPath = Path.Combine(isMod ?
            Path.Combine(strModsPath, refModInfo.Key) :
            DataHandler.strAssetPath, "data/");

        // Per Mod Data Removal
        if (refModInfo.Value?.removeIds != null && refModInfo.Value.removeIds.ContainsKey(fileType)) {
            foreach (string removeId in refModInfo.Value.removeIds[fileType]) {
                bool wasRemoved = dataDict.Remove(removeId);
                if (wasRemoved) Debug.Log($"Removed existing '{fileType}' entry: {removeId}");
            }
        }

        // Ignore Missing Folder
        string strSubFolderPath = Path.Combine(dataPath, subFolder);
        if (!Directory.Exists(strSubFolderPath)) return;

        // Parse Folder Contents
        string[] subFiles = Directory.GetFiles(strSubFolderPath, "*.json", SearchOption.AllDirectories);
        foreach (string subFile in subFiles) {
            string filePath = DataHandler.PathSanitize(subFile);

            // Check Ignored Patterns
            bool isIgnoredPath = false;
            if (aIgnorePatterns != null) {
                foreach (string ignorePattern in aIgnorePatterns) {
                    if (filePath.IndexOf(ignorePattern) >= 0) {
                        isIgnoredPath = true;
                        break;
                    }
                }
            }

            // Data Loading Subroutine
            if (isIgnoredPath) {
                Debug.LogWarning("Ignore Pattern match: " + filePath + "; Skipping...");
            } else {
                SyncToData(filePath, fileType, isMod, dataDict, fileType.IsExtedable());
            }
        }
    }

    public static void SyncToData<TJson>(string strFile, string strType, bool isMod, Dictionary<string, TJson> dataDict, bool extData) {
        Debug.Log("#Info# Loading JSON: " + strFile);
        bool logModded = FFU_BR_Defs.SyncLogging >= FFU_BR_Defs.SyncLogs.ModChanges;
        bool logRefCopy = FFU_BR_Defs.SyncLogging >= FFU_BR_Defs.SyncLogs.DeepCopy;
        bool logObjects = FFU_BR_Defs.SyncLogging >= FFU_BR_Defs.SyncLogs.ModdedDump;
        bool logExtended = FFU_BR_Defs.SyncLogging >= FFU_BR_Defs.SyncLogs.ExtendedDump;
        bool logContent = FFU_BR_Defs.SyncLogging >= FFU_BR_Defs.SyncLogs.ContentDump;
        bool logSource = FFU_BR_Defs.SyncLogging >= FFU_BR_Defs.SyncLogs.SourceDump;
        string rawDump = string.Empty;
        try {
            // Raw JSON to Data Array
            string dataFile = File.ReadAllText(strFile, Encoding.UTF8);
            rawDump += "Converting JSON into Array...\n";
            string[] rawData = isMod ? dataFile.Compressed().Split(["},{"], StringSplitOptions.None) : null;
            TJson[] fileData = JsonMapper.ToObject<TJson[]>(dataFile);

            // Parsing Each Data Block
            for (int i = 0; i < fileData.Length; i++) {
                TJson dataBlock = fileData[i];
                string rawBlock = isMod ? rawData[i] : null;
                rawDump += "Getting key: ";
                string referenceKey = null;
                string dataKey = null;

                // Validating Data Block
                PropertyInfo referenceProperty = dataBlock.GetType()?.GetProperty("strReference");
                PropertyInfo nameProperty = dataBlock.GetType()?.GetProperty("strName");
                if (nameProperty == null) {
                    JsonLogger.ReportProblem("strName is missing", ReportTypes.FailingString);
                    continue;
                }

                // Data Allocation Subroutine
                object referenceValue = referenceProperty?.GetValue(dataBlock, null);
                object nameValue = nameProperty.GetValue(dataBlock, null);
                referenceKey = referenceValue?.ToString();
                dataKey = nameValue.ToString();
                rawDump = rawDump + dataKey + "\n";
                if (isMod && dataDict.ContainsKey(dataKey)) {
                    // Modify Existing Data
                    if (logObjects) Debug.Log($"Modification Data (Dump/Before): {JsonMapper.ToJson(dataDict[dataKey])}");
                    try {
                        SyncDataSafe(dataDict[dataKey], dataBlock, ref rawBlock, strType, dataKey, extData, logModded);
                        if (logObjects) Debug.Log($"Modification Data (Dump/After): {JsonMapper.ToJson(dataDict[dataKey])}");
                    } catch (Exception ex) {
                        Exception inner = ex.InnerException;
                        Debug.LogWarning($"Modification sync for Data Block [{dataKey}] " +
                        $"has failed! Ignoring.\n{ex.Message}\n{ex.StackTrace}" + (inner != null ? 
                        $"\nInner: {inner.Message}\n{inner.StackTrace}" : ""));
                    }
                } else if (isMod && !dataDict.ContainsKey(dataKey)) {
                    // Reference Deep Copy + Apply Changes
                    if (referenceKey != null && dataDict.ContainsKey(referenceKey)) {
                        string deepCopy = JsonMapper.ToJson(dataDict[referenceKey]);
                        if (logExtended) Debug.Log($"Reference Data (Dump/Before): {deepCopy}");
                        bool isDeepCopySuccess = false;
                        deepCopy = Regex.Replace(deepCopy, "(\"strName\":)\"[^\"]*\"", match => {
                            isDeepCopySuccess = true;
                            return $"{match.Groups[1].Value}\"{dataKey}\"";
                        });
                        if (isDeepCopySuccess) {
                            TJson deepCopyBlock = JsonMapper.ToObject<TJson>(deepCopy);
                            if (logRefCopy) Debug.Log($"#Info# Modified Deep Copy Created: {referenceKey} => {dataKey}");
                            try {
                                SyncDataSafe(deepCopyBlock, dataBlock, ref rawBlock, strType, dataKey, extData, logRefCopy);
                                if (logExtended) Debug.Log($"Reference Data (Dump/After): {JsonMapper.ToJson(deepCopyBlock)}");
                            } catch (Exception ex) {
                                Exception inner = ex.InnerException;
                                Debug.LogWarning($"Reference sync for Data Block [{dataKey}] " +
                                $"has failed! Ignoring.\n{ex.Message}\n{ex.StackTrace}" + (inner != null ?
                                $"\nInner: {inner.Message}\n{inner.StackTrace}" : ""));
                            }
                            try {
                                dataDict.Add(dataKey, deepCopyBlock);
                            } catch (Exception ex) {
                                Exception inner = ex.InnerException;
                                Debug.LogWarning($"Reference add of new Data Block [{dataKey}] " +
                                $"has failed! Ignoring.\n{ex.Message}\n{ex.StackTrace}" + (inner != null ?
                                $"\nInner: {inner.Message}\n{inner.StackTrace}" : ""));
                            }
                        }
                    } else if (!string.IsNullOrEmpty(referenceKey)) {
                        Debug.LogWarning($"Reference key '{referenceKey}' " +
                        $"in Data Block [{dataKey}] is invalid! Ignoring.");
                    } else {
                        // Add New Mod Data Entry
                        if (logContent)
                            try {
                                Debug.Log($"Addendum Data (Dump/Mod): {JsonMapper.ToJson(dataBlock)}");
                            } catch (Exception ex) {
                                Exception inner = ex.InnerException;
                                Debug.LogWarning($"Addendum Data (Dump/Mod) for Data Block " +
                                $"[{dataKey}] has failed! Ignoring.\n{ex.Message}\n{ex.StackTrace}" + 
                                (inner != null ? $"\nInner: {inner.Message}\n{inner.StackTrace}" : ""));
                            }
                        try {
                            dataDict.Add(dataKey, dataBlock);
                        } catch (Exception ex) {
                            Exception inner = ex.InnerException;
                            Debug.LogWarning($"Modded Add of new Data Block [{dataKey}] " +
                            $"has failed! Ignoring.\n{ex.Message}\n{ex.StackTrace}" + (inner != null ?
                            $"\nInner: {inner.Message}\n{inner.StackTrace}" : ""));
                        }
                    }
                } else {
                    // Add New Core Data Entry
                    if (logSource)
                        try {
                            Debug.Log($"Addendum Data (Dump/Core): {JsonMapper.ToJson(dataBlock)}");
                        } catch (Exception ex) {
                            Exception inner = ex.InnerException;
                            Debug.LogWarning($"Addendum Data (Dump/Core) for Data Block " +
                            $"[{dataKey}] has failed! Ignoring.\n{ex.Message}\n{ex.StackTrace}" + 
                            (inner != null ? $"\nInner: {inner.Message}\n{inner.StackTrace}" : ""));
                        }
                    try {
                        dataDict.Add(dataKey, dataBlock);
                    } catch (Exception ex) {
                        Exception inner = ex.InnerException;
                        Debug.LogWarning($"Core Add of new Data Block [{dataKey}] " +
                        $"has failed! Ignoring.\n{ex.Message}\n{ex.StackTrace}" + (inner != null ?
                        $"\nInner: {inner.Message}\n{inner.StackTrace}" : ""));
                    }
                }
            }

            // Resetting Data Variables
            fileData = null;
            dataFile = null;
        } catch (Exception ex) {
            JsonLogger.ReportProblem(strFile, ReportTypes.SourceInfo);
            if (rawDump.Length > 1000) {
                rawDump = rawDump.Substring(rawDump.Length - 1000);
            }
            Debug.LogError(rawDump + "\n" + ex.Message + "\n" + ex.StackTrace.ToString());
        }

        // Specific File Dump
        if (strFile.IndexOf("osSGv1") >= 0) {
            Debug.Log(rawDump);
        }
    }

    public static void SyncDataSafe<TJson>(TJson currDataSet, TJson newDataSet, ref string rawDataSet, string dataType, string dataKey, bool extData, bool doLog = false) {
        Type currDataType = currDataSet.GetType();
        Type newDataType = newDataSet.GetType();

        // Iterate Over Properties
        foreach (PropertyInfo currProperty in currDataType.GetProperties()) {
            // Ignore Forbidden Property
            if (!currProperty.CanWrite || currProperty.Name.IsForbidden()) continue;

            // New Data Property Validation
            PropertyInfo newProperty = newDataType.GetProperty(currProperty.Name);
            if (newProperty != null) {
                bool doCurr = false;
                string refName = currProperty.Name;
                object newValue = newProperty.GetValue(newDataSet, null);
                object currValue = currProperty.GetValue(currDataSet, null);
                if (rawDataSet.IndexOf(refName) >= 0) {
                    try {
                        // Handle Dictionary Variables
                        if (currValue != null && newValue is IDictionary) {
                            SyncRecords(ref newValue, ref currValue, ref doCurr,
                                dataKey, refName, dataType, extData, doLog);
                        }
                        // Handle Array Variables
                        else if (currValue != null && newValue is string[]) {
                            SyncArrays(ref newValue, ref currValue,
                                dataKey, refName, extData, doLog);
                        }
                        // Handle Object Variables
                        else if (currValue != null && newValue is object[]) {
                            SyncObjects(ref newValue, ref currValue, ref doCurr,
                                dataKey, refName, dataType, extData, doLog);
                        }
                        // Handle Data Variables
                        else if (currValue != null && newValue is not string
                            && (newValue?.GetType().IsClass ?? false)) {
                            string rawSubData = JsonMapper.ToJson(currValue).Compressed();
                            SyncDataSafe(currValue, newValue, ref rawSubData,
                            dataType, $"{dataKey}/{refName}", extData, doLog);
                        }
                        // Handle Simple Variables
                        else if (doLog) {
                            Debug.Log($"#Info# Data Block [{dataKey}], Property " +
                            $"[{refName}]: {currValue.Sanitized()} => {newValue.Sanitized()}");
                        }
                        // Overwrite Existing Value
                        if (doCurr) currProperty.SetValue(currDataSet, currValue, null);
                        else currProperty.SetValue(currDataSet, newValue, null);
                    } catch (Exception ex) {
                        Exception inner = ex.InnerException;
                        Debug.LogWarning($"Value sync for Data Block [{dataKey}], Property " +
                        $"[{refName}] has failed! Ignoring.\n{ex.Message}\n{ex.StackTrace}" +
                        (inner != null ? $"\nInner: {inner.Message}\n{inner.StackTrace}" : ""));
                    }
                }
            }
        }

        // Iterate Over Fields
        BindingFlags fieldFlags = BindingFlags.Public | BindingFlags.Instance;
        foreach (FieldInfo currField in currDataType.GetFields(fieldFlags)) {
            // Ignore Forbidden Field
            if (currField.IsLiteral || currField.Name.IsForbidden()) continue;

            // New Data Field Validation
            FieldInfo newField = newDataType.GetField(currField.Name, fieldFlags);
            if (newField != null) {
                bool doCurr = false;
                string refName = currField.Name;
                object newValue = newField.GetValue(newDataSet);
                object currValue = currField.GetValue(currDataSet);
                if (rawDataSet.IndexOf(refName) >= 0) {
                    try {
                        // Handle Dictionary Variables
                        if (currValue != null && newValue is IDictionary) {
                            SyncRecords(ref newValue, ref currValue, ref doCurr,
                                dataKey, refName, dataType, extData, doLog);
                        }
                        // Handle Array Variables
                        else if (currValue != null && newValue is string[]) {
                            SyncArrays(ref newValue, ref currValue,
                                dataKey, refName, extData, doLog);
                        }
                        // Handle Object Variables
                        else if (currValue != null && newValue is object[]) {
                            SyncObjects(ref newValue, ref currValue, ref doCurr,
                                dataKey, refName, dataType, extData, doLog);
                        }
                        // Handle Data Variables
                        else if (currValue != null && newValue is not string
                            && (newValue?.GetType().IsClass ?? false)) {
                            string rawSubData = JsonMapper.ToJson(currValue).Compressed();
                            SyncDataSafe(currValue, newValue, ref rawSubData,
                            dataType, $"{dataKey}/{refName}", extData, doLog);
                        }
                        // Handle Simple Variables
                        else if (doLog) {
                            Debug.Log($"#Info# Data Block [{dataKey}], Property " +
                            $"[{refName}]: {currValue.Sanitized()} => {newValue.Sanitized()}");
                        }
                        // Overwrite Existing Value
                        if (doCurr) currField.SetValue(currDataSet, currValue);
                        else currField.SetValue(currDataSet, newValue);
                    } catch (Exception ex) {
                        Exception inner = ex.InnerException;
                        Debug.LogWarning($"Value sync for Data Block [{dataKey}], Property " +
                        $"[{refName}] has failed! Ignoring.\n{ex.Message}\n{ex.StackTrace}" + 
                        (inner != null ? $"\nInner: {inner.Message}\n{inner.StackTrace}" : ""));
                    }
                }
            }
        }
    }

    public static void SyncObjects(ref object newValue, ref object currValue, ref bool useCurrent, string dataKey, string propName, string dataType, bool extData, bool doLog) {
        useCurrent = true;
        List<object> newObjects = (newValue as Array).Cast<object>().ToList();
        List<object> currObjects = (currValue as Array).Cast<object>().ToList();

        // Perform Complex Object Operations
        foreach (var newObject in newObjects) {
            string targetID = newObject.GetIdentifier();
            if (string.IsNullOrEmpty(targetID)) {
                if (doLog) Debug.Log($"#Info# Data Block [{dataKey}], " +
                $"Property [{propName}]: {currValue} => {newValue}");
                useCurrent = false;
                return;
            }

            // Target Objects Identifier
            bool doRemove = targetID.StartsWith(FFU_BR_Defs.OPT_DEL);
            bool doReplace = targetID.StartsWith(FFU_BR_Defs.OPT_MOD);
            bool hasAction = doRemove || doReplace;
            if (hasAction) {
                targetID = targetID.Substring(1);
                newObject.SetIdentifier(targetID);
            }

            // Existing Object Operations
            int currIdx = currObjects.FindIndex(x => x.GetIdentifier() == targetID);
            if (currIdx >= 0) {
                string objID = currObjects[currIdx].GetName();
                objID = objID != targetID && objID != string.Empty ? $"{objID}:{targetID}" : targetID;
                if (doRemove) {

                    // Object Block Removal
                    if (doLog) Debug.Log($"#Info# Object [{objID}] was removed " +
                        $"from Data Block [{dataKey}/{propName}]");
                    currObjects.RemoveAt(currIdx);
                } else if (doReplace) {

                    // Object Block Override
                    if (doLog) Debug.Log($"#Info# Object [{objID}] was replaced " +
                        $"in Data Block [{dataKey}/{propName}]");
                    currObjects[currIdx] = newObject;
                } else {

                    // Sub-Object Handling
                    string rawDataSubSet = JsonMapper.ToJson(newObject);
                    SyncDataSafe(currObjects[currIdx], newObject, ref rawDataSubSet,
                    dataType, $"{dataKey}/{propName}/{objID}", extData, doLog);
                }
            } else {

                // New Object Addition
                string objID = newObject.GetName();
                objID = objID != targetID && objID != string.Empty ? $"{objID}:{targetID}" : targetID;
                if (doLog) Debug.Log($"#Info# Object [{objID}] " +
                    $"was added to Data Block [{dataKey}/{propName}]");
                currObjects.Add(newObject);
            }
        }

        // Updating Existing Objects
        Array currArray = Array.CreateInstance(currValue.GetType().GetElementType(), currObjects.Count);
        for (int i = 0; i < currObjects.Count; i++) currArray.SetValue(currObjects[i], i);
        currValue = currArray;
    }

    public static void SyncRecords(ref object newValue, ref object currValue, ref bool useCurrent, string dataKey, string propName, string dataType, bool extData, bool doLog) {
        useCurrent = true;
        IDictionary newDict = (IDictionary)newValue;
        IDictionary currDict = (IDictionary)currValue;
        Type[] valTypes = currValue.GetType().GetGenericArguments();
        Type valType = valTypes.Count() > 1 ? valTypes[1] : valTypes[0];
        bool isClassData = valType != typeof(string) && valType.IsClass;

        // Perform Dictionary Operations
        foreach (var newKey in newDict.Keys.Cast<object>().ToList()) {
            bool doRemove = newKey.ToString().StartsWith(FFU_BR_Defs.OPT_DEL);
            bool doReplace = newKey.ToString().StartsWith(FFU_BR_Defs.OPT_MOD);
            bool hasAction = doRemove || doReplace;
            object targetKey = hasAction ? newKey.ToString().Substring(1) : newKey;

            // Existing Records Handling
            if (currDict.Contains(targetKey)) {
                if (doRemove) {

                    // Record Block Removal
                    if (doLog) Debug.Log($"#Info# Property [{targetKey}] was removed " +
                        $"from Data Block [{dataKey}/{propName}]");
                    currDict.Remove(targetKey);
                } else if (doReplace) {

                    // Record Block Override
                    if (doLog) Debug.Log($"#Info# Property [{targetKey}] was replaced " +
                        $"in Data Block [{dataKey}/{propName}]");
                    currDict[targetKey] = newDict[newKey];
                } else if (isClassData) {

                    // Sub-Record Handling
                    string rawDataSubSet = JsonMapper.ToJson(newDict[targetKey]);
                    SyncDataSafe(currDict[targetKey], newDict[targetKey], ref rawDataSubSet,
                    dataType, $"{dataKey}/{propName}:{targetKey}", extData, doLog);
                } else {

                    // Record Overwrite
                    if (doLog) Debug.Log($"#Info# Data Block [{dataKey}/{propName}], " +
                        $"Property [{targetKey}]: {currDict[targetKey]} => {newDict[targetKey]}");
                    currDict[targetKey] = newDict[targetKey];
                }
            } else {

                // New Record Addition
                if (doLog) Debug.Log($"#Info# Property [{targetKey}], Value [{newDict[targetKey]}] " +
                    $"was added to Data Block [{dataKey}/{propName}]");
                currDict[targetKey] = newDict[targetKey];
            }
        }
    }

    public static void SyncArrays(ref object newValue, ref object currValue, string dataKey, string propName, bool extData, bool doLog) {
        SyncArrayOp defaultOp = extData ? SyncArrayOp.Add : SyncArrayOp.None;
        List<string> modArray = (currValue as string[]).ToList();
        List<string> refArray = (newValue as string[]).ToList();
        List<string> origArray = new(modArray);
        bool noArrayOps = true;

        // Perform Sub-Array Operations
        foreach (var refItem in refArray) {
            if (string.IsNullOrEmpty(refItem)) continue;
            if (!char.IsDigit(refItem[0]) || !refItem.Contains('|')) continue;

            // Invalid Sub-Arrays Ignored
            List<string> refSubArray = refItem.Split('|').ToList();
            int.TryParse(refSubArray[0], out int rowIndex);

            // Target Index Validation
            if (rowIndex > 0) {
                refSubArray.RemoveAt(0);
                List<string> modSubArray = modArray[rowIndex - 1].Split('|').ToList();
                SyncArrayOps(modSubArray, refSubArray, ref noArrayOps, dataKey, $"{propName}#{rowIndex}", doLog, defaultOp);
                if (noArrayOps) Debug.LogWarning($"You attempted to modify sub-array in Data Block " +
                    $"[{dataKey}], Property [{propName}#{rowIndex}], but performed no array operations. " +
                    $"Assume that something went horribly wrong and game is likely to crash.");
                modArray[rowIndex - 1] = string.Join("|", modSubArray.ToArray());
            }
        }

        // Perform Array Operations
        SyncArrayOps(modArray, refArray, ref noArrayOps, dataKey, propName, doLog, defaultOp);

        // Overwriting Existing Value
        if (noArrayOps) {
            if (doLog) Debug.Log($"#Info# Data Block [{dataKey}], " +
                $"Property [{propName}]: String[{origArray.Count}] => String[{refArray.Count}]");
            newValue = refArray.ToArray();
        } else newValue = modArray.ToArray();
    }

    public static void SyncArrayOps(List<string> modArray, List<string> refArray, ref bool noArrayOps, string dataKey, string propName, bool doLog, SyncArrayOp arrayOp = SyncArrayOp.None) {
        int opIndex = 0;
        // Array Operations Subroutine
        foreach (var refItem in refArray) {
            // Valid Sub-Arrays Ignored
            if (string.IsNullOrEmpty(refItem)) continue;
            if (char.IsDigit(refItem[0]) && refItem.Contains('|')) continue;

            // Get Operations Command
            if (refItem.StartsWith("--")) {
                switch (refItem.Substring(0, 7)) {
                    case OP_MOD: arrayOp = SyncArrayOp.Mod; break;
                    case OP_ADD: arrayOp = SyncArrayOp.Add; break;
                    case OP_INS: arrayOp = SyncArrayOp.Ins; break;
                    case OP_DEL: arrayOp = SyncArrayOp.Del; break;
                }
                if (arrayOp == SyncArrayOp.Ins) {
                    int.TryParse(refItem.Substring(7), out opIndex);
                    opIndex--;
                    if (opIndex < 0) {
                        Debug.LogWarning($"The '{OP_INS}' array operation in Data Block [{dataKey}], " +
                            $"Property [{propName}] received invalid index! Using [0] index.");
                        opIndex = 0;
                    }
                }
                continue;
            }

            // Operations Logical Check
            if (noArrayOps) noArrayOps = arrayOp == SyncArrayOp.None;
            if (noArrayOps) break;

            // Execute Array Operation
            string[] refVal = refItem.Split('=');
            bool isDataValue = refVal.Length == 2 && !refItem.Contains('|') && !string.IsNullOrEmpty(refVal[1]);
            if (isDataValue) {
                switch (arrayOp) {
                    case SyncArrayOp.Mod: OpModData(modArray, refItem, dataKey, propName, doLog); break;
                    case SyncArrayOp.Add: OpAddData(modArray, refItem, dataKey, propName, doLog); break;
                    case SyncArrayOp.Ins: OpInsData(modArray, ref opIndex, refItem, dataKey, propName, doLog); break;
                    case SyncArrayOp.Del: OpDelData(modArray, refItem, dataKey, propName, doLog); break;
                }
            } else {
                switch (arrayOp) {
                    case SyncArrayOp.Mod: Debug.LogWarning($"Non-data [{refItem}] in Data Block [{dataKey}], " +
                        $"Property [{propName}] doesn't support '{OP_MOD}' operation! Ignoring."); break;
                    case SyncArrayOp.Add: OpAddSimple(modArray, refItem, dataKey, propName, doLog); break;
                    case SyncArrayOp.Ins: OpInsSimple(modArray, ref opIndex, refItem, dataKey, propName, doLog); break;
                    case SyncArrayOp.Del: OpDelSimple(modArray, refItem, dataKey, propName, doLog); break;
                }
            }
        }
    }

    public static void OpModData (List<string> modArray, string refItem, string dataKey, string propName, bool doLog) {
        string[] refData = refItem.Split('=');
        bool isReplaced = false;
        for (int i = 0; i < modArray.Count; i++) {
            string[] itemData = modArray[i].Split('=');
            if (itemData[0] == refData[0]) {
                if (doLog) Debug.Log($"#Info# " +
                    $"Data Block [{dataKey}], Property [{propName}], Parameter " +
                    $"[{refData[0]}]: {itemData[1]} => {refData[1]}");
                modArray[i] = refItem;
                isReplaced = true;
                break;
            }
        }
        if (!isReplaced) Debug.LogWarning($"Parameter [{refData[0]}] was not " +
        $"found in Data Block [{dataKey}], Property [{propName}]! Ignoring.");
    }

    public static void OpAddData(List<string> modArray, string refItem, string dataKey, string propName, bool doLog) {
        string[] refData = refItem.Split('=');
        if (doLog) Debug.Log($"#Info# Parameter [{refData[0]}], Value [{refData[1]}] " +
            $"was added to Data Block [{dataKey}], Property [{propName}]");
        modArray.Add(refItem);
    }

    public static void OpInsData(List<string> modArray, ref int arrIndex, string refItem, string dataKey, string propName, bool doLog) {
        string[] refData = refItem.Split('=');
        if (doLog) Debug.Log($"#Info# Parameter [{refData[0]}], Value [{refData[1]}] was inserted " +
            $"into Data Block [{dataKey}], Property [{propName}] at Index [{arrIndex}]");
        if (arrIndex >= modArray.Count) {
            Debug.LogWarning($"Index [{arrIndex}] for Parameter [{refData[0]}] in Data " +
                $"Block [{dataKey}], Property [{propName}] is invalid! Adding instead.");
            modArray.Add(refItem);
        } else modArray.Insert(arrIndex, refItem);
        arrIndex++;
    }

    public static void OpDelData(List<string> modArray, string refItem, string dataKey, string propName, bool doLog) {
        string[] refData = refItem.Split('=');
        bool isFound = false;
        int removeIndex = 0;
        for (int i = 0; i < modArray.Count; i++) {
            string[] itemData = modArray[i].Split('=');
            if (itemData[0] == refData[0]) {
                removeIndex = i;
                isFound = true;
                break;
            }
        }
        if (isFound) {
            if (doLog) Debug.Log($"#Info# Parameter [{refData[0]}] was removed " +
                $"from Data Block [{dataKey}], Property [{propName}]");
            modArray.RemoveAt(removeIndex);
        } else {
            Debug.LogWarning($"Parameter [{refData[0]}] was not found " +
            $"in Data Block [{dataKey}], Property [{propName}]! Ignoring.");
        }
    }

    public static void OpAddSimple(List<string> modArray, string refItem, string dataKey, string propName, bool doLog) {
        if (doLog) Debug.Log($"#Info# Parameter [{refItem}] was added " +
            $"to Data Block [{dataKey}], Property [{propName}]");
        modArray.Add(refItem);
    }

    public static void OpInsSimple(List<string> modArray, ref int arrIndex, string refItem, string dataKey, string propName, bool doLog) {
        if (doLog) Debug.Log($"#Info# Parameter [{refItem}] was inserted into " +
            $"Data Block [{dataKey}], Property [{propName}] at Index [{arrIndex}]");
        if (arrIndex >= modArray.Count) {
            Debug.LogWarning($"Index [{arrIndex}] for Parameter [{refItem}] in Data " +
                $"Block [{dataKey}], Property [{propName}] is invalid! Adding instead.");
            modArray.Add(refItem);
        } else modArray.Insert(arrIndex, refItem);
        arrIndex++;
    }

    public static void OpDelSimple(List<string> modArray, string refItem, string dataKey, string propName, bool doLog) {
        bool isFound = false;
        int removeIndex = 0;
        for (int i = 0; i < modArray.Count; i++) {
            if (modArray[i].StartsWith(refItem)) {
                removeIndex = i;
                isFound = true;
                break;
            }
        }
        if (isFound) {
            if (doLog) Debug.Log($"#Info# Parameter [{refItem}] was removed " +
                $"from Data Block [{dataKey}], Property [{propName}]");
            modArray.RemoveAt(removeIndex);
        } else {
            Debug.LogWarning($"Parameter [{refItem}] was not found in " +
            $"Data Block [{dataKey}], Property [{propName}]!");
        }
    }

    public static string GetName(this object refObject) {
        BindingFlags fieldFlags = BindingFlags.Public | BindingFlags.Instance;
        Type objectType = refObject.GetType();
        string objectID = string.Empty;
        objectID = objectType.GetProperty("strName")?
            .GetValue(refObject, null)?
            .ToString() ?? string.Empty;
        if (string.IsNullOrEmpty(objectID)) objectID =
            objectType.GetField("strName", fieldFlags)?
            .GetValue(refObject)?.ToString() ?? string.Empty;
        return objectID;
    }

    public static string GetIdentifier(this object refObject) {
        BindingFlags fieldFlags = BindingFlags.Public | BindingFlags.Instance;
        Type objectType = refObject.GetType();
        string[] TypeIDs = ["strID", "strName"];
        string objectID = string.Empty;
        foreach (string TypeID in TypeIDs) {
            objectID = objectType.GetProperty(TypeID)?
                .GetValue(refObject, null)?
                .ToString() ?? string.Empty;
            if (string.IsNullOrEmpty(objectID)) objectID = 
                objectType.GetField(TypeID, fieldFlags)?
                .GetValue(refObject)?.ToString() ?? string.Empty;
            if (!string.IsNullOrEmpty(objectID)) break;
        }
        return objectID;
    }

    public static bool SetIdentifier(this object refObject, string newIdentifier) {
        BindingFlags fieldFlags = BindingFlags.Public | BindingFlags.Instance;
        Type objectType = refObject.GetType();
        string[] TypeIDs = ["strID", "strName"];
        bool isSuccess = false;
        foreach (string TypeID in TypeIDs) {
            PropertyInfo objProp = objectType.GetProperty(TypeID);
            if (objProp != null) {
                objProp.SetValue(refObject, newIdentifier, null);
                isSuccess = true;
                break;
            } else {
                FieldInfo objField = objectType.GetField(TypeID, fieldFlags);
                if (objField != null) {
                    objField.SetValue(refObject, newIdentifier);
                    isSuccess = true;
                    break;
                }
            }
        }
        return isSuccess;
    }

    public static bool IsForbidden(this string strProp) {
        return strProp switch {
            "strID" or "strName" or "strReference" => true,
            _ => false
        };
    }

    public static object Sanitized(this object refObject) {
        if (refObject is null) return "NULL";
        else if (refObject is string x && 
            x.Length == 0) return "EMPTY";
        else return refObject;
    }

    public static string Compressed(this string strValue) {
        return strValue.Replace("\n", "").Replace("\r", "").Replace("\t", "").Replace(" ", "");
    }

    public static bool IsCoreEntry(this string modKey) {
        return modKey == "core" || modKey == "Core";
    }

    public static bool IsExtedable(this string dataKey) {
        return dataKey switch {
            "conditions_simple" or
            "names_last" or
            "names_robots" or
            "names_first" or
            "names_full" or
            "manpages" or
            "traitscores" or
            "strings" or
            "crewskins" or
            "names_ship" or
            "names_ship_adjectives" or
            "names_ship_nouns" => true,
            _ => false
        };
    }

    public static bool TryGetCOValue(string strName, out JsonCondOwner refCO) {
        if (DataHandler.dictCOs.TryGetValue(strName, out JsonCondOwner coDict)) {
            refCO = coDict;
            return true;
        }
        if (DataHandler.dictCOOverlays.TryGetValue(strName, out JsonCOOverlay coOver)) {
            if (DataHandler.dictCOs.TryGetValue(coOver.strCOBase, out JsonCondOwner coOverDict)) {
                refCO = coOverDict;
                return true;
            }
        }
        refCO = null;
        return false;
    }

    public const string OP_MOD = "--MOD--";
    public const string OP_ADD = "--ADD--";
    public const string OP_INS = "--INS--";
    public const string OP_DEL = "--DEL--";

    public enum SyncArrayOp {
        None,
        Mod,
        Add,
        Ins,
        Del
    }
}

// Reference Output: ILSpy v9.1.0.7988 / C# 12.0 / 2022.8

/* DataHandler.Init
public static void Init()
{
	loadLog.Length = 0;
	loadLogError.Length = 0;
	loadLogWarning.Length = 0;
	if (bInitialised)
	{
		List<CondOwner> list = new List<CondOwner>(mapCOs.Values);
		foreach (CondOwner item in list)
		{
			if (!(item == null))
			{
				loadLogWarning.Append("Destroying leftover CO: ");
				loadLogWarning.Append(item.strName);
				loadLogWarning.AppendLine();
				item.Destroy();
			}
		}
		list.Clear();
		list = null;
		mapCOs.Clear();
		if (loadLogWarning.Length > 0)
		{
			Debug.LogWarning(loadLogWarning.ToString());
		}
		return;
	}
	strAssetPath = Application.streamingAssetsPath + "/";
	LoadBuildVersion();
	if ((bool)ObjReader.use)
	{
		ObjReader.use.scaleFactor = new Vector3(0.0625f, 0.0625f, 0.0625f);
		ObjReader.use.objRotation = new Vector3(90f, 0f, 180f);
	}
	SetupDicts();
	if (_interactionObjectTracker == null)
	{
		_interactionObjectTracker = new InteractionObjectTracker();
	}
	dictSettings["DefaultUserSettings"] = new JsonUserSettings();
	dictSettings["DefaultUserSettings"].Init();
	if (File.Exists(Application.persistentDataPath + "/settings.json"))
	{
		JsonToData(Application.persistentDataPath + "/settings.json", dictSettings);
	}
	else
	{
		loadLogWarning.Append("WARNING: settings.json not found. Resorting to default values.");
		loadLogWarning.AppendLine();
		dictSettings["UserSettings"] = new JsonUserSettings();
		dictSettings["UserSettings"].Init();
	}
	if (!dictSettings.ContainsKey("UserSettings") || dictSettings["UserSettings"] == null)
	{
		loadLogError.Append("ERROR: Malformed settings.json. Resorting to default values.");
		loadLogError.AppendLine();
		dictSettings["UserSettings"] = new JsonUserSettings();
		dictSettings["UserSettings"].Init();
	}
	dictSettings["DefaultUserSettings"].CopyTo(GetUserSettings());
	dictSettings.Remove("DefaultUserSettings");
	SaveUserSettings();
	bool flag = false;
	strModFolder = dictSettings["UserSettings"].strPathMods;
	if (strModFolder == null || strModFolder == string.Empty)
	{
		strModFolder = Path.Combine(Application.dataPath, "Mods/");
		loadLogWarning.Append("WARNING: Unrecognised mod folder. Setting mod path to ");
		loadLogWarning.Append(strModFolder);
		loadLogWarning.AppendLine();
	}
	string directoryName = Path.GetDirectoryName(strModFolder);
	directoryName = Path.Combine(directoryName, "loading_order.json");
	JsonModInfo jsonModInfo = new JsonModInfo();
	jsonModInfo.strName = "Core";
	dictModInfos["core"] = jsonModInfo;
	bool flag2 = ConsoleToGUI.instance != null;
	if (flag2)
	{
		ConsoleToGUI.instance.LogInfo("Attempting to load " + directoryName + "...");
	}
	if (File.Exists(directoryName))
	{
		if (flag2)
		{
			ConsoleToGUI.instance.LogInfo("loading_order.json found. Beginning mod load.");
		}
		JsonToData(directoryName, dictModList);
		JsonModList value = null;
		if (dictModList.TryGetValue("Mod Loading Order", out value))
		{
			if (value.aIgnorePatterns != null)
			{
				for (int i = 0; i < value.aIgnorePatterns.Length; i++)
				{
					value.aIgnorePatterns[i] = PathSanitize(value.aIgnorePatterns[i]);
				}
			}
			string[] aLoadOrder = value.aLoadOrder;
			foreach (string text in aLoadOrder)
			{
				flag = true;
				if (text == "core")
				{
					LoadMod(strAssetPath, value.aIgnorePatterns, jsonModInfo);
					continue;
				}
				if (text == null || text == string.Empty)
				{
					loadLogError.Append("ERROR: Invalid mod folder specified: ");
					loadLogError.Append(text);
					loadLogError.Append("; Skipping...");
					loadLogError.AppendLine();
					continue;
				}
				string text2 = text.TrimStart(Path.DirectorySeparatorChar);
				text2 = text.TrimStart(Path.AltDirectorySeparatorChar);
				text2 += "/";
				string directoryName2 = Path.GetDirectoryName(strModFolder);
				directoryName2 = Path.Combine(directoryName2, text2);
				Dictionary<string, JsonModInfo> dictionary = new Dictionary<string, JsonModInfo>();
				string text3 = Path.Combine(directoryName2, "mod_info.json");
				if (File.Exists(text3))
				{
					JsonToData(text3, dictionary);
				}
				if (dictionary.Count < 1)
				{
					JsonModInfo jsonModInfo2 = new JsonModInfo();
					jsonModInfo2.strName = text;
					dictionary[jsonModInfo2.strName] = jsonModInfo2;
					loadLogWarning.Append("WARNING: Missing mod_info.json in folder: ");
					loadLogWarning.Append(text);
					loadLogWarning.Append("; Using default name: ");
					loadLogWarning.Append(jsonModInfo2.strName);
					loadLogWarning.AppendLine();
				}
				using (Dictionary<string, JsonModInfo>.ValueCollection.Enumerator enumerator2 = dictionary.Values.GetEnumerator())
				{
					if (enumerator2.MoveNext())
					{
						JsonModInfo current2 = enumerator2.Current;
						dictModInfos[text] = current2;
						if (flag2)
						{
							ConsoleToGUI.instance.LogInfo("Loading mod: " + dictModInfos[text].strName + " from directory: " + text);
						}
					}
				}
				LoadMod(directoryName2, value.aIgnorePatterns, dictModInfos[text]);
			}
		}
	}
	if (!flag)
	{
		if (flag2)
		{
			ConsoleToGUI.instance.LogInfo("No loading_order.json found. Beginning default game data load from " + strAssetPath);
		}
		JsonModList jsonModList = new JsonModList();
		jsonModList.strName = "Default";
		jsonModList.aLoadOrder = new string[1] { "core" };
		jsonModList.aIgnorePatterns = new string[0];
		dictModList["Mod Loading Order"] = jsonModList;
		LoadMod(strAssetPath, jsonModList.aIgnorePatterns, jsonModInfo);
	}
	Application.SetStackTraceLogType(LogType.Log, StackTraceLogType.None);
	if (loadLog.Length > 0)
	{
		Debug.Log(loadLog.ToString());
	}
	Application.SetStackTraceLogType(LogType.Log, StackTraceLogType.ScriptOnly);
	if (loadLogWarning.Length > 0)
	{
		Debug.LogWarning(loadLogWarning.ToString());
	}
	if (loadLogError.Length > 0)
	{
		Debug.LogError(loadLogError.ToString());
	}
	bInitialised = true;
	if (InitComplete != null)
	{
		InitComplete();
	}
}
*/

/* DataHandler.LoadMod
private static void LoadMod(string strFolderPath, string[] aIgnorePatterns, JsonModInfo jmi)
{
	ModLoader modLoader = new ModLoader();
	modLoader.JsonModInfo = jmi;
	ModLoader modLoader2 = modLoader;
	LoadManager.LoadingQueue.Add(modLoader2);
	LoadManager.LastScheduledMod = modLoader2;
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
	aModPaths.Insert(0, strFolderPath);
	strFolderPath += "data/";
	LoadModJsons(strFolderPath + "ships/", dictShips, aIgnorePatterns);
	LoadModJsons(strFolderPath + "ads/", dictAds, aIgnorePatterns);
	LoadModJsons(strFolderPath + "ai_training/", dictAIPersonalities, aIgnorePatterns);
	LoadModJsons(strFolderPath + "attackmodes/", dictAModes, aIgnorePatterns);
	LoadModJsons(strFolderPath + "audioemitters/", dictAudioEmitters, aIgnorePatterns);
	LoadModJsons(strFolderPath + "careers/", dictCareers, aIgnorePatterns);
	LoadModJsons(strFolderPath + "chargeprofiles/", dictChargeProfiles, aIgnorePatterns);
	LoadModJsons(strFolderPath + "colors/", dictJsonColors, aIgnorePatterns);
	LoadModJsons(strFolderPath + "conditions/", dictConds, aIgnorePatterns);
	Dictionary<string, JsonSimple> condsSimple = new Dictionary<string, JsonSimple>();
	LoadModJsons(strFolderPath + "conditions_simple/", condsSimple, aIgnorePatterns);
	LoadModJsons(strFolderPath + "condowners/", dictCOs, aIgnorePatterns);
	LoadModJsons(strFolderPath + "condrules/", dictCondRules, aIgnorePatterns);
	LoadModJsons(strFolderPath + "condtrigs/", dictCTs, aIgnorePatterns);
	LoadModJsons(strFolderPath + "context/", dictContext, aIgnorePatterns);
	LoadModJsons(strFolderPath + "cooverlays/", dictCOOverlays, aIgnorePatterns);
	Dictionary<string, JsonSimple> dictSimpleCrewSkins = new Dictionary<string, JsonSimple>();
	LoadModJsons(strFolderPath + "crewskins/", dictSimpleCrewSkins, aIgnorePatterns);
	LoadModJsons(strFolderPath + "crime/", dictCrimes, aIgnorePatterns);
	LoadModJsons(strFolderPath + "gasrespires/", dictGasRespires, aIgnorePatterns);
	LoadModJsons(strFolderPath + "guipropmaps/", dictGUIPropMapUnparsed, aIgnorePatterns);
	LoadModJsons(strFolderPath + "headlines/", dictHeadlines, aIgnorePatterns);
	LoadModJsons(strFolderPath + "homeworlds/", dictHomeworlds, aIgnorePatterns);
	LoadModJsons(strFolderPath + "info/", dictInfoNodes, aIgnorePatterns);
	LoadModJsons(strFolderPath + "installables/", dictInstallables, aIgnorePatterns);
	LoadModJsons(strFolderPath + "interaction_overrides/", dictIAOverrides, aIgnorePatterns);
	LoadModJsons(strFolderPath + "interactions/", dictInteractions, aIgnorePatterns);
	LoadModJsons(strFolderPath + "items/", dictItemDefs, aIgnorePatterns);
	LoadModJsons(strFolderPath + "jobitems/", dictJobitems, aIgnorePatterns);
	LoadModJsons(strFolderPath + "jobs/", dictJobs, aIgnorePatterns);
	LoadModJsons(strFolderPath + "ledgerdefs/", dictLedgerDefs, aIgnorePatterns);
	LoadModJsons(strFolderPath + "lifeevents/", dictLifeEvents, aIgnorePatterns);
	LoadModJsons(strFolderPath + "lights/", dictLights, aIgnorePatterns);
	LoadModJsons(strFolderPath + "loot/", dictLoot, aIgnorePatterns);
	Dictionary<string, JsonSimple> dictSimpleManPages = new Dictionary<string, JsonSimple>();
	LoadModJsons(strFolderPath + "manpages/", dictSimpleManPages, aIgnorePatterns);
	LoadModJsons(strFolderPath + "market/Markets/", dictMarketConfigs, aIgnorePatterns);
	LoadModJsons(strFolderPath + "market/CoCollections/", dictSupersTemp, aIgnorePatterns);
	LoadModJsons(strFolderPath + "market/Production/", dictProductionMaps, aIgnorePatterns);
	LoadModJsons(strFolderPath + "market/CargoSpecs/", dictCargoSpecs, aIgnorePatterns);
	LoadModJsons(strFolderPath + "music/", dictMusic, aIgnorePatterns);
	Dictionary<string, JsonSimple> dictFirst = new Dictionary<string, JsonSimple>();
	LoadModJsons(strFolderPath + "names_first/", dictFirst, aIgnorePatterns);
	Dictionary<string, JsonSimple> dictFull = new Dictionary<string, JsonSimple>();
	LoadModJsons(strFolderPath + "names_full/", dictFull, aIgnorePatterns);
	Dictionary<string, JsonSimple> dictLast = new Dictionary<string, JsonSimple>();
	LoadModJsons(strFolderPath + "names_last/", dictLast, aIgnorePatterns);
	Dictionary<string, JsonSimple> dictRobots = new Dictionary<string, JsonSimple>();
	LoadModJsons(strFolderPath + "names_robots/", dictRobots, aIgnorePatterns);
	Dictionary<string, JsonSimple> dictShipNames = new Dictionary<string, JsonSimple>();
	LoadModJsons(strFolderPath + "names_ship/", dictShipNames, aIgnorePatterns);
	Dictionary<string, JsonSimple> dictShipAdjectives = new Dictionary<string, JsonSimple>();
	LoadModJsons(strFolderPath + "names_ship_adjectives/", dictShipAdjectives, aIgnorePatterns);
	Dictionary<string, JsonSimple> dictShipNouns = new Dictionary<string, JsonSimple>();
	LoadModJsons(strFolderPath + "names_ship_nouns/", dictShipNouns, aIgnorePatterns);
	LoadModJsons(strFolderPath + "parallax/", dictParallax, aIgnorePatterns);
	LoadModJsons(strFolderPath + "pda_apps/", dictPDAAppIcons, aIgnorePatterns);
	LoadModJsons(strFolderPath + "personspecs/", dictPersonSpecs, aIgnorePatterns);
	LoadModJsons(strFolderPath + "pledges/", dictPledges, aIgnorePatterns);
	LoadModJsons(strFolderPath + "plot_beat_overrides/", dictPlotBeatOverrides, aIgnorePatterns);
	LoadModJsons(strFolderPath + "plot_beats/", dictPlotBeats, aIgnorePatterns);
	LoadModJsons(strFolderPath + "plot_manager/", dictPlotManager, aIgnorePatterns);
	LoadModJsons(strFolderPath + "plots/", dictPlots, aIgnorePatterns);
	LoadModJsons(strFolderPath + "powerinfos/", dictPowerInfo, aIgnorePatterns);
	LoadModJsons(strFolderPath + "racing/leagues/", dictRacingLeagues, aIgnorePatterns);
	LoadModJsons(strFolderPath + "racing/tracks/", dictRaceTracks, aIgnorePatterns);
	LoadModJsons(strFolderPath + "rooms/", dictRoomSpecsTemp, aIgnorePatterns);
	LoadModJsons(strFolderPath + "shipspecs/", dictShipSpecs, aIgnorePatterns);
	LoadModJsons(strFolderPath + "slot_effects/", dictSlotEffects, aIgnorePatterns);
	LoadModJsons(strFolderPath + "slots/", dictSlots, aIgnorePatterns);
	LoadModJsons(strFolderPath + "star_systems/", dictStarSystems, aIgnorePatterns);
	Dictionary<string, JsonSimple> dictStringsTemp = new Dictionary<string, JsonSimple>();
	LoadModJsons(strFolderPath + "strings/", dictStringsTemp, aIgnorePatterns);
	LoadModJsons(strFolderPath + "tickers/", dictTickers, aIgnorePatterns);
	LoadModJsons(strFolderPath + "tips/", dictTips, aIgnorePatterns);
	LoadModJsons(strFolderPath + "tokens/", dictJsonTokens, aIgnorePatterns);
	Dictionary<string, JsonSimple> dictTraitsTemp = new Dictionary<string, JsonSimple>();
	LoadModJsons(strFolderPath + "traitscores/", dictTraitsTemp, aIgnorePatterns);
	LoadModJsons(strFolderPath + "transit/", dictTransit, aIgnorePatterns);
	LoadModJsons(strFolderPath + "verbs/", dictJsonVerbs, aIgnorePatterns);
	LoadModJsons(strFolderPath + "wounds/", dictWounds, aIgnorePatterns);
	LoadModJsons(strFolderPath + "zone_triggers/", dictZoneTriggers, aIgnorePatterns);
	modLoader2.PerModPostLoadAsyncOkay.Add(delegate
	{
		ParseGUIPropMaps();
	});
	modLoader2.PerModPostLoadAsyncOkay.Add(delegate
	{
		ParseConditionsSimple(condsSimple);
	});
	modLoader2.PerModPostLoadAsyncOkay.Add(delegate
	{
		ParseTraitScores(dictTraitsTemp);
	});
	modLoader2.PerModPostLoadAsyncOkay.Add(delegate
	{
		ParseSimpleIntoStringDict(dictStringsTemp, dictStrings);
	});
	modLoader2.PerModPostLoadAsyncOkay.Add(delegate
	{
		ParseSimpleIntoStringDict(dictSimpleCrewSkins, dictCrewSkins);
	});
	modLoader2.PerModPostLoadAsyncOkay.Add(delegate
	{
		ParseSimpleIntoStringDict(dictFirst, dictNamesFirst);
	});
	modLoader2.PerModPostLoadAsyncOkay.Add(delegate
	{
		ParseSimpleIntoStringDict(dictFull, dictNamesFull);
	});
	modLoader2.PerModPostLoadAsyncOkay.Add(delegate
	{
		ParseSimpleIntoStringDict(dictLast, dictNamesLast);
	});
	modLoader2.PerModPostLoadAsyncOkay.Add(delegate
	{
		ParseSimpleIntoStringDict(dictRobots, dictNamesRobots);
	});
	modLoader2.PerModPostLoadAsyncOkay.Add(delegate
	{
		ParseSimpleIntoStringDict(dictShipNames, dictNamesShip);
	});
	modLoader2.PerModPostLoadAsyncOkay.Add(delegate
	{
		ParseSimpleIntoStringDict(dictShipNouns, dictNamesShipNouns);
	});
	modLoader2.PerModPostLoadAsyncOkay.Add(delegate
	{
		ParseSimpleIntoStringDict(dictShipAdjectives, dictNamesShipAdjectives);
	});
	modLoader2.PerModPostLoadAsyncOkay.Add(delegate
	{
		ParseManPages(dictSimpleManPages);
	});
	modLoader2.PerModPostLoadAsyncOkay.Add(delegate
	{
		ParseMusic();
	});
	if (jmi.Status == GUIModRow.ModStatus.Missing)
	{
		jmi.Status = GUIModRow.ModStatus.Missing;
	}
	else if ((bool)ConsoleToGUI.instance && num < ConsoleToGUI.instance.ErrorCount)
	{
		jmi.Status = GUIModRow.ModStatus.Error;
	}
	else
	{
		jmi.Status = GUIModRow.ModStatus.Loaded;
	}
}
*/

/* DataHandler.LoadModJsons
public static void LoadModJsons<TJson>(string strFolderPath, Dictionary<string, TJson> dict, string[] aIgnorePatterns)
{
	if (!Directory.Exists(strFolderPath))
	{
		return;
	}
	string[] files = Directory.GetFiles(strFolderPath, "*.json", SearchOption.AllDirectories);
	string[] array = files;
	foreach (string strIn in array)
	{
		string strFileTemp = PathSanitize(strIn);
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
			continue;
		}
		FileLoader fileLoader = ((typeof(TJson) != typeof(JsonShip)) ? LoadManager.LastScheduledMod.AddDelegate(delegate
		{
			JsonToData(strFileTemp, dict);
		}) : LoadManager.LastScheduledMod.AddShip(delegate
		{
			JsonToData(strFileTemp, dict);
		}));
		fileLoader.fileName = strFileTemp;
	}
}
*/

/* DataHandler.JsonToData
public static void JsonToData<TJson>(string strFile, Dictionary<string, TJson> dict)
{
	StringBuilder stringBuilder = new StringBuilder(70);
	stringBuilder.Length = 0;
	try
	{
		string json = File.ReadAllText(strFile, Encoding.UTF8);
		stringBuilder.AppendLine("Converting json into Array...");
		TJson[] array = JsonMapper.ToObject<TJson[]>(json);
		TJson[] array2 = array;
		for (int i = 0; i < array2.Length; i++)
		{
			TJson val = array2[i];
			stringBuilder.Append("Getting key: ");
			string text = null;
			Type type = val.GetType();
			PropertyInfo property = type.GetProperty("strName");
			if (property == null)
			{
				JsonLogger.ReportProblem("strName is missing", ReportTypes.FailingString);
			}
			object value = property.GetValue(val, null);
			text = value.ToString();
			stringBuilder.AppendLine(text);
			lock (dictWriteLock)
			{
				if (!dict.TryAdd(text, val))
				{
					dict[text] = val;
				}
			}
		}
		array = null;
		json = null;
	}
	catch (Exception ex)
	{
		object obj = new object();
		lock (obj)
		{
			LoadManager.JsonLogErrorExceptions.Add(delegate
			{
				JsonLogger.ReportProblem(strFile, ReportTypes.SourceInfo);
			});
		}
		string text2 = ((stringBuilder.Length <= 1000) ? stringBuilder.ToString() : stringBuilder.ToString(stringBuilder.Length - 1000, 1000));
		Debug.LogError(text2 + "\n" + ex.Message + "\n" + ex.StackTrace.ToString());
	}
	if (strFile.IndexOf("osSGv1") >= 0)
	{
		Debug.Log(stringBuilder);
	}
}
*/