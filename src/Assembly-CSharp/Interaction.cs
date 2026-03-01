using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Ostranauts.Condowner;
using Ostranauts.Core;
using Ostranauts.Core.Models;
using Ostranauts.Objectives;
using Ostranauts.Tools.ExtensionMethods;
using UnityEngine;

// Runtime interaction instance built from JsonInteraction data.
// This is the live action object that AI, player UI, and save/load use after
// definitions from StreamingAssets/data/interactions have been resolved.
public class Interaction
{
	// Empty ctor exists for pooling or later re-hydration from saved data.
	public Interaction()
	{
	}

	// Main constructor for turning a JsonInteraction definition into a queued action.
	public Interaction(JsonInteraction jsonIn, JsonInteractionSave jis = null)
	{
		this.id = Guid.NewGuid();
		this.SetData(jsonIn, jis);
	}

	public string[] aTickersUs { get; set; }

	public string[] aTickersThem { get; set; }

	public string[] aSocialPrereqs { get; set; }

	public string[] aSocialPrereqsFound { get; set; }

	public string[] aSocialNew { get; set; }

	// Reuses an existing instance by clearing bound actors, then reapplying definition data.
	public void ResetObject(JsonInteraction jsonIn, JsonInteractionSave jis = null)
	{
		this.objUsTemp = null;
		this.objThemTemp = null;
		this.obj3rdTemp = null;
		this.strUsID = null;
		this.strThemID = null;
		this.str3rdID = null;
		this.SetData(jsonIn, jis);
	}

	// Copies the definition into runtime fields and resolves external references.
	// Data linkage: string ids here pull from data/strings ("COMBAT_MISSED",
	// "IA_FAIL_DEFAULT", etc.) and data/condtrigs/data/loot via DataHandler.
	private void SetData(JsonInteraction jsonIn, JsonInteractionSave jis = null)
	{
		if (jsonIn == null)
		{
			return;
		}
		if (Interaction.STR_COMBAT_MISSED == null)
		{
			Interaction.STR_COMBAT_MISSED = DataHandler.GetString("COMBAT_MISSED", false);
			Interaction.STR_TASK_RATE_START = DataHandler.GetString("TASK_RATE_START", false);
			Interaction.STR_TASK_RATE_END = DataHandler.GetString("TASK_RATE_END", false);
			Interaction.STR_IA_FAIL_DEFAULT = DataHandler.GetString("IA_FAIL_DEFAULT", false);
			Interaction.STR_IA_FAIL_FACTION = DataHandler.GetString("IA_FAIL_FACTION", false);
			Interaction.STR_IA_FAIL_LOS_END = DataHandler.GetString("IA_FAIL_LOS_END", false);
			Interaction.STR_IA_FAIL_LOS_START = DataHandler.GetString("IA_FAIL_LOS_START", false);
			Interaction.STR_IA_FAIL_MONEY = DataHandler.GetString("IA_FAIL_MONEY", false);
			Interaction.STR_IA_FAIL_NO_EQUIP = DataHandler.GetString("IA_FAIL_NO_EQUIP", false);
			Interaction.STR_IA_FAIL_INV_SPACE = DataHandler.GetString("IA_FAIL_INV_SPACE", false);
			Interaction.STR_IA_FAIL_OWNED_US = DataHandler.GetString("IA_FAIL_OWNED_US", false);
			Interaction.STR_IA_FAIL_PATH_END = DataHandler.GetString("IA_FAIL_PATH_END", false);
			Interaction.STR_IA_FAIL_PATH_START = DataHandler.GetString("IA_FAIL_PATH_START", false);
			Interaction.STR_IA_FAIL_PLAYER = DataHandler.GetString("IA_FAIL_PLAYER", false);
			Interaction.STR_IA_FAIL_AI = DataHandler.GetString("IA_FAIL_AI", false);
			Interaction.STR_IA_FAIL_RANGE_END = DataHandler.GetString("IA_FAIL_RANGE_END", false);
			Interaction.STR_IA_FAIL_RANGE_START = DataHandler.GetString("IA_FAIL_RANGE_START", false);
			Interaction.STR_IA_FAIL_THEM = DataHandler.GetString("IA_FAIL_THEM", false);
			Interaction.STR_IA_FAIL_US = DataHandler.GetString("IA_FAIL_US", false);
			Interaction.STR_ERROR_NO_ROOM_INV = DataHandler.GetString("ERROR_NO_ROOM_INV", false);
			Interaction.STR_GUI_REFUEL_PORT_SUFFIX = DataHandler.GetString("GUI_REFUEL_PORT_SUFFIX", false);
			Interaction.STR_GUI_FINANCE_LOG_RECEIVED = DataHandler.GetString("GUI_FINANCE_LOG_RECEIVED", false);
		}
		this.jis = jis;
		this.strName = jsonIn.strName;
		this.strTitle = jsonIn.strTitle;
		this.strDesc = jsonIn.strDesc;
		this.strTooltip = jsonIn.strTooltip;
		this.strVerbs = jsonIn.strVerbs;
		this.strTargetPoint = jsonIn.strTargetPoint;
		this.fTargetPointRange = jsonIn.fTargetPointRange;
		this.fForcedChance = jsonIn.fForcedChance;
		this.strAnim = jsonIn.strAnim;
		this.strAnimTrig = jsonIn.strAnimTrig;
		this.strBubble = jsonIn.strBubble;
		this.strColor = jsonIn.strColor;
		if (this.strColor == null || this.strColor == string.Empty)
		{
			this.strColor = "Neutral";
		}
		this.strDuty = jsonIn.strDuty;
		this.strThemType = jsonIn.strThemType;
		this.strRaiseUI = jsonIn.strRaiseUI;
		this.strRaiseUIThem = jsonIn.strRaiseUIThem;
		this.strSubUI = jsonIn.strSubUI;
		this.strLedgerDef = jsonIn.strLedgerDef;
		this.strLootContextUs = jsonIn.strContextLootUs;
		this.strLootContextThem = jsonIn.strContextLootThem;
		this.strCTThemMultCondUs = jsonIn.strCTThemMultCondUs;
		this.strCTThemMultCondTools = jsonIn.strCTThemMultCondTools;
		this.strImage = jsonIn.strImage;
		this.strMapIcon = jsonIn.strMapIcon;
		this.NewChainGuid();
		this.bPause = jsonIn.bPause;
		this.bSocial = jsonIn.bSocial;
		this.bImmediateReply = jsonIn.bImmediateReply;
		this.bIgnoreFeelings = jsonIn.bIgnoreFeelings;
		this.bIgnoreCancel = jsonIn.bIgnoreCancel;
		this.bRandomInverse = jsonIn.bRandomInverse;
		this.bOpener = jsonIn.bOpener;
		this.bGamit = jsonIn.bGambit;
		this.bCloser = jsonIn.bCloser;
		this.bHardCode = jsonIn.bHardCode;
		this.bApplyChain = jsonIn.bApplyChain;
		this.bInterrupt = jsonIn.bInterrupt;
		this.bUsePDA = jsonIn.bUsePDA;
		this.nLogging = (Interaction.Logging)jsonIn.nLogging;
		this.nMoveType = (Interaction.MoveType)jsonIn.nMoveType;
		this.strCrime = jsonIn.strCrime;
		this.strFactionTest = jsonIn.strFactionTest;
		this.fFactionScoreChangeThem = jsonIn.fFactionScoreChangeThem;
		this.fFactionScoreChangeUs = jsonIn.fFactionScoreChangeUs;
		this.bTargetOwned = jsonIn.bTargetOwned;
		this.bEquip = jsonIn.bEquip;
		this.bLot = jsonIn.bLot;
		this.bPassThrough = jsonIn.bPassThrough;
		this.bRecheckAllPlots = jsonIn.bRecheckAllPlots;
		this.bRecheckThisPlot = jsonIn.bRecheckThisPlot;
		this.b3rdReset = jsonIn.b3rdReset;
		this.strStartInstall = jsonIn.strStartInstall;
		this.strPledgeAdd = jsonIn.strPledgeAdd;
		this.strPledgeAddThem = jsonIn.strPledgeAddThem;
		this.strMusic = jsonIn.strMusic;
		this.bForceMusic = jsonIn.bForceMusic;
		this.bLogged = false;
		this.bRaisedUI = false;
		this.bTryWalk = false;
		this.bCancel = false;
		this.bRetestItems = false;
		this.bManual = false;
		this.bModeSwitchCheckFit = jsonIn.bModeSwitchCheckFit;
		this.bHumanOnly = jsonIn.bHumanOnly;
		this.bAIOnly = jsonIn.bAIOnly;
		this.bNoWait = jsonIn.bNoWait;
		this.bNoWalk = jsonIn.bNoWalk;
		this.fDuration = jsonIn.fDuration;
		this.fDurationOrig = this.fDuration;
		this.fRotation = jsonIn.fRotation;
		this.strPlot = null;
		this.strTeleport = jsonIn.strTeleport;
		if (!string.IsNullOrEmpty(jsonIn.strTeleportRegID))
		{
			this.teleportRegIDTarget = new Tuple<string, string>(jsonIn.strTeleportRegID, "us");
		}
		if (this.teleportRegIDTarget != null && !string.IsNullOrEmpty(jsonIn.strTeleportTarget))
		{
			this.teleportRegIDTarget.Item2 = jsonIn.strTeleportTarget;
		}
		this.strIdleAnim = jsonIn.strIdleAnim;
		this.strCancelInteraction = jsonIn.strCancelInteraction;
		this.strUseCase = jsonIn.strUseCase;
		if (jsonIn.strAttackMode != null)
		{
			this.attackMode = DataHandler.GetAttackMode(jsonIn.strAttackMode);
			if (this.attackMode != null)
			{
				this.fTargetPointRange = this.attackMode.fRange;
				this.strIAHit = jsonIn.strIAHit;
				this.strIAMiss = jsonIn.strIAMiss;
				this.strAttackerName = jsonIn.strAttackerName;
			}
		}
		this.strActionGroup = jsonIn.strActionGroup;
		this.nQabOrderPriority = jsonIn.nQabOrderPriority;
		this.aLoSReactions = (jsonIn.aLoSReactions ?? Interaction._aDefault);
		this.aAModesAddedUs = (jsonIn.aAModesAddedUs ?? Interaction._aDefault);
		this.aAModesAddedThem = (jsonIn.aAModesAddedThem ?? Interaction._aDefault);
		this.aGPMChangesUs = (jsonIn.aGPMChangesUs ?? Interaction._aDefault);
		this.aGPMChangesThem = (jsonIn.aGPMChangesThem ?? Interaction._aDefault);
		this.aCustomInfos = (jsonIn.aCustomInfos ?? Interaction._aDefault);
		this.aInverse = (jsonIn.aInverse ?? Interaction._aDefault);
		if (this.LootCTsUs == null)
		{
			this.LootCTsUs = DataHandler.GetLoot(jsonIn.LootCTsUs);
		}
		if (this.LootCTsThem == null)
		{
			this.LootCTsThem = DataHandler.GetLoot(jsonIn.LootCTsThem);
		}
		if (this.LootCTs3rd == null)
		{
			this.LootCTs3rd = DataHandler.GetLoot(jsonIn.LootCTs3rd);
		}
		if (this.LootCondsUs == null)
		{
			this.LootCondsUs = DataHandler.GetLoot(jsonIn.LootCondsUs);
		}
		if (this.LootCondsThem == null)
		{
			this.LootCondsThem = DataHandler.GetLoot(jsonIn.LootCondsThem);
		}
		if (this.LootConds3rd == null)
		{
			this.LootConds3rd = DataHandler.GetLoot(jsonIn.LootConds3rd);
		}
		if (this.LootAddFactionsUs == null)
		{
			this.LootAddFactionsUs = DataHandler.GetLoot(jsonIn.LootAddFactionsUs);
		}
		if (this.LootAddFactionsThem == null)
		{
			this.LootAddFactionsThem = DataHandler.GetLoot(jsonIn.LootAddFactionsThem);
		}
		if (this.LootAddCondRulesUs == null)
		{
			this.LootAddCondRulesUs = DataHandler.GetLoot(jsonIn.LootAddCondRulesUs);
		}
		if (this.LootAddCondRulesThem == null)
		{
			this.LootAddCondRulesThem = DataHandler.GetLoot(jsonIn.LootAddCondRulesThem);
		}
		if (this.CTTestUs == null || this.CTTestUs.ValuesWereChanged)
		{
			this.CTTestUs = DataHandler.GetCondTrigger(jsonIn.CTTestUs);
			this.CTTestUs.ValuesWereChanged = false;
		}
		if (this.CTTestThem == null || this.CTTestThem.ValuesWereChanged)
		{
			this.CTTestThem = DataHandler.GetCondTrigger(jsonIn.CTTestThem);
			this.CTTestThem.ValuesWereChanged = false;
		}
		if (this.CTTestRoom == null || this.CTTestRoom.ValuesWereChanged)
		{
			this.CTTestRoom = DataHandler.GetCondTrigger(jsonIn.CTTestRoom);
			this.CTTestRoom.ValuesWereChanged = false;
		}
		if (jsonIn.CTTest3rd != null && (this.CTTest3rd == null || this.CTTest3rd.ValuesWereChanged))
		{
			this.CTTest3rd = DataHandler.GetCondTrigger(jsonIn.CTTest3rd);
			this.CTTest3rd.ValuesWereChanged = false;
		}
		if (jsonIn.PSpecTestThem != null && jsonIn.PSpecTestThem != string.Empty)
		{
			this.PSpecTestThem = DataHandler.GetPersonSpec(jsonIn.PSpecTestThem);
		}
		if (jsonIn.PSpecTest3rd != null && jsonIn.PSpecTest3rd != string.Empty)
		{
			this.PSpecTest3rd = DataHandler.GetPersonSpec(jsonIn.PSpecTest3rd);
		}
		if (jsonIn.ShipTestUs != null && jsonIn.ShipTestUs != string.Empty)
		{
			this.ShipTestUs = DataHandler.GetShipSpec(jsonIn.ShipTestUs);
		}
		if (jsonIn.ShipTestThem != null && jsonIn.ShipTestThem != string.Empty)
		{
			this.ShipTestThem = DataHandler.GetShipSpec(jsonIn.ShipTestThem);
		}
		if (jsonIn.ShipTest3rd != null && jsonIn.ShipTest3rd != string.Empty)
		{
			this.ShipTest3rd = DataHandler.GetShipSpec(jsonIn.ShipTest3rd);
		}
		if (jsonIn.aLootItms != null)
		{
			foreach (string text in jsonIn.aLootItms)
			{
				if (text != null)
				{
					string[] array = text.Split(new char[]
					{
						','
					});
					if (array.Length >= 2)
					{
						string a = array[0].ToLower();
						if (a == "addus")
						{
							this.strLootItmAddUs = array[1];
						}
						else if (a == "addthem")
						{
							this.strLootItmAddThem = array[1];
						}
						else if (a == "removethem")
						{
							this.strLootItmRemoveThem = array[1];
						}
						else if (a == "take")
						{
							this.strLootCTsTake = array[1];
						}
						else if (array.Length >= 3)
						{
							if (a == "use")
							{
								this.strLootCTsUse = array[1];
								this.bGetItemBefore = Convert.ToBoolean(array[2]);
							}
							else if (a == "lacks")
							{
								this.strLootCTsLacks = array[1];
								this.bGetItemBefore = Convert.ToBoolean(array[2]);
							}
							else if (a == "input")
							{
								this.strLootItmInputs = array[1];
								this.bGetItemBefore = Convert.ToBoolean(array[2]);
							}
							else if (array.Length >= 4)
							{
								if (a == "give")
								{
									this.strLootCTsGive = array[1];
									this.bGetItemBefore = Convert.ToBoolean(array[2]);
									this.bGiveWholeStack = Convert.ToBoolean(array[3]);
								}
								else if (a == "removeus")
								{
									this.strLootCTsRemoveUs = array[1];
									this.bGetItemBefore = Convert.ToBoolean(array[2]);
									this.bDestroyItem = Convert.ToBoolean(array[3]);
									this.bRemoveWholeStack = Convert.ToBoolean(array[4]);
								}
							}
						}
					}
				}
			}
		}
		if (this.objLootModeSwitch == null && !string.IsNullOrEmpty(jsonIn.objLootModeSwitch))
		{
			Loot loot = DataHandler.GetLoot(jsonIn.objLootModeSwitch);
			if (loot.strName != "Blank")
			{
				this.objLootModeSwitch = loot;
			}
		}
		if (this.objLootModeSwitchThem == null && !string.IsNullOrEmpty(jsonIn.objLootModeSwitchThem))
		{
			Loot loot2 = DataHandler.GetLoot(jsonIn.objLootModeSwitchThem);
			if (loot2.strName != "Blank")
			{
				this.objLootModeSwitchThem = loot2;
			}
		}
		if (this.LootReveals == null && !string.IsNullOrEmpty(jsonIn.LootReveals))
		{
			Loot loot3 = DataHandler.GetLoot(jsonIn.LootReveals);
			if (loot3.strName != "Blank")
			{
				this.LootReveals = loot3;
			}
		}
		if (jsonIn.aSocialPrereqs == null)
		{
			this.aSocialPrereqs = Interaction._aDefault;
		}
		else
		{
			this.aSocialPrereqs = jsonIn.aSocialPrereqs;
		}
		if (this.aSocialPrereqs.Length > 0)
		{
			this.aSocialPrereqsFound = new string[this.aSocialPrereqs.Length];
		}
		else
		{
			this.aSocialPrereqsFound = Interaction._aDefault;
		}
		if (jsonIn.aSocialNew == null)
		{
			this.aSocialNew = Interaction._aDefault;
		}
		else
		{
			this.aSocialNew = jsonIn.aSocialNew;
		}
		this.strLootRELChangeThemSeesUs = jsonIn.strLootRELChangeThemSeesUs;
		this.strLootRELChangeThemSees3rd = jsonIn.strLootRELChangeThemSees3rd;
		this.strLootRELChangeUsSeesThem = jsonIn.strLootRELChangeUsSeesThem;
		this.strLootRELChangeUsSees3rd = jsonIn.strLootRELChangeUsSees3rd;
		this.strLootRELChange3rdSeesUs = jsonIn.strLootRELChange3rdSeesUs;
		this.strLootRELChange3rdSeesThem = jsonIn.strLootRELChange3rdSeesThem;
		this.aTickersUs = jsonIn.aTickersUs;
		this.aTickersThem = jsonIn.aTickersThem;
		this.strSocialCombatPreview = jsonIn.strSocialCombatPreview;
		if (Interaction.ctNotCarried == null)
		{
			Interaction.ctNotCarried = DataHandler.GetCondTrigger("TIsNotCarried");
		}
		if (jis != null)
		{
			this.strUsID = jis.objUs;
			this.strThemID = jis.objThem;
			this.str3rdID = jis.obj3rd;
		}
	}

