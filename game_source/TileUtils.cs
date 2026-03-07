using System;
using System.Collections.Generic;
using System.Linq;
using Ostranauts.Core.Models;
using Ostranauts.Ships;
using UnityEngine;

// Shared ship-grid helpers. This class appears to own temporary placement-grid
// visuals and utility methods for resizing or decorating ship tilemaps.
public class TileUtils
{
	// Expands a ship's tilemap by padding empty border tiles around it, then
	// remaps room doors, zone tile ids, and work-manager tile references.
	public static bool PadTilemap(Ship objShipIn, GameObject goShip, int nLeft, int nRight, int nTop, int nBottom)
	{
		if (objShipIn == null || goShip == null || (nLeft == 0 && nRight == 0 && nTop == 0 && nBottom == 0))
		{
			return false;
		}
		int num = objShipIn.nCols + nLeft + nRight;
		int num2 = objShipIn.nRows + nTop + nBottom;
		List<Tile> list = new List<Tile>();
		bool result = false;
		for (int i = 0; i < num2; i++)
		{
			for (int j = 0; j < num; j++)
			{
				if (i < nTop || i >= num2 - nBottom || j < nLeft || j >= num - nRight)
				{
					result = true;
					string strCO = "TIL";
					string strName = null;
					string strPortraitImg = "blank.png";
					bool bLoot = false;
					string strPrefab = "prefabQuadTile";
					Transform transform = goShip.transform;
					CondOwner condOwner = DataHandler.GetCondOwner(strCO, strName, strPortraitImg, bLoot, strPrefab, null, null, transform);
					GameObject gameObject = condOwner.gameObject;
					Tile component = gameObject.GetComponent<Tile>();
					component.bShipTile = true;
					component.coProps = condOwner;
					component.coProps.ship = objShipIn;
					component.ToggleVis();
					list.Add(component);
					gameObject.layer = 8;
					gameObject.transform.position = new Vector3(objShipIn.vShipPos.x + (float)j - (float)nLeft, objShipIn.vShipPos.y - (float)i + (float)nTop, gameObject.transform.position.z);
				}
				else
				{
					list.Add(objShipIn.aTiles[(i - nTop) * objShipIn.nCols + j - nLeft]);
				}
				list[list.Count - 1].Index = list.Count - 1;
			}
		}
		foreach (Room room in objShipIn.aRooms)
		{
			if (room == null)
			{
				Debug.LogWarning("WARNING: Null room found in ship " + objShipIn.strRegID + "'s aRooms.");
			}
			else
			{
				Dictionary<string, int> dictionary = new Dictionary<string, int>();
				foreach (KeyValuePair<string, int> keyValuePair in room.dictDoors)
				{
					int value = keyValuePair.Value;
					int value2 = list.IndexOf(objShipIn.aTiles[value]);
					dictionary[keyValuePair.Key] = value2;
				}
				room.dictDoors = dictionary;
			}
		}
		foreach (JsonZone jsonZone in objShipIn.mapZones.Values)
		{
			for (int k = 0; k < jsonZone.aTiles.Length; k++)
			{
				int num3 = jsonZone.aTiles[k];
				if (objShipIn.aTiles.Count <= num3)
				{
					Debug.LogWarning(string.Concat(new object[]
					{
						"Tile index ",
						num3,
						" out of range on ship ",
						objShipIn.strRegID
					}));
				}
				else
				{
					jsonZone.aTiles[k] = objShipIn.aTiles[num3].Index;
				}
			}
		}
		CrewSim.objInstance.workManager.RefreshTileIDs(objShipIn.strRegID, objShipIn.aTiles);
		objShipIn.aTiles = list;
		objShipIn.vShipPos.x = objShipIn.vShipPos.x - (float)nLeft;
		objShipIn.vShipPos.y = objShipIn.vShipPos.y + (float)nTop;
		objShipIn.nCols = num;
		objShipIn.nRows = num2;
		return result;
	}

	// Creates or reuses one temporary tile sprite used for placement previews.
	public static Tile NewGridSprite()
	{
		GameObject mesh = DataHandler.GetMesh("prefabQuadTile", null);
		Tile component = mesh.GetComponent<Tile>();
		TileUtils.aSelPartTiles.Add(component);
		mesh.transform.parent = TileUtils.goSelPartTiles.transform;
		mesh.transform.position = new Vector3(mesh.transform.position.x, mesh.transform.position.y, -8f);
		return component;
	}

	// Returns a cached power-input overlay sprite for install/repair previews.
	public static GameObject GetPowerInputGridSprite(int nIndex)
	{
		if (TileUtils.aPowerInputGridSprites == null)
		{
			TileUtils.aPowerInputGridSprites = new List<GameObject>();
		}
		if (TileUtils.aPowerInputGridSprites.Count > nIndex && TileUtils.aPowerInputGridSprites[nIndex] != null)
		{
			return TileUtils.aPowerInputGridSprites[nIndex];
		}
		while (TileUtils.aPowerInputGridSprites.Count < nIndex + 1)
		{
			GameObject gameObject;
			if (TileUtils.aPowerInputGridSprites.Count == 0)
			{
				gameObject = Resources.Load<GameObject>("prefabPowerInputTile");
				gameObject = UnityEngine.Object.Instantiate<GameObject>(gameObject);
			}
			else
			{
				gameObject = UnityEngine.Object.Instantiate<GameObject>(TileUtils.aPowerInputGridSprites[0]);
			}
			TileUtils.aPowerInputGridSprites.Add(gameObject);
			gameObject.transform.parent = TileUtils.goSelPartTiles.transform;
		}
		return TileUtils.aPowerInputGridSprites[nIndex];
	}

