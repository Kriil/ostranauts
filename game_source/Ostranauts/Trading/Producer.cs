using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Ostranauts.Trading
{
	public class Producer
	{
		public Producer(JsonProductionMap jProdMap)
		{
			this.Name = jProdMap.strName;
			this._friendlyDescription = jProdMap.strFriendlyDescription;
			this.OutputCollection = DataHandler.GetDataCoCollection(jProdMap.strOutputCollection);
			foreach (string text in jProdMap.aInputCollections)
			{
				string[] array = text.Split(new char[]
				{
					'='
				});
				int value = 0;
				int.TryParse(array[1], out value);
				this._dictInputCollections.Add(DataHandler.GetDataCoCollection(array[0]), value);
			}
			this._outputPerHour = jProdMap.fOutputPerHour;
			this._outputPerRun = Mathf.Max(1, jProdMap.nOutputPerRun);
		}

		public string Name { get; private set; }

		public DataCoCollection OutputCollection { get; set; }

		public double LastUpdateTime { get; set; }

		public IEnumerable<string> GetInputNames()
		{
			IEnumerable<string> result;
			if (this._dictInputCollections.Count == 0)
			{
				result = null;
			}
			else
			{
				result = from x in this._dictInputCollections
				select x.Key.Name;
			}
			return result;
		}

		public string GetDescription()
		{
			return this._friendlyDescription ?? "unknown";
		}

		public float GetDemandPerHourForCategory(string categoryName)
		{
			if (this.OutputCollection != null && this.OutputCollection.Name == categoryName)
			{
				return this._outputPerHour * -1f;
			}
			if (this._dictInputCollections != null)
			{
				foreach (KeyValuePair<DataCoCollection, int> keyValuePair in this._dictInputCollections)
				{
					if (!(keyValuePair.Key.Name != categoryName))
					{
						return (float)keyValuePair.Value * this._outputPerHour;
					}
				}
			}
			return 0f;
		}

		public bool Run(ShipMarket sm)
		{
			bool flag = true;
			foreach (KeyValuePair<DataCoCollection, int> keyValuePair in this._dictInputCollections)
			{
				if (sm.GetStockCountForCategory(keyValuePair.Key) < keyValuePair.Value)
				{
					flag = false;
					break;
				}
			}
			if (flag && this.OutputCollection != null && sm.GetAvailabeInventorySpaceForCategory(this.OutputCollection) < this._outputPerRun)
			{
				flag = false;
			}
			if (flag)
			{
				string text = string.Empty;
				foreach (KeyValuePair<DataCoCollection, int> keyValuePair2 in this._dictInputCollections)
				{
					sm.RemoveStockForCategory(keyValuePair2.Key.Name, keyValuePair2.Value, null);
					string text2 = text;
					text = string.Concat(new object[]
					{
						text2,
						"Consumed ",
						keyValuePair2.Key.Name,
						" Stock: ",
						sm.GetStockCountForCategory(keyValuePair2.Key.Name),
						" \n "
					});
				}
				if (this.OutputCollection != null)
				{
					sm.AddStockForCategory(new MarketItem(this.OutputCollection), this._outputPerRun, null);
					string text2 = text;
					text = string.Concat(new object[]
					{
						text2,
						"Produced ",
						this.OutputCollection.Name,
						" Stock: ",
						sm.GetStockCountForCategory(this.OutputCollection)
					});
				}
				if (MarketManager.ShowDebugLogs)
				{
					Debug.LogWarning("#Market# " + sm.ShipRegId + " " + text);
				}
			}
			return flag;
		}

		public bool IsReady(double epochTime)
		{
			if (this.LastUpdateTime == 0.0)
			{
				this.LastUpdateTime = StarSystem.fEpoch;
				return false;
			}
			return epochTime - this.LastUpdateTime >= (double)this.GetUpdateInterval();
		}

		private float GetUpdateInterval()
		{
			if (this._outputPerHour == 0f)
			{
				return float.PositiveInfinity;
			}
			return Mathf.Abs(3600f / this._outputPerHour);
		}

		public int GetAvailableCycles(double epochTime)
		{
			float num = (float)(epochTime - this.LastUpdateTime);
			return Mathf.FloorToInt(num / this.GetUpdateInterval());
		}

		private readonly string _friendlyDescription;

		private readonly float _outputPerHour;

		private readonly int _outputPerRun;

		private Dictionary<DataCoCollection, int> _dictInputCollections = new Dictionary<DataCoCollection, int>();
	}
}
