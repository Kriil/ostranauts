using System;
using System.Collections.Generic;
using System.Linq;
using Ostranauts.Core.Models;
using Ostranauts.Tools.ExtensionMethods;
using UnityEngine;

namespace Ostranauts.Trading
{
	public class MarketActorConfig
	{
		public MarketActorConfig(JsonMarketActorConfig jActorConfig)
		{
			this._name = jActorConfig.strName;
			this.HubRegId = jActorConfig.strHubRegId;
			this.CargoSpecs = DataHandler.GetCargoRequirements(jActorConfig.aCargoPodSpecs);
			if (jActorConfig.strCOOwnerId != null)
			{
				this.COwnerId = jActorConfig.strCOOwnerId;
			}
			this._productionMaps = new List<Producer>();
			if (jActorConfig.aSupplyDemandMaps != null)
			{
				foreach (string name in jActorConfig.aSupplyDemandMaps)
				{
					Producer productionMap = DataHandler.GetProductionMap(name);
					if (productionMap != null)
					{
						this._productionMaps.Add(productionMap);
					}
				}
			}
			this.MaxVirtualInventorySize = this.ParseStringArray(jActorConfig.aVirtualInventorySize);
			this.Stock = this.ParseStock(jActorConfig.aStock);
		}

		public bool IsCargoPod
		{
			get
			{
				return this.CargoSpecs != null;
			}
		}

		public List<JsonCargoSpec> CargoSpecs { get; private set; }

		public bool IsEmpty
		{
			get
			{
				return this._productionMaps.Count == 0 && this.Stock.Count == 0 && this.MaxVirtualInventorySize.Count == 0;
			}
		}

		public bool CanTakeCargoFrom(GUIShipMarketDTO marketDTO)
		{
			return marketDTO != null && marketDTO.DataCoCollection != null && !marketDTO.IsEmpty && marketDTO.DataCoCollection.CanFitIn(this);
		}

		public List<MarketItem> GetFlatStockList()
		{
			return this.Stock.Values.SelectMany((List<MarketItem> x) => x).ToList<MarketItem>();
		}

		public Dictionary<string, int> GetStockTotals()
		{
			Dictionary<string, int> dictionary = new Dictionary<string, int>();
			foreach (KeyValuePair<string, List<MarketItem>> keyValuePair in this.Stock)
			{
				int count = keyValuePair.Value.Count;
				dictionary.Add(keyValuePair.Key, count);
			}
			return dictionary;
		}

		public Tuple<string, int> GetStockSingle()
		{
			List<string> list = this.Stock.Keys.ToList<string>();
			if (list.Count > 0)
			{
				string item = list.First<string>();
				return new Tuple<string, int>(item, this.Stock[list.First<string>()].Count);
			}
			List<string> list2 = this.MaxVirtualInventorySize.Keys.ToList<string>();
			if (list2.Count > 0)
			{
				string item2 = list2.First<string>();
				return new Tuple<string, int>(item2, 0);
			}
			return null;
		}

		public GUIShipMarketDTO GetCargoPodData()
		{
			if (!this.IsCargoPod)
			{
				return null;
			}
			Tuple<string, int> stockSingle = this.GetStockSingle();
			if (stockSingle == null)
			{
				return new GUIShipMarketDTO
				{
					DataCoCollection = null,
					Stock = 0,
					MaxInventory = 0,
					PriceModifier = 0f,
					AvgPrice = 0.0,
					AvgMass = 0.0
				};
			}
			DataCoCollection dataCoCollection = DataHandler.GetDataCoCollection(stockSingle.Item1);
			return new GUIShipMarketDTO
			{
				DataCoCollection = dataCoCollection,
				Stock = stockSingle.Item2,
				MaxInventory = this.GetMaxVirtualInventorySize(stockSingle.Item1),
				AvgPrice = dataCoCollection.GetAveragePrice(),
				AvgMass = dataCoCollection.GetAverageMass()
			};
		}

		public void SetMaxInventoryForCategory(string category, int maxInventory)
		{
			this.MaxVirtualInventorySize[category] = maxInventory;
		}

		public void SetMaxInventoryForCategory(DataCoCollection coCollection, int maxInventory = 0)
		{
			if (maxInventory == 0)
			{
				maxInventory = (int)((double)MarketManager.CARGOPOD_DEFAULTMASSCAPACITY / coCollection.GetAverageMass());
			}
			this.SetMaxInventoryForCategory(coCollection.Name, maxInventory);
			if (MarketManager.ShowDebugLogs)
			{
				Debug.Log(string.Concat(new object[]
				{
					"#Market# + setting max inventory for pod to ",
					maxInventory,
					" for collection: ",
					coCollection.Name
				}));
			}
		}

