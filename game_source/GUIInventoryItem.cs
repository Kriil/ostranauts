using System;
using System.Collections.Generic;
using Ostranauts.Core;
using Ostranauts.Objectives;
using Ostranauts.UI.MegaToolTip;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// One draggable inventory-grid widget. Represents an item (or blocker cell) in
// a GUIInventoryWindow and handles grid sizing, drag/drop, and hover UI.
public class GUIInventoryItem : MonoBehaviour, IPointerDownHandler, IPointerEnterHandler, IPointerExitHandler, IPointerUpHandler, IEventSystemHandler
{
	// Resolves the inventory footprint for a CondOwner, preferring item size but
	// allowing the CondOwnerDef to override via inventoryWidth/inventoryHeight.
	public static PairXY GetWidthHeightForCO(CondOwner co)
	{
		Item item = co.Item;
		int x = 1;
		int y = 1;
		if (item != null)
		{
			x = item.nWidthInTiles;
			y = item.nHeightInTiles;
		}
		if (DataHandler.GetCondOwnerDef(co.strName) != null && DataHandler.GetCondOwnerDef(co.strName).inventoryWidth != 0)
		{
			x = DataHandler.GetCondOwnerDef(co.strName).inventoryWidth;
		}
		if (DataHandler.GetCondOwnerDef(co.strName) != null && DataHandler.GetCondOwnerDef(co.strName).inventoryHeight != 0)
		{
			y = DataHandler.GetCondOwnerDef(co.strName).inventoryHeight;
		}
		return new PairXY(x, y);
	}

	// Shows or hides the stack count badge based on the current stack size.
	public void UpdateStackText()
	{
		this.stackText.text = "x" + this.CO.StackCount.ToString();
		float alpha = (this.CO.StackCount <= 1) ? 0f : 1f;
		this.stackTextCG.alpha = alpha;
		this.cgBG.alpha = alpha;
		this.stackText.transform.rotation = Quaternion.identity;
	}

	// Caches the background badge used by the stack counter.
	private void Awake()
	{
		this.cgBG = base.transform.GetChild(0).GetComponent<CanvasGroup>();
	}

	// Debug-only cleanup log when the backing item widget is destroyed.
	private void OnDestroy()
	{
		if (this.CO != null)
		{
			Debug.Log("Destroying " + this.CO.strName);
		}
	}

	// Converts texture/grid dimensions into scaled canvas-space coordinates.
	public static Vector2 Vector2FromXYSCR(float x, float y)
	{
		return new Vector2(x, y) * 1.5f * CanvasManager.CanvasRatio;
	}

	// Convenience wrapper for converting a texture size into scaled canvas-space.
	public static Vector2 Vector2FromTextureWHSCR(Texture texture)
	{
		return GUIInventoryItem.Vector2FromXYSCR((float)texture.width, (float)texture.height);
	}

	// Spawns a temporary black blocker tile used when previewing invalid drops.
	public static void SpawnBlockingItem(GUIInventoryWindow window, PairXY pairXY)
	{
		if (window.gridLayout.GetInventoryItem(pairXY) != null)
		{
			return;
		}
		GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(GUIInventory.inventoryItemPrefab);
		GUIInventoryItem component = gameObject.GetComponent<GUIInventoryItem>();
		component.windowData = window;
		Texture texture = DataHandler.LoadPNG("GUIBlack16.png", false, false);
		component.GetComponent<RawImage>().texture = texture;
		component.itemRect.sizeDelta = GUIInventoryItem.Vector2FromTextureWHSCR(texture);
		component.itemWidthOnGrid = 1;
		component.itemHeightOnGrid = 1;
		component.PlaceOnGrid(pairXY);
	}

	// Returns true for placeholder/blocker widgets or items that no longer map to
	// a valid floor tile in the ground-inventory view.
	public bool IsBlocker()
	{
		if (this.CO == null)
		{
			return true;
		}
		if (this.windowData != null && this.windowData.type == InventoryWindowType.Ground)
		{
			Tile x = null;
			if (CrewSim.shipCurrentLoaded != null)
			{
				x = CrewSim.shipCurrentLoaded.GetTileAtWorldCoords1(this.CO.tf.position.x, this.CO.tf.position.y, true, true);
			}
			if (x == null)
			{
				return true;
			}
		}
		return false;
	}

	// Creates one inventory widget from a live CondOwner id and inserts it into
	// the requested inventory window.
	public static GUIInventoryItem SpawnInventoryItem(string strCOID, GUIInventoryWindow window = null)
	{
		if (strCOID == null || !DataHandler.mapCOs.ContainsKey(strCOID))
		{
			Debug.Log("ERROR: Trying to spawn null Item.");
			return null;
		}
		GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(GUIInventory.inventoryItemPrefab);
		GUIInventoryItem component = gameObject.GetComponent<GUIInventoryItem>();
		component.strCOID = strCOID;
		Item item = component.CO.Item;
		if (item != null)
		{
			component.fRotLast = item.fLastRotation;
		}
		component.transform.Rotate(0f, 0f, component.fRotLast);
		component.name = "Inventory item of " + component.CO.name + " ID#" + component.CO.strID;
		if (item != null)
		{
			component.UpdateImage(item);
		}
		PairXY widthHeightForCO = GUIInventoryItem.GetWidthHeightForCO(DataHandler.mapCOs[strCOID]);
		component.itemWidthOnGrid = widthHeightForCO.x;
		component.itemHeightOnGrid = widthHeightForCO.y;
		component.AddToWindow(window);
		component.UpdateStackText();
		return component;
	}

	// Loads the inventory sprite/material for the linked item and sizes the UI
	// rect to match the source texture.
	private void UpdateImage(Item item)
	{
		Texture texture = DataHandler.LoadPNG(item.ImgOverride + ".png", false, false);
		RawImage component = base.GetComponent<RawImage>();
		component.texture = texture;
		component.material = item.SetUpInventoryMaterial(texture);
		this.itemRect.localScale = Vector3.one;
		int num = texture.width;
		int num2 = texture.height;
		CondOwner condOwner = null;
		JsonCondOwner jsonCondOwner = null;
		if (DataHandler.mapCOs.TryGetValue(this.strCOID, out condOwner))
		{
			jsonCondOwner = DataHandler.GetCondOwnerDef(condOwner.strName);
		}
		if (jsonCondOwner != null)
		{
			if (jsonCondOwner.inventoryWidth != 0)
			{
				num = jsonCondOwner.inventoryWidth * 16;
			}
			if (jsonCondOwner.inventoryHeight != 0)
			{
				num2 = jsonCondOwner.inventoryHeight * 16;
			}
			component.material.SetVector("_Aspect", new Vector4((float)Mathf.Max(jsonCondOwner.inventoryWidth, 1), (float)Mathf.Max(jsonCondOwner.inventoryHeight, 1), (float)num, (float)num2));
		}
		this.itemRect.sizeDelta = GUIInventoryItem.Vector2FromXYSCR((float)num, (float)num2);
	}

