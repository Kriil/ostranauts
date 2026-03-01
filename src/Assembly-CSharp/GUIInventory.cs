using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Ostranauts.Core;
using Ostranauts.Events;
using Ostranauts.GUIInventory;
using Ostranauts.Objectives;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

// Main inventory and paper-doll UI.
// This manages inventory windows, slot updates, container popups, and the
// character equipment display for the currently viewed CondOwner.
public class GUIInventory : MonoBehaviour
{
	// Loads shared prefabs, sets up the paper-doll panel, and creates the inventory tooltip.
	private void Awake()
	{
		if (this.inventoryGridPrefab == null)
		{
			this.inventoryGridPrefab = Resources.Load<GameObject>("prefabGUIInventoryGrid3");
		}
		if (GUIInventory.inventoryItemPrefab == null)
		{
			GUIInventory.inventoryItemPrefab = Resources.Load<GameObject>("prefabGUIInventoryItem");
		}
		this.PaperDollBorder = base.transform.Find("PaperDoll/Border").GetComponent<Image>();
		this.PaperDollBorderCG = this.PaperDollBorder.GetComponent<CanvasGroup>();
		this.PaperDollBackgroundCG = base.transform.Find("PaperDoll/Background").GetComponent<CanvasGroup>();
		this.PaperDollImageCG = base.transform.Find("PaperDoll").GetComponent<CanvasGroup>();
		this.PaperDollImageCG.alpha = 0f;
		this.PaperDollManager = this.PaperDollImageCG.GetComponent<GUIPaperDollManager>();
		this.rectPaperDoll = this.PaperDollImageCG.GetComponent<RectTransform>();
		this.tabText = base.transform.Find("PaperDoll/Tab Background/Tab Title Text").GetComponent<TMP_Text>();
		if (this.btnShowSocialMoves != null)
		{
			this.btnShowSocialMoves.onClick.AddListener(new UnityAction(this.ToggleSocialMovesWindowVisibility));
			this.btnShowSocialMoves.gameObject.SetActive(false);
		}
		this.canvasGroup = base.GetComponent<CanvasGroup>();
		CanvasManager.HideCanvasGroup(this.canvasGroup);
		this.rectInv = base.GetComponent<RectTransform>();
		this.rectDragBounds = base.transform.Find("DragBounds").GetComponent<RectTransform>();
		GameObject gameObject = Resources.Load("prefabTooltip") as GameObject;
		gameObject = UnityEngine.Object.Instantiate<GameObject>(gameObject, base.transform);
		this.tooltip = gameObject.GetComponent<GUITooltip>();
		this.tooltip.SetWindow(GUITooltip.TooltipWindow.Inventory);
		GUIInventory.instance = this;
	}

	// Subscribes to slot-content updates once the scene is live.
	private void Start()
	{
		if (Slots.OnSlotContentUpdated == null)
		{
			Slots.OnSlotContentUpdated = new SlotUpdatedEvent();
		}
		Slots.OnSlotContentUpdated.AddListener(new UnityAction<CondOwner, CondOwner>(this.OnSlotUpdated));
	}

	// Removes listeners and clears click timing state.
	private void OnDestroy()
	{
		if (Slots.OnSlotContentUpdated != null)
		{
			Slots.OnSlotContentUpdated.RemoveListener(new UnityAction<CondOwner, CondOwner>(this.OnSlotUpdated));
		}
		this.bJustClickedItem = false;
		this.nJustClickedItem = 3;
	}

	// True while the main inventory window/paper-doll is visible.
	public bool IsInventoryVisible
	{
		get
		{
			return this.PaperDollImageCG.alpha > 0f;
		}
	}

