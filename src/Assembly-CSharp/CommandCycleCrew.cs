using System;
using System.Collections.Generic;
using Ostranauts.Core;
using Ostranauts.Objectives;
using UnityEngine;

[Serializable]
public class CommandCycleCrew : Command
{
	public CommandCycleCrew()
	{
		this.defaultCombo = new List<KeyCode>();
		this.defaultCombo.Add(KeyCode.CapsLock);
		this.commandDisplayLabel = "Cycle Crew:";
	}

	public override void Execute()
	{
		if (base.Down)
		{
			if (CrewSim.objInstance == null || CanvasManager.instance.NState != CanvasManager.GUIState.NORMAL)
			{
				return;
			}
			CrewSim.objInstance.CycleCrew(null);
			CrewSim.coPlayer.SetCondAmount("TutorialCrewCycleComplete", 1.0, 0.0);
			MonoSingleton<ObjectiveTracker>.Instance.CheckObjective(CrewSim.coPlayer.strID);
		}
	}
}