	// Inserts this widget into a window, finding a valid tile if the saved
	// position cannot be used directly.
	private void AddToWindow(GUIInventoryWindow windowData)
	{
		this.windowData = windowData;
		if (windowData == null)
		{
			return;
		}
		PairXY pairXY = this.CO.pairInventoryXY;
		if (windowData.type != InventoryWindowType.Ground)
		{
			pairXY = windowData.gridLayout.FindNearestUnoccupiedTile(this, (float)pairXY.x, (float)pairXY.y);
			if (pairXY.IsInvalid())
			{
				pairXY = windowData.gridLayout.FindFirstUnoccupiedTile(this);
			}
			if (this.IsBadPlacement(windowData, pairXY))
			{
				Debug.Log("Could not fit inventory item on grid - panic!");
				return;
			}
		}
		windowData.COGO[this.CO.strID] = base.gameObject;
		this.PlaceOnGrid(pairXY);
	}

	// Convenience inverse of IsGoodPlacement().
	private bool IsBadPlacement(GUIInventoryWindow windowData, PairXY pairXY)
	{
		return !this.IsGoodPlacement(windowData, pairXY);
	}

	// Validates a drop target against container rules, grid occupancy, and ground
	// tile validity.
	private bool IsGoodPlacement(GUIInventoryWindow windowData, PairXY pairXY)
	{
		if (windowData.type == InventoryWindowType.Container && !windowData.CO.objContainer.AllowedCO(this.CO))
		{
			return false;
		}
		if (!windowData.gridLayout.IsGridRectangleUnoccupied(pairXY.x, pairXY.y, pairXY.x + this.itemWidthOnGrid, pairXY.y + this.itemHeightOnGrid, this.CO.strID))
		{
			return false;
		}
		if (windowData.type != InventoryWindowType.Ground)
		{
			return true;
		}
		if (!windowData.IsValidPlacementTile(pairXY))
		{
			return false;
		}
		GUIInventoryItem inventoryItem = windowData.gridLayout.GetInventoryItem(pairXY);
		return inventoryItem == null || inventoryItem == this;
	}

	private void PlaceOnGrid(PairXY pairXY)
	{
		if (GUIInventory.instance.Selected == this)
		{
			GUIInventory.instance.Selected = null;
		}
		if (this.CO != null)
		{
			this.CO.pairInventoryXY = pairXY;
			foreach (CondOwner condOwner in this.CO.aStack)
			{
				condOwner.pairInventoryXY = pairXY;
			}
		}
		base.transform.SetParent(this.windowData.transform);
		base.transform.localScale = Vector3.one;
		float width = this.itemRect.rect.width;
		float height = this.itemRect.rect.height;
		if (MathUtils.IsRotationVertical(this.fRotLast))
		{
			MathUtils.Swap(ref width, ref height);
		}
		base.transform.SetParent(this.windowData.gridImageRect);
		base.transform.SetSiblingIndex(this.windowData.gridImageRect.childCount - 1);
		float num = width / 2f;
		float num2 = height / 2f;
		num += (float)this.windowData.gridImage.texture.width * ((float)pairXY.x * 1.5f * CanvasManager.CanvasRatio);
		num2 += (float)this.windowData.gridImage.texture.height * ((float)pairXY.y * 1.5f * CanvasManager.CanvasRatio);
		base.transform.localPosition = new Vector3(num, -num2);
		base.transform.rotation = Quaternion.Euler(0f, 0f, this.fRotLast);
		CanvasManager.SetAnchorsToCorners(base.transform);
		GridLayout gridLayout = this.windowData.gridLayout;
		for (int i = pairXY.y; i < pairXY.y + this.itemHeightOnGrid; i++)
		{
			for (int j = pairXY.x; j < pairXY.x + this.itemWidthOnGrid; j++)
			{
				if (j >= 0 && i >= 0)
				{
					if (j < gridLayout.gridMaxX)
					{
						if (i < gridLayout.gridMaxY)
						{
							gridLayout.gridInventoryItem[j, i] = this;
							gridLayout.gridID[j, i] = this.strCOID;
						}
					}
				}
			}
		}
	}

	public void OnPointerEnter(PointerEventData eventData)
	{
		this._isMouseOverUI = true;
		GUIInventory.instance.coTooltip = this.CO;
	}

	private void Update()
	{
		if (this.CO == null)
		{
			GUIInventory.instance.PaperDollManager.ToggleSlotIconsForCO(this.CO);
			GUIInventory.RemoveTooltip(null);
			if (this.cgPaperDollCopy != null)
			{
				this.cgPaperDollCopy.alpha = 1f;
			}
			UnityEngine.Object.Destroy(base.gameObject);
			CrewSim.objInstance.SetPartCursor(null);
			return;
		}
		if (this._isMouseOverUI && Input.GetMouseButton(1))
		{
			if (this._co != null)
			{
				if (this.CO.HasCond("IsSocialItem"))
				{
					return;
				}
				CondOwner x = CrewSim.GetSelectedCrew();
				if (x == null)
				{
					x = CrewSim.coPlayer;
				}
				CrewSim.OnRightClick.Invoke(new List<CondOwner>
				{
					this._co
				});
			}
			else
			{
				CrewSim.OnRightClick.Invoke(new List<CondOwner>());
			}
		}
	}

	public void OnPointerExit(PointerEventData eventData)
	{
		this._isMouseOverUI = false;
		if (GUIInventory.instance.coTooltip == this.CO)
		{
			GUIInventory.instance.coTooltip = null;
		}
	}

	private bool PlaceAtScreenPosition(Vector2 screenPosition, bool canPlaceSelf = true)
	{
		if (this.TrashAtScreenPosition(screenPosition))
		{
			base.GetComponent<RawImage>().material.SetFloat("_StencilComp", 3f);
			return true;
		}
		if (this.SlotAtScreenPosition(screenPosition, canPlaceSelf))
		{
			if (GUIInventory.instance.IsCOShown(CrewSim.coPlayer))
			{
				if (CrewSim.coPlayer.HasCond("TutorialClothesWaiting"))
				{
					CrewSim.coPlayer.ZeroCondAmount("TutorialClothesWaiting");
					MonoSingleton<ObjectiveTracker>.Instance.CheckObjective(CrewSim.coPlayer.strID);
				}
				CrewSimTut.CheckHelmetAtmoTutorial();
			}
			base.GetComponent<RawImage>().material.SetFloat("_StencilComp", 3f);
			return true;
		}
		int i = GUIInventory.instance.activeWindows.Count - 1;
		while (i >= 0)
		{
			GUIInventoryWindow guiinventoryWindow = GUIInventory.instance.activeWindows[i];
			if (RectTransformUtility.RectangleContainsScreenPoint(guiinventoryWindow.gridImage.rectTransform, screenPosition, CrewSim.objInstance.UICamera))
			{
				if (this.MoveInventories(guiinventoryWindow, screenPosition, canPlaceSelf))
				{
					CrewSimTut.CheckHelmetAtmoTutorial();
					base.GetComponent<RawImage>().material.SetFloat("_StencilComp", 3f);
					return true;
				}
				break;
			}
			else
			{
				i--;
			}
		}
		if (this.MoveToGround(GUIInventory.instance.CODoll.ship, canPlaceSelf))
		{
			return true;
		}
		base.transform.SetAsLastSibling();
		GUIInventory.instance.CODoll.LogMessage(DataHandler.GetString("GUI_INV_NO_FIT", false), "Bad", GUIInventory.instance.CODoll.strID);
		AudioManager.am.PlayAudioEmitter("UIMessageLogBad", false, true);
		return false;
	}