	public void PostGameLoad()
	{
		if (this.jis != null)
		{
			this.strChainOwner = this.jis.strChainOwner;
			this.strChainStart = this.jis.strChainStart;
			this.strPlot = this.jis.strPlot;
			this.bLogged = this.jis.bLogged;
			this.bRaisedUI = this.jis.bRaisedUI;
			this.bManual = this.jis.bManual;
			this.bTryWalk = this.jis.bTryWalk;
			this.bCancel = this.jis.bCancel;
			this.bRetestItems = this.jis.bRetestItems;
			this.objUs = DataHandler.mapCOs[this.jis.objUs];
			if (!(this.objThem == null) && DataHandler.mapCOs.ContainsKey(this.jis.objThem))
			{
				this.objThem = DataHandler.mapCOs[this.jis.objThem];
			}
			if (this.jis.aLootItemGiveContract != null)
			{
				foreach (string key in this.jis.aLootItemGiveContract)
				{
					CondOwner condOwner = DataHandler.mapCOs[key];
					if (condOwner != null)
					{
						if (this.aLootItemGiveContract == null)
						{
							this.aLootItemGiveContract = new List<CondOwner>();
						}
						this.aLootItemGiveContract.Add(condOwner);
					}
				}
			}
			if (this.jis.aLootItemUseContract != null)
			{
				foreach (string key2 in this.jis.aLootItemUseContract)
				{
					CondOwner condOwner = DataHandler.mapCOs[key2];
					if (condOwner != null)
					{
						if (this.aLootItemUseContract == null)
						{
							this.aLootItemUseContract = new List<CondOwner>();
						}
						this.aLootItemUseContract.Add(condOwner);
					}
				}
			}
			if (this.jis.aLootItemRemoveContract != null)
			{
				foreach (string text in this.jis.aLootItemRemoveContract)
				{
					CondOwner condOwner;
					if (DataHandler.mapCOs.TryGetValue(text, out condOwner))
					{
						if (condOwner != null)
						{
							if (this.aLootItemRemoveContract == null)
							{
								this.aLootItemRemoveContract = new List<CondOwner>();
							}
							this.aLootItemRemoveContract.Add(condOwner);
						}
					}
					else
					{
						Debug.LogWarning("Interaction " + this.strName + ", could not find CO with id: " + text);
					}
				}
			}
			if (this.jis.aLootItemTakeContract != null)
			{
				foreach (string text2 in this.jis.aLootItemTakeContract)
				{
					CondOwner condOwner;
					if (DataHandler.mapCOs.TryGetValue(text2, out condOwner))
					{
						if (condOwner != null)
						{
							if (this.aLootItemTakeContract == null)
							{
								this.aLootItemTakeContract = new List<CondOwner>();
							}
							this.aLootItemTakeContract.Add(condOwner);
						}
					}
					else
					{
						Debug.LogWarning("Interaction " + this.strName + ", could not find CO with id: " + text2);
					}
				}
			}
			if (this.jis.aSeekItemsForContract != null)
			{
				foreach (string text3 in this.jis.aSeekItemsForContract)
				{
					CondOwner condOwner;
					if (DataHandler.mapCOs.TryGetValue(text3, out condOwner))
					{
						if (condOwner != null)
						{
							if (this.aSeekItemsForContract == null)
							{
								this.aSeekItemsForContract = new List<CondOwner>();
							}
							this.aSeekItemsForContract.Add(condOwner);
						}
					}
					else
					{
						Debug.LogWarning("Interaction " + this.strName + ", could not find CO with id: " + text3);
					}
				}
			}
			if (this.jis.aDependents != null)
			{
				foreach (string item in this.jis.aDependents)
				{
					if (this.aDependents == null)
					{
						this.aDependents = new List<string>();
					}
					this.aDependents.Add(item);
				}
			}
			if (this.jis.aSocialPrereqsFound != null)
			{
				this.aSocialPrereqsFound = (this.jis.aSocialPrereqsFound.Clone() as string[]);
			}
			if (Array.IndexOf<string>(JsonCompanyRules.aDutiesNew, this.strDuty) >= 0)
			{
				CrewSim.objInstance.workManager.AddClaimedTask(this);
			}
			this.jis = null;
		}
	}

	private void NewChainGuid()
	{
		this.strChainStart = this.strName;
		this.strChainOwner = null;
	}

	private string[] GetCTStrList(string strIn)
	{
		List<CondTrigger> ctloot = DataHandler.GetLoot(strIn).GetCTLoot(null, null);
		string[] array = new string[ctloot.Count];
		for (int i = 0; i < ctloot.Count; i++)
		{
			array[i] = ctloot[i].strName;
		}
		return array;
	}

	public bool IsPlayerNoticed()
	{
		if (this.nLogging >= Interaction.Logging.GROUP)
		{
			if (this.objUs == CrewSim.GetSelectedCrew() || this.objThem == CrewSim.GetSelectedCrew())
			{
				return true;
			}
			if (this.nLogging == Interaction.Logging.ROOM)
			{
				Room currentRoom = CrewSim.GetSelectedCrew().currentRoom;
				if (currentRoom != null)
				{
					return this.objUs.ship.GetPeopleInRoom(currentRoom, null).Contains(CrewSim.GetSelectedCrew());
				}
			}
			else if (this.nLogging == Interaction.Logging.SHIP && this.objUs.ship == CrewSim.GetSelectedCrew().ship)
			{
				return true;
			}
		}
		return false;
	}

	private bool IsSocialCombatReply()
	{
		if (this.objThem == this.objUs)
		{
			return GUISocialCombat2.coUs == this.objUs && this.strSubUI != null;
		}
		return (GUISocialCombat2.coUs == this.objUs && GUISocialCombat2.coThem == this.objThem) || (GUISocialCombat2.coThem == this.objUs && GUISocialCombat2.coUs == this.objThem);
	}

	private CondOwner ParseInteractionString(string strPart, CondOwner[] cos = null)
	{
		if (cos == null)
		{
			cos = new CondOwner[]
			{
				this.objUs,
				this.objThem,
				this.obj3rd
			};
		}
		if (strPart != null)
		{
			if (strPart == "[us]")
			{
				return cos[0];
			}
			if (strPart == "[them]")
			{
				return cos[1];
			}
			if (strPart == "[3rd]")
			{
				return cos[2];
			}
		}
		return null;
	}

	public Interaction GetReply()
	{
		Interaction interaction = null;
		List<Interaction> list = new List<Interaction>();
		float num = 0f;
		float num2 = 0f;
		List<Interaction> list2 = new List<Interaction>();
		List<string> list3 = new List<string>();
		if (this.aInverse != null)
		{
			list3.AddRange(this.aInverse);
		}
		bool bNoSwap = false;
		bool flag = this.IsSocialCombatReply();
		bool flag2 = false;
		if (this.bSocial && (CrewSim.GetSelectedCrew() == this.objUs || CrewSim.GetSelectedCrew() == this.objThem))
		{
			flag2 = true;
		}
		if (list3.Count == 0)
		{
			if (!flag || !(this.objThem == GUISocialCombat2.coUs) || !(this.strName != "Wait") || this.bCloser)
			{
				return null;
			}
			list3.AddRange(this.objUs.aInteractions);
			bNoSwap = true;
		}
		StringBuilder stringBuilder = new StringBuilder();
		bool flag3 = false;
		bool flag4 = false;
		foreach (string text in list3)
		{
			string[] array = text.Split(new char[]
			{
				','
			});
			Interaction interaction2 = DataHandler.GetInteraction(array[0], null, false);
			if (interaction2 != null)
			{
				this.AssignReplyRoles(interaction2, array, bNoSwap);
				if (flag2 && !flag3)
				{
					stringBuilder.Append(interaction2.objUs.strName);
					stringBuilder.Append(" replying to ");
					stringBuilder.Append(this.strName);
					stringBuilder.Append(" (");
					stringBuilder.Append(this.strDesc);
					stringBuilder.AppendLine("):");
				}
				if (stringBuilder.Length > 0)
				{
					interaction2.bVerboseTrigger = true;
				}
				bool flag5 = interaction2.Triggered(interaction2.objUs, interaction2.objThem, true, false, false, true, null);
				bool flag6 = MathUtils.Rand(0f, 1f, MathUtils.RandType.Flat, null) <= interaction2.fForcedChance;
				if (!flag5)
				{
					if (stringBuilder.Length > 0)
					{
						stringBuilder.Append("FAILED: ");
						stringBuilder.Append(interaction2.strTitle);
						stringBuilder.Append("(");
						stringBuilder.Append(interaction2.strName);
						stringBuilder.Append(")");
						stringBuilder.Append(": Reasons. ");
						stringBuilder.AppendLine(interaction2.FailReasons(true, true, true));
					}
				}
				else
				{
					if (stringBuilder.Length > 0)
					{
						if (flag6)
						{
							stringBuilder.Append("FORCED: ");
						}
						else
						{
							stringBuilder.Append("PASSED: ");
						}
						stringBuilder.Append(interaction2.strTitle);
						stringBuilder.Append("(");
						stringBuilder.Append(interaction2.strName);
						stringBuilder.Append(")");
						stringBuilder.AppendLine(": Passed.");
					}
					if (flag && interaction2.objUs == GUISocialCombat2.coUs)
					{
						list2.Add(interaction2);
					}
					else
					{
						num = interaction2.objUs.GetNetInteractionResult(interaction2, false);
						if (interaction2.objUs.RecentlyTried(interaction2, true) >= 0.0)
						{
							if (num < 0f)
							{
								num *= -0.01f;
							}
							else
							{
								num *= 100f;
							}
						}
						if (flag6)
						{
							if (!flag4 || num < num2)
							{
								list.Clear();
								num2 = num;
							}
							list.Add(interaction2);
							if (stringBuilder.Length > 0)
							{
								stringBuilder.AppendLine("FORCED");
							}
							break;
						}
						if (this.bIgnoreFeelings)
						{
							if (list.Count == 0)
							{
								list.Add(interaction2);
								if (stringBuilder.Length > 0)
								{
									stringBuilder.AppendLine("BEST ONLY");
								}
							}
						}
						else if (list.Count == 0 || num == num2)
						{
							num2 = num;
							list.Add(interaction2);
							if (stringBuilder.Length > 0)
							{
								if (list.Count == 0)
								{
									stringBuilder.Append("BEST 1ST ");
								}
								else
								{
									stringBuilder.Append("BEST ADD ");
								}
								stringBuilder.AppendLine(num2.ToString());
							}
						}
						else if (num < num2)
						{
							foreach (Interaction interaction3 in list)
							{
								if (DataHandler.dictSocialStats.ContainsKey(interaction3.strName))
								{
									DataHandler.dictSocialStats[interaction3.strName].nLowScored++;
								}
							}
							list.Clear();
							num2 = num;
							list.Add(interaction2);
							if (stringBuilder.Length > 0)
							{
								stringBuilder.Append("BEST REPLACE ");
								stringBuilder.AppendLine(num2.ToString());
							}
						}
						else if (DataHandler.dictSocialStats.ContainsKey(interaction2.strName))
						{
							DataHandler.dictSocialStats[interaction2.strName].nLowScored++;
						}
					}
				}
			}
		}
		if (flag && (GUISocialCombat2.coUs == this.objUs || GUISocialCombat2.coThem == this.objUs))
		{
			GUISocialCombat2.strSubUI = this.strSubUI;
		}
		if (flag && list2.Count > 0)
		{
			GUISocialCombat2.objInstance.SetData(list2[0].objUs, list2[0].objThem, true, list2);
		}
		if (stringBuilder.Length > 0)
		{
			Debug.Log(stringBuilder.ToString());
		}
		if (list.Count > 0)
		{
			if (this.bRandomInverse)
			{
				interaction = list[MathUtils.Rand(0, list.Count, MathUtils.RandType.Flat, null)];
			}
			else
			{
				interaction = list[0];
			}
		}
		if (interaction != null)
		{
			interaction.strChainOwner = this.strChainOwner;
			interaction.strChainStart = this.strChainStart;
			interaction.strPlot = this.strPlot;
		}
		return interaction;
	}

	public void AssignReplyRoles(Interaction iaReply, string[] aParts, bool bNoSwap)
	{
		if (iaReply == null || aParts == null)
		{
			return;
		}
		CondOwner condOwner = this.objThem;
		CondOwner condOwner2 = this.objUs;
		CondOwner condOwner3 = (!iaReply.b3rdReset) ? this.obj3rd : null;
		CondOwner[] array = new CondOwner[]
		{
			this.objUs,
			this.objThem,
			this.obj3rd
		};
		CondOwner[] array2 = new CondOwner[]
		{
			condOwner,
			condOwner2,
			condOwner3
		};
		for (int i = 1; i < aParts.Length; i++)
		{
			if (i < array.Length)
			{
				CondOwner condOwner4 = this.ParseInteractionString(aParts[i], array);
				array2[i - 1] = ((!(condOwner4 != null)) ? array[i - 1] : condOwner4);
			}
		}
		condOwner = array2[0];
		condOwner2 = array2[1];
		condOwner3 = array2[2];
		if (bNoSwap)
		{
			CondOwner condOwner5 = condOwner;
			condOwner = condOwner2;
			condOwner2 = condOwner5;
		}
		iaReply.objUs = condOwner;
		iaReply.objThem = condOwner2;
		iaReply.obj3rd = condOwner3;
	}

	public void LogSocial(string strName, string strRel, string strEvent)
	{
		if (strName == null || strRel == null)
		{
			return;
		}
		if (this.aSocialChangelog == null)
		{
			this.aSocialChangelog = new List<string>();
		}
		this.aSocialChangelog.Add(strName);
		this.aSocialChangelog.Add(strRel);
		this.aSocialChangelog.Add(strEvent);
	}

	public void AddDependent(Interaction act)
	{
		if (act == null)
		{
			return;
		}
		if (this.aDependents == null)
		{
			this.aDependents = new List<string>();
		}
		string str = string.Empty;
		if (act.objThem != null)
		{
			str = act.objThem.strID;
		}
		this.aDependents.Add(act.strName + str);
	}

	private void AddFailReason(string strKey, string strReason)
	{
		if (this.mapFails == null)
		{
			this.mapFails = new Dictionary<string, List<string>>();
		}
		if (string.IsNullOrEmpty(strKey) || string.IsNullOrEmpty(strReason))
		{
			return;
		}
		if (this.mapFails.ContainsKey(strKey))
		{
			this.mapFails[strKey].Add(strReason);
		}
		else
		{
			this.mapFails[strKey] = new List<string>
			{
				strReason
			};
		}
	}

