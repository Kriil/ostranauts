using System;
using System.Collections.Generic;
using System.Linq;
using Ostranauts.Core;
using Ostranauts.Core.Models;
using UnityEngine;

namespace Ostranauts.Trading
{
	public class MarketManager : MonoSingleton<MarketManager>
	{
		public static void Init(JsonMarketSave jSave = null)
		{
			if (jSave == null)
			{
				MonoSingleton<MarketManager>.Instance.BuildMarketData();
			}
			else
			{
				MonoSingleton<MarketManager>.Instance.LoadMarketData(jSave);
			}
			MonoSingleton<MarketManager>.Instance.BuildHubDictionary();
			MonoSingleton<MarketManager>.Instance._init = true;
		}

		private void BuildHubDictionary()
		{
			this._regionalMarketMap = new Dictionary<string, List<string>>();
			foreach (KeyValuePair<string, ShipMarket> keyValuePair in this._market)
			{
				ShipMarket value = keyValuePair.Value;
				if (value != null)
				{
					List<MarketActorConfig> configs = value.GetConfigs();
					if (configs != null)
					{
						foreach (MarketActorConfig marketActorConfig in configs)
						{
							if (marketActorConfig != null && !string.IsNullOrEmpty(marketActorConfig.HubRegId) && !marketActorConfig.IsCargoPod)
							{
								if (CrewSim.system.GetShipByRegID(marketActorConfig.HubRegId) != null && CrewSim.system.GetShipByRegID(keyValuePair.Key) != null)
								{
									if (!this._regionalMarketMap.ContainsKey(marketActorConfig.HubRegId))
									{
										this._regionalMarketMap.Add(marketActorConfig.HubRegId, new List<string>
										{
											marketActorConfig.HubRegId
										});
									}
									this._regionalMarketMap[marketActorConfig.HubRegId].Add(keyValuePair.Key);
									break;
								}
							}
						}
					}
				}
			}
		}

		private void BuildMarketData()
		{
			foreach (KeyValuePair<string, Ship> keyValuePair in CrewSim.system.dictShips)
			{
				Ship value = keyValuePair.Value;
				if (value != null && value.IsStation(false) && value.MarketConfigs != null && value.MarketConfigs.Count != 0)
				{
					foreach (KeyValuePair<string, string> keyValuePair2 in value.MarketConfigs)
					{
						MarketActorConfig marketConfig = DataHandler.GetMarketConfig(keyValuePair2.Value);
						marketConfig.COwnerId = keyValuePair2.Key;
						if (MarketManager.GetShipMarket(keyValuePair.Key) == null)
						{
							this._market[keyValuePair.Key] = new ShipMarket(keyValuePair.Key);
						}
						this._market[keyValuePair.Key].AddMarketConfig(keyValuePair2.Key, marketConfig);
					}
					this._market[keyValuePair.Key].BuildConfig();
				}
			}
			if (MarketManager.ShowDebugLogs)
			{
				Debug.Log("#Market# Found " + this._market.Count + " markets");
			}
		}

		public static void TraderIDUpdated(string regId, string oldId, string newId)
		{
			ShipMarket shipMarket = MarketManager.GetShipMarket(regId);
			if (shipMarket == null)
			{
				return;
			}
			shipMarket.UpdateTraderIDMaps(oldId, newId);
		}

		private void LoadMarketData(JsonMarketSave jSave)
		{
			this._market = new Dictionary<string, ShipMarket>();
			if (jSave == null || jSave.mapMarket == null)
			{
				return;
			}
			foreach (KeyValuePair<string, JsonShipMarketSave> keyValuePair in jSave.mapMarket)
			{
				if (keyValuePair.Value != null && keyValuePair.Value.configMap != null)
				{
					this._market[keyValuePair.Key] = new ShipMarket(keyValuePair.Value);
				}
			}
			if (MarketManager.ShowDebugLogs)
			{
				Debug.Log("#Market# Loaded " + this._market.Count + " markets");
			}
		}

		public static JsonMarketSave GetJSONSave()
		{
			return new JsonMarketSave(MonoSingleton<MarketManager>.Instance._market);
		}

