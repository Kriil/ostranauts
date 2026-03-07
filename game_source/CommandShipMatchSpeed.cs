using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class CommandShipMatchSpeed : Command
{
	public CommandShipMatchSpeed()
	{
		this.defaultCombo = new List<KeyCode>();
		this.defaultCombo.Add(KeyCode.K);
		this.commandDisplayLabel = "Toggle station keeping:";
	}

	public override void Execute()
	{
		if (!base.Down)
		{
			return;
		}
		GUIOrbitDraw guiorbitDraw = null;
		if (CrewSim.goUI != null)
		{
			guiorbitDraw = CrewSim.goUI.GetComponent<GUIOrbitDraw>();
		}
		if (guiorbitDraw == null || guiorbitDraw.IsPDANav || guiorbitDraw.chkStationKeeping == null)
		{
			return;
		}
		guiorbitDraw.chkStationKeeping.isOn = !guiorbitDraw.chkStationKeeping.isOn;
	}
}
