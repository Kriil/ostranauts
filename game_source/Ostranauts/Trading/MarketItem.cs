using System;

namespace Ostranauts.Trading
{
	public class MarketItem
	{
		public MarketItem()
		{
		}

		public MarketItem(DataCoCollection coCollection, DataCO dataCo)
		{
			if (coCollection != null)
			{
				this.COCollectionName = coCollection.Name;
			}
			if (dataCo != null)
			{
				this.COName = dataCo.Name;
			}
		}

		public MarketItem(DataCoCollection coCollection)
		{
			this.COCollectionName = coCollection.Name;
			DataCO randomDataCo = coCollection.GetRandomDataCo();
			if (randomDataCo != null)
			{
				this.COName = randomDataCo.Name;
			}
		}

		public MarketItem(string coName)
		{
			DataCoCollection dataCoCollectionForCO = DataHandler.GetDataCoCollectionForCO(coName);
			this.COCollectionName = dataCoCollectionForCO.Name;
			this.COName = coName;
		}

		public MarketItem Clone()
		{
			return new MarketItem
			{
				COName = this.COName,
				COCollectionName = this.COCollectionName
			};
		}

		public string GetJsonName()
		{
			string text = string.Empty;
			if (this.COCollectionName != null)
			{
				text = this.COCollectionName;
			}
			if (this.COName != null)
			{
				text = text + "|" + this.COName;
			}
			return text;
		}

		public string COName;

		public string COCollectionName;
	}
}
