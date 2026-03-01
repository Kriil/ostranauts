using System;
using System.Collections.Generic;
using System.Linq;
using Ostranauts.Core;
using Ostranauts.Events;
using Ostranauts.Objectives;
using UnityEngine;

public class Ledger
{
	public static void Init(JsonLedgerLI[] aLIs)
	{
		Ledger.fCashFlow = 0f;
		if (Ledger.dictPayees != null)
		{
			Ledger.dictPayees.Clear();
		}
		else
		{
			Ledger.dictPayees = new Dictionary<string, List<LedgerLI>>();
		}
		if (Ledger.dictPayors != null)
		{
			Ledger.dictPayors.Clear();
		}
		else
		{
			Ledger.dictPayors = new Dictionary<string, List<LedgerLI>>();
		}
		if (Ledger.aHourly != null)
		{
			Ledger.aHourly.Clear();
		}
		else
		{
			Ledger.aHourly = new List<LedgerLI>();
		}
		if (Ledger.aShiftly != null)
		{
			Ledger.aShiftly.Clear();
		}
		else
		{
			Ledger.aShiftly = new List<LedgerLI>();
		}
		if (Ledger.aDaily != null)
		{
			Ledger.aDaily.Clear();
		}
		else
		{
			Ledger.aDaily = new List<LedgerLI>();
		}
		if (Ledger.aMonthly != null)
		{
			Ledger.aMonthly.Clear();
		}
		else
		{
			Ledger.aMonthly = new List<LedgerLI>();
		}
		if (Ledger.aYearly != null)
		{
			Ledger.aYearly.Clear();
		}
		else
		{
			Ledger.aYearly = new List<LedgerLI>();
		}
		if (Ledger.aMortgage != null)
		{
			Ledger.aMortgage.Clear();
		}
		else
		{
			Ledger.aMortgage = new List<LedgerLI>();
		}
		if (aLIs != null)
		{
			Ledger.bSpawnPayments = false;
			foreach (JsonLedgerLI jsLI in aLIs)
			{
				Ledger.AddLI(new LedgerLI(jsLI));
			}
			Ledger.bSpawnPayments = true;
		}
	}

	public static void AddLI(LedgerLI li)
	{
		if (li == null || (double)Mathf.Abs(li.fAmount) < 0.005)
		{
			return;
		}
		if (li.Repeats != LedgerLI.Frequency.OneTime)
		{
			Ledger.AddRepeatingLI(li);
			return;
		}
		if (!Ledger.dictPayees.ContainsKey(li.strPayee))
		{
			Ledger.dictPayees[li.strPayee] = new List<LedgerLI>();
		}
		if (!Ledger.dictPayors.ContainsKey(li.strPayor))
		{
			Ledger.dictPayors[li.strPayor] = new List<LedgerLI>();
		}
		bool flag = true;
		foreach (LedgerLI liA in Ledger.dictPayors[li.strPayor])
		{
			if (LedgerLI.Same(liA, li))
			{
				flag = false;
				break;
			}
		}
		if (flag)
		{
			Ledger.dictPayees[li.strPayee].Add(li);
			Ledger.dictPayors[li.strPayor].Add(li);
		}
		Ledger.GetMonthCash(StarSystem.fEpoch);
		CrewSim.SetCashButton(CrewSim.coPlayer.GetCondAmount("StatUSD"));
		if (li.strPayor == CrewSim.coPlayer.strName && CrewSim.coPlayer.HasCond("TutorialNoCashButtonYet"))
		{
			Objective objective = new Objective(CrewSim.coPlayer, "Open Player Finances", "TNotTutorialNoCashButtonYet");
			objective.strDisplayDesc = "Click your money total button to see and pay your bills.";
			objective.bTutorial = true;
			MonoSingleton<ObjectiveTracker>.Instance.AddObjective(objective);
		}
	}

