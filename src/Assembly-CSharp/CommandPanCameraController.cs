using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class CommandPanCameraController : Command
{
	public CommandPanCameraController()
	{
		this.defaultCombo = new List<KeyCode>();
		this.commandDisplayLabel = "Camera Pan (Joystick):";
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
		CrewSim.objInstance.delX += Input.GetAxis("Joy1");
		CrewSim.objInstance.delY -= Input.GetAxis("Joy2");
	}
}
