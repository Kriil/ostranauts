using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class CommandZoomOut : Command
{
	public CommandZoomOut()
	{
		this.defaultCombo = new List<KeyCode>();
		this.defaultCombo.Add(KeyCode.Minus);
		this.commandDisplayLabel = "Zoom Camera Out:";
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
			CrewSim.objInstance.delZ += 1f;
		}
	}
}
