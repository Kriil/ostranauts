using System;
using System.Collections.Generic;
using System.Linq;
using Ostranauts.Events;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Station services/refuel kiosk. Aggregates fuel, life support, power, docking,
// and fines into a billable services list for the current ship.
public class GUIStationRefuel : GUIData
{
	// Builds the service rows, caches labels, and wires the submit button.
	protected override void Awake()
	{
		base.Awake();
		this.tfList = base.transform.Find("pnlScrollingList/Viewport/pnlListContent");
		GameObject original = Resources.Load("GUIShip/GUIStationRow") as GameObject;
		GameObject original2 = Resources.Load("GUIShip/GUIStationRowBlank") as GameObject;
		this.rowFuelHe3 = UnityEngine.Object.Instantiate<GameObject>(original, this.tfList).GetComponent<GUIStationRow>();
		this.rowFuelH2 = UnityEngine.Object.Instantiate<GameObject>(original, this.tfList).GetComponent<GUIStationRow>();
		this.rowFuelRCS = UnityEngine.Object.Instantiate<GameObject>(original, this.tfList).GetComponent<GUIStationRow>();
		this.rowFuelConnect = UnityEngine.Object.Instantiate<GameObject>(original, this.tfList).GetComponent<GUIStationRow>();
		this.rowFuelABL = UnityEngine.Object.Instantiate<GameObject>(original, this.tfList).GetComponent<GUIStationRow>();
		this.rowFuelTotal = UnityEngine.Object.Instantiate<GameObject>(original, this.tfList).GetComponent<GUIStationRow>();
		UnityEngine.Object.Instantiate<GameObject>(original2, this.tfList);
		this.rowLifeO2 = UnityEngine.Object.Instantiate<GameObject>(original, this.tfList).GetComponent<GUIStationRow>();
		this.rowLifeN2 = UnityEngine.Object.Instantiate<GameObject>(original, this.tfList).GetComponent<GUIStationRow>();
		this.rowLifeBlack = UnityEngine.Object.Instantiate<GameObject>(original, this.tfList).GetComponent<GUIStationRow>();
		this.rowLifeExchange = UnityEngine.Object.Instantiate<GameObject>(original, this.tfList).GetComponent<GUIStationRow>();
		this.rowLifeTotal = UnityEngine.Object.Instantiate<GameObject>(original, this.tfList).GetComponent<GUIStationRow>();
		UnityEngine.Object.Instantiate<GameObject>(original2, this.tfList);
		this.rowPower = UnityEngine.Object.Instantiate<GameObject>(original, this.tfList).GetComponent<GUIStationRow>();
		this.rowPowerHookup = UnityEngine.Object.Instantiate<GameObject>(original, this.tfList).GetComponent<GUIStationRow>();
		this.rowPowerTotal = UnityEngine.Object.Instantiate<GameObject>(original, this.tfList).GetComponent<GUIStationRow>();
		UnityEngine.Object.Instantiate<GameObject>(original2, this.tfList);
		this.rowDock = UnityEngine.Object.Instantiate<GameObject>(original, this.tfList).GetComponent<GUIStationRow>();
		this.rowDockTow = UnityEngine.Object.Instantiate<GameObject>(original, this.tfList).GetComponent<GUIStationRow>();
		this.rowDockTotal = UnityEngine.Object.Instantiate<GameObject>(original, this.tfList).GetComponent<GUIStationRow>();
		UnityEngine.Object.Instantiate<GameObject>(original2, this.tfList);
		this.rowFines = UnityEngine.Object.Instantiate<GameObject>(original, this.tfList).GetComponent<GUIStationRow>();
		this.rowFinesTotal = UnityEngine.Object.Instantiate<GameObject>(original, this.tfList).GetComponent<GUIStationRow>();
		this.txtStationName = base.transform.Find("txtStationName").GetComponent<TMP_Text>();
		this.txtNameSub = base.transform.Find("txtNameSub").GetComponent<TMP_Text>();
		this.txtVessel = base.transform.Find("txtVessel").GetComponent<TMP_Text>();
		this.txtMessage = base.transform.Find("pnlService/txtMessage").GetComponent<TMP_Text>();
		this.txtMessage.text = DataHandler.GetString("GUI_REFUEL_DEVICE_SEARCH", false);
		this.txtTotal = base.transform.Find("pnlRefuelFooter/txtTotal").GetComponent<TMP_Text>();
		this.btnSubmit = base.transform.Find("btnSubmit").GetComponent<Button>();
		this.btnSubmit.onClick.AddListener(delegate()
		{
			this.OnSubmit();
		});
		AudioManager.AddBtnAudio(this.btnSubmit.gameObject, "ShipUIBtnRefuelClick", "ShipUIBtnRefuelCancel");
		GUIStationRefuel.strMessageFound = DataHandler.GetString("GUI_REFUEL_DEVICE_FOUND", false);
	}