	// Restores a saved tab/window position when present, otherwise cascades from the previous window.
	private Vector2 GetWindowPosition(GUIInventoryWindow winCurrent, GUIInventoryWindow winPrev)
	{
		string gpminfo = winCurrent.CO.GetGPMInfo("GUIInv", "TabPos");
		if (!string.IsNullOrEmpty(gpminfo))
		{
			Vector3 v = default(Vector3);
			string[] array = gpminfo.Split(new char[]
			{
				'|'
			});
			if (array.Length == 2)
			{
				float num = 0f;
				if (float.TryParse(array[0], out num))
				{
					v.x = num;
				}
				if (float.TryParse(array[1], out num))
				{
					v.y = num;
				}
				return v;
			}
		}
		float num2 = 1f / (1.5f * CanvasManager.CanvasRatio);
		float num3;
		float num4;
		if (this.rectItmAnchor != null)
		{
			num3 = num2 * this.rectItmAnchor.localPosition.x;
			num4 = num2 * this.rectItmAnchor.localPosition.y;
		}
		else
		{
			num3 = num2 * (this.rectPaperDoll.localPosition.x + this.rectPaperDoll.rect.width / 2f + 5f);
			num4 = num2 * (this.rectPaperDoll.localPosition.y + this.rectPaperDoll.rect.height / 2f);
		}
		float num5 = num4;
		float num6 = num5;
		if (winPrev != null)
		{
			num3 = winPrev.transform.localPosition.x * num2;
			num6 = winPrev.transform.localPosition.y - winPrev.gridBorderRect.rect.height - winPrev.tabImage.rectTransform.rect.height * 1.7f;
			num6 *= num2;
			float num7 = winCurrent.gridBorderRect.rect.height + winCurrent.tabImage.rectTransform.rect.height * 1.7f;
			if (num6 - num7 < num5 - num4 * 2f)
			{
				num6 = num5;
				num3 += 100f;
			}
		}
		return new Vector2(num3, num6);
	}

	// Reacts to slot changes for the currently viewed paper-doll/root object.
	private void OnSlotUpdated(CondOwner coSlot, CondOwner coItem)
	{
		if (!this.IsInventoryVisible || this._coDoll == null || coSlot == null)
		{
			return;
		}
		CondOwner condOwner = coSlot.RootParent(null);
		if (coSlot.strName != this._coDoll.strName && (condOwner == null || condOwner.strName != this._coDoll.strName))
		{
			return;
		}
		if (GUIInventory.CTOpenInv.Triggered(coItem, null, true))
		{
			this.SpawnInventoryWindow(coItem, InventoryWindowType.Container, null, null);
		}
		this.PaperDollManager.SetHelmetMask();
	}

	// Searches visible inventory windows for the UI element bound to a specific CondOwner.
	public static GUIInventoryItem GetInventoryItemFromCO(CondOwner objCO)
	{
		foreach (GUIInventoryWindow guiinventoryWindow in GUIInventory.instance.activeWindows)
		{
			GUIInventoryItem inventoryItemFromCO = guiinventoryWindow.gridLayout.GetInventoryItemFromCO(objCO);
			if (inventoryItemFromCO != null)
			{
				return inventoryItemFromCO;
			}
		}
		return null;
	}

