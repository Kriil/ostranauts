using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class CommandFlyRight : Command
{
	public CommandFlyRight()
	{
		this.defaultCombo = new List<KeyCode>();
		this.defaultCombo.Add(KeyCode.D);
		this.currentCombos = new List<List<KeyCode>>();
		this.currentCombos.Add(new List<KeyCode>
		{
			KeyCode.D
		});
		this.currentCombos.Add(new List<KeyCode>
		{
			KeyCode.RightArrow
		});
		this.commandDisplayLabel = "Thrust Right:";
	}
}
