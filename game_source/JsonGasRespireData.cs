using System;

[Serializable]
public class JsonGasRespireData
{
	public string strName { get; set; }

	public string strGasIn { get; set; }

	public string strGasOut { get; set; }

	public float fGasPressTotalRef { get; set; }

	public float fConvRate { get; set; }

	public float fStatRate { get; set; }

	public string strLootConds { get; set; }

	public Loot GetLoot()
	{
		if (this._loot == null)
		{
			this._loot = DataHandler.GetLoot(this.strLootConds);
		}
		return this._loot;
	}

	private Loot _loot;
}