	// Hides and resets the shared placement/connection preview sprites.
	public static void ResetItemGridSprites()
	{
		if (TileUtils.aSelPartTiles != null)
		{
			foreach (Tile tile in TileUtils.aSelPartTiles)
			{
				tile.SetColor(Item.rgbFit);
				tile.SetMat(Item.strFit);
				tile.gameObject.SetActive(false);
			}
		}
		if (TileUtils.aPowerInputGridSprites != null)
		{
			for (int i = TileUtils.aPowerInputGridSprites.Count - 1; i >= 0; i--)
			{
				GameObject gameObject = TileUtils.aPowerInputGridSprites[i];
				if (gameObject == null)
				{
					TileUtils.aPowerInputGridSprites.Remove(gameObject);
				}
				else
				{
					gameObject.SetActive(false);
				}
			}
		}
		if (TileUtils.goUseGridSprite != null)
		{
			TileUtils.goUseGridSprite.SetActive(false);
		}
		if (TileUtils.goPowerOutputGridSprite != null)
		{
			TileUtils.goPowerOutputGridSprite.SetActive(false);
		}
		if (TileUtils.goReactorGridSprite != null)
		{
			TileUtils.goReactorGridSprite.SetActive(false);
		}
	}

	// Lazy-loads the generic use-point preview marker.
	public static GameObject GetUseGridSprite()
	{
		if (TileUtils.goUseGridSprite != null)
		{
			return TileUtils.goUseGridSprite;
		}
		TileUtils.goUseGridSprite = Resources.Load<GameObject>("prefabUsePointTile");
		TileUtils.goUseGridSprite = UnityEngine.Object.Instantiate<GameObject>(TileUtils.goUseGridSprite);
		TileUtils.goUseGridSprite.transform.parent = TileUtils.goSelPartTiles.transform;
		return TileUtils.goUseGridSprite;
	}

	// Lazy-loads the reactor input preview marker.
	public static GameObject GetReactorGridSprite()
	{
		if (TileUtils.goReactorGridSprite != null)
		{
			return TileUtils.goReactorGridSprite;
		}
		TileUtils.goReactorGridSprite = Resources.Load<GameObject>("prefabReactorInputTile");
		TileUtils.goReactorGridSprite = UnityEngine.Object.Instantiate<GameObject>(TileUtils.goReactorGridSprite);
		TileUtils.goReactorGridSprite.transform.parent = TileUtils.goSelPartTiles.transform;
		return TileUtils.goReactorGridSprite;
	}

	// Lazy-loads the power-output preview marker.
	public static GameObject GetPowerOutputGridSprite()
	{
		if (TileUtils.goPowerOutputGridSprite != null)
		{
			return TileUtils.goPowerOutputGridSprite;
		}
		TileUtils.goPowerOutputGridSprite = Resources.Load<GameObject>("prefabPowerOutputTile");
		TileUtils.goPowerOutputGridSprite = UnityEngine.Object.Instantiate<GameObject>(TileUtils.goPowerOutputGridSprite);
		TileUtils.goPowerOutputGridSprite.transform.parent = TileUtils.goPowerOutputGridSprite.transform;
		return TileUtils.goPowerOutputGridSprite;
	}

	public static int TileRange(Tile tilStart, Tile tilEnd)
	{
		int result = 0;
		if (tilStart != null && tilEnd != null)
		{
			return TileUtils.TileRange(tilStart.transform.position, tilEnd.transform.position);
		}
		return result;
	}

	public static int TileRange(Vector2 ptStart, Vector3 ptEnd)
	{
		float num = Mathf.Abs(ptEnd.x - ptStart.x);
		float num2 = Mathf.Abs(ptEnd.y - ptStart.y);
		if (num > num2)
		{
			return Mathf.RoundToInt(num);
		}
		return Mathf.RoundToInt(num2);
	}

	public static bool TryFitItem(Item itm, Ship objShip, Vector3 vNear, out Vector3 vFits)
	{
		vFits = Vector3.zero;
		if (itm == null || objShip == null)
		{
			return false;
		}
		JsonZone jsonZone = new JsonZone();
		Tile[] surroundingTiles = TileUtils.GetSurroundingTiles(objShip.GetTileAtWorldCoords1(vNear.x, vNear.y, true, true), false, false);
		jsonZone.aTiles = new int[surroundingTiles.Length];
		for (int i = 0; i < surroundingTiles.Length; i++)
		{
			jsonZone.aTiles[i] = -1;
			if (surroundingTiles[i] != null)
			{
				jsonZone.aTiles[i] = surroundingTiles[i].Index;
			}
		}
		return TileUtils.TryFitItem(itm, objShip, jsonZone, out vFits);
	}

	public static bool TryFitItem(Item itm, Ship objShip, JsonZone jz, out Vector3 vFits)
	{
		vFits = Vector3.zero;
		if (itm == null || objShip == null || jz == null)
		{
			return false;
		}
		Vector3 b = default(Vector3);
		if (itm.nWidthInTiles % 2 == 0)
		{
			b.x = 0.5f;
		}
		if (itm.nHeightInTiles % 2 == 0)
		{
			b.y = 0.5f;
		}
		foreach (int nIndex in jz.aTiles)
		{
			vFits = objShip.GetWorldCoordsAtTileIndex1(nIndex);
			vFits += b;
			if (itm.CheckFit(vFits, objShip, null, jz))
			{
				return true;
			}
		}
		return false;
	}