	public static void AddLI(string strLedgerDef, CondOwner coPayor, CondOwner coPayee)
	{
		if (strLedgerDef != null)
		{
			JsonLedgerDef ledgerDef = DataHandler.GetLedgerDef(strLedgerDef);
			if (ledgerDef != null)
			{
				string strPayee = coPayee.strID;
				if (ledgerDef.bPayATC)
				{
					strPayee = AIShipManager.strATCLast + Interaction.STR_GUI_REFUEL_PORT_SUFFIX;
				}
				else if (!string.IsNullOrEmpty(ledgerDef.strPSpecOrCtPayee))
				{
					JsonPersonSpec personSpec = DataHandler.GetPersonSpec(ledgerDef.strPSpecOrCtPayee);
					if (personSpec != null)
					{
						PersonSpec person = StarSystem.GetPerson(personSpec, coPayor.socUs, false, null, null);
						if (person != null)
						{
							strPayee = person.strCO;
						}
					}
					else
					{
						CondTrigger condTrigger = DataHandler.GetCondTrigger(ledgerDef.strPSpecOrCtPayee);
						if (condTrigger != null && !condTrigger.IsBlank() && coPayee.ship != null)
						{
							CondOwner cofirstOccurrence = coPayee.ship.GetCOFirstOccurrence(condTrigger, true, false, true);
							if (cofirstOccurrence != null)
							{
								strPayee = cofirstOccurrence.strNameFriendly;
							}
						}
					}
				}
				if (ledgerDef.bPaid)
				{
					coPayor.AddCondAmount(ledgerDef.strCurrency, (double)(-(double)ledgerDef.fAmount), 0.0, 0f);
				}
				LedgerLI li = new LedgerLI(strPayee, coPayor.strID, ledgerDef.fAmount, ledgerDef.strDesc, ledgerDef.strCurrency, StarSystem.fEpoch, ledgerDef.bPaid, ledgerDef.Frequency);
				Ledger.AddLI(li);
			}
		}
	}

	private static void AddRepeatingLI(LedgerLI li)
	{
		if (li == null || (double)Mathf.Abs(li.fAmount) < 0.005 || li.Repeats == LedgerLI.Frequency.OneTime)
		{
			return;
		}
		List<LedgerLI> list = null;
		if (li.Repeats == LedgerLI.Frequency.Hourly)
		{
			list = Ledger.aHourly;
		}
		else if (li.Repeats == LedgerLI.Frequency.Shiftly)
		{
			list = Ledger.aShiftly;
		}
		else if (li.Repeats == LedgerLI.Frequency.Daily)
		{
			list = Ledger.aDaily;
		}
		else if (li.Repeats == LedgerLI.Frequency.Monthly)
		{
			list = Ledger.aMonthly;
		}
		else if (li.Repeats == LedgerLI.Frequency.Yearly)
		{
			list = Ledger.aYearly;
		}
		else if (li.Repeats == LedgerLI.Frequency.Mortgage)
		{
			list = Ledger.aMortgage;
		}
		foreach (LedgerLI liA in list)
		{
			if (LedgerLI.Same(liA, li))
			{
				return;
			}
		}
		list.Add(li);
		if (!Ledger.bSpawnPayments || li.fTime > StarSystem.fEpoch)
		{
			return;
		}
		LedgerLI ledgerLI = li.Clone();
		ledgerLI.Repeats = LedgerLI.Frequency.OneTime;
		if (li.Repeats == LedgerLI.Frequency.Mortgage)
		{
			ledgerLI.fAmount = MathUtils.MortgagePaymentPerShift(ledgerLI.fAmount);
			LedgerLI ledgerLI2 = ledgerLI;
			ledgerLI2.strDesc = ledgerLI2.strDesc + DataHandler.GetString("GUI_FINANCE_MORTGAGE02", false) + li.fAmount.ToString("n");
		}
		Ledger.AddLI(ledgerLI);
	}

	public static float AddDownPayment(CondOwner co, string shipRegId, float amount)
	{
		LedgerLI mortgageForShip = Ledger.GetMortgageForShip(shipRegId);
		Ledger.RemoveLI(mortgageForShip);
		List<LedgerLI> unpaidLIs = Ledger.GetUnpaidLIs(mortgageForShip.strPayee, mortgageForShip.strPayor, null, true, false);
		if (unpaidLIs != null)
		{
			foreach (LedgerLI ledgerLI in unpaidLIs)
			{
				if (!ledgerLI.Paid && ledgerLI.strDesc.Contains(shipRegId))
				{
					Ledger.RemoveLI(ledgerLI);
				}
			}
		}
		float num = mortgageForShip.fAmount - amount;
		if (num > 0f)
		{
			mortgageForShip.fAmount = num;
			num = 0f;
			Ledger.AddLI(mortgageForShip);
			Ledger.UpdateLedger(co, mortgageForShip.strPayee, (double)amount, "Ship Sale Escrow for " + mortgageForShip.strDesc);
			Ledger.UpdateLedger(co, mortgageForShip.strPayee, (double)(-(double)amount), "Repaid Lender on " + mortgageForShip.strDesc);
		}
		else
		{
			Ledger.UpdateLedger(co, mortgageForShip.strPayee, (double)mortgageForShip.fAmount, "Ship Sale Escrow for " + mortgageForShip.strDesc);
			Ledger.UpdateLedger(co, mortgageForShip.strPayee, (double)(-(double)mortgageForShip.fAmount), "Repaid Lender on " + mortgageForShip.strDesc);
		}
		return Mathf.Abs(num);
	}

