using System;

public class RemovedCOEventArgs : EventArgs
{
	public RemovedCOEventArgs(CondOwner newCO)
	{
		this.CO = newCO;
	}

	public readonly CondOwner CO;
}
