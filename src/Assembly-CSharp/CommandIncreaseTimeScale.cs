using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class CommandIncreaseTimeScale : Command
{
	public CommandIncreaseTimeScale()
	{
		this.defaultCombo = new List<KeyCode>();
		this.defaultCombo.Add(KeyCode.RightBracket);
		this.commandDisplayLabel = "Increase timescale:";
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
			CrewSim.TimeScaleMult(2f);
			AudioManager.am.PlayAudioEmitter("UIPauseBtnOut", false, false);
		}
	}
}
