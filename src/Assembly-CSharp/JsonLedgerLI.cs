using System;

// Serialized ledger line item.
// This represents one concrete payable/receivable entry in the runtime ledger,
// including amount, timestamps, counterparties, and repeat count.
public class JsonLedgerLI
{
	// Payment state and amount.
	public bool bPaid { get; set; }

	public float fAmount { get; set; }

	public double fTime { get; set; }

	public double fTimePaid { get; set; }

	public string strCurrency { get; set; }

	public string strDesc { get; set; }

	public string strPayee { get; set; }

	public string strPayor { get; set; }

	// Likely stores the enum value used by repeating charges/invoices.
	public int Repeats { get; set; }
}