	// Opens one inventory/container window for a CondOwner.
	public GUIInventoryWindow SpawnInventoryWindow(CondOwner CO, InventoryWindowType type, GUIInventoryWindow winParent, Vector3? vPos = null)
	{
		if (CO == null)
		{
			return null;
		}
		CanvasManager.ShowCanvasGroup(this.canvasGroup);
		bool flag = type == InventoryWindowType.Ground || CO.objContainer != null;
		if (CO.dictSlotsLayout.Count > 0)
		{
			flag = true;
		}
		GUIInventoryWindow guiinventoryWindow = null;
		for (int i = 0; i < this.activeWindows.Count; i++)
		{
			if (!flag)
			{
				break;
			}
			if (this.activeWindows[i].CO == CO && type == this.activeWindows[i].type)
			{
				flag = false;
				guiinventoryWindow = this.activeWindows[i];
			}
		}
		float num = 1.5f * CanvasManager.CanvasRatio;
		if (flag)
		{
			GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(this.inventoryGridPrefab, base.transform);
			GUIInventoryWindow component = gameObject.GetComponent<GUIInventoryWindow>();
			this.activeWindows.Add(component);
			component.SetData(CO, type);
			guiinventoryWindow = component;
		}
		foreach (Slot slot in CO.GetSlots(true, Slots.SortOrder.HELD_FIRST))
		{
			if (slot.strName == "social")
			{
				CondOwner coSlotted = slot.aCOs.FirstOrDefault<CondOwner>();
				this.SpawnSocialMovesWindow(coSlotted);
			}
			else if (!slot.bHide)
			{
				foreach (CondOwner condOwner in slot.aCOs)
				{
					if (!(condOwner == null) && GUIInventory.CTOpenInv.Triggered(condOwner, null, true))
					{
						if (!(condOwner.objContainer == null) || (condOwner.dictSlotsLayout != null && condOwner.dictSlotsLayout.Count != 0))
						{
							if (CO.dictSlotsLayout != null && CO.dictSlotsLayout.ContainsKey(slot.strName))
							{
								GUIInventoryWindow guiinventoryWindow2 = this.SpawnInventoryWindow(condOwner, InventoryWindowType.Container, guiinventoryWindow, new Vector3?(CO.dictSlotsLayout[slot.strName]));
								guiinventoryWindow2.ToggleTab(false);
							}
							else
							{
								GUIInventoryWindow guiinventoryWindow2 = this.SpawnInventoryWindow(condOwner, InventoryWindowType.Container, guiinventoryWindow, null);
							}
						}
					}
				}
			}
		}
		if (flag)
		{
			if (vPos != null && winParent != null)
			{
				guiinventoryWindow.transform.localPosition = winParent.transform.localPosition + vPos.GetValueOrDefault() * 1.5f * CanvasManager.CanvasRatio;
				guiinventoryWindow.transform.SetParent(winParent.transform, true);
			}
			else
			{
				guiinventoryWindow.ResetBorder();
				int num2 = this.activeWindows.IndexOf(guiinventoryWindow);
				GUIInventoryWindow guiinventoryWindow3 = winParent;
				if (guiinventoryWindow3 == null && num2 > 0)
				{
					for (int k = num2 - 1; k >= 0; k--)
					{
						if (!this.activeWindows[k].IsChild && !this.activeWindows[k].bCustomPos)
						{
							guiinventoryWindow3 = this.activeWindows[k];
							break;
						}
					}
				}
				guiinventoryWindow.transform.localPosition = this.GetWindowPosition(guiinventoryWindow, guiinventoryWindow3) * 1.5f * CanvasManager.CanvasRatio;
			}
			CanvasManager.SetAnchorsToCorners(guiinventoryWindow.transform);
			if (winParent == null)
			{
				base.StartCoroutine(this.FlyIn(guiinventoryWindow));
			}
		}
		if (CrewSim.coPlayer.HasCond("TutorialLockerWaiting") && GUIInventory.instance.IsCOShown(CrewSim.coPlayer) && (CO.HasCond("TutorialLockerTarget") || (CO.HasCond("IsStorageFurniture") && CrewSim.coPlayer.HasCond("IsENCFirstDockOKLG"))))
		{
			CrewSim.coPlayer.ZeroCondAmount("TutorialLockerWaiting");
			MonoSingleton<ObjectiveTracker>.Instance.CheckObjective(CrewSim.coPlayer.strID);
		}
		if (GUIInventory.OnOpenInventory != null && guiinventoryWindow != null)
		{
			GUIInventory.OnOpenInventory.Invoke(guiinventoryWindow);
		}
		return guiinventoryWindow;
	}

