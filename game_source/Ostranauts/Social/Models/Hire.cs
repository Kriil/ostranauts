using System;
using System.Text;
using Ostranauts.Core;
using Ostranauts.Objectives;
using UnityEngine;

namespace Ostranauts.Social.Models
{
	public class Hire : SocialStakes
	{
		public Hire()
		{
			this.strContext = "ACTHireCrewUs";
		}

		public override void UpdateUs(Interaction ia)
		{
			if (ia == null)
			{
				return;
			}
			this.UpdateUs(ia.objThem);
		}

		public override void UpdateUs(CondOwner them)
		{
			if (them == null)
			{
				return;
			}
			if (!them.HasCond("PaySalary"))
			{
				them.ZeroCondAmount("PaySigning");
				them.ZeroCondAmount("PayDeath");
				CondTrigger condTrigger = DataHandler.GetCondTrigger("TIsSkilledEngineer");
				CondTrigger condTrigger2 = DataHandler.GetCondTrigger("TIsSkilledPilot");
				if (condTrigger2.Triggered(them, null, true))
				{
					Loot loot = DataHandler.GetLoot("CONDBasePayPilot");
					loot.ApplyCondLoot(them, 1f, null, 0f);
				}
				else if (condTrigger.Triggered(them, null, true))
				{
					Loot loot2 = DataHandler.GetLoot("CONDBasePayEng");
					loot2.ApplyCondLoot(them, 1f, null, 0f);
				}
				else
				{
					Loot loot3 = DataHandler.GetLoot("CONDBasePayCrew");
					loot3.ApplyCondLoot(them, 1f, null, 0f);
				}
				them.SetCondAmount("PaySalary", (double)Mathf.RoundToInt((float)them.GetCondAmount("PaySalary")), 0.0);
				them.SetCondAmount("PaySigning", (double)Mathf.RoundToInt((float)them.GetCondAmount("PaySigning")), 0.0);
				them.SetCondAmount("PayDeath", (double)Mathf.RoundToInt((float)them.GetCondAmount("PayDeath")), 0.0);
			}
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.Append("Signing Bonus: $");
			stringBuilder.AppendLine(them.GetCondAmount("PaySigning").ToString("#.00"));
			stringBuilder.Append("Salary/day: $");
			stringBuilder.Append(them.GetCondAmount("PaySalary").ToString("#.00"));
			stringBuilder.AppendLine("/day");
			stringBuilder.Append("Death Pay: $");
			stringBuilder.AppendLine(them.GetCondAmount("PayDeath").ToString("#.00"));
			this._strMTTInfo = stringBuilder.ToString();
		}

		public override void UpdateThem(Interaction ia)
		{
			if (ia != null && ia.strName == "SOCHire2BasicAllow" && !ia.objUs.HasCond("IsPlayerCrew"))
			{
				if (MonoSingleton<ObjectiveTracker>.Instance.PAXHireTemp != null)
				{
					MonoSingleton<ObjectiveTracker>.Instance.RemoveObjective(MonoSingleton<ObjectiveTracker>.Instance.PAXHireTemp, ObjectiveTracker.REASON_COMPLETED, true);
					MonoSingleton<ObjectiveTracker>.Instance.PAXHireTemp = null;
				}
				if (!CrewSim.coPlayer.HasCond("TutorialCrewCycleStart"))
				{
					string keyName = GUIActionKeySelector.commandCycleCrew.KeyName;
					Objective objective = new Objective(CrewSim.coPlayer, "Cycle to New Crew", "TIsTutorialCrewCycleComplete");
					objective.strDisplayDesc = "Press the \"" + keyName + "\" key to cycle to the next crew member.";
					objective.strDisplayDescComplete = "Crew Member Selected";
					objective.bTutorial = true;
					MonoSingleton<ObjectiveTracker>.Instance.AddObjective(objective);
					CrewSim.coPlayer.AddCondAmount("TutorialCrewCycleStart", 1.0, 0.0, 0f);
				}
				int nUTCHour = StarSystem.nUTCHour;
				ia.objThem.Company.mapRoster[ia.objUs.strID] = new JsonCompanyRules();
				ia.objThem.Company.mapRoster[ia.objUs.strID].StartWorkdayAt(nUTCHour);
				ia.objUs.AddCondAmount("IsPlayerCrew", 1.0, 0.0, 0f);
				ia.objUs.Company = ia.objThem.Company;
				string strMsg = ia.objUs.strName + " is now a member of " + ia.objUs.Company.strName + ".";
				ia.objThem.LogMessage(strMsg, "Neutral", ia.objUs.strName);
				ia.objUs.LogMessage(strMsg, "Neutral", ia.objUs.strName);
				ia.objUs.SetShipsOwned(ia.objThem.GetShipsOwned());
				LedgerLI li = new LedgerLI(ia.objUs.strName, ia.objThem.strName, (float)ia.objUs.GetCondAmount("PaySigning"), "Sign-on Bonus", GUIFinance.strCondCurr, StarSystem.fEpoch, false, LedgerLI.Frequency.OneTime);
				Ledger.AddLI(li);
				li = new LedgerLI(ia.objUs.strName, ia.objThem.strName, (float)ia.objUs.GetCondAmount("PayDeath"), "Death Pay", GUIFinance.strCondCurr, StarSystem.fEpoch, false, LedgerLI.Frequency.OneTime);
				Ledger.AddLI(li);
				li = new LedgerLI(ia.objUs.strName, ia.objThem.strName, (float)ia.objUs.GetCondAmount("PaySalary"), "Salary", GUIFinance.strCondCurr, StarSystem.fEpoch, false, LedgerLI.Frequency.Daily);
				Ledger.AddLI(li);
			}
		}
	}
}
