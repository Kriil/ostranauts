using System;
using System.Collections.Generic;

public class GigManager
{
	public static void Init(JsonJobSave[] aJobsIn = null)
	{
		GigManager.aJobs = new List<JsonJobSave>();
		if (aJobsIn != null)
		{
			foreach (JsonJobSave jsonJobSave in aJobsIn)
			{
				GigManager.aJobs.Add(jsonJobSave.Clone());
			}
		}
	}

	private static void CullJobs()
	{
		List<JsonJobSave> list = new List<JsonJobSave>(GigManager.aJobs);
		foreach (JsonJobSave jsonJobSave in list)
		{
			if (!GigManager.CheckValid(jsonJobSave))
			{
				GigManager.aJobs.Remove(jsonJobSave);
			}
		}
	}

	private static bool CheckValid(JsonJobSave jjs)
	{
		if (jjs == null)
		{
			return false;
		}
		if (jjs.strClientID == null || jjs.strThemID == null)
		{
			jjs.bInvalid = true;
			return false;
		}
		if (!jjs.bTaken && jjs.fEpochOfferExpired <= StarSystem.fEpoch)
		{
			jjs.bInvalid = true;
			return false;
		}
		if (jjs.COClient() == null)
		{
			jjs.bInvalid = true;
			return false;
		}
		if (jjs.COThem() == null || jjs.COThem().ship == null)
		{
			jjs.bInvalid = true;
			return false;
		}
		if (jjs.str3rdID != null && (jjs.CO3rd() == null || jjs.CO3rd() == null))
		{
			jjs.bInvalid = true;
			return false;
		}
		return true;
	}

	public static bool TakeJob(JsonJobSave jjs, CondOwner coTaker, CondOwner coKiosk)
	{
		if (jjs == null || coTaker == null)
		{
			return false;
		}
		jjs.bTaken = jjs.CanCOTakeThisJob(coTaker);
		if (jjs.bTaken && jjs.strRegIDPickup != null)
		{
			if (coKiosk.ship.strRegID != jjs.strRegIDPickup || coKiosk == null)
			{
				jjs.strFailReasons = DataHandler.GetString("GUI_JOBS_MAIN_ERROR_WRONG_ORIGIN", false);
				jjs.bTaken = false;
				return jjs.bTaken;
			}
			if (coKiosk.GetCOsSafe(true, null).Count > 0)
			{
				jjs.strFailReasons = DataHandler.GetString("GUI_JOBS_MAIN_ERROR_INV_FULL", false);
				jjs.bTaken = false;
				return jjs.bTaken;
			}
		}
		if (jjs.bTaken)
		{
			Interaction interactionSetupClient = jjs.GetInteractionSetupClient();
			Interaction interactionSetupPlayer = jjs.GetInteractionSetupPlayer(coTaker);
			if (interactionSetupClient != null && interactionSetupPlayer != null)
			{
				interactionSetupClient.ApplyEffects(null, false);
				interactionSetupPlayer.ApplyEffects(null, false);
			}
			jjs.fEpochExpired = StarSystem.fEpoch + jjs.JobTemplate().fDuration * jjs.fTimeMult * 3600.0;
			coTaker.AddCondAmount(Ledger.CURRENCY, -jjs.fCostContract - (double)((int)jjs.fItemValue), 0.0, 0f);
			LedgerLI li = new LedgerLI(DataHandler.GetString("GUI_JOBS_NAMECO", false), coTaker.strID, (float)jjs.fCostContract, DataHandler.GetString("GUI_JOBS_LEDGER_COLLATERAL_PREFIX", false) + interactionSetupClient.strTitle, Ledger.CURRENCY, StarSystem.fEpoch, true, LedgerLI.Frequency.OneTime);
			Ledger.AddLI(li);
			if (jjs.strJobItems != null)
			{
				JsonJobItems jobItems = DataHandler.GetJobItems(jjs.strJobItems);
				Loot loot = DataHandler.GetLoot(jobItems.strLootPickup);
				List<CondOwner> coloot = loot.GetCOLoot(coKiosk, false, null);
				foreach (CondOwner objCO in coloot)
				{
					coKiosk.AddCO(objCO, false, true, true);
				}
			}
		}
		return jjs.bTaken;
	}

