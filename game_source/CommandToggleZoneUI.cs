using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable]
public class CommandToggleZoneUI : Command
{
	public CommandToggleZoneUI()
	{
		this.defaultCombo = new List<KeyCode>();
		this.defaultCombo.Add(KeyCode.N);
		this.commandDisplayLabel = "Toggle zone UI:";
	}

	public void ExternalExecute()
	{
		CrewSim.LowerUI(false);
		if (GUIActionKeySelector.OnKeyDown != null)
		{
			GUIActionKeySelector.OnKeyDown.Invoke(this.defaultCombo.FirstOrDefault<KeyCode>());
		}
	}

	public override void Execute()
	{
		if (CanvasManager.instance == null)
		{
			return;
		}
		if (CrewSim.bRaiseUI)
		{
			return;
		}
		if (base.Down)
		{
			this.ExternalExecute();
		}
	}
}
