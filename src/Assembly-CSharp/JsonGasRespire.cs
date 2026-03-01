using System;

[Serializable]
public class JsonGasRespire
{
	public string strName { get; set; }

	public string strPtA { get; set; }

	public string strPtB { get; set; }

	public string strCTA { get; set; }

	public string strCTB { get; set; }

	public float fVol { get; set; }

	public float fSignalCheckRate { get; set; }

	public string strSignalCTMain { get; set; }

	public string strSignalCTA { get; set; }

	public string strSignalCTB { get; set; }

	public string strAudioEmitterPump { get; set; }

	public string strAudioEmitterAir { get; set; }

	public bool bAllowExternA { get; set; }

	public bool bAllowExternB { get; set; }

	public JsonGasRespireData[] aGases { get; set; }

	public override string ToString()
	{
		return this.strName;
	}
}
