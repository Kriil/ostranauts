using System;
using System.Collections.Generic;
using System.Linq;
using Ostranauts.Core.Models;
using UnityEngine;

namespace Ostranauts.Trading
{
	// Per-ship market state. Aggregates actor configs, stock totals, virtual
	// inventory caps, and dynamic price modifiers for one trading location.
	public class ShipMarket
	{
		// Creates a new empty market state for a ship registration id.
		public ShipMarket(string regId)
		{
			this.ShipRegId = regId;
		}

		// Rehydrates market state from a save payload.
		public ShipMarket(JsonShipMarketSave jSave)
		{
			this.LoadFromSave(jSave);
		}

		public string ShipRegId { get; private set; }

		// Category/item-level price multipliers derived from current stock pressure.
		public Dictionary<string, float> PriceModifiers
		{
			get
			{
				return this._priceModifiers;
			}
		}

		public List<MarketActorConfig> GetConfigs()
		{
			return this._marketMapping.Values.ToList<MarketActorConfig>();
		}

		public MarketActorConfig GetConfigForMarketActor(string coId)
		{
			MarketActorConfig result = null;
			if (!string.IsNullOrEmpty(coId))
			{
				this._marketMapping.TryGetValue(coId, out result);
			}
			return result;
		}

		public void AddMarketConfig(string coId, MarketActorConfig marketActorConf)
		{
			if (marketActorConf == null || string.IsNullOrEmpty(coId))
			{
				return;
			}
			this._marketMapping[coId] = marketActorConf;
		}

		public List<MarketItem> RemoveMarketConfig(string coId)
		{
			List<MarketItem> result = null;
			if (string.IsNullOrEmpty(coId) || this._marketMapping == null)
			{
				return result;
			}
			MarketActorConfig marketActorConfig;
			if (this._marketMapping.TryGetValue(coId, out marketActorConfig))
			{
				result = marketActorConfig.GetFlatStockList();
			}
			this._marketMapping.Remove(coId);
			return result;
		}

		public bool IsBlank()
		{
			return (this._stock == null || this._stock.Count <= 0) && (this._maxVirtualInventory == null || this._maxVirtualInventory.Count <= 0) && (this._productionMaps == null || this._productionMaps.Count <= 0) && (this._marketMapping == null || this._marketMapping.Count <= 0);
		}

		// Rebuilds aggregate stock/production/capacity caches from the attached
		// market actor configs, then refreshes price modifiers.
		public void BuildConfig()
		{
			this._productionMaps.Clear();
			this._stock.Clear();
			this._maxVirtualInventory.Clear();
			this._cachedTotalMass = -1.0;
			foreach (MarketActorConfig marketActorConfig in this._marketMapping.Values)
			{
				this._productionMaps.AddRange(marketActorConfig.GetProductionMaps());
				this.AddToDict(marketActorConfig.GetStockTotals(), this._stock);
				this.AddToDict(marketActorConfig.MaxVirtualInventorySize, this._maxVirtualInventory);
			}
			foreach (KeyValuePair<string, int> keyValuePair in this._maxVirtualInventory)
			{
				this.UpdatePriceModifier(keyValuePair.Key);
			}
		}

		// Recalculates the combined stock totals without rebuilding every other cache.
		private void RebuildStockDict()
		{
			if (this._marketMapping == null || this._marketMapping.Count == 0)
			{
				return;
			}
			this._cachedTotalMass = -1.0;
			this._stock.Clear();
			foreach (MarketActorConfig marketActorConfig in this._marketMapping.Values)
			{
				this.AddToDict(marketActorConfig.GetStockTotals(), this._stock);
			}
		}

		private void AddToDict(Dictionary<string, int> collection, Dictionary<string, int> targetDict)
		{
			foreach (KeyValuePair<string, int> keyValuePair in collection)
			{
				if (targetDict.ContainsKey(keyValuePair.Key))
				{
					string key;
					targetDict[key = keyValuePair.Key] = targetDict[key] + keyValuePair.Value;
				}
				else
				{
					targetDict[keyValuePair.Key] = keyValuePair.Value;
				}
			}
		}

		public int GetStockCountForCategory(DataCoCollection dCO)
		{
			return this.GetStockCountForCategory(dCO.Name);
		}

		public int GetStockCountForCategory(string categoryName)
		{
			int num;
			return (!this._stock.TryGetValue(categoryName, out num)) ? 0 : num;
		}

		public int GetMaxInventoryForCategory(string coName)
		{
			int result;
			if (this._maxVirtualInventory.TryGetValue(coName, out result))
			{
				return result;
			}
			return 0;
		}

