using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Runtime/save-friendly company or crew-employer model.
// This tracks roster membership, per-member work rules, and some dismissal/payroll
// side effects for organizations that own crew and possibly ships.
public class JsonCompany
{
	// Starts with an empty company id and no rostered members.
	public JsonCompany()
	{
		this._strName = string.Empty;
		this.mapRoster = new Dictionary<string, JsonCompanyRules>();
	}

	// Internal company id used by saves and registries.
	public string _strName { get; set; }

	public string strRegID { get; set; }

	public Dictionary<string, JsonCompanyRules> mapRoster { get; set; }

	public JsonCompanyRules jcrDefaultRules { get; set; }

	// Resolves current roster ids into live CondOwner references.
	public List<CondOwner> GetCrewMembers(CondOwner excludedCO = null)
	{
		List<CondOwner> list = new List<CondOwner>();
		foreach (KeyValuePair<string, JsonCompanyRules> keyValuePair in this.mapRoster)
		{
			if (!(excludedCO != null) || !(excludedCO.strID == keyValuePair.Key))
			{
				CondOwner item;
				if (DataHandler.mapCOs.TryGetValue(keyValuePair.Key, out item))
				{
					list.Add(item);
				}
			}
		}
		return list;
	}

	// Adds a member and seeds their rules from the company default rule set.
	public void AddNewMember(string coID)
	{
		if (string.IsNullOrEmpty(coID) || this.mapRoster == null)
		{
			return;
		}
		this.mapRoster[coID] = ((this.jcrDefaultRules == null) ? new JsonCompanyRules() : this.jcrDefaultRules.Clone());
	}

	// Removes a member and cleans up company ownership/payroll-related state.
	// Likely used when firing crew or transferring them out of the player's company.
	public void DismissMember(string coID, CondOwner coBoss = null)
	{
		if (string.IsNullOrEmpty(coID) || this.mapRoster == null)
		{
			return;
		}
		this.mapRoster.Remove(coID);
		CondOwner condOwner = null;
		DataHandler.mapCOs.TryGetValue(coID, out condOwner);
		if (condOwner == null)
		{
			return;
		}
		condOwner.Company = null;
		condOwner.SetShipsOwned(new List<string>
		{
			AIShipManager.strATCLast
		});
		string strMsg = condOwner.strName + " no longer a member of " + this.strName + ".";
		condOwner.LogMessage(strMsg, "Neutral", condOwner.strName);
		if (coBoss == null)
		{
			return;
		}
		coBoss.LogMessage(strMsg, "Neutral", condOwner.strName);
		if (coBoss != CrewSim.coPlayer)
		{
			return;
		}
		condOwner.ZeroCondAmount("IsPlayerCrew");
		condOwner.ZeroCondAmount("IsDrafted");
		bool flag = condOwner.bAlive;
		List<LedgerLI> unpaidLIs = Ledger.GetUnpaidLIs(condOwner.strName, coBoss.strName, null, true, false);
		foreach (LedgerLI ledgerLI in unpaidLIs)
		{
			if (ledgerLI.strDesc.Contains("Salary"))
			{
				Ledger.RemoveLI(ledgerLI);
			}
			else if (ledgerLI.strDesc.Contains("Death Pay") && condOwner.bAlive)
			{
				Ledger.RemoveLI(ledgerLI);
				flag = false;
			}
		}
		if (flag)
		{
			float num = (float)condOwner.GetCondAmount("PayDeath");
			LedgerLI li = new LedgerLI(coBoss.strName, condOwner.strName, num, "Death Pay", GUIFinance.strCondCurr, StarSystem.fEpoch, true, LedgerLI.Frequency.OneTime);
			coBoss.AddCondAmount(GUIFinance.strCondCurr, (double)num, 0.0, 0f);
			coBoss.LogMessage(Interaction.STR_GUI_FINANCE_LOG_RECEIVED + condOwner.strName + ": " + num.ToString("n"), "Good", coBoss.strName);
			Ledger.AddLI(li);
		}
	}

