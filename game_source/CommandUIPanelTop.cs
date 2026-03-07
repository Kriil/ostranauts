using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class CommandUIPanelTop : Command
{
	public CommandUIPanelTop()
	{
		this.defaultCombo = new List<KeyCode>();
		this.currentCombos = new List<List<KeyCode>>();
		this.currentCombos.Add(new List<KeyCode>
		{
			KeyCode.LeftShift,
			KeyCode.UpArrow
		});
		this.currentCombos.Add(new List<KeyCode>
		{
			KeyCode.RightShift,
			KeyCode.UpArrow
		});
		this.commandDisplayLabel = "Switch Control Panels (Top):";
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
			CrewSim.SwitchUI("strGUIPrefabTop");
		}
	}
}
