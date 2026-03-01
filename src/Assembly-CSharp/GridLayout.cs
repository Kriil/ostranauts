using System;

public class GridLayout
{
	public GridLayout(int width, int height)
	{
		this.gridID = new string[width, height];
		this.gridInventoryItem = new GUIInventoryItem[width, height];
	}

	public int gridMaxX
	{
		get
		{
			return this.gridID.GetLength(0);
		}
	}

	public int gridMaxY
	{
		get
		{
			return this.gridID.GetLength(1);
		}
	}

	public int gridMaxSpace
	{
		get
		{
			return this.gridMaxX * this.gridMaxY;
		}
	}

	public bool Contains(PairXY pairXY)
	{
		return !pairXY.IsInvalid() && this.gridMaxX > pairXY.x && this.gridMaxY > pairXY.y;
	}

	public bool IsBlocker(PairXY pairXY)
	{
		return this.GetInventoryItem(pairXY) != null && this.GetID(pairXY) == null;
	}

	public string GetID(PairXY pairXY)
	{
		if (!this.Contains(pairXY))
		{
			return null;
		}
		return this.gridID[pairXY.x, pairXY.y];
	}

	public GUIInventoryItem GetInventoryItem(PairXY pairXY)
	{
		if (!this.Contains(pairXY))
		{
			return null;
		}
		return this.gridInventoryItem[pairXY.x, pairXY.y];
	}

	public CondOwner GetCO(PairXY pairXY)
	{
		string id = this.GetID(pairXY);
		if (id == null)
		{
			return null;
		}
		CondOwner result = null;
		DataHandler.mapCOs.TryGetValue(id, out result);
		return result;
	}

	public GUIInventoryItem GetInventoryItemFromCO(CondOwner objCO)
	{
		if (objCO == null)
		{
			return null;
		}
		string strID = objCO.strID;
		for (int i = 0; i < this.gridMaxY; i++)
		{
			for (int j = 0; j < this.gridMaxX; j++)
			{
				if (this.gridID[j, i] == strID)
				{
					return this.gridInventoryItem[j, i];
				}
			}
		}
		return null;
	}

	public void Remove(string strCOID)
	{
		if (strCOID == null)
		{
			return;
		}
		for (int i = 0; i < this.gridMaxY; i++)
		{
			for (int j = 0; j < this.gridMaxX; j++)
			{
				if (this.gridID[j, i] == strCOID)
				{
					this.gridID[j, i] = null;
					this.gridInventoryItem[j, i] = null;
				}
			}
		}
	}

	public void Remove(GUIInventoryItem inventoryItem)
	{
		if (inventoryItem == null)
		{
			return;
		}
		for (int i = 0; i < this.gridMaxY; i++)
		{
			for (int j = 0; j < this.gridMaxX; j++)
			{
				if (this.gridInventoryItem[j, i] == inventoryItem)
				{
					this.gridID[j, i] = null;
					this.gridInventoryItem[j, i] = null;
				}
			}
		}
	}

	public bool IsOccupied(int x, int y, string strIgnoreCOID = null)
	{
		return x < 0 || y < 0 || (x >= this.gridMaxX || y >= this.gridMaxY) || (!(this.gridID[x, y] == strIgnoreCOID) && (this.gridInventoryItem[x, y] != null || this.gridID[x, y] != null));
	}

	public bool IsUnoccupied(int x, int y)
	{
		return !this.IsOccupied(x, y, null);
	}

	public bool IsOccupied(PairXY pairXY)
	{
		return this.IsOccupied(pairXY.x, pairXY.y, null);
	}

	public bool IsGridRectangleUnoccupied(int x0, int y0, int x1, int y1, string strIgnoreCOID = null)
	{
		for (int i = y0; i < y1; i++)
		{
			for (int j = x0; j < x1; j++)
			{
				if (this.IsOccupied(j, i, strIgnoreCOID))
				{
					return false;
				}
			}
		}
		return true;
	}

	public PairXY FindFirstUnoccupiedTile(GUIInventoryItem item)
	{
		int itemWidthOnGrid = item.itemWidthOnGrid;
		int itemHeightOnGrid = item.itemHeightOnGrid;
		return this.FindFirstUnoccupiedTile(itemWidthOnGrid, itemHeightOnGrid, item.CO.strID);
	}

	public PairXY FindFirstUnoccupiedTile(int width, int height, string strIgnoreCOID = null)
	{
		int num = 0;
		while (num + height <= this.gridMaxY)
		{
			int num2 = 0;
			while (num2 + width <= this.gridMaxX)
			{
				if (this.IsGridRectangleUnoccupied(num2, num, num2 + width, num + height, null))
				{
					return new PairXY(num2, num);
				}
				num2++;
			}
			num++;
		}
		return PairXY.GetInvalid();
	}

	public PairXY FindNearestUnoccupiedTile(GUIInventoryItem item, float nearX, float nearY)
	{
		int itemWidthOnGrid = item.itemWidthOnGrid;
		int itemHeightOnGrid = item.itemHeightOnGrid;
		PairXY result = PairXY.GetInvalid();
		float num = 100000000f;
		int num2 = 0;
		while (num2 + itemHeightOnGrid <= this.gridMaxY)
		{
			int num3 = 0;
			while (num3 + itemWidthOnGrid <= this.gridMaxX)
			{
				float num4 = (float)num3 - nearX;
				float num5 = (float)num2 - nearY;
				float num6 = num4 * num4 + num5 * num5;
				if (num6 < num)
				{
					if (this.IsGridRectangleUnoccupied(num3, num2, num3 + itemWidthOnGrid, num2 + itemHeightOnGrid, item.CO.strID))
					{
						result = new PairXY
						{
							x = num3,
							y = num2
						};
						num = num6;
					}
				}
				num3++;
			}
			num2++;
		}
		return result;
	}

	public string[,] gridID;

	public GUIInventoryItem[,] gridInventoryItem;
}
