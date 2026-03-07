using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class CommandPanCameraLeft : Command
{
	public CommandPanCameraLeft()
	{
		this.defaultCombo = new List<KeyCode>();
		this.defaultCombo.Add(KeyCode.A);
		this.commandDisplayLabel = "Camera Left:";
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
			CrewSim.objInstance.delX -= 1f;
		}
	}
}
