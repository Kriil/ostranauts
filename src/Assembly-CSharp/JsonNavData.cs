using System;

// Serialized navigation/autopilot path state.
// This is embedded in JsonShipSitu so a ship can resume plotted burns and
// waypoint progression after loading.
public class JsonNavData
{
	// Target ship/body reg id, torching state, flow multiplier, plotted points, and atmosphere flag.
	public string strRegID { get; set; }

	public bool bIsTorching { get; set; }

	public double fFlowMultPlot { get; set; }

	public JsonNavDataPoint[] aPoints { get; set; }

	public bool bInAtmo { get; set; }
}
