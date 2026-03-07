using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class CommandScreenshot : Command
{
	public CommandScreenshot()
	{
		this.defaultCombo = new List<KeyCode>();
		this.defaultCombo.Add(KeyCode.F2);
		this.commandDisplayLabel = "Take Screenshot:";
	}
}