	private IEnumerator FlyIn(GUIInventoryWindow win)
	{
		yield return null;
		if (win == null)
		{
			yield break;
		}
		Vector3 _destination = win.transform.localPosition;
		float startTime = Time.time;
		Vector3 origin = _destination + new Vector3(500f, 0f, 0f);
		win.transform.localPosition = origin;
		while (win != null && win.transform.localPosition != _destination)
		{
			float step = (Time.time - startTime) / 0.25f / Time.timeScale;
			win.transform.localPosition = Vector3.Lerp(origin, _destination, Mathf.SmoothStep(0f, 1f, step));
			if (Vector3.Distance(_destination, win.transform.localPosition) < 0.5f)
			{
				win.transform.localPosition = _destination;
				break;
			}
			yield return null;
			if (win == null)
			{
				yield break;
			}
		}
		if (win != null && win.CO != null)
		{
			AudioEmitter component = win.CO.GetComponent<AudioEmitter>();
			if (component != null)
			{
				component.StartPickup();
			}
		}
		yield break;
	}

	private void ToggleSocialMovesWindowVisibility()
	{
		if (this._socialMovesWindow != null)
		{
			this._socialMovesWindow.gameObject.SetActive(!this._socialMovesWindow.gameObject.activeSelf);
		}
	}

	private void SpawnSocialMovesWindow(CondOwner coSlotted)
	{
		if (this._socialMovesWindow != null)
		{
			UnityEngine.Object.DestroyImmediate(this._socialMovesWindow.gameObject);
		}
		if (coSlotted == null || coSlotted.objContainer == null || this._prefabGUIInventoryList == null)
		{
			return;
		}
		this.btnShowSocialMoves.gameObject.SetActive(true);
		this._socialMovesWindow = UnityEngine.Object.Instantiate<GameObject>(this._prefabGUIInventoryList, base.transform).GetComponent<GUIInventoryList>();
		this._socialMovesWindow.SetData(coSlotted.objContainer.GetCOs(true, null));
		this._socialMovesWindow.gameObject.SetActive(false);
	}

	public void RedrawAllWindows()
	{
		for (int i = this.activeWindows.Count - 1; i >= 0; i--)
		{
			if (this.activeWindows[i] != null)
			{
				this.activeWindows[i].Redraw();
			}
		}
	}

	private void Update()
	{
		this.bLastMouseInInv = false;
		if (this.canvasGroup.alpha != 0f)
		{
			this.bLastMouseInInv = this.MouseOverInventory(Input.mousePosition);
			Vector2 vector;
			RectTransformUtility.ScreenPointToLocalPointInRectangle(this.rectInv, Input.mousePosition, CrewSim.objInstance.UICamera, out vector);
			vector.x = Mathf.Clamp(vector.x, this.rectDragBounds.rect.xMin + this.rectDragBounds.localPosition.x, this.rectDragBounds.rect.xMax + this.rectDragBounds.localPosition.x);
			vector.y = Mathf.Clamp(vector.y, this.rectDragBounds.rect.yMin + this.rectDragBounds.localPosition.y, this.rectDragBounds.rect.yMax + this.rectDragBounds.localPosition.y);
			if (this.Selected != null)
			{
				this.Selected.transform.localPosition = new Vector3((Input.mousePosition.x - (float)(Screen.width / 2)) * (1280f / (float)Screen.width * CrewSim.objInstance.AspectRatioMod()), (Input.mousePosition.y - (float)(Screen.height / 2)) * 720f / (float)Screen.height, 0f);
				CanvasManager.SetAnchorsToCorners(this.Selected.transform);
				if (this.bLastMouseInInv)
				{
					this.Selected.cgSelf.alpha = 1f;
				}
				else
				{
					this.Selected.cgSelf.alpha = 0f;
				}
			}
			if (this.selectedTab != null)
			{
				this.selectedTab.transform.parent.localPosition = new Vector3(vector.x - this.mouseOffset.x, vector.y - this.mouseOffset.y, 0f);
				CanvasManager.SetAnchorsToCorners(this.selectedTab.transform.parent.GetComponent<RectTransform>());
			}
			for (int i = this.activeWindows.Count - 1; i >= 0; i--)
			{
				this.activeWindows[i].UpdateWindow(this.CODoll);
			}
			if (this.coTooltip != null && this.Selected == null)
			{
				this.tooltip.SetTooltip(this.coTooltip, GUITooltip.TooltipWindow.Inventory);
				CrewSim.objInstance.tooltip.SetTooltip(null, GUITooltip.TooltipWindow.Hide);
				string text = this.coTooltip.FriendlyName;
				if (this.coTooltip.StackCount > 1)
				{
					string text2 = text;
					text = string.Concat(new object[]
					{
						text2,
						"(x",
						this.coTooltip.StackCount,
						")"
					});
				}
				text += "\n";
				bool flag = false;
				foreach (Condition condition in this.coTooltip.mapConds.Values)
				{
					if (condition.nDisplayOther == 2)
					{
						if (flag)
						{
							text += ",";
						}
						text = text + " " + condition.strNameFriendly;
						flag = true;
					}
				}
				this.tooltip.transform.SetSiblingIndex(base.transform.childCount - 1);
			}
			else
			{
				this.tooltip.SetTooltip(null, GUITooltip.TooltipWindow.Hide);
			}
		}
		this.LastSelected = null;
		if (this.nJustClickedItem < 3)
		{
			this.nJustClickedItem++;
		}
		if (Input.GetMouseButtonUp(0) && this.bJustClickedItem)
		{
			this.bJustClickedItem = false;
			this.nJustClickedItem = 0;
		}
	}

