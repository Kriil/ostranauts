using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class CommandDecreaseTimeScale : Command
{
	public CommandDecreaseTimeScale()
	{
		this.defaultCombo = new List<KeyCode>();
		this.defaultCombo.Add(KeyCode.LeftBracket);
		this.commandDisplayLabel = "Decrease timescale:";
	}

	public override void Execute()
	{
		if (base.Down)
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
			CrewSim.TimeScaleMult(0.5f);
			AudioManager.am.PlayAudioEmitter("UIPauseBtnOut", false, false);
		}
	}
}
