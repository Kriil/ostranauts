using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class CommandZoneAlternate : Command
{
	public CommandZoneAlternate()
	{
		this.defaultCombo = new List<KeyCode>();
		this.defaultCombo.Add(KeyCode.C);
		this.commandDisplayLabel = "Zone Subtract:";
	}
}