	private bool TrashAtScreenPosition(Vector2 screenPosition)
	{
		if (GUIInventory.instance == null || GUIInventory.instance.PaperDollManager == null || CrewSim.coPlayer == null || this.CO == null)
		{
			return false;
		}
		if (GUIInventory.instance.PaperDollManager.AlphaHit(GUIInventory.instance.PaperDollManager.rectTrash, GUIInventory.instance.PaperDollManager.rectTrash.GetComponent<RawImage>().texture as Texture2D, Input.mousePosition))
		{
			AudioEmitter component = this.CO.GetComponent<AudioEmitter>();
			if (component != null)
			{
				component.StartTrans(false);
			}
			this.CO.RemoveFromCurrentHome(false);
			GUIInventory.RemoveTooltip(null);
			if (this.cgPaperDollCopy != null)
			{
				this.cgPaperDollCopy.alpha = 1f;
			}
			CrewSim.objInstance.SetPartCursor(null);
			GUIInventory.instance.PaperDollManager.DelayTrash(this);
			UnityEngine.Object.Destroy(base.gameObject);
			return true;
		}
		return false;
	}

	private bool SlotAtScreenPosition(Vector2 screenPosition, bool canPlaceSelf)
	{
		if (GUIInventory.instance == null || GUIInventory.instance.PaperDollManager == null || CrewSim.coPlayer == null || this.CO == null)
		{
			return false;
		}
		List<Slot> slotsForScreenPosition = GUIInventory.instance.PaperDollManager.GetSlotsForScreenPosition(screenPosition);
		JsonSlotEffects jsonSlotEffects = null;
		for (int i = 0; i < slotsForScreenPosition.Count; i++)
		{
			Slot slot = slotsForScreenPosition[i];
			if (slot == null)
			{
				Debug.LogWarning("Encountered null slot in inventory");
			}
			else if (slot.aCOs != null && slot.aCOs.Length > 0 && slot.aCOs[0] == this.CO)
			{
				if (!canPlaceSelf)
				{
					return false;
				}
				GUIInventory.RemoveTooltip(null);
				if (this.cgPaperDollCopy != null)
				{
					this.cgPaperDollCopy.alpha = 1f;
				}
				UnityEngine.Object.Destroy(base.gameObject);
				CrewSim.objInstance.SetPartCursor(null);
				return true;
			}
			else if (slot.strName != null)
			{
				Slot slot2 = slot;
				bool flag = this.CO.mapSlotEffects != null && this.CO.mapSlotEffects.ContainsKey(slot.strName);
				if (flag)
				{
					JsonSlotEffects jsonSlotEffects2 = this.CO.mapSlotEffects[slot.strName];
					CondOwner outermostCO = slot.GetOutermostCO();
					if (jsonSlotEffects2 != null && jsonSlotEffects2.aSlotsSecondary != null)
					{
						if (Array.IndexOf<string>(jsonSlotEffects2.aSlotsSecondary, slot.strName) >= 0)
						{
							Slot slot3 = GUIInventory.instance.PaperDollManager.GetSlot(jsonSlotEffects2.strSlotPrimary);
							if (slot3 != null && slotsForScreenPosition.IndexOf(slot3) < 0)
							{
								slotsForScreenPosition.Add(slot3);
							}
							goto IL_6AC;
						}
						foreach (string strSlotName in jsonSlotEffects2.aSlotsSecondary)
						{
							Slot slot4 = GUIInventory.instance.PaperDollManager.GetSlot(strSlotName);
							if (slot4 == null)
							{
								flag = false;
								break;
							}
							if (outermostCO != null)
							{
								if (slot4.GetOutermostCO() != null && slot4.GetOutermostCO() != outermostCO)
								{
									flag = false;
									jsonSlotEffects = jsonSlotEffects2;
									break;
								}
							}
							else
							{
								outermostCO = slot4.GetOutermostCO();
								if (outermostCO != null)
								{
									slot2 = slot4;
								}
							}
						}
						if (!flag)
						{
							goto IL_6AC;
						}
					}
					AudioEmitter component = this.CO.GetComponent<AudioEmitter>();
					CondOwner condOwner = null;
					Ship previousShip = null;
					if (outermostCO != null && slot2.compSlots != null)
					{
						if (slot2.bAllowStacks && outermostCO.CanStackOnItem(this.CO) >= this.CO.StackCount)
						{
							previousShip = outermostCO.ship;
							condOwner = slot2.compSlots.UnSlotItem(slot2.strName, null, false);
						}
						else
						{
							if (this.CO.CanStackOnItem(outermostCO) >= outermostCO.StackCount)
							{
								outermostCO.RemoveFromCurrentHome(false);
								if (this.CO.slotNow != null && !this.CO.slotNow.bAllowStacks)
								{
									this.CO.RemoveFromCurrentHome(false);
								}
								this.CO.StackCO(outermostCO);
								if (component != null)
								{
									component.StartTrans(false);
								}
								this.ProcessRemainder(outermostCO, null, null, outermostCO.ship);
								UnityEngine.Object.Destroy(base.gameObject);
								return true;
							}
							if (slot2.bAllowStacks || this.CO.StackCount == 1)
							{
								previousShip = outermostCO.ship;
								condOwner = slot2.compSlots.UnSlotItem(slot2.strName, null, false);
							}
						}
					}
					if (condOwner != null && condOwner.CanStackOnItem(this.CO) > 0)
					{
						this.CO.RemoveFromCurrentHome(false);
						if (this.windowData != null)
						{
							this.windowData.Remove(this.strCOID);
						}
						CondOwner coRemainder = condOwner.StackCO(this.CO);
						slot2.compSlots.SlotItem(slot2.strName, this.CO, false);
						UnityEngine.Object.Destroy(base.gameObject);
						if (component != null)
						{
							component.StartTrans(false);
						}
						this.ProcessRemainder(coRemainder, null, null, this.CO.ship);
						return true;
					}
					if (slot.OpenSpaces(this.CO, false) > 0)
					{
						if (this.CO.StackCount > 1 && !slot.bAllowStacks)
						{
							CondOwner condOwner2 = this.CO.StackAsList[this.CO.StackCount - 2];
							if (condOwner2 != null)
							{
								this.CO.PopHeadFromStack();
								GUIInventoryItem guiinventoryItem = GUIInventoryItem.SpawnInventoryItem(condOwner2.strID, null);
								guiinventoryItem.AttachToCursor(null);
							}
						}
						this.CO.RemoveFromCurrentHome(false);
						GUIInventory.RemoveTooltip(null);
						if (this.windowData != null)
						{
							this.windowData.RemoveAndDestroy(this.CO.strID);
						}
						else
						{
							UnityEngine.Object.Destroy(base.gameObject);
						}
						CrewSim.objInstance.SetPartCursor(null);
						if (GUIInventory.instance.Selected == this && this.CO.Item != null)
						{
							this.CO.Item.fLastRotation = 0f;
						}
						if (slot.compSlots != null)
						{
							slot.compSlots.SlotItem(slot.strName, this.CO, false);
						}
						GUIInventory.RemoveTooltip(null);
						if (GUIInventory.CTOpenInv.Triggered(this.CO, null, true))
						{
							GUIInventory.instance.SpawnInventoryWindow(this.CO, InventoryWindowType.Container, null, null);
						}
						if (condOwner != null)
						{
							GUIInventoryItem guiinventoryItem2 = GUIInventoryItem.SpawnInventoryItem(condOwner.strID, null);
							guiinventoryItem2.AttachToCursor(previousShip);
						}
						if (component != null)
						{
							component.StartTrans(false);
						}
						if (MonoSingleton<GUIQuickBar>.Instance.COTarget == this.CO)
						{
							MonoSingleton<GUIQuickBar>.Instance.Refresh(true);
						}
						return true;
					}
					if (condOwner != null && slot.compSlots != null)
					{
						slot.compSlots.SlotItem(slot.strName, condOwner, false);
						AudioEmitter component2 = condOwner.GetComponent<AudioEmitter>();
						if (component2 != null)
						{
							component2.StartTrans(false);
						}
					}
				}
			}
			IL_6AC:;
		}
		if (jsonSlotEffects != null)
		{
			string text = DataHandler.GetString("GUI_INV_SLOT_BLOCKED_0", false);
			Slot slot5 = GUIInventory.instance.PaperDollManager.GetSlot(jsonSlotEffects.strSlotPrimary);
			if (slot5 != null)
			{
				text += slot5.FriendlyName;
			}
			else
			{
				text += jsonSlotEffects.strSlotPrimary;
			}
			text += DataHandler.GetString("GUI_INV_SLOT_BLOCKED_1", false);
			if (jsonSlotEffects.aSlotsSecondary != null)
			{
				bool flag2 = false;
				foreach (string strSlotName2 in jsonSlotEffects.aSlotsSecondary)
				{
					slot5 = GUIInventory.instance.PaperDollManager.GetSlot(strSlotName2);
					if (slot5 != null && !(slot5.GetOutermostCO() == null))
					{
						if (flag2)
						{
							text += ", ";
						}
						text += slot5.GetOutermostCO().FriendlyName;
						flag2 = true;
					}
				}
			}
			text += DataHandler.GetString("GUI_INV_SLOT_BLOCKED_2", false);
			GUIInventory.instance.PaperDollManager.LogSlotError(text);
		}
		return false;
	}

