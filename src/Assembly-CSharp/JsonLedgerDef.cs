using System;

// Ledger template/definition payload.
// Likely used to spawn recurring or scripted ledger entries such as salaries,
// station fees, or ATC charges before they become concrete JsonLedgerLI rows.
public class JsonLedgerDef
{
	// Initial paid state and whether the entry is charged through ATC/station systems.
	public bool bPaid { get; set; }

	public bool bPayATC { get; set; }

	public float fAmount { get; set; }

	public string strName { get; set; }

	public string strCurrency { get; set; }

	public string strDesc { get; set; }

	public string strPSpecOrCtPayee { get; set; }

	public string strFrequency { get; set; }

	// Parses the saved string into the runtime frequency enum.
	public LedgerLI.Frequency Frequency
	{
		get
		{
			if (!Enum.IsDefined(typeof(LedgerLI.Frequency), this.strFrequency))
			{
				return LedgerLI.Frequency.OneTime;
			}
			return (LedgerLI.Frequency)Enum.Parse(typeof(LedgerLI.Frequency), this.strFrequency);
		}
	}
}
