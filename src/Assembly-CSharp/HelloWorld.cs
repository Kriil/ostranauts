using System;

public class HelloWorld : DevCommand
{
	public HelloWorld()
	{
		this.keyword = "helloworld!";
		DevConsole.commands.Add(this.keyword, this);
	}

	public override void Execute(string input)
	{
		DevConsole.Output("<color=#41D0DDFF>>> " + input + "</color>");
		DevConsole.Output("Right back at ya bud!");
	}
}
