using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class CommandPanCameraRight : Command
{
	public CommandPanCameraRight()
	{
		this.defaultCombo = new List<KeyCode>();
		this.defaultCombo.Add(KeyCode.D);
		this.commandDisplayLabel = "Camera Right:";
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
			CrewSim.objInstance.delX += 1f;
		}
	}
}