	public static List<T> RotateTilesCW<T>(List<T> aTilesIn, int nTilesWide)
	{
		int num = Mathf.RoundToInt((float)(aTilesIn.Count / nTilesWide));
		int i = 0;
		int j = (num - 1) * nTilesWide;
		List<T> list = new List<T>();
		while (i < nTilesWide)
		{
			while (j >= 0)
			{
				list.Add(aTilesIn[i + j]);
				if (aTilesIn[i + j] is Tile)
				{
					Tile tile = aTilesIn[i + j] as Tile;
					float x = tile.transform.position.x;
					float y = tile.transform.position.y;
					tile.transform.position = new Vector3(y, -x, tile.transform.position.z);
					tile.Index = list.Count - 1;
				}
				j -= nTilesWide;
			}
			i++;
			j = (num - 1) * nTilesWide;
		}
		return list;
	}

	public static void TrimTiles(Ship objShip)
	{
		if (objShip == null)
		{
			return;
		}
		int nCols = objShip.nCols;
		int nRows = objShip.nRows;
		List<int> list = new List<int>();
		Dictionary<int, int> dictionary = new Dictionary<int, int>();
		bool flag = false;
		List<CondOwner> list2 = new List<CondOwner>();
		CondTrigger ct = new CondTrigger("NonSys", new string[0], new string[]
		{
			"IsSystem"
		}, null, null);
		for (int i = nRows - 1; i >= 0; i--)
		{
			List<int> list3 = new List<int>();
			for (int j = 0; j < nCols; j++)
			{
				int num = nCols * i + j;
				list2.Clear();
				objShip.GetCOsAtWorldCoords1(objShip.aTiles[num].tf.position, ct, false, true, list2);
				list3.Add(num);
				if (list2.Count >= 1)
				{
					flag = true;
					break;
				}
				if (TileUtils.CTShipTileOrSub.Triggered(objShip.aTiles[num].coProps, null, true))
				{
					flag = true;
					break;
				}
			}
			if (flag)
			{
				break;
			}
			list.AddRange(list3);
		}
		if (list.Count >= objShip.nCols)
		{
			list.RemoveRange(list.Count - objShip.nCols, objShip.nCols);
		}
		list.Sort();
		List<Room> list4 = new List<Room>();
		for (int k = list.Count - 1; k >= 0; k--)
		{
			Tile tile = objShip.aTiles[list[k]];
			foreach (Room room in objShip.aRooms)
			{
				if (list4.IndexOf(room) < 0)
				{
					room.RemoveTile(tile);
					if (room.aTiles.Count == 0)
					{
						list4.Add(room);
					}
					else
					{
						room.CO.tf.position = room.aTiles[0].tf.position;
					}
				}
			}
			objShip.aTiles.Remove(tile);
			tile.tf.SetParent(null);
			tile.Destroy();
			UnityEngine.Object.Destroy(tile.gameObject);
		}
		foreach (Room room2 in list4)
		{
			objShip.aRooms.Remove(room2);
			room2.Destroy();
		}
		objShip.nRows = objShip.aTiles.Count / nCols;
	}

	public static void GetPoweredTiles(Ship objShip)
	{
		CondTrigger condTrigger = new CondTrigger();
		condTrigger.aReqs = new string[]
		{
			"IsPowerGen",
			"IsInstalled"
		};
		condTrigger.aForbids = new string[]
		{
			"IsOverrideOff"
		};
		List<CondOwner> icos = objShip.GetICOs1(condTrigger, true, false, false);
		condTrigger.aReqs[0] = "IsPowerStorage";
		icos.AddRange(objShip.GetICOs1(condTrigger, true, false, false));
		condTrigger.aReqs[0] = "IsRechargingContainer";
		icos.AddRange(objShip.GetICOs1(condTrigger, true, false, false));
		Tile tile = null;
		foreach (Tile tile2 in objShip.aTiles)
		{
			tile2.aConnectedPowerCOs.Clear();
			tile2.bPathChecked = false;
		}
		List<Tile> list = new List<Tile>();
		objShip.aPwrTiles.Clear();
		foreach (CondOwner condOwner in icos)
		{
			if (condOwner.mapPoints.ContainsKey("PowerOutput"))
			{
				Vector2 pos = condOwner.GetPos("PowerOutput", false);
				tile = objShip.GetTileAtWorldCoords1(pos.x, pos.y, false, true);
				if (tile != null)
				{
					list.Add(tile);
				}
				for (int i = 0; i < list.Count; i++)
				{
					tile = list[i];
					tile.bPathChecked = true;
					tile.aConnectedPowerCOs.Add(condOwner.Pwr);
					Tile[] surroundingTiles = TileUtils.GetSurroundingTiles(tile, true, false);
					if (tile.coProps.HasCond("IsPowerPath"))
					{
						foreach (Tile tile3 in surroundingTiles)
						{
							if (tile3 != null)
							{
								if (!tile3.bPathChecked && tile3.coProps.HasCond("IsPowerPath"))
								{
									if (list.IndexOf(tile3) < 0)
									{
										list.Add(tile3);
										objShip.aPwrTiles.Add(tile);
										objShip.aPwrTiles.Add(tile3);
									}
									else
									{
										bool flag = false;
										for (int k = 0; k < objShip.aPwrTiles.Count - 2; k += 2)
										{
											if (objShip.aPwrTiles[i] == tile && objShip.aPwrTiles[i + 1] == tile3)
											{
												flag = true;
												break;
											}
										}
										if (!flag)
										{
											objShip.aPwrTiles.Add(tile);
											objShip.aPwrTiles.Add(tile3);
										}
									}
								}
							}
						}
					}
				}
				list.Clear();
				foreach (Tile tile4 in objShip.aTiles)
				{
					tile4.bPathChecked = false;
				}
			}
		}
	}

