using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Ostranauts;
using Ostranauts.COCommands;
using Ostranauts.Components;
using Ostranauts.Condowner;
using Ostranauts.Core;
using Ostranauts.Core.Models;
using Ostranauts.Core.Tutorials;
using Ostranauts.Events;
using Ostranauts.ShipGUIs;
using Ostranauts.Ships;
using Ostranauts.TargetVisualization;
using Ostranauts.Tools.ExtensionMethods;
using Ostranauts.Trading;
using Ostranauts.UI.MegaToolTip;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

// Core runtime entity wrapper for anything that can hold Conditions.
// In Ostranauts terms this covers items, crew, ships, rooms, and nested
// sub-objects. It is the live counterpart to JsonCondOwner/JsonCondOwnerSave
// and acts as the main bridge between data definitions and Unity components.
public class CondOwner : MonoBehaviour
{
	// These lazily cache common companion components attached to the same GameObject.
	public Item Item
	{
		get
		{
			if (!this.bTriedGettingItemRef && this._itemComponentReference == null)
			{
				this._itemComponentReference = base.GetComponent<Item>();
				this.bTriedGettingItemRef = true;
			}
			return this._itemComponentReference;
		}
	}

	public Pathfinder Pathfinder
	{
		get
		{
			if (!this.bTriedGettingPfRef && this._pfComponentReference == null)
			{
				this._pfComponentReference = base.GetComponent<Pathfinder>();
				this.bTriedGettingPfRef = true;
			}
			return this._pfComponentReference;
		}
	}

	public Crew Crew
	{
		get
		{
			if (!this.bTriedGettingCrewRef && this._crewComponentReference == null)
			{
				this._crewComponentReference = base.GetComponent<Crew>();
				this.bTriedGettingCrewRef = true;
			}
			return this._crewComponentReference;
		}
	}

	public GasContainer GasContainer
	{
		get
		{
			if (this._gasContainerComponentReference == null)
			{
				this._gasContainerComponentReference = base.GetComponent<GasContainer>();
			}
			return this._gasContainerComponentReference;
		}
	}

	public string strType { get; private set; }

	public Powered Pwr
	{
		get
		{
			return this.pwr;
		}
	}

	public Electrical Electrical
	{
		get
		{
			return this.elec;
		}
	}

	public Vector2 tfVector2Position
	{
		get
		{
			return this.tf.position.ToVector2();
		}
	}

	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
	public event AddCondEventHandler OnAddCond;

	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
	public event RemoveCondEventHandler OnRemoveCond;

	// Unity lifecycle setup for the live CondOwner state container.
	// Likely called when a spawned item/crew/room object is created or restored,
	// before SetData applies JSON definitions and save payloads.
	private void Awake()
	{
		if (CondOwner.UpdateWaitingReplies == null)
		{
			CondOwner.UpdateWaitingReplies = new OnUpdateWaitingRepliesEvent();
		}
		this.debugStop = false;
		this.tf = base.transform;
		this.fLastICOUpdate = StarSystem.fEpoch;
		this.fLastCleanup = StarSystem.fEpoch;
		this.fMSRedamageAmount = 0.0;
		this.aCondsTimed = new List<Condition>();
		this.aCondsTemp = new List<Condition>();
		this.aManUpdates = new List<IManUpdater>();
		this.mapConds = new Dictionary<string, Condition>();
		this.mapIAHist = new Dictionary<string, CondHistory>();
		this.mapPoints = new Dictionary<string, Vector2>();
		this.aInteractions = new List<string>();
		this.aMyShips = new List<string>();
		this.aFactions = new List<string>();
		this.mapGUIPropMaps = new Dictionary<string, Dictionary<string, string>>();
		this.mapGUIRefs = new Dictionary<string, IGUIHarness>();
		this.mapCondRules = new Dictionary<string, CondRule>();
		this.aQueue = new List<Interaction>();
		this.aReplies = new List<ReplyThread>();
		this.aAttackIAs = new List<string>();
		this.aPriorities = new List<Priority>();
		this.hashCondsImportant = new HashSet<string>();
		this.aMessages = new List<JsonLogMessage>();
		this.aCondZeroes = new List<string>();
		this.aStack = new TrackingCollection<CondOwner>(delegate(bool any)
		{
			this._hasSubCOs = any;
		});
		this.aLot = new TrackingCollection<CondOwner>(delegate(bool any)
		{
			this._hasSubCOs = any;
		});
		this.dictRecentlyTried = new Dictionary<string, double>();
		this.dictRememberScores = new Dictionary<string, double>();
		this.dictPledges = new Dictionary<int, List<Pledge2>>();
		this.aRememberIAs = new List<string>();
		this.vTextOffset = new Vector3(0f, -0.5f, 0f);
		this.vBblOffset = new Vector3(0f, 1.5f, 0f);
		this.anim = base.GetComponentInChildren<Animator>();
		this.mr = base.GetComponent<MeshRenderer>();
		this.mc = base.GetComponent<BoxCollider>();
		this.bAlive = true;
		this.bIgnoreKill = false;
		if (CondOwner.nAnimStateID < 0)
		{
			CondOwner.nAnimStateID = Animator.StringToHash("AnimState");
		}
		this.strIdleAnim = "Idle";
		this.strWalkAnim = "Walk";
		this.jsShiftLast = JsonCompany.NullShift;
		this.aTickers = new List<JsonTicker>();
		this.aMUs = new List<IManUpdater>();
		this.aMUDels = new List<IManUpdater>();
		this.aDestructableConds = new List<string>();
		if (CondOwner.selectionBracket == null)
		{
			CondOwner.selectionBracket = Resources.Load<GameObject>("prefabSelectBracket");
		}
		this.Selected = false;
		this.strPersistentCT = null;
		this.strPersistentCO = null;
		this.strSourceCO = null;
		this.strSourceInteract = null;
		this.mapInfo = new Dictionary<string, string>();
		Transform transform = this.tf.Find("progressBar");
		if (transform != null)
		{
			this.progressBar = transform.GetComponent<ProgressBar>();
		}
		if (this.anim)
		{
			CrewSim.COAnimators.Add(this.anim);
		}
		this._fieldsInitialized = true;
	}

	public static void CheckTrue(bool condition, string message)
	{
		if (!Application.isEditor || true)
		{
			return;
		}
		if (!condition)
		{
			UnityEngine.Debug.Log("ERROR: " + message);
			UnityEngine.Debug.Break();
		}
	}

	public bool ValidateParent()
	{
		if (!Application.isEditor || true)
		{
			return true;
		}
		if (this.bDestroyed)
		{
			CondOwner.CheckTrue(this.objCOParent == null, "Destroyed CO's can't have parents");
			CondOwner.CheckTrue(this.coStackHead == null, "Destroyed CO's can't be in stacks");
			CondOwner.CheckTrue(this.aStack == null, "Destroyed CO's can't be in stacks");
			CondOwner.CheckTrue(this.ship == null, "Destroyed CO's can't be on a ship");
			return false;
		}
		if (this.objCOParent)
		{
			CondOwner.CheckTrue(this.objCOParent != this, "CO cannot be its own parent");
		}
		if (this.slotNow == null)
		{
			if (this.coStackHead != null)
			{
				CondOwner.CheckTrue(this.coStackHead.aStack.IndexOf(this) >= 0, "Stacked CO not in coStackHead.aStack");
			}
			CondOwner.CheckTrue(this.aStack != null, "aStack is null");
			foreach (CondOwner condOwner in this.aStack)
			{
				CondOwner.CheckTrue(!condOwner.bDestroyed, "aStack contains a destroyed CO");
				CondOwner.CheckTrue(condOwner != null, "aStack contains a null");
				CondOwner.CheckTrue(condOwner.ValidateParent(), "aStack contains invalid COs");
			}
			foreach (CondOwner condOwner2 in this.aLot)
			{
				CondOwner.CheckTrue(!condOwner2.bDestroyed, "aLot contains a destroyed CO");
				CondOwner.CheckTrue(condOwner2 != null, "aLot contains a null");
				CondOwner.CheckTrue(condOwner2.ValidateParent(), "aLot contains invalid COs");
			}
			return true;
		}
		CondOwner condOwner3 = this.objCOParent;
		CondOwner.CheckTrue(condOwner3 != null, "Slotted but no parent");
		Slots compSlots = condOwner3.compSlots;
		CondOwner.CheckTrue(compSlots != null, "Slotted parent has no slots");
		int num = 0;
		int num2 = 1;
		foreach (Slot slot in condOwner3.GetSlots(true, Slots.SortOrder.HELD_FIRST))
		{
			JsonSlotEffects jsonSlotEffects = null;
			if (num2 == 1 && this.mapSlotEffects.TryGetValue(slot.strName, out jsonSlotEffects) && jsonSlotEffects.aSlotsSecondary != null)
			{
				num2 += jsonSlotEffects.aSlotsSecondary.Count<string>();
			}
			if (slot.aCOs != null)
			{
				foreach (CondOwner x in slot.aCOs)
				{
					if (x == this)
					{
						num++;
					}
				}
			}
		}
		if (num == num2)
		{
			return true;
		}
		CondOwner.CheckTrue(num < 2, "Slotted more than allowed");
		return false;
	}

	public void ValidateParentRecursive()
	{
	}

	public double fNextTickerSecs
	{
		get
		{
			if (this.aTickers == null)
			{
				return 0.0;
			}
			if (this.aTickers.Count > 0)
			{
				return this.aTickers[0].fTimeLeft * 3600.0;
			}
			return 0.0;
		}
	}

	public void UpdateManual(int maxRepeats = 10)
	{
		if (this.ship == null || this.ship.LoadState < Ship.Loaded.Edit)
		{
			return;
		}
		if (StarSystem.fEpoch - this.fLastCleanup > 2.0)
		{
			this.Cleanup();
		}
		if (this.aTickers.Count > 0)
		{
			this.temp_jt = this.aTickers[0];
			while (this.temp_jt != null && this.temp_jt.fTimeLeft <= 0.0)
			{
				bool flag = false;
				if (this.temp_jt.strCondLoot != null)
				{
					double fCoeff = 1.0;
					if (this.temp_jt.strCondLootCoeff != null)
					{
						fCoeff = this.GetCondAmount(this.temp_jt.strCondLootCoeff);
					}
					this.ParseCondLoot(this.temp_jt.strCondLoot, fCoeff);
					flag = true;
				}
				if (this.temp_jt.bQueue || this.temp_jt.strCondUpdate != null)
				{
					this.EndTurn();
					flag = true;
				}
				if (this.bDestroyed)
				{
					CrewSim.RemoveTicker(this);
					break;
				}
				if (flag)
				{
					this.aMUDels.Clear();
					this.aMUs.Clear();
					foreach (IManUpdater manUpdater in this.aManUpdates)
					{
						if (manUpdater == null)
						{
							this.aMUDels.Add(manUpdater);
						}
						else
						{
							this.aMUs.Add(manUpdater);
						}
					}
					foreach (IManUpdater manUpdater2 in this.aMUs)
					{
						if (manUpdater2 == null)
						{
							this.aMUDels.Add(manUpdater2);
						}
						else
						{
							manUpdater2.UpdateManual();
							if (this.bDestroyed)
							{
								CrewSim.RemoveTicker(this);
								break;
							}
						}
					}
					foreach (IManUpdater item in this.aMUDels)
					{
						this.aManUpdates.Remove(item);
					}
				}
				if (this.bDestroyed)
				{
					CrewSim.RemoveTicker(this);
					break;
				}
				this.aTickers.Remove(this.temp_jt);
				if (this.temp_jt.bRepeat)
				{
					this.temp_jt.SetTimeLeft(this.temp_jt.fTimeLeft + this.temp_jt.fPeriod);
					bool flag2 = false;
					if (!string.IsNullOrEmpty(this.temp_jt.strName))
					{
						this.temp_counterDict.Increment(this.temp_jt.strName);
						if (this.temp_counterDict[this.temp_jt.strName] > maxRepeats)
						{
							UnityEngine.Debug.Log(this.strName + " break UpdateManual()-While on Ticker " + this.temp_jt.strName + ", Too many retries this frame");
							CondOwner.nEndTurnsThisFrame = 0;
							this.temp_aTickersAside.Add(this.temp_jt);
							flag2 = true;
						}
					}
					if (!flag2)
					{
						this.AddTicker(this.temp_jt);
					}
				}
				if (this.aTickers.Count > 0)
				{
					this.temp_jt = this.aTickers[0];
				}
				else
				{
					this.temp_jt = null;
					CrewSim.RemoveTicker(this);
				}
			}
			foreach (JsonTicker jtNew in this.temp_aTickersAside)
			{
				this.AddTicker(jtNew);
			}
			this.temp_counterDict.Clear();
			this.temp_aTickersAside.Clear();
		}
		this.RefreshAnim();
		this.UpdateStats();
		if (this.Item != null)
		{
			this.Item.VisualizeOverlays(false);
		}
	}

	public void SetHighlight(float fAmount)
	{
		MaterialPropertyBlock materialPropertyBlock = new MaterialPropertyBlock();
		Crew component = base.GetComponent<Crew>();
		if (component != null)
		{
			component.SetHighlight(fAmount);
		}
		else if (this.mr != null)
		{
			this.mr.GetPropertyBlock(materialPropertyBlock);
			materialPropertyBlock.SetFloat("_Highlight", fAmount);
			this.mr.SetPropertyBlock(materialPropertyBlock);
			GUIInventoryItem inventoryItemFromCO = GUIInventory.GetInventoryItemFromCO(this);
			RawImage rawImage = null;
			if (inventoryItemFromCO != null)
			{
				rawImage = inventoryItemFromCO.GetComponent<RawImage>();
			}
			else if (GUIInventory.instance.PaperDollManager.mapCOIDsToGO.ContainsKey(this.strID))
			{
				rawImage = GUIInventory.instance.PaperDollManager.mapCOIDsToGO[this.strID].GetComponent<RawImage>();
			}
			if (rawImage != null)
			{
				rawImage.materialForRendering.SetFloat("_Highlight", fAmount);
			}
		}
	}

	public void RefreshAnim()
	{
		if (this.anim == null)
		{
			return;
		}
		if (this.bAlive && this.aQueue.Count > 0 && this.aQueue[0] != null && this.aQueue[0].strName != "Wait")
		{
			if (this.aQueue[0].strName == "Walk")
			{
				this.SetAnimState(Interaction.dictAnims[this.strWalkAnim]);
			}
			else
			{
				this.SetAnimState(this.aQueue[0].GetAnim(this));
			}
		}
		else
		{
			int animState = 0;
			Interaction.dictAnims.TryGetValue(this.strIdleAnim, out animState);
			this.SetAnimState(animState);
		}
	}

	private void Cleanup()
	{
		List<string> list = new List<string>(this.dictRecentlyTried.Keys);
		foreach (string key in list)
		{
			if (StarSystem.fEpoch - this.dictRecentlyTried[key] > 60.0)
			{
				this.dictRecentlyTried.Remove(key);
				break;
			}
		}
		bool flag = false;
		double num = double.PositiveInfinity;
		for (int i = 0; i < this.aTickers.Count; i++)
		{
			if (this.aTickers[i].bQueue)
			{
				num = this.aTickers[i].fTimeLeft;
			}
			if (i >= this.aTickers.Count - 1)
			{
				break;
			}
			if (this.aTickers[i].fTimeLeft - this.aTickers[i + 1].fTimeLeft >= 0.0002770000137388706)
			{
				flag = true;
			}
		}
		if (this.aQueue.Count > 0)
		{
			Interaction interaction = this.aQueue[0];
			if (interaction != null && interaction.fDuration < num)
			{
				JsonTicker jsonTicker = new JsonTicker();
				jsonTicker.bQueue = true;
				jsonTicker.strName = interaction.strName;
				jsonTicker.fPeriod = interaction.fDuration;
				jsonTicker.SetTimeLeft(jsonTicker.fPeriod);
				this.AddTicker(jsonTicker);
			}
			else if (interaction == null)
			{
				JsonTicker jsonTicker2 = new JsonTicker();
				jsonTicker2.bQueue = true;
				jsonTicker2.strName = "Cleanup found null interaction?";
				jsonTicker2.fPeriod = 0.0;
				jsonTicker2.SetTimeLeft(jsonTicker2.fPeriod);
				this.AddTicker(jsonTicker2);
			}
		}
		if (flag)
		{
			this.aTickers.Sort((JsonTicker x, JsonTicker y) => x.fTimeLeft.CompareTo(y.fTimeLeft));
		}
		if (this.aQueue.Count > 0 && (this.aQueue[0].strRaiseUI != null || this.aQueue[0].strRaiseUIThem != null) && this.aQueue[0].bRaisedUI && !CrewSim.bRaiseUI)
		{
			foreach (JsonTicker jsonTicker3 in this.aTickers)
			{
				if (jsonTicker3.strName == this.aQueue[0].strName && jsonTicker3.fTimeLeft > 0.1)
				{
					this.AICancelCurrent();
					break;
				}
			}
		}
		if (this.aQueue.Count > 0 && this.aQueue[0].strName == "Wait")
		{
			if (this.aQueue[0].objThem == null || this.aQueue[0].objThem.ship == null || this.aQueue[0].objThem.ship.LoadState <= Ship.Loaded.Shallow)
			{
				this.AICancelCurrent();
			}
			else if (!this.HasCond("IsPlayer") || !CrewSim.bRaiseUI)
			{
				bool flag2 = true;
				foreach (Interaction interaction2 in this.aQueue[0].objThem.aQueue)
				{
					if (interaction2.objThem == this)
					{
						flag2 = false;
						break;
					}
				}
				if (flag2)
				{
					this.AICancelCurrent();
				}
			}
		}
		if (this.HasCond("IsAIAgent"))
		{
			if (!this.HasCond("IsRobot") && (this.mapIAHist == null || !this.mapIAHist.ContainsKey("StatEsteem") || this.mapIAHist["StatEsteem"].mapInteractions == null || this.mapIAHist["StatEsteem"].mapInteractions.Count < 100))
			{
				this.SetCondAmount("DebugMemoryLoss", 1.0, 0.0);
			}
			else
			{
				this.ZeroCondAmount("DebugMemoryLoss");
			}
			if (!this.HasCond("IsSocialItemCleanupDone") && this.compSlots != null)
			{
				Slot slot = this.compSlots.GetSlot("social");
				if (slot != null && slot.GetOutermostCO() != null)
				{
					Dictionary<string, int> dictionary = new Dictionary<string, int>();
					List<CondOwner> cos = slot.GetOutermostCO().GetCOs(true, null);
					if (cos != null)
					{
						foreach (CondOwner condOwner in cos)
						{
							if (!(condOwner == null))
							{
								if (!dictionary.ContainsKey(condOwner.strName))
								{
									dictionary[condOwner.strName] = 1;
								}
								else if (dictionary[condOwner.strName] < 3)
								{
									Dictionary<string, int> dictionary2;
									string key2;
									(dictionary2 = dictionary)[key2 = condOwner.strName] = dictionary2[key2] + 1;
								}
								else
								{
									Dictionary<string, int> dictionary2;
									string key3;
									(dictionary2 = dictionary)[key3 = condOwner.strName] = dictionary2[key3] + 1;
									condOwner.RemoveFromCurrentHome(true);
									condOwner.Destroy();
								}
							}
						}
						cos.Clear();
					}
					dictionary.Clear();
				}
				double fAge = 1.0;
				Condition cond = DataHandler.GetCond("IsSocialItemCleanupDone");
				if (cond != null)
				{
					fAge = (double)cond.fDuration * MathUtils.Rand(0.1, 1.0, MathUtils.RandType.High, null);
				}
				this.AddCondAmount("IsSocialItemCleanupDone", 1.0, fAge, 0f);
			}
		}
		this.fLastCleanup = StarSystem.fEpoch;
	}

	public void Destroy()
	{
		this.ValidateParent();
		if (this.anim)
		{
			CrewSim.COAnimators.Remove(this.anim);
		}
		if (this.bDestroyed)
		{
			return;
		}
		if (this == null)
		{
			if (this == null)
			{
				UnityEngine.Debug.Log("ERROR: Destroying a null");
			}
			else
			{
				UnityEngine.Debug.Log("ERROR: Destroying a null " + this.strName);
			}
			UnityEngine.Debug.Break();
			return;
		}
		if (this.elec != null)
		{
			this.elec.CleanUp(this.elec.needsCleanup);
		}
		this.Company = null;
		this.jsShiftLast = null;
		if (this.Pathfinder != null)
		{
			this.Pathfinder.Destroy();
		}
		if (this.progressBar != null)
		{
			this.progressBar.Destroy();
		}
		List<CondOwner> list = new List<CondOwner>(this.aStack);
		foreach (CondOwner condOwner in list)
		{
			condOwner.Destroy();
		}
		list = new List<CondOwner>(this.aLot);
		foreach (CondOwner condOwner2 in this.aLot)
		{
			condOwner2.Destroy();
		}
		if (this.objContainer != null)
		{
			this.objContainer.Destroy();
			this.objContainer = null;
		}
		if (this._compSlots != null)
		{
			this._compSlots.Destroy();
			this._compSlots = null;
		}
		if (CrewSim.objInstance != null)
		{
			CrewSim.objInstance.coDicts.RemoveCO(this);
			if (CrewSim.GetBracketTarget() == this)
			{
				CrewSim.objInstance.SetBracketTarget(null, false, false);
			}
			try
			{
				CrewSim.objInstance.ShowBlocksAndLights(this, false);
			}
			catch (Exception ex)
			{
				UnityEngine.Debug.Log("ERROR: " + this.ToString() + " \n" + ex.ToString());
			}
		}
		if (this.strID != null && DataHandler.mapCOs.ContainsKey(this.strID) && DataHandler.mapCOs[this.strID] == this)
		{
			DataHandler.mapCOs.Remove(this.strID);
		}
		foreach (Condition condition in this.mapConds.Values)
		{
			condition.Destroy();
		}
		this.mapConds.Clear();
		this.mapConds = null;
		foreach (CondHistory condHistory in this.mapIAHist.Values)
		{
			condHistory.Destroy();
		}
		this.mapIAHist.Clear();
		this.mapIAHist = null;
		this.mapPoints.Clear();
		this.mapPoints = null;
		this.aInteractions.Clear();
		this.aInteractions = null;
		this.aMyShips.Clear();
		this.aMyShips = null;
		this.aFactions.Clear();
		this.aFactions = null;
		this.mapGUIPropMaps.Clear();
		this.mapGUIPropMaps = null;
		this.mapGUIRefs.Clear();
		this.mapGUIRefs = null;
		this.mapCondRules.Clear();
		this.mapCondRules = null;
		this.aMessages.Clear();
		this.aMessages = null;
		this.aCondZeroes.Clear();
		this.aCondZeroes = null;
		this.aStack.Clear();
		this.aStack = null;
		this.coStackHead = null;
		this.aLot.Clear();
		this.aLot = null;
		foreach (Interaction interaction in this.aQueue)
		{
			interaction.Destroy();
		}
		this.aQueue.Clear();
		this.aQueue = null;
		this.aReplies.Clear();
		this.aReplies = null;
		this.aAttackIAs.Clear();
		this.aAttackIAs = null;
		foreach (Priority priority in this.aPriorities)
		{
			priority.Destroy();
		}
		this.aPriorities.Clear();
		this.aPriorities = null;
		if (this.hashCondsImportant != null)
		{
			this.hashCondsImportant.Clear();
		}
		this.hashCondsImportant = null;
		this.dictRecentlyTried.Clear();
		this.dictRecentlyTried = null;
		this.aRememberIAs.Clear();
		this.aRememberIAs = null;
		this.dictRememberScores.Clear();
		this.dictRememberScores = null;
		this.anim = null;
		this.pwr = null;
		this.objCondID = null;
		this.objCOParent = null;
		this.ship = null;
		this.aCondsTimed.Clear();
		this.aCondsTimed = null;
		this.aCondsTemp.Clear();
		this.aCondsTemp = null;
		this.aTickers.Clear();
		this.aTickers = null;
		this.aDestructableConds.Clear();
		this.aDestructableConds = null;
		if (this.goBracket != null)
		{
			UnityEngine.Object.Destroy(this.goBracket);
		}
		this.goBracket = null;
		this.tf = null;
		this.dictAddCondEvents.Clear();
		this.dictAddCondEvents = null;
		this.dictRemoveCondEvents.Clear();
		this.dictRemoveCondEvents = null;
		this.bDestroyed = true;
		UnityEngine.Object.DestroyImmediate(base.gameObject);
		this.ValidateParent();
	}

	public void FallAway()
	{
		if (this.HasCond("IsFallingAway"))
		{
			return;
		}
		this.LogMessage(this.strName + DataHandler.GetString("OBJV_GRAV_LOSS", false), "SignalRed", this.strID);
		this.SetCondAmount("IsFallingAway", 1.0, 0.0);
		base.StartCoroutine(this._FallAway());
	}

	private IEnumerator _FallAway()
	{
		double timeStart = StarSystem.fEpoch;
		double timeElapsed = 0.0;
		double timeDiff = 0.0;
		while (timeElapsed < 5.0)
		{
			timeDiff = StarSystem.fEpoch - timeStart - timeElapsed;
			timeElapsed += timeDiff;
			this.tf.Rotate(0f, 0f, (float)timeDiff);
			this.tf.localScale = Vector3.one / (1f + (float)timeElapsed);
			Tile til = this.ship.GetTileAtWorldCoords1(this.tf.position.x, this.tf.position.y, true, true);
			if (til != null && !til.IsEvaTileWithGravitation())
			{
				if (this.Item == null)
				{
					this.tf.localScale = Vector3.one;
				}
				else
				{
					this.Item.ResetTransforms(this.tf.position.x, this.tf.position.y);
				}
				this.ZeroCondAmount("IsFallingAway");
				yield break;
			}
			yield return null;
		}
		if (this.HasCond("IsPlayer"))
		{
			this.LogMessage(this.strName + DataHandler.GetString("OBJV_GRAV_LOST", false), "SignalRed", this.strID);
			CanvasManager.instance.GameOver(this);
		}
		else
		{
			this.Kill = true;
			yield return null;
			this.RemoveFromCurrentHome(true);
			this.Destroy();
		}
		yield break;
	}

	public void CheckInteractionFlag()
	{
		this.bCanUndamage = false;
		this.bCanRepair = false;
		this.bCanPatch = false;
		if (this.aInteractions == null || this.aInteractions.Count <= 0)
		{
			return;
		}
		foreach (string text in this.aInteractions)
		{
			if (text.Contains("Patch") && !text.Contains("Scrap"))
			{
				this.bCanPatch = true;
			}
			else if (text.Contains("Repair"))
			{
				this.bCanRepair = true;
			}
			else if (text.Contains("Undamage"))
			{
				this.bCanUndamage = true;
			}
		}
	}

