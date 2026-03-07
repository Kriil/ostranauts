using System;
using System.Collections.Generic;
using System.Linq;

namespace Ostranauts.ShipGUIs.Trade.Models
{
	public class TradeRowData
	{
		public TradeRowData(CondOwner condOwner, CondTrigger ctBuySell, CondOwner themCo)
		{
			this.CoItem = condOwner;
			this.CtVendor = ctBuySell;
			this.CoThem = themCo;
			List<CondOwner> cos = this.CoItem.GetCOs(true, GUITrade.ctSellables);
			if (cos != null)
			{
				List<CondOwner> list = new List<CondOwner>();
				foreach (CondOwner condOwner2 in cos)
				{
					if (list.IndexOf(condOwner2) < 0)
					{
						CondOwner.NullSafeAddRange(ref list, condOwner2.GetCOs(true, GUITrade.ctSellables));
						this.ContaineredCOs.Add(new TradeRowData(condOwner2, ctBuySell, themCo));
					}
				}
			}
		}

		public TradeRowData(JsonZone jZone, List<CondOwner> containered, CondTrigger ctBuySell, CondOwner themCo)
		{
			this.JsonZone = jZone;
			this.CtVendor = ctBuySell;
			this.CoThem = themCo;
			if (containered != null)
			{
				foreach (CondOwner condOwner in containered)
				{
					this.ContaineredCOs.Add(new TradeRowData(condOwner, ctBuySell, themCo));
				}
			}
		}

		public bool IsEmptyOrPoweredOffDataContainer
		{
			get
			{
				if (this.CoItem == null)
				{
					return false;
				}
				bool flag = this.CoItem.GetSlots(true, Slots.SortOrder.HELD_FIRST).Any((Slot x) => x.strName == "data");
				return flag && (this.CTDataHidden.Triggered(this.CoItem, null, true) || this.ContaineredCOs.Count == 0);
			}
		}

		private CondTrigger CTDataHidden
		{
			get
			{
				if (this._ctDataHidden == null)
				{
					this._ctDataHidden = DataHandler.GetCondTrigger("TIsDataHiddenInside");
				}
				return this._ctDataHidden;
			}
		}

		public CondOwner CoItem;

		public CondOwner CoThem;

		public CondTrigger CtVendor;

		public CondTrigger _ctDataHidden;

		public readonly List<TradeRowData> ContaineredCOs = new List<TradeRowData>();

		public JsonZone JsonZone;
	}
}
