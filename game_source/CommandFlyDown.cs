using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class CommandFlyDown : Command
{
	public CommandFlyDown()
	{
		this.defaultCombo = new List<KeyCode>();
		this.defaultCombo.Add(KeyCode.S);
		this.currentCombos = new List<List<KeyCode>>();
		this.currentCombos.Add(new List<KeyCode>
		{
			KeyCode.S
		});
		this.currentCombos.Add(new List<KeyCode>
		{
			KeyCode.DownArrow
		});
		this.commandDisplayLabel = "Thrust Down:";
	}
}
