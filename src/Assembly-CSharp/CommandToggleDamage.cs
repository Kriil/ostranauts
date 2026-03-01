using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class CommandToggleDamage : Command
{
	public CommandToggleDamage()
	{
		this.defaultCombo = new List<KeyCode>();
		this.defaultCombo.Add(KeyCode.X);
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
			CrewSim.guiPDA.CycleVizMode();
		}
	}
}
