using System;

[Serializable]
public class JsonRacerSave
{
	public string strPilotName { get; set; }

	public double fStatPiloting { get; set; }

	public JsonRaceResultSave[] aRaceResults { get; set; }
}
