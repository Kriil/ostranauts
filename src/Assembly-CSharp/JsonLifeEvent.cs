using System;

public class JsonLifeEvent
{
	public string strName { get; set; }

	public string strInteraction { get; set; }

	public string strStartATC { get; set; }

	public float fStartATCRange { get; set; }

	public string strShipRewards { get; set; }

	public bool bShipOwned { get; set; }

	public float fCashRewardMin { get; set; }

	public float fCashRewardMax { get; set; }

	public float fShipMortgage { get; set; }

	public float fShipDmgMax { get; set; }

	public override string ToString()
	{
		return this.strName;
	}
}
