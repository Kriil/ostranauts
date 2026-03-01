using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class CommandUIPanelBottom : Command
{
	public CommandUIPanelBottom()
	{
		this.defaultCombo = new List<KeyCode>();
		this.currentCombos = new List<List<KeyCode>>();
		this.currentCombos.Add(new List<KeyCode>
		{
			KeyCode.LeftShift,
			KeyCode.DownArrow
		});
		this.currentCombos.Add(new List<KeyCode>
		{
			KeyCode.RightShift,
			KeyCode.DownArrow
		});
		this.commandDisplayLabel = "Switch Control Panels (Bottom):";
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
			CrewSim.SwitchUI("strGUIPrefabBottom");
		}
	}
}
