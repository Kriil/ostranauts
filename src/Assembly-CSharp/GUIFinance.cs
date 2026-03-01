using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Ostranauts.Core;
using Ostranauts.Objectives;
using Ostranauts.UI.Finance;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class GUIFinance : GUIData
{
	protected override void Awake()
	{
		base.Awake();
		this.bPausesGame = true;
		this.tfError = base.transform.Find("txtError");
		this.chkCostHeader = base.transform.Find("pnlPLScroll/Viewport/pnlPLList/pnlCostHeader/chk").GetComponent<Toggle>();
		this.dictExpenses = new Dictionary<Transform, LedgerLI>();
		this.btnFuture = base.transform.Find("btnFuture").GetComponent<Button>();
		this.btnFuture.onClick.AddListener(delegate()
		{
			this.NextShift();
		});
		this.btnPast = base.transform.Find("btnPast").GetComponent<Button>();
		this.btnPast.onClick.AddListener(delegate()
		{
			this.LastShift();
		});
		this.btnSubmit.onClick.AddListener(delegate()
		{
			this.Submit();
		});
		this.btnPrepay.onClick.AddListener(new UnityAction(this.Prepay));
		this.btnPrepay.gameObject.SetActive(false);
		this.btnSkip = base.transform.Find("bmpCaution/btnSkip").GetComponent<Button>();
		this.cgSkip = base.transform.Find("bmpCaution").GetComponent<CanvasGroup>();
		CanvasManager.HideCanvasGroup(this.cgSkip);
		AudioManager.AddBtnAudio(this.chkCostHeader.gameObject, "ShipUIBtnSuppliesClick", "ShipUIBtnSuppliesCancel");
		AudioManager.AddBtnAudio(this.btnFuture.gameObject, "ShipUIBtnFinanceClick", "ShipUIBtnFinanceValueUp");
		AudioManager.AddBtnAudio(this.btnPast.gameObject, "ShipUIBtnFinanceClick", "ShipUIBtnFinanceValueDown");
		AudioManager.AddBtnAudio(this.btnSkip.gameObject, "ShipUIBtnFinanceClick", "ShipUIBtnFinanceCancel");
	}

	public void Refresh()
	{
		if (this.bBusy)
		{
			return;
		}
		this.SetDate(this.dfEpoch);
	}

	private void SetDate(double dfEpochNew)
	{
		bool flag = true;
		int monthFromS = MathUtils.GetMonthFromS(dfEpochNew);
		flag &= (MathUtils.GetMonthFromS(StarSystem.fEpoch) == monthFromS);
		int dayOfMonthFromS = MathUtils.GetDayOfMonthFromS(dfEpochNew);
		if (flag)
		{
			flag &= (MathUtils.GetDayOfMonthFromS(StarSystem.fEpoch) == dayOfMonthFromS);
		}
		string @string = DataHandler.GetString("MONTH" + monthFromS, false);
		int shiftFromS = MathUtils.GetShiftFromS(dfEpochNew);
		if (flag)
		{
			flag &= (MathUtils.GetShiftFromS(StarSystem.fEpoch) == shiftFromS);
		}
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append(DataHandler.GetString("GUI_FINANCE_TITLE", false));
		stringBuilder.Append(@string);
		stringBuilder.Append(" ");
		stringBuilder.Append(dayOfMonthFromS);
		stringBuilder.Append(", ");
		stringBuilder.Append(MathUtils.GetYearFromS(dfEpochNew));
		stringBuilder.AppendLine(", Shift " + shiftFromS + "/4");
		stringBuilder.Append(DataHandler.GetString("GUI_FINANCE_SHIFT_HOURS_" + shiftFromS, false));
		TMP_Text component = base.transform.Find("txtTitle").GetComponent<TMP_Text>();
		component.text = stringBuilder.ToString();
		this.dfEpoch = dfEpochNew;
		Transform transform = base.transform.Find("pnlPLScroll/Viewport/pnlPLList/pnlIncomeList");
		Transform transform2 = base.transform.Find("pnlPLScroll/Viewport/pnlPLList/pnlCostList");
		IEnumerator enumerator = transform.GetEnumerator();
		try
		{
			while (enumerator.MoveNext())
			{
				object obj = enumerator.Current;
				Transform transform3 = (Transform)obj;
				UnityEngine.Object.Destroy(transform3.gameObject);
			}
		}
		finally
		{
			IDisposable disposable;
			if ((disposable = (enumerator as IDisposable)) != null)
			{
				disposable.Dispose();
			}
		}
		IEnumerator enumerator2 = transform2.GetEnumerator();
		try
		{
			while (enumerator2.MoveNext())
			{
				object obj2 = enumerator2.Current;
				Transform transform4 = (Transform)obj2;
				UnityEngine.Object.Destroy(transform4.gameObject);
			}
		}
		finally
		{
			IDisposable disposable2;
			if ((disposable2 = (enumerator2 as IDisposable)) != null)
			{
				disposable2.Dispose();
			}
		}
		this.dictExpenses.Clear();
		List<LedgerLI> shiftLIs = Ledger.GetShiftLIs(this.dfEpoch, flag);
		GameObject original = Resources.Load("GUIShip/GUIFinance/GUIFinanceRow") as GameObject;
		this.fCosts = 0f;
		this.fIncome = 0f;
		this.fIncomeUnpaid = 0f;
		this.fSelectedCosts = 0f;
		foreach (LedgerLI ledgerLI in shiftLIs)
		{
			CondOwner condOwner = null;
			if (ledgerLI.strPayor == this.COSelf.strID)
			{
				Transform transform5 = UnityEngine.Object.Instantiate<GameObject>(original, transform2).transform;
				string text;
				if (DataHandler.mapCOs.TryGetValue(ledgerLI.strPayee, out condOwner))
				{
					text = condOwner.strName;
				}
				else
				{
					text = ledgerLI.strPayee;
				}
				transform5.Find("txtName").GetComponent<TMP_Text>().text = text;
				transform5.Find("txtDesc").GetComponent<TMP_Text>().text = ledgerLI.strDesc;
				Toggle chk = transform5.Find("chk").GetComponent<Toggle>();
				chk.gameObject.SetActive(false);
				if (ledgerLI.Paid && ledgerLI.fTimePaid > 0.0)
				{
					transform5.Find("txtPaid").GetComponent<TMP_Text>().text = "Paid";
				}
				else
				{
					if (flag)
					{
						chk.gameObject.SetActive(true);
						chk.isOn = true;
						chk.onValueChanged.AddListener(delegate(bool A_1)
						{
							this.TogglePayee(chk);
						});
						AudioManager.AddBtnAudio(chk.gameObject, "ShipUIBtnSuppliesClick", "ShipUIBtnSuppliesCancel");
					}
					transform5.Find("txtPaid").GetComponent<TMP_Text>().text = string.Empty;
					this.fSelectedCosts -= ledgerLI.fAmount;
					this.dictExpenses.Add(transform5, ledgerLI);
				}
				transform5.Find("txtDate").GetComponent<TMP_Text>().text = MathUtils.GetUTCFromS(ledgerLI.fTime);
				transform5.Find("txtAmount").GetComponent<TMP_Text>().text = (-ledgerLI.fAmount).ToString("n");
				this.fCosts -= ledgerLI.fAmount;
			}
			else if (ledgerLI.strPayee == this.COSelf.strID)
			{
				Transform transform5 = UnityEngine.Object.Instantiate<GameObject>(original, transform).transform;
				transform5.Find("chk").gameObject.SetActive(false);
				string text;
				if (DataHandler.mapCOs.TryGetValue(ledgerLI.strPayor, out condOwner))
				{
					text = condOwner.strName;
				}
				else
				{
					text = ledgerLI.strPayor;
				}
				transform5.Find("txtName").GetComponent<TMP_Text>().text = text;
				transform5.Find("txtDesc").GetComponent<TMP_Text>().text = ledgerLI.strDesc;
				if (ledgerLI.Paid)
				{
					transform5.Find("txtPaid").GetComponent<TMP_Text>().text = "Paid";
				}
				else
				{
					transform5.Find("txtPaid").GetComponent<TMP_Text>().text = string.Empty;
					this.fIncomeUnpaid += ledgerLI.fAmount;
				}
				transform5.Find("txtDate").GetComponent<TMP_Text>().text = MathUtils.GetUTCFromS(ledgerLI.fTime);
				transform5.Find("txtAmount").GetComponent<TMP_Text>().text = ledgerLI.fAmount.ToString("n");
				this.fIncome += ledgerLI.fAmount;
			}
		}
		this.chkCostHeader.gameObject.SetActive(flag);
		this.chkCostHeader.onValueChanged.RemoveAllListeners();
		this.chkCostHeader.isOn = true;
		this.chkCostHeader.onValueChanged.AddListener(delegate(bool A_1)
		{
			this.ToggleAllPayees(this.chkCostHeader);
		});
		this.UpdatePrepayBtnState(flag);
		component = base.transform.Find("pnlPLScroll/Viewport/pnlPLList/pnlIncomeTotal/txtAmount").GetComponent<TMP_Text>();
		component.text = this.fIncome.ToString("n");
		component = base.transform.Find("pnlPLScroll/Viewport/pnlPLList/pnlCostTotal/txtAmount").GetComponent<TMP_Text>();
		component.text = this.fCosts.ToString("n");
		component = base.transform.Find("pnlProfit/txtTotal").GetComponent<TMP_Text>();
		this.FormatValue(this.fIncome + this.fCosts, component);
		component = base.transform.Find("pnlUnpaid/txtTotal").GetComponent<TMP_Text>();
		this.FormatValue(this.fSelectedCosts, component);
		component = base.transform.Find("pnlReserves/txtTotal").GetComponent<TMP_Text>();
		this.fProjReserves = Convert.ToSingle(this.COSelf.GetCondAmount(GUIFinance.strCondCurr) + (double)this.fSelectedCosts + (double)this.fIncomeUnpaid);
		this.FormatValue(this.fProjReserves, component);
		this.tfError.gameObject.SetActive(false);
		this.btnSubmit.gameObject.SetActive(this.fCosts != 0f && flag);
		if (this.fSelectedCosts < 0f)
		{
			CanvasManager.ShowCanvasGroup(this.btnSubmit.GetComponent<CanvasGroup>());
		}
		else
		{
			CanvasManager.HideCanvasGroup(this.btnSubmit.GetComponent<CanvasGroup>());
		}
		if (GUIFinance.nState == GUIFinance.State.RESOLVE_DEBTS && this.dictExpenses.Count > 0)
		{
			CanvasManager.ShowCanvasGroup(this.cgSkip.GetComponent<CanvasGroup>());
		}
		else
		{
			CanvasManager.HideCanvasGroup(this.cgSkip.GetComponent<CanvasGroup>());
		}
		LayoutRebuilder.ForceRebuildLayoutImmediate(base.transform.GetComponent<RectTransform>());
	}

	private void NextShift()
	{
		int hourFromS = MathUtils.GetHourFromS(this.dfEpoch);
		if (hourFromS >= 18 && hourFromS <= 24)
		{
			double num = this.dfEpoch % 31556926.0;
			num %= 87658.125;
			num = 87658.125 - num;
			this.SetDate(this.dfEpoch + num + 1.0);
		}
		else
		{
			this.SetDate(this.dfEpoch + 21600.0);
		}
	}

	private void LastShift()
	{
		int hourFromS = MathUtils.GetHourFromS(this.dfEpoch);
		if (hourFromS <= 5)
		{
			this.SetDate(this.dfEpoch - 87658.12777777777 + 64800.0);
		}
		else if (hourFromS == 24)
		{
			double num = this.dfEpoch % 31556926.0;
			num %= 87658.125;
			num -= 64800.0;
			this.SetDate(this.dfEpoch - num - 1.0);
		}
		else
		{
			this.SetDate(this.dfEpoch - 21600.0);
		}
	}

	private void NextDay()
	{
		this.SetDate(this.dfEpoch + 87658.12777777777);
	}

	private void LastDay()
	{
		this.SetDate(this.dfEpoch - 87658.12777777777);
	}

	private void NextMonth()
	{
		this.SetDate(this.dfEpoch + 2629743.8333333335);
	}

	private void LastMonth()
	{
		this.SetDate(this.dfEpoch - 2629743.8333333335);
	}

	private void FormatValue(float fAmount, TMP_Text txt)
	{
		txt.text = fAmount.ToString("n");
		if (fAmount >= 0f)
		{
			txt.color = Color.white;
		}
		else
		{
			txt.color = this.clrRed;
		}
	}

	private void TogglePayee(Toggle chk)
	{
		if (chk.isOn)
		{
			this.fSelectedCosts -= this.dictExpenses[chk.transform.parent].fAmount;
		}
		else
		{
			this.chkCostHeader.onValueChanged.RemoveAllListeners();
			this.chkCostHeader.isOn = false;
			this.chkCostHeader.onValueChanged.AddListener(delegate(bool A_1)
			{
				this.ToggleAllPayees(this.chkCostHeader);
			});
			this.fSelectedCosts += this.dictExpenses[chk.transform.parent].fAmount;
		}
		TMP_Text component = base.transform.Find("pnlUnpaid/txtTotal").GetComponent<TMP_Text>();
		this.FormatValue(this.fSelectedCosts, component);
		component = base.transform.Find("pnlReserves/txtTotal").GetComponent<TMP_Text>();
		this.fProjReserves = Convert.ToSingle(this.COSelf.GetCondAmount(GUIFinance.strCondCurr) + (double)this.fSelectedCosts + (double)this.fIncomeUnpaid);
		this.FormatValue(this.fProjReserves, component);
		this.UpdatePrepayBtnState(true);
		if (this.fSelectedCosts < 0f)
		{
			CanvasManager.ShowCanvasGroup(this.btnSubmit.GetComponent<CanvasGroup>());
		}
		else
		{
			CanvasManager.HideCanvasGroup(this.btnSubmit.GetComponent<CanvasGroup>());
		}
	}

	private void UpdatePrepayBtnState(bool currentShift = true)
	{
		if (string.IsNullOrEmpty(this._prepayString))
		{
			this._prepayString = DataHandler.GetString("GUI_FINANCE_BTN_PREPAY", false);
		}
		if (string.IsNullOrEmpty(this._toomanySelectedString))
		{
			this._toomanySelectedString = DataHandler.GetString("GUI_FINANCE_BTN_PREPAY_ERR", false);
		}
		this.btnPrepay.gameObject.SetActive(false);
		this.btnPrepay.interactable = true;
		if (!currentShift || this.dictExpenses == null)
		{
			return;
		}
		foreach (KeyValuePair<Transform, LedgerLI> keyValuePair in this.dictExpenses)
		{
			Toggle componentInChildren = keyValuePair.Key.GetComponentInChildren<Toggle>();
			if (!(componentInChildren == null))
			{
				if (componentInChildren.isOn && Ledger.IsMortgage(keyValuePair.Value))
				{
					if (this.btnPrepay.gameObject.activeSelf)
					{
						this.btnPrepay.interactable = false;
						TMP_Text componentInChildren2 = this.btnPrepay.GetComponentInChildren<TMP_Text>();
						if (componentInChildren2 != null)
						{
							componentInChildren2.text = this._toomanySelectedString;
						}
						break;
					}
					this.btnPrepay.gameObject.SetActive(true);
					TMP_Text componentInChildren3 = this.btnPrepay.GetComponentInChildren<TMP_Text>();
					if (componentInChildren3 != null)
					{
						componentInChildren3.text = this._prepayString;
					}
				}
			}
		}
	}

	private void ToggleAllPayees(Toggle chk)
	{
		foreach (Transform transform in this.dictExpenses.Keys)
		{
			transform.Find("chk").GetComponent<Toggle>().isOn = chk.isOn;
		}
	}

	private void Submit()
	{
		this.bBusy = true;
		bool active = true;
		if (this.fProjReserves < 0f)
		{
			this.tfError.GetComponent<TMP_Text>().text = DataHandler.GetString("GUI_FINANCE_ERROR_NO_FUNDS", false);
			AudioManager.am.PlayAudioEmitter("ShipUIBtnFinanceAcceptNeg", false, false);
		}
		else if (this.fSelectedCosts > this.fProjReserves)
		{
			this.tfError.GetComponent<TMP_Text>().text = DataHandler.GetString("GUI_FINANCE_ERROR_NO_SELECTED", false);
			AudioManager.am.PlayAudioEmitter("ShipUIBtnFinanceAcceptNeg", false, false);
		}
		else
		{
			foreach (KeyValuePair<Transform, LedgerLI> keyValuePair in this.dictExpenses)
			{
				if (keyValuePair.Key.Find("chk").GetComponent<Toggle>().isOn)
				{
					LedgerLI value = keyValuePair.Value;
					if (this.COSelf.GetCondAmount(GUIFinance.strCondCurr) < (double)value.fAmount)
					{
						this.tfError.GetComponent<TMP_Text>().text = DataHandler.GetString("GUI_FINANCE_ERROR_NO_FUNDS", false);
						this.COSelf.LogMessage(DataHandler.GetString("GUI_FINANCE_LOG_NO_FUNDS", false) + value.strPayee + " " + value.fAmount.ToString("n"), "Bad", this.COSelf.strName);
						this.tfError.gameObject.SetActive(active);
						AudioManager.am.PlayAudioEmitter("ShipUIBtnFinanceAcceptNeg", false, false);
						return;
					}
					this.COSelf.AddCondAmount(GUIFinance.strCondCurr, (double)(-(double)value.fAmount), 0.0, 0f);
					Ledger.PayLI(value);
					this.COSelf.LogMessage(DataHandler.GetString("GUI_FINANCE_LOG_PAID", false) + value.strPayee + " " + value.fAmount.ToString("n"), "Good", this.COSelf.strName);
				}
			}
			active = false;
			AudioManager.am.PlayAudioEmitter("ShipUIBtnFinanceAcceptPos", false, false);
		}
		this.bBusy = false;
		this.SetDate(this.dfEpoch);
		this.tfError.gameObject.SetActive(active);
	}

	private void Prepay()
	{
		LedgerLI selectedLI = null;
		foreach (KeyValuePair<Transform, LedgerLI> keyValuePair in this.dictExpenses)
		{
			if (keyValuePair.Key.Find("chk").GetComponent<Toggle>().isOn)
			{
				if (Ledger.IsMortgage(keyValuePair.Value))
				{
					selectedLI = keyValuePair.Value;
					break;
				}
			}
		}
		this.prepayWindow.ShowOverlay(selectedLI, this.COSelf);
	}

	public override void Init(CondOwner coSelf, Dictionary<string, string> dict, string strCOKey)
	{
		base.Init(coSelf, dict, strCOKey);
		this.SetDate(StarSystem.fEpoch);
		CrewSim.coPlayer.AddCondAmount("IsFinanceChecked", 1.0, 0.0, 0f);
		CrewSim.coPlayer.ZeroCondAmount("TutorialNoCashButtonYet");
		MonoSingleton<ObjectiveTracker>.Instance.CheckObjective(CrewSim.coPlayer.strID);
	}

	public const int SECS_PER_SHIFT = 21600;

	[SerializeField]
	private Button btnPrepay;

	[SerializeField]
	private Button btnSubmit;

	[SerializeField]
	private PrepayWindow prepayWindow;

	private double dfEpoch;

	private Dictionary<Transform, LedgerLI> dictExpenses;

	private Color clrRed = new Color(0.8392157f, 0.22352941f, 0f);

	public static string strCondCurr = "StatUSD";

	private static GUIFinance.State nState;

	private Button btnFuture;

	private Button btnPast;

	private Button btnSkip;

	private float fProjReserves;

	private float fCosts;

	private float fIncome;

	private float fIncomeUnpaid;

	private float fSelectedCosts;

	private bool bBusy;

	private Transform tfError;

	private Toggle chkCostHeader;

	private CanvasGroup cgSkip;

	private string _prepayString;

	private string _toomanySelectedString;

	public enum State
	{
		NORMAL,
		RESOLVE_DEBTS
	}
}
