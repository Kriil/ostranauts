using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// One inventory grid window. Likely represents a container, floor pickup area,
// or nested inventory and manages drag/drop placement plus visible contents.
public class GUIInventoryWindow : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IEventSystemHandler
{
	// Resolves the backing CondOwner on demand, including save/load re-link by id.
	public CondOwner CO
	{
		get
		{
			if ((this._co == null || this._co.bDestroyed || this._co.ship == null) && this.strCOID != null)
			{
				DataHandler.mapCOs.TryGetValue(this.strCOID, out this._co);
			}
			return this._co;
		}
		set
		{
			if (value == null)
			{
				return;
			}
			this.strCOID = value.strID;
			this._co = value;
		}
	}

	// Cached filter used to decide which items are allowed in this inventory view.
	// Likely combines floor-tile allow lists and inventory forbid conditions from
	// loot data (`TILFloor`, `TILItemForbidsInv`).
	private static CondTrigger ctItemAllowed
	{
		get
		{
			if (GUIInventoryWindow._ctItemAllowed == null)
			{
				Loot loot = DataHandler.GetLoot("TILFloor");
				List<string> lootNames = loot.GetLootNames(null, false, null);
				loot = DataHandler.GetLoot("TILItemForbidsInv");
				List<string> lootNames2 = loot.GetLootNames(null, false, null);
				GUIInventoryWindow._ctItemAllowed = new CondTrigger("TILItemForbidsInv", lootNames.ToArray(), lootNames2.ToArray(), null, null);
			}
			return GUIInventoryWindow._ctItemAllowed;
		}
	}

	private int gridMaxX
	{
		get
		{
			return this.gridLayout.gridMaxX;
		}
	}

	private int gridMaxY
	{
		get
		{
			return this.gridLayout.gridMaxY;
		}
	}

	public bool IsChild
	{
		get
		{
			return this.cgBG.alpha == 0f;
		}
	}

	// Converts a local UI point into inventory-grid coordinates.
	private PairXY PairXYFromLocalPoint(Vector2 localPoint, int widthOffset, int heightOffset)
	{
		int num = (int)(24f * CanvasManager.CanvasRatio);
		int x = (int)(localPoint.x - (float)(widthOffset / 2)) / num;
		int y = (int)(-localPoint.y - (float)(heightOffset / 2)) / num;
		return new PairXY(x, y);
	}

	// Converts a screen-space pointer position into inventory-grid coordinates.
	public PairXY PairXYFromPosition(Vector2 position, int widthOffset, int heightOffset)
	{
		Vector2 localPoint;
		RectTransformUtility.ScreenPointToLocalPointInRectangle(this.gridImageRect, position, CrewSim.objInstance.UICamera, out localPoint);
		return this.PairXYFromLocalPoint(localPoint, widthOffset, heightOffset);
	}

	// Removes one item id from the logical grid.
	public void Remove(string strCOID)
	{
		this.gridLayout.Remove(strCOID);
	}

	// Removes one item from the logical grid and destroys its UI object.
	public void RemoveAndDestroy(string strCOID)
	{
		this.Remove(strCOID);
		GUIInventory.RemoveTooltip(strCOID);
		if (this.COGO.ContainsKey(strCOID))
		{
			UnityEngine.Object.Destroy(this.COGO[strCOID]);
			this.COGO.Remove(strCOID);
		}
	}

	// Syncs the window contents to a new item-id list by deleting stale widgets
	// and spawning any missing inventory item visuals.
	public void RedrawWindowContents(List<string> newCOIDs)
	{
		List<string> list = new List<string>();
		foreach (KeyValuePair<string, GameObject> keyValuePair in this.COGO)
		{
			if (!newCOIDs.Remove(keyValuePair.Key))
			{
				list.Add(keyValuePair.Key);
			}
		}
		foreach (string text in list)
		{
			this.RemoveAndDestroy(text);
		}
		foreach (string text2 in newCOIDs)
		{
			GUIInventoryItem.SpawnInventoryItem(text2, this);
		}
	}

	// Utility for floor windows: finds nearby non-hidden, non-human CondOwners.
	public static List<string> GetCOsAroundPosition(Vector3 position)
	{
		Collider[] array = Physics.OverlapBox(position, new Vector3(1f, 1f, 5f));
		List<string> list = new List<string>();
		for (int i = 0; i < array.Length; i++)
		{
			CondOwner component = array[i].GetComponent<CondOwner>();
			if (!(component == null))
			{
				if (!component.HasCond("IsHiddenInv") && !component.HasCond("IsHuman"))
				{
					list.Add(component.strID);
				}
			}
		}
		return list;
	}

