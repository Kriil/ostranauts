using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class CommandQuickAction4 : Command
{
	public CommandQuickAction4()
	{
		this.defaultCombo = new List<KeyCode>();
		this.defaultCombo.Add(KeyCode.Alpha4);
		this.commandDisplayLabel = "Quick Action 4:";
	}
}
