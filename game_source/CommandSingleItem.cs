using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class CommandSingleItem : Command
{
	public CommandSingleItem()
	{
		this.defaultCombo = new List<KeyCode>();
		this.defaultCombo.Add(KeyCode.LeftControl);
		this.currentCombos = new List<List<KeyCode>>();
		this.currentCombos.Add(new List<KeyCode>
		{
			KeyCode.LeftControl
		});
		this.currentCombos.Add(new List<KeyCode>
		{
			KeyCode.RightControl
		});
		this.commandDisplayLabel = "Grab Singular:";
	}
}