	public static void AbandonJob(JsonJobSave jjs, CondOwner coTaker)
	{
		if (jjs == null || coTaker == null)
		{
			return;
		}
		GigManager.aJobs.Remove(jjs);
		Interaction interactionAbandonClient = jjs.GetInteractionAbandonClient();
		Interaction interactionAbandonPlayer = jjs.GetInteractionAbandonPlayer(coTaker);
		if (interactionAbandonClient != null && interactionAbandonPlayer != null)
		{
			interactionAbandonClient.ApplyEffects(null, false);
			interactionAbandonPlayer.ApplyEffects(null, false);
		}
	}

	public static bool TurnInJob(JsonJobSave jjs, CondOwner coTaker, CondOwner coKiosk)
	{
		if (jjs == null || coTaker == null || coKiosk == null)
		{
			return false;
		}
		if (jjs.fEpochExpired <= StarSystem.fEpoch)
		{
			jjs.strFailReasons = DataHandler.GetString("GUI_JOBS_MAIN_ERROR_EXPIRED", false);
			return false;
		}
		List<CondOwner> list = new List<CondOwner>();
		if (jjs.strRegIDDropoff != null)
		{
			if (coKiosk.ship.strRegID != jjs.strRegIDDropoff)
			{
				jjs.strFailReasons = DataHandler.GetString("GUI_JOBS_MAIN_ERROR_WRONG_DEST", false);
				return false;
			}
			if (jjs.strJobItems != null)
			{
				JsonJobItems jobItems = DataHandler.GetJobItems(jjs.strJobItems);
				Loot loot = null;
				DataHandler.dictLoot.TryGetValue("TXTJobItemsTemplate", out loot);
				loot.aCOs = (jobItems.aCTsDeliver.Clone() as string[]);
				List<CondTrigger> ctloot = loot.GetCTLoot(null, null);
				List<CondOwner> list2 = new List<CondOwner>();
				List<CondOwner> cos = coKiosk.GetCOs(true, null);
				if (cos != null)
				{
					foreach (CondOwner condOwner in cos)
					{
						foreach (CondTrigger condTrigger in ctloot)
						{
							if (condTrigger.Triggered(condOwner, null, true))
							{
								condTrigger.fCount -= (float)condOwner.StackCount;
								list.Add(condOwner);
							}
							if (condTrigger.fCount <= 0f)
							{
								ctloot.Remove(condTrigger);
								break;
							}
						}
					}
				}
				if (ctloot.Count > 0)
				{
					jjs.strFailReasons = DataHandler.GetString("GUI_JOBS_MAIN_ERROR_INV_MISSING", false);
					List<string> itemQualityList = GUITooltip.GetItemQualityList(ctloot);
					foreach (string str in itemQualityList)
					{
						jjs.strFailReasons = jjs.strFailReasons + str + "\n";
					}
					return false;
				}
			}
		}
		Interaction interactionFinishClient = jjs.GetInteractionFinishClient();
		if (interactionFinishClient == null || !interactionFinishClient.Triggered(interactionFinishClient.objUs, interactionFinishClient.objThem, false, true, false, true, null))
		{
			return false;
		}
		Interaction interactionFinishPlayer = jjs.GetInteractionFinishPlayer(coTaker);
		if (interactionFinishPlayer == null || !interactionFinishPlayer.Triggered(interactionFinishPlayer.objUs, interactionFinishPlayer.objThem, false, true, false, true, null))
		{
			return false;
		}
		interactionFinishClient.ApplyEffects(null, false);
		interactionFinishPlayer.ApplyEffects(null, false);
		foreach (CondOwner condOwner2 in list)
		{
			coKiosk.RemoveCO(condOwner2, false);
			condOwner2.Destroy();
		}
		double num = jjs.fPayout * jjs.fPayoutMult;
		int tier = GigManager.GetTier(jjs);
		num *= (double)tier;
		num += (double)((int)jjs.fItemValue);
		coTaker.AddCondAmount(Ledger.CURRENCY, num, 0.0, 0f);
		string text = interactionFinishClient.strTitle + " - " + DataHandler.GetString("GUI_JOBS_BONUS_" + tier, false);
		LedgerLI li = new LedgerLI(coTaker.strID, DataHandler.GetString("GUI_JOBS_NAMECO", false), (float)num, text, Ledger.CURRENCY, StarSystem.fEpoch, true, LedgerLI.Frequency.OneTime);
		Ledger.AddLI(li);
		GigManager.aJobs.Remove(jjs);
		coTaker.LogMessage(DataHandler.GetString("GUI_JOBS_LOG_TURNEDIN", false) + text, "Good", coTaker.strID);
		return true;
	}

