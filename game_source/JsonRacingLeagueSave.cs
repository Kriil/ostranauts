using System;

[Serializable]
public class JsonRacingLeagueSave
{
	public string strLeagueName { get; set; }

	public JsonRacerSave[] aRacerSaves { get; set; }
}