	private void OnPointerDownSelected(PointerEventData eventData)
	{
		Debug.Log("PointerDown on Selected!");
		if (GUIActionKeySelector.commandSingleItem.Held)
		{
			this.OnRightClickDownSelected(eventData);
			return;
		}
		if (Input.GetMouseButton(1))
		{
			this.OnRightClickDownSelected(eventData);
			return;
		}
		if (this.PlaceAtScreenPosition(eventData.pressPosition, true))
		{
			if (GUIInventory.instance.Selected == this)
			{
				GUIInventory.instance.DelayDeselect();
				GUIInventory.instance.JustClickedItem = true;
			}
			return;
		}
	}

	private bool PlaceOnGround(GUIInventoryWindow window, PairXY where)
	{
		Ship ship = window.ShipFromPair(where);
		if (ship == null)
		{
			return false;
		}
		Vector3 vector = window.WorldPosFromPair(where);
		CondOwner objCOParent = this.CO.objCOParent;
		Ship ship2 = this.CO.ship;
		Item item = this.CO.Item;
		if (item != null)
		{
			float fLastRotation = item.fLastRotation;
			item.fLastRotation = this.fRotLast;
			this.CO.RemoveFromCurrentHome(false);
			vector.x += (float)item.nWidthInTiles * 0.5f - 0.5f;
			vector.y -= (float)item.nHeightInTiles * 0.5f - 0.5f;
			if (!item.CheckFit(vector, ship, null, null))
			{
				CondOwner condOwner = this.CO;
				item.fLastRotation = fLastRotation;
				if (ship2 != null)
				{
					AudioEmitter component = this.CO.GetComponent<AudioEmitter>();
					if (component != null)
					{
						component.StartTrans(false);
					}
					ship2.AddCO(this.CO, true);
					return false;
				}
				if (objCOParent != null)
				{
					AudioEmitter component2 = this.CO.GetComponent<AudioEmitter>();
					if (component2 != null)
					{
						component2.StartTrans(false);
					}
					condOwner = objCOParent.AddCO(this.CO, false, true, false);
					if (condOwner != null)
					{
						condOwner = objCOParent.AddCO(condOwner, true, true, false);
					}
				}
				if (condOwner != null)
				{
					List<CondOwner> list = new List<CondOwner>();
					ship.GetCOsAtWorldCoords1(vector, null, true, false, list);
					foreach (CondOwner condOwner2 in list)
					{
						if (condOwner2.CanStackOnItem(condOwner) >= condOwner.StackCount)
						{
							condOwner = condOwner2.StackCO(condOwner);
						}
						if (condOwner == null)
						{
							return true;
						}
					}
				}
				if (condOwner != null)
				{
					condOwner.ship = window.ShipFromPair(where);
					condOwner = CrewSim.GetSelectedCrew().DropCO(condOwner, false, null, 0f, 0f, true, null);
					if (!(condOwner != null))
					{
						return true;
					}
					Debug.LogWarning("Couldn't restore " + condOwner.FriendlyName + " to its original place! Leaving on current ship");
					CrewSim.shipCurrentLoaded.AddCO(condOwner, true);
					AudioEmitter component3 = condOwner.GetComponent<AudioEmitter>();
					if (component3 != null)
					{
						component3.StartTrans(false);
					}
				}
				return false;
			}
		}
		GUIInventory.instance.Selected = null;
		this.CO.pairInventoryXY = where;
		this.CO.tf.position = vector;
		if (ship != null)
		{
			AudioEmitter component4 = this.CO.GetComponent<AudioEmitter>();
			if (component4 != null)
			{
				component4.StartTrans(false);
			}
			ship.AddCO(this.CO, true);
		}
		if (item != null)
		{
			item.ResetTransforms(vector.x, vector.y);
		}
		if (this.windowData == null)
		{
			UnityEngine.Object.Destroy(base.gameObject);
			CrewSim.objInstance.SetPartCursor(null);
		}
		this.CO.UpdateAppearance();
		return true;
	}

