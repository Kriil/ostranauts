using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Ostranauts.Core.Models;
using Ostranauts.Ships.Rooms;
using Ostranauts.UI.ShipRating;
using UnityEngine;

namespace Ostranauts.Ships
{
	public class AsyncRatingShip : Ship, IShip, IAsyncLoadable
	{
		public AsyncRatingShip(GameObject go) : base(go)
		{
		}

		public new JsonShip json
		{
			get
			{
				return this.json;
			}
			set
			{
				this.json = value;
			}
		}

		public bool FullyLoaded
		{
			get
			{
				return this._fullyLoaded;
			}
		}

		public IEnumerator Init(int iteratorCounter)
		{
			return this.Init(iteratorCounter, delegate(float x)
			{
			}, null);
		}

		public IEnumerator Init(int iteratorCounter, Action<float> progressCallback, GameObject text = null)
		{
			GameObject goPart = null;
			CondOwner co = null;
			CondOwner coSub = null;
			List<JsonItem> aSubItems = new List<JsonItem>();
			List<JsonItem> aItemsPlusCrew = new List<JsonItem>();
			this.fRCSCount = this.json.nRCSCount;
			this.fShallowMass = this.json.fShallowMass;
			aItemsPlusCrew.AddRange(this.json.aItems);
			this.gameObject.SetActive(true);
			for (int i = 0; i < aItemsPlusCrew.Count; i++)
			{
				if (aItemsPlusCrew[i].strParentID != null || aItemsPlusCrew[i].strSlotParentID != null)
				{
					if (aItemsPlusCrew[i].ForceLoad())
					{
						aSubItems.Add(aItemsPlusCrew[i]);
					}
				}
				else
				{
					string strIDTemp = DataHandler.GetNextID();
					goPart = base.CreatePart(aItemsPlusCrew[i], strIDTemp, true);
					if (!(goPart == null))
					{
						co = goPart.GetComponent<CondOwner>();
						this.AddCO(co, true);
						if (i % iteratorCounter == 0)
						{
							progressCallback((float)i / (float)aItemsPlusCrew.Count);
							yield return new WaitForSecondsRealtime(0.2f);
						}
					}
				}
			}
			int nProcessed = -1;
			int nIndex = -1;
			while (aSubItems.Count > 0)
			{
				if (nIndex < 0)
				{
					if (nProcessed == 0)
					{
						Debug.Log("ERROR: " + aSubItems.Count + " unprocessed sub items on ship ");
						break;
					}
					nIndex = aSubItems.Count - 1;
					nProcessed = 0;
				}
				JsonItem jsonItem = aSubItems[nIndex];
				nIndex--;
				string text2 = jsonItem.strParentID;
				if (text2 == null)
				{
					text2 = jsonItem.strSlotParentID;
				}
				if (this.mapICOs.ContainsKey(text2))
				{
					co = this.mapICOs[text2];
					string nextID = DataHandler.GetNextID();
					goPart = base.CreatePart(jsonItem, nextID, false);
					if (!(goPart == null))
					{
						goPart.layer = 9;
						coSub = goPart.GetComponent<CondOwner>();
						coSub.tf.localPosition = new Vector3(co.tf.position.x, co.tf.position.y, Container.fZSubOffset);
						bool flag = true;
						if (co.objContainer != null)
						{
							if (!co.objContainer.Contains(coSub))
							{
								bool bAllowStacking = co.objContainer.bAllowStacking;
								co.objContainer.bAllowStacking = false;
								co.objContainer.AddCOSimple(coSub, coSub.pairInventoryXY);
								co.objContainer.bAllowStacking = bAllowStacking;
							}
						}
						else if (jsonItem.strSlotParentID != null && coSub.jCOS != null)
						{
							if (co.compSlots == null)
							{
								Debug.LogError(string.Concat(new string[]
								{
									"ERROR: Attempting to slot ",
									coSub.strCODef,
									" - ",
									coSub.strID,
									" into parent with no slot: ",
									co.strCODef,
									" - ",
									co.strID
								}));
								flag = false;
							}
							else if (!co.compSlots.SlotItem(coSub.jCOS.strSlotName, coSub, true))
							{
								continue;
							}
						}
						if (flag)
						{
							this.mapICOs[coSub.strID] = coSub;
						}
						aSubItems.Remove(jsonItem);
						nProcessed++;
					}
				}
			}
			base.MoveShip(AsyncShipLoader.SPAWNOFFSET);
			if (text != null)
			{
				this._floatingTextPrefab = text;
				this.BuildPointOfInterestDict();
				this.BuildRoomDict();
				this.SpawnLabels();
			}
			this._fullyLoaded = true;
			yield break;
		}

