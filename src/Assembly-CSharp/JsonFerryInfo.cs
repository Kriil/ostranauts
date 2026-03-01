using System;

public class JsonFerryInfo
{
	public AIShipManager.FerryState GetFerryState()
	{
		int num = this.nFerryState;
		if (num == 1)
		{
			return AIShipManager.FerryState.COMING;
		}
		if (num != 2)
		{
			return AIShipManager.FerryState.OFF;
		}
		return AIShipManager.FerryState.ARRIVED;
	}

	public void SetFerryState(AIShipManager.FerryState nState)
	{
		if (nState != AIShipManager.FerryState.ARRIVED)
		{
			if (nState != AIShipManager.FerryState.COMING)
			{
				this.nFerryState = 0;
			}
			else
			{
				this.nFerryState = 1;
			}
		}
		else
		{
			this.nFerryState = 2;
		}
	}

	public JsonFerryInfo Clone()
	{
		return base.MemberwiseClone() as JsonFerryInfo;
	}

	public string strFerryCOID;

	public string strFerryDestination;

	public double fTimeOfFerryArrival;

	public double fPricePaid;

	public int nFerryState;

	public bool bUserCharged;
}
