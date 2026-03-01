using System;
using System.Collections;
using System.Collections.Generic;
using Ostranauts.JsonTypes.Interfaces;
using Ostranauts.Tools.ExtensionMethods;
using UnityEngine;

// Data definition for one Interaction, meaning an action a character can perform.
// Likely loaded from StreamingAssets/data/interactions or data/interaction_overrides,
// then expanded into live Interaction instances when the AI or UI queues an action.
[Serializable]
public class JsonInteraction : IVerifiable
{
	// Registry id plus player-facing text used by quick-action buttons, tooltips, and logs.
	public string strName { get; set; }

	public string strTitle { get; set; }

	public string strDesc { get; set; }

	public string[] strVerbs { get; set; }

	public string strTooltip { get; set; }

	public string strTargetPoint { get; set; }

	public float fTargetPointRange { get; set; }

	public float fForcedChance { get; set; }

	public string strAnim { get; set; }

	public string strAnimTrig { get; set; }

	public string strBubble { get; set; }

	public string strColor { get; set; }

	public string strDuty { get; set; }

	public string[] aInverse { get; set; }

	public double fDuration { get; set; }

	public float fRotation { get; set; }

	public string strTeleport { get; set; }

	public string strTeleportRegID { get; set; }

	public string strTeleportTarget { get; set; }

	public string strIdleAnim { get; set; }

	public string strCancelInteraction { get; set; }

	public string strUseCase { get; set; }

	public string strAttackMode { get; set; }

	public string strAttackerName { get; set; }

	public string strIAHit { get; set; }

	public string strIAMiss { get; set; }

	public string strActionGroup { get; set; }

	public int nQabOrderPriority { get; set; }

	public string strThemType { get; set; }

	public string strRaiseUI { get; set; }

	public string strRaiseUIThem { get; set; }

	public string strSubUI { get; set; }

	// Loot contexts are named loot scripts; these likely resolve in data/loot.
	public string strContextLootUs { get; set; }

	public string strContextLootThem { get; set; }

	public string strCTThemMultCondUs { get; set; }

	public string strCTThemMultCondTools { get; set; }

	public string strImage { get; set; }

	public string strMapIcon { get; set; }

	public string strLedgerDef { get; set; }

	public bool bPause { get; set; }

	public bool bSocial { get; set; }

	public bool bIgnoreFeelings { get; set; }

	public bool bIgnoreCancel { get; set; }

	public bool bImmediateReply { get; set; }

	public bool bRandomInverse { get; set; }

	public bool bCloser { get; set; }

	public bool bGambit { get; set; }

	public bool bOpener { get; set; }

	public int nLogging { get; set; }

	public int nMoveType { get; set; }

	public string strCrime { get; set; }

	public string strFactionTest { get; set; }

	public float fFactionScoreChangeThem { get; set; }

	public float fFactionScoreChangeUs { get; set; }

	public string LootAddFactionsUs { get; set; }

	public string LootAddFactionsThem { get; set; }

	public string LootAddCondRulesUs { get; set; }

	public string LootAddCondRulesThem { get; set; }

	public bool bHardCode { get; set; }

	public bool bApplyChain { get; set; }

	public bool bInterrupt { get; set; }

	public bool bUsePDA { get; set; }

	public bool bHumanOnly { get; set; }

	public bool bAIOnly { get; set; }

	public bool bTargetOwned { get; set; }

	public bool bEquip { get; set; }

	public bool bLot { get; set; }

	public bool bPassThrough { get; set; }

	public bool bRecheckAllPlots { get; set; }

	public bool bRecheckThisPlot { get; set; }

	public bool b3rdReset { get; set; }

	public bool bModeSwitchCheckFit { get; set; }

	public bool bNoWait { get; set; }

	public bool bNoWalk { get; set; }

	public string strStartInstall { get; set; }

	public string strPledgeAdd { get; set; }

	public string strPledgeAddThem { get; set; }

	public string strMusic { get; set; }

	public bool bForceMusic { get; set; }

	// These fields appear to apply loot-script side effects that add CondTriggers/Conditions.
	// Despite the `CT` name, the string usually points at a loot definition that then grants
	// condtrigs, conds, factions, or inventory changes to the acting parties.
	public string LootCTsUs { get; set; }

	public string LootCTsThem { get; set; }

	public string LootCTs3rd { get; set; }

	public string LootCondsUs { get; set; }