		private Dictionary<string, List<MarketItem>> ParseStock(string[] stringArray)
		{
			Dictionary<string, List<MarketItem>> dictionary = new Dictionary<string, List<MarketItem>>();
			if (stringArray == null || stringArray.Length == 0)
			{
				return dictionary;
			}
			Dictionary<string, int> dictionary2 = this.ParseStringArray(stringArray);
			foreach (KeyValuePair<string, int> keyValuePair in dictionary2)
			{
				string[] array = keyValuePair.Key.Split(new char[]
				{
					'|'
				});
				string name = array[0];
				DataCoCollection dataCoCollection = DataHandler.GetDataCoCollection(name);
				if (dataCoCollection == null)
				{
					Debug.LogWarning(string.Concat(new string[]
					{
						"#Market# No COCollection found for stock: ",
						keyValuePair.Key,
						" on MarketActor: ",
						this._name,
						" Skipping!"
					}));
				}
				else
				{
					DataCO dataCO = (array.Length != 2) ? null : DataHandler.GetDataCO(array[1]);
					List<MarketItem> list;
					if (!dictionary.TryGetValue(dataCoCollection.Name, out list))
					{
						list = new List<MarketItem>();
					}
					for (int i = keyValuePair.Value; i > 0; i--)
					{
						list.Add(new MarketItem(dataCoCollection, dataCO ?? dataCoCollection.GetRandomDataCo()));
					}
					dictionary[dataCoCollection.Name] = list;
				}
			}
			return dictionary;
		}

		private Dictionary<string, int> ParseStringArray(string[] stringArray)
		{
			Dictionary<string, int> dictionary = new Dictionary<string, int>();
			if (stringArray == null)
			{
				return dictionary;
			}
			foreach (string text in stringArray)
			{
				string[] array = text.Split(new char[]
				{
					'='
				});
				string key = array[0];
				int num = 1;
				if (array.Length == 2)
				{
					string[] array2 = array[1].Split(new char[]
					{
						'-'
					});
					int.TryParse(array2[0], out num);
					if (array2.Length > 1)
					{
						int num2 = num;
						int.TryParse(array2[1], out num2);
						num = UnityEngine.Random.Range(num, num2 + 1);
					}
				}
				int num3;
				dictionary.TryGetValue(key, out num3);
				dictionary[key] = num3 + num;
			}
			return dictionary;
		}

		public bool RunProduction(ShipMarket sm, double currentTime)
		{
			bool flag = false;
			foreach (Producer producer in this._productionMaps)
			{
				if (producer.IsReady(currentTime))
				{
					int availableCycles = producer.GetAvailableCycles(currentTime);
					for (int i = 0; i < availableCycles; i++)
					{
						flag |= producer.Run(sm);
					}
					producer.LastUpdateTime = currentTime;
				}
			}
			return flag;
		}

		public int TryAddStock(MarketItem marketItem, int amount)
		{
			int maxVirtualInventorySize = this.GetMaxVirtualInventorySize(marketItem.COCollectionName);
			if (maxVirtualInventorySize == 0)
			{
				return amount;
			}
			int stock = this.GetStock(marketItem.COCollectionName);
			if (maxVirtualInventorySize == stock && amount > 0)
			{
				return amount;
			}
			int num = stock + amount;
			int num2 = (num <= maxVirtualInventorySize) ? 0 : (num - maxVirtualInventorySize);
			amount -= num2;
			List<MarketItem> list;
			if (!this.Stock.TryGetValue(marketItem.COCollectionName, out list))
			{
				list = new List<MarketItem>();
			}
			for (int i = 0; i < amount; i++)
			{
				list.Add(marketItem.Clone());
			}
			this.Stock[marketItem.COCollectionName] = list;
			return num2;
		}

		public int TryAddStock(string coCollectionName, List<MarketItem> marketItems)
		{
			int num = marketItems.Count;
			int maxVirtualInventorySize = this.GetMaxVirtualInventorySize(coCollectionName);
			if (maxVirtualInventorySize == 0)
			{
				return num;
			}
			int stock = this.GetStock(coCollectionName);
			if (maxVirtualInventorySize == stock && num > 0)
			{
				return num;
			}
			int num2 = stock + num;
			int num3 = (num2 <= maxVirtualInventorySize) ? 0 : (num2 - maxVirtualInventorySize);
			num -= num3;
			List<MarketItem> list;
			if (!this.Stock.TryGetValue(coCollectionName, out list))
			{
				list = new List<MarketItem>();
			}
			for (int i = 0; i < num; i++)
			{
				list.Add(marketItems[i].Clone());
			}
			this.Stock[coCollectionName] = list;
			return num3;
		}

