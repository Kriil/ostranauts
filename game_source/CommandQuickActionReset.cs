using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class CommandQuickActionReset : Command
{
	public CommandQuickActionReset()
	{
		this.defaultCombo = new List<KeyCode>();
		this.defaultCombo.Add(KeyCode.Alpha6);
		this.commandDisplayLabel = "Quick Action Reset:";
	}
}