	public static List<Tile> GetFloodTiles(Tile tilStart, int nMax, CondTrigger ctFilter)
	{
		if (tilStart == null || nMax < 1)
		{
			return new List<Tile>();
		}
		List<Tile> list = new List<Tile>();
		if (ctFilter != null && !ctFilter.Triggered(tilStart.coProps, null, true))
		{
			return list;
		}
		List<Tile> list2 = new List<Tile>
		{
			tilStart
		};
		foreach (Tile tile in tilStart.coProps.ship.aTiles)
		{
			tile.bPathChecked = false;
		}
		for (int i = 0; i < list2.Count; i++)
		{
			if (list.Count > nMax)
			{
				break;
			}
			Tile tile2 = list2[i];
			tile2.bPathChecked = true;
			Tile[] surroundingTiles = TileUtils.GetSurroundingTiles(tile2, true, false);
			if (ctFilter == null || ctFilter.Triggered(tile2.coProps, null, true))
			{
				list.Add(tile2);
				foreach (Tile tile3 in surroundingTiles)
				{
					if (tile3 != null)
					{
						if (!tile3.bPathChecked)
						{
							if (ctFilter == null || ctFilter.Triggered(tile3.coProps, null, true))
							{
								if (list2.IndexOf(tile3) < 0)
								{
									list2.Add(tile3);
								}
							}
						}
					}
				}
			}
		}
		return list;
	}

	public static List<Tile> GetFloodTilesAround(Tile tilStart, int nMax, CondTrigger ctFilter)
	{
		if (tilStart == null || nMax < 1)
		{
			return new List<Tile>();
		}
		List<Tile> list = new List<Tile>();
		List<Tile> list2 = new List<Tile>
		{
			tilStart
		};
		foreach (Tile tile in tilStart.coProps.ship.aTiles)
		{
			tile.bPathChecked = false;
		}
		for (int i = 0; i < list2.Count; i++)
		{
			if (list.Count > nMax)
			{
				break;
			}
			Tile tile2 = list2[i];
			tile2.bPathChecked = true;
			Tile[] surroundingTiles = TileUtils.GetSurroundingTiles(tile2, false, true);
			if (ctFilter == null || ctFilter.Triggered(tile2.coProps, null, true) || !(tile2 != tilStart))
			{
				list.Add(tile2);
				foreach (Tile tile3 in surroundingTiles)
				{
					if (tile3 != null)
					{
						if (!tile3.bPathChecked)
						{
							if (ctFilter == null || ctFilter.Triggered(tile3.coProps, null, true))
							{
								if (list2.IndexOf(tile3) < 0)
								{
									list2.Add(tile3);
								}
							}
						}
					}
				}
			}
		}
		if (list.IndexOf(tilStart) >= 0)
		{
			list.Remove(tilStart);
		}
		return list;
	}

	public static JsonZone GetZoneFromTileRadius(Ship objShip, Vector3 vWorldPos, int nRange, bool bShuffled = false, bool bCircle = false)
	{
		JsonZone jsonZone = new JsonZone();
		if (objShip == null)
		{
			Debug.Log("Error: Null objShip!");
			Debug.Break();
			return jsonZone;
		}
		jsonZone.strName = string.Concat(new object[]
		{
			"Loot spawn: ",
			objShip.strRegID,
			": ",
			vWorldPos,
			", range: ",
			nRange
		});
		Tile[] surroundingTilesRadius = TileUtils.GetSurroundingTilesRadius(objShip.GetTileAtWorldCoords1(vWorldPos.x, vWorldPos.y, false, true), nRange, false, bCircle);
		jsonZone.aTiles = new int[surroundingTilesRadius.Length];
		for (int i = 0; i < surroundingTilesRadius.Length; i++)
		{
			jsonZone.aTiles[i] = surroundingTilesRadius[i].Index;
		}
		if (bShuffled)
		{
			MathUtils.ShuffleArray<int>(jsonZone.aTiles);
		}
		return jsonZone;
	}

