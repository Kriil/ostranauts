using System;

[Serializable]
public class JsonPowerInfo
{
	public string strName { get; set; }

	public string strUsePowerCT { get; set; }

	public string strRechargeCT { get; set; }

	public string[] aInputPts { get; set; }

	public double fAmount { get; set; }

	public string strIntPowerOff { get; set; }

	public string strIntPowerOn { get; set; }

	public bool bAllowExtPower { get; set; }

	public string strPowerSourceCT { get; set; }

	public bool Overlay()
	{
		return this.fAmount != 0.0 || (this.aInputPts != null && this.aInputPts.Length > 0) || this.strUsePowerCT != null || this.strIntPowerOn != string.Empty;
	}

	public override string ToString()
	{
		return this.strName;
	}
}