	// Applies the CondOwner definition plus optional save-state onto the live object.
	// Data linkage: `jid` likely comes from data/condowners, while `jCOSIn` is the
	// per-instance save payload carrying live conditions, queues, and ownership.
	public void SetData(JsonCondOwner jid, bool bLoot = true, JsonCondOwnerSave jCOSIn = null)
	{
		if (jid == null)
		{
			return;
		}
		if (!this._fieldsInitialized)
		{
			this.Awake();
		}
		this.jCOS = jCOSIn;
		this.strName = jid.strName;
		this.strNameFriendly = jid.strNameFriendly;
		this.strNameShort = jid.strNameShort;
		if (jid.strNameShort != null)
		{
			this.strNameShortLCase = jid.strNameShort.ToLower();
		}
		this.strDesc = jid.strDesc;
		this.strCODef = jid.strCODef;
		if (this.strCODef == null)
		{
			this.strCODef = jid.strName;
		}
		this.strItemDef = jid.strItemDef;
		this.strPortraitImg = jid.strPortraitImg;
		this.nStackLimit = Mathf.Max(1, jid.nStackLimit);
		this.bSaveMessageLog = jid.bSaveMessageLog;
		this.bSlotLocked = jid.bSlotLocked;
		this.strType = jid.strType;
		if (jid.aInteractions != null)
		{
			this.aInteractions = new List<string>(jid.aInteractions);
		}
		else
		{
			this.aInteractions = new List<string>();
		}
		this.CheckInteractionFlag();
		if (jCOSIn != null)
		{
			this.strSourceInteract = jCOSIn.strSourceInteract;
			this.strSourceCO = jCOSIn.strSourceCO;
			this.strPersistentCT = jCOSIn.strPersistentCT;
			this.strPersistentCO = jCOSIn.strPersistentCO;
			this.strIdleAnim = jCOSIn.strIdleAnim;
			this.strLastSocial = jCOSIn.strLastSocial;
			this.pairInventoryXY = new PairXY(jCOSIn.inventoryX, jCOSIn.inventoryY);
		}
		else
		{
			this.strSourceInteract = null;
			this.strSourceCO = null;
			this.strPersistentCO = null;
			this.strPersistentCT = null;
			this.strLastSocial = null;
		}
		if (jid.aSlotsWeHave != null)
		{
			this.compSlots = base.gameObject.AddComponent<Slots>();
			foreach (string text in jid.aSlotsWeHave)
			{
				this.compSlots.AddSlot(DataHandler.GetSlot(text));
			}
		}
		if (jid.strContainerCT != null)
		{
			this.objContainer = base.gameObject.AddComponent<Container>();
			CondOwner co = this.objContainer.CO;
			this.objContainer.ctAllowed = DataHandler.GetCondTrigger(jid.strContainerCT);
			if (!CrewSim.bSaveUsesOldContainerGrids && jid.nContainerHeight != 0 && jid.nContainerWidth != 0)
			{
				this.objContainer.gridLayout = new GridLayout(jid.nContainerWidth, jid.nContainerHeight);
			}
		}
		this.bLogConds = false;
		string[] array = jid.aStartingCondRules;
		if (jCOSIn != null)
		{
			array = jCOSIn.aCondRules;
		}
		if (array != null)
		{
			if (array.Contains("DEFAULT"))
			{
				List<string> list = array.ToList<string>();
				list.Remove("DEFAULT");
				JsonCondOwner condOwnerDef = DataHandler.GetCondOwnerDef(this.strCODef);
				if (condOwnerDef != null && condOwnerDef.aStartingCondRules != null)
				{
					list.AddRange(condOwnerDef.aStartingCondRules);
				}
				array = list.ToArray();
			}
			bool flag = this.bIgnoreKill;
			if (jCOSIn == null)
			{
				this.bIgnoreKill = true;
			}
			foreach (string strCondRule in array)
			{
				this.AddCondRule(strCondRule, false);
			}
			this.bIgnoreKill = flag;
		}
		this.bFreezeCondRules = true;
		if (jid.aUpdateCommands != null)
		{
			foreach (string strDef in jid.aUpdateCommands)
			{
				this.AddCommand(strDef);
			}
		}
		if (this.nStackLimit > 1)
		{
			this.AddCondAmount("IsStacking", (double)(this.nStackLimit - 1), 0.0, 0f);
		}
		string[] array3 = jid.aStartingConds;
		bool flag2 = false;
		if (jCOSIn != null)
		{
			array3 = jCOSIn.aConds;
			flag2 = (jCOSIn.aCondReveals != null && array3.Length == 2 * jCOSIn.aCondReveals.Length);
		}
		if (array3 != null)
		{
			if (array3.Contains("DEFAULT"))
			{
				List<string> list2 = array3.ToList<string>();
				list2.Remove("DEFAULT");
				JsonCondOwner condOwnerDef2 = DataHandler.GetCondOwnerDef(this.strCODef);
				list2.AddRange(condOwnerDef2.aStartingConds);
				array3 = list2.ToArray();
			}
			for (int l = 0; l < array3.Length; l++)
			{
				string text2 = array3[l];
				string[] array4 = text2.Split(new char[]
				{
					'='
				});
				if (array4.Length > 1)
				{
					this.ZeroCondAmount(array4[0]);
				}
				string text3 = this.ParseCondEquation(text2, 1.0, 0f);
				Condition condition = null;
				if (text3 != null && this.mapConds != null && this.mapConds.TryGetValue(text3, out condition))
				{
					if (flag2)
					{
						condition.nDisplaySelf = jCOSIn.aCondReveals[2 * l];
						condition.nDisplayOther = jCOSIn.aCondReveals[2 * l + 1];
					}
					if (condition.bRemoveOnLoad)
					{
						this.ZeroCondAmount(text3);
					}
				}
			}
		}
		if (jCOSIn == null)
		{
			foreach (CondRule condRule in this.mapCondRules.Values)
			{
				this.AddCondRuleEffects(condRule, 1f);
				if (condRule.fPref != 0.0)
				{
					this.hashCondsImportant.Add(condRule.strCond);
				}
			}
			this.bFreezeCondRules = false;
		}
		this.bLogConds = true;
		if (jCOSIn != null)
		{
			this.bFreezeConds = true;
		}
		JsonTicker[] array5 = null;
		if (jCOSIn != null)
		{
			array5 = jCOSIn.aTickers;
			JsonTicker[] array6 = new JsonTicker[this.aTickers.Count];
			this.aTickers.CopyTo(array6);
			foreach (JsonTicker jtRemove in array6)
			{
				this.RemoveTicker(jtRemove);
			}
		}
		else if (jid.aTickers != null)
		{
			array5 = new JsonTicker[jid.aTickers.Length];
			for (int n = 0; n < jid.aTickers.Length; n++)
			{
				array5[n] = DataHandler.GetTicker(jid.aTickers[n]);
			}
		}
		if (array5 != null)
		{
			foreach (JsonTicker jsonTicker in array5)
			{
				if (jsonTicker != null)
				{
					jsonTicker.SetTimeLeft(jsonTicker.fTimeLeft);
					this.AddTicker(jsonTicker.Clone());
				}
			}
		}
		this.mapSlotEffects = new Dictionary<string, JsonSlotEffects>();
		if (jid.mapSlotEffects != null)
		{
			Dictionary<string, string> dictionary = DataHandler.ConvertStringArrayToDict(jid.mapSlotEffects, null);
			foreach (KeyValuePair<string, string> keyValuePair in dictionary)
			{
				JsonSlotEffects slotEffect = DataHandler.GetSlotEffect(keyValuePair.Value);
				if (slotEffect != null)
				{
					this.mapSlotEffects[keyValuePair.Key] = slotEffect;
					slotEffect.strSlotPrimary = keyValuePair.Key;
					if (slotEffect.aSlotsSecondary != null)
					{
						foreach (string key in slotEffect.aSlotsSecondary)
						{
							this.mapSlotEffects[key] = slotEffect;
						}
					}
				}
				else
				{
					UnityEngine.Debug.Log(string.Concat(new string[]
					{
						this.strName,
						" was unable to load sloteffect Key: ",
						keyValuePair.Key,
						" Value: ",
						keyValuePair.Value
					}));
				}
			}
		}
		this.mapChargeProfiles = new Dictionary<string, JsonChargeProfile>();
		if (jid.mapChargeProfiles != null)
		{
			Dictionary<string, string> dictionary2 = DataHandler.ConvertStringArrayToDict(jid.mapChargeProfiles, null);
			foreach (KeyValuePair<string, string> keyValuePair2 in dictionary2)
			{
				JsonChargeProfile chargeProfile = DataHandler.GetChargeProfile(keyValuePair2.Value);
				if (chargeProfile != null)
				{
					this.mapChargeProfiles[keyValuePair2.Key] = chargeProfile;
				}
			}
		}
		if (jid.mapAltItemDefs != null)
		{
			this.mapAltItemDefs = DataHandler.ConvertStringArrayToDict(jid.mapAltItemDefs, null);
		}
		else
		{
			this.mapAltItemDefs = new Dictionary<string, string>();
		}
		if (jid.mapAltSlotImgs != null)
		{
			this.mapAltSlotImgs = DataHandler.ConvertStringArrayToDict(jid.mapAltSlotImgs, null);
		}
		else
		{
			this.mapAltSlotImgs = new Dictionary<string, string>();
		}
		if (jid.dictSlotsLayout != null)
		{
			this.dictSlotsLayout = jid.dictSlotsLayout;
		}
		else
		{
			this.dictSlotsLayout = new Dictionary<string, Vector3>();
		}
		if (bLoot && jCOSIn == null)
		{
			Loot loot = DataHandler.GetLoot(jid.strLoot);
			List<CondOwner> coloot = loot.GetCOLoot(this, false, null);
			foreach (CondOwner condOwner in coloot)
			{
				if (this.compSlots != null && condOwner.mapSlotEffects.Keys.Count > 0)
				{
					foreach (string strSlot in condOwner.mapSlotEffects.Keys)
					{
						if (this.compSlots.SlotItem(strSlot, condOwner, true))
						{
							break;
						}
					}
				}
				else
				{
					CondOwner condOwner2 = this.AddCO(condOwner, true, true, true);
					if (condOwner2 != null)
					{
						condOwner2.Destroy();
					}
				}
			}
		}
		if (jid.mapPoints != null)
		{
			foreach (string text4 in jid.mapPoints)
			{
				string[] array10 = text4.Split(new char[]
				{
					','
				});
				float x = 0f;
				if (array10[1] == "9e99")
				{
					x = float.PositiveInfinity;
				}
				else
				{
					float.TryParse(array10[1], out x);
				}
				float y = 0f;
				if (array10[2] == "9e99")
				{
					y = float.PositiveInfinity;
				}
				else
				{
					float.TryParse(array10[2], out y);
				}
				this.mapPoints[array10[0]] = new Vector2(x, y);
			}
		}
		if (jid.jsonPI != null)
		{
			this.pwr = base.gameObject.AddComponent<Powered>();
			this.pwr.SetData(jid.jsonPI);
		}
		if (jid.mapGUIPropMaps != null)
		{
			Dictionary<string, string> dictionary3 = DataHandler.ConvertStringArrayToDict(jid.mapGUIPropMaps, null);
			foreach (string key2 in dictionary3.Keys)
			{
				this.mapGUIPropMaps[key2] = DataHandler.GetGUIPropMap(dictionary3[key2]);
			}
		}
		if (jid.aComponents != null)
		{
			foreach (string typeName in jid.aComponents)
			{
				Type type = Type.GetType(typeName);
				base.gameObject.AddComponent(type);
			}
		}
		if (jCOSIn != null)
		{
			this.bAlive = jCOSIn.bAlive;
			if (jid.bSaveMessageLog)
			{
				if (jCOSIn.aMsgColors != null)
				{
					int num5 = 0;
					while (num5 < jCOSIn.aMsgColors.Length && num5 < jCOSIn.aMessages.Length)
					{
						JsonLogMessage jsonLogMessage = new JsonLogMessage();
						jsonLogMessage.strName = Guid.NewGuid().ToString();
						jsonLogMessage.strMessage = jCOSIn.aMessages[num5];
						jsonLogMessage.strColor = jCOSIn.aMsgColors[num5];
						jsonLogMessage.strOwner = "n/a";
						if (jsonLogMessage.strMessage.IndexOf(jCOSIn.strID) == 0)
						{
							jsonLogMessage.strOwner = jCOSIn.strID;
						}
						jsonLogMessage.fTime = StarSystem.fEpoch - (double)jCOSIn.aMsgColors.Length + (double)num5;
						this.aMessages.Add(jsonLogMessage);
						num5++;
					}
				}
				if (jCOSIn.aMessages2 != null)
				{
					foreach (JsonLogMessage item in jCOSIn.aMessages2)
					{
						this.aMessages.Add(item);
					}
				}
			}
			if (jCOSIn.aCondZeroes != null)
			{
				foreach (string item2 in jCOSIn.aCondZeroes)
				{
					this.aCondZeroes.Add(item2);
				}
			}
			if (jCOSIn.mapDGasMols != null)
			{
				GasContainer gasContainer = this.GasContainer;
				if (gasContainer != null)
				{
					float num8 = 0f;
					gasContainer.fDGasTemp = jCOSIn.fDGasTemp;
					if (gasContainer.fDGasTemp == 0.0)
					{
						gasContainer.fDGasTemp = 0.001;
					}
					foreach (string text5 in jCOSIn.mapDGasMols)
					{
						string[] array12 = text5.Split(new char[]
						{
							','
						});
						float.TryParse(array12[1], out num8);
						gasContainer.mapDGasMols[array12[0]] = (double)num8;
					}
				}
			}
			this.fLastICOUpdate = jCOSIn.fLastICOUpdate;
			this.fMSRedamageAmount = jCOSIn.fMSRedamageAmount;
			if (jCOSIn.aQueue != null)
			{
				foreach (JsonInteractionSave jsonInteractionSave in jCOSIn.aQueue)
				{
					Interaction interaction = DataHandler.GetInteraction(jsonInteractionSave.strName, jsonInteractionSave, false);
					if (interaction == null)
					{
						UnityEngine.Debug.Log("Interaction " + jsonInteractionSave.strName + " is missing from the DataHandler. This should probably be a crash...");
					}
					else
					{
						this.aQueue.Add(interaction);
						if (interaction.strName == "Walk" && interaction.strTargetPoint == null)
						{
							interaction.strTargetPoint = "use";
						}
					}
				}
			}
			if (jCOSIn.aReplies != null)
			{
				foreach (ReplyThread replyThread in jCOSIn.aReplies)
				{
					ReplyThread replyThread2 = new ReplyThread();
					replyThread2.fEpoch = replyThread.fEpoch;
					replyThread2.jis = replyThread.jis;
					replyThread2.strID = replyThread.strID;
					this.aReplies.Add(replyThread2);
				}
			}
			if (jCOSIn.aAttackIAs != null && !CrewSim.objInstance.VersionIsOlderThan(CrewSim.aSaveVersion, new int[]
			{
				0,
				14,
				0,
				12
			}))
			{
				this.ApplyAModes(jCOSIn.aAttackIAs, false);
			}
			if (jCOSIn.dictRecentlyTried != null)
			{
				foreach (KeyValuePair<string, double> keyValuePair3 in jCOSIn.dictRecentlyTried)
				{
					this.dictRecentlyTried.Add(keyValuePair3.Key, keyValuePair3.Value);
				}
			}
			if (jCOSIn.dictRememberScores != null)
			{
				foreach (KeyValuePair<string, double> keyValuePair4 in jCOSIn.dictRememberScores)
				{
					this.dictRememberScores.Add(keyValuePair4.Key, keyValuePair4.Value);
				}
			}
			if (jCOSIn.aRememberIAs != null)
			{
				foreach (string item3 in jCOSIn.aRememberIAs)
				{
					this.aRememberIAs.Add(item3);
				}
			}
			if (jCOSIn.aMyShips != null)
			{
				foreach (string item4 in jCOSIn.aMyShips)
				{
					this.aMyShips.Add(item4);
				}
			}
			if (jCOSIn.aFactions != null)
			{
				foreach (string item5 in jCOSIn.aFactions)
				{
					this.aFactions.Add(item5);
				}
			}
			if (jCOSIn.mapIAHist2 != null)
			{
				this.mapIAHist = new Dictionary<string, CondHistory>();
				if (jCOSIn.mapIAHist2 != null)
				{
					this.mapIAHist = new Dictionary<string, CondHistory>();
					foreach (JsonCondHistory jsonCondHistory in jCOSIn.mapIAHist2)
					{
						if (!CrewSim.bSaveHasENCPoliceBoard || jsonCondHistory.strCondName.IndexOf("ENCPoliceBoard") != 0)
						{
							this.mapIAHist[jsonCondHistory.strCondName] = jsonCondHistory.GetData();
						}
					}
				}
			}
			if (jCOSIn.social != null)
			{
				this.socUs = base.gameObject.AddComponent<global::Social>();
				this.socUs.Init(jCOSIn.social);
			}
			bool flag3 = false;
			if (jCOSIn.cgs != null)
			{
				GUIChargenStack guichargenStack = base.gameObject.AddComponent<GUIChargenStack>();
				guichargenStack.Init(jCOSIn.cgs);
				JsonPersonSpec jsonPersonSpec = new JsonPersonSpec();
				jsonPersonSpec.bAlive = this.bAlive;
				JsonPersonSpec jsonPersonSpec2 = jsonPersonSpec;
				int num16 = Convert.ToInt32(this.GetCondAmount("StatAge"));
				jsonPersonSpec.nAgeMin = num16;
				jsonPersonSpec2.nAgeMax = num16;
				if (guichargenStack.GetLatestCareer() != null)
				{
					jsonPersonSpec.strCareerNow = guichargenStack.GetLatestCareer().GetJC().strName;
				}
				jsonPersonSpec.strFirstName = guichargenStack.strFirstName;
				jsonPersonSpec.strLastName = guichargenStack.strLastName;
				jsonPersonSpec.strGender = "IsMale";
				if (this.HasCond("IsFemale"))
				{
					jsonPersonSpec.strGender = "IsFemale";
				}
				else if (this.HasCond("IsNB"))
				{
					jsonPersonSpec.strGender = "IsNB";
				}
				JsonPersonSpec jsonPersonSpec3 = jsonPersonSpec;
				string strATCCode = guichargenStack.GetHomeworld().strATCCode;
				jsonPersonSpec.strHomeworldSet = strATCCode;
				jsonPersonSpec3.strHomeworldFind = strATCCode;
				this.pspec = new PersonSpec(jsonPersonSpec, false);
				this.pspec.nStrata = guichargenStack.Strata;
				this.pspec.strCO = this.strID;
				flag3 = CrewSim.bSaveHasMissingPledgePayloads;
			}
			int num17 = 0;
			if (jCOSIn.aPledges != null)
			{
				foreach (JsonPledgeSave jsonPledgeSave in jCOSIn.aPledges)
				{
					if (CrewSim.bSaveHasMissingPledgeUs && jsonPledgeSave.strUsID != jCOSIn.strID)
					{
						string strUsID = jsonPledgeSave.strUsID;
						jsonPledgeSave.strUsID = jCOSIn.strID;
						if (jsonPledgeSave.strThemID == strUsID)
						{
							jsonPledgeSave.strThemID = jsonPledgeSave.strUsID;
						}
					}
					Pledge2 pledge = PledgeFactory.Factory(jsonPledgeSave);
					if (pledge != null)
					{
						this.AddPledge(pledge);
						num17++;
					}
				}
			}
			if (jCOSIn.strComp != null)
			{
				this.Company = CrewSim.system.GetCompany(jCOSIn.strComp);
			}
			if (flag3 && num17 < 6)
			{
				Interaction interactionCurrent = this.GetInteractionCurrent();
				if (interactionCurrent != null && interactionCurrent.strName != null && interactionCurrent.strName.IndexOf("PSP") == 0)
				{
					flag3 = false;
				}
			}
			if (flag3 && num17 < 6)
			{
				if (this.pspec.strLootIAAdds == null)
				{
					if (this.HasCond("IsRobot"))
					{
						this.pspec.strLootIAAdds = "PSPIAAddNPCRobotVenus";
					}
					else if (this.HasCond("CareerLEOfficer"))
					{
						if (this.pspec.strHomeworldNow == "OKLG")
						{
							this.pspec.strLootIAAdds = "PSPIAAddNPCPoliceOKLG";
						}
						else if (this.pspec.strHomeworldNow == "VNCA" || this.pspec.strHomeworldNow == "VCBR" || this.pspec.strHomeworldNow == "VENC")
						{
							this.pspec.strLootIAAdds = "PSPIAAddNPCPoliceVenus";
						}
					}
					else if (!this.HasCond("IsPlayer"))
					{
						this.pspec.strLootIAAdds = "PSPIAAddNPCBasic";
					}
				}
				if (this.pspec.strLootIAAdds == null)
				{
					this.pspec.strLootIAAdds = "PSPIAAddPlayer";
				}
				UnityEngine.Debug.LogWarning(string.Concat(new object[]
				{
					"Repairing missing pledges on ",
					this.pspec.strFirstName,
					" ",
					this.pspec.strLastName,
					". Found: ",
					num17,
					". Using ",
					this.pspec.strLootIAAdds
				}));
				foreach (string text6 in DataHandler.GetLoot(this.pspec.strLootIAAdds).GetLootNames(null, false, null))
				{
					Interaction interaction2 = DataHandler.GetInteraction(text6, null, false);
					if (interaction2 != null)
					{
						interaction2.objUs = this;
						interaction2.objThem = this;
						if (interaction2.Triggered(false, true, false))
						{
							interaction2.ApplyChain(null);
						}
					}
				}
			}
		}
		if (jid.strAudioEmitter != null)
		{
			JsonAudioEmitter audioEmitter = DataHandler.GetAudioEmitter(jid.strAudioEmitter);
			if (audioEmitter != null)
			{
				AudioEmitter audioEmitter2 = base.gameObject.AddComponent<AudioEmitter>();
				audioEmitter2.SetData(audioEmitter);
				audioEmitter2.RandomizePitchAll();
				audioEmitter2.FadeInSteady(-1f, -1f);
			}
		}
		this.SetUpBehaviours();
	}

	public void ApplyGPMChanges(string[] aGPMChanges)
	{
		if (aGPMChanges == null || aGPMChanges.Length == 0)
		{
			return;
		}
		foreach (string text in aGPMChanges)
		{
			if (!string.IsNullOrEmpty(text))
			{
				string[] array = text.Split(new char[]
				{
					','
				});
				if (array.Length >= 3 && !string.IsNullOrEmpty(array[0]) && !string.IsNullOrEmpty(array[1]))
				{
					if (array[0] == "Rename")
					{
						this.Rename(array[2]);
					}
					Dictionary<string, string> dictionary = null;
					if (!this.mapGUIPropMaps.TryGetValue(array[0], out dictionary))
					{
						UnityEngine.Debug.LogWarning("GPM Not found on CO: " + array[0]);
						dictionary = new Dictionary<string, string>();
						UnityEngine.Debug.LogWarning("Adding new GPM to CO: " + array[0] + " : " + this.strName);
						this.mapGUIPropMaps[array[0]] = dictionary;
					}
					dictionary[array[1]] = array[2];
				}
			}
		}
	}

	public string GetGPMInfo(string strGPM, string strKey)
	{
		if (string.IsNullOrEmpty(strGPM) || string.IsNullOrEmpty(strKey))
		{
			return null;
		}
		Dictionary<string, string> dictionary = null;
		if (this.mapGUIPropMaps.TryGetValue(strGPM, out dictionary))
		{
			string result = null;
			dictionary.TryGetValue(strKey, out result);
			return result;
		}
		return null;
	}

	public void ApplyAModes(string[] aAModeIAs, bool bRebuildQAB)
	{
		if (aAModeIAs == null || aAModeIAs.Length == 0)
		{
			return;
		}
		bool flag = false;
		foreach (string text in aAModeIAs)
		{
			if (!string.IsNullOrEmpty(text))
			{
				bool flag2 = text.IndexOf('-') == 0;
				string text2 = text;
				if (flag2)
				{
					text2 = text2.Substring(1);
				}
				if (DataHandler.dictInteractions.ContainsKey(text2))
				{
					if (flag2)
					{
						this.aAttackIAs.Remove(text2);
						flag = true;
					}
					else if (this.aAttackIAs.IndexOf(text2) < 0)
					{
						this.aAttackIAs.Add(text2);
						flag = true;
					}
				}
				else
				{
					string[] array = text2.Split(new char[]
					{
						';'
					});
					if (array.Length >= 2 && !string.IsNullOrEmpty(array[1]))
					{
						JsonInteraction jsonInteraction = null;
						if (DataHandler.dictInteractions.TryGetValue(array[0], out jsonInteraction))
						{
							JsonInteraction jsonInteraction2 = jsonInteraction.Clone();
							jsonInteraction2.strAttackMode = array[1];
							jsonInteraction2.strName = text2;
							if (array.Length >= 3)
							{
								jsonInteraction2.aLootItms = new List<string>(jsonInteraction2.aLootItms)
								{
									"Use," + array[2] + ",false"
								}.ToArray();
							}
							DataHandler.dictInteractions[text2] = jsonInteraction2;
							if (flag2)
							{
								this.aAttackIAs.Remove(text2);
								flag = true;
							}
							else if (this.aAttackIAs.IndexOf(text2) < 0)
							{
								this.aAttackIAs.Add(text2);
								flag = true;
							}
						}
					}
				}
			}
		}
		if (flag && bRebuildQAB)
		{
			MonoSingleton<GUIQuickBar>.Instance.BuildButtonList(true);
		}
	}

	public void AddCommand(string strDef)
	{
		string[] array = strDef.Split(new char[]
		{
			','
		});
		if (array.Length == 0)
		{
			return;
		}
		if (array[0] == "GasExchange")
		{
			if (array.Length < 4)
			{
				return;
			}
			float fWall = 0f;
			float.TryParse(array[3], out fWall);
			GasExchange gasExchange = base.gameObject.AddComponent<GasExchange>();
			gasExchange.SetData(array[1], array[2], fWall);
			this.aManUpdates.Add(gasExchange);
		}
		else if (array[0] == "Pledge")
		{
			if (array.Length < 2)
			{
				return;
			}
			JsonPledge pledge = DataHandler.GetPledge(array[1]);
			Pledge2 pledge2 = PledgeFactory.Factory(this, pledge, null);
			this.AddPledge(pledge2);
		}
		else if (array[0] == "OnAddCond")
		{
			if (array.Length < 7)
			{
				return;
			}
			this.dictAddCondEvents[array[1]] = new string[]
			{
				array[2],
				array[3],
				array[4],
				array[5],
				array[6]
			};
		}
		else if (array[0] == "OnRemoveCond")
		{
			if (array.Length < 7)
			{
				return;
			}
			this.dictRemoveCondEvents[array[1]] = new string[]
			{
				array[2],
				array[3],
				array[4],
				array[5],
				array[6]
			};
		}
		else if (array[0] == "GasRespire2")
		{
			if (array.Length < 3)
			{
				return;
			}
			GasPump gasPump = base.gameObject.AddComponent<GasPump>();
			gasPump.SetData(DataHandler.GetGasRespire(array[1]), true, false, array[2]);
			this.aManUpdates.Add(gasPump);
		}
		else if (array[0] == "GasPump")
		{
			if (array.Length < 3)
			{
				return;
			}
			GasPump gasPump2 = base.gameObject.AddComponent<GasPump>();
			gasPump2.SetData(DataHandler.GetGasRespire(array[1]), false, true, array[2]);
			this.aManUpdates.Add(gasPump2);
		}
		else if (array[0] == "GasPressureSense")
		{
			if (array.Length < 2)
			{
				return;
			}
			GasPressureSense gasPressureSense = base.gameObject.AddComponent<GasPressureSense>();
			gasPressureSense.SetData(DataHandler.GetGUIPropMap(array[1]));
			this.aManUpdates.Add(gasPressureSense);
		}
		else if (array[0] == "Sensor")
		{
			if (array.Length < 2)
			{
				return;
			}
			Sensor sensor = base.gameObject.AddComponent<Sensor>();
			sensor.SetData(DataHandler.GetGUIPropMap(array[1]));
			this.aManUpdates.Add(sensor);
		}
		else if (array[0] == "Destructable")
		{
			if (array.Length < 2)
			{
				return;
			}
			Destructable destructable = base.gameObject.GetComponent<Destructable>();
			if (destructable == null)
			{
				destructable = base.gameObject.AddComponent<Destructable>();
				this.aManUpdates.Add(destructable);
			}
			destructable.SetData(array);
			if (!this.aDestructableConds.Contains(array[1]))
			{
				this.aDestructableConds.Add(array[1]);
			}
		}
		else if (array[0] == "Heater")
		{
			if (array.Length < 2)
			{
				return;
			}
			Heater heater = base.gameObject.AddComponent<Heater>();
			heater.SetData(array[1]);
			this.aManUpdates.Add(heater);
		}
		else if (array[0] == "Explosion")
		{
			if (array.Length < 2)
			{
				return;
			}
			Explosion explosion = base.gameObject.AddComponent<Explosion>();
			explosion.strType = array[1];
			this.aManUpdates.Add(explosion);
		}
		else if (array[0] == "Wound")
		{
			if (array.Length < 2)
			{
				return;
			}
			this.wound = base.gameObject.AddComponent<Wound>();
			this.wound.SetData(array[1]);
			this.aManUpdates.Add(this.wound);
		}
		else if (array[0] == "Electrical")
		{
			if (array.Length < 2)
			{
				return;
			}
			this.elec = base.gameObject.AddComponent<Electrical>();
			this.elec.SetData(array);
			this.aManUpdates.Add(this.elec);
		}
		else if (array[0] == "Meat")
		{
			Meat item = base.gameObject.AddComponent<Meat>();
			this.aManUpdates.Add(item);
		}
		else if (array[0] == "Rotor")
		{
			if (array.Length < 3)
			{
				return;
			}
			Rotor rotor = base.gameObject.AddComponent<Rotor>();
			rotor.SetData(this, array[1], array[2]);
			this.aManUpdates.Add(rotor);
		}
	}

	private void AddTask(string strDuty, string strInteraction)
	{
		Task2 task = new Task2();
		task.strDuty = strDuty;
		task.strInteraction = strInteraction;
		task.strName = strInteraction + this.strID;
		task.strTargetCOID = this.strID;
		CrewSim.objInstance.workManager.AddTask(task, 1);
	}

	public void AddPledge(Pledge2 pledge)
	{
		if (pledge == null)
		{
			return;
		}
		if (!this.dictPledges.ContainsKey(pledge.Priority))
		{
			this.dictPledges[pledge.Priority] = new List<Pledge2>();
		}
		foreach (Pledge2 pl in this.dictPledges[pledge.Priority])
		{
			if (Pledge2.Same(pledge, pl))
			{
				return;
			}
		}
		this.dictPledges[pledge.Priority].Add(pledge);
	}

	public void RemovePledge(Pledge2 pledge)
	{
		if (pledge == null)
		{
			return;
		}
		foreach (List<Pledge2> list in this.dictPledges.Values)
		{
			if (list.Contains(pledge))
			{
				list.Remove(pledge);
				break;
			}
		}
	}

	public bool HasPledge(Pledge2 pledge)
	{
		if (pledge == null)
		{
			return false;
		}
		if (!this.dictPledges.ContainsKey(pledge.Priority))
		{
			return false;
		}
		foreach (Pledge2 pl in this.dictPledges[pledge.Priority])
		{
			if (Pledge2.Same(pledge, pl))
			{
				return true;
			}
		}
		return false;
	}

	public bool HasPledge(JsonPledge jp, string strThemID)
	{
		if (jp == null)
		{
			return false;
		}
		if (!this.dictPledges.ContainsKey(jp.nPriority))
		{
			return false;
		}
		foreach (Pledge2 pledge in this.dictPledges[jp.nPriority])
		{
			if (pledge.Them == null)
			{
				if (strThemID != null)
				{
					return false;
				}
			}
			else if (strThemID != pledge.Them.strID)
			{
				return false;
			}
			if (Pledge2.Same(pledge, jp))
			{
				return true;
			}
		}
		return false;
	}

	public List<Pledge2> GetPledgesOfType(JsonPledge jp)
	{
		if (jp == null)
		{
			return new List<Pledge2>();
		}
		if (!this.dictPledges.ContainsKey(jp.nPriority))
		{
			return new List<Pledge2>();
		}
		List<Pledge2> list = new List<Pledge2>();
		foreach (Pledge2 pledge in this.dictPledges[jp.nPriority])
		{
			if (Pledge2.Same(pledge, jp))
			{
				list.Add(pledge);
			}
		}
		return list;
	}

	public void AddTicker(JsonTicker jtNew)
	{
		if (jtNew == null || double.IsInfinity(jtNew.fTimeLeft) || double.IsNaN(jtNew.fTimeLeft))
		{
			return;
		}
		if (this.aTickers.Count == 0)
		{
			this.fLastICOUpdate = StarSystem.fEpoch;
			jtNew.SetOwner(this);
			this.aTickers.Add(jtNew);
			if (this.ship != null && this.ship.LoadState >= Ship.Loaded.Edit)
			{
				CrewSim.AddTicker(this);
			}
			return;
		}
		if (jtNew.nClampMax > 0)
		{
			int num = 0;
			for (int i = 0; i < this.aTickers.Count; i++)
			{
				JsonTicker jsonTicker = this.aTickers[i];
				if (jsonTicker.strName == jtNew.strName)
				{
					if (num == jtNew.nClampMax)
					{
						this.aTickers.Remove(jsonTicker);
						i--;
					}
					else
					{
						num++;
					}
				}
			}
			if (num >= jtNew.nClampMax)
			{
				return;
			}
		}
		for (int j = 0; j < this.aTickers.Count; j++)
		{
			if (this.aTickers[j].fTimeLeft > jtNew.fTimeLeft)
			{
				jtNew.SetOwner(this);
				this.aTickers.Insert(j, jtNew);
				break;
			}
			if (j == this.aTickers.Count - 1)
			{
				jtNew.SetOwner(this);
				this.aTickers.Add(jtNew);
				break;
			}
		}
	}

	public JsonTicker RemoveTicker(string strTicker)
	{
		if (strTicker == null)
		{
			return null;
		}
		JsonTicker jtRemove = null;
		foreach (JsonTicker jsonTicker in this.aTickers)
		{
			if (jsonTicker.strName == strTicker)
			{
				jtRemove = jsonTicker;
				break;
			}
		}
		return this.RemoveTicker(jtRemove);
	}

	public JsonTicker RemoveTicker(JsonTicker jtRemove)
	{
		if (jtRemove != null)
		{
			jtRemove.SetOwner(null);
			this.aTickers.Remove(jtRemove);
		}
		if (this.aTickers.Count == 0)
		{
			CrewSim.RemoveTicker(this);
		}
		return jtRemove;
	}

	public void SetTicker(string strTicker, float fTimeLeft)
	{
		foreach (JsonTicker jsonTicker in this.aTickers)
		{
			if (jsonTicker.strName == strTicker)
			{
				if (fTimeLeft != 0f || jsonTicker.fTimeLeft != 0.0)
				{
					jsonTicker.SetTimeLeft((double)fTimeLeft);
					this.RemoveTicker(jsonTicker);
					this.AddTicker(jsonTicker);
					break;
				}
			}
		}
	}

	public double GetTickerTimeleft(string strTicker)
	{
		foreach (JsonTicker jsonTicker in this.aTickers)
		{
			if (jsonTicker.strName == strTicker)
			{
				return jsonTicker.fTimeLeft;
			}
		}
		return -1.0;
	}

	public JsonTicker GetTicker(string strTicker)
	{
		if (this.aTickers == null)
		{
			return null;
		}
		foreach (JsonTicker jsonTicker in this.aTickers)
		{
			if (jsonTicker.strName == strTicker)
			{
				return jsonTicker;
			}
		}
		return null;
	}

	public bool HasTickers()
	{
		return this.aTickers.Count > 0;
	}

	public void ParseCondLoot(string strLoot, double fCoeff = 1.0)
	{
		Loot loot = DataHandler.GetLoot(strLoot);
		foreach (string strDef in loot.aCOs)
		{
			this.ParseCondEquation(strDef, fCoeff, 0f);
		}
	}

	public string ParseCondEquation(string strDef, double dCoeff = 1.0, float fCondRuleTrack = 0f)
	{
		if (this.bFreezeConds || strDef == null)
		{
			return null;
		}
		KeyValuePair<string, Tuple<double, double>> value;
		if (this.alreadyParsed.ContainsKey(strDef))
		{
			value = this.alreadyParsed[strDef];
		}
		else
		{
			value = Loot.ParseCondEquation(strDef);
			this.alreadyParsed.Add(strDef, value);
		}
		double num = value.Value.Item1;
		if (value.Value.Item2 != num)
		{
			num = MathUtils.Rand(value.Value.Item1, value.Value.Item2, MathUtils.RandType.Flat, null);
		}
		if (value.Key != string.Empty && num != 0.0)
		{
			this.AddCondAmount(value.Key, num * dCoeff, 0.0, fCondRuleTrack);
			return value.Key;
		}
		return null;
	}

	public void PostGameLoad(Ship.Loaded nLoad)
	{
		if (this.jCOS != null)
		{
			Crew component = base.gameObject.GetComponent<Crew>();
			if (component != null)
			{
				component.SetBodyFaceSkin(this.jCOS.strBodyType, this.jCOS.aFaceParts.Clone() as string[]);
			}
			if (this.jCOS.aStack != null)
			{
				List<CondOwner> list = new List<CondOwner>();
				foreach (string text in this.jCOS.aStack)
				{
					CondOwner condOwner = null;
					if (DataHandler.mapCOs.TryGetValue(text, out condOwner))
					{
						if (condOwner.slotNow == null)
						{
							list.Add(condOwner);
						}
					}
					else
					{
						UnityEngine.Debug.Log("ERROR: Missing stack item: " + this.jCOS.strFriendlyName + " with id: " + text);
						UnityEngine.Debug.Break();
					}
				}
				list.Add(this);
				Container container = null;
				Slot slot = this.slotNow;
				if (this.objCOParent != null)
				{
					container = this.objCOParent.objContainer;
				}
				Ship ship = this.RemoveFromCurrentHome(false);
				CondOwner.StackFromList(list);
				if (container != null)
				{
					container.AddCOSimple(this, this.pairInventoryXY);
				}
				else if (slot != null && slot.compSlots != null)
				{
					slot.compSlots.SlotItem(slot.strName, this, true);
				}
				else if (ship != null)
				{
					ship.AddCO(this, true);
				}
			}
			if (this.jCOS.aLot != null)
			{
				foreach (string key in this.jCOS.aLot)
				{
					CondOwner condOwner2 = null;
					DataHandler.mapCOs.TryGetValue(key, out condOwner2);
					if (condOwner2 != null)
					{
						condOwner2.RemoveFromCurrentHome(false);
						this.AddLotCO(condOwner2);
					}
				}
			}
			if (this.Pathfinder != null && this.jCOS.strDestShip != null)
			{
				this.Pathfinder.strDestCO = this.jCOS.strDestCO;
				this.Pathfinder.strDestShip = this.jCOS.strDestShip;
				this.Pathfinder.nDestTile = this.jCOS.nDestTile;
			}
			if (this.wound != null)
			{
				this.wound.PostGameLoad();
			}
			this.jCOS = null;
		}
		if (this.aQueue == null)
		{
			UnityEngine.Debug.Log("ERROR: Null aQueue on " + this.strName);
			return;
		}
		foreach (Interaction interaction in this.aQueue)
		{
			interaction.PostGameLoad();
		}
		if (nLoad < Ship.Loaded.Edit)
		{
			return;
		}
		if (this.aQueue.Count > 0)
		{
			Interaction interaction2 = this.aQueue[0];
			if (this.progressBar != null && interaction2.strName != "Walk")
			{
				double num = this.GetTickerTimeleft(interaction2.strName);
				if (num < 0.0)
				{
					num = interaction2.fDuration;
				}
				bool showLongbar = interaction2.strName != null && DataHandler.dictInstallables2.ContainsKey(interaction2.strName);
				this.progressBar.Activate((float)(num * 3600.0), showLongbar, interaction2);
			}
		}
		this.RefreshAnim();
	}