		public static void Run()
		{
			MonoSingleton<MarketManager>.Instance.UpdateMarket();
		}

		private void UpdateMarket()
		{
			if (StarSystem.fEpoch - this._lastUpdateTime < 10.0)
			{
				return;
			}
			this._lastUpdateTime = StarSystem.fEpoch;
			foreach (KeyValuePair<string, ShipMarket> keyValuePair in this._market)
			{
				if (keyValuePair.Value != null)
				{
					bool flag = keyValuePair.Value.RunProduction(StarSystem.fEpoch);
					if (flag)
					{
						keyValuePair.Value.RequestTraderUpdate();
					}
				}
			}
		}

		public static double GetSupplyDemandModifier(CondOwner co)
		{
			Ship ship = co.ship ?? CrewSim.GetSelectedCrew().ship;
			ShipMarket shipMarket = MarketManager.GetShipMarket(ship.strRegID);
			return (double)((shipMarket != null) ? shipMarket.GetPriceModifierForItem(co.strName) : 1f);
		}

		public static ShipMarket GetShipMarket(string regId)
		{
			ShipMarket result;
			MonoSingleton<MarketManager>.Instance._market.TryGetValue(regId, out result);
			return result;
		}

		public static string GetStatusForShip(string strRegID)
		{
			string result = "Market data unavailable";
			ShipMarket shipMarket = MarketManager.GetShipMarket(strRegID);
			if (shipMarket != null)
			{
				return shipMarket.GetMarketDescription();
			}
			return result;
		}

		public static Dictionary<string, Dictionary<string, float>> GetSystemMarket()
		{
			Dictionary<string, Dictionary<string, float>> dictionary = new Dictionary<string, Dictionary<string, float>>();
			foreach (KeyValuePair<string, ShipMarket> keyValuePair in MonoSingleton<MarketManager>.Instance._market)
			{
				string key = keyValuePair.Key;
				Dictionary<string, float> priceModifiers = keyValuePair.Value.PriceModifiers;
				Ship shipByRegID = CrewSim.system.GetShipByRegID(key);
				if (shipByRegID != null && shipByRegID.IsStation(false))
				{
					foreach (KeyValuePair<string, float> keyValuePair2 in priceModifiers)
					{
						string key2 = keyValuePair2.Key;
						float value = keyValuePair2.Value;
						Dictionary<string, float> dictionary2;
						if (dictionary.TryGetValue(key2, out dictionary2))
						{
							dictionary2[key] = value;
						}
						else
						{
							dictionary.Add(key2, new Dictionary<string, float>
							{
								{
									key,
									value
								}
							});
						}
					}
				}
			}
			return dictionary;
		}

		public static List<GUIShipMarketDTO> GetStationMarket(string stationRegID)
		{
			ShipMarket shipMarket = MarketManager.GetShipMarket(stationRegID);
			if (shipMarket == null)
			{
				return new List<GUIShipMarketDTO>();
			}
			return shipMarket.GetMarketReport();
		}

		public static void RegisterTrader(string regId, MarketActor trader)
		{
			if (MonoSingleton<MarketManager>.Instance._market == null)
			{
				Debug.Log("#Market# Trader ready before market! Did not register");
				return;
			}
			ShipMarket shipMarket;
			if (MonoSingleton<MarketManager>.Instance._market.TryGetValue(regId, out shipMarket))
			{
				shipMarket.RegisterLoadedTrader(trader);
			}
			else
			{
				if (MarketManager.ShowDebugLogs)
				{
					Debug.Log("#Market# <color=green>Registered new Market</color>");
				}
				shipMarket = new ShipMarket(regId);
				shipMarket.RegisterLoadedTrader(trader);
				MonoSingleton<MarketManager>.Instance._market.Add(regId, shipMarket);
			}
		}

