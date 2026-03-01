using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class CommandToggleFilters : Command
{
	public CommandToggleFilters()
	{
		this.defaultCombo = new List<KeyCode>();
		this.defaultCombo.Add(KeyCode.F8);
		this.commandDisplayLabel = "Toggle Visual Filters:";
	}

	public override void Execute()
	{
		if (this.gameRenderer == null)
		{
			return;
		}
		if (CrewSim.objInstance == null)
		{
			return;
		}
		if (base.Down && CrewSim.bEnableDebugCommands)
		{
			this.gameRenderer.SwapMode();
		}
	}

	public GameRenderer gameRenderer;
}
