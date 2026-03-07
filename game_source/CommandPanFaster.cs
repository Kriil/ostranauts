using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class CommandPanFaster : Command
{
	public CommandPanFaster()
	{
		this.defaultCombo = new List<KeyCode>();
		this.defaultCombo.Add(KeyCode.LeftShift);
		this.commandDisplayLabel = "Pan camera faster:";
		this.currentCombos = new List<List<KeyCode>>();
		this.currentCombos.Add(new List<KeyCode>
		{
			KeyCode.LeftShift
		});
		this.currentCombos.Add(new List<KeyCode>
		{
			KeyCode.RightShift
		});
	}
}
