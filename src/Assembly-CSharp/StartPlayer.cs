using System;

public class StartPlayer : DevCommand
{
	public StartPlayer()
	{
		this.keyword = "startplayer";
		DevConsole.commands.Add(this.keyword, this);
	}

	public override void Execute(string input)
	{
		CrewSim.coPlayer.debugStop = false;
		CrewSim.coPlayer.DebugKickstart();
	}
}
