using System;

namespace Ostranauts.Trading
{
	public class GUIShipMarketDTO
	{
		public bool IsEmpty
		{
			get
			{
				return this.Stock == 0 && this.MaxInventory == 0;
			}
		}

		public double TransactionPrice
		{
			get
			{
				return this.AvgPrice * (double)this.PriceModifier;
			}
		}

		public DataCoCollection DataCoCollection;

		public int Stock;

		public int MaxInventory;

		public float PriceModifier;

		public double AvgPrice;

		public double AvgMass;
	}
}