	public void DropOnGround(GUIInventoryWindow destination)
	{
		if (this.windowData == null)
		{
			UnityEngine.Object.Destroy(base.gameObject);
			CrewSim.objInstance.SetPartCursor(null);
		}
		CondOwner objCOParent = this.CO.objCOParent;
		if (objCOParent != null)
		{
			CondOwner coRemainder = objCOParent.DropCO(this.CO, false, null, 0f, 0f, true, null);
			this.ProcessRemainder(coRemainder, destination, objCOParent, null);
		}
	}

	public void CleanUpCursorItem()
	{
		if (string.IsNullOrEmpty(this.strCOID) || this.CO == null || string.IsNullOrEmpty(this.CO.strID))
		{
			return;
		}
		if (this._previousShip != null && this.CO.tf.parent == null)
		{
			CondOwner condOwner = this._previousShip.DropCO(this.CO, this.CO.tfVector2Position);
			if (condOwner != null)
			{
				this._previousShip.AddCO(condOwner, true);
			}
		}
	}

	public void OnPointerUp(PointerEventData eventData)
	{
		if (!GUIInventory.instance.IsInventoryVisible || this.CO == null)
		{
			return;
		}
		if (GUIInventory.instance.Selected == this && Time.realtimeSinceStartup > GUIInventoryItem.sPointerDownTime + 0.3f)
		{
			if (CrewSim.GetMouseButtonUp(1) && !CrewSim.objInstance.contextMenuPool.IsRaised && CrewSim.objInstance.RightMouseButtonDownTimer < 0.5f)
			{
				CrewSim.objInstance.RightMouseButtonDownTimer = 0f;
				this.OnRightClickDownSelected(eventData);
				return;
			}
			if (!GUIActionKeySelector.commandForceWalk.Held && this.PlaceAtScreenPosition(eventData.position, true))
			{
				return;
			}
		}
		GUIInventory.instance.mouseOffset = Vector3.zero;
	}

	public void OnPointerDown(PointerEventData eventData)
	{
		if (this.strCOID == null)
		{
			return;
		}
		if (Input.GetMouseButtonDown(0) && !GUIActionKeySelector.commandForceWalk.Held)
		{
			GUIInventoryItem.sPointerDownTime = Time.realtimeSinceStartup;
			if (this.windowData != null)
			{
				this.windowData.SurfaceWindow();
			}
			if (GUIInventory.instance.Selected == null && !this.CO.HasCond("IsSocialItem"))
			{
				if (GUIActionKeySelector.commandQuickMove.Held)
				{
					this.OnShiftPointerDown();
					CondOwner condOwner = CrewSim.GetSelectedCrew();
					if (condOwner == null)
					{
						condOwner = CrewSim.coPlayer;
					}
					if (condOwner.objContainer != null)
					{
						condOwner.objContainer.Redraw();
					}
					return;
				}
				this.AttachToCursor(null);
				return;
			}
			else if (GUIInventory.instance.Selected == this)
			{
				this.OnPointerDownSelected(eventData);
			}
		}
	}

	private CondOwner GetInventoryCrew()
	{
		CondOwner selectedCrew = CrewSim.GetSelectedCrew();
		if (selectedCrew != null)
		{
			return selectedCrew;
		}
		return CrewSim.coPlayer;
	}

	public bool StackOrAddToContainer(Container container)
	{
		if (container == null || !container.AllowedCO(this.CO))
		{
			return false;
		}
		CondOwner objCOParent = this.CO.objCOParent;
		Ship ship = this.CO.ship;
		foreach (CondOwner condOwner in container.GetCOs(false, null))
		{
			if (!(condOwner == this.CO))
			{
				if (!(condOwner.coStackHead != null))
				{
					if (condOwner.CanStackOnItem(this.CO) > 0)
					{
						this.CO.RemoveFromCurrentHome(false);
						if (this.windowData != null)
						{
							this.windowData.Remove(this.strCOID);
						}
						else
						{
							UnityEngine.Object.Destroy(base.gameObject);
						}
						if (container.InventoryWindow != null)
						{
							container.InventoryWindow.Redraw();
						}
						CondOwner coRemainder = condOwner.StackCO(this.CO);
						this.ProcessRemainder(coRemainder, container.InventoryWindow, objCOParent, ship);
						return true;
					}
				}
			}
		}
		PairXY pairXY;
		if (container.CanAddSimple(this.CO, out pairXY))
		{
			this.CO.RemoveFromCurrentHome(false);
			container.AddCOSimple(this.CO, pairXY);
			UnityEngine.Object.Destroy(base.gameObject);
			CrewSim.objInstance.SetPartCursor(null);
			if (container.InventoryWindow != null)
			{
				container.InventoryWindow.Redraw();
			}
			return true;
		}
		return false;
	}

	private void OnRightClickDownSelected(PointerEventData eventData)
	{
		CondOwner co = this.CO;
		List<CondOwner> stackAsList = this.CO.StackAsList;
		if (stackAsList.Count > 1)
		{
			List<Slot> slotsForScreenPosition = GUIInventory.instance.PaperDollManager.GetSlotsForScreenPosition(eventData.position);
			bool flag = slotsForScreenPosition.Count == 0;
			for (int i = 0; i < slotsForScreenPosition.Count; i++)
			{
				Slot slot = slotsForScreenPosition[i];
				if (slot.strName != null)
				{
					if (this.CO.mapSlotEffects.ContainsKey(slot.strName))
					{
						CondOwner outermostCO = slot.GetOutermostCO();
						if (!(outermostCO != null) || outermostCO.CanStackOnItem(this.CO) != 0)
						{
							flag = true;
						}
					}
				}
			}
			if (!flag)
			{
				return;
			}
			int widthOffset = (int)(this.itemRect.rect.width - 24f * CanvasManager.CanvasRatio);
			int heightOffset = (int)(this.itemRect.rect.height - 24f * CanvasManager.CanvasRatio);
			if (MathUtils.IsRotationVertical(this.fRotLast))
			{
				MathUtils.Swap(ref widthOffset, ref heightOffset);
			}
			int j = GUIInventory.instance.activeWindows.Count - 1;
			while (j >= 0)
			{
				GUIInventoryWindow guiinventoryWindow = GUIInventory.instance.activeWindows[j];
				if (RectTransformUtility.RectangleContainsScreenPoint(guiinventoryWindow.gridImage.rectTransform, eventData.position, CrewSim.objInstance.UICamera))
				{
					PairXY pairXY = guiinventoryWindow.PairXYFromPosition(eventData.position, widthOffset, heightOffset);
					GUIInventoryItem inventoryItem = guiinventoryWindow.gridLayout.GetInventoryItem(pairXY);
					if (inventoryItem != null && inventoryItem.CO == this.CO)
					{
						return;
					}
					if (this.IsBadPlacement(guiinventoryWindow, pairXY) && (inventoryItem == null || inventoryItem.CO == null || inventoryItem.CO.CanStackOnItem(this.CO) == 0))
					{
						return;
					}
					break;
				}
				else
				{
					j--;
				}
			}
		}
		CondOwner condOwner = stackAsList[stackAsList.Count - 1];
		CondOwner condOwner2 = null;
		if (stackAsList.Count > 1)
		{
			condOwner2 = condOwner.PopHeadFromStack();
		}
		this.CO = condOwner;
		if (!this.PlaceAtScreenPosition(eventData.position, true))
		{
			if (condOwner2 != null)
			{
				condOwner2.StackCO(condOwner);
			}
			this.CO = co;
			if (this.windowData != null)
			{
				this.windowData.Redraw();
			}
			return;
		}
		if (condOwner2 != null)
		{
			if (this.windowData != null)
			{
				this.windowData.Redraw();
			}
			else
			{
				UnityEngine.Object.Destroy(base.gameObject);
				CrewSim.objInstance.SetPartCursor(null);
			}
			GUIInventoryItem guiinventoryItem = GUIInventory.GetInventoryItemFromCO(condOwner2);
			if (guiinventoryItem == null)
			{
				guiinventoryItem = GUIInventoryItem.SpawnInventoryItem(condOwner2.strID, null);
			}
			guiinventoryItem.AttachToCursor(null);
		}
	}