	private bool TriggeredInternal(CondOwner objUs, CondOwner objThem, bool bStats = false, bool bIgnoreItems = false, bool bCheckPath = false, bool bFetchItems = true, List<string> aForbid3rds = null)
	{
		string text = Interaction.STR_IA_FAIL_DEFAULT;
		if (this.mapFails != null)
		{
			this.mapFails.Clear();
		}
		this.AddFailReason("main", text);
		this.bAirlockBlocked = false;
		if (this.strThemType == Interaction.TARGET_SELF && objUs != objThem)
		{
			if (this.bVerboseTrigger)
			{
				text = Interaction.STR_IA_FAIL_THEM;
				this.AddFailReason("us", text);
			}
			return false;
		}
		if (this.strThemType == Interaction.TARGET_OTHER && objUs == objThem)
		{
			if (this.bVerboseTrigger)
			{
				text = Interaction.STR_IA_FAIL_US;
				this.AddFailReason("them", text);
			}
			return false;
		}
		if (this.bTargetOwned && (objThem == null || (objThem.ship != null && !objUs.OwnsShip(objThem.ship.strRegID))))
		{
			if (this.bVerboseTrigger)
			{
				text = Interaction.STR_IA_FAIL_OWNED_US;
				this.AddFailReason("them", text);
			}
			return false;
		}
		if (!string.IsNullOrEmpty(this.strFactionTest) && this.strFactionTest != "ALWAYS" && objThem != null)
		{
			bool flag = false;
			string text2 = this.strFactionTest;
			if (text2 != null)
			{
				if (!(text2 == "DIFFERENT"))
				{
					if (!(text2 == "SAME"))
					{
						if (!(text2 == "LIKES"))
						{
							if (text2 == "DISLIKES")
							{
								float factionScore = objUs.GetFactionScore(objThem.GetAllFactions());
								flag = (JsonFaction.GetReputation(factionScore) != JsonFaction.Reputation.Dislikes);
							}
						}
						else
						{
							float factionScore = objUs.GetFactionScore(objThem.GetAllFactions());
							flag = (JsonFaction.GetReputation(factionScore) != JsonFaction.Reputation.Likes);
						}
					}
					else
					{
						flag = objUs.SharesFactionsWith(objThem);
					}
				}
				else
				{
					flag = !objUs.SharesFactionsWith(objThem);
				}
			}
			if (flag)
			{
				if (this.bVerboseTrigger)
				{
					text = Interaction.STR_IA_FAIL_FACTION;
					this.AddFailReason("us", text);
				}
				return false;
			}
		}
		if (this.bHumanOnly && objUs != CrewSim.GetSelectedCrew())
		{
			if (this.bVerboseTrigger)
			{
				text = Interaction.STR_IA_FAIL_PLAYER;
				this.AddFailReason("us", text);
			}
			return false;
		}
		if (this.bAIOnly && objUs == CrewSim.GetSelectedCrew())
		{
			if (this.bVerboseTrigger)
			{
				text = Interaction.STR_IA_FAIL_AI;
				this.AddFailReason("us", text);
			}
			return false;
		}
		if (this.ShipTestUs != null && !this.ShipTestUs.Matches(objUs.ship, objUs))
		{
			if (this.bVerboseTrigger)
			{
				text = DataHandler.GetString("IA_FAIL_SHIP_WRONG", false);
				this.AddFailReason("us", text);
				text = DataHandler.GetString("IA_FAIL_SHIP_WRONG", false);
				this.AddFailReason("debugus", text);
			}
			return false;
		}
		if (this.ShipTestThem != null && !this.ShipTestThem.Matches(objThem.ship, objThem))
		{
			if (this.bVerboseTrigger)
			{
				text = DataHandler.GetString("IA_FAIL_SHIP_WRONG", false);
				this.AddFailReason("them", text);
				text = DataHandler.GetString("IA_FAIL_SHIP_WRONG", false);
				this.AddFailReason("debugthem", text);
			}
			return false;
		}
		if (this.CTTestUs != null && !this.CTTestUs.Triggered(objUs, this.strName, true))
		{
			if (this.bVerboseTrigger)
			{
				text = this.CTTestUs.strFailReasonLast;
				this.AddFailReason("us", text);
				text = this.CTTestUs.strFailReasonLast;
				this.AddFailReason("debugus", text);
			}
			return false;
		}
		if (this.PSpecTestThem != null)
		{
			if (objUs.pspec != null)
			{
				if (!objUs.pspec.IsCOMyMother(this.PSpecTestThem, objThem))
				{
					if (this.bVerboseTrigger)
					{
						Debug.Log(!this.PSpecTestThem.Matches(objThem));
						this.AddFailReason("main", "Didn't pass ptest us");
					}
					return false;
				}
			}
			else if (!this.PSpecTestThem.Matches(objThem))
			{
				if (this.bVerboseTrigger)
				{
					this.AddFailReason("main", "Didn't pass ptest them");
				}
				return false;
			}
		}
		if (this.CTTestThem != null && !this.CTTestThem.Triggered(objThem, null, true))
		{
			if (this.bVerboseTrigger)
			{
				text = this.CTTestThem.strFailReasonLast;
				this.AddFailReason("them", text);
				text = this.CTTestThem.strFailReasonLast;
				this.AddFailReason("debugthem", text);
			}
			return false;
		}
		if (this.obj3rd != null)
		{
			if (this.PSpecTest3rd != null)
			{
				if (!objUs.pspec.IsCOMyMother(this.PSpecTest3rd, this.obj3rd))
				{
					return false;
				}
			}
			else if (this.CTTest3rd != null && !this.CTTest3rd.Triggered(this.obj3rd, null, true))
			{
				return false;
			}
		}
		else if (this.PSpecTest3rd != null)
		{
			PersonSpec person = StarSystem.GetPerson(this.PSpecTest3rd, objUs.socUs, false, aForbid3rds, this.ShipTest3rd);
			if (person != null)
			{
				this.obj3rd = person.GetCO();
			}
			if (this.obj3rd == null)
			{
				return false;
			}
		}
		else if (this.CTTest3rd != null)
		{
			List<CondOwner> list = new List<CondOwner>();
			foreach (Ship ship in CrewSim.system.GetAllLoadedShips())
			{
				if (this.ShipTest3rd == null || this.ShipTest3rd.Matches(ship, objUs))
				{
					list.AddRange(ship.GetCOs(this.CTTest3rd, true, false, true));
				}
			}
			if (aForbid3rds != null)
			{
				for (int i = list.Count - 1; i >= 0; i--)
				{
					if (aForbid3rds.Contains(list[i].strID))
					{
						list.RemoveAt(i);
					}
				}
			}
			if (list.Count <= 0)
			{
				if (this.bVerboseTrigger)
				{
					text = this.CTTest3rd.strFailReasonLast;
					this.AddFailReason("3rd", text);
					text = this.CTTest3rd.strFailReasonLast;
					this.AddFailReason("debug3rd", text);
				}
				return false;
			}
			this.obj3rd = list[MathUtils.Rand(0, list.Count, MathUtils.RandType.Flat, null)];
		}
		if (this.ShipTest3rd != null && this.obj3rd != null && !this.ShipTest3rd.Matches(this.obj3rd.ship, this.obj3rd))
		{
			if (this.bVerboseTrigger)
			{
				text = DataHandler.GetString("IA_FAIL_SHIP_WRONG", false);
				this.AddFailReason("3rd", text);
				text = DataHandler.GetString("IA_FAIL_SHIP_WRONG", false);
				this.AddFailReason("debug3rd", text);
			}
			return false;
		}
		if (this.CTTestRoom != null && !this.CTTestRoom.IsBlank() && objUs.ship != null)
		{
			Room roomAtWorldCoords = objUs.ship.GetRoomAtWorldCoords1(objUs.tf.position, true);
			if (roomAtWorldCoords != null && roomAtWorldCoords.CO != null && !this.CTTestRoom.Triggered(roomAtWorldCoords.CO, this.strName, true))
			{
				if (this.bVerboseTrigger)
				{
					text = this.CTTestRoom.strFailReasonLast;
					this.AddFailReason("room", text);
					text = this.CTTestRoom.strFailReasonLast;
					this.AddFailReason("debugroom", text);
				}
				return false;
			}
		}
		for (int j = 0; j < this.aSocialPrereqs.Length; j++)
		{
			if (objUs.socUs == null)
			{
				return false;
			}
			JsonPersonSpec personSpec = DataHandler.GetPersonSpec(this.aSocialPrereqs[j]);
			string matchingRelation = objUs.socUs.GetMatchingRelation(personSpec, null, null);
			if (matchingRelation == null)
			{
				return false;
			}
			this.aSocialPrereqsFound[j] = matchingRelation;
		}
		if (!(this.strTargetPoint == Interaction.POINT_REMOTE))
		{
			if (this.bNoWalk)
			{
				bool flag2 = true;
				bool flag3 = objUs.GetCORef(objThem) != null;
				Tile tileAtWorldCoords = objUs.ship.GetTileAtWorldCoords1(objUs.tf.position.x, objUs.tf.position.y, true, true);
				if (this.strTargetPoint != null && this.strTargetPoint != Interaction.POINT_REMOTE && !flag3)
				{
					Vector2 pos = objThem.GetPos(this.strTargetPoint, false);
					Tile tileAtWorldCoords2 = objUs.ship.GetTileAtWorldCoords1(pos.x, pos.y, true, true);
					flag2 = ((float)TileUtils.TileRange(tileAtWorldCoords, tileAtWorldCoords2) <= this.fTargetPointRange);
				}
				if (!flag2)
				{
					if (this.bVerboseTrigger)
					{
						text = Interaction.STR_IA_FAIL_RANGE_START + objThem.FriendlyName + Interaction.STR_IA_FAIL_RANGE_END;
						this.AddFailReason("main", text);
					}
					return false;
				}
				if (!Visibility.IsCondOwnerLOSVisibleBlocks(objThem, tileAtWorldCoords.tf.position, false, true))
				{
					if (this.bVerboseTrigger)
					{
						text = Interaction.STR_IA_FAIL_LOS_START + objThem.FriendlyName + Interaction.STR_IA_FAIL_LOS_END;
						this.AddFailReason("main", text);
					}
					return false;
				}
			}
			else if (bCheckPath)
			{
				Pathfinder pathfinder = objUs.Pathfinder;
				if (pathfinder != null)
				{
					Tile tilDestNew = pathfinder.tilCurrent;
					if (this.strTargetPoint != null && this.strTargetPoint != Interaction.POINT_REMOTE && objUs.GetCORef(objThem) == null)
					{
						Vector2 pos2 = objThem.GetPos(this.strTargetPoint, false);
						tilDestNew = objUs.ship.GetTileAtWorldCoords1(pos2.x, pos2.y, true, true);
					}
					bool bAllowAirlocks = objUs.HasAirlockPermission(this.bManual);
					PathResult pathResult = pathfinder.CheckGoal(tilDestNew, this.fTargetPointRange, objThem, bAllowAirlocks);
					if (!pathResult.HasPath)
					{
						if (this.bVerboseTrigger)
						{
							text = Interaction.STR_IA_FAIL_PATH_START + objThem.FriendlyName + Interaction.STR_IA_FAIL_PATH_END;
							this.AddFailReason("main", text);
							text = pathResult.FailReason(objUs);
							if (!string.IsNullOrEmpty(text))
							{
								this.AddFailReason("main", text);
							}
						}
						if (pathResult.bAirlockBlocked)
						{
							this.bAirlockBlocked = pathResult.bAirlockBlocked;
						}
						return false;
					}
				}
			}
		}
		if (this.strLedgerDef != null)
		{
			JsonLedgerDef ledgerDef = DataHandler.GetLedgerDef(this.strLedgerDef);
			if (ledgerDef != null && ledgerDef.bPaid && objUs.GetCondAmount(ledgerDef.strCurrency) < (double)ledgerDef.fAmount)
			{
				if (this.bVerboseTrigger)
				{
					text = Interaction.STR_IA_FAIL_MONEY;
					this.AddFailReason("them", text);
				}
				return false;
			}
		}
		if (bIgnoreItems)
		{
			return true;
		}
		this.aLootItemGiveContract = null;
		this.aLootItemUseContract = null;
		this.aLootItemRemoveContract = null;
		this.aLootItemTakeContract = null;
		this.aSeekItemsForContract = new List<CondOwner>();
		if (this.strLootItmInputs != null)
		{
			bool flag4 = false;
			List<CondTrigger> list2 = DataHandler.GetLoot(this.strLootItmInputs).GetCTLoot(null, null);
			List<CondOwner> list3 = new List<CondOwner>();
			List<CondOwner> list4 = new List<CondOwner>
			{
				objThem
			};
			list4.AddRange(objThem.GetLotCOs(true));
			foreach (CondOwner condOwner in list4)
			{
				foreach (CondTrigger condTrigger in list2)
				{
					if (condTrigger.Triggered(condOwner, null, true))
					{
						condTrigger.fCount -= (float)condOwner.StackCount;
					}
					if (condTrigger.fCount <= 0f)
					{
						list2.Remove(condTrigger);
						break;
					}
				}
			}
			if (list2.Count == 0)
			{
				flag4 = true;
			}
			else
			{
				HashSet<string> hashSet = new HashSet<string>();
				foreach (CondTrigger condTrigger2 in list2)
				{
					hashSet.Add(condTrigger2.strName);
				}
				List<CondTrigger> list5 = new List<CondTrigger>();
				foreach (string text3 in hashSet)
				{
					list5.Add(DataHandler.GetCondTrigger(text3));
				}
				CondOwnerVisitorAddToHashSet condOwnerVisitorAddToHashSet = new CondOwnerVisitorAddToHashSet();
				CondOwnerVisitorLazyOrCondTrigger visitor = new CondOwnerVisitorLazyOrCondTrigger(condOwnerVisitorAddToHashSet, list5);
				CondOwnerVisitorAddToHashSet a = new CondOwnerVisitorAddToHashSet();
				CondOwnerVisitorLazyOrCondTrigger condOwnerVisitorLazyOrCondTrigger = new CondOwnerVisitorLazyOrCondTrigger(a, list5);
				objUs.VisitCOs(visitor, false);
				list3 = new List<CondOwner>(condOwnerVisitorAddToHashSet.aHashSet);
				objUs.ship.VisitCOs(visitor, true, true, false);
				list4 = new List<CondOwner>(condOwnerVisitorAddToHashSet.aHashSet);
				foreach (CondOwner item in list3)
				{
					list4.Remove(item);
				}
				list4.InsertRange(0, list3);
				list3.Clear();
				if (objThem.strPersistentCO != null)
				{
					CondOwner item2 = null;
					if (DataHandler.mapCOs.TryGetValue(objThem.strPersistentCO, out item2))
					{
						int num = list4.IndexOf(item2);
						if (num > 0)
						{
							list4.Remove(item2);
							list4.Insert(0, item2);
						}
					}
				}
				list3 = this.CheckItemsAvailable(list2, list4, objUs, false, objThem.strPersistentCO, false);
				if (list3 != null)
				{
					if (!bFetchItems)
					{
						flag4 = true;
					}
					else
					{
						Dictionary<string, int> dictionary = new Dictionary<string, int>();
						foreach (CondTrigger condTrigger3 in list2)
						{
							if (dictionary.ContainsKey(condTrigger3.strName))
							{
								Dictionary<string, int> dictionary2;
								string key;
								(dictionary2 = dictionary)[key = condTrigger3.strName] = dictionary2[key] + 1;
							}
							else
							{
								dictionary[condTrigger3.strName] = 1;
							}
						}
						foreach (CondTrigger condTrigger4 in list2)
						{
							JsonInteraction jsonInteraction = null;
							string text4 = "ACTFeedItem" + condTrigger4.strName;
							DataHandler.dictInteractions.TryGetValue(text4, out jsonInteraction);
							if (jsonInteraction == null)
							{
								Loot loot = DataHandler.GetLoot(text4);
								if (loot.strName != text4)
								{
									loot.strName = text4;
									loot.strType = "trigger";
									loot.aCOs = new string[]
									{
										condTrigger4.strName + "=1.0x1"
									};
									DataHandler.dictLoot[text4] = loot;
									Debug.Log("Auto-generating Loot: " + text4);
								}
								jsonInteraction = DataHandler.dictInteractions["ACTFeedItem"].Clone();
								jsonInteraction.strName = text4;
								if (!string.IsNullOrEmpty(objThem.strPlaceholderInstallReq))
								{
									jsonInteraction.strDesc = jsonInteraction.strDesc.Replace("[object]", DataHandler.GetCOShortName(objThem.strPlaceholderInstallReq));
								}
								else
								{
									jsonInteraction.strDesc = jsonInteraction.strDesc.Replace("[object]", condTrigger4.RulesInfo);
								}
								jsonInteraction.aLootItms = new string[]
								{
									"Give," + text4 + ",true,false"
								};
								jsonInteraction.bLot = true;
								jsonInteraction.fTargetPointRange = this.fTargetPointRange;
								DataHandler.dictInteractions[jsonInteraction.strName] = jsonInteraction;
							}
							Task2 task = new Task2();
							task.strName = jsonInteraction.strName;
							task.strInteraction = jsonInteraction.strName;
							task.strTargetCOID = objThem.strID;
							if (CrewSim.objInstance.workManager.taskUpstream != null)
							{
								task.CopyFrom(CrewSim.objInstance.workManager.taskUpstream);
								task.strDuty = CrewSim.objInstance.workManager.taskUpstream.strDuty;
							}
							else
							{
								task.strDuty = "Haul";
							}
							task.bManual = false;
							CrewSim.objInstance.workManager.AddTask(task, dictionary[condTrigger4.strName]);
							text = "Items required first. Adding tasks now.";
							this.AddFailReason("main", text);
							Debug.Log(text);
						}
					}
				}
			}
			list4 = null;
			list3 = null;
			list2 = null;
			if (!flag4)
			{
				if (bStats && DataHandler.dictSocialStats.ContainsKey(this.strName))
				{
					DataHandler.dictSocialStats[this.strName].nMissingItem++;
				}
				return false;
			}
		}
		if (this.strLootCTsLacks != null)
		{
			List<CondOwner> list4 = objUs.GetCOs(false, null);
			List<CondOwner> list3 = new List<CondOwner>();
			CondTrigger condTrigger5 = DataHandler.GetCondTrigger("TIs[us]");
			List<CondOwner> list6 = null;
			bool flag5 = this.strLootCTsLacks == "CT[them]" || this.strLootCTsLacks == "CT[3rd]";
			if (flag5)
			{
				if (this.strLootCTsLacks == "CT[them]")
				{
					if (objThem == null || !list4.Contains(objThem))
					{
						list6 = null;
					}
					else
					{
						list6 = new List<CondOwner>
						{
							objThem
						};
					}
				}
				else if (this.strLootCTsLacks == "CT[3rd]")
				{
					if (this.obj3rd == null || !list4.Contains(this.obj3rd))
					{
						list6 = null;
					}
					else
					{
						list6 = new List<CondOwner>
						{
							this.obj3rd
						};
					}
				}
			}
			else
			{
				List<CondTrigger> ctlootFlat = DataHandler.GetLoot(this.strLootCTsLacks).GetCTLootFlat(condTrigger5, null);
				list6 = this.CheckItemsAvailable(ctlootFlat, list4, objUs, false, null, false);
			}
			list4 = null;
			list3 = null;
			if (list6 != null)
			{
				if (bStats && DataHandler.dictSocialStats.ContainsKey(this.strName))
				{
					DataHandler.dictSocialStats[this.strName].nMissingItem++;
				}
				return false;
			}
		}
		if (this.strLootCTsGive != null || this.strLootCTsRemoveUs != null || this.strLootCTsUse != null)
		{
			List<CondOwner> list4 = objUs.GetCOsSafe(false, null);
			if (this.bGetItemBefore)
			{
				list4.AddRange(objUs.ship.GetCOs(Interaction.ctNotCarried, true, true, false));
			}
			CondTrigger condTrigger6 = DataHandler.GetCondTrigger("TIs[us]");
			List<CondTrigger> ctlootFlat2 = DataHandler.GetLoot(this.strLootCTsGive).GetCTLootFlat(condTrigger6, null);
			List<CondTrigger> ctlootFlat3 = DataHandler.GetLoot(this.strLootCTsUse).GetCTLootFlat(condTrigger6, null);
			List<CondTrigger> ctlootFlat4 = DataHandler.GetLoot(this.strLootCTsRemoveUs).GetCTLootFlat(condTrigger6, null);
			if (objThem.strPersistentCO != null)
			{
				CondOwner item3 = null;
				if (DataHandler.mapCOs.TryGetValue(objThem.strPersistentCO, out item3))
				{
					int num2 = list4.IndexOf(item3);
					if (num2 > 0)
					{
						list4.Remove(item3);
						list4.Insert(0, item3);
					}
				}
			}
			this.aLootItemGiveContract = this.CheckItemsAvailable(ctlootFlat2, list4, objUs, true, objThem.strPersistentCO, false);
			this.aLootItemRemoveContract = this.CheckItemsAvailable(ctlootFlat4, list4, objUs, true, objThem.strPersistentCO, false);
			list4.Remove(objThem);
			this.aLootItemUseContract = this.CheckItemsAvailable(ctlootFlat3, list4, objUs, true, objThem.strPersistentCO, this.strUseCase != null);
			bool flag6 = (ctlootFlat2.Count > 0 && this.aLootItemGiveContract == null) || (ctlootFlat3.Count > 0 && this.aLootItemUseContract == null) || (ctlootFlat4.Count > 0 && this.aLootItemRemoveContract == null);
			list4 = null;
			List<CondOwner> list3 = null;
			if (flag6)
			{
				this.aSeekItemsForContract.Clear();
				if (bStats && DataHandler.dictSocialStats.ContainsKey(this.strName))
				{
					DataHandler.dictSocialStats[this.strName].nMissingItem++;
				}
				return false;
			}
		}
		else
		{
			this.aLootItemGiveContract = new List<CondOwner>();
			this.aLootItemUseContract = new List<CondOwner>();
			this.aLootItemRemoveContract = new List<CondOwner>();
		}
		if (this.strLootCTsTake != null)
		{
			List<CondOwner> list4 = objThem.GetCOs(false, null);
			CondTrigger condTrigger7 = DataHandler.GetCondTrigger("TIs[us]");
			List<CondTrigger> ctlootFlat5 = DataHandler.GetLoot(this.strLootCTsTake).GetCTLootFlat(condTrigger7, null);
			this.aLootItemTakeContract = this.CheckItemsAvailable(ctlootFlat5, list4, objUs, false, null, false);
			list4 = null;
			List<CondOwner> list3 = null;
		}
		else
		{
			this.aLootItemTakeContract = new List<CondOwner>();
		}
		if (this.aLootItemTakeContract == null)
		{
			if (bStats && DataHandler.dictSocialStats.ContainsKey(this.strName))
			{
				DataHandler.dictSocialStats[this.strName].nMissingItem++;
			}
			return false;
		}
		this.mapFails["main"][0] = string.Empty;
		return true;
	}