		private void BuildRoomDict()
		{
			if (this.json == null || this.json.aRooms == null)
			{
				return;
			}
			int num = 0;
			List<Color> list = new List<Color>
			{
				Color.red,
				Color.blue,
				Color.green,
				Color.magenta,
				Color.yellow,
				Color.white
			};
			foreach (JsonRoom jsonRoom in this.json.aRooms)
			{
				if (!(jsonRoom.roomSpec == string.Empty) && !RoomSpec.IsBlankSpec(jsonRoom.roomSpec))
				{
					List<Tile> list2 = (from tile in this.aTiles
					join id in jsonRoom.aTiles on tile.Index equals id
					select tile).ToList<Tile>();
					if (list2.Count != 0 && !(list2.FirstOrDefault<Tile>() == null))
					{
						this._roomDict.Add(jsonRoom, list2);
						foreach (Tile tile2 in list2)
						{
							tile2.SetColor(list[num]);
						}
						num = ((num != list.Count - 1) ? (num + 1) : 0);
					}
				}
			}
		}

		private void BuildPointOfInterestDict()
		{
			CondTrigger condTrigger = DataHandler.GetCondTrigger("TIsPOIShipRating");
			if (condTrigger == null)
			{
				return;
			}
			foreach (KeyValuePair<string, CondOwner> keyValuePair in this.mapICOs)
			{
				if (!(keyValuePair.Value == null) && condTrigger.Triggered(keyValuePair.Value, null, true))
				{
					bool flag = false;
					foreach (CondOwner condOwner in this._poiDict.Keys)
					{
						if (!(condOwner == null))
						{
							float num = Vector3.Distance(condOwner.tf.position, keyValuePair.Value.tf.position);
							if ((condOwner.strName == keyValuePair.Value.strName && num < 2f) || num < 1f)
							{
								flag = true;
								break;
							}
						}
					}
					if (!flag)
					{
						this._poiDict.Add(keyValuePair.Value, keyValuePair.Value.gameObject);
					}
				}
			}
		}

		public void ShowRoomTiles(bool show)
		{
			foreach (KeyValuePair<JsonRoom, List<Tile>> keyValuePair in this._roomDict)
			{
				TileUtils.ToggleShipTileVisibility(show, keyValuePair.Value, false);
			}
		}

		private void SpawnLabels()
		{
			int num = this.nRows / 2;
			int num2 = this.nCols / 2;
			Vector2 center = new Vector2(this.vShipPos.x + (float)num2, this.vShipPos.y - (float)num);
			List<Vector2> pointsOnEllipsis = this.GetPointsOnEllipsis(center, num2, num);
			float num3 = (float)((num >= num2) ? num2 : num);
			num3 = num3 * 1.1f + 2f;
			Transform transform = null;
			foreach (KeyValuePair<CondOwner, GameObject> keyValuePair in this._poiDict)
			{
				if (pointsOnEllipsis.Count == 0)
				{
					break;
				}
				if (transform == null)
				{
					transform = keyValuePair.Value.transform.parent;
				}
				Vector2 closestPoint = this.GetClosestPoint(pointsOnEllipsis, keyValuePair.Value.transform.position, num3, false);
				if (!(closestPoint == Vector2.zero))
				{
					FloatingPanel item = this.SpawnFloatingObject(transform, closestPoint, keyValuePair.Key.strNameFriendly, keyValuePair.Key.tf);
					this.poiLabels.Add(item);
					pointsOnEllipsis.Remove(closestPoint);
				}
			}
			pointsOnEllipsis = this.GetPointsOnEllipsis(center, num2, num);
			foreach (KeyValuePair<JsonRoom, List<Tile>> keyValuePair2 in this._roomDict)
			{
				if (pointsOnEllipsis.Count == 0)
				{
					break;
				}
				if (keyValuePair2.Value != null)
				{
					if (transform == null)
					{
						transform = keyValuePair2.Value.FirstOrDefault<Tile>().tf.parent;
					}
					RoomSpec roomDef = DataHandler.GetRoomDef(keyValuePair2.Key.roomSpec);
					Transform tf = TileUtils.GetTileCloseToCenter(keyValuePair2.Value).tf;
					Vector2 closestPoint2 = this.GetClosestPoint(pointsOnEllipsis, tf.position, num3, true);
					if (!(closestPoint2 == Vector2.zero))
					{
						FloatingPanel item2 = this.SpawnFloatingObject(transform, closestPoint2, roomDef.strNameFriendly, tf);
						this.roomLabels.Add(item2);
						pointsOnEllipsis.Remove(closestPoint2);
					}
				}
			}
		}

		private FloatingPanel SpawnFloatingObject(Transform parent, Vector2 closestPoint, string friendlyName, Transform targetTf)
		{
			FloatingPanel component = UnityEngine.Object.Instantiate<GameObject>(this._floatingTextPrefab, parent).GetComponent<FloatingPanel>();
			component.transform.position = new Vector3(closestPoint.x, closestPoint.y, component.transform.position.z);
			if (this.nRows > this.nCols)
			{
				component.transform.Rotate(new Vector3(0f, 0f, 90f));
			}
			component.SetData(friendlyName, targetTf);
			component.Hide();
			return component;
		}

