using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class CommandShipAttitude : Command
{
	public CommandShipAttitude()
	{
		this.defaultCombo = new List<KeyCode>();
		this.defaultCombo.Add(KeyCode.R);
		this.commandDisplayLabel = "Attitude:";
	}
}