	// Marks one preview cell as blocked or clear in the temporary ground grid.
	private void MarkValidGround(int dx, int dy, bool valid)
	{
		PairXY pairXY = new PairXY(dx + 2, dy + 2);
		if (!valid)
		{
			GUIInventoryItem.SpawnBlockingItem(this, pairXY);
			return;
		}
		if (this.gridLayout.IsBlocker(pairXY))
		{
			GUIInventoryItem inventoryItem = this.gridLayout.GetInventoryItem(pairXY);
			UnityEngine.Object.Destroy(inventoryItem.gameObject);
			this.gridLayout.Remove(inventoryItem);
		}
	}

	// Populates one ground-inventory preview cell from nearby world items, while
	// respecting line of sight and drag/hidden filters.
	private bool FillGroundInventory(int dx, int dy, ref List<string> fullCOs)
	{
		CondOwner co = this.CO;
		Vector3 position = co.tf.position;
		position.x = Mathf.Round(position.x);
		position.y = Mathf.Round(position.y);
		Vector2 vector = new Vector2(position.x + (float)dx, position.y - (float)dy);
		if (!Visibility.IsCondOwnerLOSVisibleBlocks(co, vector, false, false))
		{
			return false;
		}
		List<CondOwner> list = new List<CondOwner>();
		co.ship.GetCOsAtWorldCoords1(vector, null, true, false, list);
		for (int i = 0; i < list.Count; i++)
		{
			CondOwner condOwner = list[i];
			if (!condOwner.HasCond("IsHiddenInv") && !condOwner.HasCond("IsHuman") && !condOwner.HasCond("IsDragged"))
			{
				if (!(condOwner == GUIInventory.instance.Selected))
				{
					Item component = condOwner.GetComponent<Item>();
					if (!(component == null))
					{
						if (fullCOs.Contains(condOwner.strID))
						{
							return true;
						}
						int x = 2 + MathUtils.RoundToInt(condOwner.tf.position.x + 0.1f - (float)component.nWidthInTiles * 0.5f) - MathUtils.RoundToInt(position.x);
						int y = 2 - MathUtils.RoundToInt(condOwner.tf.position.y - 0.9f + (float)component.nHeightInTiles * 0.5f) + MathUtils.RoundToInt(position.y);
						PairXY pairXY = new PairXY(x, y);
						if (condOwner.pairInventoryXY != pairXY)
						{
							this.RemoveAndDestroy(condOwner.strID);
							condOwner.pairInventoryXY = pairXY;
						}
						fullCOs.Add(condOwner.strID);
						return true;
					}
				}
			}
		}
		return GUIInventoryWindow.IsValidPlacementTile(vector);
	}

	public static bool IsValidPlacementTile(Vector2 worldCoord)
	{
		Tile tile = null;
		if (CrewSim.shipCurrentLoaded != null)
		{
			tile = CrewSim.shipCurrentLoaded.GetTileAtWorldCoords1(worldCoord.x, worldCoord.y, true, true);
		}
		return !(tile == null) && GUIInventoryWindow.ctItemAllowed.Triggered(tile.coProps, null, true);
	}

	public bool IsValidPlacementTile(PairXY pairXYGrid)
	{
		Vector3 v = this.WorldPosFromPair(pairXYGrid);
		return GUIInventoryWindow.IsValidPlacementTile(v);
	}

	private void RedrawGroundWindow()
	{
		List<string> newCOIDs = new List<string>();
		for (int i = -2; i <= 2; i++)
		{
			for (int j = -2; j <= 2; j++)
			{
				bool valid = this.FillGroundInventory(j, i, ref newCOIDs);
				this.MarkValidGround(j, i, valid);
			}
		}
		this.RedrawWindowContents(newCOIDs);
	}

	public void Redraw()
	{
		CondOwner co = this.CO;
		if (co == null)
		{
			return;
		}
		if (this.type == InventoryWindowType.Ground)
		{
			this.RedrawGroundWindow();
			return;
		}
		Container.Redraw(co.objContainer);
	}

	public Vector3 WorldPosFromPair(PairXY where)
	{
		if (this.type == InventoryWindowType.Ground)
		{
			Vector3 position = CrewSim.GetSelectedCrew().tf.position;
			position.x = (float)(MathUtils.RoundToInt(position.x) + where.x - 2);
			position.y = (float)(MathUtils.RoundToInt(position.y) + 2 - where.y);
			return position;
		}
		return this.CO.tf.position;
	}