	public static List<CondOwner> DropCOsNearby(List<CondOwner> aCOs, IShip objShip, JsonZone jz, List<CondOwner> aNearbyCOs, CondTrigger ct, bool bIgnoreLocks, bool bDropInContainersLast = true)
	{
		if (aCOs == null || objShip == null)
		{
			return aCOs;
		}
		List<CondOwner> list = new List<CondOwner>();
		CondOwner condOwner = null;
		while (aCOs.Count > 0)
		{
			bool flag = false;
			if (condOwner == aCOs[0])
			{
				aCOs.RemoveAt(0);
				if (condOwner != null && list.IndexOf(condOwner) < 0)
				{
					list.Add(condOwner);
				}
			}
			else
			{
				condOwner = aCOs[0];
				if (aNearbyCOs != null)
				{
					for (int i = 0; i < aNearbyCOs.Count; i++)
					{
						CondOwner condOwner2 = aNearbyCOs[i];
						if (!condOwner2.HasCond("IsNotValidDrop"))
						{
							if (condOwner2.CanStackOnItem(condOwner) > 0)
							{
								CondOwner condOwner3 = condOwner2.StackCO(condOwner);
								aNearbyCOs.RemoveAt(i);
								if (ct == null || ct.Triggered(condOwner, null, true))
								{
									aNearbyCOs.Insert(i, condOwner);
								}
								AudioEmitter component = condOwner.GetComponent<AudioEmitter>();
								if (component != null)
								{
									component.StartTrans(false);
								}
								condOwner = condOwner3;
							}
							else if (!bDropInContainersLast)
							{
								CondOwner condOwner4 = condOwner2.AddCO(condOwner, false, true, bIgnoreLocks);
								condOwner = condOwner4;
							}
							if (condOwner == null)
							{
								flag = true;
								break;
							}
						}
					}
				}
				if (flag || condOwner == null)
				{
					aCOs.RemoveAt(0);
				}
				else if (jz != null && jz.aTiles != null)
				{
					if (jz.categoryConds != null && jz.categoryConds.Length > 0)
					{
						bool flag2 = false;
						int num = 0;
						while (num < jz.categoryConds.Length && !flag2)
						{
							if (condOwner.HasCond(jz.categoryConds[num]))
							{
								flag2 = true;
							}
							num++;
						}
						if (!flag2)
						{
							continue;
						}
					}
					Vector3 position = default(Vector3);
					bool flag3 = false;
					Item item = condOwner.Item;
					if (item != null)
					{
						item.ResetTransforms(condOwner.tf.position.x, condOwner.tf.position.y);
						if (TileUtils.TryFitItem(item, (Ship)objShip, jz, out position))
						{
							condOwner.tf.position = position;
							objShip.AddCO(condOwner, true);
							item.ResetTransforms(position.x, position.y);
							if (ct == null || ct.Triggered(condOwner, null, true))
							{
								aNearbyCOs.Add(condOwner);
							}
							AudioEmitter component2 = condOwner.GetComponent<AudioEmitter>();
							if (component2 != null)
							{
								component2.StartTrans(false);
							}
							condOwner = null;
							aCOs.RemoveAt(0);
							flag3 = true;
						}
					}
					else if (condOwner.Crew != null)
					{
						objShip.AddCO(condOwner, true);
						flag3 = true;
					}
					if (!flag3 && list.IndexOf(condOwner) < 0)
					{
						list.Add(condOwner);
					}
				}
			}
		}
		if (bDropInContainersLast)
		{
			condOwner = null;
			List<CondOwner> list2 = new List<CondOwner>();
			while (list.Count > 0)
			{
				bool flag4 = false;
				if (condOwner == list[0] || list[0].coStackHead != null)
				{
					list.RemoveAt(0);
					if (condOwner != null && list2.IndexOf(condOwner) < 0)
					{
						list2.Add(condOwner);
					}
				}
				else
				{
					condOwner = list[0];
					if (aNearbyCOs != null)
					{
						for (int j = 0; j < aNearbyCOs.Count; j++)
						{
							CondOwner condOwner5 = aNearbyCOs[j];
							if (!condOwner5.HasCond("IsNotValidDrop"))
							{
								condOwner = condOwner5.AddCO(condOwner, false, true, bIgnoreLocks);
								if (condOwner == null)
								{
									flag4 = true;
									break;
								}
							}
						}
					}
					if (flag4 || condOwner == null)
					{
						list.RemoveAt(0);
					}
				}
			}
			list = list2;
		}
		return list;
	}

	public static Tile GetTileCloseToCenter(List<Tile> tiles)
	{
		if (tiles == null || tiles.Count == 0)
		{
			return null;
		}
		Vector3 vector = Vector3.zero;
		foreach (Tile tile in tiles)
		{
			vector += tile.tf.position;
		}
		vector /= (float)tiles.Count;
		return TileUtils.GetClosestTile(tiles, vector);
	}

	public static Tile GetClosestTile(IEnumerable<Tile> tiles, Vector3 position)
	{
		Tile result = null;
		float num = 1000f;
		foreach (Tile tile in tiles)
		{
			if (!(tile == null))
			{
				Vector3 position2 = tile.tf.position;
				float num2 = Vector3.Distance(position, position2);
				if (num2 < num)
				{
					result = tile;
					num = num2;
				}
			}
		}
		return result;
	}

	public static Tile[] GetSurroundingTilesRadius(Tile tilCenter, int nRadius, bool bAllowDocked = false, bool bCircle = false)
	{
		List<Tile> list = new List<Tile>();
		if (tilCenter == null)
		{
			return list.ToArray();
		}
		Ship ship = tilCenter.coProps.ship;
		if (ship == null)
		{
			return list.ToArray();
		}
		int index = tilCenter.Index;
		int num = index % ship.nCols;
		int num2 = index / ship.nCols;
		float num3 = TileUtils.GridToUnits(1);
		for (int i = num2 - nRadius; i <= num2 + nRadius; i++)
		{
			for (int j = num - nRadius; j <= num + nRadius; j++)
			{
				if (!bCircle || (num - j) * (num - j) + (num2 - i) * (num2 - i) <= nRadius * nRadius)
				{
					if (j < 0 || j >= ship.nCols || i < 0 || i >= ship.nRows)
					{
						if (bAllowDocked)
						{
							list.Add(ship.GetTileAtWorldCoords1(tilCenter.tf.position.x + num3 * (float)j, tilCenter.tf.position.y + num3 * (float)i, bAllowDocked, true));
						}
					}
					else
					{
						list.Add(ship.aTiles[i * ship.nCols + j]);
					}
				}
			}
		}
		return list.ToArray();
	}