	public static void UpdateLedger(CondOwner COSelf, string COThemFriendlyName, double dfReceived, string description)
	{
		LedgerLI li;
		if (dfReceived >= 0.0)
		{
			li = new LedgerLI(COSelf.FriendlyName, COThemFriendlyName, Convert.ToSingle(dfReceived), description, GUIFinance.strCondCurr, StarSystem.fEpoch, true, LedgerLI.Frequency.OneTime);
		}
		else
		{
			li = new LedgerLI(COThemFriendlyName, COSelf.FriendlyName, Convert.ToSingle(-dfReceived), description, GUIFinance.strCondCurr, StarSystem.fEpoch, true, LedgerLI.Frequency.OneTime);
		}
		Ledger.AddLI(li);
		if (dfReceived < 0.0)
		{
			COSelf.LogMessage(DataHandler.GetString("GUI_FINANCE_LOG_PAID", false) + COThemFriendlyName + ": " + (-dfReceived).ToString("n"), "Bad", COSelf.strName);
		}
		else if (dfReceived > 0.0)
		{
			COSelf.LogMessage(DataHandler.GetString("GUI_FINANCE_LOG_RECEIVED", false) + COThemFriendlyName + ": " + dfReceived.ToString("n"), "Good", COSelf.strName);
		}
	}

	public static void ProcessRepeating(double fTimeDelta, LedgerLI.Frequency frequency)
	{
		bool flag = false;
		if (frequency == LedgerLI.Frequency.Yearly)
		{
			Ledger.ProcessRepeatingOfType(fTimeDelta, LedgerLI.Frequency.Yearly);
			flag = true;
		}
		if (flag || frequency == LedgerLI.Frequency.Monthly)
		{
			Ledger.ProcessRepeatingOfType(fTimeDelta, LedgerLI.Frequency.Monthly);
			flag = true;
		}
		if (flag || frequency == LedgerLI.Frequency.Daily)
		{
			Ledger.ProcessRepeatingOfType(fTimeDelta, LedgerLI.Frequency.Daily);
			flag = true;
		}
		if (flag || frequency == LedgerLI.Frequency.Shiftly || frequency == LedgerLI.Frequency.Mortgage)
		{
			Ledger.ProcessRepeatingOfType(fTimeDelta, LedgerLI.Frequency.Shiftly);
			Ledger.ProcessRepeatingOfType(fTimeDelta, LedgerLI.Frequency.Mortgage);
			flag = true;
		}
		if (flag || frequency == LedgerLI.Frequency.Hourly)
		{
			Ledger.ProcessRepeatingOfType(fTimeDelta, LedgerLI.Frequency.Hourly);
		}
	}