		public static void ReportTransaction(string regId, string coName, int change, string coIDActor = null)
		{
			ShipMarket shipMarket = MarketManager.GetShipMarket(regId);
			if (shipMarket == null)
			{
				Debug.LogWarning("#Market# No market found for regId:  " + regId);
				return;
			}
			MarketItem marketItem = new MarketItem(coName);
			int stockCountForCategory = shipMarket.GetStockCountForCategory(marketItem.COCollectionName);
			int maxInventoryForCategory = shipMarket.GetMaxInventoryForCategory(marketItem.COCollectionName);
			if (stockCountForCategory + change <= maxInventoryForCategory && change > 0)
			{
				shipMarket.AddStockForCategory(marketItem, change, coIDActor);
			}
			else if (change < 0 && stockCountForCategory + change >= 0)
			{
				shipMarket.RemoveStockForCategory(marketItem.COCollectionName, Mathf.Abs(change), coIDActor);
			}
			else if (change > 0 && MarketManager.ShowDebugLogs)
			{
				Debug.Log("#Market# No inventory space for " + marketItem.COName + " of collection " + marketItem.COCollectionName);
			}
		}

		public static Dictionary<string, int> SyncTraderInventory(string regId, MarketActor trader, List<CondOwner> inventory)
		{
			Dictionary<string, int> dictionary = new Dictionary<string, int>();
			ShipMarket shipMarket = MarketManager.GetShipMarket(regId);
			if (trader == null || shipMarket == null)
			{
				return dictionary;
			}
			dictionary = shipMarket.GetTradersMarketShare(trader);
			foreach (CondOwner condOwner in inventory)
			{
				if (!(condOwner == null))
				{
					foreach (KeyValuePair<string, int> keyValuePair in dictionary.ToArray<KeyValuePair<string, int>>())
					{
						if (condOwner.strName == keyValuePair.Key)
						{
							dictionary[keyValuePair.Key] = keyValuePair.Value - 1;
						}
					}
				}
			}
			return dictionary;
		}

		public static void UnregisterTrader(string shipRegID, MarketActor trader)
		{
			if (string.IsNullOrEmpty(shipRegID) || trader == null)
			{
				return;
			}
			ShipMarket shipMarket;
			if (!MonoSingleton<MarketManager>.Instance._market.TryGetValue(shipRegID, out shipMarket))
			{
				return;
			}
			shipMarket.UnRegisterLoadedTrader(trader);
		}

		public static void AddMarketActorToShip(Ship cargoShip, string coId)
		{
			ShipMarket shipMarket = MarketManager.GetShipMarket(cargoShip.strRegID);
			if (shipMarket == null)
			{
				shipMarket = new ShipMarket(cargoShip.strRegID);
				MonoSingleton<MarketManager>.Instance._market.Add(cargoShip.strRegID, shipMarket);
			}
			string text;
			if (cargoShip.MarketConfigs.TryGetValue(coId, out text) && !string.IsNullOrEmpty(text))
			{
				MarketActorConfig configForMarketActor = shipMarket.GetConfigForMarketActor(coId);
				if (configForMarketActor != null)
				{
					Debug.LogWarning("#Market# Ship has already a registered marketconfig for this CO! Overwritting it now");
				}
				MarketActorConfig marketConfig = DataHandler.GetMarketConfig(text);
				marketConfig.COwnerId = coId;
				shipMarket.AddMarketConfig(coId, marketConfig);
			}
			shipMarket.BuildConfig();
		}

		public static List<MarketItem> RemoveMarketActorFromShip(Ship cargoShip, string coId)
		{
			ShipMarket shipMarket = MarketManager.GetShipMarket(cargoShip.strRegID);
			if (shipMarket == null)
			{
				Debug.LogWarning("no market when removing");
				return null;
			}
			List<MarketItem> result = shipMarket.RemoveMarketConfig(coId);
			shipMarket.BuildConfig();
			if (shipMarket.IsBlank())
			{
				if (MarketManager.ShowDebugLogs)
				{
					Debug.LogWarning("#Market# No more market data on ship " + cargoShip.strRegID + " -removing market");
				}
				MarketManager.UnregisterShip(cargoShip.strRegID);
			}
			return result;
		}

		public static double GetCargoMassForShip(Ship cargoShip)
		{
			ShipMarket shipMarket = MarketManager.GetShipMarket(cargoShip.strRegID);
			return (shipMarket != null) ? shipMarket.GetStockMass() : 0.0;
		}

