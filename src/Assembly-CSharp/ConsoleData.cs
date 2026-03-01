using System;
using System.IO;
using UnityEngine;

public class ConsoleData
{
	public static ConsoleData CreateFromJSON(string jsonFileName)
	{
		jsonFileName = Application.persistentDataPath + "/" + jsonFileName;
		string text;
		if (File.Exists(jsonFileName))
		{
			text = File.ReadAllText(jsonFileName);
			if (text != null)
			{
				return JsonUtility.FromJson<ConsoleData>(text);
			}
		}
		Debug.LogWarning("Console Parameters JSON not found! Generating default " + jsonFileName);
		ConsoleData consoleData = new ConsoleData();
		text = JsonUtility.ToJson(consoleData, true);
		File.WriteAllText(jsonFileName, text);
		return consoleData;
	}

	public string consoleTitle = "Ostranauts Debug Console";

	public string startTxt = "=== begin log ===";

	public string fileName = "recent-log.txt";

	public bool enableLog;

	public bool enableAssert = true;

	public bool enableError = true;

	public bool enableException = true;

	public bool enableWarning = true;

	public bool enableUnknown = true;

	public bool saveToFile;

	public int textSize = 16;

	public int maxMessage = 700;

	public int maxTotal = 16382;

	public int popUpSize = 25;
}