	// Handles temporary status messaging and the looping audio while row values
	// are being adjusted.
	private void Update()
	{
		this.fMessageTimer -= CrewSim.TimeElapsedUnscaled();
		if (this.fMessageTimer < 0f)
		{
			this.txtMessage.text = GUIStationRefuel.strMessageFound;
			this.fMessageTimer = 3600f;
		}
		if (GUIStationRefuel.bPlayAEUp)
		{
			AudioManager.am.PlayAudioEmitter("ShipUIBtnRefuelValueUp", true, false);
			GUIStationRefuel.bPlayAEUp = false;
		}
		else
		{
			AudioManager.am.StopAudioEmitter("ShipUIBtnRefuelValueUp");
		}
		if (GUIStationRefuel.bPlayAEDown)
		{
			AudioManager.am.PlayAudioEmitter("ShipUIBtnRefuelValueDown", true, false);
			GUIStationRefuel.bPlayAEDown = false;
		}
		else
		{
			AudioManager.am.StopAudioEmitter("ShipUIBtnRefuelValueDown");
		}
	}

	// Initializes the shared service-price table, including gas prices pulled from
	// the `GasPrices` loot definition.
	public static void SetPrices()
	{
		if (GUIStationRefuel.dictPrices != null)
		{
			return;
		}
		GUIStationRefuel.dictPrices = new Dictionary<string, float>();
		GUIStationRefuel.dictPrices["rowFuelHe3"] = GasContainer.GetGasPrice("He3");
		GUIStationRefuel.dictPrices["rowFuelH2"] = GasContainer.GetGasPrice("H2");
		GUIStationRefuel.dictPrices["rowLifeRCS"] = GasContainer.GetGasPrice("N2");
		GUIStationRefuel.dictPrices["rowFuelConnect"] = 28f;
		GUIStationRefuel.dictPrices["rowFuelABL"] = 83.9f;
		GUIStationRefuel.dictPrices["rowLifeO2"] = GasContainer.GetGasPrice("O2");
		GUIStationRefuel.dictPrices["rowLifeN2"] = GasContainer.GetGasPrice("N2");
		GUIStationRefuel.dictPrices["rowLifeBlack"] = 130.9f;
		GUIStationRefuel.dictPrices["rowLifeExchange"] = 112.33f;
		GUIStationRefuel.dictPrices["rowPower"] = 0.12f;
		GUIStationRefuel.dictPrices["rowPowerHookup"] = 28.8f;
		GUIStationRefuel.dictPrices["rowDock"] = 28.8f;
		GUIStationRefuel.dictPrices["rowDockTow"] = 136f;
		GUIStationRefuel.dictPrices["rowDockFacilities"] = 11.83f;
	}