	// Main rules gate before an action can appear in UI or execute for AI.
	// Likely checks range, pathing, required items, condition triggers, and other
	// constraints pulled from the interaction's JSON definition.
	public bool Triggered(CondOwner objUs, CondOwner objThem, bool bStats = false, bool bIgnoreItems = false, bool bCheckPath = false, bool bFetchItems = true, List<string> aForbid3rds = null)
	{
		if (this.strActionGroup == "Ship")
		{
			return this.IsWithinShipRange() && this.TriggeredInternal(objUs, objThem, bStats, bIgnoreItems, bCheckPath, true, aForbid3rds);
		}
		return this.TriggeredInternal(objUs, objThem, bStats, bIgnoreItems, bCheckPath, bFetchItems, aForbid3rds);
	}

	public bool Triggered(bool bStats = false, bool bIgnoreItems = false, bool bCheckPath = false)
	{
		return this.Triggered(this.objUs, this.objThem, bStats, bIgnoreItems, bCheckPath, true, null);
	}

	private bool IsWithinShipRange()
	{
		if (this.fTargetPointRange == 0f)
		{
			return true;
		}
		if (this.objUs == null || this.objUs.ship == null || this.objThem == null || this.objThem.ship == null)
		{
			return false;
		}
		if (this.fTargetPointRange < 0f)
		{
			return this.objUs.ship.IsDockedWith(this.objThem.ship);
		}
		if (this.strThemType == Interaction.TARGET_SELF && this.objUs == this.objThem)
		{
			return true;
		}
		double num = (!this.objUs.ship.IsDockedWith(this.objThem.ship)) ? CollisionManager.GetRangeToCollisionKM(this.objUs.ship, this.objThem.ship) : -1.0;
		return num < (double)this.fTargetPointRange && num >= 0.0;
	}

	private int DistFromptRef(CondOwner x, CondOwner y)
	{
		if (x == null || y == null)
		{
			return 0;
		}
		float distance = MathUtils.GetDistance(this.ptRef.y, this.ptRef.y, x.tf.position.x, x.tf.position.y);
		float distance2 = MathUtils.GetDistance(this.ptRef.y, this.ptRef.y, y.tf.position.x, y.tf.position.y);
		return distance.CompareTo(distance2);
	}

	private CondOwner GetNearestTriggered(CondTrigger ct, List<CondOwner> aCOsHayStack, Pathfinder pf, CondOwner objUs, string strPersistentCO, bool bUseTest, out List<string> aOutUseFails)
	{
		aOutUseFails = new List<string>();
		if (objUs == null)
		{
			return null;
		}
		if (aCOsHayStack == null)
		{
			return null;
		}
		float num = float.PositiveInfinity;
		Tile tileAtWorldCoords = CrewSim.shipCurrentLoaded.GetTileAtWorldCoords1(objUs.tf.position.x, objUs.tf.position.y, true, true);
		CondOwner condOwner = null;
		List<CondOwner> list = new List<CondOwner>();
		List<Tile> list2 = new List<Tile>();
		List<string> list3 = new List<string>();
		foreach (CondOwner condOwner2 in aCOsHayStack)
		{
			if (ct.Triggered(condOwner2, null, false))
			{
				string text;
				if (bUseTest && !condOwner2.Usable(this.strUseCase, out text))
				{
					if (text != string.Empty && text != null)
					{
						list3.Add(text);
					}
				}
				else
				{
					if (strPersistentCO != null && condOwner2.strID == strPersistentCO)
					{
						return condOwner2;
					}
					if (condOwner2.slotNow != null && condOwner2.objCOParent == objUs)
					{
						return condOwner2;
					}
					Vector2 pos = condOwner2.GetPos("use", false);
					Vector2 pos2 = objUs.GetPos("use", false);
					if (pos == pos2)
					{
						return condOwner2;
					}
					Tile tileAtWorldCoords2 = objUs.ship.GetTileAtWorldCoords1(pos.x, pos.y, true, true);
					if (tileAtWorldCoords == tileAtWorldCoords2)
					{
						return condOwner2;
					}
					if (list2.IndexOf(tileAtWorldCoords2) < 0)
					{
						list.Add(condOwner2);
						list2.Add(tileAtWorldCoords2);
					}
				}
			}
		}
		list2 = null;
		bool flag = pf == null;
		int num2 = (!flag) ? 50 : int.MaxValue;
		List<Tuple<double, CondOwner>> list4 = null;
		if (!flag && list.Count > 10)
		{
			Vector2 b = tileAtWorldCoords.tf.position.ToVector2();
			list4 = new List<Tuple<double, CondOwner>>();
			foreach (CondOwner condOwner3 in list)
			{
				list4.Add(new Tuple<double, CondOwner>((double)Vector2.Distance(condOwner3.tf.position.ToVector2(), b), condOwner3));
			}
			list4 = (from tuple in list4
			orderby tuple.Item1
			select tuple).ToList<Tuple<double, CondOwner>>();
			list = (from tuple in list4
			select tuple.Item2).ToList<CondOwner>();
			num2 = 10;
		}
		bool bAllowAirlocks = !flag && objUs.HasAirlockPermission(this.bManual);
		int num3 = 0;
		while (num3 < list.Count && num2 > 0)
		{
			CondOwner condOwner4 = list[num3];
			PathResult pathResult = null;
			float num4;
			if (flag)
			{
				num4 = (condOwner4.tf.position - objUs.tf.position).sqrMagnitude;
			}
			else
			{
				Vector2 pos3 = condOwner4.GetPos("use", false);
				Tile tileAtWorldCoords3 = objUs.ship.GetTileAtWorldCoords1(pos3.x, pos3.y, true, true);
				if (tileAtWorldCoords == tileAtWorldCoords3)
				{
					num4 = 0f;
				}
				else
				{
					pathResult = pf.CheckGoal(tileAtWorldCoords3, 1f, condOwner4, bAllowAirlocks);
					num4 = pathResult.PathLength;
				}
			}
			if (num4 < 0f)
			{
				if (pathResult != null)
				{
					string text2 = pathResult.FailReason(objUs);
					if (!string.IsNullOrEmpty(text2))
					{
						this.AddFailReason("items", text2);
					}
					if (pathResult.bAirlockBlocked)
					{
						this.bAirlockBlocked = pathResult.bAirlockBlocked;
					}
				}
			}
			else
			{
				if (list4 == null || num3 >= list4.Count || (double)num4 <= list4[num3].Item1 * 2.0)
				{
					num2--;
				}
				if (num > num4)
				{
					num = num4;
					condOwner = condOwner4;
				}
			}
			num3++;
		}
		if (condOwner == null)
		{
			aOutUseFails = list3;
		}
		return condOwner;
	}

	private List<CondOwner> CheckItemsAvailable(List<CondTrigger> aCTsRequired, List<CondOwner> aCOsAvail, CondOwner objUs, bool bSeekFirst, string strPersistentCO, bool bUseTest)
	{
		if (aCTsRequired == null || aCTsRequired.Count == 0)
		{
			return null;
		}
		List<CondOwner> list = new List<CondOwner>();
		CondTrigger condTrigger = DataHandler.GetCondTrigger("TIs[us]");
		bool flag = true;
		Pathfinder pathfinder = objUs.Pathfinder;
		foreach (CondTrigger condTrigger2 in aCTsRequired)
		{
			if (condTrigger2.strName == condTrigger.strName)
			{
				list.Add(objUs);
			}
			else
			{
				List<string> list2;
				CondOwner nearestTriggered = this.GetNearestTriggered(condTrigger2, aCOsAvail, pathfinder, objUs, strPersistentCO, bUseTest, out list2);
				if (nearestTriggered == null)
				{
					flag = false;
					if (!this.bVerboseTrigger)
					{
						break;
					}
					this.AddFailReason("items", condTrigger2.RulesInfo);
					if (list2 != null && list2.Count > 0)
					{
						foreach (string strReason in list2)
						{
							this.AddFailReason("specs", strReason);
						}
					}
				}
				else
				{
					list.Add(nearestTriggered);
					if (!bUseTest && aCOsAvail != null)
					{
						aCOsAvail.Remove(nearestTriggered);
					}
				}
			}
		}
		if (!flag)
		{
			return null;
		}
		if (flag)
		{
			if (bSeekFirst)
			{
				List<CondOwner> list3 = new List<CondOwner>();
				foreach (CondOwner condOwner in list)
				{
					if (!(condOwner == objUs))
					{
						if (objUs.GetCORef(condOwner) == null)
						{
							bool flag2 = false;
							foreach (Slot slot in objUs.GetSlots(true, Slots.SortOrder.HELD_FIRST))
							{
								if (!slot.bHide)
								{
									if (slot.CanFit(condOwner, true, true))
									{
										flag2 = true;
										break;
									}
								}
							}
							if (!flag2)
							{
								objUs.LogMessage(Interaction.STR_IA_FAIL_INV_SPACE + condOwner.strNameFriendly, "Bad", this.strName);
								if (!objUs.HasCond("IsAIManual"))
								{
									Pledge2 pledge = PledgeFactory.Factory(objUs, DataHandler.GetPledge("AIFreeUpSpace"), null);
									objUs.AddPledge(pledge);
								}
								return null;
							}
							Vector2 pos = condOwner.GetPos("use", false);
							Tile tileAtWorldCoords = objUs.ship.GetTileAtWorldCoords1(pos.x, pos.y, true, true);
							if (pathfinder == null)
							{
								Tile tileAtWorldCoords2 = objUs.ship.GetTileAtWorldCoords1(objUs.tf.position.x, objUs.tf.position.y, true, true);
								if ((float)TileUtils.TileRange(tileAtWorldCoords2, tileAtWorldCoords) > this.fTargetPointRange)
								{
									flag = false;
									if (this.bVerboseTrigger)
									{
										this.AddFailReason("items", "Can't reach " + condOwner.strName);
									}
									break;
								}
								list3.Add(condOwner);
							}
							else
							{
								bool bAllowAirlocks = objUs.HasAirlockPermission(this.bManual);
								PathResult pathResult = pathfinder.CheckGoal(tileAtWorldCoords, this.fTargetPointRange, condOwner, bAllowAirlocks);
								if (!pathResult.HasPath)
								{
									flag = false;
									if (this.bVerboseTrigger)
									{
										this.AddFailReason("items", "Can't reach " + condOwner.strName);
										string text = pathResult.FailReason(objUs);
										if (!string.IsNullOrEmpty(text))
										{
											this.AddFailReason("items", text);
										}
									}
									if (pathResult.bAirlockBlocked)
									{
										this.bAirlockBlocked = pathResult.bAirlockBlocked;
									}
									break;
								}
							}
							list3.Add(condOwner);
						}
					}
				}
				if (flag)
				{
					this.aSeekItemsForContract.AddRange(list3);
					goto IL_3F8;
				}
				list = null;
			}
			IL_3F8:;
		}
		else
		{
			list = null;
		}
		return list;
	}

	private InteractionRangeData ParseRangesFromInteractionString(string rangeSubString)
	{
		InteractionRangeData interactionRangeData = new InteractionRangeData();
		if (string.IsNullOrEmpty(rangeSubString))
		{
			return interactionRangeData;
		}
		rangeSubString = rangeSubString.Replace("[", string.Empty).Replace("]", string.Empty);
		string[] array = rangeSubString.Split(new char[]
		{
			'='
		});
		if (array[0] != "range" && array[0] != "nolos")
		{
			return interactionRangeData;
		}
		interactionRangeData.UseLoS = (array[0] == "range");
		if (array.Length != 2)
		{
			return interactionRangeData;
		}
		string[] array2 = array[1].Split(new char[]
		{
			'-'
		});
		if (array2.Length == 1)
		{
			float.TryParse(array2[0], out interactionRangeData.MaxRange);
		}
		else if (array2.Length == 2)
		{
			float.TryParse(array2[0], out interactionRangeData.MinRange);
			float.TryParse(array2[1], out interactionRangeData.MaxRange);
		}
		return interactionRangeData;
	}

	private void ApplyLosInteraction(string[] loSReactions)
	{
		if (loSReactions == null || loSReactions.Length == 0)
		{
			return;
		}
		foreach (string text in loSReactions)
		{
			if (!string.IsNullOrEmpty(text))
			{
				string[] array = text.Split(new char[]
				{
					','
				});
				Interaction interaction = DataHandler.GetInteraction(array[0], null, false);
				if (interaction == null)
				{
					return;
				}
				InteractionRangeData rangeData = this.ParseRangesFromInteractionString(array.LastOrDefault<string>());
				IEnumerable<CondOwner> cosinRange = this.GetCOSInRange(this.objUs, this.objThem, rangeData, interaction.CTTestThem);
				if (cosinRange != null)
				{
					bool flag = false;
					foreach (CondOwner condOwner in cosinRange)
					{
						CondOwner condOwner2 = condOwner;
						CondOwner condOwner3 = this.objUs;
						CondOwner condOwner4 = this.objThem;
						CondOwner[] array2 = new CondOwner[]
						{
							condOwner2,
							condOwner3,
							condOwner4
						};
						CondOwner[] array3 = new CondOwner[]
						{
							condOwner2,
							condOwner3,
							condOwner4
						};
						for (int j = 1; j < array.Length; j++)
						{
							if (j < array2.Length)
							{
								CondOwner condOwner5 = this.ParseInteractionString(array[j], array2);
								array3[j - 1] = ((!(condOwner5 != null)) ? array2[j - 1] : condOwner5);
							}
						}
						condOwner2 = array3[0];
						condOwner3 = array3[1];
						condOwner4 = array3[2];
						if (flag)
						{
							interaction = DataHandler.GetInteraction(array[0], null, false);
						}
						interaction.objUs = condOwner2;
						interaction.objThem = condOwner3;
						interaction.obj3rd = condOwner4;
						if (interaction.Triggered(interaction.objUs, interaction.objThem, false, true, false, true, null))
						{
							interaction.ApplyChain(null);
						}
						flag = true;
					}
				}
			}
		}
	}

	private IEnumerable<CondOwner> GetCOSInRange(CondOwner coLosUs, CondOwner coLosThem, InteractionRangeData rangeData, CondTrigger ctTestUs)
	{
		if (coLosUs == null)
		{
			return null;
		}
		List<CondOwner> list = new List<CondOwner>();
		if (ctTestUs.RequiresHumans)
		{
			List<CondOwner> people = coLosUs.ship.GetPeople(true);
			foreach (CondOwner condOwner in people)
			{
				if (ctTestUs.Triggered(condOwner, null, true))
				{
					list.Add(condOwner);
				}
			}
		}
		else
		{
			list = coLosUs.ship.GetCOs(ctTestUs, false, true, false);
		}
		List<CondOwner> list2 = new List<CondOwner>();
		float num = rangeData.MaxRange * rangeData.MaxRange;
		float num2 = rangeData.MinRange * rangeData.MinRange;
		foreach (CondOwner condOwner2 in list)
		{
			if (!(condOwner2 == coLosUs) && !(condOwner2 == coLosThem))
			{
				float distanceSquared = MathUtils.GetDistanceSquared(coLosUs, condOwner2);
				if (distanceSquared <= num && distanceSquared >= num2)
				{
					if (!rangeData.UseLoS && coLosThem.currentRoom == condOwner2.currentRoom)
					{
						list2.Add(condOwner2);
					}
					else if (Visibility.IsCondOwnerLOSVisibleFromCo(coLosUs, condOwner2) || (coLosThem != null && Visibility.IsCondOwnerLOSVisibleFromCo(coLosThem, condOwner2)))
					{
						list2.Add(condOwner2);
					}
				}
			}
		}
		return list2;
	}

