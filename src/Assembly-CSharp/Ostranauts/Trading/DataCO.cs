using System;
using System.Collections.Generic;
using System.Linq;

namespace Ostranauts.Trading
{
	public class DataCO
	{
		public DataCO(JsonCondOwner jco)
		{
			this.JCO = jco;
			foreach (string value in this.JCO.aStartingConds)
			{
				DataCond item = new DataCond(value);
				this._condPool.Add(item);
			}
		}

		public DataCO(JsonCondOwner jco, JsonCOOverlay jcoo) : this(jco)
		{
			if (jcoo == null)
			{
				return;
			}
			this.JCOO = jcoo;
			Loot loot = DataHandler.GetLoot(this.JCOO.strCondLoot);
			if (loot != null)
			{
				List<string> lootNames = loot.GetLootNames(null, false, null);
				foreach (string value in lootNames)
				{
					this.AddCondToList(new DataCond(value));
				}
			}
		}

		public string Name
		{
			get
			{
				return (this.JCOO == null) ? this.JCO.strName : this.JCOO.strName;
			}
		}

		private void AddCondToList(DataCond dCond)
		{
			foreach (DataCond dataCond in this._condPool)
			{
				if (dCond.CondName == dataCond.CondName)
				{
					dataCond.JoinConds(dCond);
					return;
				}
			}
			this._condPool.Add(dCond);
		}

		public bool HasCond(string cond)
		{
			return this._condPool.Any((DataCond x) => x.CondName == cond);
		}

		public double GetCondAmount(string condName)
		{
			DataCond dataCond = this._condPool.FirstOrDefault((DataCond x) => x.CondName == condName);
			if (dataCond != null)
			{
				return (double)dataCond.Amount;
			}
			return 0.0;
		}

		public JsonCondOwner JCO;

		public JsonCOOverlay JCOO;

		private List<DataCond> _condPool = new List<DataCond>();
	}
}
