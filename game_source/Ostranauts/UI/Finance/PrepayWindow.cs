using System;
using Ostranauts.ShipGUIs;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Ostranauts.UI.Finance
{
	public class PrepayWindow : GUIDataWindow
	{
		private void Awake()
		{
			this.btnClose.onClick.AddListener(new UnityAction(this.Close));
			this.btnConfirm.onClick.AddListener(new UnityAction(this.OnPrepayConfirm));
			this.sldrAmount.onValueChanged.AddListener(new UnityAction<float>(this.OnSliderChanged));
			CanvasManager.HideCanvasGroup(this.cg);
		}

		private void OnPrepayConfirm()
		{
			double availableFunds = this.GetAvailableFunds();
			if (availableFunds < (double)this.sldrAmount.value)
			{
				return;
			}
			Ledger.RemoveLI(this._mortgageLI);
			Ledger.RemoveLI(this._selectedLI);
			this._mortgageLI.fAmount -= this.sldrAmount.value;
			this._mortgageLI.fTime = this._selectedLI.fTime;
			if (this._mortgageLI.fAmount > 0f)
			{
				Ledger.AddLI(this._mortgageLI);
			}
			this._coSelf.AddCondAmount(Ledger.CURRENCY, (double)(-(double)this.sldrAmount.value), 0.0, 0f);
			Ledger.UpdateLedger(this._coSelf, this._mortgageLI.strPayee, (double)(-(double)this.sldrAmount.value), "Prepayed " + this._mortgageLI.strDesc);
			this.Close();
		}

		private void OnSliderChanged(float value)
		{
			this.txtPaymentAmount.text = "$" + value.ToString("n");
			float fPrincipal = this._mortgageLI.fAmount - value;
			this.txtNewRate.text = MathUtils.MortgagePaymentPerShift(fPrincipal).ToString("n");
			this.txtAmountLeft.text = fPrincipal.ToString("n");
			this.btnConfirm.interactable = (value > 0f);
		}

		public void ShowOverlay(LedgerLI selectedLI, CondOwner coSelf)
		{
			if (coSelf == null || selectedLI == null)
			{
				CanvasManager.HideCanvasGroup(this.cg);
				return;
			}
			this._mortgageLI = Ledger.GetMortgageForPayment(selectedLI);
			if (this._mortgageLI == null || this._mortgageLI.Repeats != LedgerLI.Frequency.Mortgage)
			{
				return;
			}
			base.RegisterWindow();
			this._coSelf = coSelf;
			this._selectedLI = selectedLI;
			float num = Mathf.Floor((float)this.GetAvailableFunds());
			this.sldrAmount.maxValue = ((num >= this._mortgageLI.fAmount) ? this._mortgageLI.fAmount : num);
			this.sldrAmount.value = num;
			this.txtPayee.text = this._mortgageLI.strPayee;
			this.txtDescription.text = this._mortgageLI.strDesc;
			this.txtCurrentRate.text = "$" + MathUtils.MortgagePaymentPerShift(this._mortgageLI).ToString("n") + " Total amount: " + this._mortgageLI.fAmount.ToString("n");
			CanvasManager.ShowCanvasGroup(this.cg);
		}

		private double GetAvailableFunds()
		{
			if (this._coSelf == null)
			{
				return 0.0;
			}
			return this._coSelf.GetCondAmount(GUIFinance.strCondCurr);
		}

		public override void CloseExternally()
		{
			this.Close();
		}

		private void Close()
		{
			base.UnregisterWindow();
			CanvasManager.HideCanvasGroup(this.cg);
		}

		[SerializeField]
		private CanvasGroup cg;

		[SerializeField]
		private TMP_Text txtPayee;

		[SerializeField]
		private TMP_Text txtDescription;

		[SerializeField]
		private TMP_Text txtCurrentRate;

		[SerializeField]
		private TMP_Text txtNewRate;

		[SerializeField]
		private TMP_Text txtAmountLeft;

		[SerializeField]
		private TMP_Text txtPaymentAmount;

		[SerializeField]
		private Slider sldrAmount;

		[SerializeField]
		private Button btnClose;

		[SerializeField]
		private Button btnConfirm;

		private CondOwner _coSelf;

		private LedgerLI _mortgageLI;

		private LedgerLI _selectedLI;
	}
}
