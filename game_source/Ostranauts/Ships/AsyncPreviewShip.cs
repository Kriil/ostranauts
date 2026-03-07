using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Ostranauts.Ships
{
	public class AsyncPreviewShip : Ship, IShip, IAsyncLoadable
	{
		public AsyncPreviewShip(GameObject go) : base(go)
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
					if (aItemsPlusCrew[i].ForceLoad())
					{
						aSubItems.Add(aItemsPlusCrew[i]);
					}
				}
				else
				{
					string strIDTemp = aItemsPlusCrew[i].strID;
					goPart = base.CreatePart(aItemsPlusCrew[i], strIDTemp, true);
					if (!(goPart == null))
					{
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
						Debug.Log("ERROR: " + aSubItems.Count + " unprocessed sub items on ship ");
						Debug.Break();
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
			this._fullyLoaded = true;
			yield break;
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
			Ship shipByRegID = CrewSim.system.GetShipByRegID(this.strRegID);
			if (shipByRegID != null && shipByRegID.LoadState > Ship.Loaded.Shallow)
			{
				return;
			}
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
	}
}