	public void OnShiftPointerDown()
	{
		CondOwner inventoryCrew = this.GetInventoryCrew();
		if (this.windowData != null && this.windowData.type == InventoryWindowType.Container && this.CO.RootParent(null) == inventoryCrew)
		{
			if (this.CO.objCOParent.DropCO(this.CO, false, null, 0f, 0f, true, null) == null)
			{
				return;
			}
			this.AttachToCursor(null);
			return;
		}
		else if (this.windowData == null && this.CO.objCOParent != null)
		{
			if (this.CO.objCOParent.DropCO(this.CO, false, null, 0f, 0f, true, null) == null)
			{
				return;
			}
			this.AttachToCursor(null);
			return;
		}
		else
		{
			this.CO.RemoveFromCurrentHome(false);
			GUIInventory.RemoveTooltip(null);
			if (this.windowData != null)
			{
				this.windowData.RemoveAndDestroy(this.CO.strID);
			}
			else
			{
				UnityEngine.Object.Destroy(base.gameObject);
				CrewSim.objInstance.SetPartCursor(null);
			}
			CondOwner condOwner = inventoryCrew.AddCO(this.CO, true, true, false);
			if (condOwner == null)
			{
				if (GUIInventory.instance.IsCOShown(CrewSim.coPlayer) && CrewSim.coPlayer.HasCond("TutorialClothesWaiting"))
				{
					CrewSim.coPlayer.ZeroCondAmount("TutorialClothesWaiting");
					MonoSingleton<ObjectiveTracker>.Instance.CheckObjective(CrewSim.coPlayer.strID);
				}
				CrewSimTut.CheckHelmetAtmoTutorial();
				if (GUIInventory.CTOpenInv.Triggered(this.CO, null, true))
				{
					GUIInventory.instance.SpawnInventoryWindow(this.CO, InventoryWindowType.Container, null, null);
				}
				GUIInventory.instance.RedrawAllWindows();
				return;
			}
			GUIInventoryItem guiinventoryItem = GUIInventoryItem.SpawnInventoryItem(condOwner.strID, null);
			if (guiinventoryItem != null)
			{
				guiinventoryItem.AttachToCursor(null);
			}
			return;
		}
	}

	public void AttachToCursor(Ship previousShip = null)
	{
		if (previousShip != null)
		{
			this._previousShip = previousShip;
		}
		else
		{
			this._previousShip = this.CO.ship;
		}
		Debug.Log("Attaching " + this.CO.strName + " to cursor!");
		GUIInventory.instance.Selected = this;
		base.transform.SetSiblingIndex(base.transform.childCount - 1);
		base.transform.SetParent(GUIInventory.instance.transform);
		float num = 1.2f;
		base.transform.localScale = new Vector3(num, num, num);
		base.GetComponent<RawImage>().material.SetFloat("_StencilComp", 0f);
		if (this.windowData != null && this.windowData.type == InventoryWindowType.Ground)
		{
			this.windowData.Redraw();
		}
		AudioEmitter component = this.CO.GetComponent<AudioEmitter>();
		if (component != null)
		{
			component.StartPickup();
		}
		CrewSim.objInstance.SetPartCursor(this.CO.strCODef);
		if (this.CO.Item != null)
		{
			Item component2 = CrewSim.objInstance.goSelPart.GetComponent<Item>();
			component2.fLastRotation = this.CO.Item.fLastRotation;
		}
		if (GUIMegaToolTip.Selected != null)
		{
			CrewSim.OnRightClick.Invoke(new List<CondOwner>());
		}
	}

	private void ProcessRemainder(CondOwner coRemainder, GUIInventoryWindow destination, CondOwner previousContainer, Ship previousShip)
	{
		base.transform.localScale = Vector3.one;
		GUIInventory.instance.Selected = null;
		this.UpdateStackText();
		CrewSim.objInstance.SetPartCursor(null);
		if (coRemainder != null && previousContainer != null && previousContainer.objContainer != null)
		{
			PairXY pairXY;
			if (previousContainer.objContainer.CanAddSimple(coRemainder, out pairXY))
			{
				previousContainer.objContainer.AddCOSimple(coRemainder, pairXY);
			}
			else
			{
				coRemainder = previousContainer.AddCO(coRemainder, false, true, true);
			}
		}
		if (destination != null)
		{
			destination.Redraw();
		}
		if (this.windowData != null)
		{
			this.windowData.Redraw();
		}
		if (coRemainder != null)
		{
			GUIInventoryItem guiinventoryItem = null;
			if (this.windowData != null)
			{
				guiinventoryItem = this.windowData.gridLayout.GetInventoryItemFromCO(coRemainder);
			}
			if (guiinventoryItem == null && destination != null)
			{
				guiinventoryItem = destination.gridLayout.GetInventoryItemFromCO(coRemainder);
			}
			if (guiinventoryItem == null)
			{
				guiinventoryItem = GUIInventoryItem.SpawnInventoryItem(coRemainder.strID, null);
			}
			if (guiinventoryItem != null)
			{
				guiinventoryItem.AttachToCursor(null);
			}
			else if (previousShip != null)
			{
				coRemainder = previousShip.DropCO(coRemainder, this.CO.tf.position);
			}
		}
	}

