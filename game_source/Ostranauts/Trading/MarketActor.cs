using System;
using System.Collections.Generic;
using Ostranauts.ShipGUIs.Trade;
using UnityEngine;

namespace Ostranauts.Trading
{
	public class MarketActor : MonoBehaviour
	{
		protected virtual bool AllowedToSpawnItems
		{
			get
			{
				return false;
			}
		}

		public string GetMarketConfig()
		{
			return this._strMarketConfig;
		}

		protected virtual void Awake()
		{
			this.COSelf = base.GetComponent<CondOwner>();
		}

		protected void Update()
		{
			if (this.COSelf == null || this.COSelf.ship == null || this.bInit)
			{
				return;
			}
			this.bInit = true;
			Dictionary<string, string> dictionary = null;
			if (this.COSelf.mapGUIPropMaps.TryGetValue("Trader", out dictionary))
			{
				if (dictionary.TryGetValue("strMarketConfig", out this._strMarketConfig))
				{
					MarketManager.RegisterTrader(this.COSelf.ship.strRegID, this);
				}
				this.SyncInventoryToMarket();
				return;
			}
			Debug.LogWarning("#Market# MarketActor without Market Config? " + base.gameObject.name);
		}

		public virtual void ReportTransaction(CondOwner co, int change)
		{
			if (string.IsNullOrEmpty(this._strMarketConfig))
			{
				return;
			}
			if (MarketManager.ShowDebugLogs)
			{
				Debug.Log("#Market# Transaction in MarketActor: " + this.COSelf.strName);
			}
			MarketManager.ReportTransaction(this.COSelf.ship.strRegID, co.strName, change, this.COSelf.strName);
		}

		private void OnDestroy()
		{
			if (this.COSelf.ship == null)
			{
				return;
			}
			MarketManager.UnregisterTrader(this.COSelf.ship.strRegID, this);
		}

		public string GetCOId()
		{
			return (!(this.COSelf != null)) ? "missing_co" : this.COSelf.strID;
		}

		public static string GetMarketConfig(CondOwner actorCO)
		{
			string empty = string.Empty;
			Dictionary<string, string> dictionary = null;
			if (actorCO != null && actorCO.mapGUIPropMaps != null && actorCO.mapGUIPropMaps.TryGetValue("Trader", out dictionary) && dictionary != null)
			{
				dictionary.TryGetValue("strMarketConfig", out empty);
			}
			return empty;
		}

		public void SyncInventoryToMarket()
		{
			if (!this.AllowedToSpawnItems || CrewSim.bShipEdit || this._strMarketConfig == null || this.COSelf == null || this.COSelf.ship == null)
			{
				return;
			}
			List<CondOwner> inventory = this.GetInventory();
			Dictionary<string, int> dictionary = MarketManager.SyncTraderInventory(this.COSelf.ship.strRegID, this, inventory);
			if (MarketManager.ShowDebugLogs)
			{
				Debug.Log(string.Concat(new object[]
				{
					"#Market# Syncing Inventory: ",
					dictionary.Count,
					" tracked items on ",
					this.COSelf.strName
				}));
			}
			foreach (KeyValuePair<string, int> keyValuePair in dictionary)
			{
				if (keyValuePair.Value != 0)
				{
					if (MarketManager.ShowDebugLogs)
					{
						Debug.Log(string.Concat(new object[]
						{
							"#Market# Changed Trader inventory ",
							keyValuePair.Key,
							" by ",
							keyValuePair.Value
						}));
					}
					this.MarketUpdate(keyValuePair.Key, keyValuePair.Value);
				}
			}
		}

		protected void RemoveCurrentItems(string itemToRemove = null, int count = 0)
		{
			int num = (count == 0) ? 10000 : count;
			List<CondOwner> inventory = this.GetInventory();
			foreach (CondOwner condOwner in inventory)
			{
				if (condOwner.slotNow == null)
				{
					if (itemToRemove != null)
					{
						if (!(condOwner.strName != itemToRemove))
						{
							if (MarketManager.ShowDebugLogs)
							{
								Debug.Log("#Market# Destroyed item " + condOwner.strName);
							}
							condOwner.RemoveFromCurrentHome(false);
							condOwner.Destroy();
							num++;
							if (num >= 0)
							{
								break;
							}
						}
					}
					else
					{
						condOwner.RemoveFromCurrentHome(false);
						condOwner.Destroy();
					}
				}
			}
		}

