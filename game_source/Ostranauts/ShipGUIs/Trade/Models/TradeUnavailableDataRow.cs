using System;

namespace Ostranauts.ShipGUIs.Trade.Models
{
	public class TradeUnavailableDataRow
	{
		public TradeUnavailableDataRow(CondOwner coVendor, CondOwner coItem, CondTrigger ctThemBuy)
		{
			this.name = coItem.FriendlyName;
			this.price = coVendor.GetCondAmount("DiscountBuy");
			if (this.price == 0.0)
			{
				this.price = 1.0;
			}
			this.price *= coItem.GetTotalPrice(ctThemBuy, false, false);
			Item component = coItem.GetComponent<Item>();
			if (component != null)
			{
				this.imgPath = component.ImgOverride + ".png";
			}
			else
			{
				this.imgPath = "missing.png";
			}
		}

		public readonly string name;

		public readonly double price;

		public readonly string imgPath;
	}
}