	public static Tile[] GetSurroundingTiles(Tile tilCenter, bool bCardinalOnly = false, bool bAllowDocked = false)
	{
		Tile[] array = new Tile[8];
		if (tilCenter == null || tilCenter.coProps == null)
		{
			return array;
		}
		Ship ship = tilCenter.coProps.ship;
		if (ship == null)
		{
			return array;
		}
		int index = tilCenter.Index;
		int num = index % ship.nCols;
		int num2 = index / ship.nCols;
		float num3 = TileUtils.GridToUnits(1);
		if (num > 0)
		{
			array[3] = ship.aTiles[index - 1];
		}
		else if (bAllowDocked)
		{
			array[3] = ship.GetTileAtWorldCoords1(tilCenter.tf.position.x - num3, tilCenter.tf.position.y, bAllowDocked, true);
		}
		if (num2 > 0)
		{
			array[1] = ship.aTiles[index - ship.nCols];
		}
		else if (bAllowDocked)
		{
			array[1] = ship.GetTileAtWorldCoords1(tilCenter.tf.position.x, tilCenter.tf.position.y + num3, bAllowDocked, true);
		}
		if (num < ship.nCols - 1)
		{
			array[4] = ship.aTiles[index + 1];
		}
		else if (bAllowDocked)
		{
			array[4] = ship.GetTileAtWorldCoords1(tilCenter.tf.position.x + num3, tilCenter.tf.position.y, bAllowDocked, true);
		}
		if (num2 < ship.nRows - 1)
		{
			array[6] = ship.aTiles[index + ship.nCols];
		}
		else if (bAllowDocked)
		{
			array[6] = ship.GetTileAtWorldCoords1(tilCenter.tf.position.x, tilCenter.tf.position.y - num3, bAllowDocked, true);
		}
		if (bCardinalOnly)
		{
			return array;
		}
		int num4 = index - ship.nCols - 1;
		if (num4 >= 0 && ship.aTiles.Count > num4)
		{
			array[0] = ship.aTiles[num4];
		}
		else if (bAllowDocked)
		{
			array[0] = ship.GetTileAtWorldCoords1(tilCenter.tf.position.x - num3, tilCenter.tf.position.y + num3, bAllowDocked, true);
		}
		num4 = index - ship.nCols + 1;
		if (num4 >= 0 && ship.aTiles.Count > num4)
		{
			array[2] = ship.aTiles[num4];
		}
		else if (bAllowDocked)
		{
			array[2] = ship.GetTileAtWorldCoords1(tilCenter.tf.position.x + num3, tilCenter.tf.position.y + num3, bAllowDocked, true);
		}
		num4 = index + ship.nCols - 1;
		if (num4 >= 0 && ship.aTiles.Count > num4)
		{
			array[5] = ship.aTiles[num4];
		}
		else if (bAllowDocked)
		{
			array[5] = ship.GetTileAtWorldCoords1(tilCenter.tf.position.x - num3, tilCenter.tf.position.y - num3, bAllowDocked, true);
		}
		num4 = index + ship.nCols + 1;
		if (num4 >= 0 && ship.aTiles.Count > num4)
		{
			array[7] = ship.aTiles[num4];
		}
		else if (bAllowDocked)
		{
			array[7] = ship.GetTileAtWorldCoords1(tilCenter.tf.position.x + num3, tilCenter.tf.position.y - num3, bAllowDocked, true);
		}
		return array;
	}

	public static void GetSurroundingTiles(ref Tile[] aTiles, Tile tilCenter, bool forceExact = false)
	{
		if (tilCenter == null)
		{
			return;
		}
		Ship ship = tilCenter.coProps.ship;
		if (ship == null)
		{
			return;
		}
		int index = tilCenter.Index;
		int num = index % ship.nCols;
		int num2 = index / ship.nCols;
		if (!forceExact && 1 < num && 1 < num2 && num + 2 < ship.nCols && num2 + 2 < ship.nRows)
		{
			aTiles[0] = ship.aTiles[index - 1];
			aTiles[1] = ship.aTiles[index - ship.nCols];
			aTiles[2] = ship.aTiles[index + 1];
			aTiles[3] = ship.aTiles[index + ship.nCols];
			aTiles[4] = ship.aTiles[index - ship.nCols - 1];
			aTiles[5] = ship.aTiles[index - ship.nCols + 1];
			aTiles[6] = ship.aTiles[index + ship.nCols - 1];
			aTiles[7] = ship.aTiles[index + ship.nCols + 1];
			return;
		}
		float num3 = TileUtils.GridToUnits(1);
		float x = tilCenter.tf.position.x;
		float y = tilCenter.tf.position.y;
		aTiles[0] = ship.GetTileAtWorldCoords1(x - num3, y, true, true);
		aTiles[1] = ship.GetTileAtWorldCoords1(x, y + num3, true, true);
		aTiles[2] = ship.GetTileAtWorldCoords1(x + num3, y, true, true);
		aTiles[3] = ship.GetTileAtWorldCoords1(x, y - num3, true, true);
		aTiles[4] = ship.GetTileAtWorldCoords1(x - num3, y + num3, true, true);
		aTiles[5] = ship.GetTileAtWorldCoords1(x + num3, y + num3, true, true);
		aTiles[6] = ship.GetTileAtWorldCoords1(x - num3, y - num3, true, true);
		aTiles[7] = ship.GetTileAtWorldCoords1(x + num3, y - num3, true, true);
	}