	public string LootCondsThem { get; set; }

	public string LootConds3rd { get; set; }

	public string LootReveals { get; set; }

	public string CTTestUs { get; set; }

	public string CTTestThem { get; set; }

	public string CTTestRoom { get; set; }

	public string CTTest3rd { get; set; }

	public string PSpecTestThem { get; set; }

	public string PSpecTest3rd { get; set; }

	public string ShipTestUs { get; set; }

	public string ShipTestThem { get; set; }

	public string ShipTest3rd { get; set; }

	public string[] aLootItms { get; set; }

	public string objLootModeSwitch { get; set; }

	public string objLootModeSwitchThem { get; set; }

	public string[] aSocialPrereqs { get; set; }

	public string[] aSocialNew { get; set; }

	public string strLootRELChangeUsSees3rd { get; set; }

	public string strLootRELChangeUsSeesThem { get; set; }

	public string strLootRELChangeThemSees3rd { get; set; }

	public string strLootRELChangeThemSeesUs { get; set; }

	public string strLootRELChange3rdSeesThem { get; set; }

	public string strLootRELChange3rdSeesUs { get; set; }

	public string[] aTickersUs { get; set; }

	public string[] aTickersThem { get; set; }

	public string[] aLoSReactions { get; set; }

	public string[] aAModesAddedUs { get; set; }

	public string[] aAModesAddedThem { get; set; }

	public string[] aGPMChangesUs { get; set; }

	public string[] aGPMChangesThem { get; set; }

	public string[] aCustomInfos { get; set; }

	public string strSocialCombatPreview { get; set; }

	// Creates the runtime object that can actually be queued/executed by characters.
	// Likely called from UI clicks, AI planners, or save-load reconstruction.
	public Interaction Get()
	{
		return new Interaction(this, null);
	}

	// Shallow copy for template reuse; arrays and referenced payloads are mostly shared.
	public JsonInteraction Clone()
	{
		return base.MemberwiseClone() as JsonInteraction;
	}

