using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using FFU_Beyond_Reach;
using LitJson;
using MonoMod;
using Ostranauts.Core;
using Ostranauts.Tools;
using UnityEngine;

// Core loader replacement for FFU Beyond Reach.
// This patch reorders and extends DataHandler startup so mods can partially
// overwrite data, clone by reference, remove ids, and sync saved/template COs.
public static class patch_DataHandler
{
	// Replaces the vanilla data bootstrap with FFU_BR's synchronized load flow.
	// This is the main entrypoint for extended JSON layering, removeIds support,
	// and deferred save/template reconciliation via the shared changes map.
	[MonoModReplace]
	public static void Init()
	{
		DataHandler.loadLog.Length = 0;
		DataHandler.loadLogError.Length = 0;
		DataHandler.loadLogWarning.Length = 0;
		bool bInitialised = DataHandler.bInitialised;
		if (bInitialised)
		{
			List<CondOwner> list = new List<CondOwner>(DataHandler.mapCOs.Values);
			foreach (CondOwner condOwner in list)
			{
				bool flag = !(condOwner == null);
				if (flag)
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
			bool flag2 = DataHandler.loadLogWarning.Length > 0;
			if (flag2)
			{
				Debug.LogWarning(DataHandler.loadLogWarning.ToString());
			}
		}
		else
		{
			DataHandler.strAssetPath = Application.streamingAssetsPath + "/";
			DataHandler.LoadBuildVersion();
			string str = "Early Access Build: ";
			TextAsset textAsset = (TextAsset)Resources.Load("version", typeof(TextAsset));
			Debug.Log(str + ((textAsset != null) ? textAsset.text : null));
			FFU_BR_Defs.InitConfig();
			bool flag3 = ObjReader.use;
			if (flag3)
			{
				ObjReader.use.scaleFactor = new Vector3(0.0625f, 0.0625f, 0.0625f);
				ObjReader.use.objRotation = new Vector3(90f, 0f, 180f);
			}
			patch_DataHandler.dictChangesMap = new Dictionary<string, Dictionary<string, List<string>>>();
			DataHandler.SetupDicts();
			if (DataHandler._interactionObjectTracker == null)
			{
				DataHandler._interactionObjectTracker = new InteractionObjectTracker();
			}
			DataHandler.dictSettings["DefaultUserSettings"] = new JsonUserSettings();
			DataHandler.dictSettings["DefaultUserSettings"].Init();
			bool flag4 = File.Exists(Application.persistentDataPath + "/settings.json");
			if (flag4)
			{
				DataHandler.JsonToData<JsonUserSettings>(Application.persistentDataPath + "/settings.json", DataHandler.dictSettings);
			}
			else
			{
				DataHandler.loadLogWarning.Append("WARNING: settings.json not found. Resorting to default values.");
				DataHandler.loadLogWarning.AppendLine();
				DataHandler.dictSettings["UserSettings"] = new JsonUserSettings();
				DataHandler.dictSettings["UserSettings"].Init();
			}
			bool flag5 = !DataHandler.dictSettings.ContainsKey("UserSettings") || DataHandler.dictSettings["UserSettings"] == null;
			if (flag5)
			{
				DataHandler.loadLogError.Append("ERROR: Malformed settings.json. Resorting to default values.");
				DataHandler.loadLogError.AppendLine();
				DataHandler.dictSettings["UserSettings"] = new JsonUserSettings();
				DataHandler.dictSettings["UserSettings"].Init();
			}
			DataHandler.dictSettings["DefaultUserSettings"].CopyTo(DataHandler.GetUserSettings());
			DataHandler.dictSettings.Remove("DefaultUserSettings");
			DataHandler.SaveUserSettings();
			bool flag6 = false;
			DataHandler.strModFolder = DataHandler.dictSettings["UserSettings"].strPathMods;
			bool flag7 = DataHandler.strModFolder == null || DataHandler.strModFolder == string.Empty;
			if (flag7)
			{
				DataHandler.strModFolder = Path.Combine(Application.dataPath, "Mods/");
				DataHandler.loadLogWarning.Append("WARNING: Unrecognized mod folder. Setting mod path to ");
				DataHandler.loadLogWarning.Append(DataHandler.strModFolder);
				DataHandler.loadLogWarning.AppendLine();
			}
			patch_DataHandler.strModsPath = DataHandler.strModFolder.Replace("loading_order.json", string.Empty);
			string text = Path.GetDirectoryName(DataHandler.strModFolder);
			text = Path.Combine(text, "loading_order.json");
			JsonModInfo jsonModInfo = new JsonModInfo();
			jsonModInfo.strName = "Core";
			DataHandler.dictModInfos["core"] = jsonModInfo;
			bool flag8 = ConsoleToGUI.instance != null;
			bool flag9 = flag8;
			if (flag9)
			{
				ConsoleToGUI.instance.LogInfo("Attempting to load " + text + "...");
			}
			bool flag10 = File.Exists(text);
			if (flag10)
			{
				bool flag11 = flag8;
				if (flag11)
				{
					ConsoleToGUI.instance.LogInfo("loading_order.json found. Beginning mod load.");
				}
				DataHandler.JsonToData<JsonModList>(text, DataHandler.dictModList);
				JsonModList jsonModList = null;
				bool flag12 = DataHandler.dictModList.TryGetValue("Mod Loading Order", out jsonModList);
				if (flag12)
				{
					bool flag13 = jsonModList.aIgnorePatterns != null;
					if (flag13)
					{
						for (int i = 0; i < jsonModList.aIgnorePatterns.Length; i++)
						{
							jsonModList.aIgnorePatterns[i] = DataHandler.PathSanitize(jsonModList.aIgnorePatterns[i]);
						}
					}
					string[] aLoadOrder = jsonModList.aLoadOrder;
					foreach (string text2 in aLoadOrder)
					{
						flag6 = true;
						bool flag14 = text2.IsCoreEntry();
						if (flag14)
						{
							string text3 = "Loading core files: " + DataHandler.strAssetPath;
							DataHandler.loadLog.Append(text3);
							DataHandler.loadLog.AppendLine();
							Debug.Log("#Info# " + text3);
						}
						else
						{
							bool flag15 = text2 == null || text2 == string.Empty;
							if (flag15)
							{
								DataHandler.loadLogError.Append("ERROR! Invalid mod folder specified: ");
								DataHandler.loadLogError.Append(text2);
								DataHandler.loadLogError.Append("; Skipping...");
								DataHandler.loadLogError.AppendLine();
							}
							else
							{
								string path = text2.TrimStart(new char[]
								{
									Path.DirectorySeparatorChar
								});
								path = text2.TrimStart(new char[]
								{
									Path.AltDirectorySeparatorChar
								}) + "/";
								string path2 = Path.GetDirectoryName(DataHandler.strModFolder);
								path2 = Path.Combine(path2, path);
								Dictionary<string, JsonModInfo> dictionary = new Dictionary<string, JsonModInfo>();
								string text4 = Path.Combine(path2, "mod_info.json");
								bool flag16 = File.Exists(text4);
								if (flag16)
								{
									DataHandler.JsonToData<JsonModInfo>(text4, dictionary);
								}
								bool flag17 = dictionary.Count < 1;
								if (flag17)
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
									bool flag18 = enumerator2.MoveNext();
									if (flag18)
									{
										JsonModInfo value = enumerator2.Current;
										DataHandler.dictModInfos[text2] = value;
										string text5 = "Loading mod '" + DataHandler.dictModInfos[text2].strName + "' from directory: " + text2;
										bool flag19 = flag8;
										if (flag19)
										{
											ConsoleToGUI.instance.LogInfo(text5);
										}
										else
										{
											Debug.Log("#Info# " + text5);
										}
									}
								}
							}
						}
					}
				}
				patch_DataHandler.SyncLoadMods(jsonModList.aIgnorePatterns);
			}
			bool flag20 = !flag6;
			if (flag20)
			{
				string text6 = "No loading_order.json found. Loading default game data from " + DataHandler.strAssetPath;
				bool flag21 = flag8;
				if (flag21)
				{
					ConsoleToGUI.instance.LogInfo(text6);
				}
				Debug.Log("#Info# " + text6);
				JsonModList jsonModList2 = new JsonModList();
				jsonModList2.strName = "Default";
				jsonModList2.aLoadOrder = new string[]
				{
					"core"
				};
				jsonModList2.aIgnorePatterns = new string[0];
				DataHandler.dictModList["Mod Loading Order"] = jsonModList2;
				patch_DataHandler.SyncLoadMods(jsonModList2.aIgnorePatterns);
			}
			bool modSyncLoading = FFU_BR_Defs.ModSyncLoading;
			if (modSyncLoading)
			{
				foreach (KeyValuePair<string, JsonShip> keyValuePair in DataHandler.dictShips)
				{
					patch_DataHandler.SwitchSlottedItems(keyValuePair.Value, true);
					patch_DataHandler.RecoverMissingItems(keyValuePair.Value);
				}
			}
			Application.SetStackTraceLogType(3, 0);
			bool flag22 = DataHandler.loadLog.Length > 0;
			if (flag22)
			{
				Debug.Log(DataHandler.loadLog.ToString());
			}
			Application.SetStackTraceLogType(3, 1);
			bool flag23 = DataHandler.loadLogWarning.Length > 0;
			if (flag23)
			{
				Debug.LogWarning(DataHandler.loadLogWarning.ToString());
			}
			bool flag24 = DataHandler.loadLogError.Length > 0;
			if (flag24)
			{
				Debug.LogError(DataHandler.loadLogError.ToString());
			}
			DataHandler.bInitialised = true;
			bool flag25 = DataHandler.InitComplete != null;
			if (flag25)
			{
				DataHandler.InitComplete();
			}
		}
	}
	private static void SyncLoadMods(string[] aIgnorePatterns)
	{
		foreach (KeyValuePair<string, JsonModInfo> keyValuePair in DataHandler.dictModInfos)
		{
			bool flag = keyValuePair.Key.IsCoreEntry();
			if (!flag)
			{
				string text = Path.Combine(patch_DataHandler.strModsPath, keyValuePair.Key);
				bool flag2 = Directory.Exists(Path.Combine(text, "data/"));
				if (flag2)
				{
					Debug.Log("Data Mod Queued: " + keyValuePair.Key + " => " + text);
				}
				else
				{
					bool flag3 = Directory.GetDirectories(text).Length != 0;
					if (flag3)
					{
						Debug.Log("Asset Mod Queued: " + keyValuePair.Key + " => " + text);
					}
					else
					{
						bool flag4 = File.Exists(Path.Combine(text, "mod_info.json"));
						if (flag4)
						{
							Debug.Log("Patch Mod Queued: " + keyValuePair.Key + " => " + text);
						}
						else
						{
							Debug.LogWarning("Attempted to queue invalid mod: " + keyValuePair.Key);
						}
					}
				}
			}
		}
		bool flag5 = ConsoleToGUI.instance != null;
		int num = 0;
		bool flag6 = flag5;
		if (flag6)
		{
			num = ConsoleToGUI.instance.ErrorCount;
			ConsoleToGUI.instance.LogInfo("Begin loading data from these paths:");
			foreach (KeyValuePair<string, JsonModInfo> keyValuePair2 in DataHandler.dictModInfos)
			{
				bool flag7 = keyValuePair2.Key.IsCoreEntry();
				if (!flag7)
				{
					string path = Path.Combine(patch_DataHandler.strModsPath, keyValuePair2.Key);
					string text2 = Path.Combine(path, "data/");
					bool flag8 = Directory.Exists(text2);
					if (flag8)
					{
						ConsoleToGUI.instance.LogInfo(text2);
					}
				}
			}
		}
		foreach (KeyValuePair<string, patch_JsonModInfo> keyValuePair3 in patch_DataHandler.dictModInfos)
		{
			bool flag9 = keyValuePair3.Key.IsCoreEntry();
			if (!flag9)
			{
				patch_JsonModInfo value = keyValuePair3.Value;
				bool flag10 = ((value != null) ? value.changesMap : null) != null;
				if (flag10)
				{
					foreach (KeyValuePair<string, Dictionary<string, string[]>> keyValuePair4 in keyValuePair3.Value.changesMap)
					{
						bool flag11 = !patch_DataHandler.dictChangesMap.ContainsKey(keyValuePair4.Key);
						if (flag11)
						{
							patch_DataHandler.dictChangesMap[keyValuePair4.Key] = new Dictionary<string, List<string>>();
						}
						bool flag12 = keyValuePair4.Value != null;
						if (flag12)
						{
							foreach (KeyValuePair<string, string[]> keyValuePair5 in keyValuePair4.Value)
							{
								bool flag13 = keyValuePair5.Key.StartsWith("!");
								string text3 = flag13 ? keyValuePair5.Key.Substring(1) : keyValuePair5.Key;
								bool flag14 = text3 != "~";
								if (flag14)
								{
									bool flag15 = !patch_DataHandler.dictChangesMap[keyValuePair4.Key].ContainsKey(text3);
									if (flag15)
									{
										patch_DataHandler.dictChangesMap[keyValuePair4.Key][text3] = new List<string>();
									}
									bool flag16 = flag13 && !patch_DataHandler.dictChangesMap[keyValuePair4.Key][text3].Contains("*IsInverse*");
									if (flag16)
									{
										patch_DataHandler.dictChangesMap[keyValuePair4.Key][text3].Add("*IsInverse*");
									}
									bool flag17 = keyValuePair5.Value != null && keyValuePair5.Value.Length != 0;
									if (flag17)
									{
										bool flag18 = keyValuePair5.Value[0] != "~";
										if (flag18)
										{
											string[] value2 = keyValuePair5.Value;
											int i = 0;
											while (i < value2.Length)
											{
												string text4 = value2[i];
												bool flag19 = text4.StartsWith("-");
												if (flag19)
												{
													string cleanEntry = text4.Substring(1);
													string text5 = patch_DataHandler.dictChangesMap[keyValuePair4.Key][text3].Find((string x) => x.StartsWith(cleanEntry));
													bool flag20 = !string.IsNullOrEmpty(text5);
													if (flag20)
													{
														patch_DataHandler.dictChangesMap[keyValuePair4.Key][text3].Remove(text5);
													}
												}
												else
												{
													bool flag21 = text4.StartsWith("*");
													if (flag21)
													{
														string text6 = text4.Substring(1);
														string lookupKey = text6.Contains("|") ? text6.Split(new char[]
														{
															"|"[0]
														})[0] : (text6.Contains("=") ? text6.Split(new char[]
														{
															"="[0]
														})[0] : text6);
														string text7 = patch_DataHandler.dictChangesMap[keyValuePair4.Key][text3].Find((string x) => x.StartsWith(lookupKey));
														bool flag22 = !string.IsNullOrEmpty(text7);
														if (flag22)
														{
															int index = patch_DataHandler.dictChangesMap[keyValuePair4.Key][text3].IndexOf(text7);
															patch_DataHandler.dictChangesMap[keyValuePair4.Key][text3][index] = text6;
														}
													}
													else
													{
														patch_DataHandler.dictChangesMap[keyValuePair4.Key][text3].Add(text4);
													}
												}
												IL_5A9:
												i++;
												continue;
												goto IL_5A9;
											}
										}
										else
										{
											patch_DataHandler.dictChangesMap[keyValuePair4.Key].Remove(text3);
										}
									}
								}
								else
								{
									patch_DataHandler.dictChangesMap.Remove(keyValuePair4.Key);
									bool flag23 = keyValuePair4.Value.Count > 1;
									if (!flag23)
									{
										break;
									}
									patch_DataHandler.dictChangesMap[keyValuePair4.Key] = new Dictionary<string, List<string>>();
								}
							}
						}
					}
				}
			}
		}
		bool flag24 = FFU_BR_Defs.SyncLogging >= FFU_BR_Defs.SyncLogs.ModdedDump;
		if (flag24)
		{
			Debug.Log("Dynamic Changes Map (Dump): " + JsonMapper.ToJson(patch_DataHandler.dictChangesMap));
		}
		foreach (KeyValuePair<string, JsonModInfo> keyValuePair6 in DataHandler.dictModInfos)
		{
			bool flag25 = keyValuePair6.Key.IsCoreEntry();
			if (flag25)
			{
				DataHandler.aModPaths.Insert(0, DataHandler.strAssetPath);
			}
			else
			{
				DataHandler.aModPaths.Insert(0, Path.Combine(patch_DataHandler.strModsPath, keyValuePair6.Key) + "/");
			}
		}
		Dictionary<string, JsonSimple> dictionary = new Dictionary<string, JsonSimple>();
		Dictionary<string, JsonSimple> dictionary2 = new Dictionary<string, JsonSimple>();
		Dictionary<string, JsonSimple> dictionary3 = new Dictionary<string, JsonSimple>();
		Dictionary<string, JsonSimple> dictionary4 = new Dictionary<string, JsonSimple>();
		Dictionary<string, JsonSimple> dictionary5 = new Dictionary<string, JsonSimple>();
		Dictionary<string, JsonSimple> dictionary6 = new Dictionary<string, JsonSimple>();
		Dictionary<string, JsonSimple> dictionary7 = new Dictionary<string, JsonSimple>();
		Dictionary<string, JsonSimple> dictionary8 = new Dictionary<string, JsonSimple>();
		Dictionary<string, JsonSimple> dictionary9 = new Dictionary<string, JsonSimple>();
		Dictionary<string, JsonSimple> dictionary10 = new Dictionary<string, JsonSimple>();
		Dictionary<string, JsonSimple> dictionary11 = new Dictionary<string, JsonSimple>();
		Dictionary<string, JsonSimple> dictionary12 = new Dictionary<string, JsonSimple>();
		foreach (KeyValuePair<string, patch_JsonModInfo> refModInfo in patch_DataHandler.dictModInfos)
		{
			patch_DataHandler.SyncLoadJSONs<JsonShip>(refModInfo, "ships/", DataHandler.dictShips, aIgnorePatterns);
		}
		foreach (KeyValuePair<string, patch_JsonModInfo> refModInfo2 in patch_DataHandler.dictModInfos)
		{
			patch_DataHandler.SyncLoadJSONs<JsonAd>(refModInfo2, "ads/", DataHandler.dictAds, aIgnorePatterns);
		}
		foreach (KeyValuePair<string, patch_JsonModInfo> refModInfo3 in patch_DataHandler.dictModInfos)
		{
			patch_DataHandler.SyncLoadJSONs<JsonAIPersonality>(refModInfo3, "ai_training/", DataHandler.dictAIPersonalities, aIgnorePatterns);
		}
		foreach (KeyValuePair<string, patch_JsonModInfo> refModInfo4 in patch_DataHandler.dictModInfos)
		{
			patch_DataHandler.SyncLoadJSONs<JsonAttackMode>(refModInfo4, "attackmodes/", DataHandler.dictAModes, aIgnorePatterns);
		}
		foreach (KeyValuePair<string, patch_JsonModInfo> refModInfo5 in patch_DataHandler.dictModInfos)
		{
			patch_DataHandler.SyncLoadJSONs<JsonAudioEmitter>(refModInfo5, "audioemitters/", DataHandler.dictAudioEmitters, aIgnorePatterns);
		}
		foreach (KeyValuePair<string, patch_JsonModInfo> refModInfo6 in patch_DataHandler.dictModInfos)
		{
			patch_DataHandler.SyncLoadJSONs<JsonCareer>(refModInfo6, "careers/", DataHandler.dictCareers, aIgnorePatterns);
		}
		foreach (KeyValuePair<string, patch_JsonModInfo> refModInfo7 in patch_DataHandler.dictModInfos)
		{
			patch_DataHandler.SyncLoadJSONs<JsonChargeProfile>(refModInfo7, "chargeprofiles/", DataHandler.dictChargeProfiles, aIgnorePatterns);
		}
		foreach (KeyValuePair<string, patch_JsonModInfo> refModInfo8 in patch_DataHandler.dictModInfos)
		{
			patch_DataHandler.SyncLoadJSONs<JsonColor>(refModInfo8, "colors/", DataHandler.dictJsonColors, aIgnorePatterns);
		}
		foreach (KeyValuePair<string, patch_JsonModInfo> refModInfo9 in patch_DataHandler.dictModInfos)
		{
			patch_DataHandler.SyncLoadJSONs<JsonCond>(refModInfo9, "conditions/", DataHandler.dictConds, aIgnorePatterns);
		}
		foreach (KeyValuePair<string, patch_JsonModInfo> refModInfo10 in patch_DataHandler.dictModInfos)
		{
			patch_DataHandler.SyncLoadJSONs<JsonSimple>(refModInfo10, "conditions_simple/", dictionary, aIgnorePatterns);
		}
		foreach (KeyValuePair<string, patch_JsonModInfo> refModInfo11 in patch_DataHandler.dictModInfos)
		{
			patch_DataHandler.SyncLoadJSONs<JsonCondOwner>(refModInfo11, "condowners/", DataHandler.dictCOs, aIgnorePatterns);
		}
		foreach (KeyValuePair<string, patch_JsonModInfo> refModInfo12 in patch_DataHandler.dictModInfos)
		{
			patch_DataHandler.SyncLoadJSONs<CondRule>(refModInfo12, "condrules/", DataHandler.dictCondRules, aIgnorePatterns);
		}
		foreach (KeyValuePair<string, patch_JsonModInfo> refModInfo13 in patch_DataHandler.dictModInfos)
		{
			patch_DataHandler.SyncLoadJSONs<CondTrigger>(refModInfo13, "condtrigs/", DataHandler.dictCTs, aIgnorePatterns);
		}
		foreach (KeyValuePair<string, patch_JsonModInfo> refModInfo14 in patch_DataHandler.dictModInfos)
		{
			patch_DataHandler.SyncLoadJSONs<JsonContext>(refModInfo14, "context/", DataHandler.dictContext, aIgnorePatterns);
		}
		foreach (KeyValuePair<string, patch_JsonModInfo> refModInfo15 in patch_DataHandler.dictModInfos)
		{
			patch_DataHandler.SyncLoadJSONs<JsonCOOverlay>(refModInfo15, "cooverlays/", DataHandler.dictCOOverlays, aIgnorePatterns);
		}
		foreach (KeyValuePair<string, patch_JsonModInfo> refModInfo16 in patch_DataHandler.dictModInfos)
		{
			patch_DataHandler.SyncLoadJSONs<JsonSimple>(refModInfo16, "crewskins/", dictionary2, aIgnorePatterns);
		}
		foreach (KeyValuePair<string, patch_JsonModInfo> refModInfo17 in patch_DataHandler.dictModInfos)
		{
			patch_DataHandler.SyncLoadJSONs<JsonCrime>(refModInfo17, "crime/", DataHandler.dictCrimes, aIgnorePatterns);
		}
		foreach (KeyValuePair<string, patch_JsonModInfo> refModInfo18 in patch_DataHandler.dictModInfos)
		{
			patch_DataHandler.SyncLoadJSONs<JsonGasRespire>(refModInfo18, "gasrespires/", DataHandler.dictGasRespires, aIgnorePatterns);
		}
		foreach (KeyValuePair<string, patch_JsonModInfo> refModInfo19 in patch_DataHandler.dictModInfos)
		{
			patch_DataHandler.SyncLoadJSONs<JsonGUIPropMap>(refModInfo19, "guipropmaps/", DataHandler.dictGUIPropMapUnparsed, aIgnorePatterns);
		}
		foreach (KeyValuePair<string, patch_JsonModInfo> refModInfo20 in patch_DataHandler.dictModInfos)
		{
			patch_DataHandler.SyncLoadJSONs<JsonHeadline>(refModInfo20, "headlines/", DataHandler.dictHeadlines, aIgnorePatterns);
		}
		foreach (KeyValuePair<string, patch_JsonModInfo> refModInfo21 in patch_DataHandler.dictModInfos)
		{
			patch_DataHandler.SyncLoadJSONs<JsonHomeworld>(refModInfo21, "homeworlds/", DataHandler.dictHomeworlds, aIgnorePatterns);
		}
		foreach (KeyValuePair<string, patch_JsonModInfo> refModInfo22 in patch_DataHandler.dictModInfos)
		{
			patch_DataHandler.SyncLoadJSONs<JsonInfoNode>(refModInfo22, "info/", DataHandler.dictInfoNodes, aIgnorePatterns);
		}
		foreach (KeyValuePair<string, patch_JsonModInfo> refModInfo23 in patch_DataHandler.dictModInfos)
		{
			patch_DataHandler.SyncLoadJSONs<JsonInstallable>(refModInfo23, "installables/", DataHandler.dictInstallables, aIgnorePatterns);
		}
		foreach (KeyValuePair<string, patch_JsonModInfo> refModInfo24 in patch_DataHandler.dictModInfos)
		{
			patch_DataHandler.SyncLoadJSONs<JsonInteractionOverride>(refModInfo24, "interaction_overrides/", DataHandler.dictIAOverrides, aIgnorePatterns);
		}
		foreach (KeyValuePair<string, patch_JsonModInfo> refModInfo25 in patch_DataHandler.dictModInfos)
		{
			patch_DataHandler.SyncLoadJSONs<JsonInteraction>(refModInfo25, "interactions/", DataHandler.dictInteractions, aIgnorePatterns);
		}
		foreach (KeyValuePair<string, patch_JsonModInfo> refModInfo26 in patch_DataHandler.dictModInfos)
		{
			patch_DataHandler.SyncLoadJSONs<JsonItemDef>(refModInfo26, "items/", DataHandler.dictItemDefs, aIgnorePatterns);
		}
		foreach (KeyValuePair<string, patch_JsonModInfo> refModInfo27 in patch_DataHandler.dictModInfos)
		{
			patch_DataHandler.SyncLoadJSONs<JsonJobItems>(refModInfo27, "jobitems/", DataHandler.dictJobitems, aIgnorePatterns);
		}
		foreach (KeyValuePair<string, patch_JsonModInfo> refModInfo28 in patch_DataHandler.dictModInfos)
		{
			patch_DataHandler.SyncLoadJSONs<JsonJob>(refModInfo28, "jobs/", DataHandler.dictJobs, aIgnorePatterns);
		}
		foreach (KeyValuePair<string, patch_JsonModInfo> refModInfo29 in patch_DataHandler.dictModInfos)
		{
			patch_DataHandler.SyncLoadJSONs<JsonLedgerDef>(refModInfo29, "ledgerdefs/", DataHandler.dictLedgerDefs, aIgnorePatterns);
		}
		foreach (KeyValuePair<string, patch_JsonModInfo> refModInfo30 in patch_DataHandler.dictModInfos)
		{
			patch_DataHandler.SyncLoadJSONs<JsonLifeEvent>(refModInfo30, "lifeevents/", DataHandler.dictLifeEvents, aIgnorePatterns);
		}
		foreach (KeyValuePair<string, patch_JsonModInfo> refModInfo31 in patch_DataHandler.dictModInfos)
		{
			patch_DataHandler.SyncLoadJSONs<JsonLight>(refModInfo31, "lights/", DataHandler.dictLights, aIgnorePatterns);
		}
		foreach (KeyValuePair<string, patch_JsonModInfo> refModInfo32 in patch_DataHandler.dictModInfos)
		{
			patch_DataHandler.SyncLoadJSONs<Loot>(refModInfo32, "loot/", DataHandler.dictLoot, aIgnorePatterns);
		}
		foreach (KeyValuePair<string, patch_JsonModInfo> refModInfo33 in patch_DataHandler.dictModInfos)
		{
			patch_DataHandler.SyncLoadJSONs<JsonSimple>(refModInfo33, "manpages/", dictionary3, aIgnorePatterns);
		}
		foreach (KeyValuePair<string, patch_JsonModInfo> refModInfo34 in patch_DataHandler.dictModInfos)
		{
			patch_DataHandler.SyncLoadJSONs<JsonMarketActorConfig>(refModInfo34, "market/Markets/", DataHandler.dictMarketConfigs, aIgnorePatterns);
		}
		foreach (KeyValuePair<string, patch_JsonModInfo> refModInfo35 in patch_DataHandler.dictModInfos)
		{
			patch_DataHandler.SyncLoadJSONs<JsonDCOCollection>(refModInfo35, "market/CoCollections/", DataHandler.dictSupersTemp, aIgnorePatterns);
		}
		foreach (KeyValuePair<string, patch_JsonModInfo> refModInfo36 in patch_DataHandler.dictModInfos)
		{
			patch_DataHandler.SyncLoadJSONs<JsonProductionMap>(refModInfo36, "market/Production/", DataHandler.dictProductionMaps, aIgnorePatterns);
		}
		foreach (KeyValuePair<string, patch_JsonModInfo> refModInfo37 in patch_DataHandler.dictModInfos)
		{
			patch_DataHandler.SyncLoadJSONs<JsonCargoSpec>(refModInfo37, "market/CargoSpecs/", DataHandler.dictCargoSpecs, aIgnorePatterns);
		}
		foreach (KeyValuePair<string, patch_JsonModInfo> refModInfo38 in patch_DataHandler.dictModInfos)
		{
			patch_DataHandler.SyncLoadJSONs<JsonMusic>(refModInfo38, "music/", DataHandler.dictMusic, aIgnorePatterns);
		}
		foreach (KeyValuePair<string, patch_JsonModInfo> refModInfo39 in patch_DataHandler.dictModInfos)
		{
			patch_DataHandler.SyncLoadJSONs<JsonSimple>(refModInfo39, "names_first/", dictionary4, aIgnorePatterns);
		}
		foreach (KeyValuePair<string, patch_JsonModInfo> refModInfo40 in patch_DataHandler.dictModInfos)
		{
			patch_DataHandler.SyncLoadJSONs<JsonSimple>(refModInfo40, "names_full/", dictionary5, aIgnorePatterns);
		}
		foreach (KeyValuePair<string, patch_JsonModInfo> refModInfo41 in patch_DataHandler.dictModInfos)
		{
			patch_DataHandler.SyncLoadJSONs<JsonSimple>(refModInfo41, "names_last/", dictionary6, aIgnorePatterns);
		}
		foreach (KeyValuePair<string, patch_JsonModInfo> refModInfo42 in patch_DataHandler.dictModInfos)
		{
			patch_DataHandler.SyncLoadJSONs<JsonSimple>(refModInfo42, "names_robots/", dictionary7, aIgnorePatterns);
		}
		foreach (KeyValuePair<string, patch_JsonModInfo> refModInfo43 in patch_DataHandler.dictModInfos)
		{
			patch_DataHandler.SyncLoadJSONs<JsonSimple>(refModInfo43, "names_ship/", dictionary8, aIgnorePatterns);
		}
		foreach (KeyValuePair<string, patch_JsonModInfo> refModInfo44 in patch_DataHandler.dictModInfos)
		{
			patch_DataHandler.SyncLoadJSONs<JsonSimple>(refModInfo44, "names_ship_adjectives/", dictionary9, aIgnorePatterns);
		}
		foreach (KeyValuePair<string, patch_JsonModInfo> refModInfo45 in patch_DataHandler.dictModInfos)
		{
			patch_DataHandler.SyncLoadJSONs<JsonSimple>(refModInfo45, "names_ship_nouns/", dictionary10, aIgnorePatterns);
		}
		foreach (KeyValuePair<string, patch_JsonModInfo> refModInfo46 in patch_DataHandler.dictModInfos)
		{
			patch_DataHandler.SyncLoadJSONs<JsonParallax>(refModInfo46, "parallax/", DataHandler.dictParallax, aIgnorePatterns);
		}
		foreach (KeyValuePair<string, patch_JsonModInfo> refModInfo47 in patch_DataHandler.dictModInfos)
		{
			patch_DataHandler.SyncLoadJSONs<JsonPDAAppIcon>(refModInfo47, "pda_apps/", DataHandler.dictPDAAppIcons, aIgnorePatterns);
		}
		foreach (KeyValuePair<string, patch_JsonModInfo> refModInfo48 in patch_DataHandler.dictModInfos)
		{
			patch_DataHandler.SyncLoadJSONs<JsonPersonSpec>(refModInfo48, "personspecs/", DataHandler.dictPersonSpecs, aIgnorePatterns);
		}
		foreach (KeyValuePair<string, patch_JsonModInfo> refModInfo49 in patch_DataHandler.dictModInfos)
		{
			patch_DataHandler.SyncLoadJSONs<JsonPledge>(refModInfo49, "pledges/", DataHandler.dictPledges, aIgnorePatterns);
		}
		foreach (KeyValuePair<string, patch_JsonModInfo> refModInfo50 in patch_DataHandler.dictModInfos)
		{
			patch_DataHandler.SyncLoadJSONs<JsonPlotBeatOverride>(refModInfo50, "plot_beat_overrides/", DataHandler.dictPlotBeatOverrides, aIgnorePatterns);
		}
		foreach (KeyValuePair<string, patch_JsonModInfo> refModInfo51 in patch_DataHandler.dictModInfos)
		{
			patch_DataHandler.SyncLoadJSONs<JsonPlotBeat>(refModInfo51, "plot_beats/", DataHandler.dictPlotBeats, aIgnorePatterns);
		}
		foreach (KeyValuePair<string, patch_JsonModInfo> refModInfo52 in patch_DataHandler.dictModInfos)
		{
			patch_DataHandler.SyncLoadJSONs<JsonPlotManagerSettings>(refModInfo52, "plot_manager/", DataHandler.dictPlotManager, aIgnorePatterns);
		}
		foreach (KeyValuePair<string, patch_JsonModInfo> refModInfo53 in patch_DataHandler.dictModInfos)
		{
			patch_DataHandler.SyncLoadJSONs<JsonPlot>(refModInfo53, "plots/", DataHandler.dictPlots, aIgnorePatterns);
		}
		foreach (KeyValuePair<string, patch_JsonModInfo> refModInfo54 in patch_DataHandler.dictModInfos)
		{
			patch_DataHandler.SyncLoadJSONs<JsonPowerInfo>(refModInfo54, "powerinfos/", DataHandler.dictPowerInfo, aIgnorePatterns);
		}
		foreach (KeyValuePair<string, patch_JsonModInfo> refModInfo55 in patch_DataHandler.dictModInfos)
		{
			patch_DataHandler.SyncLoadJSONs<JsonRacingLeague>(refModInfo55, "racing/leagues/", DataHandler.dictRacingLeagues, aIgnorePatterns);
		}
		foreach (KeyValuePair<string, patch_JsonModInfo> refModInfo56 in patch_DataHandler.dictModInfos)
		{
			patch_DataHandler.SyncLoadJSONs<JsonRaceTrack>(refModInfo56, "racing/tracks/", DataHandler.dictRaceTracks, aIgnorePatterns);
		}
		foreach (KeyValuePair<string, patch_JsonModInfo> refModInfo57 in patch_DataHandler.dictModInfos)
		{
			patch_DataHandler.SyncLoadJSONs<JsonRoomSpec>(refModInfo57, "rooms/", DataHandler.dictRoomSpecsTemp, aIgnorePatterns);
		}
		foreach (KeyValuePair<string, patch_JsonModInfo> refModInfo58 in patch_DataHandler.dictModInfos)
		{
			patch_DataHandler.SyncLoadJSONs<JsonShipSpec>(refModInfo58, "shipspecs/", DataHandler.dictShipSpecs, aIgnorePatterns);
		}
		foreach (KeyValuePair<string, patch_JsonModInfo> refModInfo59 in patch_DataHandler.dictModInfos)
		{
			patch_DataHandler.SyncLoadJSONs<JsonSlotEffects>(refModInfo59, "slot_effects/", DataHandler.dictSlotEffects, aIgnorePatterns);
		}
		foreach (KeyValuePair<string, patch_JsonModInfo> refModInfo60 in patch_DataHandler.dictModInfos)
		{
			patch_DataHandler.SyncLoadJSONs<JsonSlot>(refModInfo60, "slots/", DataHandler.dictSlots, aIgnorePatterns);
		}
		foreach (KeyValuePair<string, patch_JsonModInfo> refModInfo61 in patch_DataHandler.dictModInfos)
		{
			patch_DataHandler.SyncLoadJSONs<JsonStarSystemSave>(refModInfo61, "star_systems/", DataHandler.dictStarSystems, aIgnorePatterns);
		}
		foreach (KeyValuePair<string, patch_JsonModInfo> refModInfo62 in patch_DataHandler.dictModInfos)
		{
			patch_DataHandler.SyncLoadJSONs<JsonSimple>(refModInfo62, "strings/", dictionary11, aIgnorePatterns);
		}
		foreach (KeyValuePair<string, patch_JsonModInfo> refModInfo63 in patch_DataHandler.dictModInfos)
		{
			patch_DataHandler.SyncLoadJSONs<JsonTicker>(refModInfo63, "tickers/", DataHandler.dictTickers, aIgnorePatterns);
		}
		foreach (KeyValuePair<string, patch_JsonModInfo> refModInfo64 in patch_DataHandler.dictModInfos)
		{
			patch_DataHandler.SyncLoadJSONs<JsonTip>(refModInfo64, "tips/", DataHandler.dictTips, aIgnorePatterns);
		}
		foreach (KeyValuePair<string, patch_JsonModInfo> refModInfo65 in patch_DataHandler.dictModInfos)
		{
			patch_DataHandler.SyncLoadJSONs<JsonCustomTokens>(refModInfo65, "tokens/", DataHandler.dictJsonTokens, aIgnorePatterns);
		}
		foreach (KeyValuePair<string, patch_JsonModInfo> refModInfo66 in patch_DataHandler.dictModInfos)
		{
			patch_DataHandler.SyncLoadJSONs<JsonSimple>(refModInfo66, "traitscores/", dictionary12, aIgnorePatterns);
		}
		foreach (KeyValuePair<string, patch_JsonModInfo> refModInfo67 in patch_DataHandler.dictModInfos)
		{
			patch_DataHandler.SyncLoadJSONs<JsonTransit>(refModInfo67, "transit/", DataHandler.dictTransit, aIgnorePatterns);
		}
		foreach (KeyValuePair<string, patch_JsonModInfo> refModInfo68 in patch_DataHandler.dictModInfos)
		{
			patch_DataHandler.SyncLoadJSONs<JsonVerbs>(refModInfo68, "verbs/", DataHandler.dictJsonVerbs, aIgnorePatterns);
		}
		foreach (KeyValuePair<string, patch_JsonModInfo> refModInfo69 in patch_DataHandler.dictModInfos)
		{
			patch_DataHandler.SyncLoadJSONs<JsonWound>(refModInfo69, "wounds/", DataHandler.dictWounds, aIgnorePatterns);
		}
		foreach (KeyValuePair<string, patch_JsonModInfo> refModInfo70 in patch_DataHandler.dictModInfos)
		{
			patch_DataHandler.SyncLoadJSONs<JsonZoneTrigger>(refModInfo70, "zone_triggers/", DataHandler.dictZoneTriggers, aIgnorePatterns);
		}
		DataHandler.ParseGUIPropMaps();
		DataHandler.ParseConditionsSimple(dictionary);
		DataHandler.ParseTraitScores(dictionary12);
		DataHandler.ParseSimpleIntoStringDict(dictionary11, DataHandler.dictStrings);
		DataHandler.ParseSimpleIntoStringDict(dictionary2, DataHandler.dictCrewSkins);
		DataHandler.ParseSimpleIntoStringDict(dictionary4, DataHandler.dictNamesFirst);
		DataHandler.ParseSimpleIntoStringDict(dictionary5, DataHandler.dictNamesFull);
		DataHandler.ParseSimpleIntoStringDict(dictionary6, DataHandler.dictNamesLast);
		DataHandler.ParseSimpleIntoStringDict(dictionary7, DataHandler.dictNamesRobots);
		DataHandler.ParseSimpleIntoStringDict(dictionary8, DataHandler.dictNamesShip);
		DataHandler.ParseSimpleIntoStringDict(dictionary10, DataHandler.dictNamesShipNouns);
		DataHandler.ParseSimpleIntoStringDict(dictionary9, DataHandler.dictNamesShipAdjectives);
		DataHandler.ParseManPages(dictionary3);
		DataHandler.ParseMusic();
		foreach (KeyValuePair<string, JsonCondOwner> keyValuePair7 in DataHandler.dictCOs)
		{
			bool bSlotLocked = keyValuePair7.Value.bSlotLocked;
			if (bSlotLocked)
			{
				patch_DataHandler.listLockedCOs.Add(keyValuePair7.Value.strName);
			}
		}
		foreach (KeyValuePair<string, JsonModInfo> keyValuePair8 in DataHandler.dictModInfos)
		{
			bool flag26 = keyValuePair8.Value.Status == 0;
			if (flag26)
			{
				keyValuePair8.Value.Status = 0;
			}
			else
			{
				bool flag27 = ConsoleToGUI.instance && num < ConsoleToGUI.instance.ErrorCount;
				if (flag27)
				{
					keyValuePair8.Value.Status = 2;
				}
				else
				{
					keyValuePair8.Value.Status = 1;
				}
			}
		}
	}
	private static void SyncLoadJSONs<TJson>(KeyValuePair<string, patch_JsonModInfo> refModInfo, string subFolder, Dictionary<string, TJson> dataDict, string[] aIgnorePatterns)
	{
		bool flag = !refModInfo.Key.IsCoreEntry();
		string text = subFolder.Remove(subFolder.Length - 1);
		string path = Path.Combine(flag ? Path.Combine(patch_DataHandler.strModsPath, refModInfo.Key) : DataHandler.strAssetPath, "data/");
		patch_JsonModInfo value = refModInfo.Value;
		bool flag2 = ((value != null) ? value.removeIds : null) != null && refModInfo.Value.removeIds.ContainsKey(text);
		if (flag2)
		{
			foreach (string text2 in refModInfo.Value.removeIds[text])
			{
				bool flag3 = dataDict.Remove(text2);
				bool flag4 = flag3;
				if (flag4)
				{
					Debug.Log("Removed existing '" + text + "' entry: " + text2);
				}
			}
		}
		string path2 = Path.Combine(path, subFolder);
		bool flag5 = !Directory.Exists(path2);
		if (!flag5)
		{
			string[] files = Directory.GetFiles(path2, "*.json", SearchOption.AllDirectories);
			foreach (string text3 in files)
			{
				string text4 = DataHandler.PathSanitize(text3);
				bool flag6 = false;
				bool flag7 = aIgnorePatterns != null;
				if (flag7)
				{
					foreach (string value2 in aIgnorePatterns)
					{
						bool flag8 = text4.IndexOf(value2) >= 0;
						if (flag8)
						{
							flag6 = true;
							break;
						}
					}
				}
				bool flag9 = flag6;
				if (flag9)
				{
					Debug.LogWarning("Ignore Pattern match: " + text4 + "; Skipping...");
				}
				else
				{
					patch_DataHandler.SyncToData<TJson>(text4, text, flag, dataDict, text.IsExtedable());
				}
			}
		}
	}
	public static void SyncToData<TJson>(string strFile, string strType, bool isMod, Dictionary<string, TJson> dataDict, bool extData)
	{
		Debug.Log("#Info# Loading JSON: " + strFile);
		bool doLog = FFU_BR_Defs.SyncLogging >= FFU_BR_Defs.SyncLogs.ModChanges;
		bool flag = FFU_BR_Defs.SyncLogging >= FFU_BR_Defs.SyncLogs.DeepCopy;
		bool flag2 = FFU_BR_Defs.SyncLogging >= FFU_BR_Defs.SyncLogs.ModdedDump;
		bool flag3 = FFU_BR_Defs.SyncLogging >= FFU_BR_Defs.SyncLogs.ExtendedDump;
		bool flag4 = FFU_BR_Defs.SyncLogging >= FFU_BR_Defs.SyncLogs.ContentDump;
		bool flag5 = FFU_BR_Defs.SyncLogging >= FFU_BR_Defs.SyncLogs.SourceDump;
		string text = string.Empty;
		try
		{
			string text2 = File.ReadAllText(strFile, Encoding.UTF8);
			text += "Converting JSON into Array...\n";
			string[] array = isMod ? text2.Compressed().Split(new string[]
			{
				"},{"
			}, StringSplitOptions.None) : null;
			TJson[] array2 = JsonMapper.ToObject<TJson[]>(text2);
			for (int i = 0; i < array2.Length; i++)
			{
				TJson tjson = array2[i];
				string text3 = isMod ? array[i] : null;
				text += "Getting key: ";
				string dataKey = null;
				Type type = tjson.GetType();
				PropertyInfo propertyInfo = (type != null) ? type.GetProperty("strReference") : null;
				Type type2 = tjson.GetType();
				PropertyInfo propertyInfo2 = (type2 != null) ? type2.GetProperty("strName") : null;
				bool flag6 = propertyInfo2 == null;
				if (flag6)
				{
					JsonLogger.ReportProblem("strName is missing", 1);
				}
				else
				{
					object obj = (propertyInfo != null) ? propertyInfo.GetValue(tjson, null) : null;
					object value = propertyInfo2.GetValue(tjson, null);
					string text4 = (obj != null) ? obj.ToString() : null;
					dataKey = value.ToString();
					text = text + dataKey + "\n";
					bool flag7 = isMod && dataDict.ContainsKey(dataKey);
					if (flag7)
					{
						bool flag8 = flag2;
						if (flag8)
						{
							Debug.Log("Modification Data (Dump/Before): " + JsonMapper.ToJson(dataDict[dataKey]));
						}
						try
						{
							patch_DataHandler.SyncDataSafe<TJson>(dataDict[dataKey], tjson, ref text3, strType, dataKey, extData, doLog);
							bool flag9 = flag2;
							if (flag9)
							{
								Debug.Log("Modification Data (Dump/After): " + JsonMapper.ToJson(dataDict[dataKey]));
							}
						}
						catch (Exception ex)
						{
							Exception innerException = ex.InnerException;
							Debug.LogWarning(string.Concat(new string[]
							{
								"Modification sync for Data Block [",
								dataKey,
								"] has failed! Ignoring.\n",
								ex.Message,
								"\n",
								ex.StackTrace,
								(innerException != null) ? ("\nInner: " + innerException.Message + "\n" + innerException.StackTrace) : ""
							}));
						}
					}
					else
					{
						bool flag10 = isMod && !dataDict.ContainsKey(dataKey);
						if (flag10)
						{
							bool flag11 = text4 != null && dataDict.ContainsKey(text4);
							if (flag11)
							{
								string text5 = JsonMapper.ToJson(dataDict[text4]);
								bool flag12 = flag3;
								if (flag12)
								{
									Debug.Log("Reference Data (Dump/Before): " + text5);
								}
								bool isDeepCopySuccess = false;
								text5 = Regex.Replace(text5, "(\"strName\":)\"[^\"]*\"", delegate(Match match)
								{
									isDeepCopySuccess = true;
									return match.Groups[1].Value + "\"" + dataKey + "\"";
								});
								bool isDeepCopySuccess2 = isDeepCopySuccess;
								if (isDeepCopySuccess2)
								{
									TJson tjson2 = JsonMapper.ToObject<TJson>(text5);
									bool flag13 = flag;
									if (flag13)
									{
										Debug.Log("#Info# Modified Deep Copy Created: " + text4 + " => " + dataKey);
									}
									try
									{
										patch_DataHandler.SyncDataSafe<TJson>(tjson2, tjson, ref text3, strType, dataKey, extData, flag);
										bool flag14 = flag3;
										if (flag14)
										{
											Debug.Log("Reference Data (Dump/After): " + JsonMapper.ToJson(tjson2));
										}
									}
									catch (Exception ex2)
									{
										Exception innerException2 = ex2.InnerException;
										Debug.LogWarning(string.Concat(new string[]
										{
											"Reference sync for Data Block [",
											dataKey,
											"] has failed! Ignoring.\n",
											ex2.Message,
											"\n",
											ex2.StackTrace,
											(innerException2 != null) ? ("\nInner: " + innerException2.Message + "\n" + innerException2.StackTrace) : ""
										}));
									}
									try
									{
										dataDict.Add(dataKey, tjson2);
									}
									catch (Exception ex3)
									{
										Exception innerException3 = ex3.InnerException;
										Debug.LogWarning(string.Concat(new string[]
										{
											"Reference add of new Data Block [",
											dataKey,
											"] has failed! Ignoring.\n",
											ex3.Message,
											"\n",
											ex3.StackTrace,
											(innerException3 != null) ? ("\nInner: " + innerException3.Message + "\n" + innerException3.StackTrace) : ""
										}));
									}
								}
							}
							else
							{
								bool flag15 = !string.IsNullOrEmpty(text4);
								if (flag15)
								{
									Debug.LogWarning(string.Concat(new string[]
									{
										"Reference key '",
										text4,
										"' in Data Block [",
										dataKey,
										"] is invalid! Ignoring."
									}));
								}
								else
								{
									bool flag16 = flag4;
									if (flag16)
									{
										try
										{
											Debug.Log("Addendum Data (Dump/Mod): " + JsonMapper.ToJson(tjson));
										}
										catch (Exception ex4)
										{
											Exception innerException4 = ex4.InnerException;
											Debug.LogWarning(string.Concat(new string[]
											{
												"Addendum Data (Dump/Mod) for Data Block [",
												dataKey,
												"] has failed! Ignoring.\n",
												ex4.Message,
												"\n",
												ex4.StackTrace,
												(innerException4 != null) ? ("\nInner: " + innerException4.Message + "\n" + innerException4.StackTrace) : ""
											}));
										}
									}
									try
									{
										dataDict.Add(dataKey, tjson);
									}
									catch (Exception ex5)
									{
										Exception innerException5 = ex5.InnerException;
										Debug.LogWarning(string.Concat(new string[]
										{
											"Modded Add of new Data Block [",
											dataKey,
											"] has failed! Ignoring.\n",
											ex5.Message,
											"\n",
											ex5.StackTrace,
											(innerException5 != null) ? ("\nInner: " + innerException5.Message + "\n" + innerException5.StackTrace) : ""
										}));
									}
								}
							}
						}
						else
						{
							bool flag17 = flag5;
							if (flag17)
							{
								try
								{
									Debug.Log("Addendum Data (Dump/Core): " + JsonMapper.ToJson(tjson));
								}
								catch (Exception ex6)
								{
									Exception innerException6 = ex6.InnerException;
									Debug.LogWarning(string.Concat(new string[]
									{
										"Addendum Data (Dump/Core) for Data Block [",
										dataKey,
										"] has failed! Ignoring.\n",
										ex6.Message,
										"\n",
										ex6.StackTrace,
										(innerException6 != null) ? ("\nInner: " + innerException6.Message + "\n" + innerException6.StackTrace) : ""
									}));
								}
							}
							try
							{
								dataDict.Add(dataKey, tjson);
							}
							catch (Exception ex7)
							{
								Exception innerException7 = ex7.InnerException;
								Debug.LogWarning(string.Concat(new string[]
								{
									"Core Add of new Data Block [",
									dataKey,
									"] has failed! Ignoring.\n",
									ex7.Message,
									"\n",
									ex7.StackTrace,
									(innerException7 != null) ? ("\nInner: " + innerException7.Message + "\n" + innerException7.StackTrace) : ""
								}));
							}
						}
					}
				}
			}
			array2 = null;
		}
		catch (Exception ex8)
		{
			JsonLogger.ReportProblem(strFile, 0);
			bool flag18 = text.Length > 1000;
			if (flag18)
			{
				text = text.Substring(text.Length - 1000);
			}
			Debug.LogError(string.Concat(new string[]
			{
				text,
				"\n",
				ex8.Message,
				"\n",
				ex8.StackTrace.ToString()
			}));
		}
		bool flag19 = strFile.IndexOf("osSGv1") >= 0;
		if (flag19)
		{
			Debug.Log(text);
		}
	}
	public static void SyncDataSafe<TJson>(TJson currDataSet, TJson newDataSet, ref string rawDataSet, string dataType, string dataKey, bool extData, bool doLog = false)
	{
		Type type = currDataSet.GetType();
		Type type2 = newDataSet.GetType();
		foreach (PropertyInfo propertyInfo in type.GetProperties())
		{
			bool flag = !propertyInfo.CanWrite || propertyInfo.Name.IsForbidden();
			if (!flag)
			{
				PropertyInfo property = type2.GetProperty(propertyInfo.Name);
				bool flag2 = property != null;
				if (flag2)
				{
					bool flag3 = false;
					string name = propertyInfo.Name;
					object value = property.GetValue(newDataSet, null);
					object value2 = propertyInfo.GetValue(currDataSet, null);
					bool flag4 = rawDataSet.IndexOf(name) >= 0;
					if (flag4)
					{
						try
						{
							bool flag5 = value2 != null && value is IDictionary;
							if (flag5)
							{
								patch_DataHandler.SyncRecords(ref value, ref value2, ref flag3, dataKey, name, dataType, extData, doLog);
							}
							else
							{
								bool flag6 = value2 != null && value is string[];
								if (flag6)
								{
									patch_DataHandler.SyncArrays(ref value, ref value2, dataKey, name, extData, doLog);
								}
								else
								{
									bool flag7 = value2 != null && value is object[];
									if (flag7)
									{
										patch_DataHandler.SyncObjects(ref value, ref value2, ref flag3, dataKey, name, dataType, extData, doLog);
									}
									else
									{
										bool flag8 = value2 != null && !(value is string) && value != null && value.GetType().IsClass;
										if (flag8)
										{
											string text = JsonMapper.ToJson(value2).Compressed();
											patch_DataHandler.SyncDataSafe<object>(value2, value, ref text, dataType, dataKey + "/" + name, extData, doLog);
										}
										else if (doLog)
										{
											Debug.Log("#Info# Data Block [" + dataKey + "], Property " + string.Format("[{0}]: {1} => {2}", name, value2.Sanitized(), value.Sanitized()));
										}
									}
								}
							}
							bool flag9 = flag3;
							if (flag9)
							{
								propertyInfo.SetValue(currDataSet, value2, null);
							}
							else
							{
								propertyInfo.SetValue(currDataSet, value, null);
							}
						}
						catch (Exception ex)
						{
							Exception innerException = ex.InnerException;
							Debug.LogWarning(string.Concat(new string[]
							{
								"Value sync for Data Block [",
								dataKey,
								"], Property [",
								name,
								"] has failed! Ignoring.\n",
								ex.Message,
								"\n",
								ex.StackTrace,
								(innerException != null) ? ("\nInner: " + innerException.Message + "\n" + innerException.StackTrace) : ""
							}));
						}
					}
				}
			}
		}
		BindingFlags bindingAttr = BindingFlags.Instance | BindingFlags.Public;
		foreach (FieldInfo fieldInfo in type.GetFields(bindingAttr))
		{
			bool flag10 = fieldInfo.IsLiteral || fieldInfo.Name.IsForbidden();
			if (!flag10)
			{
				FieldInfo field = type2.GetField(fieldInfo.Name, bindingAttr);
				bool flag11 = field != null;
				if (flag11)
				{
					bool flag12 = false;
					string name2 = fieldInfo.Name;
					object value3 = field.GetValue(newDataSet);
					object value4 = fieldInfo.GetValue(currDataSet);
					bool flag13 = rawDataSet.IndexOf(name2) >= 0;
					if (flag13)
					{
						try
						{
							bool flag14 = value4 != null && value3 is IDictionary;
							if (flag14)
							{
								patch_DataHandler.SyncRecords(ref value3, ref value4, ref flag12, dataKey, name2, dataType, extData, doLog);
							}
							else
							{
								bool flag15 = value4 != null && value3 is string[];
								if (flag15)
								{
									patch_DataHandler.SyncArrays(ref value3, ref value4, dataKey, name2, extData, doLog);
								}
								else
								{
									bool flag16 = value4 != null && value3 is object[];
									if (flag16)
									{
										patch_DataHandler.SyncObjects(ref value3, ref value4, ref flag12, dataKey, name2, dataType, extData, doLog);
									}
									else
									{
										bool flag17 = value4 != null && !(value3 is string) && value3 != null && value3.GetType().IsClass;
										if (flag17)
										{
											string text2 = JsonMapper.ToJson(value4).Compressed();
											patch_DataHandler.SyncDataSafe<object>(value4, value3, ref text2, dataType, dataKey + "/" + name2, extData, doLog);
										}
										else if (doLog)
										{
											Debug.Log("#Info# Data Block [" + dataKey + "], Property " + string.Format("[{0}]: {1} => {2}", name2, value4.Sanitized(), value3.Sanitized()));
										}
									}
								}
							}
							bool flag18 = flag12;
							if (flag18)
							{
								fieldInfo.SetValue(currDataSet, value4);
							}
							else
							{
								fieldInfo.SetValue(currDataSet, value3);
							}
						}
						catch (Exception ex2)
						{
							Exception innerException2 = ex2.InnerException;
							Debug.LogWarning(string.Concat(new string[]
							{
								"Value sync for Data Block [",
								dataKey,
								"], Property [",
								name2,
								"] has failed! Ignoring.\n",
								ex2.Message,
								"\n",
								ex2.StackTrace,
								(innerException2 != null) ? ("\nInner: " + innerException2.Message + "\n" + innerException2.StackTrace) : ""
							}));
						}
					}
				}
			}
		}
	}
	public static void SyncObjects(ref object newValue, ref object currValue, ref bool useCurrent, string dataKey, string propName, string dataType, bool extData, bool doLog)
	{
		useCurrent = true;
		List<object> list = (newValue as Array).Cast<object>().ToList<object>();
		List<object> list2 = (currValue as Array).Cast<object>().ToList<object>();
		foreach (object obj in list)
		{
			string targetID = obj.GetIdentifier();
			bool flag = string.IsNullOrEmpty(targetID);
			if (flag)
			{
				if (doLog)
				{
					Debug.Log("#Info# Data Block [" + dataKey + "], " + string.Format("Property [{0}]: {1} => {2}", propName, currValue, newValue));
				}
				useCurrent = false;
				return;
			}
			bool flag2 = targetID.StartsWith("~");
			bool flag3 = targetID.StartsWith("*");
			bool flag4 = flag2 || flag3;
			bool flag5 = flag4;
			if (flag5)
			{
				targetID = targetID.Substring(1);
				obj.SetIdentifier(targetID);
			}
			int num = list2.FindIndex((object x) => x.GetIdentifier() == targetID);
			bool flag6 = num >= 0;
			if (flag6)
			{
				string text = list2[num].GetName();
				text = ((text != targetID && text != string.Empty) ? (text + ":" + targetID) : targetID);
				bool flag7 = flag2;
				if (flag7)
				{
					if (doLog)
					{
						Debug.Log(string.Concat(new string[]
						{
							"#Info# Object [",
							text,
							"] was removed from Data Block [",
							dataKey,
							"/",
							propName,
							"]"
						}));
					}
					list2.RemoveAt(num);
				}
				else
				{
					bool flag8 = flag3;
					if (flag8)
					{
						if (doLog)
						{
							Debug.Log(string.Concat(new string[]
							{
								"#Info# Object [",
								text,
								"] was replaced in Data Block [",
								dataKey,
								"/",
								propName,
								"]"
							}));
						}
						list2[num] = obj;
					}
					else
					{
						string text2 = JsonMapper.ToJson(obj);
						patch_DataHandler.SyncDataSafe<object>(list2[num], obj, ref text2, dataType, string.Concat(new string[]
						{
							dataKey,
							"/",
							propName,
							"/",
							text
						}), extData, doLog);
					}
				}
			}
			else
			{
				string text3 = obj.GetName();
				text3 = ((text3 != targetID && text3 != string.Empty) ? (text3 + ":" + targetID) : targetID);
				if (doLog)
				{
					Debug.Log(string.Concat(new string[]
					{
						"#Info# Object [",
						text3,
						"] was added to Data Block [",
						dataKey,
						"/",
						propName,
						"]"
					}));
				}
				list2.Add(obj);
			}
		}
		Array array = Array.CreateInstance(currValue.GetType().GetElementType(), list2.Count);
		for (int i = 0; i < list2.Count; i++)
		{
			array.SetValue(list2[i], i);
		}
		currValue = array;
	}
	public static void SyncRecords(ref object newValue, ref object currValue, ref bool useCurrent, string dataKey, string propName, string dataType, bool extData, bool doLog)
	{
		useCurrent = true;
		IDictionary dictionary = (IDictionary)newValue;
		IDictionary dictionary2 = (IDictionary)currValue;
		Type[] genericArguments = currValue.GetType().GetGenericArguments();
		Type type = (genericArguments.Count<Type>() > 1) ? genericArguments[1] : genericArguments[0];
		bool flag = type != typeof(string) && type.IsClass;
		foreach (object obj in dictionary.Keys.Cast<object>().ToList<object>())
		{
			bool flag2 = obj.ToString().StartsWith("~");
			bool flag3 = obj.ToString().StartsWith("*");
			object obj2 = (flag2 || flag3) ? obj.ToString().Substring(1) : obj;
			bool flag4 = dictionary2.Contains(obj2);
			if (flag4)
			{
				bool flag5 = flag2;
				if (flag5)
				{
					if (doLog)
					{
						Debug.Log(string.Concat(new string[]
						{
							string.Format("#Info# Property [{0}] was removed ", obj2),
							"from Data Block [",
							dataKey,
							"/",
							propName,
							"]"
						}));
					}
					dictionary2.Remove(obj2);
				}
				else
				{
					bool flag6 = flag3;
					if (flag6)
					{
						if (doLog)
						{
							Debug.Log(string.Concat(new string[]
							{
								string.Format("#Info# Property [{0}] was replaced ", obj2),
								"in Data Block [",
								dataKey,
								"/",
								propName,
								"]"
							}));
						}
						dictionary2[obj2] = dictionary[obj];
					}
					else
					{
						bool flag7 = flag;
						if (flag7)
						{
							string text = JsonMapper.ToJson(dictionary[obj2]);
							patch_DataHandler.SyncDataSafe<object>(dictionary2[obj2], dictionary[obj2], ref text, dataType, string.Format("{0}/{1}:{2}", dataKey, propName, obj2), extData, doLog);
						}
						else
						{
							if (doLog)
							{
								Debug.Log(string.Concat(new string[]
								{
									"#Info# Data Block [",
									dataKey,
									"/",
									propName,
									"], ",
									string.Format("Property [{0}]: {1} => {2}", obj2, dictionary2[obj2], dictionary[obj2])
								}));
							}
							dictionary2[obj2] = dictionary[obj2];
						}
					}
				}
			}
			else
			{
				if (doLog)
				{
					Debug.Log(string.Concat(new string[]
					{
						string.Format("#Info# Property [{0}], Value [{1}] ", obj2, dictionary[obj2]),
						"was added to Data Block [",
						dataKey,
						"/",
						propName,
						"]"
					}));
				}
				dictionary2[obj2] = dictionary[obj2];
			}
		}
	}
	public static void SyncArrays(ref object newValue, ref object currValue, string dataKey, string propName, bool extData, bool doLog)
	{
		patch_DataHandler.SyncArrayOp arrayOp = extData ? patch_DataHandler.SyncArrayOp.Add : patch_DataHandler.SyncArrayOp.None;
		List<string> list = (currValue as string[]).ToList<string>();
		List<string> list2 = (newValue as string[]).ToList<string>();
		List<string> list3 = new List<string>(list);
		bool flag = true;
		foreach (string text in list2)
		{
			bool flag2 = string.IsNullOrEmpty(text);
			if (!flag2)
			{
				bool flag3 = !char.IsDigit(text[0]) || !text.Contains('|');
				if (!flag3)
				{
					List<string> list4 = text.Split(new char[]
					{
						'|'
					}).ToList<string>();
					int num;
					int.TryParse(list4[0], out num);
					bool flag4 = num > 0;
					if (flag4)
					{
						list4.RemoveAt(0);
						List<string> list5 = list[num - 1].Split(new char[]
						{
							'|'
						}).ToList<string>();
						patch_DataHandler.SyncArrayOps(list5, list4, ref flag, dataKey, string.Format("{0}#{1}", propName, num), doLog, arrayOp);
						bool flag5 = flag;
						if (flag5)
						{
							Debug.LogWarning("You attempted to modify sub-array in Data Block " + string.Format("[{0}], Property [{1}#{2}], but performed no array operations. ", dataKey, propName, num) + "Assume that something went horribly wrong and game is likely to crash.");
						}
						list[num - 1] = string.Join("|", list5.ToArray());
					}
				}
			}
		}
		patch_DataHandler.SyncArrayOps(list, list2, ref flag, dataKey, propName, doLog, arrayOp);
		bool flag6 = flag;
		if (flag6)
		{
			if (doLog)
			{
				Debug.Log("#Info# Data Block [" + dataKey + "], " + string.Format("Property [{0}]: String[{1}] => String[{2}]", propName, list3.Count, list2.Count));
			}
			newValue = list2.ToArray();
		}
		else
		{
			newValue = list.ToArray();
		}
	}
	public static void SyncArrayOps(List<string> modArray, List<string> refArray, ref bool noArrayOps, string dataKey, string propName, bool doLog, patch_DataHandler.SyncArrayOp arrayOp = patch_DataHandler.SyncArrayOp.None)
	{
		int num = 0;
		foreach (string text in refArray)
		{
			bool flag = string.IsNullOrEmpty(text);
			if (!flag)
			{
				bool flag2 = char.IsDigit(text[0]) && text.Contains('|');
				if (!flag2)
				{
					bool flag3 = text.StartsWith("--");
					if (flag3)
					{
						string text2 = text.Substring(0, 7);
						string a = text2;
						if (!(a == "--MOD--"))
						{
							if (!(a == "--ADD--"))
							{
								if (!(a == "--INS--"))
								{
									if (a == "--DEL--")
									{
										arrayOp = patch_DataHandler.SyncArrayOp.Del;
									}
								}
								else
								{
									arrayOp = patch_DataHandler.SyncArrayOp.Ins;
								}
							}
							else
							{
								arrayOp = patch_DataHandler.SyncArrayOp.Add;
							}
						}
						else
						{
							arrayOp = patch_DataHandler.SyncArrayOp.Mod;
						}
						bool flag4 = arrayOp == patch_DataHandler.SyncArrayOp.Ins;
						if (flag4)
						{
							int.TryParse(text.Substring(7), out num);
							num--;
							bool flag5 = num < 0;
							if (flag5)
							{
								Debug.LogWarning(string.Concat(new string[]
								{
									"The '--INS--' array operation in Data Block [",
									dataKey,
									"], Property [",
									propName,
									"] received invalid index! Using [0] index."
								}));
								num = 0;
							}
						}
					}
					else
					{
						bool flag6 = noArrayOps;
						if (flag6)
						{
							noArrayOps = (arrayOp == patch_DataHandler.SyncArrayOp.None);
						}
						bool flag7 = noArrayOps;
						if (flag7)
						{
							break;
						}
						string[] array = text.Split(new char[]
						{
							'='
						});
						bool flag8 = array.Length == 2 && !text.Contains('|') && !string.IsNullOrEmpty(array[1]);
						bool flag9 = flag8;
						if (flag9)
						{
							switch (arrayOp)
							{
							case patch_DataHandler.SyncArrayOp.Mod:
								patch_DataHandler.OpModData(modArray, text, dataKey, propName, doLog);
								break;
							case patch_DataHandler.SyncArrayOp.Add:
								patch_DataHandler.OpAddData(modArray, text, dataKey, propName, doLog);
								break;
							case patch_DataHandler.SyncArrayOp.Ins:
								patch_DataHandler.OpInsData(modArray, ref num, text, dataKey, propName, doLog);
								break;
							case patch_DataHandler.SyncArrayOp.Del:
								patch_DataHandler.OpDelData(modArray, text, dataKey, propName, doLog);
								break;
							}
						}
						else
						{
							switch (arrayOp)
							{
							case patch_DataHandler.SyncArrayOp.Mod:
								Debug.LogWarning(string.Concat(new string[]
								{
									"Non-data [",
									text,
									"] in Data Block [",
									dataKey,
									"], Property [",
									propName,
									"] doesn't support '--MOD--' operation! Ignoring."
								}));
								break;
							case patch_DataHandler.SyncArrayOp.Add:
								patch_DataHandler.OpAddSimple(modArray, text, dataKey, propName, doLog);
								break;
							case patch_DataHandler.SyncArrayOp.Ins:
								patch_DataHandler.OpInsSimple(modArray, ref num, text, dataKey, propName, doLog);
								break;
							case patch_DataHandler.SyncArrayOp.Del:
								patch_DataHandler.OpDelSimple(modArray, text, dataKey, propName, doLog);
								break;
							}
						}
					}
				}
			}
		}
	}
	public static void OpModData(List<string> modArray, string refItem, string dataKey, string propName, bool doLog)
	{
		string[] array = refItem.Split(new char[]
		{
			'='
		});
		bool flag = false;
		for (int i = 0; i < modArray.Count; i++)
		{
			string[] array2 = modArray[i].Split(new char[]
			{
				'='
			});
			bool flag2 = array2[0] == array[0];
			if (flag2)
			{
				if (doLog)
				{
					Debug.Log(string.Concat(new string[]
					{
						"#Info# Data Block [",
						dataKey,
						"], Property [",
						propName,
						"], Parameter [",
						array[0],
						"]: ",
						array2[1],
						" => ",
						array[1]
					}));
				}
				modArray[i] = refItem;
				flag = true;
				break;
			}
		}
		bool flag3 = !flag;
		if (flag3)
		{
			Debug.LogWarning(string.Concat(new string[]
			{
				"Parameter [",
				array[0],
				"] was not found in Data Block [",
				dataKey,
				"], Property [",
				propName,
				"]! Ignoring."
			}));
		}
	}
	public static void OpAddData(List<string> modArray, string refItem, string dataKey, string propName, bool doLog)
	{
		string[] array = refItem.Split(new char[]
		{
			'='
		});
		if (doLog)
		{
			Debug.Log(string.Concat(new string[]
			{
				"#Info# Parameter [",
				array[0],
				"], Value [",
				array[1],
				"] was added to Data Block [",
				dataKey,
				"], Property [",
				propName,
				"]"
			}));
		}
		modArray.Add(refItem);
	}
	public static void OpInsData(List<string> modArray, ref int arrIndex, string refItem, string dataKey, string propName, bool doLog)
	{
		string[] array = refItem.Split(new char[]
		{
			'='
		});
		if (doLog)
		{
			Debug.Log(string.Concat(new string[]
			{
				"#Info# Parameter [",
				array[0],
				"], Value [",
				array[1],
				"] was inserted ",
				string.Format("into Data Block [{0}], Property [{1}] at Index [{2}]", dataKey, propName, arrIndex)
			}));
		}
		bool flag = arrIndex >= modArray.Count;
		if (flag)
		{
			Debug.LogWarning(string.Concat(new string[]
			{
				string.Format("Index [{0}] for Parameter [{1}] in Data ", arrIndex, array[0]),
				"Block [",
				dataKey,
				"], Property [",
				propName,
				"] is invalid! Adding instead."
			}));
			modArray.Add(refItem);
		}
		else
		{
			modArray.Insert(arrIndex, refItem);
		}
		arrIndex++;
	}
	public static void OpDelData(List<string> modArray, string refItem, string dataKey, string propName, bool doLog)
	{
		string[] array = refItem.Split(new char[]
		{
			'='
		});
		bool flag = false;
		int index = 0;
		for (int i = 0; i < modArray.Count; i++)
		{
			string[] array2 = modArray[i].Split(new char[]
			{
				'='
			});
			bool flag2 = array2[0] == array[0];
			if (flag2)
			{
				index = i;
				flag = true;
				break;
			}
		}
		bool flag3 = flag;
		if (flag3)
		{
			if (doLog)
			{
				Debug.Log(string.Concat(new string[]
				{
					"#Info# Parameter [",
					array[0],
					"] was removed from Data Block [",
					dataKey,
					"], Property [",
					propName,
					"]"
				}));
			}
			modArray.RemoveAt(index);
		}
		else
		{
			Debug.LogWarning(string.Concat(new string[]
			{
				"Parameter [",
				array[0],
				"] was not found in Data Block [",
				dataKey,
				"], Property [",
				propName,
				"]! Ignoring."
			}));
		}
	}
	public static void OpAddSimple(List<string> modArray, string refItem, string dataKey, string propName, bool doLog)
	{
		if (doLog)
		{
			Debug.Log(string.Concat(new string[]
			{
				"#Info# Parameter [",
				refItem,
				"] was added to Data Block [",
				dataKey,
				"], Property [",
				propName,
				"]"
			}));
		}
		modArray.Add(refItem);
	}
	public static void OpInsSimple(List<string> modArray, ref int arrIndex, string refItem, string dataKey, string propName, bool doLog)
	{
		if (doLog)
		{
			Debug.Log("#Info# Parameter [" + refItem + "] was inserted into " + string.Format("Data Block [{0}], Property [{1}] at Index [{2}]", dataKey, propName, arrIndex));
		}
		bool flag = arrIndex >= modArray.Count;
		if (flag)
		{
			Debug.LogWarning(string.Concat(new string[]
			{
				string.Format("Index [{0}] for Parameter [{1}] in Data ", arrIndex, refItem),
				"Block [",
				dataKey,
				"], Property [",
				propName,
				"] is invalid! Adding instead."
			}));
			modArray.Add(refItem);
		}
		else
		{
			modArray.Insert(arrIndex, refItem);
		}
		arrIndex++;
	}
	public static void OpDelSimple(List<string> modArray, string refItem, string dataKey, string propName, bool doLog)
	{
		bool flag = false;
		int index = 0;
		for (int i = 0; i < modArray.Count; i++)
		{
			bool flag2 = modArray[i].StartsWith(refItem);
			if (flag2)
			{
				index = i;
				flag = true;
				break;
			}
		}
		bool flag3 = flag;
		if (flag3)
		{
			if (doLog)
			{
				Debug.Log(string.Concat(new string[]
				{
					"#Info# Parameter [",
					refItem,
					"] was removed from Data Block [",
					dataKey,
					"], Property [",
					propName,
					"]"
				}));
			}
			modArray.RemoveAt(index);
		}
		else
		{
			Debug.LogWarning(string.Concat(new string[]
			{
				"Parameter [",
				refItem,
				"] was not found in Data Block [",
				dataKey,
				"], Property [",
				propName,
				"]!"
			}));
		}
	}
	public static string GetName(this object refObject)
	{
		BindingFlags bindingAttr = BindingFlags.Instance | BindingFlags.Public;
		Type type = refObject.GetType();
		string text = string.Empty;
		PropertyInfo property = type.GetProperty("strName");
		string text2;
		if (property == null)
		{
			text2 = null;
		}
		else
		{
			object value = property.GetValue(refObject, null);
			text2 = ((value != null) ? value.ToString() : null);
		}
		text = (text2 ?? string.Empty);
		bool flag = string.IsNullOrEmpty(text);
		if (flag)
		{
			FieldInfo field = type.GetField("strName", bindingAttr);
			string text3;
			if (field == null)
			{
				text3 = null;
			}
			else
			{
				object value2 = field.GetValue(refObject);
				text3 = ((value2 != null) ? value2.ToString() : null);
			}
			text = (text3 ?? string.Empty);
		}
		return text;
	}
	public static string GetIdentifier(this object refObject)
	{
		BindingFlags bindingAttr = BindingFlags.Instance | BindingFlags.Public;
		Type type = refObject.GetType();
		string[] array = new string[]
		{
			"strID",
			"strName"
		};
		string text = string.Empty;
		foreach (string name in array)
		{
			PropertyInfo property = type.GetProperty(name);
			string text2;
			if (property == null)
			{
				text2 = null;
			}
			else
			{
				object value = property.GetValue(refObject, null);
				text2 = ((value != null) ? value.ToString() : null);
			}
			text = (text2 ?? string.Empty);
			bool flag = string.IsNullOrEmpty(text);
			if (flag)
			{
				FieldInfo field = type.GetField(name, bindingAttr);
				string text3;
				if (field == null)
				{
					text3 = null;
				}
				else
				{
					object value2 = field.GetValue(refObject);
					text3 = ((value2 != null) ? value2.ToString() : null);
				}
				text = (text3 ?? string.Empty);
			}
			bool flag2 = !string.IsNullOrEmpty(text);
			if (flag2)
			{
				break;
			}
		}
		return text;
	}
	public static bool SetIdentifier(this object refObject, string newIdentifier)
	{
		BindingFlags bindingAttr = BindingFlags.Instance | BindingFlags.Public;
		Type type = refObject.GetType();
		string[] array = new string[]
		{
			"strID",
			"strName"
		};
		bool result = false;
		foreach (string name in array)
		{
			PropertyInfo property = type.GetProperty(name);
			bool flag = property != null;
			if (flag)
			{
				property.SetValue(refObject, newIdentifier, null);
				result = true;
				break;
			}
			FieldInfo field = type.GetField(name, bindingAttr);
			bool flag2 = field != null;
			if (flag2)
			{
				field.SetValue(refObject, newIdentifier);
				result = true;
				break;
			}
		}
		return result;
	}
	public static bool IsForbidden(this string strProp)
	{
		if (!true)
		{
		}
		bool result = strProp == "strID" || strProp == "strName" || strProp == "strReference";
		if (!true)
		{
		}
		return result;
	}
	public static object Sanitized(this object refObject)
	{
		bool flag = refObject == null;
		object result;
		if (flag)
		{
			result = "NULL";
		}
		else
		{
			string text = refObject as string;
			bool flag2 = text != null && text.Length == 0;
			if (flag2)
			{
				result = "EMPTY";
			}
			else
			{
				result = refObject;
			}
		}
		return result;
	}
	public static string Compressed(this string strValue)
	{
		return strValue.Replace("\n", "").Replace("\r", "").Replace("\t", "").Replace(" ", "");
	}
	public static bool IsCoreEntry(this string modKey)
	{
		return modKey == "core" || modKey == "Core";
	}
	public static bool IsExtedable(this string dataKey)
	{
		if (!true)
		{
		}
		uint num = <PrivateImplementationDetails>.ComputeStringHash(dataKey);
		if (num <= 2144291562U)
		{
			if (num <= 1300112001U)
			{
				if (num != 747869852U)
				{
					if (num != 1225351302U)
					{
						if (num != 1300112001U)
						{
							goto IL_18D;
						}
						if (!(dataKey == "names_robots"))
						{
							goto IL_18D;
						}
					}
					else if (!(dataKey == "crewskins"))
					{
						goto IL_18D;
					}
				}
				else if (!(dataKey == "names_ship_nouns"))
				{
					goto IL_18D;
				}
			}
			else if (num != 1359451292U)
			{
				if (num != 1739007045U)
				{
					if (num != 2144291562U)
					{
						goto IL_18D;
					}
					if (!(dataKey == "names_ship"))
					{
						goto IL_18D;
					}
				}
				else if (!(dataKey == "manpages"))
				{
					goto IL_18D;
				}
			}
			else if (!(dataKey == "traitscores"))
			{
				goto IL_18D;
			}
		}
		else if (num <= 2960291089U)
		{
			if (num != 2740638176U)
			{
				if (num != 2874613141U)
				{
					if (num != 2960291089U)
					{
						goto IL_18D;
					}
					if (!(dataKey == "strings"))
					{
						goto IL_18D;
					}
				}
				else if (!(dataKey == "names_full"))
				{
					goto IL_18D;
				}
			}
			else if (!(dataKey == "names_last"))
			{
				goto IL_18D;
			}
		}
		else if (num != 3654133458U)
		{
			if (num != 3949129551U)
			{
				if (num != 4156796638U)
				{
					goto IL_18D;
				}
				if (!(dataKey == "names_first"))
				{
					goto IL_18D;
				}
			}
			else if (!(dataKey == "names_ship_adjectives"))
			{
				goto IL_18D;
			}
		}
		else if (!(dataKey == "conditions_simple"))
		{
			goto IL_18D;
		}
		bool result = true;
		goto IL_191;
		IL_18D:
		result = false;
		IL_191:
		if (!true)
		{
		}
		return result;
	}
	public static bool TryGetCOValue(string strName, out JsonCondOwner refCO)
	{
		JsonCondOwner jsonCondOwner;
		bool flag = DataHandler.dictCOs.TryGetValue(strName, out jsonCondOwner);
		bool result;
		if (flag)
		{
			refCO = jsonCondOwner;
			result = true;
		}
		else
		{
			JsonCOOverlay jsonCOOverlay;
			bool flag2 = DataHandler.dictCOOverlays.TryGetValue(strName, out jsonCOOverlay);
			if (flag2)
			{
				JsonCondOwner jsonCondOwner2;
				bool flag3 = DataHandler.dictCOs.TryGetValue(jsonCOOverlay.strCOBase, out jsonCondOwner2);
				if (flag3)
				{
					refCO = jsonCondOwner2;
					return true;
				}
			}
			refCO = null;
			result = false;
		}
		return result;
	}
	public static void SwitchSlottedItems(JsonShip aShipRef, bool isTemplate)
	{
		bool flag = aShipRef == null;
		if (!flag)
		{
			List<JsonItem> list = (aShipRef.aItems != null) ? aShipRef.aItems.ToList<JsonItem>() : null;
			List<JsonCondOwnerSave> list2 = (aShipRef.aCOs != null) ? aShipRef.aCOs.ToList<JsonCondOwnerSave>() : null;
			bool flag2 = list == null;
			if (!flag2)
			{
				using (List<JsonItem>.Enumerator enumerator = list.GetEnumerator())
				{
					while (enumerator.MoveNext())
					{
						JsonItem aItem = enumerator.Current;
						bool flag3 = !string.IsNullOrEmpty(aItem.strSlotParentID);
						if (flag3)
						{
							JsonItem jsonItem = list.Find((JsonItem x) => x.strID == aItem.strSlotParentID);
							bool flag4 = jsonItem != null && patch_DataHandler.dictChangesMap.ContainsKey(jsonItem.strName) && patch_DataHandler.dictChangesMap[jsonItem.strName].ContainsKey("Switch_Slotted") && patch_DataHandler.dictChangesMap[jsonItem.strName]["Switch_Slotted"] != null;
							if (flag4)
							{
								Dictionary<string, string> dictionary = (from x in patch_DataHandler.dictChangesMap[jsonItem.strName]["Switch_Slotted"]
								where x.Split(new char[]
								{
									"="[0]
								}).Length == 2
								select x.Split(new char[]
								{
									"="[0]
								})).ToDictionary((string[] x) => x[0], (string[] x) => x[1]);
								bool flag5 = dictionary.ContainsKey(aItem.strName);
								if (flag5)
								{
									string text = dictionary[aItem.strName];
									bool flag6 = string.IsNullOrEmpty(text);
									if (!flag6)
									{
										Debug.Log(string.Concat(new string[]
										{
											"#Info# Found the mismatched CO [",
											aItem.strName,
											":",
											aItem.strID,
											"] for the Parent CO [",
											jsonItem.strName,
											":",
											jsonItem.strID,
											"] for remapping! Syncing to the CO [",
											text,
											"] from the template."
										}));
										JsonCondOwner jsonCondOwner;
										bool flag7 = patch_DataHandler.TryGetCOValue(text, out jsonCondOwner);
										if (flag7)
										{
											aItem.strName = jsonCondOwner.strName;
											JsonCondOwner jsonCondOwner2;
											bool flag8 = !isTemplate && list2 != null && patch_DataHandler.TryGetCOValue(jsonItem.strName, out jsonCondOwner2);
											if (flag8)
											{
												JsonCondOwnerSave jsonCondOwnerSave = list2.Find((JsonCondOwnerSave x) => x.strID == aItem.strID);
												bool flag9 = jsonCondOwnerSave != null;
												if (flag9)
												{
													jsonCondOwnerSave.strSlotName = jsonCondOwner.mapSlotEffects.Intersect(jsonCondOwner2.aSlotsWeHave).First<string>();
													jsonCondOwnerSave.strCondID = jsonCondOwner.strName + aItem.strID;
													jsonCondOwnerSave.strFriendlyName = jsonCondOwner.strNameFriendly;
													jsonCondOwnerSave.strCODef = jsonCondOwner.strName;
												}
											}
										}
									}
								}
							}
						}
					}
				}
				bool flag10 = list2 != null;
				if (flag10)
				{
					aShipRef.aCOs = list2.ToArray();
				}
				aShipRef.aItems = list.ToArray();
			}
		}
	}
	public static void RecoverMissingItems(JsonShip aShipRef)
	{
		bool flag = aShipRef == null;
		if (!flag)
		{
			List<JsonItem> list = (aShipRef.aItems != null) ? aShipRef.aItems.ToList<JsonItem>() : null;
			List<JsonItem> list2 = new List<JsonItem>();
			bool flag2 = list == null;
			if (!flag2)
			{
				using (List<JsonItem>.Enumerator enumerator = list.GetEnumerator())
				{
					while (enumerator.MoveNext())
					{
						JsonItem aItem = enumerator.Current;
						JsonCondOwner jsonCondOwner;
						bool flag3 = patch_DataHandler.dictChangesMap.ContainsKey(aItem.strName) && patch_DataHandler.dictChangesMap[aItem.strName].ContainsKey("Recover_Missing") && patch_DataHandler.dictChangesMap[aItem.strName]["Recover_Missing"] != null && patch_DataHandler.TryGetCOValue(aItem.strName, out jsonCondOwner);
						if (flag3)
						{
							List<string> targetKeys = patch_DataHandler.dictChangesMap[aItem.strName]["Recover_Missing"].ToList<string>();
							bool isInverse = targetKeys.Remove("*IsInverse*");
							bool doAll = targetKeys.Count == 0;
							bool flag4 = jsonCondOwner.aSlotsWeHave != null && jsonCondOwner.aSlotsWeHave.Length != 0 && jsonCondOwner.strLoot != null && DataHandler.dictLoot.ContainsKey(jsonCondOwner.strLoot);
							if (flag4)
							{
								List<string> second = (from x in list.FindAll((JsonItem x) => (x.strSlotParentID == aItem.strID && patch_DataHandler.listLockedCOs.Contains(x.strName)) || targetKeys.Contains(x.strName))
								select x.strName).ToList<string>();
								List<string> first = (from x in DataHandler.dictLoot[jsonCondOwner.strLoot].GetAllLootNames()
								where (doAll && patch_DataHandler.listLockedCOs.Contains(x)) || (!isInverse && targetKeys.Contains(x)) || (isInverse && !targetKeys.Contains(x) && patch_DataHandler.listLockedCOs.Contains(x))
								select x).ToList<string>();
								List<string> list3 = first.Except(second).ToList<string>();
								foreach (string text in list3)
								{
									JsonItem jsonItem = new JsonItem();
									jsonItem.strName = text;
									jsonItem.fX = aItem.fX;
									jsonItem.fY = aItem.fY;
									jsonItem.fRotation = 0f;
									jsonItem.strID = Guid.NewGuid().ToString();
									jsonItem.strSlotParentID = aItem.strID;
									jsonItem.bForceLoad = aItem.bForceLoad;
									Debug.Log(string.Concat(new string[]
									{
										"#Info# Found the missing locked CO [",
										text,
										"] for the Parent CO [",
										aItem.strName,
										":",
										aItem.strID,
										"] in the list! New ID [",
										jsonItem.strID,
										"], adding."
									}));
									list2.Add(jsonItem);
								}
							}
						}
					}
				}
				list.AddRange(list2);
				aShipRef.aItems = list.ToArray();
			}
		}
	}
	public static void RecoverMissingCOs(JsonShip aShipRef)
	{
		bool flag = aShipRef == null;
		if (!flag)
		{
			List<JsonItem> list = (aShipRef.aItems != null) ? aShipRef.aItems.ToList<JsonItem>() : null;
			List<JsonCondOwnerSave> list2 = (aShipRef.aCOs != null) ? aShipRef.aCOs.ToList<JsonCondOwnerSave>() : null;
			bool flag2 = list == null || list2 == null;
			if (!flag2)
			{
				using (List<JsonItem>.Enumerator enumerator = list.GetEnumerator())
				{
					while (enumerator.MoveNext())
					{
						JsonItem aItem = enumerator.Current;
						bool flag3 = list2.Find((JsonCondOwnerSave x) => x.strID == aItem.strID) == null;
						if (flag3)
						{
							bool flag4 = !string.IsNullOrEmpty(aItem.strSlotParentID);
							if (flag4)
							{
								JsonItem jsonItem = list.Find((JsonItem x) => x.strID == aItem.strSlotParentID);
								JsonCondOwnerSave jsonCondOwnerSave = list2.Find((JsonCondOwnerSave x) => x.strID == aItem.strSlotParentID);
								bool flag5 = jsonItem == null || jsonCondOwnerSave == null;
								if (!flag5)
								{
									JsonCondOwner jsonCondOwner;
									JsonCondOwner jsonCondOwner2;
									bool flag6 = patch_DataHandler.TryGetCOValue(aItem.strName, out jsonCondOwner) && patch_DataHandler.TryGetCOValue(jsonItem.strName, out jsonCondOwner2) && patch_DataHandler.dictChangesMap.ContainsKey(jsonItem.strName) && patch_DataHandler.dictChangesMap[jsonItem.strName].ContainsKey("Recover_Missing");
									if (flag6)
									{
										Debug.Log(string.Concat(new string[]
										{
											"#Info# Found the CO [",
											aItem.strName,
											":",
											aItem.strID,
											"] with missing save data! Creating data from template."
										}));
										bool flag7 = jsonCondOwner.strType == "Item";
										if (flag7)
										{
											JsonCondOwnerSave jsonCondOwnerSave2 = new JsonCondOwnerSave();
											jsonCondOwnerSave2.strID = aItem.strID;
											jsonCondOwnerSave2.strCODef = aItem.strName;
											jsonCondOwnerSave2.strCondID = aItem.strName + aItem.strID;
											jsonCondOwnerSave2.bAlive = true;
											jsonCondOwnerSave2.inventoryX = 0;
											jsonCondOwnerSave2.inventoryY = 0;
											jsonCondOwnerSave2.fDGasTemp = 0.0;
											jsonCondOwnerSave2.nDestTile = 0;
											jsonCondOwnerSave2.strIdleAnim = "Idle";
											jsonCondOwnerSave2.fMSRedamageAmount = 0.0;
											jsonCondOwnerSave2.fLastICOUpdate = StarSystem.fEpoch;
											jsonCondOwnerSave2.aConds = jsonCondOwner.aStartingConds.Concat(new string[]
											{
												"DEFAULT"
											}).ToArray<string>();
											jsonCondOwnerSave2.strSlotName = jsonCondOwner.mapSlotEffects.Intersect(jsonCondOwner2.aSlotsWeHave).First<string>();
											JsonCondOwnerSave jsonCondOwnerSave3 = jsonCondOwnerSave2;
											JsonItemDef itemDef = DataHandler.GetItemDef(jsonCondOwner.strItemDef);
											jsonCondOwnerSave3.strIMGPreview = ((itemDef != null) ? itemDef.strImg : null);
											bool flag8 = jsonCondOwnerSave2.strIMGPreview == null;
											if (flag8)
											{
												jsonCondOwnerSave2.strIMGPreview = "blank";
											}
											jsonCondOwnerSave2.strFriendlyName = jsonCondOwner.strNameFriendly;
											jsonCondOwnerSave2.strRegIDLast = aShipRef.strRegID;
											list2.Add(jsonCondOwnerSave2);
										}
										else
										{
											Debug.LogWarning("Warning! The [" + aItem.strName + "] isn't item CO and not supported! Ignoring.");
										}
									}
									else
									{
										Debug.LogWarning("Warning! Template CO [" + aItem.strName + "] for parent or item doesn't exist! Ignoring.");
									}
								}
							}
						}
					}
				}
				aShipRef.aCOs = list2.ToArray();
			}
		}
	}
	public static void SyncConditions(JsonShip aShipRef)
	{
		bool flag = aShipRef == null;
		if (!flag)
		{
			List<JsonCondOwnerSave> list = (aShipRef.aCOs != null) ? aShipRef.aCOs.ToList<JsonCondOwnerSave>() : null;
			bool flag2 = list == null;
			if (!flag2)
			{
				foreach (JsonCondOwnerSave jsonCondOwnerSave in list)
				{
					JsonCondOwner jsonCondOwner;
					bool flag3 = jsonCondOwnerSave != null && patch_DataHandler.dictChangesMap.ContainsKey(jsonCondOwnerSave.strCODef) && patch_DataHandler.dictChangesMap[jsonCondOwnerSave.strCODef].ContainsKey("Sync_Conditions") && patch_DataHandler.dictChangesMap[jsonCondOwnerSave.strCODef]["Sync_Conditions"] != null && patch_DataHandler.TryGetCOValue(jsonCondOwnerSave.strCODef, out jsonCondOwner);
					if (flag3)
					{
						List<string> targetKeys = patch_DataHandler.dictChangesMap[jsonCondOwnerSave.strCODef]["Sync_Conditions"].ToList<string>();
						bool isInverse = targetKeys.Remove("*IsInverse*");
						bool doAll = targetKeys.Count == 0;
						bool flag4 = jsonCondOwner.aStartingConds != null && jsonCondOwnerSave.aConds != null && jsonCondOwner.aStartingConds.Length != 0 && jsonCondOwnerSave.aConds.Length >= 0;
						if (flag4)
						{
							List<string> list2 = jsonCondOwnerSave.aConds.ToList<string>();
							List<string> second = (from x in jsonCondOwnerSave.aConds
							select x.Split(new char[]
							{
								'='
							})[0]).ToList<string>();
							List<string> first = (from x in jsonCondOwner.aStartingConds
							select x.Split(new char[]
							{
								'='
							})[0] into x
							where doAll || (!isInverse && targetKeys.Contains(x)) || (isInverse && !targetKeys.Contains(x))
							select x).ToList<string>();
							List<string> list3 = first.Except(second).ToList<string>();
							using (List<string>.Enumerator enumerator2 = list3.GetEnumerator())
							{
								while (enumerator2.MoveNext())
								{
									string newCondKey = enumerator2.Current;
									string text = jsonCondOwner.aStartingConds.ToList<string>().Find((string x) => x.StartsWith(newCondKey + "="));
									Debug.Log(string.Concat(new string[]
									{
										"#Info# Saved CO [",
										jsonCondOwnerSave.strCODef,
										":",
										jsonCondOwnerSave.strID,
										"] is missing [",
										text,
										"] condition! Syncing to the CO from the template."
									}));
									list2.Insert(0, text);
								}
							}
							bool flag5 = list3.Count > 0;
							if (flag5)
							{
								jsonCondOwnerSave.aConds = list2.ToArray();
							}
						}
					}
				}
				aShipRef.aCOs = list.ToArray();
			}
		}
	}
	public static void UpdateConditions(JsonShip aShipRef)
	{
		bool flag = aShipRef == null;
		if (!flag)
		{
			List<JsonCondOwnerSave> list = (aShipRef.aCOs != null) ? aShipRef.aCOs.ToList<JsonCondOwnerSave>() : null;
			bool flag2 = list == null;
			if (!flag2)
			{
				foreach (JsonCondOwnerSave jsonCondOwnerSave in list)
				{
					JsonCondOwner jsonCondOwner;
					bool flag3 = jsonCondOwnerSave != null && patch_DataHandler.dictChangesMap.ContainsKey(jsonCondOwnerSave.strCODef) && patch_DataHandler.dictChangesMap[jsonCondOwnerSave.strCODef].ContainsKey("Update_Conditions") && patch_DataHandler.dictChangesMap[jsonCondOwnerSave.strCODef]["Update_Conditions"] != null && patch_DataHandler.TryGetCOValue(jsonCondOwnerSave.strCODef, out jsonCondOwner);
					if (flag3)
					{
						List<string> targetKeys = patch_DataHandler.dictChangesMap[jsonCondOwnerSave.strCODef]["Update_Conditions"].ToList<string>();
						bool isInverse = targetKeys.Remove("*IsInverse*");
						bool doAll = targetKeys.Count == 0;
						bool flag4 = jsonCondOwner.aStartingConds != null && jsonCondOwnerSave.aConds != null && jsonCondOwner.aStartingConds.Length != 0 && jsonCondOwnerSave.aConds.Length >= 0;
						if (flag4)
						{
							List<string> list2 = jsonCondOwnerSave.aConds.ToList<string>();
							List<string> second = (from x in jsonCondOwnerSave.aConds
							select x.Split(new char[]
							{
								'='
							})[0]).ToList<string>();
							List<string> first = (from x in jsonCondOwner.aStartingConds
							select x.Split(new char[]
							{
								'='
							})[0] into x
							where doAll || (!isInverse && targetKeys.Contains(x)) || (isInverse && !targetKeys.Contains(x))
							select x).ToList<string>();
							List<string> list3 = first.Intersect(second).ToList<string>();
							using (List<string>.Enumerator enumerator2 = list3.GetEnumerator())
							{
								while (enumerator2.MoveNext())
								{
									string extCondKey = enumerator2.Current;
									string value = jsonCondOwner.aStartingConds.ToList<string>().Find((string x) => x.StartsWith(extCondKey + "="));
									string item = list2.Find((string x) => x.StartsWith(extCondKey + "="));
									Debug.Log(string.Concat(new string[]
									{
										"#Info# Saved CO [",
										jsonCondOwnerSave.strCODef,
										":",
										jsonCondOwnerSave.strID,
										"] condition [",
										extCondKey,
										"] received new value from the template CO."
									}));
									list2[list2.IndexOf(item)] = value;
								}
							}
							bool flag5 = list3.Count > 0;
							if (flag5)
							{
								jsonCondOwnerSave.aConds = list2.ToArray();
							}
						}
					}
				}
				aShipRef.aCOs = list.ToArray();
			}
		}
	}
	public static void SyncSlotEffects(JsonShip aShipRef)
	{
		bool flag = aShipRef == null;
		if (!flag)
		{
			List<JsonItem> aItemList = (aShipRef.aItems != null) ? aShipRef.aItems.ToList<JsonItem>() : null;
			List<JsonCondOwnerSave> list = (aShipRef.aCOs != null) ? aShipRef.aCOs.ToList<JsonCondOwnerSave>() : null;
			bool flag2 = aItemList == null || list == null;
			if (!flag2)
			{
				using (List<JsonCondOwnerSave>.Enumerator enumerator = list.GetEnumerator())
				{
					while (enumerator.MoveNext())
					{
						JsonCondOwnerSave aSavedCO = enumerator.Current;
						JsonCondOwner jsonCondOwner;
						bool flag3 = aSavedCO != null && patch_DataHandler.dictChangesMap.ContainsKey(aSavedCO.strCODef) && patch_DataHandler.dictChangesMap[aSavedCO.strCODef].ContainsKey("Sync_Slot_Effects") && patch_DataHandler.dictChangesMap[aSavedCO.strCODef]["Sync_Slot_Effects"] != null && patch_DataHandler.dictChangesMap[aSavedCO.strCODef]["Sync_Slot_Effects"].Count > 0 && patch_DataHandler.TryGetCOValue(aSavedCO.strCODef, out jsonCondOwner);
						if (flag3)
						{
							List<string> list2 = (from x in patch_DataHandler.dictChangesMap[aSavedCO.strCODef]["Sync_Slot_Effects"]
							where x.Contains("=")
							select x).ToList<string>();
							List<string> list3 = (from x in patch_DataHandler.dictChangesMap[aSavedCO.strCODef]["Sync_Slot_Effects"]
							where x.StartsWith("!")
							select x.Substring(1)).ToList<string>();
							List<JsonCondOwnerSave> list4 = list.FindAll((JsonCondOwnerSave x) => aItemList.Any((JsonItem i) => i.strSlotParentID == aSavedCO.strID && i.strID == x.strID));
							foreach (JsonCondOwnerSave jsonCondOwnerSave in list4)
							{
								List<string> list5 = (jsonCondOwnerSave.aConds != null) ? jsonCondOwnerSave.aConds.ToList<string>() : new List<string>();
								foreach (string text in list2)
								{
									string text2 = text.Split(new char[]
									{
										"|"[0]
									})[0];
									string addCondKey = text2.Split(new char[]
									{
										"="[0]
									})[0];
									List<string> list6 = text.Split(new char[]
									{
										"|"[0]
									}).Skip(1).ToList<string>();
									bool flag4 = !list5.Any((string x) => x.StartsWith(addCondKey + "=")) && (list6.Count == 0 || list6.Contains(jsonCondOwnerSave.strCODef));
									if (flag4)
									{
										Debug.Log(string.Concat(new string[]
										{
											"#Info# Saved CO [",
											jsonCondOwnerSave.strCODef,
											":",
											jsonCondOwnerSave.strID,
											"] got condition [",
											text2,
											"] due to the Parent CO [",
											aSavedCO.strCODef,
											":",
											aSavedCO.strID,
											"] slot effects."
										}));
										list5.Insert(0, text2);
									}
								}
								foreach (string text3 in list3)
								{
									string text4 = text3.Split(new char[]
									{
										"|"[0]
									})[0];
									string remCondsKey = text4.Split(new char[]
									{
										"="[0]
									})[0];
									List<string> list7 = text3.Split(new char[]
									{
										"|"[0]
									}).Skip(1).ToList<string>();
									bool flag5 = list5.Any((string x) => x.StartsWith(remCondsKey + "=")) && (list7.Count == 0 || list7.Contains(jsonCondOwnerSave.strCODef));
									if (flag5)
									{
										Debug.Log(string.Concat(new string[]
										{
											"#Info# Saved CO [",
											jsonCondOwnerSave.strCODef,
											":",
											jsonCondOwnerSave.strID,
											"] lost condition [",
											text4,
											"] due to the Parent CO [",
											aSavedCO.strCODef,
											":",
											aSavedCO.strID,
											"] slot effects."
										}));
										list5.Remove(list5.Find((string x) => x.StartsWith(remCondsKey + "=")));
									}
								}
								bool flag6 = list2.Count > 0 || list3.Count > 0;
								if (flag6)
								{
									jsonCondOwnerSave.aConds = list5.ToArray();
								}
							}
						}
					}
				}
			}
		}
	}
	public static void SyncInvEffects(JsonShip aShipRef)
	{
		bool flag = aShipRef == null;
		if (!flag)
		{
			List<JsonItem> aItemList = (aShipRef.aItems != null) ? aShipRef.aItems.ToList<JsonItem>() : null;
			List<JsonCondOwnerSave> list = (aShipRef.aCOs != null) ? aShipRef.aCOs.ToList<JsonCondOwnerSave>() : null;
			bool flag2 = aItemList == null || list == null;
			if (!flag2)
			{
				using (List<JsonCondOwnerSave>.Enumerator enumerator = list.GetEnumerator())
				{
					while (enumerator.MoveNext())
					{
						JsonCondOwnerSave aSavedCO = enumerator.Current;
						JsonCondOwner jsonCondOwner;
						bool flag3 = aSavedCO != null && patch_DataHandler.dictChangesMap.ContainsKey(aSavedCO.strCODef) && patch_DataHandler.dictChangesMap[aSavedCO.strCODef].ContainsKey("Sync_Inv_Effects") && patch_DataHandler.dictChangesMap[aSavedCO.strCODef]["Sync_Inv_Effects"] != null && patch_DataHandler.dictChangesMap[aSavedCO.strCODef]["Sync_Inv_Effects"].Count > 0 && patch_DataHandler.TryGetCOValue(aSavedCO.strCODef, out jsonCondOwner);
						if (flag3)
						{
							List<string> list2 = (from x in patch_DataHandler.dictChangesMap[aSavedCO.strCODef]["Sync_Inv_Effects"]
							where x.Contains("=")
							select x).ToList<string>();
							List<string> list3 = (from x in patch_DataHandler.dictChangesMap[aSavedCO.strCODef]["Sync_Inv_Effects"]
							where x.StartsWith("!")
							select x.Substring(1)).ToList<string>();
							List<JsonCondOwnerSave> list4 = list.FindAll((JsonCondOwnerSave x) => aItemList.Any((JsonItem i) => i.strParentID == aSavedCO.strID && i.strID == x.strID));
							foreach (JsonCondOwnerSave jsonCondOwnerSave in list4)
							{
								List<string> list5 = (jsonCondOwnerSave.aConds != null) ? jsonCondOwnerSave.aConds.ToList<string>() : new List<string>();
								foreach (string text in list2)
								{
									string text2 = text.Split(new char[]
									{
										"|"[0]
									})[0];
									string addCondKey = text2.Split(new char[]
									{
										"="[0]
									})[0];
									List<string> list6 = text.Split(new char[]
									{
										"|"[0]
									}).Skip(1).ToList<string>();
									bool flag4 = !list5.Any((string x) => x.StartsWith(addCondKey + "=")) && (list6.Count == 0 || list6.Contains(jsonCondOwnerSave.strCODef));
									if (flag4)
									{
										Debug.Log(string.Concat(new string[]
										{
											"#Info# Saved CO [",
											jsonCondOwnerSave.strCODef,
											":",
											jsonCondOwnerSave.strID,
											"] got condition [",
											text2,
											"] due to the Parent CO [",
											aSavedCO.strCODef,
											":",
											aSavedCO.strID,
											"] inventory effects."
										}));
										list5.Insert(0, text2);
									}
								}
								foreach (string text3 in list3)
								{
									string text4 = text3.Split(new char[]
									{
										"|"[0]
									})[0];
									string remCondsKey = text4.Split(new char[]
									{
										"="[0]
									})[0];
									List<string> list7 = text3.Split(new char[]
									{
										"|"[0]
									}).Skip(1).ToList<string>();
									bool flag5 = list5.Any((string x) => x.StartsWith(remCondsKey + "=")) && (list7.Count == 0 || list7.Contains(jsonCondOwnerSave.strCODef));
									if (flag5)
									{
										Debug.Log(string.Concat(new string[]
										{
											"#Info# Saved CO [",
											jsonCondOwnerSave.strCODef,
											":",
											jsonCondOwnerSave.strID,
											"] lost condition [",
											text4,
											"] due to the Parent CO [",
											aSavedCO.strCODef,
											":",
											aSavedCO.strID,
											"] inventory effects."
										}));
										list5.Remove(list5.Find((string x) => x.StartsWith(remCondsKey + "=")));
									}
								}
								bool flag6 = list2.Count > 0 || list3.Count > 0;
								if (flag6)
								{
									jsonCondOwnerSave.aConds = list5.ToArray();
								}
							}
						}
					}
				}
			}
		}
	}
	public static string strModsPath = string.Empty;
	public static Dictionary<string, Dictionary<string, List<string>>> dictChangesMap;
	public static List<string> listLockedCOs = new List<string>();
	[MonoModIgnore]
	public static Dictionary<string, patch_JsonModInfo> dictModInfos;
	public const string OP_MOD = "--MOD--";
	public const string OP_ADD = "--ADD--";
	public const string OP_INS = "--INS--";
	public const string OP_DEL = "--DEL--";
	public enum SyncArrayOp
	{
		None,
		Mod,
		Add,
		Ins,
		Del
	}
}
