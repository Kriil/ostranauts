using System;

public class StopPlayer : DevCommand
{
	public StopPlayer()
	{
		this.keyword = "stopplayer";
		DevConsole.commands.Add(this.keyword, this);
	}

	public override void Execute(string input)
	{
		CrewSim.coPlayer.debugStop = true;
	}
}