	public static Tile[] GetSurroundingTilesCardinalFirst(Tile tilCenter, bool bCardinalOnly = false, bool bAllowDocked = false)
	{
		Tile[] array = new Tile[8];
		if (tilCenter == null)
		{
			return array;
		}
		Ship ship = tilCenter.coProps.ship;
		if (ship == null)
		{
			return array;
		}
		int index = tilCenter.Index;
		int num = index % ship.nCols;
		int num2 = index / ship.nCols;
		float num3 = TileUtils.GridToUnits(1);
		if (num > 0)
		{
			array[0] = ship.aTiles[index - 1];
		}
		else if (bAllowDocked)
		{
			array[0] = ship.GetTileAtWorldCoords1(tilCenter.tf.position.x - num3, tilCenter.tf.position.y, bAllowDocked, true);
		}
		if (num2 > 0)
		{
			array[1] = ship.aTiles[index - ship.nCols];
		}
		else if (bAllowDocked)
		{
			array[1] = ship.GetTileAtWorldCoords1(tilCenter.tf.position.x, tilCenter.tf.position.y + num3, bAllowDocked, true);
		}
		if (num < ship.nCols - 1)
		{
			array[2] = ship.aTiles[index + 1];
		}
		else if (bAllowDocked)
		{
			array[2] = ship.GetTileAtWorldCoords1(tilCenter.tf.position.x + num3, tilCenter.tf.position.y, bAllowDocked, true);
		}
		if (num2 < ship.nRows - 1)
		{
			array[3] = ship.aTiles[index + ship.nCols];
		}
		else if (bAllowDocked)
		{
			array[3] = ship.GetTileAtWorldCoords1(tilCenter.tf.position.x, tilCenter.tf.position.y - num3, bAllowDocked, true);
		}
		if (bCardinalOnly)
		{
			return array;
		}
		int num4 = index - ship.nCols - 1;
		if (num4 >= 0 && ship.aTiles.Count > num4)
		{
			array[4] = ship.aTiles[num4];
		}
		else if (bAllowDocked)
		{
			array[4] = ship.GetTileAtWorldCoords1(tilCenter.tf.position.x - num3, tilCenter.tf.position.y + num3, bAllowDocked, true);
		}
		num4 = index - ship.nCols + 1;
		if (num4 >= 0 && ship.aTiles.Count > num4)
		{
			array[5] = ship.aTiles[num4];
		}
		else if (bAllowDocked)
		{
			array[5] = ship.GetTileAtWorldCoords1(tilCenter.tf.position.x + num3, tilCenter.tf.position.y + num3, bAllowDocked, true);
		}
		num4 = index + ship.nCols - 1;
		if (num4 >= 0 && ship.aTiles.Count > num4)
		{
			array[6] = ship.aTiles[num4];
		}
		else if (bAllowDocked)
		{
			array[6] = ship.GetTileAtWorldCoords1(tilCenter.tf.position.x - num3, tilCenter.tf.position.y - num3, bAllowDocked, true);
		}
		num4 = index + ship.nCols + 1;
		if (num4 >= 0 && ship.aTiles.Count > num4)
		{
			array[7] = ship.aTiles[num4];
		}
		else if (bAllowDocked)
		{
			array[7] = ship.GetTileAtWorldCoords1(tilCenter.tf.position.x + num3, tilCenter.tf.position.y - num3, bAllowDocked, true);
		}
		return array;
	}

	public static int UnitsToGrid(float fUnits)
	{
		return Mathf.RoundToInt(fUnits);
	}

	public static float GridToUnits(int nGrid)
	{
		return 1f * (float)nGrid;
	}

	public static float GridAlign(float fCoord)
	{
		return TileUtils.GridToUnits(TileUtils.UnitsToGrid(fCoord));
	}

	public static void ToggleShipTileVisibility(bool bShow, List<Tile> tiles, bool clearColors = false)
	{
		if (tiles == null)
		{
			return;
		}
		TileUtils.bShowTiles = bShow;
		Dictionary<Vector2, Tile> dictionary = new Dictionary<Vector2, Tile>();
		foreach (Tile tile in tiles)
		{
			if (clearColors)
			{
				tile.SetColor(Tile.clrDefault);
			}
			tile.ShowTileWithFullAlphaColor(tile.bPassable);
			Tile tile2;
			if (dictionary.TryGetValue(tile.transform.position, out tile2))
			{
				if (tile.coProps != null && tile.coProps.ship.strRegID == CrewSim.coPlayer.Company.strRegID)
				{
					tile2.ToggleVis();
					tile.ToggleVis();
					dictionary[tile.transform.position] = tile;
				}
			}
			else
			{
				dictionary[tile.transform.position] = tile;
				tile.ToggleVis();
			}
		}
	}