	public Ship ShipFromPair(PairXY where)
	{
		if (this.type != InventoryWindowType.Ground)
		{
			return this.CO.objCOParent.ship;
		}
		CondOwner selectedCrew = CrewSim.GetSelectedCrew();
		if (selectedCrew == null)
		{
			return null;
		}
		Ship ship = selectedCrew.ship;
		if (ship == null)
		{
			return null;
		}
		Vector3 v = this.WorldPosFromPair(where);
		Room roomAtWorldCoords = ship.GetRoomAtWorldCoords1(v, true);
		if (roomAtWorldCoords != null)
		{
			return roomAtWorldCoords.CO.ship;
		}
		return null;
	}

	public void SetData(CondOwner co, InventoryWindowType ttype)
	{
		this.CO = co;
		this.type = ttype;
		this.fPlayerDistance = 100000000f;
		this.cgTab = this.tabImage.GetComponent<CanvasGroup>();
		this.cgBG = this.gridBGRect.GetComponent<CanvasGroup>();
		this.cgBorder = this.gridBorderRect.GetComponent<CanvasGroup>();
		this.cgGrid = this.gridImage.GetComponent<CanvasGroup>();
		if (this.type == InventoryWindowType.Ground)
		{
			this.tabText.text = "Ground";
			this.gridLayout = new GridLayout(5, 5);
		}
		else
		{
			this.tabText.text = this.CO.ShortName;
			if (this.tabText.text == string.Empty)
			{
				this.tabText.text = this.CO.strName;
			}
			if (this.CO.objContainer != null)
			{
				this.gridLayout = this.CO.objContainer.gridLayout;
			}
			else
			{
				this.gridLayout = new GridLayout(0, 0);
				CanvasManager.HideCanvasGroup(this.cgGrid);
			}
		}
		base.gameObject.name = "Inventory Window of " + this.tabText.text;
		float num = 1.5f * CanvasManager.CanvasRatio;
		this.gridImageRect.sizeDelta = new Vector2((float)(this.gridMaxX * this.gridImage.texture.width) * num, (float)(this.gridMaxY * this.gridImage.texture.height) * num);
		this.gridImage.uvRect = new Rect(0f, 0f, (float)this.gridMaxX, (float)this.gridMaxY);
		if (this.CO.dictSlotsLayout != null && this.CO.dictSlotsLayout.ContainsKey("self"))
		{
			this.gridImage.transform.localPosition = this.CO.dictSlotsLayout["self"] * num + new Vector3(this.fBorderPadding, -this.fBorderPadding);
		}
		else
		{
			this.gridImage.transform.localPosition = new Vector3(this.fBorderPadding, -this.fBorderPadding);
		}
		this.ResetBorder();
		CanvasManager.SetAnchorsToCorners(this.gridImageRect);
		this.Redraw();
		this._pinImageTf = base.transform.Find("Tab Background/PinButton/Image");
		Button component = base.transform.Find("Tab Background/CloseButton").GetComponent<Button>();
		component.onClick.AddListener(delegate()
		{
			GUIInventory.instance.Close(this);
		});
		component = base.transform.Find("Tab Background/PinButton").GetComponent<Button>();
		component.onClick.AddListener(delegate()
		{
			this.Pin(!this.bCustomPos, true);
		});
		if (this.CO.mapGUIPropMaps.ContainsKey("GUIInv") && this.CO.mapGUIPropMaps["GUIInv"].ContainsKey("TabPos"))
		{
			this.bCustomPos = true;
			this._pinImageTf.localScale = new Vector3(1f, -1f, 1f);
		}
	}

	public void Pin(bool bPin, bool bRespawn = false)
	{
		if (bPin)
		{
			Vector3 vector = base.transform.localPosition / (1.5f * CanvasManager.CanvasRatio);
			this.CO.ApplyGPMChanges(new string[]
			{
				string.Concat(new object[]
				{
					"GUIInv,TabPos,",
					vector.x,
					"|",
					vector.y
				})
			});
			this.bCustomPos = true;
			this._pinImageTf.localScale = new Vector3(1f, -1f, 1f);
		}
		else
		{
			if (this.CO.mapGUIPropMaps.ContainsKey("GUIInv") && this.CO.mapGUIPropMaps["GUIInv"].ContainsKey("TabPos"))
			{
				this.CO.mapGUIPropMaps["GUIInv"].Remove("TabPos");
			}
			this.bCustomPos = false;
			this._pinImageTf.localScale = new Vector3(1f, 1f, 1f);
			if (bRespawn)
			{
				GUIInventory.instance.RespawnWindow(this);
			}
		}
	}

