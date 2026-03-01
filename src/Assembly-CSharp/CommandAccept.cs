using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class CommandAccept : Command
{
	public CommandAccept()
	{
		this.defaultCombo = new List<KeyCode>();
		this.defaultCombo.Add(KeyCode.Return);
		this.currentCombos = new List<List<KeyCode>>();
		this.currentCombos.Add(new List<KeyCode>
		{
			KeyCode.Return
		});
		this.currentCombos.Add(new List<KeyCode>
		{
			KeyCode.KeypadEnter
		});
		this.commandDisplayLabel = "Accept/Enter:";
	}
}
