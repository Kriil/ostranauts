using System;

[Serializable]
public class JsonLapTime
{
	public string strTrackName { get; set; }

	public double fTotalTime { get; set; }

	public string[] aWayPointIDs { get; set; }

	public double[] aWayPointTimes { get; set; }
}