	public Vector2 GetMaxBorder()
	{
		Vector2 result = new Vector2(this.gridImage.transform.localPosition.x + this.gridImageRect.rect.width, this.gridImage.transform.localPosition.y - this.gridImageRect.rect.height);
		IEnumerator enumerator = base.transform.GetEnumerator();
		try
		{
			while (enumerator.MoveNext())
			{
				object obj = enumerator.Current;
				Transform transform = (Transform)obj;
				GUIInventoryWindow component = transform.GetComponent<GUIInventoryWindow>();
				if (!(component == null))
				{
					Vector2 maxBorder = component.GetMaxBorder();
					result.x = Mathf.Max(result.x, maxBorder.x + transform.localPosition.x);
					result.y = Mathf.Min(result.y, maxBorder.y + transform.localPosition.y);
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
		return result;
	}

	public void ResetBorder()
	{
		Vector2 maxBorder = this.GetMaxBorder();
		this.gridBGRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, maxBorder.x + this.fBorderPadding * 1f);
		this.gridBGRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, -maxBorder.y + this.fBorderPadding * 1f);
		this.gridBorderRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, maxBorder.x + this.fBorderPadding * 1f);
		this.gridBorderRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, -maxBorder.y + this.fBorderPadding * 1f);
		CanvasManager.SetAnchorsToCorners(this.gridBGRect);
		CanvasManager.SetAnchorsToCorners(this.gridBorderRect);
	}

	public void ToggleTab(bool bShow)
	{
		if (bShow)
		{
			CanvasManager.ShowCanvasGroup(this.cgTab);
			CanvasManager.ShowCanvasGroup(this.cgBorder);
			CanvasManager.ShowCanvasGroup(this.cgBG);
		}
		else
		{
			CanvasManager.HideCanvasGroup(this.cgTab);
			CanvasManager.HideCanvasGroup(this.cgBorder);
			CanvasManager.HideCanvasGroup(this.cgBG);
		}
	}

	private void UpdateColor(CondOwner inventoryOwner)
	{
		if (this.CO != null)
		{
			CondOwner condOwner = this.CO.RootParent("IsHuman");
			if (condOwner != null && inventoryOwner.strID == condOwner.strID)
			{
				this.gridImage.color = this._defaultContainerColor;
				return;
			}
		}
		this.gridImage.color = this._notOnPersonColor;
	}

	public void UpdateWindow(CondOwner CODoll)
	{
		if (this.type == InventoryWindowType.Ground)
		{
			this.Redraw();
			this.gridImage.color = this._notOnPersonColor;
		}
		if (this.type == InventoryWindowType.Container)
		{
			float num = (!(this.CO != null)) ? float.PositiveInfinity : Vector3.Distance(this.CO.tf.position, CODoll.tf.position);
			if (3.5f < num && this.fPlayerDistance < num)
			{
				GUIInventory.instance.Close(this);
			}
			else
			{
				this.fPlayerDistance = num;
			}
			this.UpdateColor(CODoll);
		}
	}

	public void Dim(bool dim)
	{
		if (this.cgGrid == null)
		{
			return;
		}
		if (dim)
		{
			this.cgGrid.alpha = 0.1f;
			this.tabText.color = DataHandler.GetColor("InvInaccessible");
			this.tabText.fontStyle = FontStyles.Strikethrough;
		}
		else
		{
			this.cgGrid.alpha = 1f;
			this.tabText.color = DataHandler.GetColor("InvAccessible");
			this.tabText.fontStyle = FontStyles.Normal;
		}
	}

	public void SurfaceWindow()
	{
		base.transform.SetAsLastSibling();
		GUIInventory.instance.tooltip.transform.SetAsLastSibling();
	}

	public void OnPointerUp(PointerEventData eventData)
	{
		this.SurfaceWindow();
	}

	public void OnPointerDown(PointerEventData eventData)
	{
	}

	public const int GROUND_RADIUS = 2;

	public RawImage tabImage;

	public RawImage gridImage;

	public RectTransform gridImageRect;

	public RectTransform gridBGRect;

	public RectTransform gridBorderRect;

	private Transform _pinImageTf;

	public TextMeshProUGUI tabText;

	private CanvasGroup cgTab;

	private CanvasGroup cgBG;

	private CanvasGroup cgBorder;

	private CanvasGroup cgGrid;

	private float fBorderPadding = 4f;

	private string strCOID;

	public InventoryWindowType type;

	public Dictionary<string, GameObject> COGO = new Dictionary<string, GameObject>();

	public GridLayout gridLayout;

	public float fPlayerDistance;

	public bool bCustomPos;

	private Color _notOnPersonColor = new Color(0.52156866f, 0.52156866f, 0.52156866f);

	private Color _defaultContainerColor = Color.white;

	private CondOwner _co;

	private static CondTrigger _ctItemAllowed;
}