		public static void RegisterAICargoHauler(Ship cargoShip, List<TradeRouteDTO> tradeRoutes)
		{
			if (tradeRoutes == null)
			{
				return;
			}
			TradeRouteDTO tradeRouteDTO = tradeRoutes.FirstOrDefault<TradeRouteDTO>();
			if (tradeRouteDTO == null)
			{
				return;
			}
			ShipMarket shipMarket = MarketManager.GetShipMarket(tradeRouteDTO.OriginStation);
			ShipMarket shipMarket2 = MarketManager.GetShipMarket(cargoShip.strRegID);
			if (shipMarket2 == null)
			{
				shipMarket2 = new ShipMarket(cargoShip.strRegID);
				MonoSingleton<MarketManager>.Instance._market.Add(cargoShip.strRegID, shipMarket2);
			}
			foreach (TradeRouteDTO tradeRouteDTO2 in tradeRoutes)
			{
				List<MarketItem> list = shipMarket.RemoveStockForCategory(tradeRouteDTO2.CoCollection.Name, tradeRouteDTO2.Amount, null);
				foreach (KeyValuePair<string, string> keyValuePair in cargoShip.MarketConfigs)
				{
					string value = keyValuePair.Value;
					string key = keyValuePair.Key;
					MarketActorConfig marketConfig = DataHandler.GetMarketConfig(value);
					if (marketConfig.IsCargoPod && marketConfig.IsEmpty)
					{
						marketConfig.SetMaxInventoryForCategory(tradeRouteDTO2.CoCollection, 0);
						int num = marketConfig.TryAddStock(tradeRouteDTO2.CoCollection.Name, list);
						if (num > 0)
						{
							if (MarketManager.ShowDebugLogs)
							{
								Debug.LogWarning(string.Concat(new object[]
								{
									"Could not fit: ",
									num,
									" out of ",
									tradeRouteDTO2.Amount
								}));
							}
							list = list.GetRange(list.Count - num, num);
						}
						shipMarket2.AddMarketConfig(key, marketConfig);
						if (num == 0)
						{
							break;
						}
					}
				}
				shipMarket2.BuildConfig();
				shipMarket2.RequestTraderUpdate();
				if (list != null && list.Count > 0)
				{
					if (MarketManager.ShowDebugLogs)
					{
						Debug.LogWarning("Did not transfer all items, readding: " + list.Count);
					}
					shipMarket.AddStockForCategory(tradeRouteDTO2.CoCollection.Name, list, null);
				}
				shipMarket.RequestTraderUpdate();
			}
		}

		public static List<MarketActorConfig> GetCargoPods(string regId)
		{
			List<MarketActorConfig> list = new List<MarketActorConfig>();
			ShipMarket shipMarket = MarketManager.GetShipMarket(regId);
			if (shipMarket == null)
			{
				return list;
			}
			List<MarketActorConfig> configs = shipMarket.GetConfigs();
			list.AddRange(from x in configs
			where x.IsCargoPod
			select x);
			return list;
		}

		public static void UnregisterShip(string shipRegId)
		{
			if (string.IsNullOrEmpty(shipRegId))
			{
				return;
			}
			MonoSingleton<MarketManager>.Instance._market.Remove(shipRegId);
		}

		public static void UnregisterCargoShip(string cargoShipRegId, string cargoDestinationStationRegId)
		{
			ShipMarket shipMarket = MarketManager.GetShipMarket(cargoShipRegId);
			ShipMarket shipMarket2 = MarketManager.GetShipMarket(cargoDestinationStationRegId);
			MarketManager.UnregisterShip(cargoShipRegId);
			if (shipMarket == null || shipMarket2 == null)
			{
				if (shipMarket != null)
				{
					Debug.Log("#Market# + Cargo ship despawned, no related market found");
				}
				return;
			}
			foreach (MarketActorConfig marketActorConfig in shipMarket.GetConfigs())
			{
				foreach (MarketItem marketItem in marketActorConfig.GetFlatStockList())
				{
					shipMarket2.AddStockForCategory(marketItem, 1, null);
				}
			}
			shipMarket2.RequestTraderUpdate();
			if (MarketManager.ShowDebugLogs)
			{
				Debug.Log("#Market# <color=green>Cargo Transferred</color> to " + cargoDestinationStationRegId);
			}
		}