	// Populates the station service rows for the selected ship and station.
	private void SetupFields(Ship objShip)
	{
		Ship shipByRegID = this.COSelf.ship;
		if (CrewSim.system.GetShipByRegID(AIShipManager.strATCLast) != null)
		{
			shipByRegID = CrewSim.system.GetShipByRegID(AIShipManager.strATCLast);
		}
		string text = shipByRegID.strRegID + DataHandler.GetString("GUI_REFUEL_PORT_SUFFIX", false);
		this.txtStationName.text = this.COSelf.ship.strRegID;
		this.txtNameSub.text = text;
		CondOwner objThem = this.COSelf.GetInteractionCurrent().objThem;
		this.ship = objShip;
		this.aAirpumpN2Cans = new List<CondOwner>();
		this.txtVessel.text = ((this.ship == null) ? " - " : (this.ship.strRegID + " " + this.ship.publicName));
		float num = 0f;
		float num2 = 0f;
		CondTrigger condTrigger = new CondTrigger();
		Color color = new Color(0.78039217f, 0.58431375f, 0.20392157f);
		num = 0f;
		num2 = 0f;
		condTrigger = DataHandler.GetCondTrigger("TIsCanisterLHe02Installed");
		if (this.ship != null)
		{
			List<CondOwner> icos = this.ship.GetICOs1(condTrigger, true, false, true);
			foreach (CondOwner condOwner in icos)
			{
				num2 += Convert.ToSingle(condOwner.GetCondAmount("StatVolume")) * 129.11f;
				num += Convert.ToSingle(condOwner.GetCondAmount("StatSolidHe3"));
			}
		}
		this.rowFuelHe3.Init(DataHandler.GetString("GUI_REFUEL_SERVICE_HE3", false), GUIStationRefuel.dictPrices["rowFuelHe3"], 44980.4f, 0f, num, num2, 0f, new Action(this.UpdateFields));
		num = 0f;
		num2 = 0f;
		condTrigger = DataHandler.GetCondTrigger("TIsCanisterLH02Installed");
		if (this.ship != null)
		{
			List<CondOwner> icos = this.ship.GetICOs1(condTrigger, true, false, true);
			foreach (CondOwner condOwner2 in icos)
			{
				num2 += Convert.ToSingle(condOwner2.GetCondAmount("StatVolume")) * 1107f;
				num += Convert.ToSingle(condOwner2.GetCondAmount("StatLiqD2O"));
			}
		}
		this.rowFuelH2.Init(DataHandler.GetString("GUI_REFUEL_SERVICE_D2O", false), GUIStationRefuel.dictPrices["rowFuelH2"], 568221.6f, 0f, num, num2, 0f, new Action(this.UpdateFields));
		num = ((this.ship == null) ? 0f : ((float)this.ship.GetRCSRemain()));
		num2 = ((this.ship == null) ? 0f : ((float)this.ship.GetRCSMax()));
		this.rowFuelRCS.Init(DataHandler.GetString("GUI_REFUEL_SERVICE_RCS", false), GUIStationRefuel.dictPrices["rowLifeRCS"], 568221.6f, 0f, num, num2, 0f, new Action(this.UpdateFields));
		num = 0f;
		num2 = 0f;
		condTrigger = DataHandler.GetCondTrigger("TIsFusionReactorCore01Installed");
		if (this.ship != null)
		{
			Condition cond = DataHandler.GetCond("StatICABLWall");
			float num3 = (cond == null) ? 0f : cond.fClampMax;
			List<CondOwner> icos = this.ship.GetICOs1(condTrigger, true, false, true);
			foreach (CondOwner condOwner3 in icos)
			{
				num2 += num3;
				num += Convert.ToSingle((double)num3 - condOwner3.GetCondAmount("StatICABLWall"));
			}
		}
		if (num < 0f)
		{
			num = 0f;
		}
		if (num2 < num)
		{
			num2 = num;
		}
		this.rowFuelABL.Init(DataHandler.GetString("GUI_REFUEL_SERVICE_ABL", false), GUIStationRefuel.dictPrices["rowFuelABL"], 644f, 0f, num, num2, 0f, new Action(this.UpdateFields));
		float num4 = 0f;
		float unpaidAmount = Ledger.GetUnpaidAmount(text, objThem.strID, DataHandler.GetString("GUI_REFUEL_SERVICE_FUEL_CONNECT", false));
		this.rowFuelConnect.Init(1, DataHandler.GetString("GUI_REFUEL_SERVICE_FUEL_CONNECT", false), unpaidAmount, unpaidAmount);
		this.rowFuelTotal.SetColor(color);
		num4 += unpaidAmount;
		num = 0f;
		num2 = 0f;
		condTrigger = DataHandler.GetCondTrigger("TIsRTAO2Installed");
		if (this.ship != null)
		{
			List<CondOwner> icos = this.ship.GetICOs1(condTrigger, true, false, true);
			foreach (CondOwner condOwner4 in icos)
			{
				GasContainer gasContainer = condOwner4.GasContainer;
				if (!(gasContainer == null))
				{
					float num5 = Convert.ToSingle(condOwner4.GetCondAmount("StatVolume") * condOwner4.GetCondAmount("StatGasPressureMax") / 0.008314000442624092 / condOwner4.GetCondAmount("StatGasTemp") * 0.03199880197644234);
					num2 += num5;
					num += (float)(condOwner4.GetCondAmount("StatGasPressure") / condOwner4.GetCondAmount("StatGasPressureMax") * (double)num5);
				}
			}
		}
		this.rowLifeO2.Init(DataHandler.GetString("GUI_REFUEL_SERVICE_O2", false), GUIStationRefuel.dictPrices["rowLifeO2"], 568221.6f, 0f, num, num2, 0f, new Action(this.UpdateFields));
		num = 0f;
		num2 = 0f;
		if (this.ship != null)
		{
			condTrigger = DataHandler.GetCondTrigger("TIsAirPump02Installed");
			List<CondOwner> icos = objShip.GetICOs1(condTrigger, false, false, false);
			condTrigger = DataHandler.GetCondTrigger("TIsRTAN2Installed");
			foreach (CondOwner condOwner5 in icos)
			{
				foreach (KeyValuePair<string, Vector2> keyValuePair in condOwner5.mapPoints)
				{
					if (keyValuePair.Key.IndexOf("GasInput") >= 0)
					{
						List<CondOwner> list = new List<CondOwner>();
						condOwner5.ship.GetCOsAtWorldCoords1(condOwner5.GetPos(keyValuePair.Key, false), condTrigger, true, false, list);
						foreach (CondOwner condOwner6 in list)
						{
							GasContainer gasContainer2 = condOwner6.GasContainer;
							if (!(gasContainer2 == null))
							{
								float num6 = Convert.ToSingle(condOwner6.GetCondAmount("StatVolume") * condOwner6.GetCondAmount("StatGasPressureMax") / 0.008314000442624092 / condOwner6.GetCondAmount("StatGasTemp") * 0.028013398870825768);
								num2 += num6;
								num += (float)(condOwner6.GetCondAmount("StatGasPressure") / condOwner6.GetCondAmount("StatGasPressureMax") * (double)num6);
								if (this.aAirpumpN2Cans.IndexOf(condOwner6) < 0)
								{
									this.aAirpumpN2Cans.Add(condOwner6);
								}
							}
						}
					}
				}
			}
		}
		this.rowLifeN2.Init(DataHandler.GetString("GUI_REFUEL_SERVICE_N2", false), GUIStationRefuel.dictPrices["rowLifeN2"], 568221.6f, 0f, num, num2, 0f, new Action(this.UpdateFields));
		num = 0f;
		num2 = 0f;
		this.rowLifeBlack.Init(DataHandler.GetString("GUI_REFUEL_SERVICE_BLACKWATER", false), GUIStationRefuel.dictPrices["rowLifeBlack"], 0f, 0f, num, num2, 0f, new Action(this.UpdateFields));
		unpaidAmount = Ledger.GetUnpaidAmount(text, objThem.strID, DataHandler.GetString("GUI_REFUEL_SERVICE_LIFE_EXCHANGE", false));
		this.rowLifeExchange.Init(1, DataHandler.GetString("GUI_REFUEL_SERVICE_LIFE_EXCHANGE", false), unpaidAmount, 0f);
		this.rowLifeTotal.SetColor(color);
		num4 += unpaidAmount;
		num = 0f;
		num2 = 0f;
		if (this.ship != null)
		{
			condTrigger = DataHandler.GetCondTrigger("TIsShipBatteryInstalled");
			List<CondOwner> icos = objShip.GetICOs1(condTrigger, true, false, false);
			foreach (CondOwner condOwner7 in icos)
			{
				Powered component = condOwner7.GetComponent<Powered>();
				if (!(component == null))
				{
					num2 += (float)component.PowerStoredMax;
					num += Convert.ToSingle(condOwner7.GetCondAmount("StatPower"));
				}
			}
		}
		this.rowPower.Init(DataHandler.GetString("GUI_REFUEL_SERVICE_POWER", false), GUIStationRefuel.dictPrices["rowPower"], 1E+09f, 0f, num, num2, 0f, new Action(this.UpdateFields));
		unpaidAmount = Ledger.GetUnpaidAmount(text, objThem.strID, DataHandler.GetString("GUI_REFUEL_SERVICE_POWER_HOOKUP", false));
		this.rowPowerHookup.Init(1, DataHandler.GetString("GUI_REFUEL_SERVICE_POWER_HOOKUP", false), unpaidAmount, 0f);
		this.rowPowerTotal.SetColor(color);
		num4 += unpaidAmount;
		unpaidAmount = Ledger.GetUnpaidAmount(text, objThem.strID, DataHandler.GetString("GUI_REFUEL_SERVICE_DOCK", false));
		this.rowDock.Init(1, DataHandler.GetString("GUI_REFUEL_SERVICE_DOCK", false), unpaidAmount, 0f);
		num4 += unpaidAmount;
		unpaidAmount = Ledger.GetUnpaidAmount(text, objThem.strID, "rowDockTow");
		this.rowDockTow.Init(0, DataHandler.GetString("GUI_REFUEL_SERVICE_TOW", false), unpaidAmount, 0f);
		this.rowDockTotal.SetColor(color);
		num4 += unpaidAmount;
		float total = this.GetTotal(text, objThem.strID);
		this.rowFines.Init(0, DataHandler.GetString("GUI_REFUEL_SERVICE_FINES", false), total - num4, 0f);
		this.rowFinesTotal.SetColor(color);
		this.UpdateFields();
	}