	public bool MoveInventories(GUIInventoryWindow destination, Vector2 position, bool canPlaceSelf)
	{
		if (this.CO == null || destination == null)
		{
			return false;
		}
		if (this.CO.HasCond("IsSocialItem") && destination != this.windowData)
		{
			return false;
		}
		if (!RectTransformUtility.RectangleContainsScreenPoint(destination.gridImage.rectTransform, position, CrewSim.objInstance.UICamera))
		{
			return false;
		}
		if (destination.type == InventoryWindowType.Container && destination.CO.objContainer != null && !destination.CO.objContainer.AllowedCO(this.CO))
		{
			return false;
		}
		int widthOffset = (int)(this.itemRect.rect.width - 24f * CanvasManager.CanvasRatio);
		int heightOffset = (int)(this.itemRect.rect.height - 24f * CanvasManager.CanvasRatio);
		if (MathUtils.IsRotationVertical(this.fRotLast))
		{
			MathUtils.Swap(ref widthOffset, ref heightOffset);
		}
		PairXY pairXY = destination.PairXYFromPosition(position, widthOffset, heightOffset);
		CondOwner condOwner = null;
		GUIInventoryItem guiinventoryItem = null;
		int num = 0;
		for (int i = 0; i < this.itemWidthOnGrid; i++)
		{
			for (int j = 0; j < this.itemHeightOnGrid; j++)
			{
				PairXY pairXY2 = new PairXY(pairXY.x + i, pairXY.y + j);
				GUIInventoryItem inventoryItem = destination.gridLayout.GetInventoryItem(pairXY2);
				if (inventoryItem != null && inventoryItem != this)
				{
					if (inventoryItem.IsBlocker())
					{
						return false;
					}
					if (guiinventoryItem != inventoryItem)
					{
						num++;
					}
					guiinventoryItem = inventoryItem;
					condOwner = destination.gridLayout.GetCO(pairXY2);
				}
				if (destination.type == InventoryWindowType.Ground && !destination.IsValidPlacementTile(pairXY2))
				{
					return false;
				}
			}
		}
		if (num > 1)
		{
			return false;
		}
		if (this.windowData != null)
		{
			Debug.Log(string.Concat(new object[]
			{
				"MoveInventories from ",
				this.windowData.name,
				".",
				this.CO.name,
				" to ",
				destination.name,
				"(",
				pairXY.x,
				",",
				pairXY.y,
				")"
			}));
		}
		CondOwner objCOParent = this.CO.objCOParent;
		Ship ship = this.CO.ship;
		AudioEmitter component = this.CO.GetComponent<AudioEmitter>();
		if (condOwner == this.CO)
		{
			if (!canPlaceSelf)
			{
				return false;
			}
			condOwner = null;
		}
		if (condOwner != null)
		{
			if (condOwner.CanStackOnItem(this.CO) > 0)
			{
				this.CO.RemoveFromCurrentHome(false);
				CondOwner coRemainder = condOwner.StackCO(this.CO);
				if (component != null)
				{
					component.StartTrans(false);
				}
				UnityEngine.Object.Destroy(base.gameObject);
				this.ProcessRemainder(coRemainder, destination, objCOParent, ship);
				return true;
			}
			if (condOwner.objContainer != null && this.StackOrAddToContainer(condOwner.objContainer))
			{
				if (component != null)
				{
					component.StartTrans(false);
				}
				destination.Redraw();
				if (this.windowData != null)
				{
					this.windowData.Redraw();
				}
				return true;
			}
		}
		if (guiinventoryItem != null && guiinventoryItem.CO == null)
		{
			guiinventoryItem = null;
		}
		else if (guiinventoryItem != null)
		{
			destination.Remove(guiinventoryItem.CO.strID);
		}
		if (!this.IsGoodPlacement(destination, pairXY))
		{
			if (guiinventoryItem != null)
			{
				guiinventoryItem.AddToWindow(destination);
			}
			destination.Redraw();
			return false;
		}
		this.CO.RemoveFromCurrentHome(false);
		if (this.windowData == null)
		{
			UnityEngine.Object.Destroy(base.gameObject);
			CrewSim.objInstance.SetPartCursor(null);
		}
		Item item = this.CO.Item;
		if (item != null)
		{
			item.fLastRotation = this.fRotLast;
		}
		Ship previousShip = null;
		if (condOwner != null)
		{
			CondOwner objCOParent2 = condOwner.objCOParent;
			previousShip = condOwner.ship;
			condOwner.RemoveFromCurrentHome(false);
		}
		if (destination.type == InventoryWindowType.Ground)
		{
			if (!this.PlaceOnGround(destination, pairXY))
			{
				return false;
			}
		}
		else
		{
			destination.CO.objContainer.AddCOSimple(this.CO, pairXY);
		}
		GUIInventoryItem guiinventoryItem2 = null;
		if (condOwner != null)
		{
			guiinventoryItem2 = GUIInventory.GetInventoryItemFromCO(condOwner);
			if (guiinventoryItem2 == null)
			{
				guiinventoryItem2 = GUIInventoryItem.SpawnInventoryItem(condOwner.strID, null);
			}
			guiinventoryItem2.AttachToCursor(previousShip);
		}
		if (destination != null)
		{
			destination.Redraw();
		}
		if (this.windowData != null)
		{
			this.windowData.Redraw();
		}
		if (component != null)
		{
			component.StartTrans(false);
		}
		if (MonoSingleton<GUIQuickBar>.Instance.COTarget == this.CO)
		{
			MonoSingleton<GUIQuickBar>.Instance.Refresh(true);
		}
		if (guiinventoryItem2 == null)
		{
			CrewSim.objInstance.SetPartCursor(null);
		}
		return true;
	}

