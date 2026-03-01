using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class CommandMovePlayer : Command
{
	public CommandMovePlayer()
	{
		this.defaultCombo = new List<KeyCode>();
		this.commandDisplayLabel = "Player Move (Joystick):";
	}

	public override void Execute()
	{
		if (CanvasManager.instance == null)
		{
			return;
		}
		if (CrewSim.CanvasManager.State == CanvasManager.GUIState.SHIPGUI)
		{
			return;
		}
		if (CrewSim.Typing)
		{
			return;
		}
		Vector2 vector = new Vector2(Input.GetAxis("Joy4"), -Input.GetAxis("Joy5"));
		if (vector.magnitude > 0.5f)
		{
			vector += CrewSim.coPlayer.GetPos(null, false);
			Tile tileAtWorldCoords = CrewSim.shipCurrentLoaded.GetTileAtWorldCoords1(vector.x, vector.y, true, true);
			CrewSim.coPlayer.AIIssueOrder(null, null, true, tileAtWorldCoords, vector.x, vector.y);
			Vector2 vector2 = Camera.main.WorldToScreenPoint(vector);
		}
	}
}
