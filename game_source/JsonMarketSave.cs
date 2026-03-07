using System;
using System.Collections.Generic;
using Ostranauts.Trading;

[Serializable]
// Serialized ship market registry used by the trading/economy subsystem.
// This captures each market's runtime state so prices and stock can persist.
public class JsonMarketSave
{
	public JsonMarketSave()
	{
	}

	// Converts live ShipMarket objects into pure save DTOs.
	public JsonMarketSave(Dictionary<string, ShipMarket> market)
	{
		this.mapMarket = new Dictionary<string, JsonShipMarketSave>();
		foreach (KeyValuePair<string, ShipMarket> keyValuePair in market)
		{
			this.mapMarket[keyValuePair.Key] = keyValuePair.Value.GetJson();
		}
	}

	// Keyed by market id, likely the same ids used by ships/stations that host commerce.
	public Dictionary<string, JsonShipMarketSave> mapMarket { get; set; }
}
