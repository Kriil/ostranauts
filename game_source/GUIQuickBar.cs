using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Ostranauts.Core;
using Ostranauts.Events;
using Ostranauts.UI.CrewBar.QuickBar;
using Ostranauts.UI.MegaToolTip;
using Ostranauts.UI.Quickbar.Models;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Context-sensitive quick action bar.
// This UI tracks a current CondOwner target and surfaces a small filtered set
// of immediate actions, with drag, pin, and hotkey support.
public class GUIQuickBar : MonoSingleton<GUIQuickBar>, IBeginDragHandler, IDragHandler, IEndDragHandler, IEventSystemHandler
{
	// Treated as dragging for a short grace window so click/drag transitions feel stable.
	public static bool IsBeingDragged
	{
		get
		{
			return GUIQuickBar._isBeingDragged || GUIQuickBar.nFramesSinceDragged < 2;
		}
		private set
		{
			GUIQuickBar._isBeingDragged = value;
			GUIQuickBar.nFramesSinceDragged = 0;
		}
	}

	// Current action target.
	// Setting this clears and rebuilds the button pool based on the target's interactions.
	public CondOwner COTarget
	{
		get
		{
			return this._coTarget;
		}
		set
		{
			if ((GUIInventory.instance != null && GUIInventory.instance.Selected != null) || (value != null && (value.HasCond("IsRoom") || value.HasCond("IsBodyPart") || value.HasCond("IsWound"))))
			{
				this._coTarget = null;
				this._txtTitle.text = string.Empty;
			}
			else
			{
				this._coTarget = value;
			}
			if (this._coTarget != null)
			{
				if (this._coId != this._coTarget.strID)
				{
					this._txtTitle.text = this._coTarget.ShortName;
				}
				this._coId = this._coTarget.strID;
			}
			else
			{
				this._coId = null;
			}
			foreach (GUIQuickActionButton guiquickActionButton in this.aButtons)
			{
				this.ReturnToPool(guiquickActionButton.gameObject);
			}
			this.aButtons.Clear();
			this.Refresh(false);
		}
	}

