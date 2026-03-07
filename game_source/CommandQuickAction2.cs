using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class CommandQuickAction2 : Command
{
	public CommandQuickAction2()
	{
		this.defaultCombo = new List<KeyCode>();
		this.defaultCombo.Add(KeyCode.Alpha2);
		this.commandDisplayLabel = "Quick Action 2:";
	}
}