		public static void RegisterShipToShipTransaction(string originShipRegID, string cargoDestinationStationRegId, string coCollection, int amount, string targetPodId)
		{
			ShipMarket shipMarket = MarketManager.GetShipMarket(originShipRegID);
			ShipMarket shipMarket2 = MarketManager.GetShipMarket(cargoDestinationStationRegId);
			if (shipMarket == null || shipMarket2 == null)
			{
				Debug.Log("#Market# + no related market found for ship to ship transaction");
				return;
			}
			MarketActorConfig configForMarketActor = shipMarket2.GetConfigForMarketActor(targetPodId);
			if (configForMarketActor.IsEmpty && configForMarketActor.IsCargoPod)
			{
				configForMarketActor.SetMaxInventoryForCategory(DataHandler.GetDataCoCollection(coCollection), 0);
			}
			List<MarketItem> marketItems = shipMarket.RemoveStockForCategory(coCollection, amount, null);
			shipMarket2.AddStockForCategory(coCollection, marketItems, targetPodId);
			shipMarket.RequestTraderUpdate();
			shipMarket2.RequestTraderUpdate();
		}

		public static void RegisterShipToShipTransaction(string coCollection, string originShipRegID, string cargoDestinationStationRegId, MarketActorConfig marketConfigOrigin, int amount)
		{
			ShipMarket shipMarket = MarketManager.GetShipMarket(originShipRegID);
			ShipMarket shipMarket2 = MarketManager.GetShipMarket(cargoDestinationStationRegId);
			if (shipMarket == null || shipMarket2 == null)
			{
				Debug.Log("#Market# + no related market found for ship to ship transaction");
				return;
			}
			List<MarketItem> marketItems = shipMarket.RemoveStockForCategory(coCollection, amount, marketConfigOrigin.COwnerId);
			shipMarket2.AddStockForCategory(coCollection, marketItems, null);
			if (marketConfigOrigin.Stock.Count == 0 && marketConfigOrigin.IsCargoPod)
			{
				marketConfigOrigin.MaxVirtualInventorySize.Clear();
				marketConfigOrigin.Stock.Clear();
			}
			shipMarket.RequestTraderUpdate();
			shipMarket2.RequestTraderUpdate();
		}

		public static void ResetCargoPod(string regId, MarketActorConfig podConfig)
		{
			podConfig.Reset();
			ShipMarket shipMarket = MarketManager.GetShipMarket(regId);
			if (shipMarket != null)
			{
				shipMarket.BuildConfig();
			}
		}

		private static List<TradeRouteDTO> FindSupplyDemandMarketPairs()
		{
			List<TradeRouteDTO> list = new List<TradeRouteDTO>();
			List<TradeRouteDTO> list2 = new List<TradeRouteDTO>();
			foreach (KeyValuePair<string, ShipMarket> keyValuePair in MonoSingleton<MarketManager>.Instance._market)
			{
				List<TradeRouteDTO> itemsWithSurplusOrDemand = keyValuePair.Value.GetItemsWithSurplusOrDemand();
				if (itemsWithSurplusOrDemand.Count > 0)
				{
					list2.AddRange(itemsWithSurplusOrDemand);
				}
			}
			List<TradeRouteDTO> list3 = (from x in list2
			where x.DestinationStation != null
			select x).ToList<TradeRouteDTO>();
			List<TradeRouteDTO> list4 = (from x in list2
			where x.OriginStation != null
			select x).ToList<TradeRouteDTO>();
			foreach (TradeRouteDTO tradeRouteDTO in list4)
			{
				foreach (TradeRouteDTO tradeRouteDTO2 in list3)
				{
					if (!(tradeRouteDTO.OriginStation == tradeRouteDTO2.DestinationStation) && tradeRouteDTO2.CoCollection == tradeRouteDTO.CoCollection)
					{
						list.Add(new TradeRouteDTO(tradeRouteDTO, tradeRouteDTO2));
					}
				}
			}
			MonoSingleton<MarketManager>.Instance.AdjustForLocalMarkets(ref list);
			return list;
		}

