using System;

public class AutoTask
{
	public AutoTask(CondOwner co, string strIA, double fWeight)
	{
		this.co = co;
		this.strIA = strIA;
		this.fWeight = fWeight;
	}

	public CondOwner co;

	public string strIA;

	public double fWeight;
}