	private void UpdateFields()
	{
		float num = this.rowFuelH2.fTotal + this.rowFuelHe3.fTotal + this.rowFuelRCS.fTotal + this.rowFuelConnect.fTotal + this.rowFuelABL.fTotal;
		float num2 = num;
		this.rowFuelTotal.Init(1, DataHandler.GetString("GUI_REFUEL_SUBTOTAL_FUEL", false), num, 0f);
		num = this.rowLifeBlack.fTotal + this.rowLifeExchange.fTotal + this.rowLifeO2.fTotal + this.rowLifeN2.fTotal;
		num2 += num;
		this.rowLifeTotal.Init(1, DataHandler.GetString("GUI_REFUEL_SUBTOTAL_LIFE", false), num, 0f);
		num = this.rowPowerHookup.fTotal + this.rowPower.fTotal;
		num2 += num;
		this.rowPowerTotal.Init(1, DataHandler.GetString("GUI_REFUEL_SUBTOTAL_POWER", false), num, 0f);
		num = this.rowDock.fTotal + this.rowDockTow.fTotal;
		num2 += num;
		this.rowDockTotal.Init(1, DataHandler.GetString("GUI_REFUEL_SUBTOTAL_DOCK", false), num, 0f);
		num = this.rowFines.fTotal;
		num2 += num;
		this.rowFinesTotal.Init(1, DataHandler.GetString("GUI_REFUEL_SUBTOTAL_FINES", false), num, 0f);
		this.txtTotal.text = num2.ToString("#.00");
	}

