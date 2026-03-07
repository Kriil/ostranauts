using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class CommandRotateCCW : Command
{
	public CommandRotateCCW()
	{
		this.defaultCombo = new List<KeyCode>();
		this.defaultCombo.Add(KeyCode.Q);
		this.commandDisplayLabel = "Rotate Camera CCW:";
	}

	public override void Execute()
	{
		if (CanvasManager.instance == null)
		{
			return;
		}
		if (CanvasManager.instance.State == CanvasManager.GUIState.GAMEOVER)
		{
			return;
		}
		if (CrewSim.CanvasManager.State == CanvasManager.GUIState.SHIPGUI)
		{
			return;
		}
		if (!CrewSim.bEnableDebugCommands)
		{
			return;
		}
		if (base.Down)
		{
			for (int i = 0; i < 3; i++)
			{
				CrewSim.shipCurrentLoaded.RotateCW();
				if (CrewSim.objInstance.camMain != null)
				{
					Vector3 position = CrewSim.objInstance.camMain.transform.position;
					Vector3 position2 = new Vector3(position.y, -position.x, position.z);
					CrewSim.objInstance.camMain.transform.position = position2;
				}
			}
		}
	}
}