	// Applies the actual gameplay side effects once the interaction succeeds:
	// task completion, animation state, loot scripts, conditions, UI raises,
	// social effects, and follow-up chain handling.
	public void ApplyEffects(List<string> aLog = null, bool isCancelIa = false)
	{
		if (this.objUs == null)
		{
			Debug.Log("Warning: Interaction " + this.strName + " has null objUs. Aborting.");
			return;
		}
		if (this.objThem == null)
		{
			Debug.LogWarning("Warning: Interaction " + this.strName + " has null objThem. Aborting.");
			return;
		}
		if (this.bPause && !CrewSim.bSoakTest)
		{
			CrewSim.Paused = true;
		}
		if (aLog != null)
		{
			aLog.Add(GrammarUtils.GenerateDescription(this));
		}
		if (this.strChainStart != null)
		{
			CrewSim.objInstance.workManager.CompleteTask(this.strChainStart, this.objUs.strID, this.objThem.strID);
		}
		else
		{
			CrewSim.objInstance.workManager.CompleteTask(this.strName, this.objUs.strID, this.objThem.strID);
		}
		if (this.strIdleAnim != null)
		{
			string animFor = this.strIdleAnim;
			Pathfinder pathfinder = this.objUs.Pathfinder;
			if (pathfinder != null)
			{
				animFor = pathfinder.GetAnimFor(this.strIdleAnim);
			}
			this.objUs.strIdleAnim = animFor;
		}
		if (this.strTeleport != null && this.objThem != null && this.objThem != this.objUs)
		{
			this.Teleport(this.objUs.Pathfinder, isCancelIa);
		}
		Relationship relationship = null;
		Relationship relationship2 = null;
		if (this.objUs.socUs != null && this.objThem.socUs != null && this.objUs.socUs != this.objThem.socUs)
		{
			relationship2 = this.objThem.socUs.GetRelationship(this.objUs.strName);
			if (relationship2 == null)
			{
				relationship2 = this.objThem.socUs.AddStranger(this.objUs.pspec);
			}
			relationship = this.objUs.socUs.GetRelationship(this.objThem.strName);
			if (relationship == null)
			{
				relationship = this.objUs.socUs.AddStranger(this.objThem.pspec);
			}
		}
		if (this.LootAddCondRulesUs != null)
		{
			List<string> lootNames = this.LootAddCondRulesUs.GetLootNames(null, false, null);
			foreach (string strCondRule in lootNames)
			{
				this.objUs.AddCondRule(strCondRule, true);
			}
		}
		if (this.LootAddCondRulesThem != null)
		{
			List<string> lootNames2 = this.LootAddCondRulesThem.GetLootNames(null, false, null);
			foreach (string strCondRule2 in lootNames2)
			{
				this.objThem.AddCondRule(strCondRule2, true);
			}
		}
		this.CalcRate();
		if (this.aLootItemUseContract != null)
		{
			foreach (CondOwner condOwner in this.aLootItemUseContract)
			{
				if (condOwner != null)
				{
					condOwner.Use(this.strUseCase);
				}
			}
			this.aLootItemUseContract = null;
		}
		this.ApplyLootCT(this.LootCTsUs, relationship, this.objUs, this.objThem, 1f);
		this.ApplyLootConds(this.LootCondsUs, relationship, this.objUs, this.objThem, 1f);
		this.ApplyLootCT(this.LootCTsThem, relationship2, this.objThem, this.objUs, this.fCTThemModifierUs * this.fCTThemModifierTools * (1f - this.fCTThemModifierPenalty));
		this.ApplyLootConds(this.LootCondsThem, relationship2, this.objThem, this.objUs, this.fCTThemModifierUs * this.fCTThemModifierTools * (1f - this.fCTThemModifierPenalty));
		this.ApplyLosInteraction(this.aLoSReactions);
		this.objUs.ApplyAModes(this.aAModesAddedUs, true);
		this.objThem.ApplyAModes(this.aAModesAddedThem, true);
		this.objUs.ApplyGPMChanges(this.aGPMChangesUs);
		this.objThem.ApplyGPMChanges(this.aGPMChangesThem);
		if (this.obj3rd != null)
		{
			this.ApplyLootCT(this.LootCTs3rd, null, this.obj3rd, null, 1f);
			this.ApplyLootConds(this.LootConds3rd, null, this.obj3rd, null, 1f);
		}
		if (this.aLootItemRemoveContract != null)
		{
			foreach (CondOwner condOwner2 in this.aLootItemRemoveContract)
			{
				if (!(condOwner2 == null))
				{
					List<CondOwner> singleOrStack = condOwner2.GetSingleOrStack(this.bRemoveWholeStack);
					foreach (CondOwner condOwner3 in singleOrStack)
					{
						if (!(condOwner3 == null))
						{
							Container container = null;
							Ship ship = condOwner3.ship;
							CondOwner objCOParent = condOwner3.objCOParent;
							if (objCOParent != null)
							{
								container = objCOParent.objContainer;
							}
							condOwner3.PopHeadFromStack();
							if (!this.bDestroyItem)
							{
								Vector2 vector = (!(objCOParent != null)) ? Vector2.zero : (objCOParent.tf.position - this.objUs.tf.position).ToVector2();
								CondOwner condOwner4 = this.objUs.DropCO(condOwner3, false, ship, vector.x, vector.y, true, null);
								if (condOwner4 != null && ship != null)
								{
									ship.AddCO(condOwner4, true);
									Debug.LogWarning("Could not find space for item " + condOwner3.strName + " from Interaction " + this.strName);
								}
							}
							Container.Redraw(container);
							condOwner3.UpdateAppearance();
						}
					}
					if (this.objUs == CrewSim.coPlayer && condOwner2.HasCond("IsSocialItem"))
					{
						CanvasManager.instance.goCanvasFloaties.GetComponent<GUISocialItemAnimator>().SpendSocialItemAnimation(condOwner2.strName, condOwner2.strPortraitImg);
					}
					condOwner2.strSourceInteract = this.strChainStart;
					condOwner2.strSourceCO = this.objUs.strID;
					if (this.bDestroyItem)
					{
						CrewSim.objInstance.ScheduleCODestruction(condOwner2);
					}
				}
			}
			this.aLootItemRemoveContract = null;
		}
		if (this.strLootItmRemoveThem != null)
		{
			CondTrigger condTrigger = DataHandler.GetCondTrigger("TIs[us]");
			List<CondTrigger> ctlootFlat = DataHandler.GetLoot(this.strLootItmRemoveThem).GetCTLootFlat(condTrigger, null);
			List<CondOwner> list = this.objThem.GetCOsSafe(false, null);
			list.AddRange(this.objThem.GetLotCOs(false));
			foreach (CondTrigger condTrigger2 in ctlootFlat)
			{
				if (condTrigger2 != null)
				{
					if (condTrigger2.strName == condTrigger.strName)
					{
						this.objThem.RemoveFromCurrentHome(false);
						CrewSim.objInstance.ScheduleCODestruction(this.objThem);
					}
					else
					{
						foreach (CondOwner condOwner5 in list)
						{
							if (!(condOwner5 == null))
							{
								if (condTrigger2.Triggered(condOwner5, null, true))
								{
									condOwner5.RemoveFromCurrentHome(false);
									CrewSim.objInstance.ScheduleCODestruction(condOwner5);
									list.Remove(condOwner5);
									break;
								}
							}
						}
					}
				}
			}
		}
		if (this.strLootItmAddUs != null)
		{
			List<CondOwner> list = DataHandler.GetLoot(this.strLootItmAddUs).GetCOLoot(this.objUs, false, null);
			string text = this.objUs.ShortName + " gains ";
			int num = 0;
			foreach (CondOwner condOwner6 in list)
			{
				if (!(condOwner6 == null))
				{
					if (num > 0)
					{
						text += ", ";
					}
					text += condOwner6.ShortName;
					num++;
					CondOwner condOwner7 = this.objUs.AddCO(condOwner6, this.bEquip, true, true);
					if (condOwner7 != null)
					{
						this.OverflowAddCO(this.objUs, condOwner7);
					}
					if (this.objUs == CrewSim.coPlayer && condOwner6.HasCond("IsSocialItem"))
					{
						CanvasManager.instance.goCanvasFloaties.GetComponent<GUISocialItemAnimator>().SpawnSocialItemAnimation(condOwner6.strName, condOwner6.strPortraitImg);
					}
					condOwner6.strSourceInteract = this.strChainStart;
					condOwner6.strSourceCO = this.objThem.strID;
				}
			}
			text += ".";
			if (num > 0)
			{
				this.objUs.LogMessage(text, "Neutral", this.objThem.strID);
			}
		}
		if (this.strLootItmAddThem != null)
		{
			List<CondOwner> list = DataHandler.GetLoot(this.strLootItmAddThem).GetCOLoot(this.objThem, false, null);
			string text2 = this.objThem.ShortName + " gains ";
			int num2 = 0;
			foreach (CondOwner condOwner8 in list)
			{
				if (!(condOwner8 == null))
				{
					if (num2 > 0)
					{
						text2 += ", ";
					}
					text2 += condOwner8.ShortName;
					num2++;
					CondOwner condOwner9 = this.objThem.AddCO(condOwner8, this.bEquip, true, true);
					if (condOwner9 != null)
					{
						this.OverflowAddCO(this.objThem, condOwner9);
					}
					condOwner8.strSourceInteract = this.strChainStart;
					condOwner8.strSourceCO = this.objThem.strID;
				}
			}
			text2 += ".";
			if (num2 > 0)
			{
				this.objThem.LogMessage(text2, "Neutral", this.objThem.strID);
			}
		}
		if (this.aLootItemGiveContract != null && this.aLootItemGiveContract.Count > 0)
		{
			string text3 = this.objUs.ShortName + " gives ";
			int num3 = 0;
			bool flag = this.objUs != this.objThem;
			foreach (CondOwner condOwner10 in this.aLootItemGiveContract)
			{
				if (!(condOwner10 == null))
				{
					if (num3 > 0)
					{
						text3 += ", ";
					}
					text3 += condOwner10.ShortName;
					num3++;
					flag = (flag && condOwner10 != this.objUs);
					List<CondOwner> singleOrStack2 = condOwner10.GetSingleOrStack(this.bGiveWholeStack);
					if (singleOrStack2 != null)
					{
						foreach (CondOwner condOwner11 in singleOrStack2)
						{
							if (!(condOwner11 == null))
							{
								Container container2 = null;
								if (condOwner11.objCOParent != null)
								{
									container2 = condOwner11.objCOParent.objContainer;
								}
								Ship ship2 = condOwner11.ship;
								if (!this.bGiveWholeStack)
								{
									condOwner11.PopHeadFromStack();
								}
								else
								{
									ship2 = condOwner11.RemoveFromCurrentHome(false);
								}
								if (this.bLot)
								{
									this.objThem.AddLotCO(condOwner11);
								}
								else
								{
									CondOwner condOwner12;
									if (this.bEquip)
									{
										condOwner12 = this.objThem.AddCO(condOwner11, this.bEquip, false, false);
										if (condOwner12 != null)
										{
											this.objThem.LogMessage(Interaction.STR_IA_FAIL_NO_EQUIP, "Bad", this.objThem.strID);
											condOwner12 = this.objThem.AddCO(condOwner11, this.bEquip, true, true);
										}
									}
									else
									{
										condOwner12 = this.objThem.AddCO(condOwner11, this.bEquip, true, true);
									}
									if (condOwner12 != null)
									{
										this.objThem.LogMessage(Interaction.STR_ERROR_NO_ROOM_INV, "Bad", this.objThem.strID);
										if (ship2 != null)
										{
											ship2.AddCO(condOwner12, true);
										}
									}
								}
								Container.Redraw(container2);
								Container.Redraw(this.objThem.objContainer);
							}
						}
					}
					if (this.objUs == CrewSim.coPlayer && condOwner10.HasCond("IsSocialItem"))
					{
						CanvasManager.instance.goCanvasFloaties.GetComponent<GUISocialItemAnimator>().SpendSocialItemAnimation(condOwner10.strName, condOwner10.strPortraitImg);
					}
					condOwner10.strSourceInteract = this.strChainStart;
					condOwner10.strSourceCO = this.objUs.strID;
				}
			}
			text3 = text3 + " to " + this.objThem.ShortName + ".";
			if (flag && num3 > 0)
			{
				this.objThem.LogMessage(text3, "Neutral", this.objThem.strID);
				this.objUs.LogMessage(text3, "Neutral", this.objUs.strID);
			}
			this.aLootItemGiveContract = null;
		}
		if (this.aLootItemTakeContract != null && this.aLootItemTakeContract.Count > 0)
		{
			string text4 = this.objUs.ShortName + " takes ";
			int num4 = 0;
			bool flag2 = this.objUs != this.objThem;
			foreach (CondOwner condOwner13 in this.aLootItemTakeContract)
			{
				if (!(condOwner13 == null))
				{
					if (num4 > 0)
					{
						text4 += ", ";
					}
					text4 += condOwner13.ShortName;
					num4++;
					flag2 = (flag2 && condOwner13 != this.objThem);
					condOwner13.RemoveFromCurrentHome(false);
					CondOwner condOwner14 = this.objUs.AddCO(condOwner13, this.bEquip, true, true);
					if (condOwner14 != null)
					{
						this.objUs.LogMessage(Interaction.STR_ERROR_NO_ROOM_INV, "Bad", this.objUs.strID);
						this.objUs.DropCO(condOwner14, false, this.objUs.ship, 0f, 0f, true, null);
					}
					condOwner13.strSourceInteract = this.strChainStart;
					condOwner13.strSourceCO = this.objThem.strID;
				}
			}
			text4 = text4 + " from " + this.objThem.ShortName + ".";
			if (flag2 && num4 > 0)
			{
				this.objThem.LogMessage(text4, "Neutral", this.objThem.strID);
				this.objUs.LogMessage(text4, "Neutral", this.objUs.strID);
			}
			this.aLootItemTakeContract = null;
		}
		if (this.objLootModeSwitch != null)
		{
			if (this.objUs.ship == null)
			{
				Debug.LogError("Error: Trying to modeswitch an object that has no ship assigned: " + this.objUs.ToString());
			}
			else
			{
				List<string> lootNames3 = this.objLootModeSwitch.GetLootNames(null, false, null);
				List<CondOwner> list = new List<CondOwner>();
				foreach (string strCO in lootNames3)
				{
					string strIDOld = this.objUs.strID;
					if (this.objUs.strPersistentCO != null)
					{
						strIDOld = this.objUs.strPersistentCO;
						this.objUs.strPersistentCO = null;
					}
					if (list.Count == 0)
					{
						list.Add(DataHandler.GetCondOwner(strCO, null, null, !this.objLootModeSwitch.bSuppress, null, null, strIDOld, null));
					}
					else
					{
						list.Add(DataHandler.GetCondOwner(strCO, null, null, !this.objLootModeSwitch.bSuppress, null, null, null, null));
					}
				}
				CondOwner condOwner15 = null;
				while (list.Count > 0)
				{
					if (condOwner15 == null)
					{
						condOwner15 = list[0];
						this.objUs.ModeSwitch(condOwner15, this.objUs.tf.position);
						if (condOwner15 != null)
						{
							this.objUs = condOwner15;
						}
						list.RemoveAt(0);
						break;
					}
					list.RemoveAt(0);
				}
				if (condOwner15 == null)
				{
					Debug.Log(string.Concat(new string[]
					{
						"Error: CO ",
						this.objUs.strName,
						this.objUs.strID,
						" unable to mode switch to null. Mode loot: ",
						this.objLootModeSwitch.strName
					}));
					return;
				}
				if (list.Count > 0)
				{
					Ship ship3;
					if (this.objUs.ship == null)
					{
						if (this.objUs.objCOParent != null)
						{
							if (this.objUs.objCOParent.ship != null)
							{
								ship3 = this.objUs.objCOParent.ship;
							}
							else
							{
								ship3 = CrewSim.shipCurrentLoaded;
							}
						}
						else
						{
							ship3 = CrewSim.shipCurrentLoaded;
						}
					}
					else
					{
						ship3 = this.objUs.ship;
					}
					JsonZone zoneFromTileRadius = TileUtils.GetZoneFromTileRadius(ship3, this.objUs.tf.position, 2, true, false);
					CondTrigger condTrigger3 = DataHandler.GetCondTrigger("TIsLootSpawnOK");
					List<CondOwner> cosInZone = ship3.GetCOsInZone(zoneFromTileRadius, condTrigger3, false, true);
					list = TileUtils.DropCOsNearby(list, ship3, zoneFromTileRadius, cosInZone, condTrigger3, false, true);
					foreach (CondOwner condOwner16 in list)
					{
						Vector3 position = this.objUs.tf.position;
						position.z = condOwner16.tf.position.z;
						condOwner16.tf.position = position;
						ship3.AddCO(condOwner16, true);
					}
				}
			}
		}
		if (this.objLootModeSwitchThem != null)
		{
			if (this.objThem.ship == null)
			{
				Debug.LogError("Error: Trying to modeswitch an object that has no ship assigned: " + this.objThem.ToString());
			}
			else
			{
				List<string> lootNames4 = this.objLootModeSwitchThem.GetLootNames(null, false, null);
				List<CondOwner> list = new List<CondOwner>();
				foreach (string strCO2 in lootNames4)
				{
					string strIDOld2 = this.objThem.strID;
					if (this.objThem.strPersistentCO != null)
					{
						strIDOld2 = this.objThem.strPersistentCO;
						this.objThem.strPersistentCO = null;
					}
					if (list.Count == 0)
					{
						list.Add(DataHandler.GetCondOwner(strCO2, null, null, !this.objLootModeSwitchThem.bSuppress, null, null, strIDOld2, null));
					}
					else
					{
						list.Add(DataHandler.GetCondOwner(strCO2, null, null, !this.objLootModeSwitchThem.bSuppress, null, null, null, null));
					}
				}
				CondOwner condOwner17 = null;
				while (list.Count > 0)
				{
					if (condOwner17 == null)
					{
						condOwner17 = list[0];
						this.objThem.ModeSwitch(condOwner17, this.objThem.tf.position);
						if (condOwner17 != null)
						{
							this.objThem = condOwner17;
						}
						list.RemoveAt(0);
						break;
					}
					list.RemoveAt(0);
				}
				if (condOwner17 == null)
				{
					Debug.Log(string.Concat(new string[]
					{
						"Error: CO ",
						this.objThem.strName,
						this.objThem.strID,
						" unable to mode switch to null. Mode loot: ",
						this.objLootModeSwitchThem.strName
					}));
					return;
				}
				if (list.Count > 0)
				{
					JsonZone zoneFromTileRadius2 = TileUtils.GetZoneFromTileRadius(this.objThem.ship, this.objThem.tf.position, 2, true, false);
					CondTrigger condTrigger4 = DataHandler.GetCondTrigger("TIsLootSpawnOK");
					List<CondOwner> cosInZone2 = this.objThem.ship.GetCOsInZone(zoneFromTileRadius2, condTrigger4, false, true);
					list = TileUtils.DropCOsNearby(list, this.objThem.ship, zoneFromTileRadius2, cosInZone2, condTrigger4, false, true);
					foreach (CondOwner condOwner18 in list)
					{
						if (!(condOwner18 == null))
						{
							Vector3 position2 = this.objUs.tf.position;
							position2.z = condOwner18.tf.position.z;
							condOwner18.tf.position = position2;
							this.objThem.ship.AddCO(condOwner18, true);
						}
					}
				}
			}
		}
		if (this.aTickersUs != null)
		{
			foreach (string text5 in this.aTickersUs)
			{
				if (text5[0] == '-')
				{
					this.objUs.RemoveTicker(text5.Substring(1));
				}
				else
				{
					JsonTicker ticker = DataHandler.GetTicker(text5);
					ticker.SetTimeLeft(ticker.fTimeLeft);
					this.objUs.AddTicker(ticker);
				}
			}
		}
		if (this.aTickersThem != null)
		{
			foreach (string text6 in this.aTickersThem)
			{
				if (text6[0] == '-')
				{
					this.objThem.RemoveTicker(text6.Substring(1));
				}
				else
				{
					JsonTicker ticker2 = DataHandler.GetTicker(text6);
					ticker2.SetTimeLeft(ticker2.fTimeLeft);
					this.objThem.AddTicker(ticker2);
				}
			}
		}
		int num5 = 0;
		while (this.aSocialNew != null && num5 < this.aSocialNew.Length)
		{
			if (this.objUs.socUs == null)
			{
				this.objUs.socUs = this.objUs.gameObject.AddComponent<global::Social>();
			}
			PersonSpec personSpec = null;
			string[] array = this.aSocialNew[num5].Split(new char[]
			{
				'='
			});
			JsonPersonSpec personSpec2 = DataHandler.GetPersonSpec(array[0]);
			if (personSpec2 != null)
			{
				string text7 = personSpec2.strRelSet;
				if (text7 == null || text7 == string.Empty)
				{
					text7 = "RELStranger";
				}
				Loot loot = DataHandler.GetLoot(text7);
				bool flag3 = true;
				if (array.Length > 1 && array[1].ToLower() == "true")
				{
					flag3 = false;
				}
				if (flag3)
				{
					personSpec = StarSystem.GetPerson(personSpec2, this.objUs.socUs, true, null, null);
				}
				if (personSpec == null)
				{
					personSpec = new PersonSpec(personSpec2, true);
				}
				CondOwner condOwner19 = personSpec.GetCO();
				if (condOwner19 == null)
				{
					condOwner19 = personSpec.MakeCondOwner(PersonSpec.StartShip.OLD, null);
				}
				if (this.ChangeRelationship(text7, condOwner19, this.objUs, null, true))
				{
					Relationship relationship3 = this.objUs.socUs.GetRelationship(condOwner19.strID);
					if (relationship3 != null)
					{
						relationship3.RevealDefaults();
					}
					if (condOwner19.socUs != null)
					{
						relationship3 = condOwner19.socUs.GetRelationship(this.objUs.strID);
						if (relationship3 != null)
						{
							relationship3.RevealDefaults();
						}
					}
					GUIChargenStack component = condOwner19.GetComponent<GUIChargenStack>();
					string text8 = "New ";
					bool flag4 = false;
					foreach (string text9 in loot.GetLootNames(null, false, null))
					{
						Condition cond = DataHandler.GetCond(text9);
						if (cond != null)
						{
							if (flag4)
							{
								text8 += "/";
							}
							else
							{
								flag4 = true;
							}
							text8 += cond.strNameFriendly;
						}
					}
					text8 = text8 + ": " + personSpec.FullName;
					if (component.GetLatestCareer() != null && component.GetLatestCareer().GetJC() != null)
					{
						text8 = text8 + ", " + component.GetLatestCareer().GetJC().strNameFriendly;
					}
					text8 = text8 + " from " + component.GetHomeworld().strColonyName + ".";
					if (aLog != null)
					{
						aLog.Add(text8);
					}
				}
			}
			num5++;
		}
		this.ChangeRelationship(this.strLootRELChangeThemSeesUs, this.objUs, this.objThem, aLog, false);
		this.ChangeRelationship(this.strLootRELChangeThemSees3rd, this.obj3rd, this.objThem, aLog, false);
		this.ChangeRelationship(this.strLootRELChangeUsSeesThem, this.objThem, this.objUs, aLog, false);
		this.ChangeRelationship(this.strLootRELChangeUsSees3rd, this.obj3rd, this.objUs, aLog, false);
		this.ChangeRelationship(this.strLootRELChange3rdSeesUs, this.objUs, this.obj3rd, aLog, false);
		this.ChangeRelationship(this.strLootRELChange3rdSeesThem, this.objThem, this.obj3rd, aLog, false);
		this.UpdateStakes(relationship, relationship2);
		if (this.objUs.socUs != null && !this.objUs.HasCond("IsPlayer") && this.objThem.HasCond("IsPlayer") && this.objUs.bAlive && this.objThem.socUs != null)
		{
			List<string> list2 = new List<string>();
			List<string> list3 = new List<string>();
			int num6 = 0;
			foreach (string item in this.CTTestUs.GetAllReqNames(false))
			{
				if (!list2.Contains(item))
				{
					list2.Add(item);
				}
			}
			if (this.aCondUsPriorities != null)
			{
				foreach (CondScore condScore in this.aCondUsPriorities)
				{
					string discomfortForCond = this.objUs.GetDiscomfortForCond(condScore.strName);
					if (discomfortForCond != null && !relationship2.aReveals.Contains(discomfortForCond))
					{
						list3.Add(discomfortForCond);
						break;
					}
				}
			}
			foreach (string item2 in relationship2.aReveals)
			{
				if (this.objUs.HasCond(item2) && !list3.Contains(item2))
				{
					list3.Add(item2);
				}
			}
			foreach (string text10 in list2)
			{
				if (this.objUs.HasCond(text10) && (this.objUs.mapConds[text10].nDisplayOther != 0 || this.objUs.mapConds[text10].nDisplayOther != 3) && list3.IndexOf(text10) < 0 && (num6 <= 0 || MathUtils.Rand(0.0, 1.0, MathUtils.RandType.Flat, null) < 0.5) && GUISocialCombat2.CountsAsSocialReveal(text10, true, true, true, true))
				{
					list3.Add(text10);
				}
			}
			relationship2.aReveals = list3;
			if (GUISocialCombat2.coUs == this.objUs || GUISocialCombat2.coThem == this.objUs)
			{
				GUISocialCombat2.objInstance.UpdateCO(this.objUs);
			}
			else
			{
				CrewSim.objInstance.UpdateLog(this.objUs, null);
			}
		}
		Ledger.AddLI(this.strLedgerDef, this.objUs, this.objThem);
		if (this.strPledgeAdd != null)
		{
			JsonPledge pledge = DataHandler.GetPledge(this.strPledgeAdd);
			if (pledge != null)
			{
				CondOwner coThem = null;
				if (pledge.strThemID == "[them]")
				{
					coThem = this.objThem;
				}
				else if (pledge.strThemID == "[3rd]")
				{
					coThem = this.obj3rd;
				}
				Pledge2 pledge2 = PledgeFactory.Factory(this.objUs, pledge, coThem);
				if (pledge2 != null)
				{
					pledge2.Us.AddPledge(pledge2);
				}
			}
		}
		if (this.strPledgeAddThem != null)
		{
			JsonPledge pledge3 = DataHandler.GetPledge(this.strPledgeAddThem);
			if (pledge3 != null)
			{
				CondOwner coThem2 = null;
				if (pledge3.strThemID == "[them]")
				{
					coThem2 = this.objUs;
				}
				else if (pledge3.strThemID == "[3rd]")
				{
					coThem2 = this.obj3rd;
				}
				Pledge2 pledge4 = PledgeFactory.Factory(this.objThem, pledge3, coThem2);
				if (pledge4 != null)
				{
					pledge4.Us.AddPledge(pledge4);
				}
			}
		}
		bool flag5 = true;
		if (this.attackMode != null)
		{
			double condAmount = this.objThem.GetCondAmount("StatDefense");
			double num7 = 50.0 - (condAmount + (double)this.attackMode.fTargetDefenseMod);
			num7 = MathUtils.Clamp(num7, 0.05, 1.0);
			bool flag6 = this.objThem.ship != null && this.objThem.ship.LoadState >= Ship.Loaded.Edit;
			bool bAudio = Wound.bAudio;
			bool flag7 = false;
			string text11 = (!string.IsNullOrEmpty(this.strAttackerName)) ? this.strAttackerName : this.objUs.ShortName;
			if (flag6)
			{
				Wound.bAudio = true;
				if (!this.attackMode.bPlayAudioEarly && !string.IsNullOrEmpty(this.attackMode.strAudioAttack))
				{
					AudioEmitter component2 = this.objUs.GetComponent<AudioEmitter>();
					if (component2 != null)
					{
						component2.StartOther(this.attackMode.strAudioAttack);
					}
				}
			}
			if (this.attackMode.GetAttackType() == JsonAttackMode.Type.melee)
			{
				Tile tileAtWorldCoords = this.objUs.ship.GetTileAtWorldCoords1(this.objUs.tf.position.x, this.objUs.tf.position.y, true, true);
				if (this.strTargetPoint != null && this.strTargetPoint != Interaction.POINT_REMOTE)
				{
					Vector2 pos = this.objThem.GetPos(this.strTargetPoint, false);
					Tile tileAtWorldCoords2 = this.objUs.ship.GetTileAtWorldCoords1(pos.x, pos.y, true, true);
					flag5 = ((float)TileUtils.TileRange(tileAtWorldCoords, tileAtWorldCoords2) <= this.fTargetPointRange);
				}
				if (!flag5)
				{
					string strMsg = this.objThem.ShortName + " out of range.";
					this.objUs.LogMessage(strMsg, "StatusRed", this.objUs.strName);
					if (this.objUs != this.objThem)
					{
						this.objThem.LogMessage(strMsg, "StatusRed", this.objUs.strName);
					}
				}
				else
				{
					double num8 = MathUtils.Rand(0.0, 1.0, MathUtils.RandType.Flat, null);
					if (num8 > num7)
					{
						string strMsg2 = text11 + Interaction.STR_COMBAT_MISSED;
						this.objUs.LogMessage(strMsg2, "Bad", this.objUs.strName);
						if (this.objUs != this.objThem)
						{
							this.objThem.LogMessage(strMsg2, "Bad", this.objUs.strName);
						}
					}
					else if (this.objThem.HasCond("IsWoundable"))
					{
						flag7 = true;
						if (this.attackMode.fDmgBlunt != 0f || this.attackMode.fDmgCut != 0f || this.attackMode.fDmgEnv != 0f)
						{
							bool bBlunt = (double)this.attackMode.fDmgBlunt > 0.5;
							bool bCut = (double)this.attackMode.fDmgCut > 0.5;
							Wound woundLocation = this.objThem.GetWoundLocation(bBlunt, bCut);
							if (woundLocation != null)
							{
								Vector2 vector2 = default(Vector2);
								if (string.IsNullOrEmpty(this.strAttackerName))
								{
									vector2 = woundLocation.Damage(this.attackMode, this.objUs, true, null);
								}
								else
								{
									Wound wound = woundLocation;
									JsonAttackMode jam = this.attackMode;
									CondOwner coSource = null;
									string strAttacker = text11;
									vector2 = wound.Damage(jam, coSource, true, strAttacker);
								}
								if (flag6)
								{
									if (this.objUs == CrewSim.GetSelectedCrew())
									{
										CrewSim.objInstance.CamShake(Mathf.Max(vector2.x, vector2.y));
									}
									this.objThem.PlayHitAnim((double)this.attackMode.fDmgBlunt, (double)this.attackMode.fDmgCut);
								}
							}
						}
					}
					else if (this.objThem.HasCond("IsDamageable"))
					{
						flag7 = true;
						if (this.attackMode.fDmgEnv != 0f)
						{
							Destructable component3 = this.objThem.GetComponent<Destructable>();
							if (component3 != null)
							{
								float num9 = this.attackMode.fDmgEnv * (float)this.attackMode.GetDmgAmount((!string.IsNullOrEmpty(this.strAttackerName)) ? null : this.objUs);
								component3.CO.AddCondAmount("StatDamage", (double)num9, 0.0, 0f);
								component3.DamageCheck();
								component3.CO.EndTurn();
								if (flag6 && this.objUs == CrewSim.GetSelectedCrew())
								{
									CrewSim.objInstance.CamShake(num9 * 0.1f);
								}
							}
						}
					}
				}
			}
			else
			{
				float fRadius = this.attackMode.fRange * 1f;
				Vector3 vector3 = this.objUs.GetPos(null, false);
				Vector3 vector4 = this.objThem.GetPos(null, false);
				vector4 -= vector3;
				Vector3 vector5 = new Vector3(vector4.y, -vector4.x, vector4.z);
				Vector2 v = new Vector3(-vector4.y, vector4.x, vector4.z);
				double num10 = MathUtils.Rand(0.0, 1.0, MathUtils.RandType.Flat, null);
				if (num10 > num7)
				{
					string strMsg3 = this.objUs.ShortName + Interaction.STR_COMBAT_MISSED;
					this.objUs.LogMessage(strMsg3, "Bad", this.objUs.strName);
					if (this.objUs != this.objThem)
					{
						this.objThem.LogMessage(strMsg3, "Bad", this.objUs.strName);
					}
					Vector3 target = vector5;
					if (MathUtils.Rand(0.0, 1.0, MathUtils.RandType.Flat, null) > 0.5)
					{
						target = v;
					}
					vector4 = Vector3.RotateTowards(vector4, target, 0.5f, 0f);
				}
				else
				{
					flag7 = true;
				}
				vector3.z = -0.25f;
				for (int k = 0; k < 1 + this.attackMode.nExtraRays; k++)
				{
					Vector3 target2 = vector5;
					if (MathUtils.Rand(0.0, 1.0, MathUtils.RandType.Flat, null) > 0.5)
					{
						target2 = v;
					}
					vector4 = Vector3.RotateTowards(vector4, target2, MathUtils.Rand(0f, 0.017453292f * this.attackMode.fSpread, MathUtils.RandType.Low, null), 0f);
					this.objUs.ship.DamageRay(vector3, vector4.normalized, fRadius, 1f, this.attackMode, this.objUs, true);
				}
			}
			string text12 = this.strIAMiss;
			if (flag7)
			{
				text12 = this.strIAHit;
			}
			if (text12 != null)
			{
				Interaction interaction = DataHandler.GetInteraction(text12, null, false);
				if (interaction != null)
				{
					interaction.objUs = this.objUs;
					interaction.objThem = this.objThem;
					interaction.obj3rd = this.obj3rd;
					interaction.ApplyChain(null);
				}
			}
			Wound.bAudio = bAudio;
		}
		if (!string.IsNullOrEmpty(this.strCrime))
		{
			CrimeManager.LogCrime(this);
		}
		if (this.fFactionScoreChangeThem != 0f)
		{
			this.objThem.ApplyFactionReps(this.objUs, this.fFactionScoreChangeThem);
		}
		if (this.fFactionScoreChangeUs != 0f)
		{
			this.objUs.ApplyFactionReps(this.objThem, this.fFactionScoreChangeThem);
		}
		if (this.LootAddFactionsUs != null)
		{
			this.LootAddFactions(this.LootAddFactionsUs.GetLootNames(null, false, null), this.objUs);
		}
		if (this.LootAddFactionsThem != null)
		{
			this.LootAddFactions(this.LootAddFactionsThem.GetLootNames(null, false, null), this.objThem);
		}
		if (this.bInterrupt && flag5 && this.objThem != null)
		{
			this.objThem.AICancelAll(null);
			if (this.objThem == CrewSim.GetSelectedCrew())
			{
				CrewSim.LowerUI(false);
			}
			if (GUISocialCombat2.IsInSocialCombat(this.objThem))
			{
				GUISocialCombat2.objInstance.EndSocialCombat();
			}
		}
		if (this.strMusic != null)
		{
			AudioManager.am.SuggestMusic(this.strMusic, this.bForceMusic);
		}
		if (this.teleportRegIDTarget != null)
		{
			CondOwner condOwner20 = this.objUs;
			if (this.teleportRegIDTarget.Item2.ToLower().Contains("them"))
			{
				condOwner20 = this.objThem;
			}
			else if (this.teleportRegIDTarget.Item2.ToLower().Contains("3rd"))
			{
				condOwner20 = this.obj3rd;
			}
			if (this.teleportRegIDTarget.Item1 == "FERRY")
			{
				if (AIShipManager.FerryComingForCO(condOwner20.strID))
				{
					CrewSim.objInstance.TeleportCO(condOwner20, AIShipManager.FerryDestForCO(condOwner20.strID));
				}
			}
			else
			{
				CrewSim.objInstance.TeleportCO(condOwner20, this.teleportRegIDTarget.Item1);
			}
		}
		if (this.objUs != null)
		{
			MonoSingleton<ObjectiveTracker>.Instance.CheckObjective(this.objUs.strID);
		}
		if (this.objThem != null)
		{
			MonoSingleton<ObjectiveTracker>.Instance.CheckObjective(this.objThem.strID);
		}
		if (this.bRecheckAllPlots)
		{
			PlotManager.AddPlotCheck((PlotManager.PlotTensionType)3);
		}
		else if (this.bRecheckThisPlot)
		{
			int nPlayerNoticed = PlotManager.nPlayerNoticed;
			PlotManager.nPlayerNoticed = 0;
			PlotManager.CheckPlot(this.strPlot, CrewSim.GetSelectedCrew(), (PlotManager.PlotTensionType)3, null, false);
			PlotManager.nPlayerNoticed = nPlayerNoticed;
		}
		if (this.aCustomInfos != null && this.aCustomInfos.Length > 0)
		{
			CrewSim.SetCustomInfos(this.aCustomInfos);
		}
		if (this.bHardCode)
		{
			string[] array2 = new string[]
			{
				"SOCHireAllow",
				"SOCHire2BasicAllow"
			};
			string[] array3 = new string[]
			{
				"SOCHireFire"
			};
			if (Array.IndexOf<string>(array3, this.strName) >= 0)
			{
				if (this.objUs.Company == null)
				{
					Debug.Log("Error: Null Company on " + this.objUs.ToString() + " with quitter: " + this.objThem.ToString());
				}
				else
				{
					this.objUs.Company.DismissMember(this.objThem.strID, this.objUs);
				}
			}
			array3 = new string[]
			{
				"SOCHireQuits",
				"SOCHireQuitsGlad"
			};
			if (Array.IndexOf<string>(array3, this.strName) >= 0)
			{
				if (this.objThem.Company == null)
				{
					Debug.Log("Error: Null company on " + this.objThem.ToString() + " with quitter: " + this.objUs.ToString());
				}
				else
				{
					this.objThem.Company.DismissMember(this.objUs.strID, this.objThem);
				}
				if (this.strName == "SOCHireQuits")
				{
					AudioManager.am.SuggestMusic("Loss", true);
				}
			}
			array3 = new string[]
			{
				"SOCHireQuitsMad"
			};
			if (Array.IndexOf<string>(array3, this.strName) >= 0)
			{
				if (this.objThem.Company == null)
				{
					Debug.Log("Error: Null company on " + this.objThem.ToString() + " with quitter: " + this.objUs.ToString());
				}
				else
				{
					this.objThem.Company.DismissMember(this.objUs.strID, this.objThem);
				}
				AudioManager.am.SuggestMusic("Loss", true);
			}
			array3 = new string[]
			{
				"PickupDragStart",
				"PickupDragStartNPCPledge"
			};
			if (Array.IndexOf<string>(array3, this.strName) >= 0 && !this.objUs.compSlots.SlotItem("drag", this.objThem, true))
			{
				Debug.LogWarning("Tried to drag " + this.objThem.strNameFriendly + " but had no drag slot!");
			}
			array3 = new string[]
			{
				"PickupDragStop"
			};
			if (Array.IndexOf<string>(array3, this.strName) >= 0)
			{
				Slot slot = this.objUs.compSlots.GetSlot("drag");
				if (slot != null && !this.objUs.HasCond("IsDragging"))
				{
					CondOwner condOwner21 = this.objUs.compSlots.UnSlotItem("drag", null, false);
					Vector3 vector6 = Vector3.zero;
					if (!(condOwner21 != null))
					{
						return;
					}
					vector6 = condOwner21.tf.position - this.objUs.tf.position;
					Item item3 = condOwner21.Item;
					if (item3 != null)
					{
						this.objUs.DropCO(condOwner21, false, this.objUs.ship, vector6.x, vector6.y, true, null);
					}
					else
					{
						condOwner21.tf.parent = this.objThem.tf.parent;
						condOwner21.ship = this.objThem.ship;
						condOwner21.currentRoom = this.objThem.currentRoom;
					}
				}
				else
				{
					slot = this.objThem.compSlots.GetSlot("drag");
					if (slot != null)
					{
						CondOwner condOwner22 = this.objThem.compSlots.UnSlotItem("drag", null, false);
						if (condOwner22 != null)
						{
							Vector3 vector7 = condOwner22.tf.position - this.objThem.tf.position;
							Item item4 = condOwner22.Item;
							if (item4 != null)
							{
								this.objThem.DropCO(condOwner22, false, this.objThem.ship, 0f, 0f, true, null);
							}
							else
							{
								condOwner22.tf.parent = this.objThem.tf.parent;
								condOwner22.ship = this.objThem.ship;
								condOwner22.currentRoom = this.objThem.currentRoom;
							}
						}
					}
				}
			}
			array3 = new string[]
			{
				"DropCorpse"
			};
			if (Array.IndexOf<string>(array3, this.strName) >= 0)
			{
				Slot slot2 = this.objUs.compSlots.GetSlot("drag");
				if (slot2 != null && this.objUs.HasCond("IsDragging"))
				{
					Ship ship4 = this.objUs.ship;
					CondOwner condOwner23 = this.objUs.compSlots.UnSlotItem("drag", null, false);
					if (ship4 != null)
					{
						ship4.AddCO(condOwner23, true);
						condOwner23.currentRoom = ship4.GetRoomAtWorldCoords1(condOwner23.tf.position, true);
					}
				}
			}
			array3 = new string[]
			{
				"JettisonCorpse"
			};
			if (Array.IndexOf<string>(array3, this.strName) >= 0)
			{
				Slot slot3 = this.objUs.compSlots.GetSlot("drag");
				if (slot3 != null && this.objUs.HasCond("IsDragging"))
				{
					CondOwner condOwner24 = this.objUs.compSlots.UnSlotItem("drag", null, false);
					if (condOwner24 != null)
					{
						CrewSim.objInstance.ScheduleCODestruction(condOwner24);
					}
				}
			}
			array3 = new string[]
			{
				"JettisonRobot"
			};
			if (Array.IndexOf<string>(array3, this.strName) >= 0)
			{
				CondOwner objThem = this.objThem;
				Slot slot4 = this.objUs.compSlots.GetSlot("drag");
				if (slot4 != null && this.objUs.HasCond("IsDragging"))
				{
					this.objUs.compSlots.UnSlotItem("drag", null, false);
				}
				if (this.objThem.HasCond("IsSlotted") && this.objThem.GetSlotParent() != null)
				{
					this.objThem.GetSlotParent().RemoveCO(this.objThem, true);
				}
				if (objThem != null)
				{
					CrewSim.objInstance.ScheduleCODestruction(objThem);
				}
			}
			array3 = new string[]
			{
				"Strip"
			};
			if (Array.IndexOf<string>(array3, this.strName) >= 0)
			{
				List<CondOwner> cos = this.objThem.compSlots.GetCOs(null, false, null);
				this.objThem.DropSlottedItems(cos);
			}
			array3 = new string[]
			{
				"ShakeOff"
			};
			if (Array.IndexOf<string>(array3, this.strName) >= 0)
			{
				CondOwner objCO = this.objThem.compSlots.UnSlotItem("drag", null, false);
				this.objThem.DropCO(objCO, false, null, 0f, 0f, true, null);
			}
		}
	}

