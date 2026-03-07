using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class CommandShipCW : Command
{
	public CommandShipCW()
	{
		this.defaultCombo = new List<KeyCode>();
		this.defaultCombo.Add(KeyCode.E);
		this.commandDisplayLabel = "Turn CW:";
	}
}
