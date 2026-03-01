using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class CommandPDAToggle : Command
{
	public CommandPDAToggle()
	{
		this.defaultCombo = new List<KeyCode>();
		this.defaultCombo.Add(KeyCode.Tab);
		this.commandDisplayLabel = "Toggle PDA:";
	}

	public override void Execute()
	{
		if (CanvasManager.instance == null)
		{
			return;
		}
		if (CrewSim.bRaiseUI)
		{
			return;
		}
		if (CrewSim.objInstance != null && base.Down)
		{
			CrewSim.guiPDA.ToggleHome();
		}
	}
}
