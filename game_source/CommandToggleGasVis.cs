using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class CommandToggleGasVis : Command
{
	public CommandToggleGasVis()
	{
		this.defaultCombo = new List<KeyCode>();
		this.defaultCombo.Add(KeyCode.G);
		this.commandDisplayLabel = "Toggle Gas Vis:";
	}
}
