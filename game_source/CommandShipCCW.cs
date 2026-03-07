using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class CommandShipCCW : Command
{
	public CommandShipCCW()
	{
		this.defaultCombo = new List<KeyCode>();
		this.defaultCombo.Add(KeyCode.Q);
		this.commandDisplayLabel = "Turn CCW:";
	}
}
