using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class CommandQuickMove : Command
{
	public CommandQuickMove()
	{
		this.defaultCombo = new List<KeyCode>();
		this.defaultCombo.Add(KeyCode.LeftShift);
		this.currentCombos = new List<List<KeyCode>>();
		this.currentCombos.Add(new List<KeyCode>
		{
			KeyCode.LeftShift
		});
		this.currentCombos.Add(new List<KeyCode>
		{
			KeyCode.RightShift
		});
		this.commandDisplayLabel = "Quick Move Item:";
	}
}
