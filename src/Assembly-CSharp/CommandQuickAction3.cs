using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class CommandQuickAction3 : Command
{
	public CommandQuickAction3()
	{
		this.defaultCombo = new List<KeyCode>();
		this.defaultCombo.Add(KeyCode.Alpha3);
		this.commandDisplayLabel = "Quick Action 3:";
	}
}