	private static void ProcessRepeatingOfType(double fTimeDelta, LedgerLI.Frequency frequency)
	{
		if (frequency == LedgerLI.Frequency.OneTime)
		{
			return;
		}
		List<LedgerLI> list = null;
		if (frequency == LedgerLI.Frequency.Hourly)
		{
			list = Ledger.aHourly;
		}
		else if (frequency == LedgerLI.Frequency.Shiftly)
		{
			list = Ledger.aShiftly;
		}
		else if (frequency == LedgerLI.Frequency.Daily)
		{
			list = Ledger.aDaily;
		}
		else if (frequency == LedgerLI.Frequency.Monthly)
		{
			list = Ledger.aMonthly;
		}
		else if (frequency == LedgerLI.Frequency.Yearly)
		{
			list = Ledger.aYearly;
		}
		else if (frequency == LedgerLI.Frequency.Mortgage)
		{
			list = Ledger.aMortgage;
		}
		foreach (LedgerLI ledgerLI in list)
		{
			if (ledgerLI.fTime <= StarSystem.fEpoch)
			{
				if (frequency == LedgerLI.Frequency.Hourly)
				{
					for (double num = StarSystem.fEpoch - fTimeDelta + 3600.0; num <= StarSystem.fEpoch; num += 3600.0)
					{
						LedgerLI ledgerLI2 = ledgerLI.Clone();
						ledgerLI2.Repeats = LedgerLI.Frequency.OneTime;
						ledgerLI2.fTime = StarSystem.fEpoch;
						LedgerLI ledgerLI3 = ledgerLI2;
						ledgerLI3.strDesc = ledgerLI3.strDesc + " (" + MathUtils.GetUTCFromS(num) + ")";
						Ledger.AddLI(ledgerLI2);
					}
				}
				else
				{
					LedgerLI ledgerLI4 = ledgerLI.Clone();
					ledgerLI4.Repeats = LedgerLI.Frequency.OneTime;
					ledgerLI4.fTime = StarSystem.fEpoch;
					if (frequency == LedgerLI.Frequency.Mortgage)
					{
						ledgerLI4.fAmount = MathUtils.MortgagePaymentPerShift(ledgerLI);
						LedgerLI ledgerLI5 = ledgerLI4;
						ledgerLI5.strDesc = ledgerLI5.strDesc + DataHandler.GetString("GUI_FINANCE_MORTGAGE02", false) + ledgerLI.fAmount.ToString("n");
						if (ledgerLI4.fAmount > ledgerLI.fAmount)
						{
							ledgerLI4.fAmount = ledgerLI.fAmount;
						}
					}
					Ledger.AddLI(ledgerLI4);
				}
			}
		}
	}

	public static void Skip(double epoch)
	{
		List<LedgerLI> list = new List<LedgerLI>();
		List<LedgerLI> unpaidLIs = Ledger.GetUnpaidLIs(null, CrewSim.coPlayer.strID, null, false, false);
		bool flag = false;
		string @string = DataHandler.GetString("GUI_FINANCE_OVERDUE", false);
		string string2 = DataHandler.GetString("GUI_FINANCE_LATE", false);
		using (List<LedgerLI>.Enumerator enumerator = unpaidLIs.GetEnumerator())
		{
			while (enumerator.MoveNext())
			{
				Ledger.<Skip>c__AnonStorey0 <Skip>c__AnonStorey = new Ledger.<Skip>c__AnonStorey0();
				<Skip>c__AnonStorey.li = enumerator.Current;
				if (Ledger.IsMortgage(<Skip>c__AnonStorey.li) && !<Skip>c__AnonStorey.li.strDesc.Contains(@string))
				{
					<Skip>c__AnonStorey.li.strDesc = @string + <Skip>c__AnonStorey.li.strDesc;
				}
				string descr = string2 + MathUtils.GetUTCFromS(<Skip>c__AnonStorey.li.fTime);
				float num = <Skip>c__AnonStorey.li.fAmount * 0.175f;
				LedgerLI ledgerLI = unpaidLIs.FirstOrDefault((LedgerLI x) => x.strPayee == <Skip>c__AnonStorey.li.strPayee && x.strPayor == <Skip>c__AnonStorey.li.strPayor && x.strDesc == descr && !x.Paid);
				if (ledgerLI != null)
				{
					num += ledgerLI.fAmount;
					ledgerLI.fTimePaid = -epoch;
				}
				LedgerLI ledgerLI2 = list.FirstOrDefault((LedgerLI x) => x.strPayee == <Skip>c__AnonStorey.li.strPayee && x.strPayor == <Skip>c__AnonStorey.li.strPayor && x.strDesc == descr);
				if (ledgerLI2 != null)
				{
					ledgerLI2.fAmount += num;
				}
				else
				{
					LedgerLI item = new LedgerLI(<Skip>c__AnonStorey.li.strPayee, <Skip>c__AnonStorey.li.strPayor, num, descr, <Skip>c__AnonStorey.li.strCurrency, StarSystem.fEpoch, false, LedgerLI.Frequency.OneTime);
					list.Add(item);
				}
				flag = true;
			}
		}
		foreach (LedgerLI li in list)
		{
			Ledger.AddLI(li);
		}
		if (flag)
		{
			AudioManager.am.PlayAudioEmitter("ShipUIBtnFinanceAcceptNeg", false, false);
			CrewSim.coPlayer.LogMessage(DataHandler.GetString("GUI_FINANCE_SHIFT_PENAL", false), "Bad", CrewSim.coPlayer.strID);
		}
	}