	public void EndTurn()
	{
		if (this.tf == null || this.ship == null || false)
		{
			return;
		}
		double num = StarSystem.fEpoch - this.fLastICOUpdate;
		if (num != 0.0)
		{
			float elapsed = Convert.ToSingle(num);
			this.aCondsTemp.AddRange(this.aCondsTimed);
			foreach (Condition condition in this.aCondsTemp)
			{
				condition.Update(elapsed, this);
			}
			this.aCondsTemp.Clear();
		}
		this.fLastICOUpdate = StarSystem.fEpoch;
		this.AIHandleCancels();
		bool flag = this.bAlive && !this.HasCond("Unconscious");
		this.CleanupReplies(flag);
		if (CrewSim.GetSelectedCrew() == this)
		{
			CondOwner.UpdateWaitingReplies.Invoke(this.aReplies);
		}
		if (!this.bAlive)
		{
			return;
		}
		if (this.Company != null)
		{
			int hourFromS = MathUtils.GetHourFromS(StarSystem.fEpoch);
			if (hourFromS != MathUtils.GetHourFromS(StarSystem.fEpoch - num))
			{
				this.ShiftChange(this.Company.GetShift(hourFromS, this), false);
			}
		}
		bool flag2 = GUISocialCombat2.coUs == this || GUISocialCombat2.coThem == this;
		Pledge2 pledge = null;
		MonoSingleton<TargetVisController>.Instance.UpdateTargetVis(this, this.aQueue);
		if (flag && this.HasCond("IsPledgeChecker") && this.HasPledgeEmergency(out pledge))
		{
			if (pledge != null)
			{
				bool flag3 = pledge.Do();
				if (flag3)
				{
					if (this.RecentWorkHistory == null)
					{
						this.RecentWorkHistory = new COWorkHistoryDTO();
					}
					this.RecentWorkHistory.RecordPledge(pledge);
				}
			}
		}
		else if (this.aQueue.Count > 0)
		{
			bool flag4 = false;
			Interaction interaction = this.aQueue[0];
			if (interaction.strName == "Walk")
			{
				this.progressBar.DeactivateImmediate();
				if (this.Pathfinder == null || this.Pathfinder.InRange(null))
				{
					interaction.fDuration = 0.0;
				}
			}
			else if (interaction.objThem == null)
			{
				interaction.fDuration = 0.0;
			}
			else if (interaction.strName == "Wait" && interaction.fDuration > 0.0)
			{
				bool flag5 = false;
				foreach (Interaction interaction2 in interaction.objThem.aQueue)
				{
					if (interaction2.objThem == this)
					{
						flag5 = true;
						break;
					}
				}
				if (!flag5)
				{
					this.WaitFor(interaction.objThem, true);
				}
			}
			else if (interaction.aSeekItemsForContract != null && interaction.aSeekItemsForContract.Count > 0)
			{
				Interaction interaction3;
				if (interaction.bEquip)
				{
					interaction3 = DataHandler.GetInteraction("EquipItem", null, false);
				}
				else if (interaction.bLot)
				{
					interaction3 = DataHandler.GetInteraction("PickupItemStack", null, false);
				}
				else
				{
					interaction3 = DataHandler.GetInteraction("PickupItem", null, false);
				}
				if (interaction3 != null)
				{
					interaction3.objUs = this;
					interaction3.objThem = interaction.aSeekItemsForContract[0];
					interaction3.bManual = interaction.bManual;
					this.QueueInteraction(interaction3.objThem, interaction3, true);
					interaction.aSeekItemsForContract.RemoveAt(0);
					interaction.bRetestItems = true;
					interaction3.AddDependent(interaction);
					interaction = interaction3;
				}
			}
			else if (interaction.bRetestItems)
			{
				bool bGetItemBefore = interaction.bGetItemBefore;
				bool bVerboseTrigger = interaction.bVerboseTrigger;
				interaction.bGetItemBefore = false;
				interaction.bVerboseTrigger = true;
				if (!interaction.Triggered(interaction.objUs, interaction.objThem, false, false, false, true, null))
				{
					string text = interaction.objThem.strName;
					if (interaction.objThem.strNameFriendly != null)
					{
						text = interaction.objThem.strNameFriendly;
					}
					this.LogMessage(DataHandler.GetString("ERROR_CANT_DO_IA", false) + interaction.strTitle + ". " + interaction.FailReasons(false, true, false), "Bad", this.strName);
					UnityEngine.Debug.Log(string.Concat(new string[]
					{
						DataHandler.GetString("ERROR_CANT_DO_IA", false),
						interaction.strTitle,
						"; Target: ",
						text,
						". ",
						interaction.FailReasons(false, true, true)
					}));
					this.ClearInteraction(interaction, false);
				}
				else
				{
					interaction.bRetestItems = false;
				}
				if (interaction != null)
				{
					interaction.bGetItemBefore = bGetItemBefore;
					interaction.bVerboseTrigger = bVerboseTrigger;
				}
			}
			else
			{
				Tile tileAtWorldCoords = this.ship.GetTileAtWorldCoords1(this.tf.position.x, this.tf.position.y, true, true);
				Tile tilStart = tileAtWorldCoords;
				if (interaction.strTargetPoint != null && interaction.strTargetPoint != Interaction.POINT_REMOTE)
				{
					Vector2 pos = interaction.objThem.GetPos(interaction.strTargetPoint, false);
					tilStart = this.ship.GetTileAtWorldCoords1(pos.x, pos.y, true, true);
				}
				if (this.Pathfinder == null)
				{
					if ((float)TileUtils.TileRange(tilStart, tileAtWorldCoords) <= interaction.fTargetPointRange)
					{
						flag4 = true;
					}
				}
				else if (interaction.strTargetPoint == null || interaction.strTargetPoint == Interaction.POINT_REMOTE || this.Pathfinder.InRange(null))
				{
					flag4 = true;
				}
				else if (!this.CheckWalk(interaction, interaction.objThem))
				{
					string text2 = interaction.objThem.strName;
					if (interaction.objThem.strNameFriendly != null)
					{
						text2 = interaction.objThem.strNameFriendly;
					}
					this.LogMessage(DataHandler.GetString("ERROR_CANT_REACH_DEST", false) + text2 + ".", "Bad", this.strName);
					UnityEngine.Debug.Log(string.Concat(new string[]
					{
						DataHandler.GetString("ERROR_CANT_REACH_DEST", false),
						interaction.strName,
						"; Target: ",
						text2,
						"."
					}));
					this.ClearInteraction(interaction, false);
				}
			}
			if (flag4)
			{
				if (interaction.fEpochAdded == 0.0)
				{
					interaction.fEpochAdded = StarSystem.fEpoch;
				}
				if (this.pspec != null && interaction.objThem != this && interaction.objThem.pspec != null && interaction.objThem.strName != this.strLastSocial)
				{
					Relationship relationship;
					if (this.strLastSocial != null)
					{
						relationship = this.socUs.GetRelationship(this.strLastSocial);
						if (relationship != null)
						{
							relationship.ApplyConds(this, true);
						}
					}
					relationship = this.socUs.GetRelationship(interaction.objThem.strName);
					if (relationship == null)
					{
						relationship = this.socUs.AddStranger(interaction.objThem.pspec);
					}
					else
					{
						relationship.ApplyConds(this, false);
					}
					this.strLastSocial = interaction.objThem.strName;
				}
				if (interaction.strTeleport != null && interaction.objThem != null && interaction.objThem != this)
				{
					interaction.Teleport(this.Pathfinder, false);
				}
				if (interaction.nLogging != Interaction.Logging.NONE && !interaction.bLogged)
				{
					if (!string.IsNullOrEmpty(interaction.strAnimTrig))
					{
						this.SetAnimTrigger(interaction.strAnimTrig);
					}
					if (interaction.attackMode != null && interaction.attackMode.bPlayAudioEarly && !string.IsNullOrEmpty(interaction.attackMode.strAudioAttack))
					{
						AudioEmitter component = base.GetComponent<AudioEmitter>();
						if (component != null)
						{
							component.StartOther(interaction.attackMode.strAudioAttack);
						}
					}
					interaction.ApplyLogging(this.strName, false);
					if (this.progressBar != null && interaction.strName != "Walk")
					{
						bool showLongbar = interaction.strName != null && DataHandler.dictInstallables2.ContainsKey(interaction.strName);
						this.progressBar.Activate((float)(interaction.fDuration * 3600.0), showLongbar, interaction);
					}
					CondOwner selectedCrew = CrewSim.GetSelectedCrew();
					if (interaction.objThem == selectedCrew)
					{
						if (interaction.attackMode != null)
						{
							string strReason = DataHandler.GetString("AUTOPAUSE_ATTACK_INCOMING", false) + interaction.strTitle + DataHandler.GetString("AUTOPAUSE_ATTACK_FROM", false) + interaction.objUs.FriendlyName;
							CrewSim.TriggerAutoPause(strReason);
						}
					}
					else if (interaction.objUs == selectedCrew && interaction.strActionGroup == "Talk" && interaction.objThem != null && !interaction.objThem.HasCond("IsInCombat"))
					{
						Interaction interactionCurrent = interaction.objThem.GetInteractionCurrent();
						if (interactionCurrent == null || interactionCurrent.strName == "Walk" || interactionCurrent.strName == "QuickWait")
						{
							interaction.objThem.AICancelAll(this);
							interaction.objThem.QueueInteraction(this, DataHandler.GetInteraction("QuickWait", null, false), false);
						}
					}
					if (flag2 && interaction.strName != "Wait")
					{
						CrewSim.objInstance.CamCenter(this);
						if (CanvasManager.instance.State == CanvasManager.GUIState.SOCIAL)
						{
							GUISocialCombat2.objInstance.ThrobOn(this);
						}
					}
				}
				if ((interaction.strRaiseUI != null || interaction.strRaiseUIThem != null) && !interaction.bRaisedUI)
				{
					if (interaction.bUsePDA)
					{
						string gpminfo = this.GetGPMInfo(interaction.strRaiseUI, "strGUIPrefab");
						GUIPDA.UIState stateFromString = GUIPDA.GetStateFromString(gpminfo);
						GUIPDA.instance.State = stateFromString;
					}
					else if (interaction.strRaiseUI != null)
					{
						CrewSim.RaiseUI(interaction.strRaiseUI, this);
					}
					else
					{
						CrewSim.RaiseUI(interaction.strRaiseUIThem, interaction.objThem);
					}
					interaction.bRaisedUI = true;
				}
				if (this.Pathfinder != null && interaction.objThem != null && interaction.objThem != this)
				{
					this.LookAt(interaction.objThem, false);
				}
				interaction.fDuration -= num / 60.0 / 60.0;
			}
			if (interaction.fDuration <= 0.0)
			{
				if ((interaction.strRaiseUI != null || interaction.strRaiseUIThem != null) && interaction.bRaisedUI)
				{
					CrewSim.LowerUI(false);
				}
				CondOwner objThem = interaction.objThem;
				if (objThem != null)
				{
					string strIAName = interaction.strName;
					bool flag6 = interaction.strName == "Wait" || interaction.strName == "Walk";
					bool bCloser = interaction.bCloser;
					Interaction interaction4 = this.Interact();
					if (interaction4 != null)
					{
						bool flag7 = interaction4.objUs.CheckWalk(interaction4, interaction4.objThem);
						if (flag7)
						{
							interaction4.objUs.QueueInteraction(interaction4.objThem, interaction4, false);
						}
						else
						{
							string text3 = interaction4.objThem.strName;
							if (interaction4.objThem.strNameFriendly != null)
							{
								text3 = interaction4.objThem.strNameFriendly;
							}
							interaction4.objUs.LogMessage(DataHandler.GetString("ERROR_CANT_REACH_DEST", false) + text3 + ".", "Bad", this.strName);
							UnityEngine.Debug.Log(string.Concat(new string[]
							{
								DataHandler.GetString("ERROR_CANT_REACH_DEST", false),
								interaction4.strName,
								"; Target: ",
								text3,
								"."
							}));
							interaction4 = null;
						}
					}
					if (flag2 && !flag6)
					{
						if (GUISocialCombat2.coThem != null && !GUISocialCombat2.coThem.bAlive)
						{
							GUISocialCombat2.objInstance.EndSocialCombat();
						}
						else if (interaction4 == null)
						{
							if (bCloser)
							{
								GUISocialCombat2.objInstance.EndSocialCombat();
							}
							else
							{
								GUISocialCombat2.ResetSocialCombat(this);
							}
						}
					}
					bool flag8 = false;
					foreach (ReplyThread replyThread in this.aReplies)
					{
						if (replyThread.Fulfills(strIAName, objThem.strID))
						{
							replyThread.bDone = true;
							flag8 = true;
						}
					}
					if (flag8 && CrewSim.GetSelectedCrew() == this)
					{
						CondOwner.UpdateWaitingReplies.Invoke(this.aReplies);
					}
				}
				else
				{
					this.ClearInteraction(interaction, false);
				}
				CondOwner selectedCrew2 = CrewSim.GetSelectedCrew();
				if ((selectedCrew2 != null && selectedCrew2 == this) || MonoSingleton<GUIQuickBar>.Instance.COTarget == this || interaction.objThem == selectedCrew2)
				{
					ModuleHost.UpdateUI.Invoke();
					MonoSingleton<GUIQuickBar>.Instance.BuildButtonList(false);
				}
			}
		}
		else if (!flag2)
		{
			if (flag)
			{
				if (this.progressBar != null)
				{
					this.progressBar.DeactivateImmediate();
				}
				bool flag9 = false;
				if (this.socUs != null && this.strLastSocial != null)
				{
					Relationship relationship = this.socUs.GetRelationship(this.strLastSocial);
					if (relationship != null)
					{
						relationship.ApplyConds(this, true);
						this.strLastSocial = null;
					}
				}
				if (this.ship != null && this.HasCond("IsPledgeChecker"))
				{
					int i = 10;
					while (i > 0)
					{
						if (!this.dictPledges.ContainsKey(i))
						{
							goto IL_FCA;
						}
						if (this.dictPledges[i].Count != 0)
						{
							Pledge2[] array = new Pledge2[this.dictPledges[i].Count];
							this.dictPledges[i].CopyTo(array);
							foreach (Pledge2 pledge2 in array)
							{
								flag9 = pledge2.Do();
								if (this.dictPledges[i].Contains(pledge2))
								{
									this.dictPledges[i].Remove(pledge2);
									this.dictPledges[i].Add(pledge2);
								}
								if (flag9)
								{
									if (this.RecentWorkHistory == null)
									{
										this.RecentWorkHistory = new COWorkHistoryDTO();
									}
									this.RecentWorkHistory.RecordPledge(pledge2);
									break;
								}
							}
							goto IL_FCA;
						}
						IL_FD6:
						i--;
						continue;
						IL_FCA:
						if (flag9)
						{
							break;
						}
						goto IL_FD6;
					}
				}
				if (!flag9 && this.IsHumanOrRobot)
				{
					if (CondOwner.nEndTurnsThisFrame > 0)
					{
						JsonTicker jsonTicker = new JsonTicker();
						jsonTicker.strName = "AINudge";
						jsonTicker.bQueue = true;
						jsonTicker.fPeriod = 2.7800000680144876E-05;
						jsonTicker.SetTimeLeft(jsonTicker.fPeriod);
						this.AddTicker(jsonTicker);
						return;
					}
					this.ZeroCondAmount("IsEmergencyOverride");
					if (this.aReplies.Count <= 0 && !this.HasCond("InSocialCombat"))
					{
						if (this.jsShiftLast.nID == 2)
						{
							this.GetWork();
						}
						else
						{
							this.GetMove2();
						}
					}
					CondOwner.nEndTurnsThisFrame++;
				}
			}
		}
	}

	private void CleanupReplies(bool bConscious)
	{
		for (int i = this.aReplies.Count - 1; i >= 0; i--)
		{
			bool flag = false;
			CondOwner condOwner;
			if (DataHandler.mapCOs.TryGetValue(this.aReplies[i].strID, out condOwner) && (!bConscious || condOwner.HasCond("Unconscious") || !condOwner.bAlive))
			{
				Interaction interaction = DataHandler.GetInteraction("SocialCombatExitSilent", null, false);
				interaction.objUs = this;
				interaction.objThem = condOwner;
				interaction.ApplyChain(null);
				flag = true;
			}
			if (flag || this.aReplies[i].bDone || StarSystem.fEpoch - this.aReplies[i].fEpoch > 30.0)
			{
				this.aReplies.RemoveAt(i);
			}
		}
	}

	private bool HasPledgeEmergency(out Pledge2 pld)
	{
		pld = null;
		for (int i = 10; i > 0; i--)
		{
			if (this.dictPledges.ContainsKey(i))
			{
				foreach (Pledge2 pledge in this.dictPledges[i])
				{
					if (pledge != null && pledge.IsEmergency())
					{
						pld = pledge;
						return true;
					}
				}
			}
		}
		return false;
	}

	public void ShiftChange(JsonShift js, bool bSilent)
	{
		if (this.jsShiftLast == null)
		{
			this.jsShiftLast = JsonCompany.NullShift;
		}
		if (js == null)
		{
			js = JsonCompany.NullShift;
		}
		if (js.nID != this.jsShiftLast.nID)
		{
			bool flag = this.bLogConds;
			this.bLogConds = !bSilent;
			Loot loot = DataHandler.GetLoot(js.strCondLoot);
			Loot loot2 = DataHandler.GetLoot(this.jsShiftLast.strCondLoot);
			foreach (string strDef in loot2.aCOs)
			{
				this.ParseCondEquation(strDef, -1.0, 0f);
			}
			foreach (string strDef2 in loot.aCOs)
			{
				this.ParseCondEquation(strDef2, 1.0, 0f);
			}
			this.jsShiftLast = js;
			if (!bSilent)
			{
				this.LogMessage(this.FriendlyName + DataHandler.GetString("SHIFT_CHANGE", false) + js.strName.Replace("CONDShift", string.Empty) + ".", "Neutral", this.strName);
			}
			PlayerMarker.AddMarker(this);
			this.bLogConds = flag;
		}
	}

	private void GetWork()
	{
		if (this.ship == null || !this.IsHumanOrRobot)
		{
			return;
		}
		CondOwner.FreeWillLoot.ApplyCondLoot(this, 1f, null, 0f);
		if (!this.HasCond("IsPlayer") && this.Company == CrewSim.coPlayer.Company)
		{
			Interaction interaction = DataHandler.GetInteraction("SeekSocialDeny", null, false);
			interaction.objUs = this;
			interaction.objThem = CrewSim.coPlayer;
			if (interaction.Triggered(interaction.objUs, interaction.objThem, false, false, false, true, null))
			{
				this.Pathfinder.Reset();
				this.QueueInteraction(interaction.objThem, interaction, false);
				CrewSim.objInstance.workManager.IdleAdd(this);
				return;
			}
		}
		Task2 task = CrewSim.objInstance.workManager.ClaimNextTask(this);
		if (task != null)
		{
			if (task.strInteraction == "QuickWait")
			{
				return;
			}
			if (task.strInteraction == "ACTHaulItem")
			{
				if (!WorkManager.CTHaul.Triggered(task.GetIA().objThem, null, true))
				{
					return;
				}
				Tile tile = null;
				Ship ship = null;
				CrewSim.system.dictShips.TryGetValue(task.strTileShip, out ship);
				if (ship != null)
				{
					if (ship.aTiles.Count > task.nTile)
					{
						tile = ship.aTiles[task.nTile];
					}
					else if (CrewSim.objInstance.workManager.HaulZone(this, task, task.GetIA().objThem) != null)
					{
						tile = this.ship.aTiles[task.nTile];
						CrewSim.system.dictShips.TryGetValue(task.strTileShip, out ship);
					}
				}
				if (tile == null)
				{
					if (this.RecentWorkHistory == null)
					{
						this.RecentWorkHistory = new COWorkHistoryDTO();
					}
					this.RecentWorkHistory.RecordFailedWorkAttempt("Haul Item, destination unreachable");
					CrewSim.objInstance.workManager.UnclaimTask(task);
					CrewSim.objInstance.workManager.IdleAdd(this);
					return;
				}
				Interaction interaction2 = DataHandler.GetInteraction("PickupItemStack", null, false);
				this.QueueInteraction(task.GetIA().objThem, interaction2, false);
				task.strInteraction = interaction2.strName;
				interaction2 = DataHandler.GetInteraction("Walk", null, false);
				interaction2.strTargetPoint = "use";
				interaction2.fTargetPointRange = 0f;
				this.QueueInteraction(tile.coProps, interaction2, false);
				interaction2 = DataHandler.GetInteraction("DropItemStack", null, false);
				this.QueueInteraction(task.GetIA().objThem, interaction2, false);
				task.SetIA(interaction2);
			}
			else
			{
				this.QueueInteraction(task.GetIA().objThem, task.GetIA(), false);
			}
			CrewSim.objInstance.workManager.IdleRemove(this);
			return;
		}
		else
		{
			CrewSim.objInstance.workManager.IdleAdd(this);
			bool flag = this.HasCond("IsAIManual");
			bool flag2 = true;
			if (this.Company != null)
			{
				flag2 = this.Company.mapRoster[this.strID].bRestorePermission;
			}
			if (!flag && flag2)
			{
				List<CondOwner> list = new List<CondOwner>();
				if (this.OwnsShip(this.ship.strRegID))
				{
					list.AddRange(this.ship.GetCOs(this.ctRestoreItem, true, false, true));
				}
				if (this.aATsPatch == null)
				{
					this.aATsPatch = new List<AutoTask>();
				}
				else
				{
					this.aATsPatch.Clear();
				}
				if (this.aATsRepair == null)
				{
					this.aATsRepair = new List<AutoTask>();
				}
				else
				{
					this.aATsRepair.Clear();
				}
				if (this.aATsRestore == null)
				{
					this.aATsRestore = new List<AutoTask>();
				}
				else
				{
					this.aATsRestore.Clear();
				}
				double num = 1.0;
				double num2 = 1.0;
				double num3 = 1.0;
				if (this.Company != null)
				{
					num = Math.Pow(10.0, (double)this.Company.GetDutyLevel(this, "Patch"));
					num2 = Math.Pow(10.0, (double)this.Company.GetDutyLevel(this, "Restore"));
					num3 = Math.Pow(10.0, (double)this.Company.GetDutyLevel(this, "Repair"));
				}
				num = num;
				num3 += 1.0;
				num2 += 2.0;
				foreach (CondOwner condOwner in list)
				{
					if (condOwner.bCanPatch)
					{
						foreach (string text in condOwner.aInteractions)
						{
							if (text.Contains("Patch") && !text.Contains("Scrap"))
							{
								this.InsertUndamage(condOwner, text, true, num, this.aATsPatch);
								break;
							}
						}
					}
					if (condOwner.bCanRepair)
					{
						foreach (string text2 in condOwner.aInteractions)
						{
							if (text2.Contains("Repair"))
							{
								this.InsertUndamage(condOwner, text2, true, num3, this.aATsRepair);
								break;
							}
						}
					}
					if (condOwner.bCanUndamage)
					{
						foreach (string text3 in condOwner.aInteractions)
						{
							if (text3.Contains("Undamage"))
							{
								this.InsertUndamage(condOwner, text3, false, num2, this.aATsRestore);
								break;
							}
						}
					}
				}
				if (this.aATsPatch.Count + this.aATsRepair.Count + this.aATsRestore.Count > 0)
				{
					if (num > num3)
					{
						if (num3 > num2)
						{
							this.aATsRestore.AddRange(this.aATsRepair);
							this.aATsRestore.AddRange(this.aATsPatch);
						}
						else
						{
							if (num2 < num)
							{
								this.aATsRepair.AddRange(this.aATsRestore);
								this.aATsRepair.AddRange(this.aATsPatch);
							}
							else
							{
								this.aATsRepair.AddRange(this.aATsPatch);
								this.aATsRepair.AddRange(this.aATsRestore);
							}
							this.aATsRestore = this.aATsRepair;
						}
					}
					else if (num3 < num2)
					{
						this.aATsPatch.AddRange(this.aATsRepair);
						this.aATsPatch.AddRange(this.aATsRestore);
						this.aATsRestore = this.aATsPatch;
					}
					else if (num2 < num)
					{
						this.aATsRestore.AddRange(this.aATsPatch);
						this.aATsRestore.AddRange(this.aATsRepair);
					}
					else
					{
						this.aATsPatch.AddRange(this.aATsRestore);
						this.aATsPatch.AddRange(this.aATsRepair);
						this.aATsRestore = this.aATsPatch;
					}
					int num4 = Mathf.Min(this.aATsRestore.Count, 2);
					string text4 = null;
					for (int i = 0; i < num4; i++)
					{
						CondOwner co = this.aATsRestore[i].co;
						Interaction interaction3 = DataHandler.GetInteraction(this.aATsRestore[i].strIA, null, false);
						interaction3.bHumanOnly = false;
						if (i == num4 - 1)
						{
							interaction3.bVerboseTrigger = true;
						}
						this.dictRecentlyTried[co.strID + interaction3.strName] = StarSystem.fEpoch;
						if (interaction3.Triggered(this, co, false, false, true, true, null))
						{
							this.QueueInteraction(co, interaction3, false);
							return;
						}
						text4 = string.Concat(new string[]
						{
							"Auto ",
							interaction3.strTitle,
							" ",
							co.strNameFriendly,
							": ",
							interaction3.FailReasons(true, true, false)
						});
					}
					if (!string.IsNullOrEmpty(text4))
					{
						if (this.RecentWorkHistory == null)
						{
							this.RecentWorkHistory = new COWorkHistoryDTO();
						}
						this.RecentWorkHistory.RecordFailedWorkAttempt(text4);
					}
				}
			}
			if (this.HasCond("IsPlayer") || (CrewSim.GetSelectedCrew() == this && flag))
			{
				return;
			}
			if (flag)
			{
				return;
			}
			this.GetMove2();
			return;
		}
	}

	private void InsertUndamage(CondOwner co, string strUndamageIA, bool doRepair, double fDutyWeight, List<AutoTask> aATs)
	{
		string key = co.strID + strUndamageIA;
		if (this.dictRecentlyTried.ContainsKey(key))
		{
			return;
		}
		if (!doRepair && co.GetIsLikeNew())
		{
			return;
		}
		double damageState = co.GetDamageState();
		double num = (double)(co.GetPos(null, false) - this.GetPos(null, false)).magnitude;
		if (num < 2.0)
		{
			num = 0.5;
		}
		double num2 = num * damageState * damageState * fDutyWeight;
		int num3 = 0;
		foreach (AutoTask autoTask in aATs)
		{
			if (autoTask.fWeight >= num2)
			{
				break;
			}
			num3++;
		}
		aATs.Insert(num3, new AutoTask(co, strUndamageIA, num2));
	}

	public void DebugKickstart()
	{
		UnityEngine.Debug.Log(this.debugStop);
		this.GetMove2();
	}

	public bool HasAirlockPermission(bool bManual)
	{
		bool flag = !this.HasCond("IsAIManual");
		if (this.HasCond("IsEmergencyOverride"))
		{
			return true;
		}
		if (!flag && this.ctSuffocatingManWalk.Triggered(this, null, true))
		{
			return true;
		}
		bool flag2 = false;
		JsonCompanyRules jsonCompanyRules = null;
		if (this.Company != null && this.Company.mapRoster.TryGetValue(this.strID, out jsonCompanyRules))
		{
			flag2 = jsonCompanyRules.bAirlockPermission;
		}
		return flag2 && ((bManual && !flag) || (this.HasCond("IsAirtight") || this.HasCond("IsAirtightFake")));
	}

	public bool HasShoreLeave()
	{
		bool result = true;
		if (!this.HasCond("IsEmergencyOverride"))
		{
			if (!(CrewSim.coPlayer != null) || this.Company == CrewSim.coPlayer.Company || this.ship == null || this.ship != CrewSim.coPlayer.ship || !CrewSim.coPlayer.OwnsShip(this.ship.strRegID))
			{
				if (this.Company != null)
				{
					if (this.Company.mapRoster.ContainsKey(this.strID) && !this.Company.mapRoster[this.strID].bShoreLeave)
					{
						result = false;
					}
				}
				else
				{
					result = false;
				}
			}
		}
		return result;
	}

	private void GetMove2()
	{
		if (this.debugStop)
		{
			return;
		}
		if (this.aInteractions.Count == 0 || this.ship == null || !this.IsHumanOrRobot)
		{
			return;
		}
		float num = 0f;
		CondOwner condOwner = null;
		Interaction interaction = null;
		bool flag = false;
		string text = null;
		if (this.socUs == null)
		{
			this.socUs = base.gameObject.AddComponent<global::Social>();
		}
		bool flag2 = this.HasAirlockPermission(false);
		Dictionary<string, List<CondOwner>> dictionary = new Dictionary<string, List<CondOwner>>();
		bool flag3 = false;
		if (flag3)
		{
			CondTrigger condTrigger = DataHandler.GetCondTrigger("TIsAIRandomTrainerItem");
			List<CondOwner> cos = this.ship.GetCOs(condTrigger, true, false, false);
			for (int i = 0; i < 10; i++)
			{
				if (cos.Count == 0)
				{
					break;
				}
				CondOwner condOwner2 = cos[MathUtils.Rand(0, cos.Count - 1, MathUtils.RandType.Flat, null)];
				if (condOwner2.aInteractions.Count != 0)
				{
					interaction = DataHandler.GetInteraction(condOwner2.aInteractions[MathUtils.Rand(0, condOwner2.aInteractions.Count - 1, MathUtils.RandType.Flat, null)], null, true);
					if (interaction != null)
					{
						if (interaction.bOpener && !interaction.bHumanOnly && !CondOwner.aAIRandomAvoid.Contains(interaction.strName) && interaction.Triggered(this, condOwner2, false, false, false, true, null))
						{
							condOwner = condOwner2;
							UnityEngine.Debug.Log("Random chose: " + interaction.strName + " on " + condOwner.strName);
							break;
						}
						DataHandler.ReleaseTrackedInteraction(interaction);
						interaction = null;
					}
				}
			}
		}
		List<Priority> list = new List<Priority>(this.aPriorities);
		if (this.jsShiftLast != null && this.jsShiftLast.nID == 1 && this.HasCond("StatSleep") && DataHandler.GetCondTrigger("TIsSleepy").Triggered(this, null, true))
		{
			Priority item = new Priority(-500.0, this.mapConds["StatSleep"]);
			list.Insert(0, item);
		}
		foreach (Priority priority in list)
		{
			if (flag3)
			{
				break;
			}
			if (this.mapConds.ContainsValue(priority.objCond))
			{
				List<InteractionHistory> list2 = new List<InteractionHistory>();
				CondHistory ch = this.GetCH(priority.objCond.strName);
				foreach (InteractionHistory interactionHistory in ch.mapInteractions.Values)
				{
					if (!CondOwner.aAIRandomAvoid.Contains(interactionHistory.strName))
					{
						Interaction interaction2 = DataHandler.GetInteraction(interactionHistory.strName, null, true);
						if (interaction2 == null || interaction2.bHumanOnly || !interaction2.bOpener || !interaction2.CTTestUs.Triggered(this, null, true))
						{
							DataHandler.ReleaseTrackedInteraction(interaction2);
						}
						else
						{
							if (list2.Count == 0 && interactionHistory.fAverage < 0f)
							{
								list2.Add(interactionHistory);
							}
							else if (list2.Count > 0 && interactionHistory.fAverage <= list2[list2.Count - 1].fAverage)
							{
								list2.Add(interactionHistory);
							}
							DataHandler.ReleaseTrackedInteraction(interaction2);
						}
					}
				}
				foreach (InteractionHistory interactionHistory2 in list2)
				{
					Interaction interaction3 = DataHandler.GetInteraction(interactionHistory2.strName, null, true);
					if (interaction3.CTTestThem != null)
					{
						interaction3.CTTestThem.logReason = false;
					}
					List<CondOwner> list3;
					if (interaction3.strThemType == Interaction.TARGET_SELF)
					{
						list3 = new List<CondOwner>();
						if (interaction3.CTTestThem.Triggered(this, null, true))
						{
							list3.Add(this);
						}
					}
					else if (interaction3.strThemType == Interaction.TARGET_OTHER)
					{
						if (interaction3.PSpecTestThem != null)
						{
							list3 = new List<CondOwner>();
							PersonSpec person = this.ship.GetPerson(interaction3.PSpecTestThem, this.socUs, false, null);
							if (person != null)
							{
								list3.Add(person.MakeCondOwner(PersonSpec.StartShip.OLD, null));
							}
						}
						else
						{
							list3 = this.GetCOsSafe(true, interaction3.CTTestThem);
							List<CondOwner> cos2;
							if (dictionary.TryGetValue(interaction3.CTTestThem.strName, out cos2))
							{
								list3.AddRange(cos2);
							}
							else
							{
								cos2 = this.ship.GetCOs(interaction3.CTTestThem, false, true, false);
								dictionary.Add(interaction3.CTTestThem.strName, cos2);
								list3.AddRange(cos2);
							}
							list3.Remove(this);
						}
					}
					else
					{
						list3 = new List<CondOwner>();
					}
					if (interaction3.CTTestThem != null)
					{
						interaction3.CTTestThem.logReason = true;
					}
					for (int j = list3.Count - 1; j >= 0; j--)
					{
						CondOwner condOwner3 = list3[j];
						if (condOwner3 == null || condOwner3.bBusy)
						{
							list3.RemoveAt(j);
						}
						else
						{
							text = condOwner3.strID + interaction3.strName;
							if (this.dictRecentlyTried.ContainsKey(text))
							{
								list3.RemoveAt(j);
							}
							else
							{
								bool flag4 = false;
								if (condOwner3 != this && condOwner3 == CrewSim.GetSelectedCrew())
								{
									if (GUISocialCombat2.coUs == condOwner3 || GUISocialCombat2.coThem == condOwner3)
									{
										list3.RemoveAt(j);
										goto IL_610;
									}
									flag4 = true;
								}
								else if (condOwner3 != this && condOwner3.jsShiftLast != null && condOwner3.jsShiftLast.nID > 0)
								{
									flag4 = true;
								}
								if (flag4)
								{
									list3.RemoveAt(j);
								}
							}
						}
						IL_610:;
					}
					int num2 = Mathf.Min(10, list3.Count);
					for (int k = 0; k < num2; k++)
					{
						int index = Mathf.RoundToInt(UnityEngine.Random.value * (float)(list3.Count - 1));
						CondOwner condOwner4 = list3[index];
						if (interaction3.Triggered(this, condOwner4, false, false, false, true, null))
						{
							if (!flag2 && interaction3.strName == "MSPortalOpenStart")
							{
								Vector2 pos = condOwner4.GetPos("use", false);
								if (Pathfinder.CheckPressure(pos, condOwner4.ship, condOwner4.currentRoom))
								{
									goto IL_6D8;
								}
							}
							float num3 = interactionHistory2.fAverage + this.GetCOScore(condOwner4, interactionHistory2);
							if (num3 < num)
							{
								num = num3;
								condOwner = condOwner4;
								interaction = interaction3;
							}
						}
						IL_6D8:;
					}
					if (interaction != interaction3)
					{
						DataHandler.ReleaseTrackedInteraction(interaction3);
					}
					if (num < 0f)
					{
						flag = true;
						break;
					}
				}
				if (flag)
				{
					break;
				}
			}
		}
		if (interaction == null)
		{
			for (int l = 0; l < 3; l++)
			{
				int index2 = Mathf.RoundToInt(UnityEngine.Random.value * (float)(this.aInteractions.Count - 1));
				interaction = DataHandler.GetInteraction(this.aInteractions[index2], null, true);
				if (interaction != null)
				{
					if (!interaction.bOpener)
					{
						DataHandler.ReleaseTrackedInteraction(interaction);
					}
					else if (interaction == null || interaction.bHumanOnly || !interaction.CTTestUs.Triggered(this, null, true))
					{
						DataHandler.ReleaseTrackedInteraction(interaction);
					}
					else
					{
						List<CondOwner> list3;
						if (interaction.strThemType == Interaction.TARGET_SELF)
						{
							list3 = new List<CondOwner>
							{
								this
							};
						}
						else if (interaction.strThemType == Interaction.TARGET_OTHER)
						{
							if (interaction.PSpecTestThem != null)
							{
								list3 = new List<CondOwner>();
								PersonSpec person2 = this.ship.GetPerson(interaction.PSpecTestThem, this.socUs, false, null);
								if (person2 != null)
								{
									list3.Add(person2.MakeCondOwner(PersonSpec.StartShip.OLD, null));
								}
							}
							else
							{
								list3 = this.GetCOsSafe(true, interaction.CTTestThem);
								List<CondOwner> cos3;
								if (dictionary.TryGetValue(interaction.CTTestThem.strName, out cos3))
								{
									list3.AddRange(cos3);
								}
								else
								{
									cos3 = this.ship.GetCOs(interaction.CTTestThem, false, true, false);
									dictionary.Add(interaction.CTTestThem.strName, cos3);
									list3.AddRange(cos3);
								}
								list3.Remove(this);
							}
						}
						else
						{
							list3 = new List<CondOwner>();
						}
						if (list3.Count == 0)
						{
							DataHandler.ReleaseTrackedInteraction(interaction);
							interaction = null;
						}
						else
						{
							int index3 = Mathf.RoundToInt(UnityEngine.Random.value * (float)(list3.Count - 1));
							if (!flag2 && interaction.strName == "MSPortalOpenStart")
							{
								Vector2 pos2 = list3[index3].GetPos("use", false);
								if (Pathfinder.CheckPressure(pos2, list3[index3].ship, list3[index3].currentRoom))
								{
									DataHandler.ReleaseTrackedInteraction(interaction);
									goto IL_AF8;
								}
							}
							condOwner = list3[index3];
							bool flag5 = interaction.Triggered(this, condOwner, false, false, false, true, null);
							if (!flag5 || condOwner.bBusy || this.GetNetInteractionResult(interaction, false) > 0f)
							{
								DataHandler.ReleaseTrackedInteraction(interaction);
								interaction = null;
								condOwner = null;
							}
							else
							{
								bool flag6 = false;
								if (condOwner == CrewSim.GetSelectedCrew() && condOwner != this)
								{
									if (GUISocialCombat2.coUs == condOwner || GUISocialCombat2.coThem == condOwner)
									{
										DataHandler.ReleaseTrackedInteraction(interaction);
										interaction = null;
										condOwner = null;
										goto IL_AF8;
									}
									flag6 = true;
								}
								else if (condOwner != this && condOwner.jsShiftLast != null && condOwner.jsShiftLast.nID > 0)
								{
									flag6 = true;
								}
								if (flag6)
								{
									UnityEngine.Debug.Log(this.strName + " was going to bother " + condOwner.FriendlyName + ", but decided not to! (Case 2)");
									DataHandler.ReleaseTrackedInteraction(interaction);
									interaction = null;
									condOwner = null;
								}
								else if (interaction != null && condOwner != null)
								{
									text = condOwner.strID + interaction.strName;
									if (!this.dictRecentlyTried.ContainsKey(text))
									{
										break;
									}
									DataHandler.ReleaseTrackedInteraction(interaction);
									interaction = null;
									condOwner = null;
								}
							}
						}
					}
				}
				IL_AF8:;
			}
		}
		CondOwner.FreeWillLoot.ApplyCondLoot(this, 1f, null, 0f);
		if (interaction == null || condOwner == null)
		{
			return;
		}
		if (text == null)
		{
			UnityEngine.Debug.Log("strRef is null in GetMove2() on " + this.strName);
		}
		else
		{
			this.dictRecentlyTried[condOwner.strID + interaction.strName] = StarSystem.fEpoch;
		}
		if (condOwner == CrewSim.GetSelectedCrew() && this != condOwner && interaction.bSocial && interaction.strRaiseUI == null && interaction.strRaiseUIThem == null)
		{
			BeatManager.GenerateSocial(interaction);
		}
		if (this.CheckWalk(interaction, condOwner))
		{
			DataHandler.KeepInteraction(interaction);
			this.QueueInteraction(condOwner, interaction, false);
		}
		else
		{
			string text2 = condOwner.strName;
			if (condOwner.strNameFriendly != null)
			{
				text2 = condOwner.strNameFriendly;
			}
			this.LogMessage(DataHandler.GetString("ERROR_CANT_REACH_DEST", false) + text2 + ".", "Bad", this.strName);
			UnityEngine.Debug.Log(string.Concat(new string[]
			{
				this.strName,
				" ",
				DataHandler.GetString("ERROR_CANT_REACH_DEST", false),
				interaction.strName,
				"; Target: ",
				text2,
				"."
			}));
			DataHandler.ReleaseTrackedInteraction(interaction);
		}
	}