	private void OverflowAddCO(CondOwner objTarget, CondOwner coRemain)
	{
		if (coRemain == null || objTarget == null)
		{
			return;
		}
		if (objTarget.ship != null)
		{
			if (objTarget.ship.LoadState >= Ship.Loaded.Edit)
			{
				coRemain = objTarget.DropCO(coRemain, false, null, 0f, 0f, true, null);
			}
			else
			{
				CondOwner condOwner = DataHandler.GetCondOwner("SysLootSpawnerLot");
				condOwner.ApplyGPMChanges(new string[]
				{
					"Panel A,strType,Lot Loot",
					"Panel A,strRange,2",
					"Panel A,strCount,1",
					"Panel A,strLoot,aLot",
					"Panel A,strNew,true",
					"Panel A,strDamaged,true",
					"Panel A,strDerelict,true"
				});
				condOwner.AddLotCO(coRemain);
				condOwner.Item.ResetTransforms(objTarget.tf.position.x, objTarget.tf.position.y);
				objTarget.AddLotCO(condOwner);
			}
		}
		else
		{
			coRemain.RemoveFromCurrentHome(false);
			coRemain.Destroy();
		}
	}

	private void LootAddFactions(List<string> aFactions, CondOwner target)
	{
		if (aFactions == null || target == null)
		{
			return;
		}
		foreach (string text in aFactions)
		{
			bool flag = text.IndexOf("-") == 0;
			if (flag)
			{
				target.RemoveFaction(CrewSim.system.GetFaction(text.Substring(1)));
			}
			else
			{
				target.AddFaction(CrewSim.system.GetFaction(text));
			}
		}
	}

