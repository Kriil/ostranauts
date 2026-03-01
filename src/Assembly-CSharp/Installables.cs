using System;
using System.Collections.Generic;
using UnityEngine;

public class Installables
{
	public static void Create(JsonInstallable ji)
	{
		if (ji == null)
		{
			return;
		}
		JsonInteraction jsonInteraction = null;
		if (ji.bHeadless || DataHandler.dictInteractions.TryGetValue(ji.strInteractionTemplate, out jsonInteraction))
		{
			JsonInteraction jsonInteraction2 = null;
			if (DataHandler.dictInteractions.TryGetValue(ji.strInteractionTemplate + "Allow", out jsonInteraction2))
			{
				string text = "ACT" + ji.strName + "Allow";
				if (!ji.bHeadless)
				{
					JsonInteraction jsonInteraction3 = jsonInteraction.Clone();
					jsonInteraction3.strName = "ACT" + ji.strName;
					jsonInteraction3.strStartInstall = ji.strStartInstall;
					jsonInteraction3.strActionGroup = ji.strActionGroup;
					ji.strInteractionName = jsonInteraction3.strName;
					jsonInteraction3.fTargetPointRange = ji.fTargetPointRange;
					List<string> list = new List<string>
					{
						text + ",[us],[them]"
					};
					if (ji.aInverse != null)
					{
						foreach (string item in ji.aInverse)
						{
							list.Add(item);
						}
					}
					jsonInteraction3.aInverse = list.ToArray();
					jsonInteraction3.CTTestUs = ji.CTUs;
					jsonInteraction3.CTTestThem = ji.CTThem;
					List<string> list2 = new List<string>();
					if (jsonInteraction3.aLootItms != null)
					{
						list2.AddRange(jsonInteraction3.aLootItms);
					}
					if (ji.aInputs != null && ji.aInputs.Length > 0)
					{
						Loot loot = new Loot();
						loot.strName = "FeedItmCT" + jsonInteraction3.strName;
						loot.strType = "trigger";
						loot.aCOs = ji.aInputs;
						DataHandler.dictLoot[loot.strName] = loot;
						list2.Add("input," + loot.strName + ",true");
					}
					if (ji.aToolCTsUse != null && ji.aToolCTsUse.Length > 0)
					{
						string toolUseLoot = Installables.GetToolUseLoot(ji);
						if (toolUseLoot != null)
						{
							list2.Add("Use," + toolUseLoot + ",true,");
						}
					}
					jsonInteraction3.aLootItms = list2.ToArray();
					DataHandler.dictInteractions[jsonInteraction3.strName] = jsonInteraction3;
					JsonCondOwner jsonCondOwner = null;
					DataHandler.dictCOs.TryGetValue(ji.strActionCO, out jsonCondOwner);
					if (jsonCondOwner != null)
					{
						list.Clear();
						if (jsonCondOwner.aInteractions != null)
						{
							list.AddRange(jsonCondOwner.aInteractions);
						}
						list.Add(jsonInteraction3.strName);
						jsonCondOwner.aInteractions = list.ToArray();
					}
					if (ji.strJobType == "install")
					{
						if (ji.strBuildType == null)
						{
							Debug.Log("ERROR: Null strBuildType for install job " + ji.strName);
						}
						else
						{
							if (Installables.dictJobBuildOptions == null)
							{
								Installables.dictJobBuildOptions = new Dictionary<string, Dictionary<string, JsonInstallable>>();
								Installables.dictJobBuildOptionsListed = new Dictionary<string, Dictionary<string, JsonInstallable>>();
							}
							if (!Installables.dictJobBuildOptions.ContainsKey(ji.strBuildType))
							{
								Installables.dictJobBuildOptions[ji.strBuildType] = new Dictionary<string, JsonInstallable>();
								Installables.dictJobBuildOptionsListed[ji.strBuildType] = new Dictionary<string, JsonInstallable>();
							}
							Installables.dictJobBuildOptions[ji.strBuildType][ji.strStartInstall] = ji;
							if (!ji.bNoJobMenu)
							{
								Installables.dictJobBuildOptionsListed[ji.strBuildType][ji.strStartInstall] = ji;
							}
						}
					}
					else if (ji.strJobType != null && jsonCondOwner != null)
					{
						jsonCondOwner.AddJobAction(ji.strJobType, jsonInteraction3.strName);
					}
				}
				JsonInteraction jsonInteraction4 = jsonInteraction2.Clone();
				jsonInteraction4.strName = text;
				jsonInteraction4.fDuration = (double)ji.fDuration;
				jsonInteraction4.strActionGroup = ji.strActionGroup;
				jsonInteraction4.fTargetPointRange = ji.fTargetPointRange;
				jsonInteraction4.CTTestUs = ji.CTAllowUs;
				if (ji.strJobType == "install")
				{
					jsonInteraction4.CTTestThem = "TIsPlaceholder";
				}
				else
				{
					jsonInteraction4.CTTestThem = ji.CTThem;
				}
				if (ji.aToolCTsUse != null && ji.aToolCTsUse.Length > 0)
				{
					List<string> list3 = new List<string>();
					if (jsonInteraction4.aLootItms != null)
					{
						list3.AddRange(jsonInteraction4.aLootItms);
					}
					string toolUseLoot2 = Installables.GetToolUseLoot(ji);
					if (toolUseLoot2 != null)
					{
						list3.Add("Use," + toolUseLoot2 + ",true,");
						jsonInteraction4.aLootItms = list3.ToArray();
					}
				}
				jsonInteraction4.aInverse = new string[]
				{
					text + ",[us],[them]"
				};
				if (!ji.bNoDestructable && (ji.aLootCOs != null || ji.strLootOut != null))
				{
					JsonCondOwner jsonCondOwner2 = null;
					if (DataHandler.dictCOs.TryGetValue(ji.strActionCO, out jsonCondOwner2))
					{
						string text2 = ji.strLootOut;
						if (text2 == null)
						{
							text2 = "Output" + ji.strName;
							if (ji.aLootCOs.Length > 0 && !DataHandler.dictLoot.ContainsKey(text2))
							{
								List<string> list4 = new List<string>();
								Loot loot2 = new Loot();
								loot2.strName = text2;
								loot2.strType = "item";
								foreach (string str in ji.aLootCOs)
								{
									list4.Add(str + "=1.0x1");
								}
								loot2.aCOs = list4.ToArray();
								DataHandler.dictLoot[text2] = loot2;
							}
						}
						string text3 = ji.strFinishInteraction;
						if (ji.strFinishInteraction == null)
						{
							text3 = "MS" + ji.strName;
						}
						if (!DataHandler.dictInteractions.ContainsKey(text3))
						{
							JsonInteraction jsonInteraction5 = new JsonInteraction();
							jsonInteraction5.strName = text3;
							jsonInteraction5.strDesc = "[us] is done.";
							jsonInteraction5.strThemType = "Other";
							jsonInteraction5.objLootModeSwitch = text2;
							DataHandler.dictInteractions[text3] = jsonInteraction5;
						}
						if (!DataHandler.dictLoot.ContainsKey(text3))
						{
							List<string> list5 = new List<string>
							{
								text3 + "=1.0x1"
							};
							Loot loot3 = new Loot();
							loot3.strName = text3;
							loot3.strType = "interaction";
							loot3.aCOs = list5.ToArray();
							DataHandler.dictLoot[text3] = loot3;
						}
						string text4 = ji.strProgressStat + "Max";
						string text5 = string.Concat(new string[]
						{
							"Destructable,",
							ji.strProgressStat,
							",",
							text3,
							",",
							text4,
							",1.0"
						});
						string[] array = null;
						if (jsonCondOwner2.aUpdateCommands == null)
						{
							array = new string[]
							{
								text5
							};
						}
						else if (Array.IndexOf<string>(jsonCondOwner2.aUpdateCommands, text5) < 0)
						{
							array = new string[jsonCondOwner2.aUpdateCommands.Length + 1];
							for (int k = 0; k < jsonCondOwner2.aUpdateCommands.Length; k++)
							{
								array[k] = jsonCondOwner2.aUpdateCommands[k];
							}
							array[array.Length - 1] = text5;
						}
						if (array != null)
						{
							jsonCondOwner2.aUpdateCommands = array;
						}
					}
				}
				jsonInteraction4.LootCTsThem = ji.strAllowLootCTsThem;
				jsonInteraction4.LootCTsUs = ji.strAllowLootCTsUs;
				jsonInteraction4.strCTThemMultCondUs = ji.strCTThemMultCondUs;
				jsonInteraction4.strCTThemMultCondTools = ji.strCTThemMultCondTools;
				DataHandler.dictInteractions[jsonInteraction4.strName] = jsonInteraction4;
				DataHandler.dictInstallables2.Add(text, ji);
			}
			else
			{
				Debug.Log("WARNING: Unable to find installable strInteractionTemplate: " + ji.strInteractionTemplate + "Allow on installable: " + ji.strName);
			}
		}
	}

	public static string GetToolUseLoot(JsonInstallable ji)
	{
		if (ji == null)
		{
			return null;
		}
		string text = "ToolsACT" + ji.strName;
		if (DataHandler.dictLoot.ContainsKey(text))
		{
			return text;
		}
		List<string> list = new List<string>();
		Loot loot = new Loot();
		loot.strName = text;
		loot.strType = "trigger";
		foreach (string str in ji.aToolCTsUse)
		{
			list.Add(str + "=1.0x1");
		}
		loot.aCOs = list.ToArray();
		DataHandler.dictLoot[loot.strName] = loot;
		return loot.strName;
	}

	public static JsonInstallable GetJsonInstallable(string strStartInstall)
	{
		if (strStartInstall == null)
		{
			return null;
		}
		foreach (string key in Installables.dictJobBuildOptions.Keys)
		{
			if (Installables.dictJobBuildOptions[key].ContainsKey(strStartInstall))
			{
				return Installables.dictJobBuildOptions[key][strStartInstall].Clone();
			}
		}
		return null;
	}

	public static Dictionary<string, Dictionary<string, JsonInstallable>> dictJobBuildOptions;

	public static Dictionary<string, Dictionary<string, JsonInstallable>> dictJobBuildOptionsListed;
}