	// Duplicates a template while remapping embedded ids.
	// Likely used by variant generation where one interaction family is cloned with
	// a different suffix/prefix and re-registered into DataHandler.dictInteractions.
	public JsonInteraction CloneDeep(string strFind, string strReplace)
	{
		if (string.IsNullOrEmpty(strReplace) || string.IsNullOrEmpty(strFind) || strReplace == strFind)
		{
			return this.Clone();
		}
		JsonInteraction jsonInteraction = this.Clone();
		jsonInteraction.strName = this.strName.Replace(strFind, strReplace);
		jsonInteraction.strCancelInteraction = JsonInteraction.CloneDeep(this.strCancelInteraction, strReplace, strFind);
		if (this.aInverse != null)
		{
			jsonInteraction.aInverse = (string[])this.aInverse.Clone();
			for (int i = 0; i < this.aInverse.Length; i++)
			{
				jsonInteraction.aInverse[i] = JsonInteraction.CloneDeep(this.aInverse[i], strReplace, strFind);
			}
		}
		if (this.aLoSReactions != null)
		{
			jsonInteraction.aLoSReactions = (string[])this.aLoSReactions.Clone();
			for (int j = 0; j < this.aLoSReactions.Length; j++)
			{
				if (!string.IsNullOrEmpty(this.aLoSReactions[j]))
				{
					string[] array = this.aLoSReactions[j].Split(new char[]
					{
						','
					});
					if (!string.IsNullOrEmpty(array[0]))
					{
						string newValue = JsonInteraction.CloneDeep(array[0], strReplace, strFind);
						jsonInteraction.aLoSReactions[j] = jsonInteraction.aLoSReactions[j].Replace(array[0], newValue);
					}
				}
			}
		}
		jsonInteraction.strContextLootUs = Loot.CloneDeep(this.strContextLootUs, strReplace, strFind);
		jsonInteraction.strContextLootThem = Loot.CloneDeep(this.strContextLootThem, strReplace, strFind);
		jsonInteraction.strLootRELChangeThemSeesUs = Loot.CloneDeep(this.strLootRELChangeThemSeesUs, strReplace, strFind);
		jsonInteraction.strLootRELChangeThemSees3rd = Loot.CloneDeep(this.strLootRELChangeThemSees3rd, strReplace, strFind);
		jsonInteraction.strLootRELChangeUsSeesThem = Loot.CloneDeep(this.strLootRELChangeUsSeesThem, strReplace, strFind);
		jsonInteraction.strLootRELChangeUsSees3rd = Loot.CloneDeep(this.strLootRELChangeUsSees3rd, strReplace, strFind);
		jsonInteraction.strLootRELChange3rdSeesUs = Loot.CloneDeep(this.strLootRELChange3rdSeesUs, strReplace, strFind);
		jsonInteraction.strLootRELChange3rdSeesThem = Loot.CloneDeep(this.strLootRELChange3rdSeesThem, strReplace, strFind);
		jsonInteraction.LootCTsUs = Loot.CloneDeep(this.LootCTsUs, strReplace, strFind);
		jsonInteraction.LootCTsThem = Loot.CloneDeep(this.LootCTsThem, strReplace, strFind);
		jsonInteraction.LootCTs3rd = Loot.CloneDeep(this.LootCTs3rd, strReplace, strFind);
		jsonInteraction.LootCondsUs = Loot.CloneDeep(this.LootCondsUs, strReplace, strFind);
		jsonInteraction.LootCondsThem = Loot.CloneDeep(this.LootCondsThem, strReplace, strFind);
		jsonInteraction.LootConds3rd = Loot.CloneDeep(this.LootConds3rd, strReplace, strFind);
		jsonInteraction.LootAddFactionsUs = Loot.CloneDeep(this.LootAddFactionsUs, strReplace, strFind);
		jsonInteraction.LootAddFactionsThem = Loot.CloneDeep(this.LootAddFactionsThem, strReplace, strFind);
		jsonInteraction.LootAddCondRulesUs = Loot.CloneDeep(this.LootAddCondRulesUs, strReplace, strFind);
		jsonInteraction.LootAddCondRulesThem = Loot.CloneDeep(this.LootAddCondRulesThem, strReplace, strFind);
		jsonInteraction.objLootModeSwitch = Loot.CloneDeep(this.objLootModeSwitch, strReplace, strFind);
		jsonInteraction.objLootModeSwitchThem = Loot.CloneDeep(this.objLootModeSwitchThem, strReplace, strFind);
		jsonInteraction.strCTThemMultCondUs = CondTrigger.CloneDeep(this.strCTThemMultCondUs, strReplace, strFind);
		jsonInteraction.strCTThemMultCondTools = CondTrigger.CloneDeep(this.strCTThemMultCondTools, strReplace, strFind);
		jsonInteraction.CTTestUs = CondTrigger.CloneDeep(this.CTTestUs, strReplace, strFind);
		jsonInteraction.CTTestThem = CondTrigger.CloneDeep(this.CTTestThem, strReplace, strFind);
		jsonInteraction.CTTestRoom = CondTrigger.CloneDeep(this.CTTestRoom, strReplace, strFind);
		jsonInteraction.CTTest3rd = CondTrigger.CloneDeep(this.CTTest3rd, strReplace, strFind);
		jsonInteraction.strPledgeAdd = JsonPledge.CloneDeep(this.strPledgeAdd, strReplace, strFind);
		jsonInteraction.strPledgeAddThem = JsonPledge.CloneDeep(this.strPledgeAddThem, strReplace, strFind);
		jsonInteraction.PSpecTestThem = JsonPersonSpec.CloneDeep(this.PSpecTestThem, strReplace, strFind);
		jsonInteraction.PSpecTest3rd = JsonPersonSpec.CloneDeep(this.PSpecTest3rd, strReplace, strFind);
		jsonInteraction.ShipTestUs = JsonShipSpec.CloneDeep(this.ShipTestUs, strReplace, strFind);
		jsonInteraction.ShipTestThem = JsonShipSpec.CloneDeep(this.ShipTestThem, strReplace, strFind);
		jsonInteraction.ShipTest3rd = JsonShipSpec.CloneDeep(this.ShipTest3rd, strReplace, strFind);
		if (this.aLootItms != null)
		{
			jsonInteraction.aLootItms = (string[])this.aLootItms.Clone();
			for (int k = 0; k < this.aLootItms.Length; k++)
			{
				if (!string.IsNullOrEmpty(this.aLootItms[k]))
				{
					string[] array2 = this.aLootItms[k].Split(new char[]
					{
						','
					});
					if (!string.IsNullOrEmpty(array2[1]))
					{
						string newValue2 = Loot.CloneDeep(array2[1], strReplace, strFind);
						jsonInteraction.aLootItms[k] = jsonInteraction.aLootItms[k].Replace(array2[1], newValue2);
					}
				}
			}
		}
		if (this.aSocialNew != null)
		{
			jsonInteraction.aSocialNew = (string[])this.aSocialNew.Clone();
			for (int l = 0; l < this.aSocialNew.Length; l++)
			{
				if (!string.IsNullOrEmpty(this.aSocialNew[l]))
				{
					string[] array3 = this.aSocialNew[l].Split(new char[]
					{
						','
					});
					if (!string.IsNullOrEmpty(array3[0]))
					{
						string newValue3 = JsonPersonSpec.CloneDeep(array3[0], strReplace, strFind);
						jsonInteraction.aSocialNew[l] = jsonInteraction.aSocialNew[l].Replace(array3[0], newValue3);
					}
				}
			}
		}
		DataHandler.dictInteractions[jsonInteraction.strName] = jsonInteraction;
		return jsonInteraction;
	}

