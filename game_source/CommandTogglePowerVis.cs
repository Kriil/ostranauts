using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class CommandTogglePowerVis : Command
{
	public CommandTogglePowerVis()
	{
		this.defaultCombo = new List<KeyCode>();
		this.defaultCombo.Add(KeyCode.L);
		this.commandDisplayLabel = "Toggle PDA Vizor:";
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
			CrewSim.guiPDA.ToggleVizUI();
		}
	}
}
