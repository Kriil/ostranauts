using System;
using System.Collections.Generic;
using Ostranauts.Core;
using UnityEngine;

[Serializable]
public class CommandSave : Command
{
	public CommandSave()
	{
		this.defaultCombo = new List<KeyCode>();
		this.defaultCombo.Add(KeyCode.F5);
		this.commandDisplayLabel = "Save Game:";
	}

	public override void Execute()
	{
		if (!base.Down)
		{
			return;
		}
		if (CrewSim.bUILock)
		{
			return;
		}
		MonoSingleton<LoadManager>.Instance.AutoSave(true);
	}
}