	// Helper for remapping one interaction id through the shared registry.
	public static string CloneDeep(string strOrigName, string strReplace, string strFind)
	{
		if (string.IsNullOrEmpty(strOrigName) || string.IsNullOrEmpty(strReplace) || string.IsNullOrEmpty(strFind) || strReplace == strFind || strOrigName.IndexOf(strFind) < 0)
		{
			return strOrigName;
		}
		JsonInteraction jsonInteraction = null;
		if (!DataHandler.dictInteractions.TryGetValue(strOrigName, out jsonInteraction))
		{
			return strOrigName;
		}
		string text = strOrigName.Replace(strFind, strReplace);
		JsonInteraction jsonInteraction2 = null;
		if (!DataHandler.dictInteractions.TryGetValue(text, out jsonInteraction2))
		{
			jsonInteraction2 = jsonInteraction.CloneDeep(strFind, strReplace);
		}
		return text;
	}

	public override string ToString()
	{
		return this.strName;
	}

	// Validation pass for data load; gathers ids that should exist in other JSON registries.
	public IDictionary<string, IEnumerable> GetVerifiables()
	{
		Dictionary<string, IEnumerable> dictionary = new Dictionary<string, IEnumerable>();
		if (!string.IsNullOrEmpty(this.CTTest3rd))
		{
			dictionary.TryAdd(this.CTTest3rd, new Type[]
			{
				typeof(CondTrigger)
			});
		}
		if (!string.IsNullOrEmpty(this.CTTestRoom))
		{
			dictionary.TryAdd(this.CTTestRoom, new Type[]
			{
				typeof(CondTrigger)
			});
		}
		if (!string.IsNullOrEmpty(this.CTTestThem))
		{
			dictionary.TryAdd(this.CTTestThem, new Type[]
			{
				typeof(CondTrigger)
			});
		}
		if (!string.IsNullOrEmpty(this.CTTestUs))
		{
			dictionary.TryAdd(this.CTTestUs, new Type[]
			{
				typeof(CondTrigger)
			});
		}
		if (!string.IsNullOrEmpty(this.LootCTs3rd))
		{
			dictionary.TryAdd(this.LootCTs3rd, new Type[]
			{
				typeof(Loot)
			});
		}
		if (!string.IsNullOrEmpty(this.LootCTsThem))
		{
			dictionary.TryAdd(this.LootCTsThem, new Type[]
			{
				typeof(Loot)
			});
		}
		if (!string.IsNullOrEmpty(this.LootCTsUs))
		{
			dictionary.TryAdd(this.LootCTsUs, new Type[]
			{
				typeof(Loot)
			});
		}
		if (!string.IsNullOrEmpty(this.LootConds3rd))
		{
			dictionary.TryAdd(this.LootConds3rd, new Type[]
			{
				typeof(Loot)
			});
		}
		if (!string.IsNullOrEmpty(this.LootCondsThem))
		{
			dictionary.TryAdd(this.LootCondsThem, new Type[]
			{
				typeof(Loot)
			});
		}
		if (!string.IsNullOrEmpty(this.LootCondsUs))
		{
			dictionary.TryAdd(this.LootCondsUs, new Type[]
			{
				typeof(Loot)
			});
		}
		if (!string.IsNullOrEmpty(this.objLootModeSwitch))
		{
			dictionary.TryAdd(this.objLootModeSwitch, new Type[]
			{
				typeof(Loot)
			});
		}
		if (!string.IsNullOrEmpty(this.objLootModeSwitchThem))
		{
			dictionary.TryAdd(this.objLootModeSwitchThem, new Type[]
			{
				typeof(Loot)
			});
		}
		if (!string.IsNullOrEmpty(this.LootAddFactionsUs))
		{
			dictionary.TryAdd(this.LootAddFactionsUs, new Type[]
			{
				typeof(Loot)
			});
		}
		if (!string.IsNullOrEmpty(this.LootAddFactionsThem))
		{
			dictionary.TryAdd(this.LootAddFactionsThem, new Type[]
			{
				typeof(Loot)
			});
		}
		if (!string.IsNullOrEmpty(this.LootAddCondRulesUs))
		{
			dictionary.TryAdd(this.LootAddCondRulesUs, new Type[]
			{
				typeof(Loot)
			});
		}
		if (!string.IsNullOrEmpty(this.LootAddCondRulesThem))
		{
			dictionary.TryAdd(this.LootAddCondRulesThem, new Type[]
			{
				typeof(Loot)
			});
		}
		if (!string.IsNullOrEmpty(this.ShipTestUs))
		{
			dictionary.TryAdd(this.ShipTestUs, new Type[]
			{
				typeof(JsonShipSpec)
			});
		}
		if (!string.IsNullOrEmpty(this.ShipTestThem))
		{
			dictionary.TryAdd(this.ShipTestThem, new Type[]
			{
				typeof(JsonShipSpec)
			});
		}
		if (!string.IsNullOrEmpty(this.ShipTest3rd))
		{
			dictionary.TryAdd(this.ShipTest3rd, new Type[]
			{
				typeof(JsonShipSpec)
			});
		}
		if (this.aInverse != null && this.aInverse.Length > 0)
		{
			foreach (string text in this.aInverse)
			{
				string[] array = text.Split(new char[]
				{
					','
				});
				if (!string.IsNullOrEmpty(array[0]))
				{
					dictionary.TryAdd(array[0], new Type[]
					{
						typeof(JsonInteraction)
					});
				}
			}
		}
		if (this.aLootItms != null && this.aLootItms.Length > 0)
		{
			foreach (string text2 in this.aLootItms)
			{
				if (!string.IsNullOrEmpty(text2))
				{
					string[] array2 = text2.Split(new char[]
					{
						','
					});
					if (array2.Length < 2)
					{
						Debug.LogError("Loot item could not be parsed: " + this.strName + " aLootItms: " + text2);
					}
					else if (!string.IsNullOrEmpty(array2[1]))
					{
						dictionary.TryAdd(array2[1], null);
						string a = array2[0].ToLower();
						if (a == "addus")
						{
							dictionary.TryAdd(array2[1], new Type[]
							{
								typeof(Loot)
							});
						}
						else if (a == "addthem")
						{
							dictionary.TryAdd(array2[1], new Type[]
							{
								typeof(Loot)
							});
						}
						else if (a == "removethem")
						{
							dictionary.TryAdd(array2[1], new Type[]
							{
								typeof(Loot)
							});
						}
						else if (a == "take")
						{
							dictionary.TryAdd(array2[1], new Type[]
							{
								typeof(Loot)
							});
						}
						else if (array2.Length < 3)
						{
							Debug.LogError("Loot item could not be parsed: " + this.strName + " aLootItms: " + text2);
						}
						else if (a == "use")
						{
							dictionary.TryAdd(array2[1], new Type[]
							{
								typeof(Loot)
							});
						}
						else if (a == "lacks")
						{
							dictionary.TryAdd(array2[1], new Type[]
							{
								typeof(Loot)
							});
						}
						else if (a == "input")
						{
							dictionary.TryAdd(array2[1], new Type[]
							{
								typeof(Loot)
							});
						}
						else if (array2.Length < 4)
						{
							Debug.LogError("Loot item could not be parsed: " + this.strName + " aLootItms: " + text2);
						}
						else if (a == "give")
						{
							dictionary.TryAdd(array2[1], new Type[]
							{
								typeof(Loot)
							});
						}
						else if (a == "removeus")
						{
							dictionary.TryAdd(array2[1], new Type[]
							{
								typeof(Loot)
							});
						}
					}
				}
			}
		}
		return dictionary;
	}
}
