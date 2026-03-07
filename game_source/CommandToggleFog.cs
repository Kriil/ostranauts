using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class CommandToggleFog : Command
{
	public CommandToggleFog()
	{
		this.defaultCombo = new List<KeyCode>();
		this.defaultCombo.Add(KeyCode.F1);
		this.commandDisplayLabel = "Toggle Fog Of War (Experimental):";
	}
}