	public static int GetTier(JsonJobSave jjs)
	{
		if (jjs == null)
		{
			return 1;
		}
		double num = jjs.fEpochExpired - jjs.JobTemplate().fDuration * jjs.fTimeMult * 3600.0;
		if (StarSystem.fEpoch - num <= jjs.JobTemplate().fDuration * jjs.fTimeMult * 3600.0 / 8.0)
		{
			return 8;
		}
		if (StarSystem.fEpoch - num <= jjs.JobTemplate().fDuration * jjs.fTimeMult * 3600.0 / 4.0)
		{
			return 4;
		}
		if (StarSystem.fEpoch - num <= jjs.JobTemplate().fDuration * jjs.fTimeMult * 3600.0 / 2.0)
		{
			return 2;
		}
		return 1;
	}

	public static void GetJobs()
	{
		GigManager.CullJobs();
		List<string> list = new List<string>
		{
			CrewSim.coPlayer.strID
		};
		CondOwner selectedCrew = CrewSim.GetSelectedCrew();
		Ship nearestStationRegional = CrewSim.system.GetNearestStationRegional(selectedCrew.ship.objSS.vPosx, selectedCrew.ship.objSS.vPosy);
		string closestStationId = GigManager.GetClosestStationId(selectedCrew);
		string str = (nearestStationRegional == null) ? string.Empty : nearestStationRegional.strRegID;
		int num = 0;
		int num2 = 0;
		foreach (JsonJobSave jsonJobSave in GigManager.aJobs)
		{
			if (GigManager.CheckValid(jsonJobSave))
			{
				if (GigManager.CheckLocal(jsonJobSave, selectedCrew, 0.006684587064179629))
				{
					num2++;
				}
				if (jsonJobSave.strJobName.Contains("MVPCourierTestDeliverCOShortHop") && jsonJobSave.strJobName.Contains(closestStationId))
				{
					num++;
				}
			}
		}
		string text = "MVPCourierTestDeliverCOShortHop";
		if (DataHandler.dictJobs.ContainsKey(text + closestStationId))
		{
			text += closestStationId;
		}
		int i = 7;
		while (num <= 0 && i > 0)
		{
			i--;
			JsonJobSave jsonJobSave2 = GigManager.MakeJob(text, list);
			if (jsonJobSave2 != null)
			{
				GigManager.aJobs.Add(jsonJobSave2);
				list.Add(jsonJobSave2.strClientID);
				break;
			}
		}
		if (GigManager.aJobs.Count > 5 && num2 > 0)
		{
			return;
		}
		i = MathUtils.Rand(5, 10, MathUtils.RandType.Flat, null);
		Loot loot = DataHandler.GetLoot("TXTJobList" + str);
		if (loot.strName == "Blank")
		{
			loot = DataHandler.GetLoot("TXTJobList");
			if (loot.strName == "Blank")
			{
				return;
			}
		}
		while (i > 0)
		{
			i--;
			List<string> lootNames = loot.GetLootNames(null, false, null);
			if (lootNames.Count == 0)
			{
				break;
			}
			JsonJobSave jsonJobSave3 = GigManager.MakeJob(lootNames[0], list);
			if (jsonJobSave3 != null)
			{
				GigManager.aJobs.Add(jsonJobSave3);
				list.Add(jsonJobSave3.strClientID);
			}
		}
	}

