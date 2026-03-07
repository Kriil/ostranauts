using System;

public class JsonTicker
{
	public string strName { get; set; }

	public string strCondLoot { get; set; }

	public string strCondLootCoeff { get; set; }

	public string strCondUpdate { get; set; }

	public double fPeriod { get; set; }

	public double fEpochStart { get; set; }

	public bool bQueue { get; set; }

	public bool bRepeat { get; set; }

	public bool bTickWhileAway { get; set; }

	public JsonTicker Clone()
	{
		JsonTicker jsonTicker = new JsonTicker();
		jsonTicker.strName = this.strName;
		jsonTicker.strCondLoot = this.strCondLoot;
		jsonTicker.strCondLootCoeff = this.strCondLootCoeff;
		jsonTicker.strCondUpdate = this.strCondUpdate;
		jsonTicker.fPeriod = this.fPeriod;
		jsonTicker.fEpochStart = this.fEpochStart;
		jsonTicker.nClampMax = this.nClampMax;
		jsonTicker.bQueue = this.bQueue;
		jsonTicker.bRepeat = this.bRepeat;
		jsonTicker.bTickWhileAway = this.bTickWhileAway;
		jsonTicker.SetOwner(this.coOwner);
		return jsonTicker;
	}

	public void SetOwner(CondOwner co)
	{
		this.coOwner = co;
	}

	public void SetTimeLeft(double fTimeLeftNew)
	{
		this.fEpochStart = 3600.0 * (fTimeLeftNew - this.fPeriod) + StarSystem.fEpoch;
	}

	public override string ToString()
	{
		return string.Concat(new object[]
		{
			this.strName,
			": ",
			this.fTimeLeft,
			"/",
			this.fPeriod
		});
	}

	public double fTimeLeft
	{
		get
		{
			return this.fPeriod - (StarSystem.fEpoch - this.fEpochStart) / 3600.0;
		}
	}

	public int nClampMax;

	private CondOwner coOwner;
}
