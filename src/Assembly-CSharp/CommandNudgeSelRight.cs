using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class CommandNudgeSelRight : Command
{
	public CommandNudgeSelRight()
	{
		this.defaultCombo = new List<KeyCode>();
		this.defaultCombo.Add(KeyCode.RightArrow);
		this.commandDisplayLabel = "Nudge Selection Right:";
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
		CommandNudgeSelUp.Nudge(array, new Vector3(1f, 0f, 0f));
	}
}
