using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class CommandForceWalk : Command
{
	public CommandForceWalk()
	{
		this.defaultCombo = new List<KeyCode>();
		this.defaultCombo.Add(KeyCode.F);
		this.commandDisplayLabel = "Force Walk:";
	}

	public override void Execute()
	{
		if (CrewSim.objInstance == null)
		{
			return;
		}
	}
}
