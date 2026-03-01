using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class CommandCentrePlayer : Command
{
	public CommandCentrePlayer()
	{
		this.defaultCombo = new List<KeyCode>();
		this.defaultCombo.Add(KeyCode.Home);
		this.commandDisplayLabel = "Centre camera:";
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
		if (base.Down)
		{
			CrewSim.objInstance.CamCenter(CrewSim.GetSelectedCrew());
			CrewSim.objInstance.camFollow = true;
		}
	}
}
