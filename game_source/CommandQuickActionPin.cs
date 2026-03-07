using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class CommandQuickActionPin : Command
{
	public CommandQuickActionPin()
	{
		this.defaultCombo = new List<KeyCode>();
		this.defaultCombo.Add(KeyCode.Alpha7);
		this.commandDisplayLabel = "Quick Action Toggle Pin:";
	}
}