	public static bool IsExposedToSpace(CondOwner coTarget)
	{
		if (coTarget == null || coTarget.ship == null)
		{
			return false;
		}
		Tile tileAtWorldCoords = coTarget.ship.GetTileAtWorldCoords1(coTarget.tf.position.x, coTarget.tf.position.y, true, true);
		Tile[] surroundingTiles = TileUtils.GetSurroundingTiles(tileAtWorldCoords, false, false);
		for (int i = 0; i < surroundingTiles.Length; i++)
		{
			if (!(surroundingTiles[i] == null) && !(surroundingTiles[i].coProps == null))
			{
				if (!TileUtils.CTShipTile.Triggered(surroundingTiles[i].coProps, null, true) || surroundingTiles[i].coProps.HasCond("IsEVATile"))
				{
					return true;
				}
				List<CondOwner> list = new List<CondOwner>();
				coTarget.ship.GetCOsAtWorldCoords1(surroundingTiles[i].tf.position, null, true, true, list);
				if (list.Any((CondOwner t) => t != null && t.HasCond("IsDockSys") && t.HasCond("IsOpen")))
				{
					return true;
				}
			}
		}
		return false;
	}

	public static void DestroyAllEVACOs(Ship ship)
	{
		if (ship.aTiles == null)
		{
			return;
		}
		CondTrigger condTrigger = DataHandler.GetCondTrigger("TIsNotInstalled");
		List<CondOwner> list = new List<CondOwner>();
		foreach (Tile tile in ship.aTiles)
		{
			if (!(tile == null) && !(tile.coProps == null) && !TileUtils.CTShipTile.Triggered(tile.coProps, null, true) && !tile.coProps.HasCond("IsFloorFlex"))
			{
				ship.GetCOsAtWorldCoords1(tile.tf.position, condTrigger, false, false, list);
				for (int i = list.Count - 1; i >= 0; i--)
				{
					if (!(list[i] == null))
					{
						list[i].FallAway();
					}
				}
			}
		}
	}

	public static bool IsTileAboveAirlock(Tile tile, Tuple<Vector2, Vector2> bounds = null)
	{
		if (bounds == null && tile != null && tile.coProps != null && tile.coProps.ship != null)
		{
			bounds = TileUtils.GetAirlockBounds(tile.coProps.ship);
		}
		if (bounds == null)
		{
			return false;
		}
		Vector2 item = bounds.Item1;
		Vector2 item2 = bounds.Item2;
		return tile.tf.position.x <= item2.x && tile.tf.position.x >= item.x && tile.tf.position.y <= item2.y && tile.tf.position.y >= item.y;
	}

	public static Tuple<Vector2, Vector2> GetAirlockBounds(Ship ship)
	{
		Vector2 item = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
		Vector2 item2 = new Vector2(float.NegativeInfinity, float.NegativeInfinity);
		if (ship == null || ship.aDocksys == null)
		{
			return new Tuple<Vector2, Vector2>(item2, item);
		}
		foreach (CondOwner condOwner in ship.aDocksys)
		{
			Vector2 pos = condOwner.GetPos("DockA", false);
			Vector2 pos2 = condOwner.GetPos("DockB", false);
			Vector2 vector = pos2 - pos;
			float num = vector.magnitude / 2f;
			if (vector.y > 0.5f)
			{
				item.y = pos.y + num;
			}
			else if (vector.y < -0.5f)
			{
				item2.y = pos.y - num;
			}
			else if (vector.x > 0.5f)
			{
				item.x = pos.x + num;
			}
			else if (vector.x < -0.5f)
			{
				item2.x = pos.x - num;
			}
		}
		return new Tuple<Vector2, Vector2>(item2, item);
	}

	public static CondTrigger CTShipTile
	{
		get
		{
			if (TileUtils._ctShipTile == null)
			{
				TileUtils._ctShipTile = DataHandler.GetCondTrigger("TIsShipTile");
			}
			return TileUtils._ctShipTile;
		}
	}

	public static CondTrigger CTShipTileOrSub
	{
		get
		{
			if (TileUtils._ctShipTileOrSub == null)
			{
				TileUtils._ctShipTileOrSub = DataHandler.GetCondTrigger("TIsShipTileOrSub");
			}
			return TileUtils._ctShipTileOrSub;
		}
	}

	public const int nPPUCoeff = 1;

	public const int nPPGrid = 16;

	private const int SIDE_TOPLEFT = 0;

	private const int SIDE_TOP = 1;

	private const int SIDE_TOPRIGHT = 2;

	private const int SIDE_LEFT = 3;

	private const int SIDE_RIGHT = 4;

	private const int SIDE_BOTTOMLEFT = 5;

	private const int SIDE_BOTTOM = 6;

	private const int SIDE_BOTTOMRIGHT = 7;

	public const int LAYER_TILE_HELPERS = 8;

	public static bool bShowTiles;

	private static CondTrigger _ctShipTile;

	private static CondTrigger _ctShipTileOrSub;

	public static List<Tile> aSelPartTiles;

	public static GameObject goSelPartTiles;

	public static GameObject goPartTiles;

	private static List<GameObject> aPowerInputGridSprites;

	private static GameObject goUseGridSprite;

	private static GameObject goPowerOutputGridSprite;

	private static GameObject goReactorGridSprite;

	private const float OVERLAY_HEIGHT = -8f;
}