	public bool CheckWalk(Interaction objInteraction, CondOwner co)
	{
		if (this.ship == null)
		{
			return false;
		}
		if (objInteraction == null || co == null || (!co.gameObject.activeInHierarchy && objInteraction.strTargetPoint != Interaction.POINT_REMOTE))
		{
			return false;
		}
		if (objInteraction.strTargetPoint == null || objInteraction.strTargetPoint == Interaction.POINT_REMOTE)
		{
			return true;
		}
		if (objInteraction.strTargetPoint == "random")
		{
			if (this.Pathfinder == null)
			{
				return false;
			}
			bool flag = false;
			for (int i = 0; i < 10; i++)
			{
				Tile tile = this.ship.GetRandomAtmoTile(true);
				if (tile == null)
				{
					tile = this.ship.GetRandomTile1(true, false);
				}
				if (!(tile == null))
				{
					PathResult pathResult = this.Pathfinder.SetGoal2(tile, 0f, null, 0f, 0f, this.HasAirlockPermission(false));
					if (pathResult.HasPath)
					{
						flag = true;
						break;
					}
				}
			}
			if (!flag)
			{
				return false;
			}
		}
		else if (objInteraction.strTargetPoint == "random_offship")
		{
			if (this.Pathfinder == null)
			{
				return false;
			}
			List<Ship> allDockedShips = this.ship.GetAllDockedShips();
			bool flag2 = false;
			foreach (Ship ship in allDockedShips)
			{
				if (!this.HasShoreLeave())
				{
					break;
				}
				Tile tile2 = ship.GetRandomAtmoTile(true);
				if (tile2 == null)
				{
					tile2 = ship.GetRandomTile1(true, false);
				}
				if (!(tile2 == null))
				{
					Pathfinder pathfinder = this.Pathfinder;
					Tile tilDestNew = tile2;
					float fRange = 0f;
					CondOwner coDest = null;
					bool bAllowAirlocks = this.HasAirlockPermission(false);
					PathResult pathResult2 = pathfinder.SetGoal2(tilDestNew, fRange, coDest, 0f, 0f, bAllowAirlocks);
					if (pathResult2.HasPath)
					{
						flag2 = true;
						break;
					}
				}
			}
			if (!flag2)
			{
				return false;
			}
		}
		else
		{
			Vector2 vector = co.GetPos(objInteraction.strTargetPoint, false);
			if (this.GetCORef(co) != null)
			{
				vector = this.tf.position;
			}
			Tile tileAtWorldCoords = this.ship.GetTileAtWorldCoords1(vector.x, vector.y, true, true);
			if (this.Pathfinder == null)
			{
				Tile tileAtWorldCoords2 = this.ship.GetTileAtWorldCoords1(this.tf.position.x, this.tf.position.y, true, true);
				return (float)TileUtils.TileRange(tileAtWorldCoords2, tileAtWorldCoords) <= objInteraction.fTargetPointRange;
			}
			PathResult pathResult3 = this.Pathfinder.SetGoal2(tileAtWorldCoords, objInteraction.fTargetPointRange, co, vector.x, vector.y, this.HasAirlockPermission(objInteraction.bManual));
			if (!pathResult3.HasPath)
			{
				return false;
			}
		}
		if (this.Pathfinder.tilDest != null && this.Pathfinder.tilDest != this.Pathfinder.tilCurrent)
		{
			Interaction interaction = DataHandler.GetInteraction("Walk", null, false);
			if (this.Pathfinder.coDest != null && this.Pathfinder.coDest.Crew != null)
			{
				if (interaction != null)
				{
					interaction.bManual = objInteraction.bManual;
					interaction.strTargetPoint = objInteraction.strTargetPoint;
					interaction.fTargetPointRange = objInteraction.fTargetPointRange;
				}
				this.QueueInteraction(this.Pathfinder.coDest, interaction, true);
				this.aQueue[0].objThem = this.Pathfinder.coDest;
			}
			else
			{
				interaction.bManual = objInteraction.bManual;
				this.QueueInteraction(this.Pathfinder.tilDest.coProps, interaction, true);
				this.aQueue[0].objThem = this.Pathfinder.tilDest.coProps;
				this.aQueue[0].strTargetPoint = objInteraction.strTargetPoint;
				this.aQueue[0].fTargetPointRange = 0f;
			}
		}
		return true;
	}

	public bool RemembersInteract(string strIAName)
	{
		if (strIAName == null || strIAName == string.Empty)
		{
			return false;
		}
		foreach (CondHistory condHistory in this.mapIAHist.Values)
		{
			if (condHistory.mapInteractions.ContainsKey(strIAName))
			{
				return true;
			}
		}
		return false;
	}

	public float GetNetInteractionResult(Interaction objInteraction, bool bVerbose = false)
	{
		float num = 0f;
		if (objInteraction == null)
		{
			return num;
		}
		CondHistory condHistory = null;
		InteractionHistory interactionHistory = null;
		objInteraction.aCondUsPriorities = new List<CondScore>();
		string[] array = objInteraction.strName.Split(new char[]
		{
			';'
		});
		foreach (Priority priority in this.aPriorities)
		{
			Condition condition;
			if (this.mapConds.TryGetValue(priority.objCond.strName, out condition))
			{
				condHistory = null;
				interactionHistory = null;
				this.mapIAHist.TryGetValue(priority.objCond.strName, out condHistory);
				if (condHistory != null && !condHistory.mapInteractions.TryGetValue(objInteraction.strName, out interactionHistory) && array.Length > 1)
				{
					condHistory.mapInteractions.TryGetValue(array[0], out interactionHistory);
				}
				if (interactionHistory != null)
				{
					CondScore condScore = new CondScore(priority.objCond.strName);
					condScore.fTotalValue = interactionHistory.fAverage * -(float)priority.fValue;
					num += condScore.fTotalValue;
					objInteraction.aCondUsPriorities.Add(condScore);
					if (bVerbose)
					{
						UnityEngine.Debug.Log(string.Concat(new object[]
						{
							"GetNetInteractionResult ",
							objInteraction.strName,
							": ",
							priority.ToString(),
							" = ",
							-priority.fValue,
							" * ",
							interactionHistory.fAverage
						}));
					}
				}
			}
		}
		objInteraction.aCondUsPriorities.Sort((CondScore x, CondScore y) => x.fTotalValue.CompareTo(y.fTotalValue));
		if (num == 0f && objInteraction.strActionGroup == "Ship")
		{
			num = 1f;
		}
		return num;
	}

	private float GetCOScore(CondOwner objCO, InteractionHistory objIH)
	{
		float num = 0f;
		foreach (CondScore condScore in objIH.mapScores.Values)
		{
			if (objCO.HasCond(condScore.strName))
			{
				num += condScore.fAverage;
			}
		}
		return num;
	}

	public void UpdateCondRecords(Condition objCond)
	{
		if (this.mapConds == null)
		{
			return;
		}
		if (objCond.fCount <= 0.0)
		{
			this.mapConds.Remove(objCond.strName);
			this.aCondsTimed.Remove(objCond);
			if (this.faceRef != null)
			{
				this.faceRef.RecordCond(objCond.strName, true);
			}
			MonoSingleton<GUIRenderTargets>.Instance.UpdateFaces(this, objCond.strName, true);
			if (objCond.bPersists && this.aCondZeroes.IndexOf(objCond.strName) < 0)
			{
				this.aCondZeroes.Add(objCond.strName);
			}
		}
		else if (objCond.fDuration > 0f)
		{
			this.mapConds[objCond.strName] = objCond;
			if (objCond.bPersists && this.aCondZeroes.IndexOf(objCond.strName) >= 0)
			{
				this.aCondZeroes.Remove(objCond.strName);
			}
		}
	}

	public void ZeroCondAmount(string strName)
	{
		this.AddCondAmount(strName, -this.GetCondAmount(strName), 0.0, 0f);
	}

	public void SetCondAmount(string strName, double dAmount, double dAge = 0.0)
	{
		double condAmount = this.GetCondAmount(strName);
		double fAmount = dAmount - condAmount;
		this.AddCondAmount(strName, fAmount, dAge, 0f);
	}

	public bool IsThreshold(string condName)
	{
		return condName != null && condName.Length >= 6 && (condName[0] == 'T' && condName[1] == 'h' && condName[2] == 'r' && condName[3] == 'e' && condName[4] == 's' && condName[5] == 'h');
	}

	public void AddCondAmount(string strName, double fAmount, double fAge = 0.0, float fCondRuleTrack = 0f)
	{
		if (this.bFreezeConds || strName == null || this.mapConds == null)
		{
			return;
		}
		if (fAmount == 0.0)
		{
			return;
		}
		if (double.IsNaN(fAmount))
		{
			return;
		}
		if (this.IsThreshold(strName))
		{
			if (this.bFreezeCondRules)
			{
				return;
			}
			CondRule condRule = null;
			string key = strName.Substring(6);
			this.mapCondRules.TryGetValue(key, out condRule);
			if (condRule != null)
			{
				condRule.ChangeThresh(this, fAmount);
			}
			return;
		}
		else
		{
			bool flag = false;
			Condition cond;
			if (this.mapConds.TryGetValue(strName, out cond))
			{
				flag = true;
			}
			else
			{
				cond = DataHandler.GetCond(strName);
			}
			if (cond == null)
			{
				return;
			}
			if (fAmount > 0.0 && cond.ctImmune != null && cond.ctImmune.Triggered(this, null, true))
			{
				return;
			}
			if (cond.strAnti != null && fAmount > 0.0)
			{
				bool isThreshold = this.IsThreshold(cond.strAnti);
				if (this.HasCond(cond.strAnti, isThreshold))
				{
					double condAmount = this.GetCondAmount(cond.strAnti, isThreshold);
					if (condAmount > fAmount)
					{
						this.AddCondAmount(cond.strAnti, -fAmount, 0.0, 0f);
						return;
					}
					this.ZeroCondAmount(cond.strAnti);
					fAmount -= condAmount;
				}
			}
			if (cond.bRoom)
			{
				if (this.ship != null)
				{
					Room roomAtWorldCoords = this.ship.GetRoomAtWorldCoords1(this.tf.position, true);
					if (roomAtWorldCoords != null && roomAtWorldCoords.CO != this)
					{
						roomAtWorldCoords.CO.AddCondAmount(strName, fAmount, fAge, 0f);
					}
				}
				this.AddCondAmount("IsRoomStat", fAmount, 0.0, 0f);
			}
			double num = cond.fCount;
			if (num < 0.0)
			{
				num = 0.0;
			}
			if (strName == "StatFatigue" && !this.bFreezeCondRules && this.HasCond("StatFatigueCoeff", false))
			{
				fAmount *= this.GetCondAmount("StatFatigueCoeff", false);
			}
			if (!flag && cond.bAlert && this.objCompany != null && this.objCompany == CrewSim.coPlayer.objCompany)
			{
				if ((double)Time.timeScale > 1.0)
				{
					CrewSim.ResetTimeScale();
				}
				if (CrewSim.GetSelectedCrew() != this)
				{
					CrewSim.GetSelectedCrew().LogMessage(CrewSim.GetSelectedCrew().strNameFriendly + " notices something is wrong with " + this.strNameFriendly, "Badish", CrewSim.GetSelectedCrew().strID);
				}
				if (CrewSim.GetSelectedCrew() != CrewSim.coPlayer && this != CrewSim.coPlayer)
				{
					CrewSim.coPlayer.LogMessage(CrewSim.coPlayer.strNameFriendly + " notices something is wrong with " + this.strNameFriendly, "Badish", CrewSim.coPlayer.strID);
				}
			}
			cond.AddAmount(this, fAmount);
			if (fAge != 0.0)
			{
				cond.SetAge((float)fAge + cond.GetAge());
			}
			if (cond.bCondRuleTrackAlways)
			{
				cond.fCondRuleTrack = cond.fCount - num;
			}
			else
			{
				cond.fCondRuleTrack = (double)fCondRuleTrack;
			}
			cond.fCondRuleTrackTime = StarSystem.fEpoch;
			if (strName == "StatPowerMax" && this.Pwr != null)
			{
				this.Pwr.ResetMaxPower();
			}
			if (!flag && fAmount > 0.0)
			{
				if (this.faceRef != null)
				{
					this.faceRef.RecordCond(cond.strName, false);
					if (cond.pairFaceSprite != null && this.Crew != null && cond.pairFaceSprite.nValue >= 0 && cond.pairFaceSprite.nValue < this.Crew.FaceParts.Length)
					{
						this.Crew.FaceParts[cond.pairFaceSprite.nValue] = cond.pairFaceSprite.strName;
						MonoSingleton<GUIRenderTargets>.Instance.SetFace(this, true);
					}
				}
				MonoSingleton<GUIRenderTargets>.Instance.UpdateFaces(this, cond.strName, false);
				if (cond.fDuration > 0f && !float.IsInfinity(cond.fDuration))
				{
					this.AddCondTicker(cond, fAge);
					this.aCondsTimed.Add(cond);
				}
				if (this.bLogConds && ((cond.nDisplaySelf == 2 && this == CrewSim.coPlayer) || (cond.nDisplayOther == 2 && this != CrewSim.coPlayer)))
				{
					this.LogMessage(GUIStatus.GetStatusText(cond, Color.white) + GrammarUtils.GetInflectedString(cond.strDesc, cond, this), cond.strColor, this.strName);
				}
				if (cond.bFatal)
				{
					UnityEngine.Debug.Log("Fatal cond " + cond.strName + " added to " + this.strID);
					this.Kill = true;
				}
				else if (!this.bFreezeCondRules && cond.bKO)
				{
					this.KO();
				}
			}
			if (flag)
			{
				if (cond.fCount <= 0.0)
				{
					if (this.faceRef != null && cond.pairFaceSprite != null && this.Crew != null && cond.pairFaceSprite.nValue >= 0 && cond.pairFaceSprite.nValue < this.Crew.FaceParts.Length)
					{
						this.Crew.FaceParts[cond.pairFaceSprite.nValue] = FaceAnim2.PartNameDefault(cond.pairFaceSprite.nValue);
						MonoSingleton<GUIRenderTargets>.Instance.SetFace(this, true);
					}
					if (cond.fDuration > 0f && !float.IsInfinity(cond.fDuration))
					{
						this.RemoveTicker(cond.strName);
						this.aCondsTimed.Remove(cond);
					}
					if (this.bLogConds && ((cond.nDisplaySelf == 2 && this == CrewSim.coPlayer) || (cond.nDisplayOther == 2 && this != CrewSim.coPlayer)))
					{
						this.LogMessage(GUIStatus.GetStatusText(cond, Color.white) + GrammarUtils.GetInflectedString(cond.strDesc, cond, this).Replace(" are ", " are no longer "), cond.strColor + "Remove", this.strName);
					}
					if (this.dictRemoveCondEvents.ContainsKey(cond.strName) && this.dictRemoveCondEvents[cond.strName] != null)
					{
						CondTrigger condTrigger = DataHandler.GetCondTrigger(this.dictRemoveCondEvents[cond.strName][1]);
						if (condTrigger.Triggered(this, null, true))
						{
							if (this.dictRemoveCondEvents[cond.strName][0] == "AddTask")
							{
								bool flag2 = true;
								bool.TryParse(this.dictRemoveCondEvents[cond.strName][4], out flag2);
								if (!flag2 || (this.ship != null && this.ship == CrewSim.shipPlayerOwned))
								{
									this.AddTask(this.dictRemoveCondEvents[cond.strName][2], this.dictRemoveCondEvents[cond.strName][3]);
								}
							}
							else if (this.dictRemoveCondEvents[cond.strName][0] == "RemoveTask")
							{
								CrewSim.objInstance.workManager.RemoveTask(this.dictRemoveCondEvents[cond.strName][2], this.dictRemoveCondEvents[cond.strName][3], this.strID);
							}
						}
					}
					if (cond.strName == "IsHuman" && this.Crew != null)
					{
						UnityEngine.Object.Destroy(this.Crew);
						UnityEngine.Object.Destroy(base.gameObject.GetComponent<Pathfinder>());
					}
					if (cond.strName == "IsReactorIC" && base.gameObject.GetComponent<FusionIC>() != null)
					{
						UnityEngine.Object.Destroy(base.gameObject.GetComponent<FusionIC>());
					}
					if (cond.strName == "IsAirtight" && this.GasContainer != null)
					{
						this.aManUpdates.Remove(this.GasContainer);
						Room roomAtWorldCoords2 = this.currentRoom;
						if (roomAtWorldCoords2 == null && this.ship != null)
						{
							roomAtWorldCoords2 = this.ship.GetRoomAtWorldCoords1(this.tf.position, false);
						}
						if (roomAtWorldCoords2 != null)
						{
							GasContainer.MergeGasContainersAndDestroy(this, roomAtWorldCoords2.CO);
						}
						else
						{
							GasContainer.MergeGasContainersAndDestroy(this, null);
						}
					}
					if (cond.strName == "IsPassiveRewarmer" && base.gameObject.GetComponent<BodyTemp>() != null)
					{
						this.aManUpdates.Remove(base.gameObject.GetComponent<BodyTemp>());
						UnityEngine.Object.DestroyImmediate(base.gameObject.GetComponent<BodyTemp>());
					}
					if (cond.strName == "IsCrewArmLLamp" && this.Crew != null)
					{
						this.Crew.ArmLLamp = cond.fCount;
					}
					if (cond.strName == "IsCrewArmRLamp" && this.Crew != null)
					{
						this.Crew.ArmRLamp = cond.fCount;
					}
					if (cond.strName == "IsCrewHandLLamp" && this.Crew != null)
					{
						this.Crew.HandLLamp = cond.fCount;
					}
					if (cond.strName == "IsCrewHandRLamp" && this.Crew != null)
					{
						this.Crew.HandRLamp = cond.fCount;
					}
					if (cond.strName == "IsCrewHeadLamp" && this.Crew != null)
					{
						this.Crew.HeadLamp = cond.fCount;
					}
					if (cond.strName == "IsCrewToolSpark" && this.Crew != null)
					{
						this.Crew.Sparks = false;
					}
					if (cond.strName == "IsCCTV" && base.gameObject.GetComponentInChildren<CCTV>() != null)
					{
						CCTV componentInChildren = base.gameObject.GetComponentInChildren<CCTV>();
						componentInChildren.transform.SetParent(null);
						UnityEngine.Object.Destroy(componentInChildren);
					}
					if (cond.strName == "IsTraderNPC" && base.gameObject.GetComponent<Trader>() != null)
					{
						UnityEngine.Object.Destroy(base.gameObject.GetComponent<Trader>());
					}
					if (cond.bQABRefresh && (CrewSim.GetSelectedCrew() == this || GUIMegaToolTip.Selected == this))
					{
						MonoSingleton<GUIQuickBar>.Instance.BuildButtonList(false);
					}
					if (!this.bFreezeCondRules && cond.bKO)
					{
						this.KOWake();
					}
				}
				else if (fAge != 0.0 && !double.IsInfinity((double)cond.fDuration))
				{
					this.RemoveTicker(cond.strName);
					this.AddCondTicker(cond, fAge);
				}
			}
			else if (!flag && cond.fCount > 0.0)
			{
				if (this.dictAddCondEvents.ContainsKey(cond.strName) && this.dictAddCondEvents[cond.strName] != null)
				{
					CondTrigger condTrigger2 = DataHandler.GetCondTrigger(this.dictAddCondEvents[cond.strName][1]);
					if (condTrigger2.Triggered(this, null, true))
					{
						if (this.dictAddCondEvents[cond.strName][0] == "AddTask")
						{
							bool flag3 = true;
							bool.TryParse(this.dictAddCondEvents[cond.strName][4], out flag3);
							if (!flag3 || (this.ship != null && this.ship == CrewSim.shipPlayerOwned))
							{
								this.AddTask(this.dictAddCondEvents[cond.strName][2], this.dictAddCondEvents[cond.strName][3]);
							}
						}
						else if (this.dictAddCondEvents[cond.strName][0] == "RemoveTask")
						{
							CrewSim.objInstance.workManager.RemoveTask(this.dictAddCondEvents[cond.strName][2], this.dictAddCondEvents[cond.strName][3], this.strID);
						}
					}
				}
				if ((cond.strName == "IsHuman" || cond.strName == "IsRobot") && this.Crew == null)
				{
					this._pfComponentReference = base.gameObject.AddComponent<Pathfinder>();
					Crew crew = (!(cond.strName == "IsHuman")) ? base.gameObject.AddComponent<Robot>() : base.gameObject.AddComponent<Crew>();
					this._crewComponentReference = crew;
					crew.SetData(this.strItemDef, 0f, 0f, 0f);
					base.gameObject.AddComponent<AwaitsReplyObserver>();
				}
				if (cond.strName == "IsReactorIC" && base.gameObject.GetComponent<FusionIC>() == null)
				{
					base.gameObject.AddComponent<FusionIC>();
				}
				if (cond.strName == "IsAirtight")
				{
					GasContainer gasContainer = base.gameObject.AddComponent<GasContainer>();
					Room roomAtWorldCoords3 = this.currentRoom;
					if (roomAtWorldCoords3 == null && this.ship != null)
					{
						roomAtWorldCoords3 = this.ship.GetRoomAtWorldCoords1(this.tf.position, false);
					}
					if (roomAtWorldCoords3 != null)
					{
						gasContainer.CarveNewGasContainerFromRoom(roomAtWorldCoords3.CO, this);
					}
					this.aManUpdates.Add(gasContainer);
				}
				if (cond.strName == "IsPassiveRewarmer" && base.gameObject.GetComponent<BodyTemp>() == null)
				{
					BodyTemp item = base.gameObject.AddComponent<BodyTemp>();
					this.aManUpdates.Add(item);
				}
				if (cond.strName == "IsCrewArmLLamp" && this.Crew != null)
				{
					this.Crew.ArmLLamp = cond.fCount;
				}
				if (cond.strName == "IsCrewArmRLamp" && this.Crew != null)
				{
					this.Crew.ArmRLamp = cond.fCount;
				}
				if (cond.strName == "IsCrewHandLLamp" && this.Crew != null)
				{
					this.Crew.HandLLamp = cond.fCount;
				}
				if (cond.strName == "IsCrewHandRLamp" && this.Crew != null)
				{
					this.Crew.HandRLamp = cond.fCount;
				}
				if (cond.strName == "IsCrewHeadLamp" && this.Crew != null)
				{
					this.Crew.HeadLamp = cond.fCount;
				}
				if (cond.strName == "IsCrewToolSpark" && this.Crew != null)
				{
					this.Crew.Sparks = true;
				}
				if (cond.strName == "IsCCTV" && base.gameObject.GetComponentInChildren<CCTV>() == null)
				{
					GameObject original = (GameObject)Resources.Load("prefabCCTV");
					Transform transform = UnityEngine.Object.Instantiate<GameObject>(original, this.tf).transform;
				}
				if (cond.strName == "IsTraderNPC" && base.gameObject.GetComponent<Trader>() == null)
				{
					base.gameObject.AddComponent<Trader>();
				}
				if (cond.strName == "IsMarketActor" && base.gameObject.GetComponent<MarketActor>() == null)
				{
					base.gameObject.AddComponent<MarketActor>();
				}
				if (cond.strName.Length >= 7 && cond.strName.Substring(0, 7) == "Trigger")
				{
					if (this.Pathfinder != null)
					{
						this.Pathfinder.AddTriggerListener(new UnityAction<JsonZone>(this.OnZoneTriggerEntered));
					}
					else
					{
						UnityEngine.Debug.Log("Tried to add zone trigger to non pathfinder: " + this.strName);
					}
				}
				if (cond.strName == "Prone")
				{
					this.SetAnimState(Interaction.dictAnims["Fallen"]);
					this.strIdleAnim = "Fallen";
					this.SetAnimTrigger("FallenTrigger");
					if (this.aQueue.Count > 0 && this.aQueue[0] != null)
					{
						this.aQueue[0].strAnim = "Fallen";
						this.aQueue[0].strIdleAnim = "Fallen";
					}
					if (CrewSim.GetSelectedCrew() == this && GUIMegaToolTip.Selected == null)
					{
						CrewSim.OnRightClick.Invoke(new List<CondOwner>
						{
							this
						});
					}
				}
				if (cond.bQABRefresh && (CrewSim.GetSelectedCrew() == this || GUIMegaToolTip.Selected == this))
				{
					MonoSingleton<GUIQuickBar>.Instance.BuildButtonList(false);
				}
			}
			this.UpdatePriority(cond);
			CondRule condRule2;
			if (!this.bFreezeCondRules && this.mapCondRules.TryGetValue(cond.strName, out condRule2))
			{
				condRule2.ChangeStat(this, num, cond.fCount);
			}
			if (this.aDestructableConds.Contains(cond.strName))
			{
				Destructable component = base.gameObject.GetComponent<Destructable>();
				if (component != null)
				{
					component.ScheduleDamageCheck();
				}
			}
			return;
		}
	}

	private void AddCondTicker(Condition objCond, double fAge)
	{
		if (objCond == null || double.IsInfinity((double)objCond.fDuration))
		{
			return;
		}
		JsonTicker jsonTicker = new JsonTicker();
		jsonTicker.strCondUpdate = objCond.strName;
		jsonTicker.bRepeat = false;
		jsonTicker.fPeriod = Convert.ToDouble(objCond.fDuration);
		jsonTicker.SetTimeLeft(jsonTicker.fPeriod - fAge);
		jsonTicker.strName = objCond.strName;
		this.AddTicker(jsonTicker);
	}

	public bool HasCond(string strName, bool isThreshold)
	{
		if (this.mapConds == null)
		{
			return false;
		}
		if (isThreshold)
		{
			CondRule condRule = null;
			string key = strName.Substring(6);
			this.mapCondRules.TryGetValue(key, out condRule);
			return condRule != null;
		}
		return this.mapConds.ContainsKey(strName);
	}

	public bool HasCond(string strName)
	{
		return this.HasCond(strName, this.IsThreshold(strName));
	}

	public double GetCondAmount(string strName)
	{
		return this.GetCondAmount(strName, this.IsThreshold(strName));
	}

	public double GetCondAmount(string strName, bool isThreshold)
	{
		if (strName == null || this.mapConds == null)
		{
			return 0.0;
		}
		if (isThreshold)
		{
			CondRule condRule = null;
			string key = strName.Substring(6);
			this.mapCondRules.TryGetValue(key, out condRule);
			if (condRule != null)
			{
				return condRule.Modifier;
			}
			return 0.0;
		}
		else
		{
			Condition condition = null;
			if (this.mapConds.TryGetValue(strName, out condition))
			{
				return condition.fCount;
			}
			return 0.0;
		}
	}

	private void OnZoneTriggerEntered(JsonZone jz)
	{
		if (jz == null)
		{
			return;
		}
		foreach (string str in jz.categoryConds)
		{
			if (this.HasCond(str))
			{
				UnityEngine.Debug.Log("Zone triggered: " + str);
				JsonZoneTrigger zoneTrigger = DataHandler.GetZoneTrigger(str);
				if (zoneTrigger != null)
				{
					if (zoneTrigger.strRunEncounter != null)
					{
						BeatManager.RunEncounter(zoneTrigger.strRunEncounter, zoneTrigger.bRunEncounterInterrupt);
					}
					if (zoneTrigger.strApplyInteractionChain != null)
					{
						Interaction interaction = DataHandler.GetInteraction(zoneTrigger.strApplyInteractionChain, null, false);
						if (interaction != null)
						{
							interaction.ApplyChain(null);
						}
					}
					if (zoneTrigger.strQueueInteraction != null)
					{
						Interaction interaction2 = DataHandler.GetInteraction(zoneTrigger.strQueueInteraction, null, false);
						if (interaction2 != null && this.ship != null)
						{
							List<CondOwner> cos = this.ship.GetCOs(interaction2.CTTestThem, true, true, false);
							if (cos.Count > 0 && interaction2.Triggered(this, cos[0], false, false, false, true, null))
							{
								this.QueueInteraction(cos[0], interaction2, zoneTrigger.bQueueInteractionInsert);
							}
						}
					}
					if (zoneTrigger.bRemoveOnTrigger)
					{
						if (GUIZones.instance != null)
						{
							GUIZones.instance.DeleteZone(jz.strName);
						}
						else
						{
							Ship loadedShipByRegId = CrewSim.GetLoadedShipByRegId(jz.strRegID);
							if (loadedShipByRegId != null)
							{
								GUIZones.DeleteTilesFromZone(jz, false);
								loadedShipByRegId.mapZones.Remove(jz.strName);
							}
						}
					}
				}
			}
		}
	}

	public double GetTotalMass()
	{
		double num = this.GetCondAmount("StatMass");
		foreach (CondOwner condOwner in this.aStack)
		{
			num += condOwner.GetTotalMass();
		}
		return num;
	}

	public string GetDiscomfortForCond(string strCond)
	{
		if (strCond == null || this.mapConds == null)
		{
			return null;
		}
		if (this.mapCondRules.ContainsKey(strCond))
		{
			foreach (CondRuleThresh condRuleThresh in this.mapCondRules[strCond].aThresholds)
			{
				Loot loot = DataHandler.GetLoot(condRuleThresh.strLootNew);
				List<string> lootNames = loot.GetLootNames(null, true, null);
				foreach (string text in lootNames)
				{
					if (this.mapConds.ContainsKey(text))
					{
						return text;
					}
				}
			}
		}
		return null;
	}

	public void AddCondRule(string strCondRule, bool bApplyEffects = true)
	{
		if (string.IsNullOrEmpty(strCondRule))
		{
			return;
		}
		bool flag = strCondRule.IndexOf("-") == 0;
		CondRule condRule = (!flag) ? CondRule.LoadSaveInfo(strCondRule) : CondRule.LoadSaveInfo(strCondRule.Substring(1));
		if (condRule == null)
		{
			UnityEngine.Debug.Log(string.Concat(new string[]
			{
				"Cannot ",
				(!flag) ? "add" : "remove",
				" Condrule ",
				strCondRule,
				" on CO ",
				this.strName
			}));
			return;
		}
		if (flag)
		{
			if (bApplyEffects)
			{
				this.AddCondRuleEffects(condRule, -1f);
			}
			this.mapCondRules.Remove(condRule.strCond);
			if (condRule.fPref != 0.0)
			{
				this.hashCondsImportant.Remove(condRule.strCond);
			}
		}
		else
		{
			this.mapCondRules[condRule.strCond] = condRule;
			if (bApplyEffects)
			{
				this.AddCondRuleEffects(condRule, 1f);
			}
			if (condRule.fPref != 0.0)
			{
				this.hashCondsImportant.Add(condRule.strCond);
			}
		}
	}

	private void AddCondRuleEffects(CondRule cr, float fCoeff)
	{
		if (cr == null)
		{
			return;
		}
		CondRuleThresh currentThresh = cr.GetCurrentThresh(this);
		if (currentThresh != null)
		{
			Loot loot = DataHandler.GetLoot(currentThresh.strLootNew);
			if (loot.strName != "Blank")
			{
				loot.ApplyCondLoot(this, currentThresh.fMinAdd * fCoeff, null, 0f);
			}
		}
	}

	public CondRule GetCondRule(string strCond)
	{
		if (strCond == null || this.mapConds == null)
		{
			return null;
		}
		if (this.mapCondRules.ContainsKey(strCond))
		{
			return this.mapCondRules[strCond];
		}
		return null;
	}

	public Interaction GetInteraction(string strN = null, CondOwner objTarget = null)
	{
		if (strN == null && objTarget == null)
		{
			return null;
		}
		foreach (Interaction interaction in this.aQueue)
		{
			if (interaction.strName == strN)
			{
				if (objTarget == null)
				{
					return interaction;
				}
				if (objTarget == interaction.objThem)
				{
					return interaction;
				}
			}
			if (objTarget == interaction.objThem)
			{
				if (strN == null)
				{
					return interaction;
				}
				if (strN == interaction.strName)
				{
					return interaction;
				}
			}
		}
		return null;
	}

	public Interaction GetInteractionCurrent()
	{
		if (this.aQueue == null)
		{
			return null;
		}
		if (this.aQueue.Count == 0)
		{
			return null;
		}
		return this.aQueue[0];
	}

	public CondTrigger GetCTForThis()
	{
		CondTrigger condTrigger = new CondTrigger();
		condTrigger.strName = this.strName;
		List<string> list = new List<string>();
		foreach (string text in this.mapConds.Keys)
		{
			if (text.IndexOf("Is") == 0)
			{
				list.Add(text);
			}
		}
		condTrigger.aReqs = list.ToArray();
		return condTrigger;
	}

	public void AddLotCO(CondOwner co)
	{
		if (this.aLot.IndexOf(co) < 0)
		{
			if (co.objCOParent != null)
			{
				co.RemoveFromCurrentHome(false);
			}
			this.aLot.Add(co);
		}
		if (co == this)
		{
			UnityEngine.Debug.Log("ERROR: Assigning self as own parent.");
		}
		co.objCOParent = this;
		co.ship = this.ship;
		co.tf.SetParent(this.tf, true);
		co.Visible = false;
		if (this.strPersistentCO == null)
		{
			this.strPersistentCO = co.strID;
		}
	}

	public CondOwner RemoveLotCO(CondOwner co)
	{
		if (this.aLot.IndexOf(co) < 0)
		{
			return null;
		}
		this.aLot.Remove(co);
		co.objCOParent = null;
		if (co.ship != null)
		{
			co.ship.RemoveCO(co, true);
		}
		else
		{
			co.tf.SetParent(null, true);
		}
		if (this.strPersistentCO == co.strID)
		{
			this.strPersistentCO = null;
		}
		return co;
	}

	public List<CondOwner> GetLotCOs(bool bSubItems)
	{
		List<CondOwner> list = new List<CondOwner>();
		if (this.aLot != null)
		{
			list.AddRange(this.aLot);
			if (bSubItems)
			{
				foreach (CondOwner condOwner in this.aLot)
				{
					CondOwner.NullSafeAddRange(ref list, condOwner.GetCOs(true, null));
					CondOwner.NullSafeAddRange(ref list, condOwner.GetLotCOs(true));
				}
			}
		}
		return list;
	}

	public CondOwner AddCO(CondOwner objCO, bool bEquip, bool bOverflow, bool bIgnoreLocks)
	{
		if (objCO == null || this.bDestroyed)
		{
			return null;
		}
		if (this.compSlots != null)
		{
			foreach (string strSlot in objCO.mapSlotEffects.Keys)
			{
				Slot slot = this.compSlots.GetSlot(strSlot);
				if (slot != null)
				{
					if ((bEquip || slot.bHoldSlot) && this.compSlots.SlotItem(strSlot, objCO, true))
					{
						return null;
					}
				}
			}
		}
		CondOwner condOwner = this.StackCO(objCO);
		if (condOwner != null && !bOverflow)
		{
			return condOwner;
		}
		if (this.objContainer != null && (bIgnoreLocks || !this.objContainer.Locked))
		{
			condOwner = this.objContainer.AddCO(condOwner);
		}
		if (condOwner != null)
		{
			foreach (Slot slot2 in this.GetSlots(false, Slots.SortOrder.HELD_FIRST))
			{
				condOwner = slot2.AddCO(condOwner, false, true, bIgnoreLocks);
				if (condOwner == null)
				{
					break;
				}
			}
		}
		return condOwner;
	}