	// Returns the worker's shift definition for the given hour.
	public JsonShift GetShift(int nHour, CondOwner coWorker)
	{
		if (coWorker == null || string.IsNullOrEmpty(coWorker.strID))
		{
			return JsonCompany.NullShift;
		}
		if (nHour < 0)
		{
			nHour += 25;
		}
		else if (nHour > 24)
		{
			nHour -= 25;
		}
		JsonCompanyRules jsonCompanyRules = null;
		this.mapRoster.TryGetValue(coWorker.strID, out jsonCompanyRules);
		if (jsonCompanyRules == null)
		{
			return JsonCompany.NullShift;
		}
		if (nHour != 24)
		{
			return JsonCompanyRules.Shifts()[jsonCompanyRules.aHours[nHour]];
		}
		if (coWorker.HasCond("IsPlayer"))
		{
			return JsonCompanyRules.Shifts()[jsonCompanyRules.aHours[23]];
		}
		return JsonCompany.NullShift;
	}

	// Reads one named duty level from the worker's roster rules.
	public int GetDutyLevel(CondOwner coWorker, string strDuty)
	{
		if (coWorker == null || string.IsNullOrEmpty(coWorker.strID) || string.IsNullOrEmpty(strDuty) || !JsonCompanyRules.aDutiesNew.Contains(strDuty))
		{
			return JsonCompanyRules.nDutyDefault;
		}
		JsonCompanyRules jsonCompanyRules = null;
		this.mapRoster.TryGetValue(coWorker.strID, out jsonCompanyRules);
		if (jsonCompanyRules == null)
		{
			return JsonCompanyRules.nDutyDefault;
		}
		int num = Array.IndexOf<string>(JsonCompanyRules.aDutiesNew, strDuty);
		if (num < 0 || jsonCompanyRules.aDutyLvls.Length <= num)
		{
			return JsonCompanyRules.nDutyDefault;
		}
		return jsonCompanyRules.aDutyLvls[num];
	}

	// Fallback shift used when no valid roster/shift mapping exists.
	public static JsonShift NullShift
	{
		get
		{
			if (JsonCompany.jsNull == null)
			{
				JsonCompany.jsNull = new JsonShift(-1, "Null Shift", null, Color.magenta);
			}
			return JsonCompany.jsNull;
		}
	}

	// Deep-ish copy of roster rules for save duplication or snapshotting.
	public JsonCompany Clone()
	{
		JsonCompany jsonCompany = new JsonCompany();
		jsonCompany._strName = this._strName;
		jsonCompany.strRegID = this.strRegID;
		if (this.mapRoster != null)
		{
			jsonCompany.mapRoster = new Dictionary<string, JsonCompanyRules>();
			foreach (KeyValuePair<string, JsonCompanyRules> keyValuePair in this.mapRoster)
			{
				jsonCompany.mapRoster[keyValuePair.Key] = this.mapRoster[keyValuePair.Key].Clone();
			}
		}
		if (this.jcrDefaultRules != null)
		{
			jsonCompany.jcrDefaultRules = this.jcrDefaultRules.Clone();
		}
		return jsonCompany;
	}

	// Clears roster references before the company object is discarded.
	public void Destroy()
	{
		if (this.mapRoster != null)
		{
			this.mapRoster.Clear();
		}
		this.mapRoster = null;
	}

	// Permission toggles for common crew policy flags.
	public void SetPermissionAirlock(string strCO, bool bAllowed)
	{
		JsonCompanyRules jsonCompanyRules;
		if (strCO == null || this.mapRoster == null || !this.mapRoster.TryGetValue(strCO, out jsonCompanyRules))
		{
			return;
		}
		jsonCompanyRules.bAirlockPermission = bAllowed;
	}

	public void SetPermissionShore(string strCO, bool bAllowed)
	{
		JsonCompanyRules jsonCompanyRules;
		if (strCO == null || this.mapRoster == null || !this.mapRoster.TryGetValue(strCO, out jsonCompanyRules))
		{
			return;
		}
		jsonCompanyRules.bShoreLeave = bAllowed;
	}

	public void SetPermissionRestore(string strCO, bool bAllowed)
	{
		JsonCompanyRules jsonCompanyRules;
		if (strCO == null || this.mapRoster == null || !this.mapRoster.TryGetValue(strCO, out jsonCompanyRules))
		{
			return;
		}
		jsonCompanyRules.bRestorePermission = bAllowed;
	}

	// Returns the internal company id for logs/debugging.
	public override string ToString()
	{
		return this._strName;
	}

	public string strName
	{
		get
		{
			return this._strName;
		}
		set
		{
			if (value == null)
			{
				return;
			}
			string strName = this._strName;
			this._strName = value;
			if (CrewSim.system != null && !CrewSim.system.RenameCompany(strName, this))
			{
				this._strName = strName;
				return;
			}
		}
	}

	private static JsonShift jsNull;

	public string[] ranks;
}