	private float GetTotal(string strPA, string strUserID)
	{
		List<LedgerLI> unpaidLIs = Ledger.GetUnpaidLIs(strPA, strUserID, null, false, false);
		float num = 0f;
		foreach (LedgerLI ledgerLI in unpaidLIs)
		{
			num += ledgerLI.fAmount;
		}
		float num2 = this.rowFuelH2.fTotal + this.rowFuelHe3.fTotal + this.rowFuelRCS.fTotal + this.rowFuelABL.fTotal;
		num2 += this.rowLifeBlack.fTotal + this.rowLifeO2.fTotal + this.rowLifeN2.fTotal;
		num2 += this.rowPower.fTotal;
		num += num2;
		return num;
	}

	private void OnSubmit()
	{
		if (this.ship == null)
		{
			return;
		}
		Ship ship = this.COSelf.ship;
		string text = ship.strRegID + DataHandler.GetString("GUI_REFUEL_PORT_SUFFIX", false);
		CondOwner objThem = this.COSelf.GetInteractionCurrent().objThem;
		float num = this.GetTotal(text, objThem.strID);
		if ((double)num > objThem.GetCondAmount("StatUSD"))
		{
			this.txtMessage.text = DataHandler.GetString("GUI_TRADE_ERROR_NO_FUNDS", false);
			AudioManager.am.PlayAudioEmitter("ShipUIBtnRefuelAcceptNeg", false, false);
			this.SetupFields(this.ship);
			return;
		}
		Ledger.AddLI(new LedgerLI(text, objThem.strID, this.rowFuelH2.fTotal, DataHandler.GetString("GUI_REFUEL_SERVICE_D2O", false), "$", StarSystem.fEpoch, false, LedgerLI.Frequency.OneTime));
		Ledger.AddLI(new LedgerLI(text, objThem.strID, this.rowFuelHe3.fTotal, DataHandler.GetString("GUI_REFUEL_SERVICE_HE3", false), "$", StarSystem.fEpoch, false, LedgerLI.Frequency.OneTime));
		Ledger.AddLI(new LedgerLI(text, objThem.strID, this.rowFuelRCS.fTotal, DataHandler.GetString("GUI_REFUEL_SERVICE_RCS", false), "$", StarSystem.fEpoch, false, LedgerLI.Frequency.OneTime));
		Ledger.AddLI(new LedgerLI(text, objThem.strID, this.rowFuelABL.fTotal, DataHandler.GetString("GUI_REFUEL_SERVICE_ABL", false), "$", StarSystem.fEpoch, false, LedgerLI.Frequency.OneTime));
		Ledger.AddLI(new LedgerLI(text, objThem.strID, this.rowLifeBlack.fTotal, DataHandler.GetString("GUI_REFUEL_SERVICE_BLACKWATER", false), "$", StarSystem.fEpoch, false, LedgerLI.Frequency.OneTime));
		Ledger.AddLI(new LedgerLI(text, objThem.strID, this.rowLifeO2.fTotal, DataHandler.GetString("GUI_REFUEL_SERVICE_O2", false), "$", StarSystem.fEpoch, false, LedgerLI.Frequency.OneTime));
		Ledger.AddLI(new LedgerLI(text, objThem.strID, this.rowLifeN2.fTotal, DataHandler.GetString("GUI_REFUEL_SERVICE_N2", false), "$", StarSystem.fEpoch, false, LedgerLI.Frequency.OneTime));
		Ledger.AddLI(new LedgerLI(text, objThem.strID, this.rowPower.fTotal, DataHandler.GetString("GUI_REFUEL_SERVICE_POWER", false), "$", StarSystem.fEpoch, false, LedgerLI.Frequency.OneTime));
		List<LedgerLI> unpaidLIs = Ledger.GetUnpaidLIs(text, objThem.strID, null, false, false);
		foreach (LedgerLI ledgerLI in unpaidLIs)
		{
			if ((double)ledgerLI.fAmount <= objThem.GetCondAmount("StatUSD"))
			{
				objThem.AddCondAmount("StatUSD", (double)(-(double)ledgerLI.fAmount), 0.0, 0f);
				Ledger.PayLI(ledgerLI);
			}
		}
		if (objThem.HasCond("TutorialDockingFeesStart"))
		{
			objThem.AddCondAmount("TutorialDockingFeesComplete", 1.0, 0.0, 0f);
		}
		AudioManager.am.PlayAudioEmitter("ShipUIBtnRefuelAcceptPos", false, false);
		CondTrigger objCondTrig = new CondTrigger();
		float num2 = 0f;
		num2 = this.rowFuelHe3.fPurchased;
		objCondTrig = DataHandler.GetCondTrigger("TIsCanisterLHe02Installed");
		List<CondOwner> list = this.ship.GetICOs1(objCondTrig, true, false, true);
		foreach (CondOwner condOwner in list)
		{
			float num3 = Convert.ToSingle(condOwner.GetCondAmount("StatVolume")) * 129.11f;
			float num4 = Convert.ToSingle(condOwner.GetCondAmount("StatSolidHe3"));
			num = Mathf.Min(num3 - num4, num2);
			condOwner.AddCondAmount("StatSolidHe3", (double)num, 0.0, 0f);
			num2 -= num;
			if (num2 <= 0f)
			{
				break;
			}
		}
		num2 = this.rowFuelH2.fPurchased;
		objCondTrig = DataHandler.GetCondTrigger("TIsCanisterLH02Installed");
		list = this.ship.GetICOs1(objCondTrig, true, false, true);
		foreach (CondOwner condOwner2 in list)
		{
			float num3 = Convert.ToSingle(condOwner2.GetCondAmount("StatVolume")) * 1107f;
			float num4 = Convert.ToSingle(condOwner2.GetCondAmount("StatLiqD2O"));
			num = Mathf.Min(num3 - num4, num2);
			condOwner2.AddCondAmount("StatLiqD2O", (double)num, 0.0, 0f);
			num2 -= num;
			if (num2 <= 0f)
			{
				break;
			}
		}
		num2 = this.rowFuelRCS.fPurchased;
		list = this.ship.GetRCSCans();
		foreach (CondOwner condOwner3 in list)
		{
			GasContainer gasContainer = condOwner3.GasContainer;
			if (!(gasContainer == null))
			{
				float num3 = Convert.ToSingle(condOwner3.GetCondAmount("StatVolume") * condOwner3.GetCondAmount("StatGasPressureMax") / 0.008314000442624092 / condOwner3.GetCondAmount("StatGasTemp") * 0.028013398870825768);
				float num4 = (float)(condOwner3.GetCondAmount("StatGasPressure") / condOwner3.GetCondAmount("StatGasPressureMax") * (double)num3);
				num = Mathf.Min(num3 - num4, num2);
				gasContainer.AddGasMols("N2", (double)(num / 0.028013399f), true);
				num2 -= num;
				if (num2 <= 0f)
				{
					break;
				}
			}
		}
		num2 = this.rowFuelABL.fPurchased;
		objCondTrig = DataHandler.GetCondTrigger("TIsFusionReactorCore01Installed");
		list = this.ship.GetICOs1(objCondTrig, true, false, true);
		Condition cond = DataHandler.GetCond("StatICABLWall");
		float num5 = (cond == null) ? 0f : cond.fClampMax;
		foreach (CondOwner condOwner4 in list)
		{
			float num3 = num5;
			float num4 = num5 - Convert.ToSingle(condOwner4.GetCondAmount("StatICABLWall"));
			num = Mathf.Min(num3 - num4, num2);
			condOwner4.AddCondAmount("StatICABLWall", (double)(-(double)num), 0.0, 0f);
			num2 -= num;
			if (num2 <= 0f)
			{
				break;
			}
		}
		if (this.rowFuelRCS.fPurchased > 0f && objThem.HasCond("TutorialRefuelStart"))
		{
			objThem.AddCondAmount("TutorialRefuelComplete", 1.0, 0.0, 0f);
		}
		num2 = this.rowLifeO2.fPurchased;
		objCondTrig = DataHandler.GetCondTrigger("TIsRTAO2Installed");
		list = this.ship.GetICOs1(objCondTrig, true, false, true);
		bool flag = num2 < 0f;
		foreach (CondOwner condOwner5 in list)
		{
			GasContainer gasContainer2 = condOwner5.GasContainer;
			if (!(gasContainer2 == null))
			{
				float num3 = Convert.ToSingle(condOwner5.GetCondAmount("StatVolume") * condOwner5.GetCondAmount("StatGasPressureMax") / 0.008314000442624092 / condOwner5.GetCondAmount("StatGasTemp") * 0.03199880197644234);
				float num4 = (float)(condOwner5.GetCondAmount("StatGasPressure") / condOwner5.GetCondAmount("StatGasPressureMax") * (double)num3);
				if (flag)
				{
					num = Mathf.Max(-num4, num2);
					gasContainer2.AddGasMols("O2", (double)(num / 0.031998802f), true);
					num2 -= num;
					if (num2 >= 0f)
					{
						break;
					}
				}
				else
				{
					num = Mathf.Min(num3 - num4, num2);
					gasContainer2.AddGasMols("O2", (double)(num / 0.031998802f), true);
					num2 -= num;
					if (num2 <= 0f)
					{
						break;
					}
				}
			}
		}
		num2 = this.rowLifeN2.fPurchased;
		flag = (num2 < 0f);
		foreach (CondOwner condOwner6 in this.aAirpumpN2Cans)
		{
			GasContainer gasContainer3 = condOwner6.GasContainer;
			if (!(gasContainer3 == null))
			{
				float num3 = Convert.ToSingle(condOwner6.GetCondAmount("StatVolume") * condOwner6.GetCondAmount("StatGasPressureMax") / 0.008314000442624092 / condOwner6.GetCondAmount("StatGasTemp") * 0.028013398870825768);
				float num4 = (float)(condOwner6.GetCondAmount("StatGasPressure") / condOwner6.GetCondAmount("StatGasPressureMax") * (double)num3);
				if (flag)
				{
					num = Mathf.Max(-num4, num2);
					gasContainer3.AddGasMols("N2", (double)(num / 0.028013399f), true);
					num2 -= num;
					if (num2 >= 0f)
					{
						break;
					}
				}
				else
				{
					num = Mathf.Min(num3 - num4, num2);
					gasContainer3.AddGasMols("N2", (double)(num / 0.028013399f), true);
					num2 -= num;
					if (num2 <= 0f)
					{
						break;
					}
				}
			}
		}
		num2 = this.rowPower.fPurchased;
		objCondTrig = DataHandler.GetCondTrigger("TIsShipBatteryInstalled");
		list = this.ship.GetICOs1(objCondTrig, true, false, false);
		foreach (CondOwner condOwner7 in list)
		{
			Powered component = condOwner7.GetComponent<Powered>();
			if (!(component == null))
			{
				float num3 = (float)component.PowerStoredMax;
				float num4 = Convert.ToSingle(condOwner7.GetCondAmount("StatPower"));
				num = Mathf.Min(num3 - num4, num2);
				condOwner7.AddCondAmount("StatPower", (double)num, 0.0, 0f);
				num2 -= num;
				if (num2 <= 0f)
				{
					break;
				}
			}
		}
		this.SetupFields(this.ship);
		GUIStationRefuel.OnGUIRefuelSuccess.Invoke();
	}

