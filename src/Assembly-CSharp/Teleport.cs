using System;
using UnityEngine;

public class Teleport : DevCommand
{
	public Teleport()
	{
		this.keyword = "teleport";
		DevConsole.commands.Add(this.keyword, this);
	}

	public override void Execute(string input)
	{
		UnityEngine.Object.FindObjectOfType<DebugTeleport>().StartMouseTeleport();
	}
}
