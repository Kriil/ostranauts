using System;
using Ostranauts.Events.DTOs;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Ostranauts.ShipGUIs.ShipBroker
{
	public class ConfirmBuyShipPopup : ConfirmationPopupBase
	{
		private new void Awake()
		{
			base.Awake();
			this.sldrMortgage.onValueChanged.AddListener(new UnityAction<float>(this.OnMortgageSliderchanged));
		}

		public override void ShowPanel(ShipPurchaseDTO shipDto, double availableFunds)
		{
			base.ShowPanel(shipDto, availableFunds);
			TMP_Text tmp_Text = this.txtdefaultMake;
			string make = shipDto.Make;
			this.txtMake.text = make;
			tmp_Text.text = make;
			this.txtdefaultName.text = shipDto.ShipName;
			this.txtModel.text = shipDto.Model;
			if (shipDto.TransactionType == TransactionTypes.Mortgage)
			{
				this.sldrMortgage.minValue = ((!shipDto.IsSpecialOffer) ? 0.5f : 0f);
				this.sldrMortgage.value = (float)((this._availableFunds - 0.01) / shipDto.ShipValue);
				this.OnMortgageSliderchanged(this.sldrMortgage.value);
				this.mortgageContainer.SetActive(true);
				this.defaultContainer.SetActive(false);
			}
			else
			{
				this.txtPaymemtPShift.text = string.Empty;
				this.txtPrice.text = "$" + shipDto.ShipValue.ToString("n");
				this._shipDto.TransactionPrice = shipDto.ShipValue;
				this.mortgageContainer.SetActive(false);
				this.defaultContainer.SetActive(true);
			}
		}

		private void OnMortgageSliderchanged(float value)
		{
			this._shipDto.TransactionPrice = this._shipDto.ShipValue * (double)value;
			this.txtPrice.text = "$" + this._shipDto.TransactionPrice.ToString("n");
			this.txtPaymentPercent.text = (int)(value * 100f) + "%";
			this.txtPaymemtPShift.text = ((value >= 1f) ? string.Empty : ("$" + MathUtils.MortgagePaymentPerShift((float)(this._shipDto.ShipValue - this._shipDto.TransactionPrice)).ToString("n")));
		}

		protected override void OnBtnPurchase()
		{
			if (this._availableFunds - this._shipDto.TransactionPrice < 0.0)
			{
				base.ShowError(DataHandler.GetString("GUI_TRADE_ERROR_NO_FUNDS", false));
				return;
			}
			base.OnBtnPurchase();
		}

		[Header("Main")]
		[SerializeField]
		private TMP_Text txtModel;

		[SerializeField]
		private TMP_Text txtMake;

		[SerializeField]
		private TMP_Text txtPrice;

		[Header("Default Container")]
		[SerializeField]
		private GameObject defaultContainer;

		[SerializeField]
		private TMP_Text txtdefaultMake;

		[SerializeField]
		private TMP_Text txtdefaultName;

		[Header("Mortgage")]
		[SerializeField]
		private GameObject mortgageContainer;

		[SerializeField]
		private TMP_Text txtPaymemtPShift;

		[SerializeField]
		private TMP_Text txtPaymentPercent;

		[SerializeField]
		private Slider sldrMortgage;
	}
}