	public static void RemoveLI(LedgerLI li)
	{
		if (li == null)
		{
			return;
		}
		if (Ledger.dictPayees.ContainsKey(li.strPayee))
		{
			List<LedgerLI> list = new List<LedgerLI>(Ledger.dictPayees[li.strPayee]);
			foreach (LedgerLI liA in list)
			{
				if (LedgerLI.Same(liA, li))
				{
					Ledger.dictPayees[li.strPayee].Remove(li);
				}
			}
		}
		if (Ledger.dictPayors.ContainsKey(li.strPayor))
		{
			List<LedgerLI> list2 = new List<LedgerLI>(Ledger.dictPayors[li.strPayor]);
			foreach (LedgerLI liA2 in list2)
			{
				if (LedgerLI.Same(liA2, li))
				{
					Ledger.dictPayors[li.strPayor].Remove(li);
				}
			}
		}
		if (li.Repeats != LedgerLI.Frequency.OneTime)
		{
			List<LedgerLI> list3 = null;
			if (li.Repeats == LedgerLI.Frequency.Hourly)
			{
				list3 = Ledger.aHourly;
			}
			else if (li.Repeats == LedgerLI.Frequency.Shiftly)
			{
				list3 = Ledger.aShiftly;
			}
			else if (li.Repeats == LedgerLI.Frequency.Daily)
			{
				list3 = Ledger.aDaily;
			}
			else if (li.Repeats == LedgerLI.Frequency.Monthly)
			{
				list3 = Ledger.aMonthly;
			}
			else if (li.Repeats == LedgerLI.Frequency.Yearly)
			{
				list3 = Ledger.aYearly;
			}
			else if (li.Repeats == LedgerLI.Frequency.Mortgage)
			{
				list3 = Ledger.aMortgage;
			}
			List<LedgerLI> list4 = new List<LedgerLI>();
			foreach (LedgerLI ledgerLI in list3)
			{
				if (LedgerLI.Same(ledgerLI, li))
				{
					list4.Add(ledgerLI);
				}
			}
			foreach (LedgerLI item in list4)
			{
				list3.Remove(item);
			}
		}
		Ledger.GetMonthCash(StarSystem.fEpoch);
		CrewSim.SetCashButton(CrewSim.coPlayer.GetCondAmount("StatUSD"));
	}

	public static void PayLI(LedgerLI li)
	{
		if (li == null || li.Paid)
		{
			return;
		}
		if (Ledger.dictPayees.ContainsKey(li.strPayee))
		{
			li.fTimePaid = StarSystem.fEpoch;
		}
		if (Ledger.dictPayors.ContainsKey(li.strPayor))
		{
			li.fTimePaid = StarSystem.fEpoch;
		}
		if (li.Paid)
		{
			LedgerLI mortgageForPayment = Ledger.GetMortgageForPayment(li);
			if (mortgageForPayment != null)
			{
				mortgageForPayment.fAmount -= li.fAmount;
				if (mortgageForPayment.fAmount <= 0f)
				{
					mortgageForPayment.fTimePaid = StarSystem.fEpoch;
				}
			}
			Ledger.GetMonthCash(StarSystem.fEpoch);
			CrewSim.SetCashButton(CrewSim.coPlayer.GetCondAmount("StatUSD"));
			Ledger.onLedgerPaid.Invoke();
		}
	}

	public static LedgerLI GetMortgageForPayment(LedgerLI li)
	{
		foreach (LedgerLI ledgerLI in Ledger.aMortgage)
		{
			if (li.strPayee == ledgerLI.strPayee && li.strPayor == ledgerLI.strPayor && li.strDesc.IndexOf(ledgerLI.strDesc) >= 0 && li.strCurrency == ledgerLI.strCurrency)
			{
				return ledgerLI;
			}
		}
		return null;
	}

	public static LedgerLI GetMortgageForShip(string regId)
	{
		foreach (LedgerLI ledgerLI in Ledger.aMortgage)
		{
			if (ledgerLI.strDesc.Contains(regId))
			{
				return ledgerLI;
			}
		}
		return null;
	}

	public static bool IsMortgage(LedgerLI li)
	{
		return li != null && Ledger.GetMortgageForPayment(li) != null;
	}