	// Caches filter buttons and initializes the hidden bar.
	private new void Awake()
	{
		base.Awake();
		CanvasManager.HideCanvasGroup(this._cg);
		this.aButtons = new List<GUIQuickActionButton>();
		this._ctJoinFightTarget = DataHandler.GetCondTrigger("TIsValidMeleeTarget");
		IEnumerator enumerator = this._tfFilterButtonParent.GetEnumerator();
		try
		{
			while (enumerator.MoveNext())
			{
				object obj = enumerator.Current;
				Transform transform = (Transform)obj;
				GUIQuickFilterButton component = transform.GetComponent<GUIQuickFilterButton>();
				if (!(component == null))
				{
					this._filterButtons[component.Filter] = component;
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

	// Wires target-selection events, slot updates, and the bar controls.
	private void Start()
	{
		TooltipPreviewButton.OnPreviewButtonClicked.AddListener(delegate(CondOwner selectedCO)
		{
			this.COTarget = selectedCO;
		});
		if (Slots.OnSlotContentUpdated == null)
		{
			Slots.OnSlotContentUpdated = new SlotUpdatedEvent();
		}
		Slots.OnSlotContentUpdated.AddListener(new UnityAction<CondOwner, CondOwner>(this.OnSlotsUpdated));
		this._resetButton.onClick.AddListener(delegate()
		{
			this.Refresh(false);
		});
		this._pinButton.onClick.AddListener(delegate()
		{
			this.OnPinButtonDown(false);
		});
		this._closeButton.onClick.AddListener(new UnityAction(this.OnCloseButtonDown));
		this._expandButton.onClick.AddListener(delegate()
		{
			this.ExpandCollapse(false);
		});
		this.Setup();
	}

	// Handles quickbar hotkeys and keeps the bar positioned against the active target.
	private void Update()
	{
		if (GUIQuickBar._isBeingDragged)
		{
			return;
		}
		if (GUIQuickBar.nFramesSinceDragged < 3)
		{
			GUIQuickBar.nFramesSinceDragged++;
		}
		if (CrewSim.objInstance == null || !CrewSim.objInstance.FinishedLoading || CrewSim.Typing)
		{
			return;
		}
		if (GUIActionKeySelector.commandQuickAction1.Down && this.aButtons.Count >= this.nHotkeyIndex + 1)
		{
			this.OnButtonClicked(this.aButtons[this.nHotkeyIndex]);
		}
		if (GUIActionKeySelector.commandQuickAction2.Down && this.aButtons.Count >= this.nHotkeyIndex + 2)
		{
			this.OnButtonClicked(this.aButtons[this.nHotkeyIndex + 1]);
		}
		if (GUIActionKeySelector.commandQuickAction3.Down && this.aButtons.Count >= this.nHotkeyIndex + 3)
		{
			this.OnButtonClicked(this.aButtons[this.nHotkeyIndex + 2]);
		}
		if (GUIActionKeySelector.commandQuickAction4.Down && this.aButtons.Count >= this.nHotkeyIndex + 4)
		{
			this.OnButtonClicked(this.aButtons[this.nHotkeyIndex + 3]);
		}
		if (GUIActionKeySelector.commandQuickActionReset.Down)
		{
			this.Refresh(false);
		}
		if (GUIActionKeySelector.commandQuickActionExpand.Down)
		{
			this.ExpandCollapse(false);
		}
		if (GUIActionKeySelector.CommandQuickActionPin.Down)
		{
			this.OnPinButtonDown(false);
		}
		this.RefreshCOReference();
		this.UpdatePosition();
	}

	private void OnDestroy()
	{
		Slots.OnSlotContentUpdated.RemoveListener(new UnityAction<CondOwner, CondOwner>(this.OnSlotsUpdated));
		TooltipPreviewButton.OnPreviewButtonClicked.RemoveListener(delegate(CondOwner selectedCO)
		{
			this.COTarget = selectedCO;
		});
	}

	private void Setup()
	{
		this.dictColors = new Dictionary<string, Color>();
		this.dictColors["Fight"] = DataHandler.GetColor("QuickActionFight");
		this.dictColors["Talk"] = DataHandler.GetColor("QuickActionTalk");
		this.dictColors["Use"] = DataHandler.GetColor("QuickActionUse");
		this.dictColors["Work"] = DataHandler.GetColor("QuickActionWork");
		this.dictColors["Disabled"] = DataHandler.GetColor("QuickActionDisabled");
		this.dictColors["Gambit"] = DataHandler.GetColor("QuickActionGambit");
		this.dictSprites = new Dictionary<string, Sprite>();
		this.dictSprites["Fight"] = this.GetImage("IcoFight.png");
		this.dictSprites["Talk"] = this.GetImage("IcoChat.png");
		this.dictSprites["Use"] = this.GetImage("IcoUse.png");
		this.dictSprites["Work"] = this.GetImage("IcoHammer.png");
		this.dictSprites["Disabled"] = this.GetImage("blank.png");
		foreach (KeyValuePair<string, GUIQuickFilterButton> keyValuePair in this._filterButtons)
		{
			keyValuePair.Value.SetData(this.dictSprites, this.dictColors, delegate(string filterType)
			{
				this._activeFilter = filterType;
				this.Refresh(false);
			});
		}
		this.aHotkeyCommands = new List<Command>
		{
			GUIActionKeySelector.commandQuickAction1,
			GUIActionKeySelector.commandQuickAction2,
			GUIActionKeySelector.commandQuickAction3,
			GUIActionKeySelector.commandQuickAction4
		};
		this.CreateButtonPool();
	}

	private float GetMinSize()
	{
		int count = this.aButtons.Count;
		if (count > 4)
		{
			return 72f;
		}
		return (float)(count * 16 + 2);
	}

	private void UpdatePosition()
	{
		if (this._coTarget == null)
		{
			CanvasManager.HideCanvasGroup(this._cg);
			return;
		}
		if (GUIInventory.instance.Selected != null && GUIInventory.instance.Selected.CO.strID == this._coTarget.strID)
		{
			CanvasManager.HideCanvasGroup(this._cg);
			return;
		}
		CanvasManager.ShowCanvasGroup(this._cg);
		if (this._pinPosition != Vector3.zero)
		{
			(base.transform as RectTransform).localPosition = this._pinPosition;
			return;
		}
		Vector3 localPosition = Vector3.zero;
		bool flag = false;
		if (GUIInventory.instance.IsOpen)
		{
			foreach (GUIInventoryWindow guiinventoryWindow in GUIInventory.instance.activeWindows)
			{
				if (guiinventoryWindow.COGO.ContainsKey(this._coTarget.strID) && guiinventoryWindow.COGO[this._coTarget.strID].transform.parent != null && guiinventoryWindow.COGO[this._coTarget.strID].transform.parent.parent != null && guiinventoryWindow.COGO[this._coTarget.strID].transform.parent.parent.parent != null && guiinventoryWindow.COGO[this._coTarget.strID].transform.parent.parent.parent == GUIInventory.instance.transform)
				{
					flag = true;
					localPosition = guiinventoryWindow.COGO[this._coTarget.strID].transform.parent.parent.parent.InverseTransformPoint(guiinventoryWindow.COGO[this._coTarget.strID].transform.position);
					float num = this._panelObject.rect.width / 2f;
					float num2 = 3f + this._panelObject.rect.height;
					localPosition = new Vector3(localPosition.x + num, localPosition.y - num2, localPosition.z);
				}
			}
			if (GUIInventory.instance.PaperDollManager.mapCOIDsToGO.ContainsKey(this._coTarget.strID) && GUIInventory.instance.PaperDollManager.mapCOIDsToGO[this._coTarget.strID] != null)
			{
				Transform transform = GUIInventory.instance.PaperDollManager.mapCOIDsToGO[this._coTarget.strID].transform;
				localPosition = GUIInventory.instance.transform.InverseTransformPoint(transform.position);
				flag = true;
				float num3 = this._panelObject.rect.width / 2f;
				float num4 = 3f + this._panelObject.rect.height;
				localPosition = new Vector3(localPosition.x + num3, localPosition.y - num4, localPosition.z);
			}
		}
		if (!flag)
		{
			float num5 = this._itemOffsetX;
			float num6 = this._itemOffsetY;
			if (this._coTarget.Item != null)
			{
				Item item = this._coTarget.Item;
				num5 = (float)item.nWidthInTiles / 2f + this._itemOffsetX;
				num6 = -((float)item.nHeightInTiles / 2f + this._itemOffsetY);
			}
			if (num5 < this._itemOffsetX)
			{
				num5 = this._itemOffsetX;
			}
			if (num6 > this._itemOffsetY)
			{
				num6 = this._itemOffsetY;
			}
			localPosition = CrewSim.objInstance.camMain.WorldToScreenPoint(this._coTarget.transform.position + new Vector3(num5, num6, 0f));
			float num7 = 1280f / (float)Screen.width * CrewSim.objInstance.AspectRatioMod();
			float num8 = 720f / (float)Screen.height;
			float num9 = (localPosition.x - (float)Screen.width / 2f) * num7;
			float num10 = (localPosition.y - (float)Screen.height / 2f) * num8;
			float z = 25600f / (float)Screen.width;
			float num11 = this._panelObject.rect.width / 2f;
			float num12 = this._panelObject.rect.height / 2f;
			if (num9 + num11 * 2f > (float)Screen.width * num7 * this._boundsXR)
			{
				num9 = (float)Screen.width * num7 * this._boundsXR - num11 * 2f;
			}
			else if (num9 - num11 * 2f < (float)Screen.width * num7 * this._boundsXL)
			{
				num9 = (float)Screen.width * num7 * this._boundsXL + num11 * 2f;
			}
			if (num10 + num12 * 2f > (float)Screen.height * num8 * this._boundsYT)
			{
				num10 = (float)Screen.height * num8 * this._boundsYT - num12 * 2f;
			}
			else if (num10 - num12 * 2f < (float)Screen.height * num8 * this._boundsYB)
			{
				num10 = (float)Screen.height * num8 * this._boundsYB + num12 * 2f;
			}
			localPosition = new Vector3(num9 + num11, num10, z);
		}
		(base.transform as RectTransform).localPosition = localPosition;
	}

	private void RefreshCOReference()
	{
		if (this._coTarget != null || string.IsNullOrEmpty(this._coId))
		{
			return;
		}
		if (!DataHandler.mapCOs.TryGetValue(this._coId, out this._coTarget))
		{
			this._coId = null;
		}
		if (this.aButtons != null && this.aButtons.Count > 0)
		{
			this.COTarget = this._coTarget;
		}
	}

	private void OnCloseButtonDown()
	{
		this.COTarget = null;
	}

	private void OnPinButtonDown(bool forceOn = false)
	{
		if (this._pinPosition == Vector3.zero || forceOn)
		{
			this._pinPosition = (base.transform as RectTransform).localPosition;
			this._pinImageTf.localScale = new Vector3(1f, -1f, 1f);
		}
		else
		{
			this._pinPosition = Vector3.zero;
			this._pinImageTf.localScale = new Vector3(1f, 1f, 1f);
		}
	}

	private void OnSlotsUpdated(CondOwner coSlot, CondOwner coItem)
	{
		if (coSlot == null || this._coTarget == null)
		{
			return;
		}
		CondOwner x = coSlot.RootParent(null);
		if ((x != null && (x == this._coTarget || x == CrewSim.GetSelectedCrew())) || coSlot == this._coTarget)
		{
			this.BuildButtonList(false);
		}
	}

	private void OnButtonClicked(GUIQuickActionButton qab)
	{
		if (!qab.Clickable)
		{
			return;
		}
		if (GUIQuickBar.OnQABButtonClicked != null)
		{
			GUIQuickBar.OnQABButtonClicked(qab);
		}
		if (CrewSim.objInstance.tooltip.window == GUITooltip.TooltipWindow.QAB)
		{
			CrewSim.objInstance.tooltip.SetTooltipIA(null, GUITooltip.TooltipWindow.QAB);
		}
		RadialContextMenuObject.ProcessInteraction(qab);
		if (CrewSim.bUnpauseShield)
		{
			CrewSim.bUnpauseShield = false;
		}
		else
		{
			CrewSim.Paused = false;
		}
	}

	public void Refresh(bool ignoreInput = false)
	{
		this.nHotkeyIndex = 0;
		this.BuildButtonList(ignoreInput);
	}

	public void Reset()
	{
		this._pinPosition = Vector3.zero;
		this.OnCloseButtonDown();
	}

	private void ExpandCollapse(bool refreshSizeOnly = false)
	{
		if (!refreshSizeOnly)
		{
			this.bExpanded = !this.bExpanded;
		}
		if (this.bExpanded)
		{
			this._expandIcon.sprite = this._sprites[0];
			this._panelObject.sizeDelta = new Vector2(this._panelObject.sizeDelta.x, Mathf.Max(this.GetMinSize(), 2f + 16f * (float)this.aButtons.Count));
		}
		else
		{
			this._expandIcon.sprite = this._sprites[1];
			this._panelObject.sizeDelta = new Vector2(this._panelObject.sizeDelta.x, this.GetMinSize());
		}
	}

	private void RemoveUnusedButtons(List<AvailableActionDTO> availableIas)
	{
		for (int i = this.aButtons.Count - 1; i >= 0; i--)
		{
			bool flag = false;
			int num = 0;
			while (num < availableIas.Count && !flag)
			{
				if (availableIas[num].Ia.strName == this.aButtons[i].strIANameLast)
				{
					this.aButtons[i].IA = availableIas[num].Ia;
					flag = true;
				}
				num++;
			}
			if (!flag)
			{
				this.aButtons[i].IA = null;
				this.ReturnToPool(this.aButtons[i].gameObject);
				this.aButtons.RemoveAt(i);
			}
		}
	}

	public void BuildButtonList(bool ignoreInput = false)
	{
		if (!ignoreInput && Input.GetMouseButton(0))
		{
			return;
		}
		CrewSim.objInstance.tooltip.SetTooltipIA(null, GUITooltip.TooltipWindow.QAB);
		CanvasManager.HideCanvasGroup(this._cg);
		CondOwner selectedCrew = CrewSim.GetSelectedCrew();
		if (this.COTarget == null || selectedCrew == null)
		{
			return;
		}
		List<AvailableActionDTO> list = CrewSim.GetAvailActionsForCO(selectedCrew, this._coTarget);
		this.AssignOldOrder(list);
		this.RemoveAllButtons();
		List<AvailableActionDTO> list2 = (from y in list
		where y.Ia != null && !y.IsFight
		select y into x
		orderby x.Ia.strActionGroup
		select x).ThenByDescending((AvailableActionDTO z) => z.IAOrderPriority).ThenBy((AvailableActionDTO y) => y.IndexPosition).ToList<AvailableActionDTO>();
		List<AvailableActionDTO> list3 = (from x in list
		where x.Ia != null && x.IsFight
		select x).ToList<AvailableActionDTO>();
		this.AddCrewOrders(selectedCrew, list3);
		if (selectedCrew.HasCond("IsInCombat"))
		{
			list3.AddRange(list2);
			list = list3;
		}
		else
		{
			list2.AddRange(list3);
			list = list2;
		}
		int num = 0;
		for (int i = list.Count - 1; i >= num; i--)
		{
			if (list[i].IsClickable)
			{
				AvailableActionDTO item = list[i];
				list.Remove(item);
				list.Insert(0, item);
				i++;
				num++;
			}
		}
		foreach (KeyValuePair<string, GUIQuickFilterButton> keyValuePair in this._filterButtons)
		{
			keyValuePair.Value.ResetCount();
		}
		foreach (AvailableActionDTO availableActionDTO in list)
		{
			if (availableActionDTO.Ia != null)
			{
				if (!string.IsNullOrEmpty(availableActionDTO.Ia.strActionGroup) && this._filterButtons.ContainsKey(availableActionDTO.Ia.strActionGroup))
				{
					this._filterButtons[availableActionDTO.Ia.strActionGroup].IncreaseCount();
				}
				if (this._activeFilter == "All" || availableActionDTO.Ia.strActionGroup == this._activeFilter)
				{
					this.CreateAction(availableActionDTO, selectedCrew);
				}
			}
		}
		if (this._activeFilter != "All" && this.aButtons.Count == 0)
		{
			this._activeFilter = "All";
			this.Refresh(ignoreInput);
			return;
		}
		this.UpdateFilterButtons();
		int num2 = 0;
		int num3 = 0;
		foreach (GUIQuickActionButton guiquickActionButton in this.aButtons)
		{
			if (num3 >= this.nHotkeyIndex && num2 < this.aHotkeyCommands.Count)
			{
				guiquickActionButton.HotKey = this.aHotkeyCommands[num2].KeyName.Substring(this.aHotkeyCommands[num2].KeyName.Length - 1);
				num2++;
			}
			else
			{
				guiquickActionButton.HotKey = null;
			}
			num3++;
		}
		if (list.Count > 0)
		{
			CanvasManager.ShowCanvasGroup(this._cg);
		}
		else
		{
			CanvasManager.HideCanvasGroup(this._cg);
		}
		base.StartCoroutine(this.RefreshContentWindow());
		this.ExpandCollapse(true);
	}

	private void UpdateFilterButtons()
	{
		int num = 0;
		foreach (KeyValuePair<string, GUIQuickFilterButton> keyValuePair in this._filterButtons)
		{
			if (keyValuePair.Value.UpdateElement())
			{
				num++;
			}
		}
		if (num > 1)
		{
			this._filterButtons["All"].ForceOn();
			num++;
		}
		this._sidePanelObject.sizeDelta = new Vector2(this._sidePanelObject.sizeDelta.x, 3f + 13f * (float)num);
	}

	private IEnumerator RefreshContentWindow()
	{
		yield return null;
		RectTransform contentRect = this._tfContent.GetComponent<RectTransform>();
		contentRect.anchoredPosition = new Vector3(contentRect.anchoredPosition.x, 0f);
		yield break;
	}

	private void RemoveAllButtons()
	{
		foreach (GUIQuickActionButton guiquickActionButton in this.aButtons)
		{
			this.ReturnToPool(guiquickActionButton.gameObject);
		}
		this.aButtons.Clear();
	}

	private void AssignOldOrder(List<AvailableActionDTO> aActions)
	{
		if (this.aButtons == null || aActions == null)
		{
			return;
		}
		for (int i = 0; i < this.aButtons.Count; i++)
		{
			if (!(this.aButtons[i] == null) && this.aButtons[i].IA != null)
			{
				foreach (AvailableActionDTO availableActionDTO in aActions)
				{
					if (availableActionDTO.Matches(this.aButtons[i].strIANameLast))
					{
						availableActionDTO.IndexPosition = i;
						break;
					}
				}
			}
		}
	}

	private void AddCrewOrders(CondOwner coUs, List<AvailableActionDTO> attackActions)
	{
		if (attackActions == null || this.COTarget == null || (this.COTarget != null && this.COTarget.HasCond("IsPlayerCrew")) || !this._ctJoinFightTarget.Triggered(this.COTarget, null, true) || CrewSim.coPlayer.Company == null)
		{
			return;
		}
		List<CondOwner> crewMembers = CrewSim.coPlayer.Company.GetCrewMembers(coUs);
		foreach (CondOwner condOwner in crewMembers)
		{
			Interaction interaction = DataHandler.GetInteraction("ACTJoinFightCommand", null, false);
			if (interaction != null)
			{
				interaction.objUs = coUs;
				interaction.objThem = condOwner;
				interaction.obj3rd = this.COTarget;
				Interaction interaction2 = interaction;
				interaction2.strTitle = interaction2.strTitle + " " + condOwner;
				attackActions.Add(new CustomActionDTO(interaction));
			}
		}
	}

	private GUIQuickActionButton AddAction(AvailableActionDTO actionDTO, CondOwner coUs)
	{
		if (actionDTO == null)
		{
			return null;
		}
		Interaction ia = actionDTO.Ia;
		foreach (GUIQuickActionButton guiquickActionButton in this.aButtons)
		{
			if (guiquickActionButton.strIANameLast == ia.strName)
			{
				ia.objUs = coUs;
				ia.objThem = this._coTarget;
				guiquickActionButton.IA = ia;
				guiquickActionButton.SetIsReply(actionDTO.IsReply);
				guiquickActionButton.Clickable = actionDTO.IsClickable;
				return guiquickActionButton;
			}
		}
		return this.CreateAction(actionDTO, coUs);
	}

	private GUIQuickActionButton CreateAction(AvailableActionDTO availableActionDto, CondOwner coUs)
	{
		GUIQuickActionButton guiquickActionButton = this.RequestButtonFromPool();
		Interaction ia = availableActionDto.Ia;
		guiquickActionButton.IA = ia;
		string key = "Disabled";
		if (ia.strActionGroup != null)
		{
			key = ia.strActionGroup;
		}
		Color color = this.dictColors["Disabled"];
		if (availableActionDto.IsGambit)
		{
			this.dictColors.TryGetValue("Gambit", out color);
		}
		else if (this.dictColors.ContainsKey(key))
		{
			this.dictColors.TryGetValue(key, out color);
		}
		guiquickActionButton.Color = color;
		Sprite icon = this.dictSprites["Disabled"];
		if (this.dictSprites.ContainsKey(key))
		{
			this.dictSprites.TryGetValue(key, out icon);
		}
		guiquickActionButton.Icon = icon;
		if (ia.nMoveType == Interaction.MoveType.GAMBIT)
		{
			guiquickActionButton.Text = DataHandler.GetString("QAB_IA_TITLE_GAMBIT", false) + ia.strTitle;
		}
		else if (ia.nMoveType == Interaction.MoveType.SOCIAL_CORE)
		{
			guiquickActionButton.Text = DataHandler.GetString("QAB_IA_TITLE_CORE", false) + ia.strTitle;
		}
		else if (availableActionDto.IsGig)
		{
			guiquickActionButton.Text = DataHandler.GetString("QAB_IA_TITLE_GIG", false) + ia.strTitle;
		}
		else if (ia.nMoveType == Interaction.MoveType.COMMAND)
		{
			guiquickActionButton.Text = DataHandler.GetString("QAB_IA_TITLE_COMMAND", false) + ia.strTitle;
		}
		else if (ia.nMoveType == Interaction.MoveType.STAKES)
		{
			guiquickActionButton.Text = DataHandler.GetString("QAB_IA_TITLE_STAKES", false) + ia.strTitle;
		}
		else if (ia.strActionGroup == "Talk")
		{
			guiquickActionButton.Text = DataHandler.GetString("QAB_IA_TITLE_SOCIAL", false) + ia.strTitle;
		}
		else
		{
			guiquickActionButton.Text = ia.strTitle;
		}
		guiquickActionButton.SetIsReply(availableActionDto.IsReply);
		guiquickActionButton.Clickable = availableActionDto.IsClickable;
		if (!(availableActionDto is CustomActionDTO))
		{
			ia.objUs = coUs;
			ia.objThem = this._coTarget;
		}
		guiquickActionButton.transform.SetAsLastSibling();
		this.aButtons.Add(guiquickActionButton);
		return guiquickActionButton;
	}

	private Sprite GetImage(string imagePath)
	{
		Texture2D texture2D = DataHandler.LoadPNG(imagePath, false, false);
		texture2D.filterMode = FilterMode.Bilinear;
		return Sprite.Create(texture2D, new Rect(0f, 0f, (float)texture2D.width, (float)texture2D.height), Vector2.zero);
	}

	private void CreateButtonPool()
	{
		for (int i = 0; i < 10; i++)
		{
			GameObject gameObject = this.InstantiateButton().gameObject;
			gameObject.SetActive(false);
			this._buttonPool.Add(gameObject);
		}
	}

	private GUIQuickActionButton InstantiateButton()
	{
		GUIQuickActionButton qab = UnityEngine.Object.Instantiate<GameObject>(this._selectionButtonPrefab, this._tfContent).GetComponent<GUIQuickActionButton>();
		Button component = qab.GetComponent<Button>();
		AudioManager.AddBtnAudio(component.gameObject, "ShipUIBtnPDAClick01", "ShipUIBtnPDAClick02");
		component.onClick.AddListener(delegate()
		{
			this.OnButtonClicked(qab);
		});
		return qab;
	}

	private GUIQuickActionButton RequestButtonFromPool()
	{
		GameObject gameObject = this._buttonPool.LastOrDefault<GameObject>();
		if (gameObject == null)
		{
			return this.InstantiateButton();
		}
		this._buttonPool.RemoveAt(this._buttonPool.Count - 1);
		gameObject.SetActive(true);
		return gameObject.GetComponent<GUIQuickActionButton>();
	}

	private void ReturnToPool(GameObject go)
	{
		go.SetActive(false);
		this._buttonPool.Add(go);
	}

	public void OnDrag(PointerEventData eventData)
	{
		this.SetDraggedPosition(eventData);
	}

	public void OnBeginDrag(PointerEventData eventData)
	{
		GUIQuickBar.IsBeingDragged = true;
		RectTransformUtility.ScreenPointToLocalPointInRectangle(base.transform as RectTransform, eventData.position, eventData.pressEventCamera, out this._dragOffset);
		this.OnPinButtonDown(true);
		this.SetDraggedPosition(eventData);
	}

	public void OnEndDrag(PointerEventData eventData)
	{
		GUIQuickBar.IsBeingDragged = false;
	}

	private void SetDraggedPosition(PointerEventData data)
	{
		Vector2 a;
		if (RectTransformUtility.ScreenPointToLocalPointInRectangle(this._parentCanvasRect, data.position, data.pressEventCamera, out a))
		{
			this._pinPosition = a - this._dragOffset;
			this.UpdatePosition();
		}
	}

	[SerializeField]
	private GameObject _selectionButtonPrefab;

	[SerializeField]
	private ScrollRect _sr;

	[SerializeField]
	private RectTransform _panelObject;

	[SerializeField]
	private RectTransform _sidePanelObject;

	[SerializeField]
	private Image _expandIcon;

	[SerializeField]
	private Sprite[] _sprites;

	[SerializeField]
	private TMP_Text _txtTitle;

	[SerializeField]
	private Button _resetButton;

	[SerializeField]
	private Button _pinButton;

	[SerializeField]
	private Transform _pinImageTf;

	[SerializeField]
	private Button _closeButton;

	[SerializeField]
	private Button _expandButton;

	[SerializeField]
	private CanvasGroup _cg;

	[SerializeField]
	private Transform _tfContent;

	[SerializeField]
	private Transform _tfFilterButtonParent;

	[SerializeField]
	private RectTransform _parentCanvasRect;

	[SerializeField]
	private float _boundsXR = 0.5f;

	[SerializeField]
	private float _boundsXL = -0.55f;

	[SerializeField]
	private float _boundsYT = 0.6f;

	[SerializeField]
	private float _boundsYB = -0.3f;

	private double fTimeLastUpdate;

	private CondOwner _coTarget;

	private string _coId;

	private CondTrigger _ctJoinFightTarget;

	private Dictionary<string, Color> dictColors;

	private Dictionary<string, Sprite> dictSprites;

	private List<GUIQuickActionButton> aButtons;

	private List<Command> aHotkeyCommands;

	private int nHotkeyIndex;

	private bool bExpanded;

	private string _activeFilter = "All";

	private Vector2 _dragOffset;

	private static bool _isBeingDragged;

	private static int nFramesSinceDragged = 2;

	private Vector3 _pinPosition;

	private readonly Dictionary<string, GUIQuickFilterButton> _filterButtons = new Dictionary<string, GUIQuickFilterButton>();

	private List<GameObject> _buttonPool = new List<GameObject>();

	private float _itemOffsetX = 1f;

	private float _itemOffsetY = -1f;

	public static UnityAction<GUIQuickActionButton> OnQABButtonClicked;
}
