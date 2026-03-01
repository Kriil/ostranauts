using System;

[Serializable]
public class JsonShipUniques
{
	public string strCOID { get; set; }

	public string[] aConds { get; set; }

	public JsonShipUniques Clone()
	{
		return new JsonShipUniques
		{
			strCOID = this.strCOID,
			aConds = ((this.aConds == null) ? null : ((string[])this.aConds.Clone()))
		};
	}
}