	public static float GetUnpaidAmount(string strPayee, string strPayor, string strDesc)
	{
		float num = 0f;
		List<LedgerLI> list2;
		if (strPayee != null)
		{
			List<LedgerLI> list;
			if (Ledger.dictPayees.TryGetValue(strPayee, out list) && list != null)
			{
				foreach (LedgerLI ledgerLI in list)
				{
					if (!ledgerLI.Paid)
					{
						if (strPayor == null || !(ledgerLI.strPayor != strPayor))
						{
							if (strDesc == null || !(ledgerLI.strDesc != strDesc))
							{
								num += ledgerLI.fAmount;
							}
						}
					}
				}
			}
		}
		else if (strPayor != null && Ledger.dictPayors.TryGetValue(strPayor, out list2) && list2 != null)
		{
			foreach (LedgerLI ledgerLI2 in list2)
			{
				if (!ledgerLI2.Paid)
				{
					if (strDesc == null || !(ledgerLI2.strDesc != strDesc))
					{
						num += ledgerLI2.fAmount;
					}
				}
			}
		}
		return num;
	}

	public static void GetCRO(GameObject goCanvas)
	{
		GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(Resources.Load("prefabPnlOptionSelect") as GameObject);
		List<string> list = new List<string>();
		string text = "DPQZSJHIUACMVFACPUUMFAHBNFT";
		string text2 = string.Empty;
		foreach (char c in text)
		{
			if (c == 'A')
			{
				list.Add(text2);
				text2 = string.Empty;
			}
			else
			{
				c -= '\u0001';
				text2 += c;
			}
		}
		list.Add(text2);
		gameObject.transform.SetParent(goCanvas.transform, false);
		gameObject.GetComponent<GUIOptionSelect>().Init(list, null);
	}

	public static List<LedgerLI> GetUnpaidLIs(string strPayee, string strPayor, string strDesc, bool bIncludeRepeating, bool includeClosed = false)
	{
		List<LedgerLI> list = new List<LedgerLI>();
		if (strPayee == null && strPayor == null)
		{
			return list;
		}
		List<LedgerLI> list3;
		if (strPayee != null)
		{
			List<LedgerLI> list2;
			if (Ledger.dictPayees.TryGetValue(strPayee, out list2) && list2 != null)
			{
				foreach (LedgerLI ledgerLI in list2)
				{
					if (!ledgerLI.Paid)
					{
						if (strPayor == null || !(ledgerLI.strPayor != strPayor))
						{
							if (strDesc == null || !(ledgerLI.strDesc != strDesc))
							{
								list.Add(ledgerLI);
							}
						}
					}
				}
			}
		}
		else if (Ledger.dictPayors.TryGetValue(strPayor, out list3) && list3 != null)
		{
			foreach (LedgerLI ledgerLI2 in list3)
			{
				if (!ledgerLI2.Paid)
				{
					if (strDesc == null || !(ledgerLI2.strDesc != strDesc))
					{
						list.Add(ledgerLI2);
					}
				}
			}
		}
		if (bIncludeRepeating)
		{
			List<LedgerLI> list4 = new List<LedgerLI>(Ledger.aHourly);
			list4.AddRange(Ledger.aShiftly);
			list4.AddRange(Ledger.aDaily);
			list4.AddRange(Ledger.aMonthly);
			list4.AddRange(Ledger.aYearly);
			foreach (LedgerLI ledgerLI3 in list4)
			{
				if (!ledgerLI3.Paid)
				{
					if (strPayor == null || !(ledgerLI3.strPayor != strPayor))
					{
						if (strPayee == null || !(ledgerLI3.strPayee != strPayee))
						{
							if (strDesc == null || !(ledgerLI3.strDesc != strDesc))
							{
								list.Add(ledgerLI3);
							}
						}
					}
				}
			}
		}
		return list;
	}

	public static List<LedgerLI> GetMonthLIs(double dfEpoch)
	{
		List<LedgerLI> list = new List<LedgerLI>();
		int monthFromS = MathUtils.GetMonthFromS(dfEpoch);
		int yearFromS = MathUtils.GetYearFromS(dfEpoch);
		foreach (List<LedgerLI> list2 in Ledger.dictPayees.Values)
		{
			foreach (LedgerLI ledgerLI in list2)
			{
				int monthFromS2 = MathUtils.GetMonthFromS(ledgerLI.fTime);
				int yearFromS2 = MathUtils.GetYearFromS(ledgerLI.fTime);
				if (monthFromS == monthFromS2 && yearFromS == yearFromS2)
				{
					list.Add(ledgerLI);
				}
			}
		}
		return list;
	}

