using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Ostranauts.ShipGUIs.Trade;
using UnityEngine;

namespace Ostranauts.Ships
{
	public class BarterZoneShip : Ship, IShip, IAsyncLoadable
	{
		public BarterZoneShip(GameObject go) : base(go)
		{
		}

		public new JsonShip json { get; set; }

		public bool FullyLoaded
		{
			get
			{
				return this._fullyLoaded;
			}
		}

		public IEnumerator Init(int iteratorCounter)
		{
			GameObject goPart = null;
			CondOwner co = null;
			CondOwner coSub = null;
			List<JsonItem> aSubItems = new List<JsonItem>();
			List<JsonItem> aItemsPlusCrew = new List<JsonItem>();
			aItemsPlusCrew.AddRange(this.json.aItems);
			this.gameObject.SetActive(true);
			for (int i = 0; i < aItemsPlusCrew.Count; i++)
			{
				if (aItemsPlusCrew[i].strParentID != null || aItemsPlusCrew[i].strSlotParentID != null)
				{
					aSubItems.Add(aItemsPlusCrew[i]);
				}
				else
				{
					string strIDTemp = aItemsPlusCrew[i].strID;
					goPart = base.CreatePart(aItemsPlusCrew[i], strIDTemp, true);
					if (!(goPart == null))
					{
						goPart.layer = LayerMask.NameToLayer(AsyncShipLoader.ASYNCLAYERNAME);
						IEnumerator enumerator = goPart.transform.GetEnumerator();
						try
						{
							while (enumerator.MoveNext())
							{
								object obj = enumerator.Current;
								Transform transform = (Transform)obj;
								transform.gameObject.layer = LayerMask.NameToLayer(AsyncShipLoader.ASYNCLAYERNAME);
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
						co = goPart.GetComponent<CondOwner>();
						this.AddCO(co, true);
						if (i % iteratorCounter == 0)
						{
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
						break;
					}
					nIndex = aSubItems.Count - 1;
					nProcessed = 0;
				}
				JsonItem jsonItem = aSubItems[nIndex];
				nIndex--;
				string text = jsonItem.strParentID;
				if (text == null)
				{
					text = jsonItem.strSlotParentID;
				}
				if (this.mapICOs.ContainsKey(text))
				{
					co = this.mapICOs[text];
					string strID = jsonItem.strID;
					goPart = base.CreatePart(jsonItem, strID, false);
					if (!(goPart == null))
					{
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
			base.SetZoneData(this.json.aZones);
			if (this.mapICOs != null)
			{
				foreach (CondOwner condOwner in this.mapICOs.Values.ToArray<CondOwner>())
				{
					condOwner.PostGameLoad(Ship.Loaded.Shallow);
					condOwner.gameObject.layer = LayerMask.NameToLayer(AsyncShipLoader.ASYNCLAYERNAME);
					IEnumerator enumerator2 = condOwner.transform.GetEnumerator();
					try
					{
						while (enumerator2.MoveNext())
						{
							object obj2 = enumerator2.Current;
							Transform transform2 = (Transform)obj2;
							transform2.gameObject.layer = LayerMask.NameToLayer(AsyncShipLoader.ASYNCLAYERNAME);
						}
					}
					finally
					{
						IDisposable disposable2;
						if ((disposable2 = (enumerator2 as IDisposable)) != null)
						{
							disposable2.Dispose();
						}
					}
				}
				this.AddToLocalInstance();
			}
			base.MoveShip(AsyncShipLoader.SPAWNOFFSET);
			for (int k = 0; k < this.nCols; k++)
			{
				for (int l = 0; l < this.nRows; l++)
				{
					int num = l * this.nCols + k;
					if (num >= this.aTiles.Count)
					{
						break;
					}
					bool flag2 = false;
					if (l == 0 || l == this.nRows - 1)
					{
						flag2 = true;
					}
					else if (k == 0 || k == this.nCols - 1)
					{
						flag2 = true;
					}
					if (flag2)
					{
						this.aTiles[num].coProps.AddCondAmount("IsObstruction", 1.0, 0.0, 0f);
					}
				}
			}
			this._fullyLoaded = true;
			yield break;
		}

		public void SaveChangedCOs(ref Ship originalShip)
		{
			List<JsonItem> list = new List<JsonItem>();
			using (Dictionary<string, CondOwner>.Enumerator enumerator = this.mapICOs.GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					KeyValuePair<string, CondOwner> kvp = enumerator.Current;
					if (originalShip.json.aItems.FirstOrDefault((JsonItem x) => x.strID == kvp.Key) == null)
					{
						kvp.Value.tf.position -= AsyncShipLoader.SPAWNOFFSET;
						kvp.Value.mapGUIPropMaps.Add(GUITradeBase.ASYNCIDENTIFIER, new Dictionary<string, string>
						{
							{
								GUITradeBase.ASYNCIDENTIFIER,
								GUITradeBase.ASYNCIDENTIFIER
							}
						});
						JsonItem jsonItem = base.GetJsonItem(kvp.Value);
						list.Add(jsonItem);
						JsonCondOwnerSave jsonsave = kvp.Value.GetJSONSave();
						if (jsonsave != null)
						{
							DataHandler.dictCOSaves[jsonsave.strID] = jsonsave;
						}
						kvp.Value.tf.position += AsyncShipLoader.SPAWNOFFSET;
					}
				}
			}
			List<JsonItem> list2 = this.json.aItems.ToList<JsonItem>();
			list2.AddRange(list);
			originalShip.json.aItems = list2.ToArray();
			this.json.aItems = originalShip.json.aItems;
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

		public new void AddCO(CondOwner objICO, bool bTiles)
		{
			if (objICO == null)
			{
				return;
			}
			objICO.gameObject.layer = LayerMask.NameToLayer(AsyncShipLoader.ASYNCLAYERNAME);
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
			this.mapICOs[objICO.strID] = objICO;
			objICO.ship = this;
			if (objICO.objCOParent == null)
			{
				objICO.tf.SetParent(this.gameObject.transform, false);
			}
		}

		public new void RemoveCO(CondOwner objCO, bool bForce = false)
		{
			if (objCO == null)
			{
				if (objCO != null && objCO.strID != null)
				{
					this.mapICOs.Remove(objCO.strID);
				}
				this.aDocksys.Remove(objCO);
				return;
			}
			objCO.gameObject.layer = LayerMask.NameToLayer(AsyncShipLoader.DEFAULTLAYERNAME);
			if (objCO.objCOParent != null && objCO.coStackHead == null)
			{
				if (objCO.slotNow != null)
				{
					Slots compSlots = objCO.objCOParent.compSlots;
					if (compSlots != null)
					{
						compSlots.UnSlotItem(objCO, bForce);
						objCO.ValidateParent();
						CondOwner.CheckTrue(objCO.objCOParent == null, "Unslotted but still have parent...");
					}
				}
			}
			else if (objCO.coStackHead != null)
			{
				CondOwner coStackHead = objCO.coStackHead;
				coStackHead.aStack.Remove(objCO);
				objCO.coStackHead = null;
				Item component = objCO.GetComponent<Item>();
				component.fLastRotation = coStackHead.tf.rotation.eulerAngles.z;
				objCO.tf.position = new Vector3(coStackHead.tf.position.x, coStackHead.tf.position.y, coStackHead.tf.position.z);
				coStackHead.UpdateAppearance();
				objCO.UpdateAppearance();
			}
			else
			{
				base.UpdateTiles(objCO, true, false);
			}
			Tile tileAtWorldCoords = base.GetTileAtWorldCoords1(objCO.tf.position.x, objCO.tf.position.y, true, true);
			List<CondOwner> list = new List<CondOwner>();
			list.AddRange(objCO.aStack);
			if (objCO.objContainer != null)
			{
				list.AddRange(objCO.objContainer.GetCOs(true, null));
			}
			Slots compSlots2 = objCO.compSlots;
			if (compSlots2 != null)
			{
				list.AddRange(compSlots2.GetCOs(null, false, null));
			}
			list.AddRange(objCO.aLot);
			base.RemoveInternalLot(tileAtWorldCoords, list);
			foreach (CondOwner condOwner in list)
			{
				this.RemoveFromLocalInstance(condOwner.strID);
			}
			base.RemoveInternal(tileAtWorldCoords, objCO);
			this.RemoveFromLocalInstance(objCO.strID);
			objCO.ValidateParent();
		}

		private void RemoveFromLocalInstance(string coID)
		{
			foreach (JsonItem jsonItem in this.json.aItems)
			{
				if (!(jsonItem.strID != coID))
				{
					List<JsonItem> list = this.json.aItems.ToList<JsonItem>();
					list.Remove(jsonItem);
					this.json.aItems = list.ToArray();
					return;
				}
			}
		}

		private void AddToLocalInstance()
		{
			if (this.mapICOs == null)
			{
				return;
			}
			List<CondOwner> list = this.mapICOs.Values.ToList<CondOwner>();
			foreach (CondOwner condOwner in list)
			{
				bool flag = false;
				foreach (JsonItem jsonItem in this.json.aItems)
				{
					if (jsonItem.strID == condOwner.strID)
					{
						flag = true;
						break;
					}
				}
				if (!flag)
				{
					JsonItem jsonItem2 = base.GetJsonItem(condOwner);
					List<JsonItem> list2 = this.json.aItems.ToList<JsonItem>();
					list2.Add(jsonItem2);
					this.json.aItems = list2.ToArray();
				}
			}
		}

		private bool _fullyLoaded;
	}
}
