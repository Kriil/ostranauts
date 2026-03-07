using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class CommandFlyUp : Command
{
	public CommandFlyUp()
	{
		this.defaultCombo = new List<KeyCode>();
		this.defaultCombo.Add(KeyCode.W);
		this.currentCombos = new List<List<KeyCode>>();
		this.currentCombos.Add(new List<KeyCode>
		{
			KeyCode.W
		});
		this.currentCombos.Add(new List<KeyCode>
		{
			KeyCode.UpArrow
		});
		this.commandDisplayLabel = "Thrust Up:";
	}
}