	public void RemoveAndDestroy(string strCOID)
	{
		foreach (GUIInventoryWindow guiinventoryWindow in this.activeWindows)
		{
			guiinventoryWindow.RemoveAndDestroy(strCOID);
		}
	}

	public bool IsCOShown(CondOwner co)
	{
		return !(co == null) && this.IsOpen && co.strID == this.PaperDollImageCG.GetComponent<GUIPaperDollManager>().strCOIDLast;
	}

	public void RotateCWSelected()
	{
		if (this.Selected == null)
		{
			return;
		}
		this.Selected.transform.Rotate(0f, 0f, -90f);
		this.Selected.fRotLast = this.Selected.transform.rotation.eulerAngles.z;
		this.Selected.UpdateStackText();
		MathUtils.Swap(ref this.Selected.itemWidthOnGrid, ref this.Selected.itemHeightOnGrid);
	}

	public static void RemoveTooltip(string strCOID = null)
	{
		if (CrewSim.inventoryGUI == null)
		{
			return;
		}
		if (strCOID == null)
		{
			CrewSim.inventoryGUI.coTooltip = null;
			return;
		}
		if (CrewSim.inventoryGUI.coTooltip == null)
		{
			return;
		}
		if (CrewSim.inventoryGUI.coTooltip.strID == strCOID)
		{
			CrewSim.inventoryGUI.coTooltip = null;
		}
	}

	public void Close(GUIInventoryWindow window)
	{
		if (this.Selected != null)
		{
			this.Reset(window);
		}
		List<GUIInventoryWindow> list = new List<GUIInventoryWindow>();
		for (int i = 0; i < this.activeWindows.Count; i++)
		{
			if (this.activeWindows[i] == null || this.activeWindows[i] == window || this.activeWindows[i].transform.parent == window.transform)
			{
				list.Add(this.activeWindows[i]);
			}
		}
		foreach (GUIInventoryWindow guiinventoryWindow in list)
		{
			this.activeWindows.Remove(guiinventoryWindow);
			if (guiinventoryWindow != null)
			{
				UnityEngine.Object.Destroy(guiinventoryWindow.gameObject);
			}
		}
		list.Clear();
		list = null;
	}

	public void RespawnWindow(GUIInventoryWindow win)
	{
		if (win == null)
		{
			return;
		}
		CondOwner co = win.CO;
		InventoryWindowType type = win.type;
		this.Close(win);
		this.SpawnInventoryWindow(co, type, null, null);
	}