		protected List<CondOwner> GetInventory()
		{
			CondTrigger condTrigger = DataHandler.GetCondTrigger("TIsBarter");
			List<CondOwner> cosSafe = this.COSelf.GetCOsSafe(true, condTrigger);
			List<Ship> list = new List<Ship>
			{
				this.COSelf.ship
			};
			list.AddRange(this.COSelf.ship.GetAllDockedShips());
			this.mapZonesUs.Clear();
			foreach (Ship ship in list)
			{
				List<JsonZone> zones = ship.GetZones("IsZoneBarter", this.COSelf, false, false);
				foreach (JsonZone key in zones)
				{
					this.mapZonesUs[key] = ship;
				}
			}
			List<CondOwner> list2 = new List<CondOwner>();
			foreach (KeyValuePair<JsonZone, Ship> keyValuePair in this.mapZonesUs)
			{
				list2 = keyValuePair.Value.GetCOsInZone(keyValuePair.Key.strName, condTrigger, true);
				foreach (CondOwner item in list2)
				{
					if (cosSafe.IndexOf(item) < 0)
					{
						cosSafe.Add(item);
					}
				}
				List<CondOwner> cosInZone = keyValuePair.Value.GetCOsInZone(keyValuePair.Key.strName, GUITradeBase.ctInfinite, true);
				foreach (CondOwner condOwner in cosInZone)
				{
					list2 = condOwner.GetCOs(true, null);
					if (list2 != null)
					{
						foreach (CondOwner condOwner2 in list2)
						{
							if (!(condOwner2.objCOParent != condOwner))
							{
								cosSafe.Add(condOwner2);
							}
						}
					}
				}
			}
			return cosSafe;
		}

		protected int AddNewItems(string cosToSpawn)
		{
			Loot loot = DataHandler.GetLoot(cosToSpawn);
			List<CondOwner> coloot = loot.GetCOLoot(null, false, null);
			return this.AddNewItems(coloot);
		}

		protected int AddNewItems(List<CondOwner> aCOs)
		{
			if (aCOs == null)
			{
				return 0;
			}
			CondTrigger condTrigger = DataHandler.GetCondTrigger("TIsLootSpawnOK");
			int num = 0;
			foreach (CondOwner condOwner in aCOs)
			{
				if (!(condOwner == null))
				{
					bool flag = false;
					Item component = condOwner.GetComponent<Item>();
					if (component != null)
					{
						component.fLastRotation = 0f;
					}
					List<CondOwner> aCOs2 = new List<CondOwner>
					{
						condOwner
					};
					List<CondOwner> list = new List<CondOwner>();
					foreach (KeyValuePair<JsonZone, Ship> keyValuePair in this.mapZonesUs)
					{
						List<CondOwner> cosInZone = keyValuePair.Value.GetCOsInZone(keyValuePair.Key, condTrigger, true, true);
						list = TileUtils.DropCOsNearby(aCOs2, keyValuePair.Value, keyValuePair.Key, cosInZone, condTrigger, true, false);
						if (list.Count == 0)
						{
							flag = true;
							break;
						}
						aCOs2 = list;
					}
					if (!flag)
					{
						list.Clear();
						CondOwner condOwner2 = this.COSelf.AddCO(condOwner, false, true, true);
						if (condOwner2 != null)
						{
							list.Add(condOwner2);
						}
						else
						{
							flag = true;
						}
					}
					if (!flag)
					{
						num++;
						condOwner.Destroy();
					}
				}
			}
			return num;
		}

		protected void MarketUpdate(string marketItem, int change)
		{
			if (change < 0)
			{
				this.RemoveCurrentItems(marketItem, change);
			}
			else
			{
				List<CondOwner> list = new List<CondOwner>();
				for (int i = 0; i < change; i++)
				{
					CondOwner condOwner = DataHandler.GetCondOwner(marketItem, null, null, false, null, null, null, null);
					if (condOwner != null)
					{
						list.Add(condOwner);
					}
				}
				if (list.Count > 0)
				{
					change = this.AddNewItems(list);
				}
			}
		}

		protected CondOwner COSelf;

		private Dictionary<JsonZone, Ship> mapZonesUs = new Dictionary<JsonZone, Ship>();

		protected bool bInit;

		protected string _strMarketConfig;
	}
}
