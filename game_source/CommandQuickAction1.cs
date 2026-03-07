using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class CommandQuickAction1 : Command
{
	public CommandQuickAction1()
	{
		this.defaultCombo = new List<KeyCode>();
		this.defaultCombo.Add(KeyCode.Alpha1);
		this.commandDisplayLabel = "Quick Action 1:";
	}
}
