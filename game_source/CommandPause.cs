using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class CommandPause : Command
{
	public CommandPause()
	{
		this.defaultCombo = new List<KeyCode>();
		this.defaultCombo.Add(KeyCode.Space);
		this.commandDisplayLabel = "Pause:";
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
		if (CrewSim.Typing)
		{
			return;
		}
		if (base.Down)
		{
			CrewSim.Paused = !CrewSim.Paused;
			AudioManager.am.PlayAudioEmitter("UIPauseBtnOut", false, false);
		}
	}
}