	public Ship RemoveFromCurrentHome(bool bForce = false)
	{
		Ship ship = this.ship;
		if (this.objCOParent != null)
		{
			if (this.objCOParent.aLot.IndexOf(this) >= 0)
			{
				this.objCOParent.RemoveLotCO(this);
				CondOwner.CheckTrue(this.objCOParent == null, "Failed to remove from parent.");
				CondOwner.CheckTrue(this.ship == null, "Failed to remove from ship.");
				return ship;
			}
			this.objCOParent.RemoveCO(this, bForce);
			CondOwner.CheckTrue(this.objCOParent == null, "Failed to remove from parent.");
			CondOwner.CheckTrue(this.ship == null, "Failed to remove from ship.");
			return ship;
		}
		else
		{
			if (this.ship != null)
			{
				if (this.ship.GetType() == typeof(BarterZoneShip))
				{
					((BarterZoneShip)this.ship).RemoveCO(this, false);
				}
				else
				{
					this.ship.RemoveCO(this, bForce);
				}
				CondOwner.CheckTrue(this.ship == null, "Failed to remove from ship.");
				return ship;
			}
			return null;
		}
	}

	public CondOwner RemoveCO(CondOwner objCO, bool bForce = false)
	{
		if (objCO == null || objCO == this || this.bDestroyed)
		{
			return null;
		}
		if (this.compSlots != null)
		{
			if (objCO.slotNow != null)
			{
				CondOwner condOwner = this.compSlots.UnSlotItem(objCO.slotNow.strName, objCO, bForce);
				if (condOwner != null)
				{
					return condOwner;
				}
			}
			else
			{
				foreach (Slot slot in this.GetSlots(false, Slots.SortOrder.HELD_FIRST))
				{
					if (slot != null)
					{
						CondOwner condOwner2 = slot.RemoveCO(objCO, bForce);
						if (condOwner2 != null)
						{
							return condOwner2;
						}
					}
				}
			}
		}
		int num = this.aStack.IndexOf(objCO);
		if (num >= 0)
		{
			if (objCO.ship != null)
			{
				if (objCO == this)
				{
					UnityEngine.Debug.Log("ERROR: Assigning self as own parent.");
				}
				objCO.objCOParent = this;
				objCO.ship.RemoveCO(objCO, bForce);
			}
			objCO.objCOParent = null;
			objCO.coStackHead = null;
			Item item = objCO.Item;
			item.fLastRotation = this.tf.rotation.eulerAngles.z;
			objCO.tf.position = new Vector3(this.tf.position.x, this.tf.position.y, this.tf.position.z);
			objCO.tf.SetParent(null, true);
			if (this.objCOParent != null)
			{
				this.objCOParent.AddMass(-objCO.GetCondAmount("StatMass"), false);
			}
			objCO.UpdateAppearance();
			this.UpdateAppearance();
			return objCO;
		}
		if (this.objContainer != null)
		{
			CondOwner condOwner3 = this.objContainer.RemoveCO(objCO, bForce);
			if (condOwner3 != null)
			{
				this.UpdateAppearance();
				return condOwner3;
			}
		}
		num = this.aLot.IndexOf(objCO);
		if (num >= 0)
		{
			return this.RemoveLotCO(objCO);
		}
		return null;
	}

	public CondOwner RootParent(string strCond = null)
	{
		if (this.AreWeGettingDragged(this, this.objCOParent))
		{
			return null;
		}
		CondOwner condOwner = this.objCOParent;
		CondOwner condOwner2 = null;
		while (condOwner != null)
		{
			if (strCond == null || condOwner.HasCond(strCond))
			{
				condOwner2 = condOwner;
			}
			condOwner = condOwner.objCOParent;
			if (this.AreWeGettingDragged(condOwner2, condOwner))
			{
				return condOwner2;
			}
		}
		return condOwner2;
	}

	private bool AreWeGettingDragged(CondOwner us, CondOwner them)
	{
		return !(us == null) && !(them == null) && (us.Crew != null && them.Crew != null);
	}

	public Slot GetSlotParent()
	{
		CondOwner condOwner = this.objCOParent;
		while (condOwner != null)
		{
			if (condOwner.slotNow != null)
			{
				return condOwner.slotNow;
			}
			condOwner = condOwner.objCOParent;
		}
		return null;
	}

	public CondOwner DropCO(CondOwner objCO, bool bAllowLocked, Ship objShipRef = null, float xOffset = 0f, float yOffset = 0f, bool dropInContainersLast = true, Func<int[], int[]> sortingProvider = null)
	{
		if (objCO == null)
		{
			return objCO;
		}
		if (objShipRef == null)
		{
			objShipRef = this.ship;
		}
		if (objShipRef == null)
		{
			return objCO;
		}
		JsonZone zoneFromTileRadius;
		if (xOffset != 0f || yOffset != 0f)
		{
			zoneFromTileRadius = TileUtils.GetZoneFromTileRadius(objShipRef, new Vector3(this.tf.position.x + xOffset, this.tf.position.y + yOffset, this.tf.position.z), 2, true, false);
		}
		else
		{
			zoneFromTileRadius = TileUtils.GetZoneFromTileRadius(objShipRef, this.tf.position, 2, true, false);
		}
		Tile tileAtWorldCoords = objShipRef.GetTileAtWorldCoords1(this.tf.position.x + xOffset, this.tf.position.y + yOffset, true, true);
		if (tileAtWorldCoords != null && tileAtWorldCoords.jZone != null)
		{
			List<int> list = new List<int>();
			foreach (int num in tileAtWorldCoords.jZone.aTiles)
			{
				if (Array.IndexOf<int>(zoneFromTileRadius.aTiles, num) >= 0)
				{
					list.Add(num);
				}
			}
			zoneFromTileRadius.aTiles = list.ToArray();
		}
		this.RemoveCO(objCO, false);
		CondTrigger condTrigger = DataHandler.GetCondTrigger("TIsLootSpawnOK");
		List<CondOwner> cosInZone = objShipRef.GetCOsInZone(zoneFromTileRadius, condTrigger, bAllowLocked, true);
		cosInZone.Remove(this);
		List<int> list2 = new List<int>();
		foreach (int num2 in zoneFromTileRadius.aTiles)
		{
			Vector2 worldCoordsAtTileIndex = objShipRef.GetWorldCoordsAtTileIndex1(num2);
			if (Visibility.IsCondOwnerLOSVisibleBlocks(this, worldCoordsAtTileIndex, true, false))
			{
				list2.Add(num2);
			}
		}
		zoneFromTileRadius.aTiles = list2.ToArray();
		Vector2 where = this.tf.position.ToVector2();
		for (int k = cosInZone.Count - 1; k >= 0; k--)
		{
			if (!Visibility.IsCondOwnerLOSVisibleBlocks(cosInZone[k], where, false, false))
			{
				cosInZone.RemoveAt(k);
			}
		}
		if (sortingProvider != null && zoneFromTileRadius.aTiles.Length > 0)
		{
			zoneFromTileRadius.aTiles = (sortingProvider(zoneFromTileRadius.aTiles) ?? zoneFromTileRadius.aTiles);
		}
		List<CondOwner> list3 = null;
		if (dropInContainersLast)
		{
			List<CondOwner> aNearbyCOs = (from x in cosInZone
			where x.objContainer == null && x.GetSlots(false, Slots.SortOrder.HELD_FIRST).Count == 0
			select x).ToList<CondOwner>();
			list3 = TileUtils.DropCOsNearby(new List<CondOwner>
			{
				objCO
			}, objShipRef, zoneFromTileRadius, aNearbyCOs, condTrigger, bAllowLocked, true);
		}
		if (list3 == null || list3.Count != 0 || !dropInContainersLast)
		{
			list3 = TileUtils.DropCOsNearby(new List<CondOwner>
			{
				objCO
			}, objShipRef, zoneFromTileRadius, cosInZone, condTrigger, bAllowLocked, true);
		}
		CondOwner result = null;
		foreach (CondOwner objCO2 in list3)
		{
			result = this.AddCO(objCO2, false, true, true);
		}
		return result;
	}

	public CondOwner GetCORef(CondOwner objCO)
	{
		if (this == null)
		{
			UnityEngine.Debug.Log("ERROR: Getting CO from a null");
			UnityEngine.Debug.Break();
			return null;
		}
		if (objCO == null)
		{
			return null;
		}
		if (objCO == this)
		{
			return this;
		}
		if (objCO.strCODef == this.strCODef && this.aStack.IndexOf(objCO) >= 0)
		{
			return objCO;
		}
		CondOwner condOwner = null;
		if (this.objContainer != null)
		{
			condOwner = this.objContainer.GetCORef(objCO);
		}
		if (condOwner != null)
		{
			return condOwner;
		}
		foreach (Slot slot in this.GetSlots(false, Slots.SortOrder.HELD_FIRST))
		{
			if (slot != null)
			{
				condOwner = slot.GetCORef(objCO);
				if (condOwner != null)
				{
					return condOwner;
				}
			}
		}
		return null;
	}

	public int CanStackOnItem(CondOwner objIncoming)
	{
		if (objIncoming == null)
		{
			return 0;
		}
		if (objIncoming == this)
		{
			return 0;
		}
		if (this.strCODef != objIncoming.strCODef)
		{
			return 0;
		}
		if (this.bDestroyed || objIncoming.bDestroyed)
		{
			return 0;
		}
		int val = Math.Max(this.nStackLimit - this.StackCount, 0);
		if (this.bFreezeConds)
		{
			val = objIncoming.StackCount;
		}
		return Math.Min(objIncoming.StackCount, val);
	}

	public static CondOwner StackFromList(List<CondOwner> aStack)
	{
		if (aStack.Count == 0)
		{
			return null;
		}
		foreach (CondOwner condOwner in aStack)
		{
			if (condOwner == null || condOwner.aStack == null)
			{
				UnityEngine.Debug.LogError("Error: Null entry in stack");
			}
			else
			{
				condOwner.aStack.Clear();
				condOwner.coStackHead = null;
			}
		}
		CondOwner condOwner2 = aStack[aStack.Count - 1];
		foreach (CondOwner condOwner3 in aStack)
		{
			if (condOwner3 == null || condOwner2 == null)
			{
				UnityEngine.Debug.LogError("Error: Null entry in stack");
			}
			else if (condOwner3 != condOwner2)
			{
				condOwner2.aStack.Add(condOwner3);
				condOwner3.coStackHead = condOwner2;
			}
		}
		CondOwner condOwner4 = aStack[aStack.Count - 1];
		foreach (CondOwner condOwner5 in aStack)
		{
			if (condOwner5 == null || condOwner4 == null)
			{
				UnityEngine.Debug.LogError("Error: Null entry in stack");
			}
			else
			{
				if (condOwner5 != condOwner4)
				{
					condOwner5.tf.SetParent(condOwner4.tf, true);
					condOwner5.tf.localPosition = new Vector3(0f, 0f, Container.fZSubOffset);
					condOwner5.Visible = false;
				}
				condOwner5.UpdateAppearance();
			}
		}
		return condOwner2;
	}

	public CondOwner PopHeadFromStack()
	{
		if (this.aStack.Count == 0)
		{
			this.RemoveFromCurrentHome(false);
			return null;
		}
		bool visible = this.Visible;
		Transform parent = this.tf.parent;
		Container container = null;
		PairXY pairXY = default(PairXY);
		Slot slot = this.slotNow;
		Ship ship = this.ship;
		if (this.objCOParent != null)
		{
			container = this.objCOParent.objContainer;
			pairXY.x = this.pairInventoryXY.x;
			pairXY.y = this.pairInventoryXY.y;
		}
		this.ValidateParent();
		this.RemoveFromCurrentHome(false);
		List<CondOwner> stackAsList = this.StackAsList;
		CondOwner.CheckTrue(stackAsList[stackAsList.Count - 1] == this, "top of stack is no longer head");
		stackAsList.RemoveAt(stackAsList.Count - 1);
		this.aStack.Clear();
		CondOwner result = CondOwner.StackFromList(stackAsList);
		if (stackAsList.Count > 0)
		{
			CondOwner condOwner = stackAsList[stackAsList.Count - 1];
			condOwner.ValidateParent();
			condOwner.tf.position = new Vector3(this.tf.position.x, this.tf.position.y, this.tf.position.z);
			if (container != null)
			{
				container.AddCOSimple(condOwner, pairXY);
				container.Redraw();
			}
			else if (slot != null && slot.compSlots != null)
			{
				slot.compSlots.SlotItem(slot.strName, condOwner, true);
			}
			else
			{
				if (ship != null)
				{
					ship.AddCO(condOwner, visible);
				}
				else
				{
					condOwner.tf.SetParent(parent, true);
				}
				condOwner.Visible = visible;
			}
			condOwner.UpdateAppearance();
			condOwner.ValidateParent();
		}
		this.UpdateAppearance();
		this.ValidateParent();
		return result;
	}

	public CondOwner StackCO(CondOwner objCO)
	{
		if (this.tf == null)
		{
			UnityEngine.Debug.LogError(string.Concat(new object[]
			{
				"ERROR: Trying to stack ",
				objCO,
				" on null object ",
				this.strName
			}));
			return objCO;
		}
		this.ValidateParentRecursive();
		if (objCO == null)
		{
			return null;
		}
		objCO.ValidateParentRecursive();
		if (this.coStackHead != null)
		{
			return this.coStackHead.StackCO(objCO);
		}
		if (objCO.coStackHead != null)
		{
			return this.StackCO(objCO.coStackHead);
		}
		int num = this.CanStackOnItem(objCO);
		if (num == 0)
		{
			return objCO;
		}
		if (this.slotNow != null && this.slotNow.compSlots != null && this.slotNow.compSlots.SlotItem(this.slotNow.strName, objCO, true))
		{
			return null;
		}
		Vector3 position = objCO.tf.position;
		Transform parent = objCO.tf.parent;
		bool visible = objCO.Visible;
		Vector3 position2 = this.tf.position;
		Transform parent2 = this.tf.parent;
		bool visible2 = this.Visible;
		List<CondOwner> stackAsList = this.StackAsList;
		List<CondOwner> stackAsList2 = objCO.StackAsList;
		List<CondOwner> list = new List<CondOwner>();
		List<CondOwner> list2 = new List<CondOwner>();
		list.AddRange(stackAsList);
		list.AddRange(stackAsList2.GetRange(stackAsList2.Count - num, num));
		list2 = stackAsList2.GetRange(0, stackAsList2.Count - num);
		foreach (CondOwner condOwner in list)
		{
			if (!(condOwner == null) && !condOwner.bDestroyed)
			{
				condOwner.aStack.Clear();
				condOwner.coStackHead = null;
			}
		}
		foreach (CondOwner condOwner2 in list2)
		{
			if (!condOwner2.bDestroyed)
			{
				condOwner2.aStack.Clear();
				condOwner2.coStackHead = null;
			}
		}
		foreach (CondOwner isInOurStack in list)
		{
			this.SetIsInOurStack(isInOurStack);
		}
		CondOwner condOwner3 = CondOwner.StackFromList(list);
		CondOwner condOwner4 = CondOwner.StackFromList(list2);
		if (condOwner4 != null)
		{
			condOwner4.Visible = visible;
			condOwner4.tf.position = position;
			condOwner4.tf.SetParent(parent, true);
			condOwner4.UpdateAppearance();
		}
		condOwner3.Visible = visible2;
		condOwner3.tf.position = position2;
		condOwner3.tf.SetParent(parent2, true);
		condOwner3.UpdateAppearance();
		return condOwner4;
	}

	private void SetIsInOurStack(CondOwner co)
	{
		if (co == null)
		{
			UnityEngine.Debug.LogWarning("Warning: Attempting to set null in stack of " + this.strName);
			return;
		}
		co.tf.localPosition = new Vector3(0f, 0f, Container.fZSubOffset);
		if (co.ship != this.ship)
		{
			if (co.ship != null)
			{
				co.ship.RemoveCO(co, false);
			}
			if (this.ship != null)
			{
				this.ship.AddCO(co, false);
			}
		}
		if (co.objCOParent != this.objCOParent)
		{
			if (co.objCOParent && co.objCOParent.objContainer != null)
			{
				co.objCOParent.objContainer.RemoveCOSimple(co);
			}
			if (this.objCOParent != null && this.objCOParent.objContainer != null && !this.objCOParent.objContainer.Contains(co))
			{
				this.objCOParent.objContainer.AddCOSimple(co, this.pairInventoryXY);
			}
		}
		if (co == this.objCOParent)
		{
			UnityEngine.Debug.Log("ERROR: Assigning self as own parent.");
		}
		co.objCOParent = this.objCOParent;
	}

	public void UpdateAppearance()
	{
		if (this == null || base.gameObject == null)
		{
			if (this == null)
			{
				UnityEngine.Debug.Log("ERROR: Called UpdateAppearance a null");
			}
			else
			{
				UnityEngine.Debug.Log("ERROR: Called UpdateAppearanceon a null " + this.strCODef + ":" + this.strName);
			}
			UnityEngine.Debug.Break();
			return;
		}
		string alt = string.Empty;
		if (this.mapAltItemDefs != null && this.mapAltItemDefs.Count > 0 && this.objContainer != null && this.Item != null)
		{
			alt = this.objContainer.GetAltImageMatch(this.mapAltItemDefs);
			this.Item.SetAlt(alt);
			GUIInventoryItem inventoryItemFromCO = GUIInventory.GetInventoryItemFromCO(this);
			if (inventoryItemFromCO != null)
			{
				GUIInventoryWindow windowData = inventoryItemFromCO.windowData;
				windowData.RemoveAndDestroy(this.strID);
				GUIInventoryItem.SpawnInventoryItem(this.strID, windowData);
			}
			this.Item.VisualizeOverlays(false);
		}
		if (this.slotNow != null && CrewSim.inventoryGUI.IsCOShown(this.RootParent("IsHuman")))
		{
			CrewSim.inventoryGUI.PaperDollManager.UpdatePaperDollImage(this);
		}
		if (this.txtStack == null)
		{
			if (this.StackCount <= 1)
			{
				return;
			}
			this.txtStack = ((GameObject)UnityEngine.Object.Instantiate(Resources.Load("txtStack"), this.tf)).GetComponent<TMP_Text>();
		}
		this.txtStack.transform.rotation = Quaternion.identity;
		this.txtStack.text = "x" + this.StackCount;
		bool flag = this.Visible && this.StackCount > 1;
		if (this.txtStack.IsActive() != flag)
		{
			this.txtStack.gameObject.SetActive(flag);
		}
		if (GUIInventory.instance.IsInventoryVisible)
		{
			GUIInventoryItem inventoryItemFromCO2 = GUIInventory.GetInventoryItemFromCO(this);
			if (inventoryItemFromCO2 != null)
			{
				inventoryItemFromCO2.UpdateStackText();
			}
		}
	}

	public void Use(string strUseCase)
	{
		if (strUseCase == null || !this.mapChargeProfiles.ContainsKey(strUseCase))
		{
			return;
		}
		JsonChargeProfile jsonChargeProfile = this.mapChargeProfiles[strUseCase];
		if (jsonChargeProfile.fCondAmount != 0f && jsonChargeProfile.strCondName != null)
		{
			float num = jsonChargeProfile.fCondAmount;
			List<CondOwner> list = new List<CondOwner>();
			if (jsonChargeProfile.bUseSelf)
			{
				list.Add(this);
			}
			if (jsonChargeProfile.bUseContained)
			{
				list = this.GetCOs(true, null);
			}
			if (list != null)
			{
				foreach (CondOwner coUsed in list)
				{
					if (num <= 0f)
					{
						break;
					}
					num = this.UseCharge(coUsed, jsonChargeProfile.strCondName, num, jsonChargeProfile.fDmgAmountCharge);
				}
			}
		}
		if (jsonChargeProfile.nItemAmount > 0)
		{
			int num2 = jsonChargeProfile.nItemAmount;
			List<CondOwner> list2 = this.GetCOs(true, jsonChargeProfile.CTItem());
			while (list2 != null && num2 > 0)
			{
				if (list2.Count == 0)
				{
					list2 = this.GetCOs(true, jsonChargeProfile.CTItem());
					if (list2 == null || list2.Count == 0)
					{
						break;
					}
				}
				CondOwner condOwner = list2[0];
				if (condOwner.StackCount <= num2)
				{
					num2 -= condOwner.StackCount;
					if (!jsonChargeProfile.bSkipRemove)
					{
						this.RemoveCO(condOwner, false);
						condOwner.Destroy();
					}
					list2.RemoveAt(0);
				}
				else
				{
					list2 = condOwner.StackAsList;
					for (int i = 0; i < num2; i++)
					{
						if (!jsonChargeProfile.bSkipRemove)
						{
							this.RemoveCO(list2[i], false);
							list2[i].Destroy();
						}
					}
					num2 = 0;
					list2.Clear();
				}
			}
		}
		if (jsonChargeProfile.fDmgAmountUs != 0f)
		{
			this.AddCondAmount("StatDamage", (double)jsonChargeProfile.fDmgAmountUs, 0.0, 0f);
		}
	}

	private float QueryCharge(CondOwner coUsed, string strCondName, float fCondAmount)
	{
		if (coUsed == null || strCondName == null || fCondAmount == 0f)
		{
			return 0f;
		}
		fCondAmount -= (float)coUsed.GetCondAmount(strCondName);
		if (fCondAmount <= 0f)
		{
			return 0f;
		}
		for (int i = coUsed.StackCount - 2; i >= 0; i--)
		{
			if (fCondAmount <= 0f)
			{
				break;
			}
			fCondAmount = this.QueryCharge(coUsed.aStack[i], strCondName, fCondAmount);
		}
		return fCondAmount;
	}

	private float UseCharge(CondOwner coUsed, string strCondName, float fCondAmount, float fDmgAmountCharge)
	{
		if (coUsed == null)
		{
			return fCondAmount;
		}
		if (fDmgAmountCharge == 0f && (strCondName == null || fCondAmount == 0f))
		{
			return fCondAmount;
		}
		if (coUsed.GetCondAmount(strCondName) >= (double)fCondAmount)
		{
			coUsed.AddCondAmount(strCondName, (double)(-(double)fCondAmount), 0.0, 0f);
			if (fDmgAmountCharge != 0f && coUsed != this)
			{
				coUsed.AddCondAmount("StatDamage", (double)fDmgAmountCharge, 0.0, 0f);
			}
			fCondAmount = 0f;
		}
		else
		{
			fCondAmount -= (float)coUsed.GetCondAmount(strCondName);
			coUsed.ZeroCondAmount(strCondName);
			if (fDmgAmountCharge != 0f && coUsed != this)
			{
				coUsed.AddCondAmount("StatDamage", (double)fDmgAmountCharge, 0.0, 0f);
			}
			for (int i = coUsed.StackCount - 2; i >= 0; i--)
			{
				if (fCondAmount <= 0f)
				{
					break;
				}
				fCondAmount = this.UseCharge(coUsed.aStack[i], strCondName, fCondAmount, fDmgAmountCharge);
			}
		}
		return fCondAmount;
	}

	public bool Usable(string strUseCase, out string strOut)
	{
		strOut = string.Empty;
		if (strUseCase == null || !this.mapChargeProfiles.ContainsKey(strUseCase))
		{
			return true;
		}
		JsonChargeProfile jsonChargeProfile = this.mapChargeProfiles[strUseCase];
		if (jsonChargeProfile.fCondAmount != 0f && jsonChargeProfile.strCondName != null)
		{
			float num = jsonChargeProfile.fCondAmount;
			List<CondOwner> list = new List<CondOwner>();
			if (jsonChargeProfile.bUseSelf)
			{
				list.Add(this);
			}
			if (jsonChargeProfile.bUseContained)
			{
				list = this.GetCOs(true, null);
			}
			if (list != null)
			{
				foreach (CondOwner coUsed in list)
				{
					if (num <= 0f)
					{
						break;
					}
					num = this.QueryCharge(coUsed, jsonChargeProfile.strCondName, num);
				}
			}
			if (num > 0f)
			{
				strOut = jsonChargeProfile.strCondName;
				return false;
			}
		}
		if (jsonChargeProfile.nItemAmount > 0)
		{
			int num2 = jsonChargeProfile.nItemAmount;
			List<CondOwner> cos = this.GetCOs(true, jsonChargeProfile.CTItem());
			if (cos != null)
			{
				foreach (CondOwner condOwner in cos)
				{
					if (num2 <= 0)
					{
						break;
					}
					num2 -= condOwner.StackCount;
				}
			}
			if (num2 > 0)
			{
				strOut = jsonChargeProfile.strItemCT;
				return false;
			}
		}
		return true;
	}

	public CondHistory GetCH(string strCond)
	{
		if (!this.mapIAHist.ContainsKey(strCond))
		{
			this.mapIAHist[strCond] = new CondHistory(strCond);
		}
		return this.mapIAHist[strCond];
	}

	public void AddRememberScore(string strCondName, double fCount)
	{
		if (!this.dictRememberScores.ContainsKey(strCondName))
		{
			this.dictRememberScores[strCondName] = fCount;
		}
		else
		{
			Dictionary<string, double> dictionary;
			(dictionary = this.dictRememberScores)[strCondName] = dictionary[strCondName] + fCount;
		}
	}

	public void RememberLess()
	{
		List<string> list = new List<string>(this.dictRememberScores.Keys);
		List<string> list2 = new List<string>();
		foreach (string text in list)
		{
			Dictionary<string, double> dictionary;
			string key;
			(dictionary = this.dictRememberScores)[key = text] = dictionary[key] * (double)this.fRememberDecay;
			if (Mathf.Abs((float)this.dictRememberScores[text]) < 0.25f)
			{
				list2.Add(text);
			}
		}
		foreach (string key2 in list2)
		{
			this.dictRememberScores.Remove(key2);
		}
		while (this.aRememberIAs.Count > 5)
		{
			this.aRememberIAs.RemoveAt(5);
		}
	}

	public void RememberInteractionEffectTraining(string strInteractionName)
	{
		List<string> aConds = new List<string>();
		if (strInteractionName != null)
		{
			Interaction interaction = DataHandler.GetInteraction(strInteractionName, null, false);
			if (interaction != null)
			{
				if (interaction.bHumanOnly)
				{
					return;
				}
				aConds = interaction.CTTestThem.GetAllReqNames(false);
			}
		}
		this._RememberInteractionEffect2(aConds);
	}

	public void RememberEffects2(CondOwner objThem)
	{
		if (!this.bAlive || !this.HasCond("IsAIAgent") || this.HasCond("Unconscious"))
		{
			return;
		}
		if (objThem != null)
		{
			List<string> list = new List<string>();
			Relationship relationship = null;
			if (objThem != this && this.socUs != null)
			{
				relationship = this.socUs.GetRelationship(objThem.strID);
			}
			if (relationship != null)
			{
				list = relationship.aReveals;
			}
			else
			{
				foreach (Condition condition in objThem.mapConds.Values)
				{
					if ((condition.nDisplaySelf == 2 && objThem == this) || (condition.nDisplayOther == 2 && objThem != this))
					{
						list.Add(condition.strName);
					}
				}
			}
			this._RememberInteractionEffect2(list);
		}
	}

	private void _RememberInteractionEffect2(List<string> aConds)
	{
		foreach (KeyValuePair<string, double> keyValuePair in this.dictRememberScores)
		{
			if (this.hashCondsImportant.Contains(keyValuePair.Key))
			{
				float num = 1f;
				CondHistory ch = this.GetCH(keyValuePair.Key);
				bool flag = true;
				foreach (string text in this.aRememberIAs)
				{
					if (!CondOwner.aAIRandomAvoid.Contains(text))
					{
						ch.AddInteractionScore(text, (float)keyValuePair.Value * num, flag);
						if (flag)
						{
							foreach (string strCond in aConds)
							{
								ch.AddCondScore(text, strCond, (float)keyValuePair.Value * num, flag);
							}
							flag = false;
						}
						num *= this.fRememberDecay;
					}
				}
			}
		}
	}

	private Interaction Interact()
	{
		while (this.aQueue.Count > 0 && this.aQueue[0] == null)
		{
			this.ClearInteraction(this.aQueue[0], false);
		}
		if (this.aQueue.Count == 0)
		{
			return null;
		}
		Interaction interaction = this.aQueue[0];
		bool flag = false;
		if (this.Pathfinder != null)
		{
			if (!this.Pathfinder.InRange(null))
			{
				flag = true;
			}
		}
		else
		{
			Vector2 pos = interaction.objThem.GetPos(interaction.strTargetPoint, false);
			Vector2 vector = new Vector2(this.tf.position.x, this.tf.position.y);
			float num = 1.5f + interaction.fTargetPointRange;
			if (Mathf.Abs(vector.x - pos.x) > num || Mathf.Abs(vector.y - pos.y) > num)
			{
				flag = true;
			}
		}
		if (flag)
		{
			if (interaction.bTryWalk || !this.CheckWalk(interaction, interaction.objThem))
			{
				this.ClearInteraction(interaction, false);
				return null;
			}
			interaction.bTryWalk = true;
		}
		if (interaction.bApplyChain)
		{
			interaction.ApplyChain(null);
		}
		else
		{
			interaction.ApplyEffects(null, false);
		}
		if (this.bDestroyed)
		{
			return null;
		}
		if (this.IsHumanOrRobot && interaction.strName != null && DataHandler.dictSocialStats.ContainsKey(interaction.strName))
		{
			DataHandler.dictSocialStats[interaction.strName].nUsed++;
			if (interaction.strChainStart == interaction.strName)
			{
				DataHandler.dictSocialStats[interaction.strName].nChecked++;
			}
		}
		Interaction interaction2 = null;
		if (!interaction.bApplyChain && interaction.aInverse != null && interaction.aInverse.Length > 0)
		{
			bool flag2 = false;
			bool flag3 = false;
			if (interaction.bImmediateReply || interaction.bIgnoreFeelings || this.pspec == null)
			{
				interaction2 = interaction.GetReply();
				if (interaction2 != null && interaction2.objUs == this)
				{
					flag2 = true;
				}
				if (this.pspec != null && interaction.objThem.pspec != null)
				{
					flag3 = true;
				}
			}
			else
			{
				flag3 = true;
				string text = interaction.aInverse[0];
				if (!string.IsNullOrEmpty(text))
				{
					string[] array = text.Split(new char[]
					{
						','
					});
					if (array.Length > 1 && array[1] == "[us]")
					{
						flag2 = true;
					}
				}
			}
			if (flag3)
			{
				if (flag2)
				{
					interaction.objUs.AddReplyThread(StarSystem.fEpoch, interaction.objThem.strID, interaction);
				}
				else
				{
					interaction.objThem.AddReplyThread(StarSystem.fEpoch, this.strID, interaction);
				}
			}
		}
		if (CrewSim.bRaiseUI && interaction.objUs != interaction.objThem && interaction.objThem == CrewSim.GetSelectedCrew() && interaction.strName != "Wait" && interaction.strName != "Walk")
		{
			interaction.objThem.AICancelAll(interaction.objUs);
		}
		this.dictRecentlyTried[interaction.objThem.strID + interaction.strName] = StarSystem.fEpoch;
		this.ClearInteraction(interaction, false);
		if (this.Item != null)
		{
			this.Item.VisualizeOverlays(false);
		}
		return interaction2;
	}

	public void ModeSwitch(CondOwner coNew, Vector3 vDropPos)
	{
		this.ValidateParentRecursive();
		if (coNew == null)
		{
			return;
		}
		coNew.ValidateParentRecursive();
		if (this.elec != null)
		{
			if (coNew.elec != null)
			{
				this.elec.CleanUp(false);
			}
			else
			{
				this.elec.CleanUp(true);
			}
		}
		Item item = coNew.Item;
		Item item2 = this.Item;
		bool highlight = this.Highlight;
		bool dimLights = this.DimLights;
		Transform transform = coNew.tf;
		float fLastRotation = this.tf.rotation.eulerAngles.z;
		if (item2 != null)
		{
			fLastRotation = item2.fLastRotation;
		}
		if (item != null)
		{
			item.fLastRotation = fLastRotation;
			vDropPos.z = item.GetZPos();
		}
		transform.position = vDropPos;
		if (this.HasCond("IsAirtight") && !coNew.HasCond("IsAirtight"))
		{
			this.ZeroCondAmount("IsAirtight");
		}
		CondOwner condOwner = this;
		if (this.strPersistentCO != null && DataHandler.mapCOs.ContainsKey(this.strPersistentCO))
		{
			condOwner = DataHandler.mapCOs[this.strPersistentCO];
		}
		else if (this.strPersistentCT != null && this.aLot.Count > 0)
		{
			CondTrigger condTrigger = DataHandler.GetCondTrigger(this.strPersistentCT);
			foreach (CondOwner condOwner2 in this.aLot)
			{
				if (condTrigger.Triggered(condOwner2, null, true))
				{
					condOwner = condOwner2;
					break;
				}
			}
		}
		COOverlay component = condOwner.GetComponent<COOverlay>();
		if (component != null)
		{
			string text = component.ModeSwitch(coNew.strCODef);
			if (text != null)
			{
				COOverlay cooverlay = coNew.gameObject.AddComponent<COOverlay>();
				cooverlay.Init(text);
			}
		}
		foreach (Condition condition in condOwner.mapConds.Values)
		{
			if (condition.bPersists)
			{
				coNew.AddCondAmount(condition.strName, condition.fCount - coNew.GetCondAmount(condition.strName), (double)condition.GetAge(), 0f);
			}
		}
		foreach (string item3 in condOwner.aCondZeroes)
		{
			double condAmount = coNew.GetCondAmount(item3);
			coNew.AddCondAmount(item3, -condAmount, 0.0, 0f);
			if (!coNew.aCondZeroes.Contains(item3))
			{
				coNew.aCondZeroes.Add(item3);
			}
		}
		coNew.strID = condOwner.strID;
		foreach (KeyValuePair<string, Dictionary<string, string>> keyValuePair in condOwner.mapGUIPropMaps)
		{
			coNew.mapGUIPropMaps[keyValuePair.Key] = keyValuePair.Value;
		}
		List<CondOwner> list = new List<CondOwner>();
		if (condOwner.objContainer != null)
		{
			list.AddRange(condOwner.objContainer.GetCOs(true, null));
		}
		for (int i = list.Count - 1; i >= 0; i--)
		{
			if (list[i].coStackHead != null)
			{
				list.RemoveAt(i);
			}
			else if (list[i].objCOParent != condOwner)
			{
				list.RemoveAt(i);
			}
		}
		Dictionary<CondOwner, string> dictionary = new Dictionary<CondOwner, string>();
		if (this.compSlots != null)
		{
			this.GatherSlottedItems(this.compSlots, dictionary);
		}
		else if (condOwner.compSlots != null)
		{
			this.GatherSlottedItems(condOwner.compSlots, dictionary);
		}
		Slot slot = null;
		CondOwner condOwner3 = this.objCOParent;
		if (this.HasCond("IsSlotted") && this.objCOParent != null && this.objCOParent.compSlots != null)
		{
			slot = this.objCOParent.compSlots.GetSlotForCO(this);
		}
		bool flag = false;
		if (this.HasCond("IsCheckRoom") && coNew.HasCond("IsCheckRoom") && item.nWidthInTiles == item2.nWidthInTiles && item.nHeightInTiles == item2.nHeightInTiles)
		{
			this.AddCondAmount("IsModeSwitching", 1.0, 0.0, 0f);
			coNew.AddCondAmount("IsModeSwitching", 1.0, 0.0, 0f);
			coNew.ZeroCondAmount("IsCheckRoom");
			flag = true;
		}
		if (GUISocialCombat2.coUs == this)
		{
			GUISocialCombat2.coUs = coNew;
		}
		if (GUISocialCombat2.coThem == this)
		{
			GUISocialCombat2.coThem = coNew;
		}
		bool bCheckRooms = this.ship.bCheckRooms;
		Ship ship = this.ship;
		if (this.aStack != null && this.aStack.Count > 0)
		{
			this.PopHeadFromStack();
		}
		else if (this.coStackHead != null)
		{
			this.coStackHead.RemoveCO(this, false);
		}
		else
		{
			this.RemoveFromCurrentHome(false);
		}
		if (condOwner3 == null)
		{
			Vector3 position = default(Vector3);
			if (!coNew.HasCond("IsInstalled") && !item.CheckFit(vDropPos, ship, null, null))
			{
				bool flag2 = Vector3.Distance(vDropPos, coNew.tf.position) < 0.01f;
				ship.ShiftTorwardsClosestCrewMember(coNew, 1f);
				vDropPos = ((!flag2) ? vDropPos : coNew.tf.position);
				if (TileUtils.TryFitItem(item, ship, vDropPos, out position))
				{
					position.z = transform.position.z;
					transform.position = position;
					ship.AddCO(coNew, true);
				}
				else
				{
					CondOwner x = this.DropCO(coNew, false, ship, 0f, 0f, true, null);
					if (x != null)
					{
						ship.AddCO(coNew, true);
					}
				}
			}
			else
			{
				ship.AddCO(coNew, true);
			}
		}
		else if (slot != null)
		{
			if (!condOwner3.compSlots.SlotItem(slot.strName, coNew, true))
			{
				UnityEngine.Debug.Log(string.Concat(new string[]
				{
					"Couldn't slot ",
					coNew.strName,
					" into ",
					slot.strName,
					" upon modeswitch!"
				}));
				CondOwner condOwner4 = condOwner3.DropCO(coNew, false, null, 0f, 0f, true, null);
				if (condOwner4 != null)
				{
					UnityEngine.Debug.Log("Couldn't drop " + condOwner4.strName + " upon modeswitch!");
					ship.AddCO(coNew, true);
				}
			}
		}
		else
		{
			CondOwner objCO = condOwner3.AddCO(coNew, false, true, true);
			this.DropCO(objCO, false, ship, 0f, 0f, true, null);
		}
		foreach (CondOwner condOwner5 in list)
		{
			condOwner5.RemoveFromCurrentHome(false);
			CondOwner objCO2 = coNew.AddCO(condOwner5, false, true, true);
			this.DropCO(objCO2, false, ship, 0f, 0f, true, null);
		}
		List<CondOwner> list2 = new List<CondOwner>();
		List<CondOwner> list3 = null;
		foreach (KeyValuePair<CondOwner, string> keyValuePair2 in dictionary)
		{
			keyValuePair2.Key.RemoveFromCurrentHome(true);
			if (!(coNew.compSlots != null) || !coNew.compSlots.SlotItem(keyValuePair2.Value, keyValuePair2.Key, true))
			{
				if (keyValuePair2.Key.bSlotLocked)
				{
					if (list3 == null)
					{
						list3 = new List<CondOwner>();
					}
					list3.Add(keyValuePair2.Key);
					if (!(keyValuePair2.Key.objContainer == null))
					{
						List<CondOwner> cos = keyValuePair2.Key.objContainer.GetCOs(false, null);
						if (cos != null)
						{
							foreach (CondOwner condOwner6 in cos)
							{
								if (!condOwner6.bSlotLocked)
								{
									condOwner6.RemoveFromCurrentHome(false);
									CondOwner x2 = coNew.AddCO(condOwner6, true, true, true);
									if (x2 != null && condOwner3 != null)
									{
										x2 = condOwner3.AddCO(condOwner6, false, true, true);
									}
									if (x2 != null)
									{
										if (!condOwner6.HasCond("IsSolid"))
										{
											list3.Add(condOwner6);
										}
										else if (ship != null)
										{
											this.DropCO(condOwner6, false, ship, 0f, 0f, true, null);
										}
									}
								}
							}
						}
					}
				}
				else
				{
					this.DropCO(keyValuePair2.Key, false, ship, 0f, 0f, true, null);
					list2.Add(keyValuePair2.Key);
				}
			}
		}
		this.ReslotFailedCOs(list2, coNew, "IsHuman");
		if (list3 != null)
		{
			foreach (CondOwner condOwner7 in list3)
			{
				condOwner7.Destroy();
			}
		}
		if (flag)
		{
			coNew.AddCondAmount("IsCheckRoom", 1.0, 0.0, 0f);
			ship.bCheckRooms = bCheckRooms;
		}
		coNew.CheckForRename();
		CrewSim.objInstance.SetBracketTarget(this.strID, true, false);
		if (GUIMegaToolTip.Selected == this)
		{
			CrewSim.OnRightClick.Invoke(new List<CondOwner>
			{
				coNew
			});
		}
		this.aTickers.Clear();
		while (this.aLot.Count > 0)
		{
			CondOwner condOwner8 = this.aLot[0];
			this.RemoveLotCO(condOwner8);
			condOwner8.Destroy();
		}
		UnityEngine.Object.Destroy(base.gameObject);
		this.Highlight = highlight;
		this.DimLights = dimLights;
		if (!AudioManager.bIgnoreCOTrans)
		{
			AudioEmitter component2 = coNew.GetComponent<AudioEmitter>();
			if (component2 != null)
			{
				component2.StartTrans(false);
			}
		}
		this.ValidateParentRecursive();
		coNew.ValidateParentRecursive();
		double condAmount2 = coNew.GetCondAmount("StatDamageMax");
		double num = condAmount2 * 1.0 / 8000.0;
		if (this.HasCond("IsSolidState"))
		{
			num = 0.0;
		}
		num += condAmount2 * this.fMSRedamageAmount;
		coNew.AddCondAmount("StatDamage", num, 0.0, 0f);
		item.VisualizeOverlays(false);
	}