		public List<MarketItem> RemoveStockForCategory(string categoryName, int change, string preferOwner = null)
		{
			int num = change;
			List<MarketItem> list = new List<MarketItem>();
			MarketActorConfig marketActorConfig;
			if (preferOwner != null && this._marketMapping.TryGetValue(preferOwner, out marketActorConfig))
			{
				list.AddRange(marketActorConfig.TakeOutStock(categoryName, num));
				num -= list.Count;
			}
			if (num != 0)
			{
				foreach (MarketActorConfig marketActorConfig2 in this._marketMapping.Values)
				{
					List<MarketItem> list2 = marketActorConfig2.TakeOutStock(categoryName, num);
					if (list2.Count != 0)
					{
						list.AddRange(list2);
						num -= list.Count;
					}
				}
			}
			if (num != 0)
			{
				Debug.Log("#Market# Tried to remove stock but found not enough items " + this.ShipRegId);
			}
			this.RebuildStockDict();
			this.UpdatePriceModifier(categoryName);
			return list;
		}

		public void AddStockForCategory(MarketItem marketItem, int change, string preferOwner = null)
		{
			int num = change;
			MarketActorConfig marketActorConfig;
			if (preferOwner != null && this._marketMapping.TryGetValue(preferOwner, out marketActorConfig))
			{
				num = marketActorConfig.TryAddStock(marketItem, num);
			}
			if (num != 0)
			{
				foreach (MarketActorConfig marketActorConfig2 in this._marketMapping.Values)
				{
					num = marketActorConfig2.TryAddStock(marketItem, num);
				}
			}
			if (num != 0 && MarketManager.ShowDebugLogs)
			{
				Debug.Log("#Market# Tried to add stock but found no inventory space " + this.ShipRegId);
			}
			this.RebuildStockDict();
			this.UpdatePriceModifier(marketItem.COCollectionName);
		}

		public void AddStockForCategory(string coCollectionName, List<MarketItem> marketItems, string preferOwner = null)
		{
			int num = marketItems.Count;
			MarketActorConfig marketActorConfig;
			if (preferOwner != null && this._marketMapping.TryGetValue(preferOwner, out marketActorConfig))
			{
				num = marketActorConfig.TryAddStock(coCollectionName, marketItems);
			}
			if (num != 0)
			{
				foreach (MarketActorConfig marketActorConfig2 in this._marketMapping.Values)
				{
					num = marketActorConfig2.TryAddStock(coCollectionName, marketItems);
				}
			}
			if (num != 0)
			{
				Debug.Log("#Market# Tried to add stock but found no inventory space " + this.ShipRegId);
			}
			this.RebuildStockDict();
			this.UpdatePriceModifier(coCollectionName);
		}

		private void UpdatePriceModifier(string categoryName)
		{
			float value = this.CalculatePriceModifier(categoryName, this.GetInventoryFillFraction(categoryName));
			this._priceModifiers[categoryName] = value;
		}

		private float CalculatePriceModifier(string categoryName, float fillFraction)
		{
			float num = 0f;
			foreach (Producer producer in this._productionMaps)
			{
				num += producer.GetDemandPerHourForCategory(categoryName);
			}
			num = Mathf.Clamp(num, -0.8f, 0.8f);
			float num2;
			if (num < 0f)
			{
				num2 = fillFraction;
			}
			else
			{
				num2 = 1f - fillFraction;
			}
			return 1f + num * num2;
		}

		public float PredictPriceModifierForInventoryChange(string categoryName, float fchange)
		{
			int num = Mathf.RoundToInt(fchange);
			int num2 = this.GetStockCountForCategory(categoryName) + num;
			if (num2 < 0)
			{
				num2 = 0;
			}
			int maxInventoryForCategory = this.GetMaxInventoryForCategory(categoryName);
			float fillFraction = (maxInventoryForCategory == 0) ? 0f : ((float)num2 / (float)maxInventoryForCategory);
			return this.CalculatePriceModifier(categoryName, fillFraction);
		}

		public float GetPriceModifierForItem(string coItemName)
		{
			foreach (KeyValuePair<string, float> keyValuePair in this._priceModifiers)
			{
				DataCoCollection dataCoCollection = DataHandler.GetDataCoCollection(keyValuePair.Key);
				if (dataCoCollection.IsPartOfCollection(coItemName))
				{
					return keyValuePair.Value;
				}
			}
			return 1f;
		}

		public float GetPriceModifierForCategory(string coCollectionName)
		{
			float num;
			return (!this._priceModifiers.TryGetValue(coCollectionName, out num)) ? 1f : num;
		}

		public bool RunProduction(double currentTime)
		{
			bool flag = false;
			foreach (KeyValuePair<string, MarketActorConfig> keyValuePair in this._marketMapping)
			{
				flag |= keyValuePair.Value.RunProduction(this, currentTime);
			}
			return flag;
		}

