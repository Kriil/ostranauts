using System;
using Ostranauts.ShipGUIs.Trade.Models;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Ostranauts.ShipGUIs.Trade
{
	public class GUITradeRowSingle : GUITradeRowBase
	{
		public override float Price { get; set; }

		public override float Subtotal
		{
			get
			{
				return this.Price * (float)this._amountToTrade;
			}
		}

		public override float TransactionCost
		{
			get
			{
				return this.Subtotal;
			}
		}

		private void Awake()
		{
		}

		private void Start()
		{
			this.btnLess.onClick.AddListener(delegate()
			{
				this.OnAmount(-1);
			});
			this.btnLessMin.onClick.AddListener(new UnityAction(this.SetToMin));
			this.btnMore.onClick.AddListener(delegate()
			{
				this.OnAmount(1);
			});
			this.btnMoreMax.onClick.AddListener(delegate()
			{
				this.SetToMax();
			});
			this.tboxAmount.onValueChanged.AddListener(new UnityAction<string>(this.OnTBoxAmount));
			this.tboxAmount.onSelect.AddListener(delegate(string _)
			{
				CrewSim.Typing = true;
			});
			this.tboxAmount.onDeselect.AddListener(delegate(string _)
			{
				CrewSim.Typing = false;
			});
			this.UpdateButtonvisibility(this._amountToTrade);
		}

		private void OnAmount(int nChange)
		{
			base.SetValue(nChange);
			if (this._parentContainer != null)
			{
				if (nChange <= 0)
				{
					this._parentContainer.SetToMin();
				}
				this._parentContainer.UpdateSuptotal();
			}
			this.UpdateButtonvisibility(this._amountToTrade);
			this.tboxAmount.text = this._amountToTrade.ToString();
		}

		private void UpdateUI()
		{
			int num = (this._coIDs == null) ? 0 : this._coIDs.Count;
			this.txtStock.text = num.ToString();
			this.txtPriceSubtotal.text = this.Subtotal.ToString("n");
			if (num > 0)
			{
				CanvasManager.HideCanvasGroup(this.cgPnlWanted);
			}
			else
			{
				CanvasManager.ShowCanvasGroup(this.cgPnlWanted);
				this.goInteractionContainer.SetActive(false);
			}
		}

		private void OnTBoxAmount(string strAmount)
		{
			int nChange = 0;
			if (int.TryParse(strAmount, out nChange))
			{
				this._amountToTrade = 0;
				this.OnAmount(nChange);
			}
			this.tboxAmount.MoveToEndOfLine(true, true);
			AudioManager.am.PlayAudioEmitter("ShipUIBtnSuppliesValueUp", false, false);
			this.UpdateUI();
		}

		protected override float GetCOPrice(CondOwner co, CondTrigger ctVendor)
		{
			return (float)co.GetTotalPrice(ctVendor, false, false);
		}

		protected override void UpdateButtonvisibility(int currentAmount)
		{
			base.UpdateButtonvisibility(currentAmount);
			this.btnLessMin.gameObject.SetActive(currentAmount > 0);
			this.btnMoreMax.gameObject.SetActive(currentAmount < this._coIDs.Count);
		}

		public override void Reset()
		{
			this.SetToMin();
		}

		public override float SetToMax()
		{
			if (this._wantedByTrader)
			{
				this.OnAmount(this._coIDs.Count);
			}
			return this.Subtotal;
		}

		public override void SetToMin()
		{
			if (this._amountToTrade != 0)
			{
				this.OnAmount(this._amountToTrade = 0);
			}
		}

		public override void SetCOs(TradeRowData trd, GUITrade guiTradeReference)
		{
			this._guiTrade = guiTradeReference;
			this._parentContainer = base.GetComponentInParent<GUITradeRowContainer>();
			this._co = trd.CoItem;
			this._coIDs.Add(trd.CoItem.strID);
			if (this._parentContainer)
			{
				this.containerDepth = this._parentContainer.containerDepth + 1;
			}
			this.bmpImage.sprite = base.GetImage(trd.CoItem);
			this._wantedByTrader = trd.CtVendor.Triggered(trd.CoItem, null, true);
			this.Price = base.GetPrice(this._co, trd.CoThem, trd.CtVendor);
			this.txtPrice.text = this.Price.ToString("n");
			this.txtName.text = base.GetItemName(trd.CoItem);
			this.UpdateUI();
			if (!this._wantedByTrader)
			{
				this.txtPrice.text = "Not Wanted";
				this.txtPriceSubtotal.gameObject.SetActive(false);
				this.goInteractionContainer.SetActive(false);
				this.pnlNotWanted.SetActive(true);
			}
		}

		public void SetUnavailableSingle(string itemName, double price, string imagePath)
		{
			this.Price = (float)price;
			this.txtPrice.text = this.Price.ToString("n");
			this.txtName.text = itemName;
			this.bmpImage.sprite = base.GetImage(imagePath);
			this.UpdateUI();
		}

		public override void IncreaseCount(string id)
		{
			if (this._coIDs == null || this._coIDs.Contains(id))
			{
				return;
			}
			this._coIDs.Add(id);
			this.UpdateUI();
		}

		[Header("Buttons")]
		[SerializeField]
		private Button btnLessMin;

		[SerializeField]
		private Button btnMoreMax;

		[Header("Text & Inputfields")]
		[SerializeField]
		private TMP_Text txtName;

		[SerializeField]
		private TMP_Text txtPrice;

		[SerializeField]
		private TMP_Text txtStock;

		[SerializeField]
		protected TMP_InputField tboxAmount;

		[Header("Others")]
		[SerializeField]
		private Image bmpImage;

		[SerializeField]
		private CanvasGroup cgPnlWanted;

		[SerializeField]
		private GameObject goInteractionContainer;
	}
}