	private void GatherSlottedItems(Slots compSlotsIn, Dictionary<CondOwner, string> mapSlottedSubCOs)
	{
		foreach (Slot slot in compSlotsIn.GetSlotsDepthFirst(false))
		{
			foreach (CondOwner condOwner in slot.aCOs)
			{
				if (!(condOwner == null))
				{
					mapSlottedSubCOs[condOwner] = slot.strName;
				}
			}
		}
	}

	private void ReslotFailedCOs(IEnumerable<CondOwner> failedItems, CondOwner coNew, string rootCondition = "IsHuman")
	{
		if (coNew == null || failedItems == null)
		{
			return;
		}
		foreach (CondOwner condOwner in failedItems)
		{
			CondOwner condOwner2 = coNew.RootParent(rootCondition);
			if (condOwner2 != null && condOwner.mapSlotEffects != null && condOwner.mapSlotEffects.Count > 0)
			{
				string key = condOwner.mapSlotEffects.First<KeyValuePair<string, JsonSlotEffects>>().Key;
				Ship objShipRef = condOwner.RemoveFromCurrentHome(false);
				if (condOwner2.compSlots.SlotItem(key, condOwner, true))
				{
					UnityEngine.Debug.Log(string.Concat(new string[]
					{
						"<color=yellow>Reslotted ",
						condOwner.strName,
						" to ",
						condOwner.transform.GetPath(),
						"</color>"
					}));
				}
				else
				{
					UnityEngine.Debug.Log(string.Concat(new string[]
					{
						"<color=yellow>Failed to reslot item: ",
						condOwner.strName,
						" on ",
						coNew.strName,
						" again, was supposed to go to ",
						coNew.transform.GetPath(),
						"</color>"
					}));
					this.DropCO(condOwner, false, objShipRef, 0f, 0f, true, null);
				}
			}
		}
	}

	public bool QueueInteraction(CondOwner objTarget, Interaction objInteraction, bool bInsert = false)
	{
		if (objInteraction == null || this == null)
		{
			return false;
		}
		objInteraction.objUs = this;
		objInteraction.objThem = objTarget;
		if (objInteraction.strChainOwner == null)
		{
			objInteraction.strChainOwner = this.strID;
		}
		if (bInsert)
		{
			this.aQueue.Insert(0, objInteraction);
		}
		else
		{
			this.aQueue.Add(objInteraction);
		}
		this.SetCondAmount("TaskBusy", 1.0, 0.0);
		if (this.aQueue[0] == objInteraction)
		{
			float num = -1f;
			if (objInteraction.strName != "Walk" && !this.HasCond("IsSpaced") && this.Pathfinder != null)
			{
				Tile tile = this.Pathfinder.tilCurrent;
				if (objInteraction.strTargetPoint != null && objInteraction.strTargetPoint != Interaction.POINT_REMOTE)
				{
					Vector2 vector = objInteraction.objThem.GetPos(objInteraction.strTargetPoint, false);
					if (this.GetCORef(objInteraction.objThem) != null)
					{
						vector = this.tf.position;
					}
					tile = this.ship.GetTileAtWorldCoords1(vector.x, vector.y, true, true);
				}
				if (tile == this.Pathfinder.tilCurrent)
				{
					num = 0f;
				}
				else
				{
					Pathfinder pathfinder = this.Pathfinder;
					Tile tilDestNew = tile;
					float fTargetPointRange = objInteraction.fTargetPointRange;
					CondOwner objThem = objInteraction.objThem;
					bool bAllowAirlocks = this.HasAirlockPermission(objInteraction.bManual);
					PathResult pathResult = pathfinder.SetGoal2(tilDestNew, fTargetPointRange, objThem, 0f, 0f, bAllowAirlocks);
					num = pathResult.PathLength;
				}
				if (num > 0f && !CondOwner.CTCanWalk.Triggered(this, null, true))
				{
					string strMsg = this.FriendlyName + DataHandler.GetString("ERROR_STUNNED", false) + CondOwner.CTCanWalk.strFailReasonLast;
					this.LogMessage(strMsg, "Bad", this.strName);
					this.ClearInteraction(objInteraction, false);
					return false;
				}
				if (num < 0f)
				{
					string text = objInteraction.objThem.strName;
					if (objInteraction.objThem.strNameFriendly != null)
					{
						text = objInteraction.objThem.strNameFriendly;
					}
					this.LogMessage(DataHandler.GetString("ERROR_CANT_REACH_DEST", false) + text + ".", "Bad", this.strName);
					UnityEngine.Debug.Log(string.Concat(new string[]
					{
						DataHandler.GetString("ERROR_CANT_REACH_DEST", false),
						objInteraction.strName,
						"; Target: ",
						text,
						"."
					}));
					this.ClearInteraction(objInteraction, false);
					return false;
				}
				if (num == 0f && objInteraction.fDuration != 0.0)
				{
					double num2 = StarSystem.fEpoch - this.fLastICOUpdate;
					objInteraction.fDuration += num2 / 3600.0;
				}
			}
			if (num == 0f && objInteraction.objThem != this)
			{
				this.LookAt(objInteraction.objThem, false);
			}
			for (int i = 0; i < this.aTickers.Count; i++)
			{
				JsonTicker jsonTicker = this.aTickers[i];
				if (jsonTicker.bQueue || jsonTicker.strName == "AIAgent")
				{
					this.aTickers.Remove(jsonTicker);
					i--;
				}
			}
			JsonTicker jsonTicker2 = new JsonTicker();
			jsonTicker2.strName = objInteraction.strName;
			jsonTicker2.bQueue = true;
			jsonTicker2.fPeriod = objInteraction.fDuration;
			jsonTicker2.SetTimeLeft(jsonTicker2.fPeriod);
			this.AddTicker(jsonTicker2);
			if (jsonTicker2.fPeriod != 0.0)
			{
				jsonTicker2 = jsonTicker2.Clone();
				jsonTicker2.fPeriod = 0.0;
				jsonTicker2.SetTimeLeft(jsonTicker2.fPeriod);
				this.AddTicker(jsonTicker2);
			}
		}
		CondOwner selectedCrew = CrewSim.GetSelectedCrew();
		if (selectedCrew != null && selectedCrew == this)
		{
			MonoSingleton<GUIQuickBar>.Instance.BuildButtonList(false);
		}
		if (this.OnQueueInteraction != null)
		{
			this.OnQueueInteraction(objInteraction);
		}
		return true;
	}

	public void ClearInteraction(Interaction objInteraction, bool bCancelling = false)
	{
		if (objInteraction == null || this == null)
		{
			return;
		}
		int num = this.aQueue.IndexOf(objInteraction);
		bool isHumanOrRobot = this.IsHumanOrRobot;
		bool flag = this.HasCond("IsInCombat");
		bool flag2 = CrewSim.GetSelectedCrew() == this && (flag || objInteraction.strActionGroup == "Fight") && objInteraction.objUs == this && objInteraction.fDurationOrig > 0.0;
		bool flag3 = false;
		string strReason = null;
		if (flag2)
		{
			strReason = objInteraction.strTitle + DataHandler.GetString("AUTOPAUSE_ACT_ENDED", false);
		}
		else if (!flag && objInteraction.strActionGroup == "Talk" && objInteraction.aInverse != null)
		{
			if (objInteraction.aInverse.Length > 0)
			{
				if (!string.IsNullOrEmpty(objInteraction.aInverse[0]))
				{
					string[] array = objInteraction.aInverse[0].Split(new char[]
					{
						','
					});
					bool flag4 = !string.IsNullOrEmpty(objInteraction.strLootContextThem) && objInteraction.strLootContextThem != "Default";
					if ((array.Length == 1 || array[1] != "[us]") && (array[0] != "SOCBlank" || flag4))
					{
						if (objInteraction.objThem == CrewSim.GetSelectedCrew())
						{
							flag2 = true;
							strReason = this.FriendlyName + DataHandler.GetString("AUTOPAUSE_NPC_REPLIED", false);
						}
						else if (objInteraction.objUs != CrewSim.GetSelectedCrew())
						{
							flag3 = true;
						}
					}
				}
			}
			else if (this == CrewSim.GetSelectedCrew())
			{
				if (objInteraction.strName != "WaitReply" && objInteraction.objUs != objInteraction.objThem)
				{
					flag2 = true;
					strReason = this.FriendlyName + DataHandler.GetString("AUTOPAUSE_NPC_REPLIED", false);
				}
			}
			else if (objInteraction.objThem == CrewSim.GetSelectedCrew())
			{
				flag2 = true;
				strReason = this.FriendlyName + DataHandler.GetString("AUTOPAUSE_NPC_REPLIED", false);
			}
		}
		if (objInteraction.fDuration > 0.0 && objInteraction.aDependents != null && objInteraction.aDependents.Count > 0)
		{
			foreach (string b in objInteraction.aDependents)
			{
				for (int i = num + 1; i < this.aQueue.Count; i++)
				{
					if (this.aQueue[i].objThem == null)
					{
						UnityEngine.Debug.LogWarning("Null target on interaction " + this.aQueue[i].ToString());
						this.aQueue.RemoveAt(i);
					}
					else if (this.aQueue[i].strName + this.aQueue[i].objThem.strID == b)
					{
						if (isHumanOrRobot)
						{
							CrewSim.objInstance.workManager.UnclaimTask(this.aQueue[i]);
						}
						this.aQueue.RemoveAt(i);
						break;
					}
				}
			}
		}
		if (this.aQueue.Remove(objInteraction))
		{
			if (isHumanOrRobot)
			{
				CrewSim.objInstance.workManager.UnclaimTask(objInteraction);
			}
			if (objInteraction != null && objInteraction.objThem != null && objInteraction.objThem.tf != null && objInteraction.strName != "Wait")
			{
				objInteraction.objThem.WaitFor(this, true);
			}
			if (num == 0)
			{
				for (int j = 0; j < this.aTickers.Count; j++)
				{
					JsonTicker jsonTicker = this.aTickers[j];
					if (jsonTicker.bQueue)
					{
						this.aTickers.Remove(jsonTicker);
						j--;
					}
				}
				if (this.progressBar != null)
				{
					this.progressBar.DeactivateImmediate();
				}
			}
			if (objInteraction.bRaisedUI && (CrewSim.GetSelectedCrew() == objInteraction.objUs || CrewSim.GetSelectedCrew() == objInteraction.objThem))
			{
				CrewSim.LowerUI(false);
			}
		}
		if (bCancelling && objInteraction.strCancelInteraction != null)
		{
			Interaction interaction = DataHandler.GetInteraction(objInteraction.strCancelInteraction, null, true);
			if (interaction != null)
			{
				interaction.objUs = objInteraction.objUs;
				interaction.objThem = objInteraction.objThem;
				interaction.ApplyEffects(null, true);
				DataHandler.ReleaseTrackedInteraction(interaction);
			}
		}
		if (this.aQueue.Count == 0)
		{
			this.ZeroCondAmount("TaskBusy");
			if (this.HasCond("IsAIAgent"))
			{
				JsonTicker ticker = DataHandler.GetTicker("AIAgent");
				ticker.SetTimeLeft(1E-06);
				this.AddTicker(ticker);
			}
		}
		else if (this.aQueue[0] != null)
		{
			bool flag5 = this.aQueue[0].objThem != null && this.aQueue[0].objThem.tf != null;
			if (this.Pathfinder != null && this.ship != null && !this.ship.bDestroyed)
			{
				Tile tile = this.Pathfinder.tilCurrent;
				if (this.GetCORef(this.aQueue[0].objThem) != null)
				{
					tile = tile;
				}
				else if (this.aQueue[0].strTargetPoint != null && objInteraction.strTargetPoint != Interaction.POINT_REMOTE && flag5)
				{
					Vector2 pos = this.aQueue[0].objThem.GetPos(this.aQueue[0].strTargetPoint, false);
					tile = this.ship.GetTileAtWorldCoords1(pos.x, pos.y, true, true);
				}
				Pathfinder pathfinder = this.Pathfinder;
				Tile tilDestNew = tile;
				float fTargetPointRange = this.aQueue[0].fTargetPointRange;
				CondOwner objThem = this.aQueue[0].objThem;
				bool bAllowAirlocks = this.HasAirlockPermission(objInteraction.bManual);
				PathResult pathResult = pathfinder.SetGoal2(tilDestNew, fTargetPointRange, objThem, 0f, 0f, bAllowAirlocks);
				if (!pathResult.HasPath)
				{
					this.ClearInteraction(this.aQueue[0], false);
					return;
				}
			}
			if (num == 0)
			{
				JsonTicker jsonTicker2 = new JsonTicker();
				jsonTicker2.strName = this.aQueue[0].strName;
				jsonTicker2.bQueue = true;
				jsonTicker2.fPeriod = this.aQueue[0].fDuration;
				jsonTicker2.SetTimeLeft(jsonTicker2.fPeriod);
				this.AddTicker(jsonTicker2);
				if (jsonTicker2.fPeriod != 0.0)
				{
					jsonTicker2 = jsonTicker2.Clone();
					jsonTicker2.fPeriod = 0.0;
					jsonTicker2.SetTimeLeft(jsonTicker2.fPeriod);
					this.AddTicker(jsonTicker2);
				}
			}
			if (this.aQueue[0].strThemType != Interaction.TARGET_SELF)
			{
				this.LookAt(this.aQueue[0].objThem, false);
			}
		}
		if (objInteraction != null)
		{
			objInteraction = objInteraction.Destroy();
		}
		if (flag2 && this.aQueue.Count == 0)
		{
			CrewSim.ScheduleAutoPause(0.5, strReason);
		}
		if (flag3)
		{
			this.QueueInteraction(this, DataHandler.GetInteraction("QuickWait", null, false), false);
		}
		CondOwner selectedCrew = CrewSim.GetSelectedCrew();
		if (selectedCrew != null && selectedCrew == this)
		{
			MonoSingleton<GUIQuickBar>.Instance.BuildButtonList(false);
		}
	}

	public void WaitFor(CondOwner objCO, bool bRelease)
	{
		if (objCO == null || objCO == this)
		{
			return;
		}
		Interaction interaction = null;
		foreach (Interaction interaction2 in this.aQueue)
		{
			if (interaction2.strName == "Wait" && objCO == interaction2.objThem)
			{
				interaction = interaction2;
				break;
			}
		}
		if (!bRelease && interaction == null)
		{
			this.QueueInteraction(objCO, DataHandler.GetInteraction("Wait", null, false), true);
			if (this.objCOParent != null && this.objCOParent != objCO)
			{
				this.objCOParent.WaitFor(objCO, false);
			}
		}
		if (bRelease && interaction != null)
		{
			interaction.fDuration = 0.0;
			this.SetTicker(interaction.strName, 0f);
			this.UpdateManual(10);
			if (this.objCOParent != null && this.objCOParent != objCO)
			{
				this.objCOParent.WaitFor(objCO, true);
			}
		}
	}

	public void LookAt(CondOwner objTarget, bool bLookBack = false)
	{
		if (this.Pathfinder == null || objTarget == null)
		{
			return;
		}
		this.tf.rotation = Quaternion.LookRotation(Vector3.forward, objTarget.tf.position - this.tf.position);
		if (bLookBack)
		{
			objTarget.LookAt(this, false);
		}
	}

	public void AICancelCurrent()
	{
		Interaction interactionCurrent = this.GetInteractionCurrent();
		if (interactionCurrent != null)
		{
			interactionCurrent.bCancel = true;
			JsonTicker jsonTicker = new JsonTicker();
			jsonTicker.bQueue = true;
			jsonTicker.strName = "Cleanup canceled interactions.";
			jsonTicker.fPeriod = 0.0;
			jsonTicker.SetTimeLeft(jsonTicker.fPeriod);
			this.AddTicker(jsonTicker);
		}
	}

	private void AIHandleCancels()
	{
		if (this.aQueue.Count == 0)
		{
			return;
		}
		int num = 0;
		bool flag = false;
		for (Interaction interaction = this.aQueue[num]; interaction != null; interaction = this.aQueue[num])
		{
			if (interaction.bCancel)
			{
				int count = this.aQueue.Count;
				this.ClearInteraction(interaction, true);
				if (count == this.aQueue.Count)
				{
					num++;
				}
				else if (interaction.strRaiseUI == "SocialCombat")
				{
					GUISocialCombat2.objInstance.EndSocialCombat();
				}
				flag = true;
			}
			else
			{
				num++;
			}
			if (num >= this.aQueue.Count)
			{
				break;
			}
		}
		if (flag && this.bAlive)
		{
			this.AddCondAmount("IsCrewToolSpark", -this.GetCondAmount("IsCrewToolSpark"), 0.0, 0f);
		}
	}

	public void AICancelAll(CondOwner coException = null)
	{
		if (!this.IsHumanOrRobot)
		{
			return;
		}
		if (this.aQueue.Count == 0)
		{
			return;
		}
		bool flag = false;
		int num = 0;
		for (Interaction interaction = this.aQueue[num]; interaction != null; interaction = this.aQueue[num])
		{
			if (!interaction.bCloser && !interaction.bCancel && !interaction.bIgnoreCancel)
			{
				CondOwner objThem = interaction.objThem;
				if (coException == null || coException != objThem || interaction.strSubUI != null)
				{
					interaction.bCancel = true;
					flag = true;
				}
			}
			num++;
			if (num >= this.aQueue.Count)
			{
				break;
			}
		}
		if (flag)
		{
			JsonTicker jsonTicker = new JsonTicker();
			jsonTicker.bQueue = true;
			jsonTicker.strName = "Cleanup canceled interactions.";
			jsonTicker.fPeriod = 0.0;
			jsonTicker.SetTimeLeft(jsonTicker.fPeriod);
			this.AddTicker(jsonTicker);
		}
	}

	public bool AIIssueOrder(CondOwner coTarget, Interaction objInt, bool bPlayerOrdered, Tile til, float fPosX = 0f, float fPosY = 0f)
	{
		if (!this.Kill)
		{
			return false;
		}
		if (bPlayerOrdered && this.HasCond("Stunned") && !CondOwner.CTCanAIOrder.Triggered(this, null, true))
		{
			string strMsg = this.FriendlyName + DataHandler.GetString("ERROR_STUNNED", false) + CondOwner.CTCanAIOrder.strFailReasonLast;
			this.LogMessage(strMsg, "Bad", this.strName);
			return false;
		}
		this.AICancelAll(null);
		if (bPlayerOrdered && !this.HasCond("IsPlayer"))
		{
			CondOwner.FreeWillPenalty.ApplyCondLoot(this, 1f, null, 0f);
			Interaction interaction = DataHandler.GetInteraction("SeekSocialDeny", null, false);
			interaction.objUs = this;
			interaction.objThem = CrewSim.coPlayer;
			if (interaction.Triggered(interaction.objUs, interaction.objThem, false, false, true, true, null))
			{
				this.Pathfinder.Reset();
				this.QueueInteraction(interaction.objThem, interaction, false);
				return true;
			}
		}
		if (coTarget != null && objInt != null)
		{
			if (this.Pathfinder == null)
			{
				UnityEngine.Debug.Log(string.Format("WARNING: Order {0} Issued to non-pathfinding object {1} with target {2}.", objInt.strName, this.strName, coTarget.strName));
			}
			else
			{
				this.Pathfinder.Reset();
			}
			this.QueueInteraction(coTarget, objInt, false);
			if (this.Pathfinder != null)
			{
				this.Pathfinder.VisualisePath(this.Pathfinder.currentPath);
			}
			return true;
		}
		if (til != null)
		{
			bool flag = false;
			if (CondOwner.CTIsProneAwake.Triggered(this, null, true))
			{
				Interaction interaction2 = DataHandler.GetInteraction("ACTStandUp", null, false);
				interaction2.bManual = bPlayerOrdered;
				if (this.QueueInteraction(this, interaction2, false))
				{
					flag = true;
				}
			}
			if (!CondOwner.CTCanWalk.Triggered(this, null, true) && !flag)
			{
				string strMsg2 = this.FriendlyName + DataHandler.GetString("ERROR_STUNNED", false) + CondOwner.CTCanWalk.strFailReasonLast;
				this.LogMessage(strMsg2, "Bad", this.strName);
				return false;
			}
			PathResult pathResult = this.Pathfinder.SetGoal2(til, 0f, null, fPosX, fPosY, this.HasAirlockPermission(bPlayerOrdered));
			if (pathResult.HasPath)
			{
				float realtimeSinceStartup = Time.realtimeSinceStartup;
				this.Pathfinder.VisualisePath(this.Pathfinder.currentPath);
				Interaction interaction3 = DataHandler.GetInteraction("Walk", null, false);
				interaction3.bManual = bPlayerOrdered;
				if (this.QueueInteraction(this, interaction3, false))
				{
					interaction3.objThem = til.coProps;
					interaction3.strTargetPoint = "use";
					interaction3.fTargetPointRange = 0f;
					return true;
				}
				this.LogMessage(DataHandler.GetString("AI_PATHFIND_NO_GENERAL", false), "Bad", this.strName);
			}
			else
			{
				string text = pathResult.FailReason(this);
				if (string.IsNullOrEmpty(text))
				{
					text = DataHandler.GetString("AI_PATHFIND_NO_GENERAL", false);
				}
				this.LogMessage(text, "Bad", this.strName);
				if (bPlayerOrdered)
				{
					this.TriggerRosterPermissionTutorial(pathResult);
				}
			}
		}
		return false;
	}

	private void TriggerRosterPermissionTutorial(PathResult pr)
	{
		if (CondOwner._hasSeenRosterTutorial || pr == null || !pr.bAirlockBlocked || pr.bDisembarkBlocked)
		{
			return;
		}
		if (CrewSim.coPlayer != this || !CrewSim.coPlayer.HasCond("IsAIManual"))
		{
			return;
		}
		CondOwner._hasSeenRosterTutorial = (CrewSim.coPlayer.HasCond("TutorialRosterShow") || CrewSim.coPlayer.HasCond("TutorialRosterComplete"));
		if (CondOwner._hasSeenRosterTutorial)
		{
			return;
		}
		CrewSimTut.BeginTutorialBeat<RosterPermission>();
		CrewSim.coPlayer.AddCondAmount("TutorialRosterShow", 1.0, 0.0, 0f);
		CondOwner._hasSeenRosterTutorial = true;
	}

	public void CatchUp()
	{
		double num = StarSystem.fEpoch - this.fLastICOUpdate;
		this.fLastICOUpdate = StarSystem.fEpoch;
		if (num != 0.0)
		{
			float elapsed = Convert.ToSingle(num);
			this.aCondsTemp.AddRange(this.aCondsTimed);
			foreach (Condition condition in this.aCondsTemp)
			{
				condition.Update(elapsed, this);
			}
			this.aCondsTemp.Clear();
		}
		if (this.Company != null)
		{
			int hourFromS = MathUtils.GetHourFromS(StarSystem.fEpoch);
			if (hourFromS != MathUtils.GetHourFromS(StarSystem.fEpoch - num))
			{
				this.ShiftChange(this.Company.GetShift(hourFromS, this), false);
			}
		}
		if (this.aTickers == null)
		{
			UnityEngine.Debug.LogWarning("null aTickers found on " + this.strName + ". Skipping.");
			return;
		}
		List<JsonTicker> list = new List<JsonTicker>();
		foreach (JsonTicker jsonTicker in this.aTickers)
		{
			if (jsonTicker.bTickWhileAway)
			{
				list.Add(jsonTicker);
			}
			else
			{
				double num2 = 0.0;
				if (jsonTicker.bQueue && jsonTicker.fPeriod > 0.0 && this.aQueue.Count > 0)
				{
					if (this.aQueue[0].bCancel)
					{
						num2 = 0.0;
					}
					else
					{
						num2 = this.aQueue[0].fDuration - num / 3600.0;
					}
					if (num < 0.0)
					{
						UnityEngine.Debug.Log(string.Concat(new object[]
						{
							"    ********",
							this.aQueue[0].strName,
							" catchup = ",
							num
						}));
					}
					this.aQueue[0].fDuration = num2;
				}
				else if (jsonTicker.strCondUpdate == null || !this.mapConds.ContainsKey(jsonTicker.strCondUpdate))
				{
					num2 = jsonTicker.fTimeLeft % jsonTicker.fPeriod;
					if (num2 < 0.0)
					{
						num2 += jsonTicker.fPeriod;
					}
				}
				if (double.IsNaN(num2))
				{
					num2 = 0.0;
				}
				jsonTicker.SetTimeLeft(num2);
			}
		}
		foreach (JsonTicker jsonTicker2 in list)
		{
			this.RemoveTicker(jsonTicker2);
			if (jsonTicker2.fTimeLeft <= 0.0 && !string.IsNullOrEmpty(jsonTicker2.strCondUpdate))
			{
				Condition condition2 = null;
				if (this.mapConds.TryGetValue(jsonTicker2.strCondUpdate, out condition2))
				{
					condition2.Update((float)(StarSystem.fEpoch - jsonTicker2.fEpochStart), this);
				}
			}
			else
			{
				this.AddTicker(jsonTicker2);
			}
		}
		if (this.aManUpdates == null)
		{
			UnityEngine.Debug.LogWarning("null aManUpdates found on " + this.strName + ". Skipping.");
			return;
		}
		foreach (IManUpdater manUpdater in this.aManUpdates)
		{
			manUpdater.CatchUp();
		}
	}

	public int GetAnimState()
	{
		if (this.anim != null)
		{
			return this.anim.GetInteger(CondOwner.nAnimStateID);
		}
		return -1;
	}

	private void SetAnimState(int nState)
	{
		if (this.anim == null)
		{
			return;
		}
		if (nState == 1)
		{
			nState = Interaction.dictAnims[this.strWalkAnim];
			float num = (float)(1.0 - this.GetCondAmount("StatMovSpeedPenalty"));
			num = Mathf.Max(num, 0.05f);
			this.anim.speed = num;
		}
		else
		{
			this.anim.speed = 1f;
		}
		if (base.gameObject.activeInHierarchy)
		{
			this.anim.SetInteger(CondOwner.nAnimStateID, nState);
		}
	}

	public void SetAnimTrigger(string strTrigger)
	{
		if (this.anim != null)
		{
			this.anim.SetTrigger(strTrigger);
		}
	}

	public void LogMove(string originRegId, string destinationRegId, MoveReason moveReason, string optionalData = null)
	{
		string text = string.Empty;
		switch (moveReason)
		{
		case MoveReason.DOCKED:
			if (optionalData == null)
			{
				text = string.Concat(new string[]
				{
					this.strNameFriendly,
					DataHandler.GetString("CREW_LOG_DOCKED", false),
					destinationRegId,
					DataHandler.GetString("CREW_LOG_ABOARD", false),
					originRegId
				});
			}
			else
			{
				text = string.Concat(new string[]
				{
					optionalData,
					DataHandler.GetString("CREW_LOG_HAULED", false),
					this.strNameFriendly,
					DataHandler.GetString("CREW_LOG_TO", false),
					destinationRegId,
					DataHandler.GetString("CREW_LOG_ABOARD", false),
					originRegId
				});
			}
			break;
		case MoveReason.ADDCREW:
			text = string.Concat(new string[]
			{
				this.strNameFriendly,
				DataHandler.GetString("CREW_LOG_TRANSFER", false),
				originRegId,
				DataHandler.GetString("CREW_LOG_TO", false),
				destinationRegId
			});
			break;
		case MoveReason.ADDNEWCREW:
			text = this.strNameFriendly + DataHandler.GetString("CREW_LOG_REPORTSFORDUTY", false) + destinationRegId;
			break;
		case MoveReason.REGIONCLEANUP:
			text = string.Concat(new string[]
			{
				this.strNameFriendly,
				DataHandler.GetString("CREW_LOG_ASSIGNMENT", false),
				originRegId,
				DataHandler.GetString("CREW_LOG_RETURNSTO", false),
				destinationRegId
			});
			break;
		case MoveReason.PASS:
			text = string.Concat(new string[]
			{
				this.strNameFriendly,
				DataHandler.GetString("CREW_LOG_LEAVES", false),
				originRegId,
				DataHandler.GetString("CREW_LOG_PASSENGER", false),
				destinationRegId
			});
			if (optionalData != null)
			{
				text = text + DataHandler.GetString("CREW_LOG_BOUNDFOR", false) + optionalData;
			}
			break;
		default:
			UnityEngine.Debug.LogWarning("No matching statement for move reason " + moveReason.ToString());
			break;
		}
		this.LogMessage(text, "Neutral", this.strName);
	}

	public void LogMessage(string strMsg, string strColor, string strOwner)
	{
		if (strMsg == null || strColor == null)
		{
			return;
		}
		if (this.aMessages.Count == 0 || this.aMessages[this.aMessages.Count - 1].strMessage != strMsg)
		{
			JsonLogMessage jsonLogMessage = new JsonLogMessage();
			jsonLogMessage.strName = Guid.NewGuid().ToString();
			jsonLogMessage.strMessage = strMsg;
			jsonLogMessage.strColor = strColor;
			jsonLogMessage.strOwner = strOwner;
			jsonLogMessage.fTime = StarSystem.fEpoch;
			this.aMessages.Add(jsonLogMessage);
			if (this.aMessages.Count > 50)
			{
				this.aMessages.RemoveRange(0, this.aMessages.Count - 50);
			}
		}
		if (CrewSim.objInstance.FinishedLoading && (GUISocialCombat2.coUs == this || GUISocialCombat2.coThem == this))
		{
			GUISocialCombat2.objInstance.UpdateCO(this);
		}
		CrewSim.objInstance.UpdateLog(this, strColor);
	}

	public string GetDebugQueue()
	{
		string text = "None\n";
		if (this.aQueue.Count > 0)
		{
			text = string.Empty;
			foreach (Interaction interaction in this.aQueue)
			{
				if (interaction == null || interaction.objThem == null)
				{
					text += "null\n";
				}
				else
				{
					string text2 = text;
					text = string.Concat(new object[]
					{
						text2,
						interaction.strName,
						"->",
						interaction.objThem.strName,
						": ",
						interaction.fDuration * 60.0 * 60.0
					});
					text += "\n";
				}
			}
		}
		return text;
	}

	public string GetDebugPriorities()
	{
		double num = 0.0;
		string text = "None\n";
		if (this.aPriorities.Count > 0)
		{
			text = string.Empty;
			foreach (Priority priority in this.aPriorities)
			{
				if (priority == null || priority.objCond == null)
				{
					text += "null\n";
				}
				else
				{
					string text2 = text;
					text = string.Concat(new object[]
					{
						text2,
						priority.objCond.strName,
						": ",
						Mathf.RoundToInt(Convert.ToSingle(priority.fValue.ToString("#.00")))
					});
					text += "\n";
					num += priority.fValue;
				}
			}
			text = "Total: " + num.ToString("#.00") + "\n" + text;
		}
		return text;
	}

