using System;
using System.Collections.Generic;

namespace Ostranauts.ShipGUIs.Trade.Models
{
	public class TradeUnavailableDataContainer
	{
		public Dictionary<string, TradeUnavailableDataRow> DataRows = new Dictionary<string, TradeUnavailableDataRow>();

		public bool HasFinishedLoading;
	}
}
