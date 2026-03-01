using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class CommandQuickActionExpand : Command
{
	public CommandQuickActionExpand()
	{
		this.defaultCombo = new List<KeyCode>();
		this.defaultCombo.Add(KeyCode.Alpha5);
		this.commandDisplayLabel = "Quick Action Expand:";
	}
}