	public string GetDebugTickers()
	{
		string text = string.Empty;
		foreach (JsonTicker jsonTicker in this.aTickers)
		{
			if (jsonTicker == null)
			{
				text += "null\n";
			}
			else
			{
				text = text + jsonTicker.ToString() + "\n";
			}
		}
		return text;
	}

	public string GetDebugConds(string strPrefix)
	{
		StringBuilder stringBuilder = new StringBuilder();
		foreach (Condition condition in this.mapConds.Values)
		{
			if (strPrefix == null || condition.strName.IndexOf(strPrefix) == 0)
			{
				stringBuilder.Append(condition.strName);
				stringBuilder.Append(": ");
				stringBuilder.AppendLine(condition.fCount.ToString());
			}
		}
		return stringBuilder.ToString();
	}

	public void VisitCOsEarlyOut(CondOwnerVisitor visitor, bool bAllowLocked)
	{
		if (this.bDestroyed)
		{
			UnityEngine.Debug.Log("ERROR: Accessing destroyed object " + this.strName + " - " + this.strID);
			return;
		}
		if (this.aStack == null)
		{
			UnityEngine.Debug.Log("ERROR: Accessing null stack on object " + this.strName + " - " + this.strID);
			return;
		}
		foreach (CondOwner condOwner in this.aStack)
		{
			if (!condOwner.bDestroyed)
			{
				visitor.Visit(condOwner);
				condOwner.VisitCOsEarlyOut(visitor, bAllowLocked);
			}
		}
		foreach (Slot slot in this.GetSlots(false, Slots.SortOrder.HELD_FIRST))
		{
			if (slot != null)
			{
				slot.VisitCOs(visitor, bAllowLocked);
			}
		}
		if (this.objContainer != null)
		{
			this.objContainer.VisitCOs(visitor, bAllowLocked);
		}
	}

	public void VisitCOs(CondOwnerVisitor visitor, bool bAllowLocked)
	{
		if (this.bDestroyed)
		{
			UnityEngine.Debug.Log("ERROR: Accessing destroyed object " + this.strName + " - " + this.strID);
			return;
		}
		if (visitor is CondOwnerVisitorEarlyOut)
		{
			CondOwnerVisitorEarlyOut condOwnerVisitorEarlyOut = (CondOwnerVisitorEarlyOut)visitor;
			if (condOwnerVisitorEarlyOut.CO != null)
			{
				return;
			}
		}
		if (this.aStack == null)
		{
			UnityEngine.Debug.Log("ERROR: Accessing null stack on object " + this.strName + " - " + this.strID);
			return;
		}
		foreach (CondOwner condOwner in this.aStack)
		{
			if (!condOwner.bDestroyed)
			{
				visitor.Visit(condOwner);
				condOwner.VisitCOs(visitor, bAllowLocked);
			}
		}
		foreach (Slot slot in this.GetSlots(false, Slots.SortOrder.HELD_FIRST))
		{
			if (slot != null)
			{
				slot.VisitCOs(visitor, bAllowLocked);
			}
		}
		if (this.objContainer != null)
		{
			this.objContainer.VisitCOs(visitor, bAllowLocked);
		}
	}

	public List<CondOwner> GetCOsEarlyOut(bool bAllowLocked, CondTrigger objCondTrig = null)
	{
		if (!this.HasSubCOs)
		{
			return null;
		}
		this.temp_vHash1.CO = null;
		this.temp_vWrap = CondOwnerVisitorEarlyOut.WrapVisitor(this.temp_vHash1, objCondTrig);
		this.VisitCOs(this.temp_vWrap, bAllowLocked);
		return new List<CondOwner>
		{
			this.temp_vHash1.CO
		};
	}

	public List<CondOwner> GetCOs(bool bAllowLocked, CondTrigger objCondTrig = null)
	{
		if (!this.HasSubCOs)
		{
			return null;
		}
		this.temp_vHash.aHashSet.Clear();
		this.temp_vWrap = CondOwnerVisitorCondTrigger.WrapVisitor(this.temp_vHash, objCondTrig);
		this.VisitCOs(this.temp_vWrap, bAllowLocked);
		return new List<CondOwner>(this.temp_vHash.aHashSet);
	}

	public List<CondOwner> GetCOsSafe(bool bAllowLocked, CondTrigger objCondTrig = null)
	{
		List<CondOwner> cos = this.GetCOs(bAllowLocked, objCondTrig);
		if (cos == null)
		{
			return new List<CondOwner>();
		}
		return cos;
	}

	public static void NullSafeAddRange(ref List<CondOwner> aCOs, List<CondOwner> aAdds)
	{
		if (aAdds == null || aAdds.Count == 0)
		{
			return;
		}
		aCOs.AddRange(aAdds);
	}

	[Obsolete("GetICOs is deprecated, please use GetCOs instead.")]
	public List<CondOwner> GetICOs(bool bAllowLocked, CondTrigger ct = null)
	{
		return this.GetCOs(bAllowLocked, ct);
	}

	public bool IsInsideContainer()
	{
		if (this.slotNow != null)
		{
			return false;
		}
		if (this.coStackHead)
		{
			return this.coStackHead.IsInsideContainer();
		}
		return this.objCOParent != null;
	}

	public void UpdatePriority(Condition objCond)
	{
		double num = this.GetPrefs(objCond.strName) - objCond.fCount;
		for (int i = 0; i < this.aPriorities.Count; i++)
		{
			if (this.aPriorities[i].objCond.strName == objCond.strName)
			{
				this.aPriorities[i].objCond = null;
				this.aPriorities.RemoveAt(i);
				break;
			}
		}
		if (num >= 0.0)
		{
			return;
		}
		if (this.aPriorities.Count == 0)
		{
			this.aPriorities.Add(new Priority(num, objCond));
		}
		else
		{
			bool flag = false;
			for (int j = 0; j < this.aPriorities.Count; j++)
			{
				if (this.aPriorities[j].fValue >= num)
				{
					this.aPriorities.Insert(j, new Priority(num, objCond));
					flag = true;
					break;
				}
			}
			if (!flag)
			{
				this.aPriorities.Add(new Priority(num, objCond));
			}
		}
	}

	public double GetPrefs(string strName)
	{
		double num = 0.0;
		double num2 = 0.0;
		bool flag = false;
		CondRule condRule;
		if (this.mapCondRules.TryGetValue(strName, out condRule))
		{
			flag = (condRule.fPref > 0.0);
			num = condRule.Preference;
		}
		Condition condition;
		if (this.mapConds.TryGetValue(strName, out condition))
		{
			num2 = condition.fCount;
		}
		if (!flag || num2 <= num)
		{
			return num2;
		}
		if (num2 > num)
		{
			return num;
		}
		return 0.0;
	}

	public bool CanSee(CondOwner coTarget)
	{
		return !(coTarget == null);
	}

	public Vector2 GetPos(string strPointName = null, bool bIgnoreParent = false)
	{
		if (!bIgnoreParent && this.objCOParent != null)
		{
			return this.objCOParent.GetPos(strPointName, false);
		}
		if (strPointName == "room")
		{
			Room roomAtWorldCoords = this.ship.GetRoomAtWorldCoords1(this.tf.position, false);
			if (roomAtWorldCoords != null)
			{
				return roomAtWorldCoords.GetRandomWalkableTile().tf.position;
			}
			return this.tf.position;
		}
		else
		{
			Vector2 vector;
			if (strPointName != null && this.mapPoints.TryGetValue(strPointName, out vector))
			{
				Vector3 right = this.tf.right;
				float num = right.x / 16f;
				float num2 = right.y / 16f;
				float num3 = this.tf.position.x + num * vector.x - num2 * vector.y;
				float num4 = this.tf.position.y + num2 * vector.x + num * vector.y;
				if (float.IsNaN(num3))
				{
					num3 = this.tf.position.x;
				}
				if (float.IsNaN(num4))
				{
					num4 = this.tf.position.y;
				}
				return new Vector2(num3, num4);
			}
			return this.tf.position;
		}
	}

	public JsonCondOwnerSave GetJSONSave()
	{
		if (this == null)
		{
			if (this == null)
			{
				UnityEngine.Debug.Log("ERROR: Saving a null");
			}
			else
			{
				UnityEngine.Debug.Log("ERROR: Saving a null " + this.strName + " - " + this.strID);
			}
			UnityEngine.Debug.Break();
			return null;
		}
		JsonCondOwnerSave jsonCondOwnerSave = new JsonCondOwnerSave();
		jsonCondOwnerSave.strID = this.strID;
		jsonCondOwnerSave.strCODef = this.strCODef;
		jsonCondOwnerSave.bAlive = this.bAlive;
		if (this.ship != null)
		{
			jsonCondOwnerSave.strRegIDLast = this.ship.strRegID;
		}
		jsonCondOwnerSave.strFriendlyName = this.FriendlyName;
		if (this.Item != null)
		{
			jsonCondOwnerSave.strIMGPreview = this.Item.ImgOverride;
		}
		if (this.Company != null)
		{
			jsonCondOwnerSave.strComp = this.Company.strName;
			this.ShiftChange(JsonCompany.NullShift, true);
		}
		List<string> list = new List<string>();
		JsonCondOwner condOwnerDef = DataHandler.GetCondOwnerDef(this.strCODef);
		List<string> list2 = new List<string>();
		if (condOwnerDef != null && condOwnerDef.aStartingConds != null)
		{
			list2.AddRange(condOwnerDef.aStartingConds);
		}
		int num = 0;
		string text = string.Empty;
		foreach (string text2 in this.mapConds.Keys)
		{
			if (!(text2 == this.objCondID.strName))
			{
				text = text2 + "=1.0x" + this.mapConds[text2].fCount;
				list.Add(text);
				if (list2.Contains(text) || list2.Contains(text + ".0") || list2.Contains(text + ".00"))
				{
					num++;
				}
			}
		}
		if (num > 1 && num == list2.Count)
		{
			foreach (string text3 in list2)
			{
				if (!list.Remove(text3))
				{
					if (!list.Remove(text3 + ".0"))
					{
						list.Remove(text3 + ".00");
					}
				}
			}
			list.Add("DEFAULT");
			list.TrimExcess();
		}
		jsonCondOwnerSave.aConds = list.ToArray();
		list.Clear();
		List<string> list3 = new List<string>();
		if (condOwnerDef != null && condOwnerDef.aStartingCondRules != null)
		{
			list3.AddRange(condOwnerDef.aStartingCondRules);
		}
		num = 0;
		foreach (string key in this.mapCondRules.Keys)
		{
			string saveInfo = this.mapCondRules[key].GetSaveInfo();
			list.Add(saveInfo);
			if (list3.Contains(saveInfo))
			{
				num++;
			}
		}
		if (num > 1 && num == list3.Count)
		{
			foreach (string item in list3)
			{
				list.Remove(item);
			}
			list.Add("DEFAULT");
			list.TrimExcess();
		}
		jsonCondOwnerSave.aCondRules = list.ToArray();
		list.Clear();
		List<ReplyThread> list4 = new List<ReplyThread>();
		foreach (ReplyThread replyThread in this.aReplies)
		{
			if (replyThread != null)
			{
				list4.Add(replyThread.Clone());
			}
		}
		jsonCondOwnerSave.aReplies = list4.ToArray();
		list4.Clear();
		jsonCondOwnerSave.strPersistentCT = this.strPersistentCT;
		jsonCondOwnerSave.strPersistentCO = this.strPersistentCO;
		jsonCondOwnerSave.strSourceCO = this.strSourceCO;
		jsonCondOwnerSave.strSourceInteract = this.strSourceInteract;
		jsonCondOwnerSave.strCondID = this.objCondID.strName;
		jsonCondOwnerSave.strIdleAnim = this.strIdleAnim;
		jsonCondOwnerSave.strLastSocial = this.strLastSocial;
		if (this.slotNow != null)
		{
			jsonCondOwnerSave.strSlotName = this.slotNow.strName;
		}
		if (this.bSaveMessageLog)
		{
			List<JsonLogMessage> list5 = new List<JsonLogMessage>();
			foreach (JsonLogMessage jsonLogMessage in this.aMessages)
			{
				list5.Add(jsonLogMessage.Clone());
			}
			jsonCondOwnerSave.aMessages2 = list5.ToArray();
			list.Clear();
		}
		jsonCondOwnerSave.aCondZeroes = this.aCondZeroes.ToArray();
		GasContainer gasContainer = this.GasContainer;
		if (gasContainer != null)
		{
			jsonCondOwnerSave.fDGasTemp = gasContainer.fDGasTemp;
			if (double.IsNaN(jsonCondOwnerSave.fDGasTemp))
			{
				jsonCondOwnerSave.fDGasTemp = 0.001;
			}
			if (gasContainer.mapDGasMols == null)
			{
				UnityEngine.Debug.LogWarning("ERROR: null gascontainer dict on " + this.strName + ". Setting to empty for now.");
				gasContainer.mapDGasMols = new Dictionary<string, double>();
			}
			foreach (string text4 in gasContainer.mapDGasMols.Keys)
			{
				list.Add(text4 + "," + gasContainer.mapDGasMols[text4]);
			}
		}
		jsonCondOwnerSave.mapDGasMols = list.ToArray();
		list.Clear();
		jsonCondOwnerSave.fLastICOUpdate = this.fLastICOUpdate;
		jsonCondOwnerSave.fMSRedamageAmount = this.fMSRedamageAmount;
		List<JsonInteractionSave> list6 = new List<JsonInteractionSave>();
		foreach (Interaction interaction in this.aQueue)
		{
			JsonInteractionSave jsonsave = interaction.GetJSONSave();
			if (jsonsave != null)
			{
				list6.Add(jsonsave);
			}
		}
		jsonCondOwnerSave.aQueue = list6.ToArray();
		list6.Clear();
		jsonCondOwnerSave.dictRecentlyTried = new Dictionary<string, double>(this.dictRecentlyTried);
		jsonCondOwnerSave.dictRememberScores = new Dictionary<string, double>(this.dictRememberScores);
		jsonCondOwnerSave.aRememberIAs = this.aRememberIAs.ToArray();
		if (this.mapIAHist != null)
		{
			List<JsonCondHistory> list7 = new List<JsonCondHistory>();
			foreach (KeyValuePair<string, CondHistory> keyValuePair in this.mapIAHist)
			{
				list7.Add(keyValuePair.Value.GetJson());
			}
			jsonCondOwnerSave.mapIAHist2 = list7.ToArray();
		}
		if (this.Pathfinder != null)
		{
			if (this.Pathfinder.tilDest != null)
			{
				jsonCondOwnerSave.nDestTile = this.Pathfinder.tilDest.Index;
				jsonCondOwnerSave.strDestShip = this.Pathfinder.tilDest.coProps.ship.strRegID;
				if (this.Pathfinder.coDest != null)
				{
					jsonCondOwnerSave.strDestCO = this.Pathfinder.coDest.strID;
				}
			}
			else
			{
				jsonCondOwnerSave.nDestTile = -1;
				jsonCondOwnerSave.strDestShip = null;
				jsonCondOwnerSave.strDestCO = null;
			}
		}
		Crew component = base.gameObject.GetComponent<Crew>();
		if (component != null)
		{
			jsonCondOwnerSave.aFaceParts = (component.FaceParts.Clone() as string[]);
			jsonCondOwnerSave.strBodyType = component.BodyType;
		}
		if (this.socUs != null)
		{
			jsonCondOwnerSave.social = this.socUs.GetJSON();
		}
		GUIChargenStack component2 = base.GetComponent<GUIChargenStack>();
		if (component2 != null)
		{
			jsonCondOwnerSave.cgs = component2.GetJSON();
		}
		jsonCondOwnerSave.inventoryX = this.pairInventoryXY.x;
		jsonCondOwnerSave.inventoryY = this.pairInventoryXY.y;
		if (this.aTickers.Count > 0)
		{
			jsonCondOwnerSave.aTickers = new JsonTicker[this.aTickers.Count];
			for (int i = 0; i < this.aTickers.Count; i++)
			{
				jsonCondOwnerSave.aTickers[i] = this.aTickers[i].Clone();
			}
		}
		List<JsonPledgeSave> list8 = new List<JsonPledgeSave>();
		foreach (List<Pledge2> list9 in this.dictPledges.Values)
		{
			foreach (Pledge2 pledge in list9)
			{
				list8.Add(pledge.GetJSON());
			}
		}
		jsonCondOwnerSave.aPledges = list8.ToArray();
		if (this.aStack.Count > 0)
		{
			jsonCondOwnerSave.aStack = new string[this.aStack.Count];
			for (int j = 0; j < this.aStack.Count; j++)
			{
				jsonCondOwnerSave.aStack[j] = this.aStack[j].strID;
			}
		}
		if (this.aLot.Count > 0)
		{
			jsonCondOwnerSave.aLot = new string[this.aLot.Count];
			for (int k = 0; k < this.aLot.Count; k++)
			{
				jsonCondOwnerSave.aLot[k] = this.aLot[k].strID;
			}
		}
		if (this.aMyShips.Count > 0)
		{
			jsonCondOwnerSave.aMyShips = new string[this.aMyShips.Count];
			for (int l = 0; l < this.aMyShips.Count; l++)
			{
				jsonCondOwnerSave.aMyShips[l] = this.aMyShips[l];
			}
		}
		if (this.aFactions.Count > 0)
		{
			jsonCondOwnerSave.aFactions = new string[this.aFactions.Count];
			for (int m = 0; m < this.aFactions.Count; m++)
			{
				jsonCondOwnerSave.aFactions[m] = this.aFactions[m];
			}
		}
		if (this.aAttackIAs.Count > 0)
		{
			jsonCondOwnerSave.aAttackIAs = this.aAttackIAs.ToArray();
		}
		if (this.Company != null)
		{
			this.ShiftChange(this.Company.GetShift(StarSystem.nUTCHour, this), true);
		}
		return jsonCondOwnerSave;
	}

	public void UpdateGravity()
	{
		if (this.bFreezeConds || !this.IsHumanOrRobot)
		{
			return;
		}
		bool flag = this.bLogConds;
		this.bLogConds = false;
		if (this.objCOParent == this)
		{
			UnityEngine.Debug.Log("ERROR: CO is own objCOParent: " + this.strCODef);
		}
		else if (this.objCOParent != null)
		{
			this.objCOParent.UpdateGravity();
		}
		double num = 0.3;
		if (this.ship != null)
		{
			num = this.ship.Gravity;
		}
		this.AddCondAmount("StatEncumbrance", this.GetCondAmount("StatMass") * num - this.GetCondAmount("StatEncumbrance"), 0.0, 0f);
		this.bLogConds = flag;
	}

	public void AddMass(double fMass, bool bSilent = false)
	{
		if (this.bFreezeConds)
		{
			return;
		}
		bool flag = this.bLogConds;
		this.bLogConds = !bSilent;
		this.AddCondAmount("StatMass", fMass, 0.0, 0f);
		if (this.objCOParent == this)
		{
			UnityEngine.Debug.Log("ERROR: CO is own objCOParent: " + this.strCODef);
		}
		else if (this.objCOParent != null)
		{
			this.objCOParent.AddMass(fMass, false);
		}
		if (this.GetCondAmount("IsHuman") > 0.0)
		{
			double num = 0.3;
			if (this.ship != null)
			{
				num = this.ship.Gravity;
			}
			this.AddCondAmount("StatEncumbrance", this.GetCondAmount("StatMass") * num - this.GetCondAmount("StatEncumbrance"), 0.0, 0f);
		}
		this.mapInfo["StatMass"] = this.GetCondAmount("StatMass").ToString("#.00") + "kg";
		this.bLogConds = flag;
	}

	public double GetTotalPrice(CondTrigger ct, bool bIncludeStack, bool skipMarketModifier = true)
	{
		double num = this.GetBasePrice(skipMarketModifier);
		List<CondOwner> cos = this.GetCOs(true, ct);
		if (cos == null)
		{
			return num;
		}
		foreach (CondOwner condOwner in cos)
		{
			num += condOwner.GetTotalPrice(ct, false, skipMarketModifier);
		}
		if (!bIncludeStack)
		{
			foreach (CondOwner condOwner2 in this.aStack)
			{
				num -= condOwner2.GetTotalPrice(ct, true, skipMarketModifier);
			}
		}
		return num;
	}

	public double GetBasePrice(bool skipMarketModifier = true)
	{
		double num = this.GetCondAmount("StatBasePrice");
		if (num == 0.0)
		{
			num = (double)Convert.ToSingle(this.GetCondAmount("StatMass"));
		}
		if (this.HasCond("StatDamageMax"))
		{
			double damageState = this.GetDamageState();
			if (damageState > CondOwner.aDamageThresholds[0])
			{
				num = num;
			}
			else if (damageState > CondOwner.aDamageThresholds[1])
			{
				num *= 0.75;
			}
			else if (damageState > CondOwner.aDamageThresholds[2])
			{
				num *= 0.5;
			}
			else
			{
				num *= 0.25;
			}
		}
		if (this.HasCond("StatGasPressure"))
		{
			GasContainer component = base.GetComponent<GasContainer>();
			if (component != null)
			{
				float totalGasValue = component.GetTotalGasValue();
				num += (double)totalGasValue;
			}
		}
		Condition condition;
		if (this.mapConds.TryGetValue("StatLiqD2O", out condition) && condition != null)
		{
			num += (double)GasContainer.GetGasPrice("H2") * condition.fCount;
		}
		if (this.mapConds.TryGetValue("StatSolidHe3", out condition) && condition != null)
		{
			num += (double)GasContainer.GetGasPrice("He3") * condition.fCount;
		}
		if (!skipMarketModifier)
		{
			double supplyDemandModifier = MarketManager.GetSupplyDemandModifier(this);
			num *= supplyDemandModifier;
		}
		return num;
	}

	public double GetDamageState()
	{
		double condAmount = this.GetCondAmount("StatDamageMax");
		if (condAmount > 0.0)
		{
			return 1.0 - this.GetCondAmount("StatDamage") / condAmount;
		}
		return 1.0;
	}

	public string GetDamageDescriptor()
	{
		if (!this.HasCond("StatDamageMax"))
		{
			return string.Empty;
		}
		double damageState = this.GetDamageState();
		if (damageState > CondOwner.aDamageThresholds[0])
		{
			return DataHandler.GetString("DAMAGE_DESC_0", false);
		}
		if (damageState > CondOwner.aDamageThresholds[1])
		{
			return DataHandler.GetString("DAMAGE_DESC_1", false);
		}
		if (damageState > CondOwner.aDamageThresholds[2])
		{
			return DataHandler.GetString("DAMAGE_DESC_2", false);
		}
		return DataHandler.GetString("DAMAGE_DESC_3", false);
	}

	public bool GetIsLikeNew()
	{
		if (!this.HasCond("StatDamageMax"))
		{
			return true;
		}
		double damageState = this.GetDamageState();
		return damageState > CondOwner.aDamageThresholds[0];
	}

	public void ClaimShip(string strRegID)
	{
		if (strRegID == null)
		{
			return;
		}
		if (this.HasCond("IsPlayer"))
		{
			Ship shipByRegID = CrewSim.system.GetShipByRegID(strRegID);
			if (shipByRegID == null)
			{
				return;
			}
			if (shipByRegID.IsStation(false) || shipByRegID.IsStationHidden(false))
			{
				return;
			}
		}
		if (!this.aMyShips.Contains(strRegID))
		{
			this.aMyShips.Add(strRegID);
		}
		if (this.Company != null && this.socUs != null)
		{
			JsonPersonSpec personSpec = DataHandler.GetPersonSpec("RELCrewSubordinate");
			List<string> matchingRelationsAll = this.socUs.GetMatchingRelationsAll(personSpec);
			foreach (string text in matchingRelationsAll)
			{
				if (!(text == this.strID))
				{
					CondOwner condOwner = null;
					if (DataHandler.mapCOs.TryGetValue(text, out condOwner))
					{
						condOwner.ClaimShip(strRegID);
					}
				}
			}
		}
	}

	public void UnclaimShip(string strRegID)
	{
		if (strRegID == null)
		{
			return;
		}
		if (this.aMyShips.Contains(strRegID))
		{
			this.aMyShips.Remove(strRegID);
		}
	}

	public bool OwnsShip(string strRegID)
	{
		return strRegID != null && this.aMyShips.Contains(strRegID);
	}

	public List<string> GetShipsOwned()
	{
		return this.aMyShips;
	}

	public void SetShipsOwned(List<string> aOwned)
	{
		if (aOwned == null)
		{
			return;
		}
		this.aMyShips.Clear();
		foreach (string item in aOwned)
		{
			this.aMyShips.Add(item);
		}
		string text = string.Empty;
		foreach (string str in this.aMyShips)
		{
			text = text + str + ", ";
		}
		UnityEngine.Debug.Log(this.strID + " restting ships. Now claims ship(s) " + text);
	}

	public void AddFaction(JsonFaction jf)
	{
		if (jf == null || this.aFactions.IndexOf(jf.strName) >= 0)
		{
			return;
		}
		this.aFactions.Add(jf.strName);
		if (this.socUs != null)
		{
			if (jf.aMembers.IndexOf(this.strID) < 0)
			{
				jf.aMembers.Add(this.strID);
			}
			JsonCompany company = CrewSim.system.GetCompany(jf.strCompany);
			if (company != null)
			{
				company.AddNewMember(this.strID);
				this.Company = company;
			}
		}
	}

	public void RemoveFaction(JsonFaction jf)
	{
		if (jf == null)
		{
			return;
		}
		this.aFactions.Remove(jf.strName);
		if (this.socUs != null)
		{
			jf.aMembers.Remove(this.strID);
		}
	}

	public bool HasFaction(string strFaction)
	{
		return !string.IsNullOrEmpty(strFaction) && this.aFactions.IndexOf(strFaction) >= 0;
	}

	public void ApplyFactionReps(CondOwner coDoing, float fChange)
	{
		if (coDoing == null || coDoing == this || fChange == 0f)
		{
			return;
		}
		List<string> allFactions = coDoing.GetAllFactions();
		if (allFactions.Count == 0)
		{
			return;
		}
		if (!this.HasCond("IsSocial"))
		{
			if (this.ship == null)
			{
				return;
			}
			bool flag = false;
			foreach (CondOwner condOwner in this.ship.GetPeople(true))
			{
				if (!(condOwner == coDoing))
				{
					if (condOwner.bAlive && !condOwner.HasCond("Unconscious"))
					{
						if (Visibility.IsCondOwnerLOSVisibleFromCo(coDoing, condOwner))
						{
							if (condOwner.SharesFactionsWith(this))
							{
								if (fChange >= 0f || condOwner.pspec == null || condOwner.pspec.IsCOMyMother(CondOwner.JPSRelReportFactionNeg, coDoing))
								{
									flag = true;
									break;
								}
							}
						}
					}
				}
			}
			if (!flag)
			{
				return;
			}
		}
		foreach (string text in allFactions)
		{
			JsonFaction faction = CrewSim.system.GetFaction(text);
			if (faction != null)
			{
				foreach (string text2 in this.aFactions)
				{
					JsonFaction faction2 = CrewSim.system.GetFaction(text2);
					if (faction2 != null)
					{
						float num = fChange;
						if (faction.aMembers.Count > 1)
						{
							num = fChange / (float)faction.aMembers.Count;
						}
						if (faction2.aMembers.Count > 1)
						{
							num /= (float)faction2.aMembers.Count;
						}
						faction2.ApplyFactionRep(faction.strName, num);
					}
				}
			}
		}
	}

	public float GetFactionScore(string strFaction)
	{
		if (string.IsNullOrEmpty(strFaction))
		{
			return 0f;
		}
		float num = 0f;
		foreach (string text in this.aFactions)
		{
			JsonFaction faction = CrewSim.system.GetFaction(text);
			if (faction != null)
			{
				num += faction.GetFactionScore(strFaction);
			}
		}
		return num;
	}

	public float GetFactionScore(List<string> aFactionsThem)
	{
		if (aFactionsThem == null)
		{
			return 0f;
		}
		float num = 0f;
		foreach (string strFaction in aFactionsThem)
		{
			num += this.GetFactionScore(strFaction);
		}
		return num;
	}

	public List<string> GetAllFactions()
	{
		if (this.aFactions == null)
		{
			return new List<string>();
		}
		return new List<string>(this.aFactions);
	}

	public bool SharesFactionsWith(CondOwner coThem)
	{
		if (coThem == null)
		{
			return false;
		}
		foreach (string strFaction in this.aFactions)
		{
			if (coThem.HasFaction(strFaction))
			{
				return true;
			}
		}
		return false;
	}

	public bool SharesFactionsWith(List<JsonFaction> aFactionsThem)
	{
		if (aFactionsThem == null || aFactionsThem.Count == 0)
		{
			return false;
		}
		foreach (JsonFaction jsonFaction in aFactionsThem)
		{
			if (this.aFactions.Contains(jsonFaction.strName))
			{
				return true;
			}
		}
		return false;
	}

	public void SetFactions(List<JsonFaction> aJFs, bool bRemoveOld)
	{
		if (bRemoveOld)
		{
			this.aFactions.Clear();
		}
		if (aJFs == null)
		{
			return;
		}
		foreach (JsonFaction jf in aJFs)
		{
			this.AddFaction(jf);
		}
	}

	public void SetCrewZOffset()
	{
		Vector3 position = this.tf.position;
		position.z = 0f;
		this.tf.position = position;
	}

	public string GetMessageLog(int nTail = -1)
	{
		if (nTail < 1)
		{
			nTail = this.aMessages.Count;
		}
		int num = this.aMessages.Count - nTail;
		if (num < 0)
		{
			num = 0;
		}
		int count = this.aMessages.Count;
		if (this.messageLogSB == null)
		{
			this.messageLogSB = new StringBuilder(5000);
		}
		else
		{
			this.messageLogSB.Length = 0;
		}
		for (int i = num; i < count; i++)
		{
			this.messageLogSB.Append("<color=#");
			this.messageLogSB.Append(DataHandler.GetColorHTML(this.aMessages[i].strColor));
			this.messageLogSB.Append(">");
			bool flag = this.aMessages[i].strOwner != this.strName;
			if (flag)
			{
				this.messageLogSB.Append("<align=\"right\"><alpha=#80>");
			}
			this.messageLogSB.Append(this.aMessages[i].strMessage);
			if (flag)
			{
				this.messageLogSB.Append("<alpha=#FF></align>");
			}
			this.messageLogSB.Append("</color>");
			this.messageLogSB.AppendLine();
		}
		return this.messageLogSB.ToString();
	}

	public List<string> GetJobActions(string strJobType)
	{
		JsonCondOwner jsonCondOwner = null;
		List<string> result;
		if (DataHandler.dictCOs.TryGetValue(this.strCODef, out jsonCondOwner))
		{
			result = jsonCondOwner.GetJobActions(strJobType.ToLower());
		}
		else if (base.GetComponent<COOverlay>() != null)
		{
			COOverlay component = base.GetComponent<COOverlay>();
			result = component.GetJobActions(strJobType);
		}
		else
		{
			result = new List<string>();
		}
		return result;
	}

	public bool HasQueuedInteraction(string strName)
	{
		if (this.aQueue == null)
		{
			return false;
		}
		foreach (Interaction interaction in this.aQueue)
		{
			if (interaction.strName == strName)
			{
				return true;
			}
		}
		return false;
	}

	public void DebugInv(StringBuilder sb, string strPrefix)
	{
		if (this.aStack == null)
		{
			return;
		}
		if (this.aStack.Count > 0)
		{
			sb.AppendLine(strPrefix + this.strID + " stack contains:");
		}
		foreach (CondOwner condOwner in this.aStack)
		{
			sb.AppendLine(string.Concat(new object[]
			{
				strPrefix,
				this.strID,
				"->",
				condOwner.strID,
				".bDestroyed = ",
				condOwner.bDestroyed
			}));
			foreach (CondOwner condOwner2 in condOwner.aStack)
			{
				condOwner2.DebugInv(sb, strPrefix + this.strID + "->");
			}
		}
	}

	public Wound GetWoundLocation(bool bBlunt, bool bCut)
	{
		if (!this.HasCond("StatWoundFraction"))
		{
			return null;
		}
		List<CondOwner> cos = this.GetCOs(true, Wound.CTWound);
		if (cos == null)
		{
			return null;
		}
		List<Wound> list = new List<Wound>();
		float num = 0f;
		foreach (CondOwner condOwner in cos)
		{
			if (!(condOwner == null))
			{
				Wound component = condOwner.GetComponent<Wound>();
				if (!(component == null))
				{
					if (!bBlunt || component.Bluntable)
					{
						if (!bCut || component.Cuttable)
						{
							list.Add(component);
							num += component.fHitChance;
						}
					}
				}
			}
		}
		if (list.Count == 0)
		{
			return null;
		}
		float num2 = MathUtils.Rand(0f, num, MathUtils.RandType.Flat, null);
		num = 0f;
		foreach (Wound wound in list)
		{
			num += wound.fHitChance;
			if (num2 <= num)
			{
				return wound;
			}
		}
		return null;
	}

	public List<Wound> GetAllWounds()
	{
		List<Wound> list = new List<Wound>();
		List<CondOwner> cos = this.GetCOs(true, Wound.CTWound);
		if (cos == null)
		{
			return list;
		}
		foreach (CondOwner condOwner in cos)
		{
			if (!(condOwner == null))
			{
				Wound component = condOwner.GetComponent<Wound>();
				if (!(component == null))
				{
					list.Add(component);
				}
			}
		}
		return list;
	}

	public bool AlwaysLoad
	{
		get
		{
			if (this.IsHumanOrRobot)
			{
				return true;
			}
			if (this.objCOParent != null)
			{
				return this.objCOParent.IsHumanOrRobot || this.objCOParent.AlwaysLoad;
			}
			return this.coStackHead != null && this.coStackHead.AlwaysLoad;
		}
	}

	public int LotCount
	{
		get
		{
			if (this.aLot != null)
			{
				return this.aLot.Count;
			}
			return 0;
		}
	}

	public int QueueCount
	{
		get
		{
			if (this.aQueue != null)
			{
				return this.aQueue.Count;
			}
			return 0;
		}
	}

	public Ship ship
	{
		get
		{
			return this._objShip;
		}
		set
		{
			this._objShip = value;
			this.UpdateGravity();
		}
	}

	private void KO()
	{
		if (!this.bAlive || this.HasCond("Unconscious"))
		{
			return;
		}
		UnityEngine.Debug.Log("NPC KOed " + this.FriendlyName + " on " + this.ship.ToString());
		this.AICancelAll(null);
		this.AddCondAmount("Unconscious", 1.0, 0.0, 0f);
		this.SetAnimState(Interaction.dictAnims["Dead"]);
		this.strIdleAnim = "Dead";
		if (this.Pathfinder != null)
		{
			this.Pathfinder.Reset();
		}
		if (this.compSlots != null && this.ship != null && this.ship.LoadState > Ship.Loaded.Shallow)
		{
			List<CondOwner> cos = this.compSlots.GetCOs("heldL", false, null);
			cos.AddRange(this.compSlots.GetCOs("heldR", false, null));
			cos.AddRange(this.compSlots.GetCOs("drag", false, null));
			this.DropSlottedItems(cos);
		}
		if (this.ship.ShipCO != null)
		{
			Interaction interaction = DataHandler.GetInteraction("Unconscious", null, false);
			this.QueueInteraction(this.ship.ShipCO, interaction, true);
		}
		if (CrewSim.GetSelectedCrew() == this)
		{
			CrewSim.objInstance.CycleCrew(null);
		}
	}

	private void KOWake()
	{
		if (!this.bAlive || !this.HasCond("Unconscious"))
		{
			return;
		}
		if (this.ship.ShipCO != null)
		{
			Interaction interaction = DataHandler.GetInteraction("SeekSleepSimpleWake", null, false);
			this.QueueInteraction(this.ship.ShipCO, interaction, true);
		}
	}