	public bool MoveToGround(Ship objShip, bool canPlaceSelf)
	{
		if (this.CO == null)
		{
			return false;
		}
		if (this.CO.HasCond("IsSocialItem"))
		{
			return false;
		}
		if (GUIInventory.instance.bLastMouseInInv || CrewSim.objInstance.goSelPart == null)
		{
			return false;
		}
		Vector2 v = CrewSim.objInstance.goSelPart.GetComponent<Item>().rend.bounds.center;
		Vector2 vector = new Vector2(v.x - ((float)this.itemWidthOnGrid / 2f - 0.5f) * 1f, v.y + ((float)this.itemHeightOnGrid / 2f - 0.5f) * 1f);
		vector.x -= 1f;
		vector.y += 1f;
		int num = 0;
		Vector2 tfVector2Position = CrewSim.GetSelectedCrew().tfVector2Position;
		Vector2 vector2 = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
		Vector2 vector3 = new Vector2(float.NegativeInfinity, float.NegativeInfinity);
		foreach (CondOwner condOwner in objShip.aDocksys)
		{
			Vector2 pos = condOwner.GetPos("DockA", false);
			Vector2 pos2 = condOwner.GetPos("DockB", false);
			Vector2 vector4 = pos2 - pos;
			float num2 = vector4.magnitude / 2f;
			if (vector4.y > 0.5f)
			{
				vector2.y = pos.y + num2;
			}
			else if (vector4.y < -0.5f)
			{
				vector3.y = pos.y - num2;
			}
			else if (vector4.x > 0.5f)
			{
				vector2.x = pos.x + num2;
			}
			else if (vector4.x < -0.5f)
			{
				vector3.x = pos.x - num2;
			}
		}
		CondOwner condOwner2 = null;
		int num3 = 0;
		float num4 = (float)(1 + Mathf.Max(this.itemHeightOnGrid, this.itemWidthOnGrid));
		for (int i = 0; i < this.itemHeightOnGrid + 2; i++)
		{
			for (int j = 0; j < this.itemWidthOnGrid + 2; j++)
			{
				bool flag = i == 0 || i == this.itemHeightOnGrid + 1 || j == 0 || j == this.itemWidthOnGrid + 1;
				num = i * (this.itemWidthOnGrid + 2) + j;
				Vector2 vector5 = new Vector2(vector.x + (float)j, vector.y - (float)i);
				bool flag2 = vector5.x <= vector2.x && vector5.x >= vector3.x && vector5.y <= vector2.y && vector5.y >= vector3.y;
				if ((float)TileUtils.TileRange(vector5, tfVector2Position) > num4)
				{
					flag2 = false;
				}
				if (flag2)
				{
					if (num >= this.CO.Item.aSocketReqs.Count)
					{
						break;
					}
					bool flag3 = this.CO.Item.aSocketReqs[num].aCOs.Length + this.CO.Item.aSocketReqs[num].aLoots.Length == 0;
					bool flag4 = this.CO.Item.aSocketForbids[num].aCOs.Length + this.CO.Item.aSocketReqs[num].aLoots.Length == 0;
					if (!flag3 || !flag4)
					{
						Tile tileAtWorldCoords = objShip.GetTileAtWorldCoords1(vector5.x, vector5.y, true, true);
						if (!(tileAtWorldCoords == null))
						{
							CondOwner coProps = tileAtWorldCoords.coProps;
							if (!new CondTrigger
							{
								aReqs = this.CO.Item.aSocketReqs[num].GetLootNames(null, false, null).ToArray(),
								aForbids = this.CO.Item.aSocketForbids[num].GetLootNames(null, false, null).ToArray()
							}.Triggered(coProps, null, true))
							{
								List<CondOwner> list = new List<CondOwner>();
								objShip.GetCOsAtWorldCoords1(vector5, GUIInventory.CTGroundItem, true, false, list);
								CondOwner condOwner3 = null;
								if (list.Count > 0)
								{
									condOwner3 = list[0];
								}
								if (condOwner2 != condOwner3)
								{
									num3++;
								}
								condOwner2 = condOwner3;
							}
						}
					}
				}
			}
			if (num >= this.CO.Item.aSocketReqs.Count)
			{
				break;
			}
		}
		if (num3 > 1)
		{
			return false;
		}
		if (this.windowData != null)
		{
			Debug.Log(string.Concat(new object[]
			{
				"MoveInventories from ",
				this.windowData.name,
				".",
				this.CO.name,
				" to ground (",
				v.x,
				",",
				v.y,
				")"
			}));
		}
		CondOwner objCOParent = this.CO.objCOParent;
		Ship ship = this.CO.ship;
		AudioEmitter component = this.CO.GetComponent<AudioEmitter>();
		if (condOwner2 == this.CO)
		{
			if (!canPlaceSelf)
			{
				return false;
			}
			condOwner2 = null;
		}
		if (condOwner2 != null)
		{
			if (condOwner2.CanStackOnItem(this.CO) > 0)
			{
				this.CO.RemoveFromCurrentHome(false);
				CondOwner coRemainder = condOwner2.StackCO(this.CO);
				if (component != null)
				{
					component.StartTrans(false);
				}
				UnityEngine.Object.Destroy(base.gameObject);
				this.ProcessRemainder(coRemainder, null, objCOParent, ship);
				return true;
			}
			if (condOwner2.objContainer != null && condOwner2.objContainer.gridLayout.FindFirstUnoccupiedTile(this).IsValid() && this.StackOrAddToContainer(condOwner2.objContainer))
			{
				if (component != null)
				{
					component.StartTrans(false);
				}
				if (this.windowData != null)
				{
					this.windowData.Redraw();
				}
				return true;
			}
		}
		Ship ship2 = null;
		if (condOwner2 != null)
		{
			ship2 = condOwner2.RemoveFromCurrentHome(false);
			condOwner2.Visible = false;
		}
		if (!CrewSim.objInstance.goSelPart.GetComponent<CondOwner>().Item.CheckFit(v, objShip, null, null))
		{
			if (condOwner2 != null && ship2 != null)
			{
				ship2.AddCO(condOwner2, true);
			}
			return false;
		}
		this.CO.RemoveFromCurrentHome(false);
		this.CO.tf.position = CrewSim.objInstance.goSelPart.transform.position;
		if (this.windowData == null)
		{
			UnityEngine.Object.Destroy(base.gameObject);
			CrewSim.objInstance.SetPartCursor(null);
		}
		Item item = this.CO.Item;
		if (item != null)
		{
			item.fLastRotation = this.fRotLast;
		}
		objShip.AddCO(this.CO, true);
		string text = null;
		if (condOwner2 != null)
		{
			GUIInventoryItem guiinventoryItem = GUIInventory.GetInventoryItemFromCO(condOwner2);
			if (guiinventoryItem == null)
			{
				guiinventoryItem = GUIInventoryItem.SpawnInventoryItem(condOwner2.strID, null);
			}
			guiinventoryItem.AttachToCursor(ship2);
			text = condOwner2.strCODef;
			GUIInventory.instance.JustClickedItem = true;
		}
		if (this.windowData != null)
		{
			this.windowData.Redraw();
		}
		if (component != null)
		{
			component.StartTrans(false);
		}
		if (MonoSingleton<GUIQuickBar>.Instance.COTarget == this.CO)
		{
			MonoSingleton<GUIQuickBar>.Instance.Refresh(true);
		}
		if (text == null)
		{
			CrewSim.objInstance.SetPartCursor(text);
		}
		return true;
	}

	public CondOwner CO
	{
		get
		{
			if (this._co == null || this._co.bDestroyed || this._co.ship == null)
			{
				this._co = null;
			}
			if (this._co == null && this.strCOID != null)
			{
				DataHandler.mapCOs.TryGetValue(this.strCOID, out this._co);
			}
			return this._co;
		}
		set
		{
			this.strCOID = ((!(value == null)) ? value.strID : null);
			this._co = value;
		}
	}

	private const float SINGLECLICKTIME = 0.3f;

	private string strCOID;

	private Ship _previousShip;

	public GUIInventoryWindow windowData;

	public CanvasGroup cgPaperDollCopy;

	public int itemWidthOnGrid;

	public int itemHeightOnGrid;

	public RectTransform itemRect;

	public TextMeshProUGUI stackText;

	public CanvasGroup cgSelf;

	public CanvasGroup cgBG;

	public CanvasGroup stackTextCG;

	public const float sizeCoefficient = 1.5f;

	public float fRotLast;

	private bool _isMouseOverUI;

	private static float sPointerDownTime;

	private CondOwner _co;
}
