using System;

public class AddedCOEventArgs : EventArgs
{
	public AddedCOEventArgs(CondOwner newCO)
	{
		this.CO = newCO;
	}

	public readonly CondOwner CO;
}