	public static List<LedgerLI> GetShiftLIs(double dfEpoch, bool includeUnpaid)
	{
		List<LedgerLI> list = new List<LedgerLI>();
		int shiftFromS = MathUtils.GetShiftFromS(dfEpoch);
		int dayOfYearFromS = MathUtils.GetDayOfYearFromS(dfEpoch);
		int yearFromS = MathUtils.GetYearFromS(dfEpoch);
		foreach (List<LedgerLI> list2 in Ledger.dictPayees.Values)
		{
			foreach (LedgerLI ledgerLI in list2)
			{
				if (ledgerLI.Paid)
				{
					int yearFromS2 = MathUtils.GetYearFromS(ledgerLI.fTime);
					int yearFromS3 = MathUtils.GetYearFromS(Math.Abs(ledgerLI.fTimePaid));
					if (yearFromS2 <= yearFromS && yearFromS3 >= yearFromS)
					{
						int dayOfYearFromS2 = MathUtils.GetDayOfYearFromS(ledgerLI.fTime);
						int dayOfYearFromS3 = MathUtils.GetDayOfYearFromS(Math.Abs(ledgerLI.fTimePaid));
						if ((dayOfYearFromS2 <= dayOfYearFromS || yearFromS2 != yearFromS) && (dayOfYearFromS3 >= dayOfYearFromS || yearFromS3 != yearFromS))
						{
							int shiftFromS2 = MathUtils.GetShiftFromS(ledgerLI.fTime);
							int shiftFromS3 = MathUtils.GetShiftFromS(Math.Abs(ledgerLI.fTimePaid));
							if ((shiftFromS2 <= shiftFromS || dayOfYearFromS2 != dayOfYearFromS) && (shiftFromS3 >= shiftFromS || dayOfYearFromS3 != dayOfYearFromS))
							{
								list.Add(ledgerLI);
							}
						}
					}
				}
				else
				{
					int yearFromS4 = MathUtils.GetYearFromS(ledgerLI.fTime);
					if (yearFromS4 <= yearFromS)
					{
						int dayOfYearFromS4 = MathUtils.GetDayOfYearFromS(ledgerLI.fTime);
						if (dayOfYearFromS4 <= dayOfYearFromS || yearFromS4 != yearFromS)
						{
							int shiftFromS4 = MathUtils.GetShiftFromS(ledgerLI.fTime);
							if (shiftFromS4 <= shiftFromS || dayOfYearFromS4 != dayOfYearFromS)
							{
								list.Add(ledgerLI);
							}
						}
					}
				}
			}
		}
		return list;
	}

	public static List<LedgerLI> GetDayLIs(double dfEpoch)
	{
		List<LedgerLI> list = new List<LedgerLI>();
		int dayOfYearFromS = MathUtils.GetDayOfYearFromS(dfEpoch);
		int yearFromS = MathUtils.GetYearFromS(dfEpoch);
		foreach (List<LedgerLI> list2 in Ledger.dictPayees.Values)
		{
			foreach (LedgerLI ledgerLI in list2)
			{
				int dayOfYearFromS2 = MathUtils.GetDayOfYearFromS(ledgerLI.fTime);
				int yearFromS2 = MathUtils.GetYearFromS(ledgerLI.fTime);
				if (dayOfYearFromS == dayOfYearFromS2 && yearFromS == yearFromS2)
				{
					list.Add(ledgerLI);
				}
			}
		}
		return list;
	}

	public static void GetMonthCash(double dfEpoch = -1.0)
	{
		Ledger.fCashFlow = 0f;
		int monthFromS = MathUtils.GetMonthFromS(dfEpoch);
		foreach (List<LedgerLI> list in Ledger.dictPayees.Values)
		{
			foreach (LedgerLI ledgerLI in list)
			{
				int monthFromS2 = MathUtils.GetMonthFromS(ledgerLI.fTime);
				if (monthFromS == monthFromS2)
				{
					if (ledgerLI.strPayee == CrewSim.coPlayer.strID)
					{
						Ledger.fCashFlow += ledgerLI.fAmount;
					}
					else if (ledgerLI.strPayor == CrewSim.coPlayer.strID)
					{
						Ledger.fCashFlow -= ledgerLI.fAmount;
					}
				}
			}
		}
	}

