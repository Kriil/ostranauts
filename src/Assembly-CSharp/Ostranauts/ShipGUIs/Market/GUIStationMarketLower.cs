using System;
using System.Collections;
using Ostranauts.Trading;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Ostranauts.ShipGUIs.Market
{
	public class GUIStationMarketLower : MonoBehaviour
	{
		private void Awake()
		{
			this.cg.alpha = 0f;
			this.txtTransfer.text = string.Empty;
			this.sliderTransfer.interactable = false;
			this.sliderTransfer.onValueChanged.AddListener(new UnityAction<float>(this.OnSliderChanged));
		}

		private void OnDestroy()
		{
			if (this._errorCoroutine != null)
			{
				base.StopCoroutine(this._errorCoroutine);
			}
		}

		public void Show()
		{
			this.cg.alpha = 1f;
			this.sliderTransfer.interactable = true;
			this.btnConfirmTransaction.interactable = true;
			this._isVisaHolder = GUIBulkTrader.IsVisaHolder(CrewSim.GetSelectedCrew());
			this.txtVisaDiscount.gameObject.SetActive(this._isVisaHolder);
			this.txtVisaDiscount.text = (GUIBulkTrader.VisaPercentBaseModifier * 100.0).ToString("N0") + "% Visa Discount: Active";
		}

		public void Hide()
		{
			this.cg.alpha = 0f;
			this.txtTransfer.text = string.Empty;
			this.sliderTransfer.interactable = false;
			this.SetSlider(0, 10, 5f);
			this.btnConfirmTransaction.interactable = false;
		}

		public int GetSliderValue()
		{
			return (int)this.sliderTransfer.value;
		}

		public void SetSlider(int min, int max, float value)
		{
			this.sliderTransfer.maxValue = (float)max;
			this.sliderTransfer.minValue = (float)min;
			this.sliderTransfer.value = value;
		}

		private void OnSliderChanged(float sldValue)
		{
			double num = 1.0;
			if (sldValue < 0f)
			{
				this.txtbtnConfirm.text = "Buy";
				this.txtBuy.color = new Color(1f, 0.8901961f, 0f, 1f);
				this.txtSell.color = Color.white;
				this.txtSell.fontStyle = FontStyles.Normal;
				this.txtBuy.fontStyle = FontStyles.Bold;
				if (this._isVisaHolder)
				{
					num = 1.0 - GUIBulkTrader.VisaPercentBaseModifier;
				}
			}
			else if (sldValue > 0f)
			{
				this.txtbtnConfirm.text = "Sell";
				this.txtSell.color = new Color(1f, 0.8901961f, 0f, 1f);
				this.txtBuy.color = Color.white;
				this.txtBuy.fontStyle = FontStyles.Normal;
				this.txtSell.fontStyle = FontStyles.Bold;
				if (this._isVisaHolder)
				{
					num = 1.0 + GUIBulkTrader.VisaPercentBaseModifier;
				}
			}
			else
			{
				this.txtbtnConfirm.text = "Buy";
				this.txtBuy.color = Color.white;
				this.txtSell.color = Color.white;
				this.txtBuy.fontStyle = FontStyles.Normal;
				this.txtBuy.fontStyle = FontStyles.Normal;
				if (this._isVisaHolder)
				{
					num = 1.0 - GUIBulkTrader.VisaPercentBaseModifier;
				}
			}
			GUIShipMarketDTO marketDTOForCategory = this._guiShipMarket.GetMarketDTOForCategory();
			double transactionPrice = this._guiShipMarket.GetTransactionPrice(this.sliderTransfer.value, marketDTOForCategory);
			if (marketDTOForCategory == null || marketDTOForCategory.AvgPrice <= 0.0 || transactionPrice == 0.0)
			{
				this.txtTransfer.text = " -";
			}
			else
			{
				float num2 = Mathf.Abs(this.sliderTransfer.value);
				this.txtTransfer.text = string.Concat(new object[]
				{
					num2,
					" units for $",
					(transactionPrice * (double)num2 * num).ToString("F2"),
					" total Mass: ",
					((double)num2 * marketDTOForCategory.AvgMass).ToString("F2"),
					"kg"
				});
			}
		}

		public void ShowPaymentWarning()
		{
			string text = this.txtTransfer.text;
			this.txtTransfer.text = "<color=red>Insufficient funds!</color>";
			if (this._errorCoroutine != null)
			{
				base.StopCoroutine(this._errorCoroutine);
			}
			this._errorCoroutine = base.StartCoroutine(this.ResetTransactionText(text));
		}

		private IEnumerator ResetTransactionText(string previousText)
		{
			yield return new WaitForSeconds(2f);
			this.OnSliderChanged(this.sliderTransfer.value);
			this._errorCoroutine = null;
			yield break;
		}

		[SerializeField]
		private CanvasGroup cg;

		[SerializeField]
		private Slider sliderTransfer;

		[SerializeField]
		private TMP_Text txtTransfer;

		[SerializeField]
		public Button btnConfirmTransaction;

		[SerializeField]
		private TMP_Text txtbtnConfirm;

		[SerializeField]
		private GUIShipMarket _guiShipMarket;

		[SerializeField]
		private TMP_Text txtSell;

		[SerializeField]
		private TMP_Text txtBuy;

		[SerializeField]
		private TMP_Text txtVisaDiscount;

		private Coroutine _errorCoroutine;

		private bool _isVisaHolder;
	}
}