		public int GetAvailabeInventorySpaceForCategory(DataCoCollection dCO)
		{
			return this.GetAvailabeInventorySpaceForCategory(dCO.Name);
		}

		private int GetAvailabeInventorySpaceForCategory(string collectionName)
		{
			int maxInventoryForCategory = this.GetMaxInventoryForCategory(collectionName);
			int stockCountForCategory = this.GetStockCountForCategory(collectionName);
			return maxInventoryForCategory - stockCountForCategory;
		}

		private float GetInventoryFillFraction(string collectionName)
		{
			int stockCountForCategory = this.GetStockCountForCategory(collectionName);
			int maxInventoryForCategory = this.GetMaxInventoryForCategory(collectionName);
			if (maxInventoryForCategory == 0)
			{
				return 0f;
			}
			return (float)stockCountForCategory / (float)maxInventoryForCategory;
		}

		public List<TradeRouteDTO> GetItemsWithSurplusOrDemand()
		{
			List<TradeRouteDTO> list = new List<TradeRouteDTO>();
			if (this._productionMaps == null || this._productionMaps.Count == 0)
			{
				return list;
			}
			using (List<Producer>.Enumerator enumerator = this._productionMaps.GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					Producer producer = enumerator.Current;
					if (producer.OutputCollection != null)
					{
						float priceModifierForCategory = this.GetPriceModifierForCategory(producer.OutputCollection.Name);
						if ((double)this.GetInventoryFillFraction(producer.OutputCollection.Name) > 0.2 && priceModifierForCategory < 1f && !list.Any((TradeRouteDTO x) => x.CoCollection != null && x.CoCollection == producer.OutputCollection))
						{
							TradeRouteDTO item = new TradeRouteDTO().SetSupply(this.ShipRegId, producer.OutputCollection, this.GetStockCountForCategory(producer.OutputCollection), priceModifierForCategory);
							list.Add(item);
						}
					}
					IEnumerable<string> inputNames = producer.GetInputNames();
					if (inputNames != null)
					{
						foreach (string text in inputNames)
						{
							float priceModifierForCategory2 = this.GetPriceModifierForCategory(text);
							if (this.GetInventoryFillFraction(text) < 0.3f && priceModifierForCategory2 > 1f)
							{
								DataCoCollection dataCoCollection = DataHandler.GetDataCoCollection(text);
								TradeRouteDTO item2 = new TradeRouteDTO().SetDemand(this.ShipRegId, dataCoCollection, this.GetAvailabeInventorySpaceForCategory(text), priceModifierForCategory2);
								list.Add(item2);
							}
						}
					}
				}
			}
			return list;
		}

		public List<GUIShipMarketDTO> GetMarketReport()
		{
			List<GUIShipMarketDTO> list = new List<GUIShipMarketDTO>();
			foreach (KeyValuePair<string, float> keyValuePair in this._priceModifiers)
			{
				DataCoCollection dataCoCollection = DataHandler.GetDataCoCollection(keyValuePair.Key);
				if (dataCoCollection != null)
				{
					GUIShipMarketDTO item = new GUIShipMarketDTO
					{
						DataCoCollection = dataCoCollection,
						Stock = this.GetStockCountForCategory(keyValuePair.Key),
						MaxInventory = this.GetMaxInventoryForCategory(keyValuePair.Key),
						PriceModifier = keyValuePair.Value,
						AvgPrice = dataCoCollection.GetAveragePrice(),
						AvgMass = dataCoCollection.GetAverageMass()
					};
					list.Add(item);
				}
			}
			return list;
		}

		public string GetMarketDescription()
		{
			string text = string.Empty;
			HashSet<string> hashSet = new HashSet<string>();
			foreach (Producer producer in this._productionMaps)
			{
				string description = producer.GetDescription();
				if (!hashSet.Contains(description))
				{
					hashSet.Add(description);
					text = text + description + " \n";
				}
			}
			foreach (KeyValuePair<string, int> keyValuePair in this._stock)
			{
				float inventoryFillFraction = this.GetInventoryFillFraction(keyValuePair.Key);
				string text2 = text;
				text = string.Concat(new string[]
				{
					text2,
					"Inventory of ",
					keyValuePair.Key,
					" at ",
					(100f * inventoryFillFraction).ToString(),
					"% \n"
				});
			}
			if (text == string.Empty)
			{
				text = "Not producing at the moment";
			}
			return text;
		}