	public bool Kill
	{
		get
		{
			return this.bAlive;
		}
		set
		{
			if (this.bIgnoreKill)
			{
				return;
			}
			this.bAlive = !value;
			string key = "Dead";
			if (this.bAlive)
			{
				this.ZeroCondAmount("IsDead");
				key = "Idle";
			}
			else
			{
				this.AddCondAmount("IsDead", 1.0, 0.0, 0f);
				while (this.aQueue != null && this.aQueue.Count > 0)
				{
					this.ClearInteraction(this.aQueue[0], false);
				}
			}
			this.strIdleAnim = key;
			this.SetAnimState(Interaction.dictAnims[key]);
			if (!this.bAlive)
			{
				if (this.HasCond("IsPlayer"))
				{
					CanvasManager.instance.GameOver(this);
				}
				else if (CrewSim.GetSelectedCrew() == this)
				{
					CrewSim.objInstance.CycleCrew(null);
				}
				if (this.HasCond("IsHuman"))
				{
					UnityEngine.Debug.Log(this.strID + " died.");
					if (this.HasCond("IsPlayerCrew") && !this.bFreezeCondRules)
					{
						if (this.ship != null && this.ship.LoadState >= Ship.Loaded.Edit)
						{
							List<CondOwner> list = new List<CondOwner>();
							list.AddRange(this.ship.GetPeople(true));
							foreach (CondOwner condOwner in list)
							{
								condOwner.LogMessage(this.FriendlyName + DataHandler.GetString("CREW_DEAD", false), "Bad", this.strID);
							}
							AudioManager.am.SuggestMusic("Loss", true);
						}
						if (this.Company != null)
						{
							this.Company.DismissMember(this.strID, CrewSim.coPlayer);
						}
					}
					AIShipManager.ValidateCrew(this);
				}
				if (this.compSlots != null && this.ship != null && this.ship.LoadState > Ship.Loaded.Shallow)
				{
					List<CondOwner> cos = this.compSlots.GetCOs("heldL", false, null);
					cos.AddRange(this.compSlots.GetCOs("heldR", false, null));
					cos.AddRange(this.compSlots.GetCOs("drag", false, null));
					this.DropSlottedItems(cos);
				}
				this.AddCondAmount("DisallowPaperDoll", 1.0, 0.0, 0f);
				if (this._pfComponentReference != null)
				{
					this._pfComponentReference.HideFootprints();
					this._pfComponentReference.enabled = false;
				}
				BodyTemp component = base.GetComponent<BodyTemp>();
				if (component != null)
				{
					component.enabled = false;
				}
				Heater component2 = base.GetComponent<Heater>();
				if (component2 != null)
				{
					component2.enabled = false;
				}
				GasPump component3 = base.GetComponent<GasPump>();
				if (component3 != null)
				{
					component3.enabled = false;
				}
			}
			else
			{
				this.ZeroCondAmount("DisallowPaperDoll");
				if (this._pfComponentReference != null)
				{
					this._pfComponentReference.enabled = true;
				}
				BodyTemp component4 = base.GetComponent<BodyTemp>();
				if (component4 != null)
				{
					component4.enabled = true;
				}
				Heater component5 = base.GetComponent<Heater>();
				if (component5 != null)
				{
					component5.enabled = true;
				}
				GasPump component6 = base.GetComponent<GasPump>();
				if (component6 != null)
				{
					component6.enabled = true;
				}
			}
		}
	}

	public void DropSlottedItems(List<CondOwner> aCOs)
	{
		if (aCOs == null)
		{
			return;
		}
		aCOs = aCOs.Distinct<CondOwner>().ToList<CondOwner>();
		for (int i = aCOs.Count - 1; i >= 0; i--)
		{
			CondOwner condOwner = aCOs[i];
			if (condOwner == null || condOwner.slotNow == null)
			{
				aCOs.RemoveAt(i);
			}
			else if (condOwner.HasCond("IsHiddenInv") || condOwner.HasCond("IsSystem") || condOwner.bSlotLocked)
			{
				aCOs.RemoveAt(i);
			}
		}
		foreach (CondOwner objCO in aCOs)
		{
			CondOwner condOwner2 = this.DropCO(objCO, false, null, 0f, 0f, true, null);
			if (condOwner2 != null)
			{
				if (this.ship != null)
				{
					UnityEngine.Debug.LogWarning("Unable to drop slotted item " + condOwner2.strName + ", Readding to ship");
					this.ship.AddCO(condOwner2, true);
				}
				else
				{
					UnityEngine.Debug.LogWarning("Unable to drop slotted item " + condOwner2.strName + ", Ship already null! Destroying");
					condOwner2.Destroy();
				}
			}
		}
	}

	public string PrintIAH()
	{
		string text = string.Empty;
		foreach (CondHistory condHistory in this.mapIAHist.Values)
		{
			text += condHistory.Print(this.strName);
		}
		return text;
	}

	public override string ToString()
	{
		return this.strName;
	}

	public bool bBusy
	{
		get
		{
			return !this.bDestroyed && (this.aQueue.Count > 0 || (this.objCOParent != null && this.objCOParent.aQueue.Count > 0) || (GUISocialCombat2.coUs == this || GUISocialCombat2.coThem == this) || (CrewSim.GetSelectedCrew() == this && CrewSim.bRaiseUI));
		}
	}

	public string strID
	{
		get
		{
			return this._strID;
		}
		set
		{
			if (value == null)
			{
				return;
			}
			if (this.strID != null && DataHandler.mapCOs.ContainsKey(this.strID) && DataHandler.mapCOs[this.strID] == this)
			{
				DataHandler.mapCOs.Remove(this.strID);
			}
			DataHandler.mapCOs[value] = this;
			if (this.ship != null)
			{
				this.ship.ChangeCOID(this, value);
			}
			CrewSim.objInstance.workManager.ChangeCOID(this.strID, value);
			if (this.Company != null)
			{
				if (this.Company.strName.IndexOf(this._strID) >= 0)
				{
					string text = value + "'s Company";
					this.Company.strName = text;
				}
				if (this.Company.mapRoster.ContainsKey(this._strID))
				{
					JsonCompanyRules value2 = this.Company.mapRoster[this._strID];
					this.Company.mapRoster.Remove(this._strID);
					this.Company.mapRoster[value] = value2;
				}
			}
			if (this.socUs != null)
			{
				CrewSim.system.RenameFaction(value, CrewSim.system.GetFaction(this._strID));
			}
			MonoSingleton<GUIRenderTargets>.Instance.UpdateName(this._strID, value);
			Ledger.UpdateCOID(this._strID, value);
			this._strID = value;
		}
	}

	public bool Selected
	{
		get
		{
			return !this.bDestroyed && this.goBracket != null && this.goBracket.activeInHierarchy;
		}
		set
		{
			if (this.bDestroyed || (!value && this.goBracket == null))
			{
				return;
			}
			if (this.goBracket == null && value)
			{
				this.goBracket = UnityEngine.Object.Instantiate<GameObject>(CondOwner.selectionBracket, this.tf);
				this.goBracket.transform.position = new Vector3(this.goBracket.transform.position.x, this.goBracket.transform.position.y, -10f);
				this.goBracket.SetActive(true);
				this.goBracket.transform.localScale = new Vector3(2f / this.tf.localScale.x, 2f / this.tf.localScale.y, 1f);
				if (base.gameObject.GetComponent<Crew>() != null)
				{
					this.goBracket.GetComponent<SpriteRenderer>().size = new Vector2(this.tf.GetComponent<BoxCollider>().size.x / 2f, this.tf.GetComponent<BoxCollider>().size.y / 2f);
				}
				else
				{
					this.goBracket.GetComponent<SpriteRenderer>().size = new Vector2(this.mr.bounds.size.x / 2f, this.mr.bounds.size.y / 2f);
				}
			}
			else if (this.goBracket != null && !value)
			{
				UnityEngine.Object.Destroy(this.goBracket);
				this.goBracket = null;
			}
		}
	}

	public bool Highlight
	{
		get
		{
			return !this.bDestroyed && base.gameObject.layer == LayerMask.NameToLayer("Tile Helpers");
		}
		set
		{
			if (this.bDestroyed)
			{
				return;
			}
			if (base.gameObject.layer == LayerMask.NameToLayer("Ship Offscreen"))
			{
				return;
			}
			if (value)
			{
				base.gameObject.layer = LayerMask.NameToLayer("Tile Helpers");
			}
			else
			{
				base.gameObject.layer = LayerMask.NameToLayer("Default");
			}
		}
	}

	public bool HighlightObjective
	{
		get
		{
			return !this.bDestroyed && base.gameObject.layer == LayerMask.NameToLayer("ObjectiveHighlight");
		}
		set
		{
			if (this.bDestroyed)
			{
				return;
			}
			Crew component = base.GetComponent<Crew>();
			if (value)
			{
				base.gameObject.layer = LayerMask.NameToLayer("ObjectiveHighlight");
			}
			else
			{
				base.gameObject.layer = LayerMask.NameToLayer("Default");
			}
			if (component != null)
			{
				component.HighlightObjective = value;
			}
		}
	}

	public bool DimLights
	{
		get
		{
			if (this.bDestroyed)
			{
				return false;
			}
			if (this.Item != null)
			{
				using (List<Visibility>.Enumerator enumerator = this.Item.aLights.GetEnumerator())
				{
					if (enumerator.MoveNext())
					{
						Visibility visibility = enumerator.Current;
						return visibility.gameObject.activeInHierarchy;
					}
				}
			}
			return this.pwr != null && this.pwr.Hide;
		}
		set
		{
			if (this.bDestroyed)
			{
				return;
			}
			if (this.Item != null)
			{
				foreach (Visibility visibility in this.Item.aLights)
				{
					visibility.gameObject.SetActive(!value);
					if (!value)
					{
						visibility.bRedraw = true;
					}
				}
				foreach (Transform transform in this.Item.dictLightSprites.Keys)
				{
					transform.gameObject.SetActive(!value);
				}
			}
			if (this.pwr != null)
			{
				this.pwr.Hide = value;
			}
		}
	}

	public bool Visible
	{
		get
		{
			return !this.bDestroyed && this.mr.enabled;
		}
		set
		{
			if (this.bDestroyed)
			{
				return;
			}
			this.mr.enabled = value;
			if (this.mc != null)
			{
				this.mc.enabled = value;
			}
			this.DimLights = !value;
			if (this.txtStack != null)
			{
				this.txtStack.gameObject.SetActive(value);
			}
		}
	}

	public int StackCount
	{
		get
		{
			if (this.bDestroyed)
			{
				return 0;
			}
			return this.aStack.Count + 1;
		}
	}

	public List<CondOwner> StackAsList
	{
		get
		{
			List<CondOwner> list = new List<CondOwner>();
			list.AddRange(this.aStack);
			list.Add(this);
			return list;
		}
	}

	public List<CondOwner> GetSingleOrStack(bool bWholeStack)
	{
		if (!bWholeStack)
		{
			return new List<CondOwner>
			{
				this
			};
		}
		if (this.coStackHead)
		{
			return this.coStackHead.StackAsList;
		}
		return this.StackAsList;
	}

	public List<Slot> GetSlots(bool bDeep, Slots.SortOrder sortOrder = Slots.SortOrder.HELD_FIRST)
	{
		if (this.compSlots == null)
		{
			List<Slot> result;
			if ((result = CondOwner._emptySlotsResult) == null)
			{
				result = (CondOwner._emptySlotsResult = new List<Slot>());
			}
			return result;
		}
		if (sortOrder == Slots.SortOrder.BY_DEPTH)
		{
			return this.compSlots.GetSlotsDepthFirst(bDeep);
		}
		if (sortOrder != Slots.SortOrder.CHILD_FIRST)
		{
			return this.compSlots.GetSlotsHeldFirst(bDeep);
		}
		return this.compSlots.GetSlotsChildFirst(bDeep);
	}

	public Slots compSlots
	{
		get
		{
			return this._compSlots;
		}
		private set
		{
			this._compSlots = value;
		}
	}

	public string FriendlyName
	{
		get
		{
			if (this.strNameFriendly != null)
			{
				return this.strNameFriendly;
			}
			return this.strName;
		}
	}

	public string ShortName
	{
		get
		{
			if (this.pspec != null)
			{
				return this.strName;
			}
			if (this.strNameShort != null)
			{
				return this.strNameShort;
			}
			return this.FriendlyName;
		}
	}

	public JsonCompany Company
	{
		get
		{
			return this.objCompany;
		}
		set
		{
			this.objCompany = value;
			if (value == null)
			{
				this.ShiftChange(JsonCompany.NullShift, this.bFreezeConds);
			}
			else
			{
				this.ShiftChange(value.GetShift(StarSystem.nUTCHour, this), this.bFreezeConds);
			}
			PlayerMarker.AddMarker(this);
		}
	}

	public Vector2 TLTileCoords
	{
		get
		{
			if (this.bDestroyed)
			{
				return default(Vector2);
			}
			Vector2 vector = this.tf.position;
			return new Vector2(vector.x - ((float)this.Item.nWidthInTiles / 2f - 0.5f) * 1f, vector.y + ((float)this.Item.nHeightInTiles / 2f - 0.5f) * 1f);
		}
	}

	public string Skin
	{
		get
		{
			string result = "A";
			if (this.faceRef != null)
			{
				Crew component = base.GetComponent<Crew>();
				if (component != null)
				{
					result = FaceAnim2.GetFaceGroups(component.FaceParts)[0];
				}
			}
			return result;
		}
	}

	public bool IsRobot
	{
		get
		{
			bool? isRobot = this._isRobot;
			if (isRobot == null)
			{
				CondTrigger condTrigger = DataHandler.GetCondTrigger("TIsRobot");
				this._isRobot = new bool?(condTrigger.Triggered(this, null, false));
			}
			bool? isRobot2 = this._isRobot;
			return isRobot2.Value;
		}
	}

	public bool IsHumanOrRobot
	{
		get
		{
			bool? isHumanOrRobot = this._isHumanOrRobot;
			if (isHumanOrRobot == null)
			{
				this._isHumanOrRobot = new bool?(this.HasCond("IsHuman") || this.HasCond("IsRobot"));
			}
			bool? isHumanOrRobot2 = this._isHumanOrRobot;
			return isHumanOrRobot2.Value;
		}
	}

	private static CondTrigger CTCanAIOrder
	{
		get
		{
			if (CondOwner._ctCanAIOrder == null)
			{
				CondOwner._ctCanAIOrder = DataHandler.GetCondTrigger("TCanAIOrder");
			}
			return CondOwner._ctCanAIOrder;
		}
	}

	private static CondTrigger CTCanWalk
	{
		get
		{
			if (CondOwner._ctCanWalk == null)
			{
				CondOwner._ctCanWalk = DataHandler.GetCondTrigger("TCanWalk");
			}
			return CondOwner._ctCanWalk;
		}
	}

	private static CondTrigger CTIsProneAwake
	{
		get
		{
			if (CondOwner._ctIsProneAwake == null)
			{
				CondOwner._ctIsProneAwake = DataHandler.GetCondTrigger("TIsProneAwake");
			}
			return CondOwner._ctIsProneAwake;
		}
	}

	private static JsonPersonSpec JPSRelReportFactionNeg
	{
		get
		{
			if (CondOwner._jpsRelReportFactionNeg == null)
			{
				CondOwner._jpsRelReportFactionNeg = DataHandler.GetPersonSpec("RELReportFactionNeg");
			}
			return CondOwner._jpsRelReportFactionNeg;
		}
	}

	public double RecentlyTried(Interaction ia, bool allowIaOnly = false)
	{
		if (ia == null || ia.objThem == null)
		{
			return -1.0;
		}
		double result = -1.0;
		if (this.dictRecentlyTried.TryGetValue(ia.objThem.strID + ia.strName, out result))
		{
			return result;
		}
		if (allowIaOnly && this.dictRecentlyTried.TryGetValue(ia.strName, out result))
		{
			return result;
		}
		return -1.0;
	}

	public void AddRecentlyTried(string strRef)
	{
		if (string.IsNullOrEmpty(strRef))
		{
			return;
		}
		this.dictRecentlyTried[strRef] = StarSystem.fEpoch;
	}

	public static Loot FreeWillLoot
	{
		get
		{
			if (CondOwner._FreeWillLoot == null)
			{
				CondOwner._FreeWillLoot = DataHandler.GetLoot("CONDAIFreeWillLoot");
			}
			return CondOwner._FreeWillLoot;
		}
	}

	public static Loot FreeWillPenalty
	{
		get
		{
			if (CondOwner._FreeWillPenalty == null)
			{
				CondOwner._FreeWillPenalty = DataHandler.GetLoot("CONDAIFreeWillPenalty");
			}
			return CondOwner._FreeWillPenalty;
		}
	}

	private void OnDestroy()
	{
		DataHandler.debugCOCount--;
	}

	public void PrintCondRules()
	{
		foreach (CondRule condRule in this.mapCondRules.Values)
		{
			string text = string.Concat(new object[]
			{
				condRule.strCond,
				" = ",
				this.GetCondAmount(condRule.strCond),
				"; "
			});
			string text2 = text;
			text = string.Concat(new object[]
			{
				text2,
				"CurrentThresh: ",
				Array.IndexOf<CondRuleThresh>(condRule.aThresholds, condRule.GetCurrentThresh(this)),
				"; "
			});
			string value = "Dc" + condRule.strCond.Replace("Stat", string.Empty);
			foreach (string text3 in this.mapConds.Keys)
			{
				if (text3.IndexOf(value) >= 0)
				{
					text = text + "DC: " + text3;
					break;
				}
			}
			UnityEngine.Debug.LogWarning(text);
		}
	}

	public float CondPercentage(string numer, string denom)
	{
		double num = 0.0;
		double num2 = 0.0;
		if (numer != null && numer != string.Empty)
		{
			num = this.GetCondAmount(numer);
		}
		if (denom != null && denom != string.Empty)
		{
			num2 = this.GetCondAmount(denom);
		}
		if (num < 0.0)
		{
			num = 0.0;
		}
		double num3;
		if (num2 <= 0.0)
		{
			num3 = 0.0;
		}
		else
		{
			num3 = num / num2;
		}
		return (float)num3;
	}

	public void AddReplyThread(double fEpoch, string strID, Interaction objInteraction)
	{
		if (objInteraction == null || strID == null)
		{
			return;
		}
		for (int i = this.aReplies.Count - 1; i >= 0; i--)
		{
			ReplyThread replyThread = this.aReplies[i];
			if (replyThread.strID == strID && replyThread.jis.strName == objInteraction.strName)
			{
				this.aReplies.RemoveAt(i);
			}
		}
		this.aReplies.Add(new ReplyThread
		{
			fEpoch = fEpoch,
			strID = strID,
			jis = objInteraction.GetJSONSave()
		});
		if (CrewSim.GetSelectedCrew() == this)
		{
			CondOwner.UpdateWaitingReplies.Invoke(this.aReplies);
		}
	}

	public void SetUpBehaviours()
	{
		if (!this.HasCond("StatInstallProgressMax"))
		{
			this.AddCondAmount("StatInstallProgressMax", 1000.0, 0.0, 0f);
		}
		if (!this.HasCond("StatUninstallProgressMax"))
		{
			this.AddCondAmount("StatUninstallProgressMax", 1000.0, 0.0, 0f);
		}
		if (!this.HasCond("StatRepairProgressMax"))
		{
			this.AddCondAmount("StatRepairProgressMax", 1000.0, 0.0, 0f);
		}
		if (this.HasCond("StatDamageMax") && !this.HasCond("IsSystem"))
		{
			if (!this.HasCond("IsDestructable") && !this.HasCond("IsIndestructable"))
			{
				if (this.HasCond("IsTechnology"))
				{
					this.AddCommand("Destructable,StatDamage,ACTTechDestroy,StatDamageMax,1.0");
				}
				else if (this.HasCond("IsMechanical"))
				{
					this.AddCommand("Destructable,StatDamage,ACTMechDestroy,StatDamageMax,1.0");
				}
				else
				{
					this.AddCommand("Destructable,StatDamage,ACTDefaultDestroy,StatDamageMax,1.0");
				}
			}
			if (!this.HasCond("IsUndamageable") && (this.HasCond("IsInstalled") || this.HasCond("IsSolid")))
			{
				if ((double)this.tf.localScale.x > 1.0 || (double)this.tf.localScale.y > 1.0)
				{
					if (this.aInteractions.IndexOf("ACTBashBig") < 0 && this.aInteractions.IndexOf("ACTBash") < 0)
					{
						this.aInteractions.Add("ACTBashBig");
					}
				}
				else if (this.aInteractions.IndexOf("ACTBashBig") < 0 && this.aInteractions.IndexOf("ACTBash") < 0)
				{
					this.aInteractions.Add("ACTBash");
				}
				if (!this.HasCond("IsDamageable"))
				{
					this.AddCondAmount("IsDamageable", 1.0, 0.0, 0f);
				}
			}
		}
	}

	public void PlayHitAnim(double fDmgBlunt, double fDmgCut)
	{
		if (this.bAlive && !this.HasCond("Unconscious") && !this.HasCond("Prone"))
		{
			if (fDmgBlunt > 5.0 || fDmgCut > 5.0)
			{
				this.SetAnimTrigger("Hit");
			}
			else
			{
				this.SetAnimTrigger("HitLess");
			}
		}
	}

	public void BreakIn(float percentage = 1f, bool allowMultiple = false, double fRepairTarget = 0.0)
	{
		this.SetUpBehaviours();
		double condAmount = this.GetCondAmount("StatDamageMax");
		if (condAmount > 0.0 && !this.HasCond("IsSystem"))
		{
			bool flag = false;
			double condAmount2 = this.GetCondAmount("StatDamage");
			if (allowMultiple || (!CrewSim.bShipEdit && condAmount2 == 0.0))
			{
				double num = MathUtils.Rand(0.0, condAmount * (double)percentage, MathUtils.RandType.Flat, null);
				double num2 = (condAmount - condAmount2) / condAmount;
				if (fRepairTarget > num2)
				{
					num = -MathUtils.Clamp(num, 0.0, (fRepairTarget - num2) * condAmount);
				}
				else
				{
					num = MathUtils.Clamp(num, 0.0, (num2 - fRepairTarget) * condAmount);
				}
				if (num != 0.0)
				{
					this.AddCondAmount("StatDamage", num, 0.0, 0f);
					flag = true;
				}
			}
			if (flag)
			{
				if (this.Item != null)
				{
					this.Item.VisualizeOverlays(false);
				}
				if (this.Pwr != null)
				{
					this.Pwr.ResetCurrentToMaxPower();
				}
			}
		}
		this.UpdateStats();
	}

	private void UpdateStats()
	{
		if (this._statDamageMax == null || this._statDamage == null)
		{
			this.FetchDamageConds();
		}
		if (this._statDamageMax != null && this._statDamageMax.fCount > 0.0 && this._statDamage != null)
		{
			float damage = this.GetDamage();
			if (this._lastDamageUpdate != damage)
			{
				this._lastDamageUpdate = damage;
				this.mapInfo["Condition"] = ((double)(1f - this._lastDamageUpdate) * 100.0).ToString("#.00") + "%";
			}
		}
		else
		{
			this.mapInfo["Condition"] = "100%";
		}
	}

	private void FetchDamageConds()
	{
		this.mapConds.TryGetValue("StatDamageMax", out this._statDamageMax);
		this.mapConds.TryGetValue("StatDamage", out this._statDamage);
	}

	public float GetDamage()
	{
		if (this._statDamageMax == null || this._statDamage == null)
		{
			this.FetchDamageConds();
		}
		double num = (this._statDamage == null) ? 0.0 : this._statDamage.fCount;
		double num2 = (this._statDamageMax == null) ? 1.0 : this._statDamageMax.fCount;
		return (float)(num / num2);
	}

	public List<CondOwner> GetFollowers()
	{
		if (this.ship == null)
		{
			return new List<CondOwner>();
		}
		CondTrigger condTrigger = DataHandler.GetCondTrigger("TIsFollowCommand");
		return this.ship.GetCOs(condTrigger, false, true, false);
	}

	public void DebugReportCauseOfDeath()
	{
		if (!this.HasCond("IsDead"))
		{
			return;
		}
		List<string> list = new List<string>();
		foreach (Condition condition in this.mapConds.Values)
		{
			if (condition.bFatal)
			{
				list.Add(condition.strName);
				if (condition.strName == "DcBlood04")
				{
					double condAmount = this.GetCondAmount("StatBlood");
				}
			}
		}
		UnityEngine.Debug.Log(string.Concat(new string[]
		{
			this.strName,
			" dead on ship: ",
			(this.ship == null) ? "null ship" : this.ship.strRegID,
			": ",
			string.Join(",", list.ToArray())
		}));
		if (list.Count == 0)
		{
			foreach (JsonLogMessage jsonLogMessage in this.aMessages)
			{
				UnityEngine.Debug.Log(jsonLogMessage.strMessage);
			}
		}
	}

	public void DebugReportCondrules()
	{
		foreach (CondRule condRule in this.mapCondRules.Values)
		{
			UnityEngine.Debug.Log(condRule.strName + ": " + condRule.Modifier);
		}
	}

	public void DebugFixOldCondRules()
	{
		if (this.HasCond("IsDebugCondRuleFixed"))
		{
			return;
		}
		foreach (CondRule condRule in this.mapCondRules.Values)
		{
			List<CondRuleThresh> list = new List<CondRuleThresh>();
			foreach (CondRuleThresh condRuleThresh in condRule.aThresholds)
			{
				if (!string.IsNullOrEmpty(condRuleThresh.strLootNew))
				{
					Loot loot = DataHandler.GetLoot(condRuleThresh.strLootNew);
					if (!(loot.strName == "Blank"))
					{
						List<string> lootNames = loot.GetLootNames(null, true, null);
						foreach (string text in lootNames)
						{
							if (text.IndexOf("Dc") == 0)
							{
								if (this.mapConds.ContainsKey(text))
								{
									list.Add(condRuleThresh);
									break;
								}
							}
						}
					}
				}
			}
			if (list.Count > 1)
			{
				bool flag = this.bLogConds;
				this.bLogConds = false;
				string text2 = string.Empty;
				foreach (CondRuleThresh condRuleThresh2 in list)
				{
					if (!string.IsNullOrEmpty(condRuleThresh2.strLootNew))
					{
						Loot loot2 = DataHandler.GetLoot(condRuleThresh2.strLootNew);
						if (!(loot2.strName == "Blank"))
						{
							loot2.ApplyCondLoot(this, -1f, null, 0f);
							if (text2.Length > 0)
							{
								text2 += ", ";
							}
							text2 += loot2.strName;
						}
					}
				}
				CondRuleThresh currentThresh = condRule.GetCurrentThresh(this);
				Loot loot3 = DataHandler.GetLoot(currentThresh.strLootNew);
				if (loot3.strName != "Blank")
				{
					loot3.ApplyCondLoot(this, -1f, null, 0f);
				}
				text2 = text2 + ". Restoring " + loot3.strName;
				this.bLogConds = flag;
				UnityEngine.Debug.Log("Duplicate DCs found on " + this.strName + ". Removing " + text2);
			}
		}
		this.AddCondAmount("IsDebugCondRuleFixed", 1.0, 0.0, 0f);
	}

	public bool HasSubCOs
	{
		get
		{
			return this._hasSubCOs;
		}
		set
		{
			this._hasSubCOs = value;
		}
	}

	private CondTrigger ctRestoreItem
	{
		get
		{
			if (CondOwner._ctRestoreItem == null)
			{
				CondOwner._ctRestoreItem = DataHandler.GetCondTrigger("TIsAIRestoreItem");
			}
			return CondOwner._ctRestoreItem;
		}
	}

	private CondTrigger ctSuffocatingManWalk
	{
		get
		{
			if (CondOwner._ctSuffocatingManWalk == null)
			{
				CondOwner._ctSuffocatingManWalk = DataHandler.GetCondTrigger("TIsSuffocatingManWalkEmerg");
			}
			return CondOwner._ctSuffocatingManWalk;
		}
	}

	public void Rename(string newName)
	{
		if (this.IsHumanOrRobot)
		{
			UnityEngine.Debug.LogWarning("Trying to rename human or robot which is forbidden. Aborting.");
			return;
		}
		if (newName != null && newName != string.Empty)
		{
			Dictionary<string, string> dictionary = new Dictionary<string, string>();
			dictionary.Add("strName", newName);
			if (this.mapGUIPropMaps.ContainsKey("Rename"))
			{
				this.mapGUIPropMaps.Remove("Rename");
			}
			this.mapGUIPropMaps.Add("Rename", dictionary);
		}
		else
		{
			this.mapGUIPropMaps.Remove("Rename");
		}
		this._Rename(newName);
		CrewSim.OnRightClick.Invoke(new List<CondOwner>
		{
			this
		});
	}

	public void CheckForRename()
	{
		Dictionary<string, string> dictionary;
		if (!this.mapGUIPropMaps.TryGetValue("Rename", out dictionary))
		{
			return;
		}
		this._Rename(dictionary["strName"]);
	}

	private void _Rename(string newName)
	{
		if (newName != null && newName != string.Empty)
		{
			this.strNameFriendly = newName;
			this.strNameShort = newName;
		}
		else
		{
			this.strNameFriendly = DataHandler.GetCOFriendlyName(this.strName);
			this.strNameShort = DataHandler.GetCOShortName(this.strName);
		}
	}

	public static OnUpdateWaitingRepliesEvent UpdateWaitingReplies = new OnUpdateWaitingRepliesEvent();

	[SerializeField]
	private string _strID;

	public Transform tf;

	public string strName;

	public string strNameFriendly;

	public string strNameShort;

	public string strNameShortLCase;

	public string strDesc;

	public string strCODef;

	public string strItemDef;

	private Item _itemComponentReference;

	private bool bTriedGettingItemRef;

	private Pathfinder _pfComponentReference;

	private bool bTriedGettingPfRef;

	private Crew _crewComponentReference;

	private bool bTriedGettingCrewRef;

	private GasContainer _gasContainerComponentReference;

	public string strPortraitImg;

	public Dictionary<string, string> mapInfo;

	public Vector3 vTextOffset;

	public Vector3 vBblOffset;

	private Ship _objShip;

	private List<string> aMyShips;

	private List<string> aFactions;

	public CondOwner objCOParent;

	public Condition objCondID;

	public PersonSpec pspec;

	public string strPersistentCT;

	public string strPersistentCO;

	public string strPlaceholderInstallReq;

	public string strPlaceholderInstallFinish;

	public string strSourceCO;

	public string strSourceInteract;

	public string strIdleAnim;

	public string strWalkAnim;

	public Container objContainer;

	public PairXY pairInventoryXY;

	public Dictionary<string, Condition> mapConds;

	public Dictionary<string, CondHistory> mapIAHist;

	public Dictionary<string, Vector2> mapPoints;

	public List<string> aInteractions;

	private bool bCanPatch;

	private bool bCanRepair;

	private bool bCanUndamage;

	public Dictionary<string, Dictionary<string, string>> mapGUIPropMaps;

	public Dictionary<string, IGUIHarness> mapGUIRefs;

	public Dictionary<string, JsonSlotEffects> mapSlotEffects;

	private Dictionary<string, JsonChargeProfile> mapChargeProfiles;

	private Dictionary<string, CondRule> mapCondRules;

	public Dictionary<string, string> mapAltItemDefs;

	public Dictionary<string, string> mapAltSlotImgs;

	public Dictionary<int, List<Pledge2>> dictPledges;

	public Dictionary<string, Vector3> dictSlotsLayout;

	public List<JsonLogMessage> aMessages;

	public List<string> aCondZeroes;

	public CondOwner coStackHead;

	public TrackingCollection<CondOwner> aStack;

	public TrackingCollection<CondOwner> aLot;

	private Slots _compSlots;

	public TMP_Text txtStack;

	public JsonCondOwnerSave jCOS;

	public bool bAlive;

	public bool bIgnoreKill;

	public bool bSlotLocked;

	public Slot slotNow;

	private JsonCompany objCompany;

	public int nStackLimit = 1;

	private int nRecentlyTriedMax = 6;

	public JsonShift jsShiftLast;

	private MeshRenderer mr;

	private BoxCollider mc;

	public bool bFreezeConds;

	public bool bFreezeCondRules;

	public FaceAnim2 faceRef;

	private List<JsonTicker> aTickers;

	private List<IManUpdater> aMUs;

	private List<IManUpdater> aMUDels;

	private bool bSaveMessageLog;

	public bool bLogConds = true;

	public Room currentRoom;

	public bool bDestroyed;

	private string strLastSocial;

	private double fLastICOUpdate;

	private double fLastCleanup;

	private float fCondTick = 1f;

	public List<Interaction> aQueue;

	public List<ReplyThread> aReplies;

	public List<string> aAttackIAs;

	private List<Priority> aPriorities;

	private HashSet<string> hashCondsImportant;

	private Dictionary<string, double> dictRecentlyTried;

	public List<string> aRememberIAs;

	public Dictionary<string, double> dictRememberScores;

	private float fRememberDecay = 0.5f;

	private GameObject goBracket;

	private Animator anim;

	private Powered pwr;

	private Wound wound;

	private Electrical elec;

	public global::Social socUs;

	private List<IManUpdater> aManUpdates;

	private List<Condition> aCondsTimed;

	private List<Condition> aCondsTemp;

	private List<AutoTask> aATsRepair;

	private List<AutoTask> aATsRestore;

	private List<AutoTask> aATsPatch;

	private static int nAnimStateID = -1;

	private static Loot _FreeWillLoot;

	private static Loot _FreeWillPenalty;

	public const string STR_US = "[us]";

	public const string STR_US_SPACE = "[us] ";

	public const string STR_THEM = "[them]";

	public const string STR_3RD = "[3rd]";

	private static double[] aDamageThresholds = new double[]
	{
		0.95,
		0.66,
		0.33
	};

	public List<string> aDestructableConds;

	private static CondTrigger _ctCanAIOrder;

	private static CondTrigger _ctCanWalk;

	private static CondTrigger _ctIsProneAwake;

	private static CondTrigger _ctRestoreItem;

	private static CondTrigger _ctSuffocatingManWalk;

	private static JsonPersonSpec _jpsRelReportFactionNeg;

	public bool debugStop;

	public const bool bDebugValidate = false;

	public const bool bDebugAIRandomChoices = false;

	private static string[] aAIRandomAvoid = new string[]
	{
		"DropItem",
		"DropItemStack",
		"PickupItem",
		"PickupItemStack"
	};

	public double fMSRedamageAmount;

	public Dictionary<string, string[]> dictAddCondEvents = new Dictionary<string, string[]>();

	public Dictionary<string, string[]> dictRemoveCondEvents = new Dictionary<string, string[]>();

	private ProgressBar progressBar;

	private Condition _statDamageMax;

	private Condition _statDamage;

	private float _lastDamageUpdate;

	private static GameObject selectionBracket;

	public static int nEndTurnsThisFrame = 0;

	public COWorkHistoryDTO RecentWorkHistory;

	private Dictionary<string, int> temp_counterDict = new Dictionary<string, int>();

	private List<JsonTicker> temp_aTickersAside = new List<JsonTicker>();

	private JsonTicker temp_jt;

	private CondOwnerVisitorAddToHashSet temp_vHash = new CondOwnerVisitorAddToHashSet();

	private CondOwnerVisitorEarlyOut temp_vHash1 = new CondOwnerVisitorEarlyOut();

	private CondOwnerVisitor temp_vWrap;

	private bool _fieldsInitialized;

	[NonSerialized]
	public bool GasChanged;

	private Dictionary<string, KeyValuePair<string, Tuple<double, double>>> alreadyParsed = new Dictionary<string, KeyValuePair<string, Tuple<double, double>>>();

	public Action<Interaction> OnQueueInteraction;

	private static bool _hasSeenRosterTutorial;

	private StringBuilder messageLogSB;

	private static List<Slot> _emptySlotsResult;

	private bool? _isRobot;

	private bool? _isHumanOrRobot;

	private bool _hasSubCOs;
}