	private void UpdateStakes(Relationship relUs, Relationship relThem)
	{
		if (this.strLootContextUs == "Default" || this.strLootContextThem == "Default")
		{
			if ((this.objUs == CrewSim.coPlayer || this.objThem == CrewSim.coPlayer) && this.objUs.socUs != null)
			{
				GUISocialCombat2.UpdateContext(this);
			}
			if (relUs != null && this.strLootContextUs != null)
			{
				relUs.strContext = this.strLootContextUs;
			}
			if (relThem != null && this.strLootContextThem != null)
			{
				relThem.strContext = this.strLootContextThem;
			}
		}
		else
		{
			if (relUs != null && this.strLootContextUs != null)
			{
				relUs.strContext = this.strLootContextUs;
			}
			if (relThem != null && this.strLootContextThem != null)
			{
				relThem.strContext = this.strLootContextThem;
			}
			if ((this.objUs == CrewSim.coPlayer || this.objThem == CrewSim.coPlayer) && this.objUs.socUs != null)
			{
				GUISocialCombat2.UpdateContext(this);
			}
		}
	}

	private bool ChangeRelationship(string strLootRELChange, CondOwner coRelative, CondOwner coUs, List<string> aLog = null, bool bDoReciprocal = false)
	{
		if (coRelative == null || coUs == null || coUs.pspec == null || coUs.socUs == null || coRelative.pspec == null || coRelative.socUs == null)
		{
			return false;
		}
		bool result = false;
		List<string> lootNames = DataHandler.GetLoot(strLootRELChange).GetLootNames(null, false, null);
		if (lootNames.Count > 0)
		{
			foreach (string text in lootNames)
			{
				if (text.IndexOf("-") == 0)
				{
					result = true;
					coUs.socUs.RemovePerson(coRelative.pspec, new List<string>
					{
						text.Substring(1)
					});
					if (bDoReciprocal)
					{
						string reciprocalREL = Relationship.GetReciprocalREL(text.Substring(1), null);
						coRelative.socUs.RemovePerson(coUs.pspec, new List<string>
						{
							reciprocalREL
						});
					}
				}
				else
				{
					Condition cond = DataHandler.GetCond(text);
					if (cond != null)
					{
						coUs.socUs.AddPerson(new Relationship(coRelative.pspec, new List<string>
						{
							text
						}, new List<string>
						{
							"Became " + cond.strNameFriendly + " during: " + this.strTitle
						}));
						result = true;
						if (aLog != null)
						{
							string item = string.Concat(new string[]
							{
								coRelative.strName,
								" becomes a ",
								cond.strNameFriendly,
								" to ",
								coUs.strName,
								"."
							});
							aLog.Add(item);
						}
						this.LogSocial(coRelative.strName, text, this.strTitle);
						if (bDoReciprocal)
						{
							string reciprocalREL2 = Relationship.GetReciprocalREL(text, null);
							cond = DataHandler.GetCond(reciprocalREL2);
							if (cond != null)
							{
								coRelative.socUs.AddPerson(new Relationship(coUs.pspec, new List<string>
								{
									reciprocalREL2
								}, new List<string>
								{
									"Became " + cond.strNameFriendly + " during: " + this.strTitle
								}));
							}
						}
					}
				}
			}
		}
		return result;
	}

	public void ApplyChain(List<string> aLog = null)
	{
		if (this.objUs == null || this.objThem == null)
		{
			return;
		}
		this.ApplyLogging(this.objUs.strName, true);
		this.ApplyEffects(aLog, false);
		Interaction reply = this.GetReply();
		if (reply != null)
		{
			reply.ApplyChain(null);
		}
	}

	public void ApplyLogging(string strOwner, bool bTraitSuffix)
	{
		if (this.nLogging != Interaction.Logging.NONE && !this.bLogged)
		{
			string strMsg = GrammarUtils.GenerateDescription(this, true);
			List<CondOwner> list = new List<CondOwner>();
			switch (this.nLogging)
			{
			case Interaction.Logging.GROUP:
				list.Add(this.objUs);
				if (this.strThemType == Interaction.TARGET_OTHER)
				{
					list.Add(this.objThem);
				}
				break;
			case Interaction.Logging.ROOM:
				if (this.objUs.currentRoom != null)
				{
					list.AddRange(this.objUs.ship.GetPeopleInRoom(this.objUs.currentRoom, null));
				}
				if (!list.Contains(this.objUs))
				{
					list.Add(this.objUs);
				}
				if (!list.Contains(this.objThem))
				{
					list.Add(this.objThem);
				}
				break;
			case Interaction.Logging.SHIP:
				if (this.objUs.ship != null)
				{
					list.AddRange(this.objUs.ship.GetPeople(true));
				}
				break;
			}
			foreach (CondOwner condOwner in list)
			{
				condOwner.LogMessage(strMsg, this.strColor, strOwner);
			}
			this.bLogged = true;
		}
	}

	private void ApplyLootCT(Loot LootCTs, Relationship relUs, CondOwner coUs, CondOwner coThem, float fCoeff)
	{
		if (coUs == null)
		{
			return;
		}
		Dictionary<string, double> dictionary = null;
		if (LootCTs != null && LootCTs.strName != "Blank")
		{
			List<CondTrigger> ctloot = LootCTs.GetCTLoot(null, null);
			if (relUs != null)
			{
				dictionary = new Dictionary<string, double>();
			}
			foreach (CondTrigger condTrigger in ctloot)
			{
				if (condTrigger != null && condTrigger.Triggered(coUs, null, true))
				{
					condTrigger.ApplyChanceID(true, coUs, fCoeff, 0f);
					coUs.AddRememberScore(condTrigger.strCondName, (double)(condTrigger.fCount * fCoeff));
					if (dictionary != null)
					{
						if (!dictionary.ContainsKey(condTrigger.strCondName))
						{
							dictionary[condTrigger.strCondName] = (double)(condTrigger.fCount * fCoeff);
						}
						else
						{
							Dictionary<string, double> dictionary2;
							string strCondName;
							(dictionary2 = dictionary)[strCondName = condTrigger.strCondName] = dictionary2[strCondName] + (double)(condTrigger.fCount * fCoeff);
						}
					}
				}
			}
		}
		if (coUs == this.objUs && !this.bHumanOnly)
		{
			coUs.aRememberIAs.Insert(0, this.strName);
		}
		coUs.RememberEffects2(coThem);
		if (coUs == this.objUs)
		{
			coUs.RememberLess();
		}
		if (relUs != null)
		{
			relUs.StoreCond(coUs, dictionary, coThem);
		}
	}

	private void ApplyLootConds(Loot LootConds, Relationship relUs, CondOwner coUs, CondOwner coThem, float fCoeff)
	{
		if (coUs == null)
		{
			return;
		}
		Dictionary<string, double> dictionary = null;
		if (LootConds != null && LootConds.strName != "Blank")
		{
			Dictionary<string, double> condLoot = LootConds.GetCondLoot(fCoeff, null, null);
			if (relUs != null)
			{
				dictionary = new Dictionary<string, double>();
			}
			foreach (KeyValuePair<string, double> keyValuePair in condLoot)
			{
				coUs.AddCondAmount(keyValuePair.Key, keyValuePair.Value, 0.0, 0f);
				coUs.AddRememberScore(keyValuePair.Key, keyValuePair.Value);
				if (dictionary != null)
				{
					if (!dictionary.ContainsKey(keyValuePair.Key))
					{
						dictionary[keyValuePair.Key] = keyValuePair.Value;
					}
					else
					{
						Dictionary<string, double> dictionary2;
						string key;
						(dictionary2 = dictionary)[key = keyValuePair.Key] = dictionary2[key] + keyValuePair.Value;
					}
				}
			}
		}
		if (coUs == this.objUs && !this.bHumanOnly)
		{
			coUs.aRememberIAs.Insert(0, this.strName);
		}
		coUs.RememberEffects2(coThem);
		if (coUs == this.objUs)
		{
			coUs.RememberLess();
		}
		if (relUs != null)
		{
			relUs.StoreCond(coUs, dictionary, coThem);
		}
	}

	public string GetTextLinked(CondOwner coHighlight = null, string strOpen = null, string strClose = null)
	{
		GrammarUtils.highlight = coHighlight;
		string text = GrammarUtils.GetInflectedString(this.strDesc, this);
		if (this.aSocialPrereqsFound != null)
		{
			for (int i = 0; i < this.aSocialPrereqsFound.Length; i++)
			{
				text = text.Replace("[prereq" + i + "]", this.aSocialPrereqsFound[i]);
			}
		}
		return text;
	}

	public string GetDataPayload()
	{
		Loot loot = this.LootCTsUs;
		CondOwner condOwner = this.objUs;
		if (this.LootCTsUs != null && this.LootCTsUs.strName != "Blank" && (this.LootCTsUs.strType == "trigger" || this.LootCTsUs.strType == "data"))
		{
			loot = this.LootCTsUs;
			condOwner = this.objUs;
		}
		else if (this.LootCTsThem != null && this.LootCTsThem.strName != "Blank" && (this.LootCTsThem.strType == "trigger" || this.LootCTsThem.strType == "data"))
		{
			loot = this.LootCTsThem;
			condOwner = this.objThem;
		}
		string text = string.Empty;
		if (loot == null || loot.strName == "Blank" || condOwner == null || condOwner.ship == null)
		{
			return text;
		}
		if (loot.strType == "trigger")
		{
			List<string> lootNames = loot.GetLootNames(null, false, null);
			if (lootNames != null && lootNames.Count >= 1)
			{
				CondTrigger condTrigger = DataHandler.GetCondTrigger(lootNames.First<string>());
				if (condTrigger.IsBlank())
				{
					return text;
				}
				List<CondOwner> cos = condOwner.ship.GetCOs(condTrigger, true, false, true);
				for (int i = 0; i < cos.Count; i++)
				{
					CondOwner condOwner2 = cos[i];
					text += condOwner2.FriendlyName;
					if (i != cos.Count - 1)
					{
						text += ", ";
					}
				}
			}
			return text;
		}
		if (loot.strType == "data")
		{
			try
			{
				List<string> lootNames2 = loot.GetLootNames(null, false, null);
				foreach (string text2 in lootNames2)
				{
					if (!string.IsNullOrEmpty(text2) && !(text2 == "Blank"))
					{
						if (text2.FirstOrDefault<char>() == '+')
						{
							text += text2.Replace("+", string.Empty);
						}
						else
						{
							PropertyInfo property = typeof(Ship).GetProperty(text2);
							if (property != null)
							{
								object value = property.GetValue(condOwner.ship, null);
								if (value != null)
								{
									string text3 = value.ToString();
									double num;
									if (double.TryParse(text3, out num))
									{
										text3 = text3.Split(new char[]
										{
											'.'
										}).FirstOrDefault<string>();
									}
									text += text3;
								}
							}
							else if (typeof(Ship).GetField(text2) != null)
							{
								FieldInfo field = typeof(Ship).GetField(text2);
								if (field != null)
								{
									object value2 = field.GetValue(condOwner.ship);
									if (value2 != null)
									{
										string text4 = value2.ToString();
										double num2;
										if (double.TryParse(text4, out num2))
										{
											text4 = text4.Split(new char[]
											{
												'.'
											}).FirstOrDefault<string>();
										}
										text += text4;
									}
								}
							}
							else
							{
								MethodInfo method = typeof(Ship).GetMethod(text2);
								if (method != null)
								{
									object obj = method.Invoke(condOwner.ship, null);
									if (obj != null)
									{
										string text5 = obj.ToString();
										double num3;
										if (double.TryParse(text5, out num3))
										{
											text5 = text5.Split(new char[]
											{
												'.'
											}).FirstOrDefault<string>();
										}
										text += text5;
									}
								}
							}
						}
					}
				}
			}
			catch (Exception ex)
			{
				Debug.LogWarning("Could not parse Data Loot from interaction");
			}
		}
		return text;
	}

	public string GetTextRate()
	{
		this.CalcRate();
		string empty = string.Empty;
		return " " + Interaction.STR_TASK_RATE_START + (this.fCTThemModifierUs * this.fCTThemModifierTools * (1f - this.fCTThemModifierPenalty)).ToString("n1") + Interaction.STR_TASK_RATE_END;
	}

	private void CalcRate()
	{
		if (this.strActionGroup != "Work" || this.bCTThemModifierCalculated)
		{
			return;
		}
		this.fCTThemModifierUs = 1f;
		if (this.strCTThemMultCondUs != null)
		{
			this.fCTThemModifierUs = (float)this.objUs.GetCondAmount(this.strCTThemMultCondUs);
		}
		this.fCTThemModifierUs = Mathf.Clamp(this.fCTThemModifierUs, 1f, 10f);
		this.fCTThemModifierTools = 1f;
		if (this.strCTThemMultCondTools != null)
		{
			this.fCTThemModifierTools = 0f;
			if (this.aLootItemUseContract != null)
			{
				foreach (CondOwner condOwner in this.aLootItemUseContract)
				{
					if (condOwner != null && this.strCTThemMultCondTools != null)
					{
						this.fCTThemModifierTools += (float)condOwner.GetCondAmount(this.strCTThemMultCondTools);
					}
				}
			}
		}
		this.fCTThemModifierPenalty = (float)this.objUs.GetCondAmount("StatWorkSpeedPenalty");
		if ((double)this.fCTThemModifierPenalty > 0.99)
		{
			this.fCTThemModifierPenalty = 0.99f;
		}
		this.bCTThemModifierCalculated = true;
	}

	public int GetAnim(CondOwner co = null)
	{
		int result = Interaction.dictAnims["Idle"];
		if (co != null && co.gameObject.activeInHierarchy)
		{
			result = co.GetAnimState();
		}
		if (this.strAnim != null && Interaction.dictAnims.ContainsKey(this.strAnim))
		{
			result = Interaction.dictAnims[this.strAnim];
		}
		else if (co != null && co.strIdleAnim != null && Interaction.dictAnims.ContainsKey(co.strIdleAnim))
		{
			result = Interaction.dictAnims[co.strIdleAnim];
		}
		return result;
	}

	public void Teleport(Pathfinder pfUs, bool isCancelIa = false)
	{
		Vector3 vector = this.objThem.GetPos(this.strTeleport, false);
		if (isCancelIa)
		{
			float num = Vector3.Distance(vector, this.objUs.tf.position);
			float num2 = 1.41f;
			float num3 = num2 * this.fTargetPointRange;
			if (num > num3 && num > num2)
			{
				return;
			}
		}
		vector.z = this.objUs.tf.position.z;
		this.objUs.tf.position = vector;
		if (pfUs != null)
		{
			pfUs.ReacquireTILCurrent();
			pfUs.tilDest = null;
			pfUs.UpdateManual();
		}
		vector = this.objThem.tf.rotation.eulerAngles;
		vector.z += this.fRotation;
		this.objUs.tf.eulerAngles = vector;
	}