		public double GetStockMass()
		{
			if (this._cachedTotalMass >= 0.0)
			{
				return this._cachedTotalMass;
			}
			double num = 0.0;
			if (this._stock == null)
			{
				return num;
			}
			foreach (KeyValuePair<string, int> keyValuePair in this._stock)
			{
				if (!string.IsNullOrEmpty(keyValuePair.Key) && keyValuePair.Value != 0)
				{
					DataCoCollection dataCoCollection = DataHandler.GetDataCoCollection(keyValuePair.Key);
					if (dataCoCollection != null)
					{
						num += dataCoCollection.GetAverageMass() * (double)keyValuePair.Value;
					}
				}
			}
			this._cachedTotalMass = num;
			return num;
		}

		public void RegisterLoadedTrader(MarketActor trader)
		{
			if (this._loadedMarketActors == null)
			{
				this._loadedMarketActors = new List<MarketActor>();
			}
			else if (this._loadedMarketActors.Contains(trader))
			{
				return;
			}
			if (!this._marketMapping.ContainsKey(trader.GetCOId()))
			{
				if (MarketManager.ShowDebugLogs)
				{
					Debug.Log("#Market# Trader config not found: Added to market! " + trader.gameObject.name);
				}
				MarketActorConfig marketConfig = DataHandler.GetMarketConfig(trader.GetMarketConfig());
				marketConfig.COwnerId = trader.GetCOId();
				this.AddMarketConfig(trader.GetCOId(), marketConfig);
				this.BuildConfig();
			}
			this._loadedMarketActors.Add(trader);
			if (MarketManager.ShowDebugLogs)
			{
				Debug.Log("#Market# Trader registered " + trader.gameObject.name);
			}
		}

		public void UnRegisterLoadedTrader(MarketActor trader)
		{
			if (trader == null || this._loadedMarketActors == null)
			{
				return;
			}
			this._loadedMarketActors.Remove(trader);
		}

		public Dictionary<string, int> GetTradersMarketShare(MarketActor marketActor)
		{
			Dictionary<string, int> dictionary = new Dictionary<string, int>();
			MarketActorConfig marketActorConfig;
			if (!this._marketMapping.TryGetValue(marketActor.GetCOId(), out marketActorConfig))
			{
				return dictionary;
			}
			foreach (List<MarketItem> list in marketActorConfig.Stock.Values)
			{
				foreach (MarketItem marketItem in list)
				{
					int num;
					dictionary.TryGetValue(marketItem.COName, out num);
					dictionary[marketItem.COName] = num + 1;
				}
			}
			return dictionary;
		}

		public void UpdateTraderIDMaps(string oldId, string newId)
		{
			MarketActorConfig marketActorConfig;
			if (!this._marketMapping.TryGetValue(oldId, out marketActorConfig))
			{
				return;
			}
			marketActorConfig.COwnerId = newId;
			this._marketMapping.Remove(oldId);
			this._marketMapping[newId] = marketActorConfig;
		}

		public void RequestTraderUpdate()
		{
			if (this._loadedMarketActors == null || this._loadedMarketActors.Count == 0)
			{
				return;
			}
			foreach (MarketActor marketActor in this._loadedMarketActors)
			{
				if (marketActor != null)
				{
					marketActor.SyncInventoryToMarket();
				}
			}
		}

		public JsonShipMarketSave GetJson()
		{
			JsonShipMarketSave jsonShipMarketSave = new JsonShipMarketSave();
			jsonShipMarketSave.strShipRegId = this.ShipRegId;
			Dictionary<string, JsonMarketActorConfig> dictionary = new Dictionary<string, JsonMarketActorConfig>();
			foreach (KeyValuePair<string, MarketActorConfig> keyValuePair in this._marketMapping)
			{
				dictionary.Add(keyValuePair.Key, keyValuePair.Value.GetJson());
			}
			jsonShipMarketSave.configMap = dictionary;
			return jsonShipMarketSave;
		}

		private void LoadFromSave(JsonShipMarketSave jSave)
		{
			this.ShipRegId = jSave.strShipRegId;
			this._marketMapping = new Dictionary<string, MarketActorConfig>();
			foreach (KeyValuePair<string, JsonMarketActorConfig> keyValuePair in jSave.configMap)
			{
				this.AddMarketConfig(keyValuePair.Key, new MarketActorConfig(keyValuePair.Value));
			}
			this.BuildConfig();
		}

		private List<Producer> _productionMaps = new List<Producer>();

		private Dictionary<string, int> _stock = new Dictionary<string, int>();

		private Dictionary<string, int> _maxVirtualInventory = new Dictionary<string, int>();

		private Dictionary<string, float> _priceModifiers = new Dictionary<string, float>();

		private List<MarketActor> _loadedMarketActors = new List<MarketActor>();

		private Dictionary<string, MarketActorConfig> _marketMapping = new Dictionary<string, MarketActorConfig>();

		private double _cachedTotalMass = -1.0;
	}
}
