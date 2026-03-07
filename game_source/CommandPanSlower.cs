using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class CommandPanSlower : Command
{
	public CommandPanSlower()
	{
		this.defaultCombo = new List<KeyCode>();
		this.defaultCombo.Add(KeyCode.LeftAlt);
		this.commandDisplayLabel = "Pan camera slower:";
		this.currentCombos = new List<List<KeyCode>>();
		this.currentCombos.Add(new List<KeyCode>
		{
			KeyCode.LeftAlt
		});
		this.currentCombos.Add(new List<KeyCode>
		{
			KeyCode.RightAlt
		});
	}
}