	public Interaction Destroy()
	{
		DataHandler.ReleaseTrackedInteraction(this);
		this.strName = null;
		this.strTitle = null;
		this.strDesc = null;
		this.strTargetPoint = null;
		this.strAnim = null;
		this.strAnimTrig = null;
		this.strBubble = null;
		this.strColor = null;
		this.strDuty = null;
		this.strRaiseUI = null;
		this.strRaiseUIThem = null;
		this.strSubUI = null;
		this.strLedgerDef = null;
		this.strLootContextUs = null;
		this.strLootContextThem = null;
		this.strImage = null;
		this.strMapIcon = null;
		this.aInverse = null;
		this.PSpecTestThem = null;
		this.PSpecTest3rd = null;
		this.strLootItmAddUs = null;
		this.strLootItmAddThem = null;
		this.strLootCTsRemoveUs = null;
		this.strLootItmRemoveThem = null;
		this.strLootCTsGive = null;
		this.strLootCTsUse = null;
		this.strLootCTsLacks = null;
		this.strLootCTsTake = null;
		this.strTeleport = null;
		this.teleportRegIDTarget = null;
		if (this.aLootItemGiveContract != null)
		{
			this.aLootItemGiveContract.Clear();
			this.aLootItemGiveContract = null;
		}
		if (this.aLootItemUseContract != null)
		{
			this.aLootItemUseContract.Clear();
			this.aLootItemUseContract = null;
		}
		if (this.aLootItemRemoveContract != null)
		{
			this.aLootItemRemoveContract.Clear();
			this.aLootItemRemoveContract = null;
		}
		if (this.aLootItemTakeContract != null)
		{
			this.aLootItemTakeContract.Clear();
			this.aLootItemTakeContract = null;
		}
		if (this.aSeekItemsForContract != null)
		{
			this.aSeekItemsForContract.Clear();
			this.aSeekItemsForContract = null;
		}
		if (this.aDependents != null)
		{
			this.aDependents.Clear();
			this.aDependents = null;
		}
		this.objUs = null;
		this.objThem = null;
		return null;
	}

	public JsonInteractionSave GetJSONSave()
	{
		if (this.objThem == null && string.IsNullOrEmpty(this.strThemID))
		{
			return null;
		}
		JsonInteractionSave jsonInteractionSave = new JsonInteractionSave(this);
		if (this.objThem != null && this.objThem.HasCond("IsTile"))
		{
			List<CondOwner> list = new List<CondOwner>();
			CondTrigger ct = new CondTrigger("IsFloor", new string[]
			{
				"IsFloorGrate"
			}, null, null, null);
			this.objThem.ship.GetCOsAtWorldCoords1(this.objThem.tf.position, ct, true, true, list);
			if (list.Count > 0)
			{
				jsonInteractionSave.objThem = list[0].strID;
			}
			else
			{
				jsonInteractionSave.objThem = this.objUs.strID;
			}
			list.Clear();
		}
		else
		{
			jsonInteractionSave.objThem = this.strThemID;
		}
		jsonInteractionSave.obj3rd = this.str3rdID;
		List<string> list2 = new List<string>();
		if (this.aLootItemGiveContract != null)
		{
			foreach (CondOwner condOwner in this.aLootItemGiveContract)
			{
				list2.Add(condOwner.strID);
			}
			jsonInteractionSave.aLootItemGiveContract = list2.ToArray();
			list2.Clear();
		}
		if (this.aLootItemRemoveContract != null)
		{
			foreach (CondOwner condOwner2 in this.aLootItemRemoveContract)
			{
				list2.Add(condOwner2.strID);
			}
			jsonInteractionSave.aLootItemRemoveContract = list2.ToArray();
			list2.Clear();
		}
		if (this.aLootItemTakeContract != null)
		{
			foreach (CondOwner condOwner3 in this.aLootItemTakeContract)
			{
				list2.Add(condOwner3.strID);
			}
			jsonInteractionSave.aLootItemTakeContract = list2.ToArray();
			list2.Clear();
		}
		if (this.aSeekItemsForContract != null)
		{
			foreach (CondOwner condOwner4 in this.aSeekItemsForContract)
			{
				list2.Add(condOwner4.strID);
			}
			jsonInteractionSave.aSeekItemsForContract = list2.ToArray();
			list2.Clear();
		}
		if (this.aDependents != null)
		{
			foreach (string item in this.aDependents)
			{
				list2.Add(item);
			}
			jsonInteractionSave.aDependents = list2.ToArray();
			list2.Clear();
		}
		if (this.aSocialPrereqsFound != null)
		{
			foreach (string item2 in this.aSocialPrereqsFound)
			{
				list2.Add(item2);
			}
			jsonInteractionSave.aSocialPrereqsFound = list2.ToArray();
			list2.Clear();
		}
		list2.Clear();
		return jsonInteractionSave;
	}

	public override string ToString()
	{
		if (this.strName != null)
		{
			string text = this.strName + ": ";
			if (this.objUs != null)
			{
				text += this.objUs.strName;
			}
			else
			{
				text += "null";
			}
			text += "->";
			if (this.objThem != null)
			{
				text += this.objThem.strName;
			}
			else
			{
				text += "null";
			}
			return text;
		}
		return string.Empty;
	}

	public string FailReasons(bool bUsThem, bool bItems, bool bDebug)
	{
		StringBuilder stringBuilder = new StringBuilder();
		if (this.mapFails == null || this.mapFails["main"] == null || this.mapFails["main"].Count == 0)
		{
			return string.Empty;
		}
		if (this.mapFails.ContainsKey("main"))
		{
			foreach (string value in this.mapFails["main"])
			{
				stringBuilder.Append(value);
				stringBuilder.Append(" ");
			}
		}
		if (bUsThem)
		{
			if (this.mapFails.ContainsKey("us"))
			{
				foreach (string value2 in this.mapFails["us"])
				{
					stringBuilder.Append(" We are ");
					stringBuilder.Append(value2);
				}
			}
			if (this.mapFails.ContainsKey("them"))
			{
				foreach (string value3 in this.mapFails["them"])
				{
					stringBuilder.Append(" Target is ");
					stringBuilder.Append(value3);
				}
			}
			if (this.mapFails.ContainsKey("room"))
			{
				foreach (string value4 in this.mapFails["room"])
				{
					stringBuilder.Append(" Room is ");
					stringBuilder.Append(value4);
				}
			}
			if (this.mapFails.ContainsKey("3rd"))
			{
				foreach (string value5 in this.mapFails["3rd"])
				{
					stringBuilder.Append(" 3rd party is ");
					stringBuilder.Append(value5);
				}
			}
		}
		if (bItems)
		{
			if (this.mapFails.ContainsKey("items"))
			{
				List<string> list = new List<string>();
				List<int> list2 = new List<int>();
				foreach (string item in this.mapFails["items"])
				{
					int num = list.IndexOf(item);
					if (num < 0)
					{
						list.Add(item);
						list2.Add(1);
					}
					else
					{
						List<int> list3;
						int index;
						(list3 = list2)[index = num] = list3[index] + 1;
					}
				}
				for (int i = 0; i < list.Count; i++)
				{
					int num2 = list2[i];
					string value6 = list[i];
					if (num2 > 1)
					{
						stringBuilder.Append(" Missing item x");
						stringBuilder.Append(num2);
						stringBuilder.Append(": ");
						stringBuilder.Append(value6);
					}
					else
					{
						stringBuilder.Append(" Missing item: ");
						stringBuilder.Append(value6);
					}
				}
			}
			if (this.mapFails.ContainsKey("specs"))
			{
				List<string> list4 = new List<string>();
				List<int> list5 = new List<int>();
				foreach (string item2 in this.mapFails["specs"])
				{
					int num3 = list4.IndexOf(item2);
					if (num3 < 0)
					{
						list4.Add(item2);
						list5.Add(1);
					}
					else
					{
						List<int> list3;
						int index2;
						(list3 = list5)[index2 = num3] = list3[index2] + 1;
					}
				}
				for (int j = 0; j < list4.Count; j++)
				{
					int num4 = list5[j];
					string value7 = list4[j];
					if (num4 > 1)
					{
						stringBuilder.Append(" Item present but, ");
						stringBuilder.Append(num4);
						stringBuilder.Append(" x Not Enough: ");
						stringBuilder.Append(value7);
					}
					else
					{
						stringBuilder.Append(" Item present but, Not Enough: ");
						stringBuilder.Append(value7);
					}
				}
			}
		}
		if (bDebug)
		{
			if (this.mapFails.ContainsKey("debugus"))
			{
				foreach (string value8 in this.mapFails["debugus"])
				{
					stringBuilder.AppendLine();
					stringBuilder.Append(" - DEBUG US: ");
					stringBuilder.Append(value8);
				}
			}
			if (this.mapFails.ContainsKey("debugthem"))
			{
				foreach (string value9 in this.mapFails["debugthem"])
				{
					stringBuilder.AppendLine();
					stringBuilder.Append(" - DEBUG THEM: ");
					stringBuilder.Append(value9);
				}
			}
		}
		return stringBuilder.ToString();
	}

	public CondOwner objUs
	{
		get
		{
			if ((this.objUsTemp == null || this.objUsTemp.tf == null || this.objUsTemp.ship == null) && this.strUsID != null)
			{
				DataHandler.mapCOs.TryGetValue(this.strUsID, out this.objUsTemp);
			}
			return this.objUsTemp;
		}
		set
		{
			if (value == null)
			{
				return;
			}
			this.strUsID = value.strID;
			this.objUsTemp = value;
		}
	}

	public CondOwner objThem
	{
		get
		{
			if ((this.objThemTemp == null || this.objThemTemp.tf == null || this.objThemTemp.ship == null) && this.strThemID != null)
			{
				DataHandler.mapCOs.TryGetValue(this.strThemID, out this.objThemTemp);
			}
			return this.objThemTemp;
		}
		set
		{
			if (value == null)
			{
				return;
			}
			this.strThemID = value.strID;
			this.objThemTemp = value;
		}
	}

	public CondOwner obj3rd
	{
		get
		{
			if ((this.obj3rdTemp == null || this.obj3rdTemp.tf == null || this.obj3rdTemp.ship == null) && this.str3rdID != null)
			{
				DataHandler.mapCOs.TryGetValue(this.str3rdID, out this.obj3rdTemp);
			}
			return this.obj3rdTemp;
		}
		set
		{
			if (value == null)
			{
				return;
			}
			this.str3rdID = value.strID;
			this.obj3rdTemp = value;
		}
	}

	public Dictionary<string, List<InflectedTokenData>> stringsToTokens { get; set; }

	public readonly Guid id;

	private static readonly string[] _aDefault = new string[0];

	public string strName;

	public string strTitle;

	public string strDesc;

	public string strTooltip;

	public string strTargetPoint;

	public float fTargetPointRange;

	public float fForcedChance;

	public string strAnim;

	public string strAnimTrig;

	public string strBubble;

	public string strColor;

	public string strDuty;

	public string strChainStart;

	public string strThemType;

	public string strRaiseUI;

	public string strRaiseUIThem;

	public string strSubUI;

	public string strLedgerDef;

	public string strLootContextUs;

	public string strLootContextThem;

	public string strImage;

	public string strMapIcon;

	public string strIdleAnim;

	public string strCancelInteraction;

	public string strUseCase;

	public JsonAttackMode attackMode;

	public string strAttackerName;

	public string strIAHit;

	public string strIAMiss;

	public string strActionGroup;

	public int nQabOrderPriority;

	public string strChainOwner;

	public double fDuration;

	public double fDurationOrig;

	public double fEpochAdded;

	public float fRotation;

	public string strTeleport;

	private Tuple<string, string> teleportRegIDTarget;

	public string[] aInverse;

	private string[] aLoSReactions;

	public string[] strVerbs;

	public List<InflectedTokenData> aReplacements;

	private string[] aAModesAddedUs;

	private string[] aAModesAddedThem;

	private string[] aGPMChangesUs;

	private string[] aGPMChangesThem;

	private string[] aCustomInfos;

	public Loot LootCTsUs;

	public Loot LootCTsThem;

	public Loot LootCTs3rd;

	public Loot LootCondsUs;

	public Loot LootCondsThem;

	public Loot LootConds3rd;

	public CondTrigger CTTestUs;

	public CondTrigger CTTestThem;

	public CondTrigger CTTestRoom;

	public CondTrigger CTTest3rd;

	public JsonPersonSpec PSpecTestThem;

	public JsonPersonSpec PSpecTest3rd;

	public JsonShipSpec ShipTestUs;

	public JsonShipSpec ShipTestThem;

	public JsonShipSpec ShipTest3rd;

	public string strLootItmAddUs;

	public string strLootItmAddThem;

	public string strLootCTsRemoveUs;

	public string strLootItmRemoveThem;

	public string strLootCTsGive;

	public string strLootCTsUse;

	public string strLootCTsTake;

	public string strLootCTsLacks;

	public string strLootItmInputs;

	public string strCTThemMultCondUs;

	public string strCTThemMultCondTools;

	public Loot objLootModeSwitch;

	public Loot objLootModeSwitchThem;

	public Loot LootReveals;

	public List<CondOwner> aLootItemGiveContract;

	public List<CondOwner> aLootItemUseContract;

	public List<CondOwner> aLootItemRemoveContract;

	public List<CondOwner> aLootItemTakeContract;

	public List<CondOwner> aSeekItemsForContract;

	public List<string> aDependents;

	public List<string> aSocialChangelog;

	public JsonInteractionSave jis;

	public List<CondScore> aCondUsPriorities;

	private Dictionary<string, List<string>> mapFails;

	public bool bTargetOwned;

	public bool bEquip;

	public bool bLot;

	public bool bPassThrough;

	public bool bRecheckAllPlots;

	public bool bRecheckThisPlot;

	public bool b3rdReset;

	public string strPlot;

	public string strStartInstall;

	public string strPledgeAdd;

	public string strPledgeAddThem;

	public string strMusic;

	public bool bForceMusic;

	public Interaction.Logging nLogging;

	public Interaction.MoveType nMoveType;

	public string strCrime;

	public string strFactionTest = "ALWAYS";

	public float fFactionScoreChangeThem;

	public float fFactionScoreChangeUs;

	public Loot LootAddFactionsUs;

	public Loot LootAddFactionsThem;

	public Loot LootAddCondRulesUs;

	public Loot LootAddCondRulesThem;

	public string strLootRELChangeThemSeesUs;

	public string strLootRELChangeThemSees3rd;

	public string strLootRELChangeUsSeesThem;

	public string strLootRELChangeUsSees3rd;

	public string strLootRELChange3rdSeesThem;

	public string strLootRELChange3rdSeesUs;

	private CondOwner objUsTemp;

	private CondOwner objThemTemp;

	private CondOwner obj3rdTemp;

	private Vector2 ptRef;

	private string strUsID;

	private string strThemID;

	private string str3rdID;

	public bool bPause;

	public bool bSocial;

	public bool bImmediateReply;

	public bool bIgnoreFeelings;

	public bool bIgnoreCancel;

	public bool bRandomInverse;

	public bool bOpener;

	public bool bGamit;

	public bool bCloser;

	public bool bLogged;

	public bool bRaisedUI;

	public bool bUsePDA;

	public bool bTryWalk;

	public bool bGetItemBefore;

	public bool bDestroyItem = true;

	public bool bHumanOnly;

	public bool bAIOnly;

	public bool bGiveWholeStack;

	public bool bRemoveWholeStack;

	public bool bModeSwitchCheckFit;

	public bool bNoWait;

	public bool bNoWalk;

	public bool bVerboseTrigger;

	public bool bHardCode;

	public bool bInterrupt;

	public bool bCancel;

	public bool bRetestItems;

	public bool bManual;

	public bool bAirlockBlocked;

	public bool bApplyChain;

	public string strSocialCombatPreview;

	private float fCTThemModifierUs = 1f;

	private float fCTThemModifierTools = 1f;

	private float fCTThemModifierPenalty;

	private bool bCTThemModifierCalculated;

	private static CondTrigger ctNotCarried = null;

	public static readonly string TARGET_SELF = "Self";

	public static readonly string TARGET_OTHER = "Other";

	public static readonly string POINT_REMOTE = "remote";

	private static string STR_COMBAT_MISSED = null;

	private static string STR_TASK_RATE_START = null;

	private static string STR_TASK_RATE_END = null;

	private static string STR_IA_FAIL_DEFAULT = null;

	private static string STR_IA_FAIL_FACTION = null;

	private static string STR_IA_FAIL_LOS_END = null;

	private static string STR_IA_FAIL_LOS_START = null;

	private static string STR_IA_FAIL_MONEY = null;

	private static string STR_IA_FAIL_NO_EQUIP = null;

	private static string STR_IA_FAIL_INV_SPACE = null;

	private static string STR_IA_FAIL_OWNED_US = null;

	private static string STR_IA_FAIL_PATH_END = null;

	private static string STR_IA_FAIL_PATH_START = null;

	private static string STR_IA_FAIL_PLAYER = null;

	private static string STR_IA_FAIL_AI = null;

	private static string STR_IA_FAIL_RANGE_END = null;

	private static string STR_IA_FAIL_RANGE_START = null;

	private static string STR_IA_FAIL_THEM = null;

	private static string STR_IA_FAIL_US = null;

	private static string STR_ERROR_NO_ROOM_INV = null;

	public static string STR_GUI_REFUEL_PORT_SUFFIX = null;

	public static string STR_GUI_FINANCE_LOG_RECEIVED = null;

	public const string FT_ALWAYS = "ALWAYS";

	public const string FT_DIFFERENT = "DIFFERENT";

	public const string FT_SAME = "SAME";

	public const string FT_LIKES = "LIKES";

	public const string FT_DISLIKES = "DISLIKES";

	public static readonly Dictionary<string, int> dictAnims = new Dictionary<string, int>
	{
		{
			"Idle",
			0
		},
		{
			"Walk",
			1
		},
		{
			"Use",
			2
		},
		{
			"Dead",
			3
		},
		{
			"Sitting",
			4
		},
		{
			"Defecating",
			5
		},
		{
			"Tooling",
			6
		},
		{
			"Sleeping",
			7
		},
		{
			"Talk",
			8
		},
		{
			"Angry",
			9
		},
		{
			"Yes",
			10
		},
		{
			"No",
			11
		},
		{
			"Tablet",
			12
		},
		{
			"Burpee",
			13
		},
		{
			"Clapping",
			14
		},
		{
			"Bash",
			15
		},
		{
			"Spaced1",
			16
		},
		{
			"HypoxicSpaced",
			17
		},
		{
			"Bartending",
			18
		},
		{
			"Pull",
			19
		},
		{
			"Pull_Single",
			20
		},
		{
			"TakeHit",
			21
		},
		{
			"ShootPistol",
			22
		},
		{
			"Surrender",
			23
		},
		{
			"Punch",
			24
		},
		{
			"Block",
			25
		},
		{
			"Dodge",
			26
		},
		{
			"Fallen",
			27
		},
		{
			"Squat",
			28
		}
	};

	public enum Logging
	{
		NONE,
		GROUP,
		ROOM,
		SHIP
	}

	public enum MoveType
	{
		DEFAULT,
		GAMBIT,
		GAMBIT_FAIL,
		GAMBIT_PASS,
		SOCIAL_CORE,
		STAKES,
		COMMAND,
		GIG
	}
}