	public bool IsOpen
	{
		get
		{
			return this.PaperDollImageCG.alpha > 0f && this.canvasGroup.alpha > 0f;
		}
	}

	public void Reset(GUIInventoryWindow window = null)
	{
		this.RememberSelected();
		if (this.Selected != null && window == null)
		{
			this.Selected.CleanUpCursorItem();
			this.Selected = null;
			this.UnsetDoll();
			CrewSim.objInstance.SetPartCursor(null);
		}
		GUIInventoryItem[] array = (!(window == null)) ? window.GetComponentsInChildren<GUIInventoryItem>(true) : base.GetComponentsInChildren<GUIInventoryItem>(true);
		if (array != null)
		{
			for (int i = array.Length - 1; i >= 0; i--)
			{
				UnityEngine.Object.Destroy(array[i].gameObject);
			}
		}
	}

	public bool MouseOverInventory(Vector2 position)
	{
		if (!this.canvasGroup.interactable || this.activeWindows == null)
		{
			return false;
		}
		if (this.PaperDollBackgroundCG != null && this.PaperDollImageCG.alpha > 0f)
		{
			RectTransform component = this.PaperDollBackgroundCG.GetComponent<RectTransform>();
			if (component != null && RectTransformUtility.RectangleContainsScreenPoint(component, position, CrewSim.objInstance.UICamera))
			{
				return true;
			}
		}
		foreach (GUIInventoryWindow guiinventoryWindow in GUIInventory.instance.activeWindows)
		{
			if (RectTransformUtility.RectangleContainsScreenPoint(guiinventoryWindow.gridBGRect, position, CrewSim.objInstance.UICamera))
			{
				return true;
			}
		}
		return false;
	}

	public bool ClickedInventory(Vector2 position)
	{
		return this.Selected != null || this.selectedTab != null || this.MouseOverInventory(position);
	}

	public List<CondOwner> GetGroundCOsAtPos(Ship ship, Vector2 vCheckPos)
	{
		List<CondOwner> list = new List<CondOwner>();
		if (ship == null)
		{
			return list;
		}
		ship.GetCOsAtWorldCoords1(vCheckPos, GUIInventory.CTGroundItem, true, false, list);
		return list;
	}

	public void UnsetDoll()
	{
		this._coDoll = null;
	}

	public CondOwner CODoll
	{
		get
		{
			if (this._coDoll == null)
			{
				this._coDoll = CrewSim.GetSelectedCrew();
				if (this._coDoll == null)
				{
					this._coDoll = CrewSim.coPlayer;
				}
				this.tabText.text = this._coDoll.FriendlyName;
			}
			return this._coDoll;
		}
	}

	public GUIInventoryItem Selected
	{
		get
		{
			return this._Selected;
		}
		set
		{
			Debug.Log(string.Concat(new object[]
			{
				"Selected: ",
				this._Selected,
				"->",
				value
			}));
			this.LastSelected = this._Selected;
			this._Selected = value;
			if (value == null)
			{
				foreach (GUIInventoryWindow guiinventoryWindow in this.activeWindows)
				{
					if (!(guiinventoryWindow == null))
					{
						guiinventoryWindow.Dim(false);
					}
				}
				this.PaperDollManager.ToggleSlotIconsForCO(null);
			}
			else
			{
				foreach (GUIInventoryWindow guiinventoryWindow2 in this.activeWindows)
				{
					if (!(guiinventoryWindow2 == null) && !(guiinventoryWindow2.CO == null))
					{
						if (guiinventoryWindow2.CO.objContainer != null)
						{
							if (guiinventoryWindow2.CO.objContainer.AllowedCO(value.CO))
							{
								guiinventoryWindow2.Dim(false);
							}
							else
							{
								guiinventoryWindow2.Dim(true);
							}
						}
					}
				}
				this.PaperDollManager.ToggleSlotIconsForCO(this._Selected.CO);
			}
		}
	}

	public void DelayDeselect()
	{
		base.StartCoroutine(this._DelayDeselect());
	}

