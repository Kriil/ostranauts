using System;
using System.Collections.Generic;

public class LogHistory
{
	public void Clear()
	{
		this.active = false;
		this.relsToPlayer.Clear();
	}

	public void Apply(LogHistory history)
	{
		this.COID = history.COID;
		this.relsToPlayer.AddRange(history.relsToPlayer);
		this.Alias = history.Alias;
		this.lastTimeUsed = history.lastTimeUsed;
		this.active = true;
	}

	public string COID = string.Empty;

	public List<string> relsToPlayer = new List<string>();

	public string Alias = string.Empty;

	public double lastTimeUsed = -1.0;

	public bool active;
}
