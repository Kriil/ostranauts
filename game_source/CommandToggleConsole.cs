using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class CommandToggleConsole : Command
{
	public CommandToggleConsole()
	{
		this.defaultCombo = new List<KeyCode>();
		this.defaultCombo.Add(KeyCode.F3);
		this.commandDisplayLabel = "Toggle Debug Console:";
	}
}
