using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Ostranauts.Trading
{
	public class DataCoCollection
	{
		public DataCoCollection(JsonDCOCollection jCollection)
		{
			this.Name = jCollection.strName;
			this._friendlyName = jCollection.strFriendlyName;
			this.CargoSpecs = DataHandler.GetCargoRequirements(jCollection.aReqPodTypes);
			if (jCollection.aIncludeCOs != null)
			{
				for (int i = 0; i < jCollection.aIncludeCOs.Length; i++)
				{
					DataCO dataCO = DataHandler.GetDataCO(jCollection.aIncludeCOs[i]);
					if (dataCO != null)
					{
						this._dataCos[dataCO.Name] = dataCO;
					}
				}
			}
			if (jCollection.aCondReqs != null || jCollection.aCondForbids != null)
			{
				this.FindMatchingDataCOs(jCollection.aCondReqs, jCollection.aCondForbids, jCollection.bAND);
			}
		}

		public string FriendlyName
		{
			get
			{
				return this._friendlyName ?? this.Name;
			}
		}

		public List<JsonCargoSpec> CargoSpecs { get; private set; }

		public DataCO GetRandomDataCo()
		{
			if (this._dataCos.Count == 0)
			{
				return null;
			}
			int index = UnityEngine.Random.Range(0, this._dataCos.Count);
			return this._dataCos.ElementAt(index).Value;
		}

		public bool CanFitIn(MarketActorConfig targetConfig)
		{
			List<JsonCargoSpec> cargoSpecs = targetConfig.CargoSpecs;
			if (cargoSpecs == null && this.CargoSpecs == null)
			{
				return true;
			}
			if (cargoSpecs != null && this.CargoSpecs == null && cargoSpecs.Count == 1)
			{
				JsonCargoSpec jsonCargoSpec = cargoSpecs.FirstOrDefault<JsonCargoSpec>();
				if (jsonCargoSpec != null && jsonCargoSpec.bIsDefault)
				{
					return true;
				}
			}
			if (this.CargoSpecs != null && cargoSpecs != null)
			{
				using (List<JsonCargoSpec>.Enumerator enumerator = this.CargoSpecs.GetEnumerator())
				{
					while (enumerator.MoveNext())
					{
						JsonCargoSpec requiredSpec = enumerator.Current;
						if (!cargoSpecs.Any((JsonCargoSpec x) => x.strName == requiredSpec.strName))
						{
							return false;
						}
					}
				}
				return true;
			}
			return true;
		}

		public bool IsPartOfCollection(string coName)
		{
			return this._dataCos.ContainsKey(coName);
		}

		public double GetAverageMass()
		{
			if (this._avgMass >= 0.0)
			{
				return this._avgMass;
			}
			this._avgMass = this.GetAverageCond("StatMass");
			return this._avgMass;
		}

		public double GetAveragePrice()
		{
			if (this._avgPrice >= 0.0)
			{
				return this._avgPrice;
			}
			this._avgPrice = this.GetAverageCond("StatBasePrice");
			return this._avgPrice;
		}

		private double GetAverageCond(string condName)
		{
			double num = 0.0;
			int num2 = 0;
			foreach (KeyValuePair<string, DataCO> keyValuePair in this._dataCos)
			{
				double condAmount = keyValuePair.Value.GetCondAmount(condName);
				if (condAmount != 0.0)
				{
					num += condAmount;
					num2++;
				}
			}
			if (num2 == 0)
			{
				num = 0.0;
			}
			else
			{
				num /= (double)num2;
			}
			return num;
		}

		private void FindMatchingDataCOs(string[] aCondReqs, string[] aCondForbids, bool AND)
		{
			foreach (DataCO dataCO in DataHandler.dictDataCOs.Values)
			{
				bool flag = false;
				if (aCondForbids != null)
				{
					foreach (string cond in aCondForbids)
					{
						if (dataCO.HasCond(cond))
						{
							flag = true;
							break;
						}
					}
				}
				if (!flag && aCondReqs != null && aCondReqs.Length > 0)
				{
					bool flag2 = false;
					foreach (string cond2 in aCondReqs)
					{
						if (dataCO.HasCond(cond2))
						{
							flag2 = true;
							if (!AND)
							{
								break;
							}
						}
						else if (AND)
						{
							flag = true;
							break;
						}
					}
					if (!flag2)
					{
						flag = true;
					}
				}
				if (!flag)
				{
					this._dataCos[dataCO.Name] = dataCO;
				}
			}
		}

		public readonly string Name;

		private readonly string _friendlyName;

		private double _avgPrice = -1.0;

		private double _avgMass = -1.0;

		private Dictionary<string, DataCO> _dataCos = new Dictionary<string, DataCO>();
	}
}
