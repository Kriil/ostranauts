using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class CommandUIPanelRight : Command
{
	public CommandUIPanelRight()
	{
		this.defaultCombo = new List<KeyCode>();
		this.currentCombos = new List<List<KeyCode>>();
		this.currentCombos.Add(new List<KeyCode>
		{
			KeyCode.LeftShift,
			KeyCode.RightArrow
		});
		this.currentCombos.Add(new List<KeyCode>
		{
			KeyCode.RightShift,
			KeyCode.RightArrow
		});
		this.commandDisplayLabel = "Switch Control Panels (Right):";
	}

	public override void Execute()
	{
		if (CanvasManager.instance == null)
		{
			return;
		}
		if (!CrewSim.bRaiseUI)
		{
			return;
		}
		if (CrewSim.objInstance != null && base.Down)
		{
			CrewSim.SwitchUI("strGUIPrefabRight");
		}
	}
}
