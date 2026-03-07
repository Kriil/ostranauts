using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class CommandFlyLeft : Command
{
	public CommandFlyLeft()
	{
		this.defaultCombo = new List<KeyCode>();
		this.defaultCombo.Add(KeyCode.A);
		this.currentCombos = new List<List<KeyCode>>();
		this.currentCombos.Add(new List<KeyCode>
		{
			KeyCode.A
		});
		this.currentCombos.Add(new List<KeyCode>
		{
			KeyCode.LeftArrow
		});
		this.commandDisplayLabel = "Thrust Left:";
	}
}
