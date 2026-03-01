using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class CommandShipLockW : Command
{
	public CommandShipLockW()
	{
		this.defaultCombo = new List<KeyCode>();
		this.defaultCombo.Add(KeyCode.LeftControl);
		this.defaultCombo.Add(KeyCode.W);
		this.commandDisplayLabel = "Locking Thrust:";
	}
}