		public List<MarketItem> TakeOutStock(string categoryName, int amount)
		{
			List<MarketItem> list = new List<MarketItem>();
			if (this.GetMaxVirtualInventorySize(categoryName) == 0)
			{
				return list;
			}
			List<MarketItem> list2;
			if (this.Stock.TryGetValue(categoryName, out list2))
			{
				int num = amount;
				while (num > 0 && list2.Count > 0)
				{
					list.Add(list2[list2.Count - 1]);
					list2.RemoveAt(list2.Count - 1);
					num--;
				}
				if (list2.Count == 0)
				{
					this.Stock.Remove(categoryName);
				}
			}
			return list;
		}

		public void AddStockAndInventory(List<MarketItem> superItems)
		{
			string cocollectionName = superItems.First<MarketItem>().COCollectionName;
			List<MarketItem> list;
			if (!this.Stock.TryGetValue(cocollectionName, out list))
			{
				list = new List<MarketItem>();
			}
			list.AddRange(superItems);
			this.Stock[cocollectionName] = list;
			if (this.MaxVirtualInventorySize.ContainsKey(cocollectionName))
			{
				Dictionary<string, int> maxVirtualInventorySize;
				string key;
				(maxVirtualInventorySize = this.MaxVirtualInventorySize)[key = cocollectionName] = maxVirtualInventorySize[key] + superItems.Count;
			}
			else
			{
				this.MaxVirtualInventorySize.Add(cocollectionName, superItems.Count);
			}
		}

		private int GetMaxVirtualInventorySize(string coName)
		{
			int result = 0;
			this.MaxVirtualInventorySize.TryGetValue(coName, out result);
			return result;
		}

		private int GetStock(string categoryName)
		{
			List<MarketItem> list;
			if (this.Stock.TryGetValue(categoryName, out list))
			{
				return list.Count;
			}
			return 0;
		}

		public void Reset()
		{
			this.MaxVirtualInventorySize.Clear();
			this.Stock.Clear();
		}

		public List<Producer> GetProductionMaps()
		{
			return this._productionMaps;
		}

		public JsonMarketActorConfig GetJson()
		{
			JsonMarketActorConfig jsonMarketActorConfig = new JsonMarketActorConfig();
			jsonMarketActorConfig.strName = this._name;
			jsonMarketActorConfig.strCOOwnerId = this.COwnerId;
			jsonMarketActorConfig.strHubRegId = this.HubRegId;
			List<string> list = new List<string>();
			foreach (Producer producer in this._productionMaps)
			{
				list.Add(producer.Name);
			}
			jsonMarketActorConfig.aSupplyDemandMaps = list.ToArray();
			jsonMarketActorConfig.aStock = this.BuildStockJson();
			if (this.CargoSpecs != null)
			{
				jsonMarketActorConfig.aCargoPodSpecs = (from x in this.CargoSpecs
				select x.strName).ToArray<string>();
			}
			List<string> list2 = new List<string>();
			foreach (KeyValuePair<string, int> keyValuePair in this.MaxVirtualInventorySize)
			{
				list2.Add(keyValuePair.Key + "=" + keyValuePair.Value);
			}
			jsonMarketActorConfig.aVirtualInventorySize = list2.ToArray();
			return jsonMarketActorConfig;
		}

		private string[] BuildStockJson()
		{
			List<MarketItem> flatStockList = this.GetFlatStockList();
			Dictionary<string, int> dictionary = new Dictionary<string, int>();
			foreach (MarketItem marketItem in flatStockList)
			{
				string jsonName = marketItem.GetJsonName();
				if (!string.IsNullOrEmpty(jsonName))
				{
					dictionary.Increment(jsonName);
				}
			}
			List<string> list = new List<string>();
			foreach (KeyValuePair<string, int> keyValuePair in dictionary)
			{
				if (keyValuePair.Value > 0 && !string.IsNullOrEmpty(keyValuePair.Key))
				{
					list.Add(keyValuePair.Key + "=" + keyValuePair.Value);
				}
			}
			return list.ToArray();
		}

		private string _name;

		public string COwnerId;

		public string HubRegId;

		private List<Producer> _productionMaps;

		public Dictionary<string, List<MarketItem>> Stock;

		public Dictionary<string, int> MaxVirtualInventorySize;
	}
}