	public static bool CheckLocal(JsonJobSave jjs, CondOwner coUser, double fSearchRadius)
	{
		if (jjs == null)
		{
			return false;
		}
		bool flag = false;
		if (coUser.ship.GetRangeTo(jjs.COThem().ship) < fSearchRadius)
		{
			flag = true;
		}
		if (jjs.CO3rd() != null && coUser.ship.GetRangeTo(jjs.CO3rd().ship) < fSearchRadius)
		{
			flag = true;
		}
		if (!jjs.bTaken && !flag)
		{
			return false;
		}
		if (!jjs.bTaken)
		{
			double num = 1.0;
			double fTimeMult = 1.0;
			Ship ship = coUser.ship;
			if (jjs.strRegIDPickup != null)
			{
				ship = CrewSim.system.GetShipByRegID(jjs.strRegIDPickup);
			}
			Ship ship2 = jjs.COThem().ship;
			if (jjs.strRegIDDropoff != null)
			{
				ship2 = CrewSim.system.GetShipByRegID(jjs.strRegIDDropoff);
			}
			if (ship2 != null && ship != null)
			{
				if (ship == ship2)
				{
					num = 1.0;
				}
				else if (JsonTransit.IsTransitConnected(ship.strRegID, ship2.strRegID))
				{
					num = 1.2;
				}
				else
				{
					double rangeTo = ship.GetRangeTo(ship2);
					double num2 = rangeTo * 149597872.0 / 3600.0;
					num = (num2 * 2.0 + jjs.fPayout + 1000.0) / jjs.fPayout;
					if (!ship2.objSS.bIsBO)
					{
						num *= 3.0;
					}
					if (rangeTo > 3.342293712194078E-05)
					{
						fTimeMult = Math.Max(1.0, rangeTo * 30.0);
					}
				}
			}
			jjs.fTimeMult = fTimeMult;
			jjs.fPayoutMult = num;
		}
		return true;
	}

	private static string GetClosestStationId(CondOwner player)
	{
		if (player == null || player.ship == null)
		{
			return string.Empty;
		}
		Ship ship = player.ship;
		if (ship != null && !ship.IsStation(false))
		{
			Ship nearestStation = CrewSim.system.GetNearestStation(player.ship.objSS.vPosx, player.ship.objSS.vPosy, false);
			if (nearestStation != null && player.ship.GetRangeTo(nearestStation) < 3.342293553032505E-08 && !nearestStation.HideFromSystem)
			{
				ship = nearestStation;
			}
		}
		return ship.strRegID;
	}

