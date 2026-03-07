using System;

// Serializable ship log entry used by the ship event/history feed.
public class JsonShipLog
{
	public double fEpoch { get; set; }

	public string strEntry { get; set; }

	public bool bShowEpoch { get; set; }

	// Shallow copy is enough because this payload only stores scalar values.
	public JsonShipLog Clone()
	{
		return (JsonShipLog)base.MemberwiseClone();
	}

	// Convenience factory for logging ship events without manual object setup.
	public static JsonShipLog Make(string strEntry, double fEpoch = 0.0, bool bShowEpoch = false)
	{
		if (string.IsNullOrEmpty(strEntry))
		{
			strEntry = string.Empty;
		}
		return new JsonShipLog
		{
			strEntry = strEntry,
			fEpoch = fEpoch,
			bShowEpoch = bShowEpoch
		};
	}
}
