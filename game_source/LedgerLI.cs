using System;

public class LedgerLI
{
	public LedgerLI(string strPayee, string strPayor, float fAmount, string strDesc, string strCurrency, double fTime, bool bPaid, LedgerLI.Frequency reps)
	{
		this.strPayee = strPayee;
		this.strPayor = strPayor;
		this.fAmount = fAmount;
		this.strDesc = strDesc;
		this.strCurrency = strCurrency;
		this.fTime = fTime;
		this.bPaid = bPaid;
		this.Repeats = reps;
	}

	public LedgerLI(JsonLedgerLI jsLI) : this(jsLI.strPayee, jsLI.strPayor, jsLI.fAmount, jsLI.strDesc, jsLI.strCurrency, jsLI.fTime, jsLI.bPaid, (LedgerLI.Frequency)jsLI.Repeats)
	{
		if (jsLI.bPaid && jsLI.fTimePaid == 0.0)
		{
			this.fTimePaid = jsLI.fTime;
		}
		else
		{
			this.fTimePaid = jsLI.fTimePaid;
		}
	}

	public bool Paid
	{
		get
		{
			return this.bPaid || this.fTimePaid != 0.0;
		}
	}

	public LedgerLI Clone()
	{
		return new LedgerLI(this.strPayee, this.strPayor, this.fAmount, this.strDesc, this.strCurrency, this.fTime, this.bPaid, this.Repeats)
		{
			fTimePaid = this.fTimePaid
		};
	}

	public JsonLedgerLI GetJSON()
	{
		return new JsonLedgerLI
		{
			bPaid = this.bPaid,
			fAmount = this.fAmount,
			fTime = this.fTime,
			fTimePaid = this.fTimePaid,
			strCurrency = this.strCurrency,
			strDesc = this.strDesc,
			strPayee = this.strPayee,
			strPayor = this.strPayor,
			Repeats = (int)this.Repeats
		};
	}

	public static bool Same(LedgerLI liA, LedgerLI liB)
	{
		return liA != null && liB != null && !(liA.strCurrency != liB.strCurrency) && liA.bPaid == liB.bPaid && liA.Repeats == liB.Repeats && (liA.Repeats == LedgerLI.Frequency.Mortgage || (double)Math.Abs(liA.fAmount - liB.fAmount) <= 0.001) && Math.Abs(liA.fTime - liB.fTime) <= 0.001 && !(liA.strDesc != liB.strDesc) && !(liA.strPayee != liB.strPayee) && !(liA.strPayor != liB.strPayor);
	}

	public string strPayee;

	public string strPayor;

	public float fAmount;

	public string strDesc;

	public string strCurrency;

	public double fTime;

	[Obsolete]
	private bool bPaid;

	public double fTimePaid;

	public LedgerLI.Frequency Repeats;

	public enum Frequency
	{
		OneTime,
		Hourly,
		Shiftly,
		Daily,
		Monthly,
		Yearly,
		Mortgage
	}
}
