using System;

[Serializable]
public class JsonRaceResultSave
{
	public string strTrackName { get; set; }

	public string strPilotName { get; set; }

	public int nPointsEarned { get; set; }

	public int nFinishingPosition { get; set; }

	public JsonLapTime[] aLapTimes { get; set; }
}