	public override void Init(CondOwner coSelf, Dictionary<string, string> dict, string strCOKey)
	{
		base.Init(coSelf, dict, strCOKey);
		GUIStationRefuel.SetPrices();
		List<Ship> allDockedShips = coSelf.ship.GetAllDockedShips();
		this.SetupFields(allDockedShips.FirstOrDefault<Ship>());
	}

	public static OnGUIRefuelSuccess OnGUIRefuelSuccess = new OnGUIRefuelSuccess();

	public const float DENSITY_HE3 = 129.11f;

	public const float DENSITY_D2O = 1107f;

	private const float DENSITY_H2O = 1000f;

	private const float MOLMASS_O2 = 0.031998802f;

	private const float MOLMASS_N2 = 0.028013399f;

	private TMP_Text txtHeaderPrice;

	private TMP_Text txtHeaderTotal;

	private TMP_Text txtFooterTotal;

	private TMP_Text txtStationName;

	private TMP_Text txtNameSub;

	private TMP_Text txtVessel;

	private TMP_Text txtMessage;

	private TMP_Text txtTotal;

	private Button btnSubmit;

	private Transform tfList;

	private GUIStationRow rowFuelHe3;

	private GUIStationRow rowFuelH2;

	private GUIStationRow rowFuelRCS;

	private GUIStationRow rowFuelABL;

	private GUIStationRow rowFuelConnect;

	private GUIStationRow rowFuelTotal;

	private GUIStationRow rowLifeO2;

	private GUIStationRow rowLifeN2;

	private GUIStationRow rowLifeBlack;

	private GUIStationRow rowLifeExchange;

	private GUIStationRow rowLifeTotal;

	private GUIStationRow rowPower;

	private GUIStationRow rowPowerHookup;

	private GUIStationRow rowPowerMaint;

	private GUIStationRow rowPowerTotal;

	private GUIStationRow rowDock;

	private GUIStationRow rowDockTow;

	private GUIStationRow rowDockTotal;

	private GUIStationRow rowFines;

	private GUIStationRow rowFinesTotal;

	private Ship ship;

	private List<CondOwner> aAirpumpN2Cans;

	private float fMessageTimer = 2f;

	public static Dictionary<string, float> dictPrices;

	private static string strMessageFound = null;

	public static bool bPlayAEUp = false;

	public static bool bPlayAEDown = false;
}