		private Vector2 GetClosestPoint(IEnumerable<Vector2> pointList, Vector3 position, float maxDistance, bool rooms = false)
		{
			Vector2 result = Vector2.zero;
			float num = 1000f;
			foreach (Vector2 vector in pointList)
			{
				float num2 = Vector2.Distance(position, vector);
				if (num2 < maxDistance && num2 < num && !this.IsIntersecting(position, vector, rooms))
				{
					result = vector;
					num = num2;
				}
			}
			return result;
		}

		private bool IsIntersecting(Vector3 position, Vector3 targetPos, bool rooms = false)
		{
			List<FloatingPanel> list = (!rooms) ? this.poiLabels : this.roomLabels;
			foreach (FloatingPanel floatingPanel in list)
			{
				if (floatingPanel.IsIntersecting(targetPos, position))
				{
					return true;
				}
			}
			return false;
		}

		private List<Vector2> GetPointsOnEllipsis(Vector2 center, int xAxis, int yAxis)
		{
			xAxis += 2;
			yAxis += 2;
			int num;
			if (xAxis > yAxis)
			{
				xAxis = (int)((float)xAxis * 1.2f);
				num = yAxis;
			}
			else
			{
				yAxis = (int)((float)yAxis * 1.2f);
				num = xAxis;
			}
			List<Vector2> list = new List<Vector2>();
			for (int i = 0; i < num; i++)
			{
				double num2 = 6.283185307179586 * (double)i / (double)num;
				double num3 = (double)center.x + (double)xAxis * Math.Cos(num2);
				double num4 = (double)center.y + (double)yAxis * Math.Sin(num2);
				list.Add(new Vector2((float)num3, (float)num4));
			}
			return this.SortClockWiseAroundCenter(list, center);
		}

		private List<Vector2> SortClockWiseAroundCenter(List<Vector2> points, Vector2 center)
		{
			List<Tuple<Vector2, float>> list = new List<Tuple<Vector2, float>>();
			foreach (Vector2 vector in points)
			{
				Tuple<Vector2, float> tuple = new Tuple<Vector2, float>(vector, 0f);
				Vector2 vector2 = vector - center;
				tuple.Item2 = Mathf.Atan2(vector2.x, vector2.y) * 57.29578f;
				tuple.Item2 = (tuple.Item2 + 360f) % 360f;
				list.Add(tuple);
			}
			return (from x in list
			orderby x.Item2
			select x.Item1).ToList<Vector2>();
		}

		public void SaveChangedCOs(ref Ship original)
		{
		}

		public new void AddCO(CondOwner objICO, bool bTiles)
		{
			if (objICO == null)
			{
				return;
			}
			if (bTiles)
			{
				base.UpdateTiles(objICO, false, false);
			}
			List<CondOwner> list = new List<CondOwner>();
			list.AddRange(objICO.aStack);
			if (objICO.objContainer != null)
			{
				list.AddRange(objICO.objContainer.GetCOs(true, null));
			}
			Slots compSlots = objICO.compSlots;
			if (compSlots != null)
			{
				list.AddRange(compSlots.GetCOs(null, false, null));
			}
			if (objICO.objCOParent == null)
			{
				objICO.Visible = this.gameObject.activeInHierarchy;
			}
			foreach (CondOwner condOwner in list)
			{
				this.mapICOs[condOwner.strID] = condOwner;
				condOwner.ship = this;
			}
			if (objICO.HasCond("IsLootSpawner"))
			{
				objICO.Visible = false;
			}
			this.mapICOs[objICO.strID] = objICO;
			objICO.ship = this;
			if (objICO.objCOParent == null)
			{
				objICO.tf.SetParent(this.gameObject.transform, false);
			}
		}

		public new void Destroy(bool isDespawning = true)
		{
			this.json = null;
			foreach (KeyValuePair<string, CondOwner> keyValuePair in this.mapICOs)
			{
				if (DataHandler.mapCOs.ContainsKey(keyValuePair.Key))
				{
					DataHandler.mapCOs.Remove(keyValuePair.Key);
				}
			}
			UnityEngine.Object.DestroyImmediate(this.gameObject);
		}

		private bool _fullyLoaded;

		private Dictionary<CondOwner, GameObject> _poiDict = new Dictionary<CondOwner, GameObject>();

		private Dictionary<JsonRoom, List<Tile>> _roomDict = new Dictionary<JsonRoom, List<Tile>>();

		public List<FloatingPanel> poiLabels = new List<FloatingPanel>();

		public List<FloatingPanel> roomLabels = new List<FloatingPanel>();

		private GameObject _floatingTextPrefab;
	}
}
