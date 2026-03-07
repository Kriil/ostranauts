using System;
using System.Collections.Generic;

[Serializable]
public class JsonRacingLeague
{
	public string strName { get; set; }

	public string strNameFriendly { get; set; }

	public string strDescription { get; set; }

	public string strImgPath { get; set; }

	public string ctLeagueHost { get; set; }

	public string ctEntryRequirements { get; set; }

	public string strEntryFeeLedgerDef { get; set; }

	public string strPsPecParticipant { get; set; }

	public string strStartingLoot { get; set; }

	public RacePrize[] aRacePrize { get; set; }

	public string[] aTracks { get; set; }

	public List<string> GetPrizeStrings()
	{
		if (this.aRacePrize == null)
		{
			return new List<string>();
		}
		List<string> list = new List<string>();
		for (int i = 0; i < this.aRacePrize.Length; i++)
		{
			string text = string.Empty;
			text = text + (i + 1) + ". ";
			if (this.aRacePrize[i].Loot != null)
			{
				Loot loot = DataHandler.GetLoot(this.aRacePrize[i].Loot);
				if (loot != null)
				{
					Dictionary<string, double> condLoot = loot.GetCondLoot(1f, null, null);
					int num = 0;
					foreach (KeyValuePair<string, double> keyValuePair in condLoot)
					{
						string key = keyValuePair.Key;
						if (!key.StartsWith("-"))
						{
							Condition cond = DataHandler.GetCond(key);
							if (num != 0)
							{
								text += ", ";
							}
							if (keyValuePair.Value > 1.0)
							{
								text = text + keyValuePair.Value.ToString("N0") + "x";
							}
							text += cond.strNameFriendly;
							num++;
						}
					}
				}
			}
			if (this.aRacePrize[i].LedgerDef != null)
			{
				JsonLedgerDef ledgerDef = DataHandler.GetLedgerDef(this.aRacePrize[i].LedgerDef);
				if (ledgerDef != null)
				{
					text = text + "\n$" + ledgerDef.fAmount.ToString("F1");
				}
			}
			list.Add(text);
		}
		return list;
	}
}
