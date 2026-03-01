using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class CommandUIPanelLeft : Command
{
	public CommandUIPanelLeft()
	{
		this.defaultCombo = new List<KeyCode>();
		this.currentCombos = new List<List<KeyCode>>();
		this.currentCombos.Add(new List<KeyCode>
		{
			KeyCode.LeftShift,
			KeyCode.LeftArrow
		});
		this.currentCombos.Add(new List<KeyCode>
		{
			KeyCode.RightShift,
			KeyCode.LeftArrow
		});
		this.commandDisplayLabel = "Switch Control Panels (Left):";
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
			CrewSim.SwitchUI("strGUIPrefabLeft");
		}
	}
}