	private IEnumerator _DelayDeselect()
	{
		yield return null;
		this.Selected = null;
		yield break;
	}

	public void RememberSelected()
	{
		if (GUIInventory.instance.Selected == null)
		{
			GUIInventory.strCOReselect = null;
		}
		else
		{
			GUIInventory.strCOReselect = GUIInventory.instance.Selected.CO.strID;
		}
	}

	public void RestoreSelected()
	{
		if (GUIInventory.strCOReselect == null)
		{
			return;
		}
		CondOwner objCO = null;
		if (!DataHandler.mapCOs.TryGetValue(GUIInventory.strCOReselect, out objCO))
		{
			GUIInventory.strCOReselect = null;
			return;
		}
		GUIInventoryItem guiinventoryItem = GUIInventory.GetInventoryItemFromCO(objCO);
		if (guiinventoryItem == null)
		{
			guiinventoryItem = GUIInventoryItem.SpawnInventoryItem(GUIInventory.strCOReselect, null);
		}
		if (guiinventoryItem != null)
		{
			this.Selected = guiinventoryItem;
			guiinventoryItem.AttachToCursor(null);
			guiinventoryItem.CO.RemoveFromCurrentHome(false);
			guiinventoryItem.CO.Visible = false;
		}
		GUIInventory.strCOReselect = null;
	}

	public bool NeedsRestore()
	{
		return !string.IsNullOrEmpty(GUIInventory.strCOReselect);
	}

	public bool JustClickedItem
	{
		get
		{
			return this.bJustClickedItem || this.nJustClickedItem == 0;
		}
		set
		{
			this.bJustClickedItem = value;
			this.nJustClickedItem = ((!value) ? 3 : 0);
		}
	}

	public static CondTrigger CTOpenInv
	{
		get
		{
			if (GUIInventory._ctOpenInv == null)
			{
				GUIInventory._ctOpenInv = DataHandler.GetCondTrigger("TIsOpenInInv");
			}
			return GUIInventory._ctOpenInv;
		}
	}

	public static CondTrigger CTGroundItem
	{
		get
		{
			if (GUIInventory._ctGroundItem == null)
			{
				GUIInventory._ctGroundItem = DataHandler.GetCondTrigger("TIsInvGroundItem");
			}
			return GUIInventory._ctGroundItem;
		}
	}

	public static OnOpenInventoryEvent OnOpenInventory = new OnOpenInventoryEvent();

	[SerializeField]
	private GameObject _prefabGUIInventoryList;

	[SerializeField]
	private Button btnShowSocialMoves;

	[SerializeField]
	private RectTransform rectItmAnchor;

	private GameObject inventoryGridPrefab;

	public static GameObject inventoryItemPrefab;

	private GUIInventoryItem _Selected;

	public GUIInventoryItem LastSelected;

	public GameObject selectedTab;

	private TMP_Text tabText;

	private static string strCOReselect;

	public Vector3 mouseOffset;

	public CondOwner coTooltip;

	public List<GUIInventoryWindow> activeWindows = new List<GUIInventoryWindow>();

	private CanvasGroup canvasGroup;

	private RectTransform rectDragBounds;

	private RectTransform rectInv;

	public Image PaperDollBorder;

	public CanvasGroup PaperDollBorderCG;

	public CanvasGroup PaperDollBackgroundCG;

	public CanvasGroup PaperDollImageCG;

	public GUIPaperDollManager PaperDollManager;

	private RectTransform rectPaperDoll;

	public Dictionary<string, RectTransform> mapPaperDoll;

	private static CondTrigger _ctOpenInv;

	private static CondTrigger _ctGroundItem;

	public GUITooltip tooltip;

	public CondOwner _coDoll;

	public static GUIInventory instance;

	public bool bLastMouseInInv;

	private bool bJustClickedItem;

	private int nJustClickedItem = 3;

	private GUIInventoryList _socialMovesWindow;
}
