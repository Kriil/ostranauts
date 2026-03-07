using System;

public abstract class DevCommand
{
	public virtual void Execute(string input)
	{
	}

	public string keyword;
}