	private static JsonJobSave MakeJob(string strJobName, List<string> aUsedClients)
	{
		if (string.IsNullOrEmpty(strJobName) || !DataHandler.dictJobs.ContainsKey(strJobName))
		{
			return null;
		}
		if (aUsedClients == null)
		{
			aUsedClients = new List<string>();
		}
		JsonJob job = DataHandler.GetJob(strJobName);
		if (job == null)
		{
			return null;
		}
		Interaction interaction = DataHandler.GetInteraction(job.strIASetupClient, null, false);
		if (interaction == null)
		{
			return null;
		}
		if (job.strPSpecClient == null)
		{
			return null;
		}
		PersonSpec person = StarSystem.GetPerson(DataHandler.GetPersonSpec(job.strPSpecClient), null, false, aUsedClients, null);
		if (person == null)
		{
			return null;
		}
		CondOwner co = person.GetCO();
		Social soc = null;
		if (co != null)
		{
			soc = co.socUs;
		}
		List<string> list = new List<string>
		{
			co.strID
		};
		if (interaction.PSpecTestThem == null)
		{
			return null;
		}
		PersonSpec person2 = StarSystem.GetPerson(interaction.PSpecTestThem, soc, false, list, interaction.ShipTestThem);
		if (person2 == null)
		{
			return null;
		}
		PersonSpec personSpec = null;
		if (interaction.PSpecTest3rd != null)
		{
			list.Add(person2.FullName);
			personSpec = StarSystem.GetPerson(interaction.PSpecTest3rd, soc, false, list, null);
			if (personSpec == null)
			{
				return null;
			}
		}
		double num = MathUtils.Rand(job.fContractMin, job.fContractMax, MathUtils.RandType.Flat, null);
		double num2 = MathUtils.Rand(job.fPayoutMin, job.fPayoutMax, MathUtils.RandType.Flat, null);
		int num3 = 5;
		while (num3 >= 0 && num >= num2)
		{
			num3--;
			num = MathUtils.Rand(job.fContractMin, job.fContractMax, MathUtils.RandType.Flat, null);
			num2 = MathUtils.Rand(job.fPayoutMin, job.fPayoutMax, MathUtils.RandType.Flat, null);
		}
		if (num >= num2)
		{
			return null;
		}
		string randomText = GigManager.GetRandomText(job.strLootRegIDsOrigin);
		string randomText2 = GigManager.GetRandomText(job.strLootRegIDsDest);
		if (randomText != null && randomText == randomText2)
		{
			return null;
		}
		JsonJobSave jsonJobSave = new JsonJobSave();
		jsonJobSave.strJobName = job.strName;
		jsonJobSave.fCostContract = num;
		jsonJobSave.fPayout = num2;
		jsonJobSave.fEpochOfferExpired = StarSystem.fEpoch + 3600.0 * MathUtils.Rand(3.0, 6.0, MathUtils.RandType.Flat, null);
		if (personSpec != null)
		{
			jsonJobSave.str3rdID = personSpec.FullName;
		}
		if (person != null)
		{
			jsonJobSave.strClientID = person.FullName;
		}
		if (person2 != null)
		{
			jsonJobSave.strThemID = person2.FullName;
		}
		jsonJobSave.strTxt1 = GigManager.GetRandomText(job.strLootTxt1);
		jsonJobSave.strRegIDPickup = randomText;
		jsonJobSave.strRegIDDropoff = randomText2;
		jsonJobSave.strJobItems = GigManager.GetRandomText(job.strLootJobItems);
		if (jsonJobSave.strJobItems != null)
		{
			JsonJobItems jobItems = DataHandler.GetJobItems(jsonJobSave.strJobItems);
			Loot loot = DataHandler.GetLoot(jobItems.strLootPickup);
			List<CondOwner> coloot = loot.GetCOLoot(null, false, null);
			double num4 = 0.0;
			foreach (CondOwner condOwner in coloot)
			{
				num4 += condOwner.GetTotalPrice(null, true, true);
				condOwner.Destroy();
			}
			jsonJobSave.fItemValue = num4 * MathUtils.Rand(0.5, 1.5, MathUtils.RandType.Mid, null);
		}
		return jsonJobSave;
	}

	private static string GetRandomText(string strLoot)
	{
		Loot loot = DataHandler.GetLoot(strLoot);
		if (loot.strName != "Blank")
		{
			List<string> lootNames = loot.GetLootNames(null, false, null);
			if (lootNames.Count > 0)
			{
				return lootNames[0];
			}
		}
		return null;
	}

	public static List<JsonJobSave> aJobs;
}