	public static bool HasUnpaidDockingFees(string regIdStation, CondOwner coUs)
	{
		if (coUs == null || string.IsNullOrEmpty(regIdStation))
		{
			return false;
		}
		bool result = false;
		string strPayee = regIdStation + DataHandler.GetString("GUI_REFUEL_PORT_SUFFIX", false);
		List<LedgerLI> unpaidLIs = Ledger.GetUnpaidLIs(strPayee, coUs.strName, null, false, false);
		foreach (LedgerLI ledgerLI in unpaidLIs)
		{
			if (ledgerLI.fAmount > 0f)
			{
				result = true;
				if (coUs.HasCond("IsDockingFreePass"))
				{
					result = false;
				}
				break;
			}
		}
		return result;
	}

	public static void UpdateCOID(string oldId, string newId)
	{
		if (string.IsNullOrEmpty(oldId) || string.IsNullOrEmpty(newId) || oldId == newId)
		{
			return;
		}
		if (!Ledger.dictPayees.ContainsKey(oldId) && !Ledger.dictPayors.ContainsKey(oldId))
		{
			return;
		}
		JsonLedgerLI[] jsonsave = Ledger.GetJSONSave();
		foreach (JsonLedgerLI jsonLedgerLI in jsonsave)
		{
			if (jsonLedgerLI != null)
			{
				if (jsonLedgerLI.strPayee == oldId)
				{
					jsonLedgerLI.strPayee = newId;
				}
				if (jsonLedgerLI.strPayor == oldId)
				{
					jsonLedgerLI.strPayor = newId;
				}
			}
		}
		Ledger.Init(jsonsave);
	}

	public static JsonLedgerLI[] GetJSONSave()
	{
		List<JsonLedgerLI> list = new List<JsonLedgerLI>();
		foreach (string key in Ledger.dictPayees.Keys)
		{
			List<LedgerLI> list2 = Ledger.dictPayees[key];
			foreach (LedgerLI ledgerLI in list2)
			{
				JsonLedgerLI json = ledgerLI.GetJSON();
				list.Add(json);
			}
		}
		foreach (LedgerLI ledgerLI2 in Ledger.aHourly)
		{
			JsonLedgerLI json2 = ledgerLI2.GetJSON();
			list.Add(json2);
		}
		foreach (LedgerLI ledgerLI3 in Ledger.aShiftly)
		{
			JsonLedgerLI json3 = ledgerLI3.GetJSON();
			list.Add(json3);
		}
		foreach (LedgerLI ledgerLI4 in Ledger.aDaily)
		{
			JsonLedgerLI json4 = ledgerLI4.GetJSON();
			list.Add(json4);
		}
		foreach (LedgerLI ledgerLI5 in Ledger.aMonthly)
		{
			JsonLedgerLI json5 = ledgerLI5.GetJSON();
			list.Add(json5);
		}
		foreach (LedgerLI ledgerLI6 in Ledger.aYearly)
		{
			JsonLedgerLI json6 = ledgerLI6.GetJSON();
			list.Add(json6);
		}
		foreach (LedgerLI ledgerLI7 in Ledger.aMortgage)
		{
			JsonLedgerLI json7 = ledgerLI7.GetJSON();
			list.Add(json7);
		}
		JsonLedgerLI[] result = list.ToArray();
		list.Clear();
		list = null;
		return result;
	}

	private static Dictionary<string, List<LedgerLI>> dictPayees;

	private static Dictionary<string, List<LedgerLI>> dictPayors;

	private static List<LedgerLI> aHourly;

	private static List<LedgerLI> aShiftly;

	private static List<LedgerLI> aDaily;

	private static List<LedgerLI> aMonthly;

	private static List<LedgerLI> aYearly;

	private static List<LedgerLI> aMortgage;

	public static float fCashFlow = 0f;

	public static bool bSpawnPayments = true;

	public const int MORTGAGE_NUM_PAYMENTS = 720;

	public const float MORTGAGE_RATE = 0.0021f;

	public static readonly string CURRENCY = "StatUSD";

	public static readonly OnLedgerPaid onLedgerPaid = new OnLedgerPaid();
}
