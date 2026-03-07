using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class CommandNudgeSelDown : Command
{
	public CommandNudgeSelDown()
	{
		this.defaultCombo = new List<KeyCode>();
		this.defaultCombo.Add(KeyCode.DownArrow);
		this.commandDisplayLabel = "Nudge Selection Down:";
	}

	public override void Execute()
	{
		if (CanvasManager.instance == null)
		{
			return;
		}
		if (!CrewSim.bShipEdit || CrewSim.aSelected.Count == 0)
		{
			return;
		}
		if (CrewSim.Typing)
		{
			return;
		}
		if (!base.Down)
		{
			return;
		}
		CondOwner[] array = new CondOwner[CrewSim.aSelected.Count];
		CrewSim.aSelected.CopyTo(array);
		CommandNudgeSelUp.Nudge(array, new Vector3(0f, -1f, 0f));
	}
}
