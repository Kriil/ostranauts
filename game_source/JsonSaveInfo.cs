using System;

// Lightweight save-slot summary. This is likely the metadata shown in the
// save/load menus before the full JsonGameSave is opened.
public class JsonSaveInfo
{
	// Likely the save-info record id.
	public string strName { get; set; }

	public string playerName { get; set; }

	public string shipName { get; set; }

	public string saveNote { get; set; }

	public string formerOccupation { get; set; }

	public string nationality { get; set; }

	public string version { get; set; }

	public double age { get; set; }

	public double money { get; set; }

	public float playTimeElapsed { get; set; }

	public double simTimeElapsed { get; set; }

	public double simTimeCurrent { get; set; }

	public string realWorldTime { get; set; }

	public long epochCreationTime { get; set; }

	public string seedId { get; set; }

	public int autoSaveCounter { get; set; }

	public string strSaveLog { get; set; }

	public string[] tutorialsCurrent { get; set; }

	public string[] tutorialsCompleted { get; set; }
}
