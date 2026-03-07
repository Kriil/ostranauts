using System;

public class UniqueIDCOPair
{
	public CondOwner CO
	{
		get
		{
			if (!this._co && !string.IsNullOrEmpty(this.ID))
			{
				DataHandler.mapCOs.TryGetValue(this.ID, out this._co);
			}
			return this._co;
		}
	}

	public string ID;

	public CondOwner _co;
}
