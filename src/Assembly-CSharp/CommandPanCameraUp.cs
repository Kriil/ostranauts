using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class CommandPanCameraUp : Command
{
	public CommandPanCameraUp()
	{
		this.defaultCombo = new List<KeyCode>();
		this.defaultCombo.Add(KeyCode.W);
		this.commandDisplayLabel = "Camera Up:";
	}

	public override void Execute()
	{
		if (CanvasManager.instance == null)
		{
			return;
		}
		if (CrewSim.CanvasManager.State == CanvasManager.GUIState.SHIPGUI)
		{
			return;
		}
		if (CrewSim.Typing)
		{
			return;
		}
		if (base.Held)
		{
			CrewSim.objInstance.delY += 1f;
		}
	}
}
