using System;
using System.Collections.Generic;
using Ostranauts.Core;
using Ostranauts.Objectives;

public class CrimeManager
{
	public static void Init()
	{
		CrimeManager.dictCrimeTypes = new Dictionary<string, Dictionary<string, JsonCrime>>();
		if (CrimeManager._ctValidVictim == null)
		{
			CrimeManager._ctValidVictim = DataHandler.GetCondTrigger("TIsAIReportCrimeValidVictim");
		}
		foreach (JsonCrime jsonCrime in DataHandler.dictCrimes.Values)
		{
			if (!string.IsNullOrEmpty(jsonCrime.strLaw))
			{
				if (!CrimeManager.dictCrimeTypes.ContainsKey(jsonCrime.strLaw))
				{
					CrimeManager.dictCrimeTypes[jsonCrime.strLaw] = new Dictionary<string, JsonCrime>();
				}
				CrimeManager.dictCrimeTypes[jsonCrime.strLaw][jsonCrime.strCrime] = jsonCrime;
			}
		}
	}

	public static void LogCrime(Interaction ia)
	{
		if (ia == null || string.IsNullOrEmpty(ia.strCrime) || ia.objUs == null || ia.objUs.ship == null || string.IsNullOrEmpty(ia.objUs.ship.strLaw))
		{
			return;
		}
		Dictionary<string, JsonCrime> dictionary = null;
		if (CrimeManager.dictCrimeTypes.TryGetValue(ia.objUs.ship.strLaw, out dictionary))
		{
			JsonCrime jsonCrime = null;
			if (!dictionary.TryGetValue(ia.strCrime, out jsonCrime) || CrimeManager._ctValidVictim.Triggered(ia.objThem, null, true) || ia.objUs.HasCond("CareerLEOfficer"))
			{
				return;
			}
			bool witnessed = CrimeManager.EvaluateCrime(ia, jsonCrime);
			MonoSingleton<ObjectiveTracker>.Instance.CreateCrimeWarning(ia.objUs, jsonCrime.strCrime, witnessed);
		}
	}

	private static bool EvaluateCrime(Interaction ia, JsonCrime jc)
	{
		if (jc == null)
		{
			return false;
		}
		bool flag = false;
		foreach (CondOwner condOwner in ia.objUs.ship.GetPeople(true))
		{
			if (!(condOwner == ia.objUs))
			{
				if (condOwner.bAlive && !condOwner.HasCond("Unconscious"))
				{
					if (Visibility.IsCondOwnerLOSVisibleBlocks(ia.objUs, condOwner.tfVector2Position, false, true))
					{
						if (condOwner.HasCond("CareerLEOfficer"))
						{
							CrimeManager.ApplyCrimeLoot(ia.objUs, jc.strLootCondsPerp);
						}
						else if (!string.IsNullOrEmpty(jc.strPledge))
						{
							JsonPledge pledge = DataHandler.GetPledge(jc.strPledge);
							condOwner.AddPledge(PledgeFactory.Factory(condOwner, pledge, ia.objUs));
						}
						JsonTicker jsonTicker = new JsonTicker();
						jsonTicker.strName = "CrimeAlert";
						jsonTicker.bQueue = true;
						jsonTicker.fPeriod = 0.0;
						jsonTicker.SetTimeLeft(jsonTicker.fPeriod);
						condOwner.AddTicker(jsonTicker);
						flag = true;
					}
				}
			}
		}
		if (!flag && jc.bAutoApply)
		{
			CrimeManager.ApplyCrimeLoot(ia.objUs, jc.strLootCondsPerp);
		}
		return flag;
	}

	private static void ApplyCrimeLoot(CondOwner us, string lootConds)
	{
		if (string.IsNullOrEmpty(lootConds) || us == null)
		{
			return;
		}
		Loot loot = DataHandler.GetLoot(lootConds);
		loot.ApplyCondLoot(us, 1f, null, 0f);
	}

	private static void RemoveCrimeLoot(CondOwner co, Dictionary<string, double> condLoot)
	{
		foreach (KeyValuePair<string, double> keyValuePair in condLoot)
		{
			co.AddCondAmount(keyValuePair.Key, -keyValuePair.Value, 0.0, 0f);
		}
	}

	public static void ClearCrimeFlags(string currentLaw, List<CondOwner> cos)
	{
		if (cos == null)
		{
			return;
		}
		foreach (JsonCrime jsonCrime in DataHandler.dictCrimes.Values)
		{
			if (jsonCrime != null && jsonCrime.bAutoApply && !(currentLaw == jsonCrime.strLaw))
			{
				Loot loot = DataHandler.GetLoot(jsonCrime.strLootCondsPerp);
				if (loot != null)
				{
					Dictionary<string, double> condLoot = loot.GetCondLoot(1f, null, null);
					if (condLoot.Count != 0)
					{
						foreach (CondOwner condOwner in cos)
						{
							bool flag = true;
							foreach (KeyValuePair<string, double> keyValuePair in condLoot)
							{
								if (!condOwner.HasCond(keyValuePair.Key))
								{
									flag = false;
									break;
								}
							}
							if (flag)
							{
								CrimeManager.RemoveCrimeLoot(condOwner, condLoot);
							}
						}
					}
				}
			}
		}
	}

	private static Dictionary<string, Dictionary<string, JsonCrime>> dictCrimeTypes;

	private static CondTrigger _ctValidVictim;
}