		private void AdjustForLocalMarkets(ref List<TradeRouteDTO> allRoutes)
		{
			if (allRoutes == null)
			{
				return;
			}
			foreach (KeyValuePair<string, List<string>> keyValuePair in this._regionalMarketMap)
			{
				foreach (TradeRouteDTO tradeRouteDTO in allRoutes)
				{
					bool flag = keyValuePair.Value.Contains(tradeRouteDTO.OriginStation);
					bool flag2 = keyValuePair.Value.Contains(tradeRouteDTO.DestinationStation);
					if ((!flag || !flag2) && (flag || flag2) && !(tradeRouteDTO.OriginStation == keyValuePair.Key) && !(tradeRouteDTO.DestinationStation == keyValuePair.Key))
					{
						ShipMarket shipMarket = MarketManager.GetShipMarket(keyValuePair.Key);
						if (shipMarket != null)
						{
							int availabeInventorySpaceForCategory = shipMarket.GetAvailabeInventorySpaceForCategory(tradeRouteDTO.CoCollection);
							if (flag)
							{
								if (availabeInventorySpaceForCategory - tradeRouteDTO.Amount >= 0)
								{
									tradeRouteDTO.DestinationStation = shipMarket.ShipRegId;
									if (tradeRouteDTO.Amount > availabeInventorySpaceForCategory)
									{
										tradeRouteDTO.Amount = availabeInventorySpaceForCategory;
									}
								}
								else
								{
									tradeRouteDTO.OriginStation = shipMarket.ShipRegId;
								}
							}
							else if (flag2)
							{
								if (availabeInventorySpaceForCategory - tradeRouteDTO.Amount >= 0)
								{
									tradeRouteDTO.DestinationStation = shipMarket.ShipRegId;
									if (tradeRouteDTO.Amount > availabeInventorySpaceForCategory)
									{
										tradeRouteDTO.Amount = availabeInventorySpaceForCategory;
									}
								}
								else
								{
									tradeRouteDTO.OriginStation = shipMarket.ShipRegId;
								}
							}
							if (MarketManager.ShowDebugLogs)
							{
								Debug.LogWarning("Redirect: " + tradeRouteDTO.OriginStation + " to " + tradeRouteDTO.DestinationStation);
							}
						}
					}
				}
			}
		}

		public static List<TradeRouteDTO> GetTradeRoute()
		{
			List<TradeRouteDTO> list = MarketManager.FindSupplyDemandMarketPairs();
			if (list.Count == 0)
			{
				return null;
			}
			if (list.Count == 1)
			{
				return new List<TradeRouteDTO>
				{
					list.First<TradeRouteDTO>()
				};
			}
			IOrderedEnumerable<TradeRouteDTO> orderedEnumerable = from x in list
			orderby x.RouteValue descending
			select x;
			using (IEnumerator<TradeRouteDTO> enumerator = orderedEnumerable.GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					TradeRouteDTO tradeRoute = enumerator.Current;
					if (tradeRoute.RouteValue > 0.0)
					{
						if (UnityEngine.Random.Range(0, 10) < 2)
						{
							List<TradeRouteDTO> list2 = new List<TradeRouteDTO>();
							list2.Add(tradeRoute);
							List<TradeRouteDTO> collection = (from x in list
							where x.OriginStation == tradeRoute.OriginStation && x.DestinationStation == tradeRoute.DestinationStation
							select x).ToList<TradeRouteDTO>();
							list2.AddRange(collection);
							return list2;
						}
					}
				}
			}
			return null;
		}

		public static int CARGOPOD_DEFAULTMASSCAPACITY = 1000;

		private double _lastUpdateTime;

		private Dictionary<string, ShipMarket> _market = new Dictionary<string, ShipMarket>();

		private Dictionary<string, List<string>> _regionalMarketMap = new Dictionary<string, List<string>>();

		private bool _init;

		public static bool ShowDebugLogs;
	}
}
