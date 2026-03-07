using System;
using System.Collections.Generic;
using Ostranauts.ShipGUIs.Trade.Models;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Ostranauts.ShipGUIs.Trade
{
	public abstract class GUITradeRowBase : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IEventSystemHandler
	{
		private bool IsSelling
		{
			get
			{
				return this._guiTrade.chkSell.isOn;
			}
		}

		public virtual float Price { get; set; }

		public abstract float TransactionCost { get; }

		public abstract float Subtotal { get; }

		public void OnPointerEnter(PointerEventData eventData)
		{
			if (this._guiTrade != null)
			{
				if (!this._guiTrade.deepestTradeRowHovered)
				{
					this._guiTrade.CoTooltip = this._co;
					this._guiTrade.deepestTradeRowHovered = this;
				}
				else if (this.containerDepth > this._guiTrade.deepestTradeRowHovered.containerDepth)
				{
					this._guiTrade.CoTooltip = this._co;
					this._guiTrade.deepestTradeRowHovered = this;
				}
			}
		}

		public void OnPointerExit(PointerEventData eventData)
		{
			if (this._guiTrade != null && this._guiTrade.CoTooltip == this._co)
			{
				this._guiTrade.CoTooltip = null;
				this._guiTrade.deepestTradeRowHovered = null;
				if (this._parentContainer && eventData.hovered.Contains(this._parentContainer.gameObject))
				{
					this._guiTrade.CoTooltip = this._parentContainer._co;
					this._guiTrade.deepestTradeRowHovered = this._parentContainer;
				}
			}
		}

		public abstract void SetCOs(TradeRowData trd, GUITrade guiTradeReference);

		public virtual void IncreaseCount(string id)
		{
		}

		public void AddStackedItems(TradeRowData trd)
		{
			if (trd == null || trd.ContaineredCOs == null || trd.CoItem.StackCount <= 1)
			{
				return;
			}
			foreach (TradeRowData tradeRowData in trd.ContaineredCOs)
			{
				this.IncreaseCount(tradeRowData.CoItem.strID);
			}
		}

		public abstract float SetToMax();

		public abstract void SetToMin();

		public abstract void Reset();

		protected void SetValue(int change)
		{
			this._amountToTrade += change;
			if (this._amountToTrade < 0)
			{
				this._amountToTrade = 0;
			}
			if (this._amountToTrade > this._coIDs.Count)
			{
				this._amountToTrade = this._coIDs.Count;
			}
			this.txtPriceSubtotal.text = this.Subtotal.ToString("n");
			this._guiTrade.AddToSale(this, this._amountToTrade);
			this.UpdateButtonvisibility(this._amountToTrade);
		}

		protected virtual void UpdateButtonvisibility(int currentAmount)
		{
			this.btnLess.gameObject.SetActive(currentAmount > 0);
			this.btnMore.gameObject.SetActive(currentAmount < this._coIDs.Count);
		}

		public List<string> GetIDsOfTradedCOs()
		{
			if (this._amountToTrade > this._coIDs.Count)
			{
				return null;
			}
			return this._coIDs.GetRange(0, this._amountToTrade);
		}

		protected float GetPrice(CondOwner coItem, CondOwner coVendor, CondTrigger ctVendor)
		{
			float num = (!this.IsSelling) ? ((float)coVendor.GetCondAmount("DiscountSell")) : ((float)coVendor.GetCondAmount("DiscountBuy"));
			if (num == 0f)
			{
				num = 1f;
			}
			if (ctVendor.Triggered(coItem, null, true))
			{
				return num * this.GetCOPrice(coItem, ctVendor);
			}
			return 0f;
		}

		protected virtual float GetCOPrice(CondOwner co, CondTrigger ctVendor)
		{
			return Convert.ToSingle(co.GetBasePrice(false));
		}

		protected Sprite GetImage(CondOwner coItem)
		{
			Item component = coItem.GetComponent<Item>();
			if (component == null)
			{
				return null;
			}
			return this.GetImage(component.ImgOverride + ".png");
		}

		protected Sprite GetImage(string imagePath)
		{
			Texture2D texture2D = DataHandler.LoadPNG(imagePath, false, false);
			return Sprite.Create(texture2D, new Rect(0f, 0f, (float)texture2D.width, (float)texture2D.height), Vector2.zero);
		}

		protected string GetItemName(CondOwner coItem)
		{
			string text = coItem.FriendlyName + coItem.GetDamageDescriptor();
			if (coItem.slotNow != null)
			{
				text = "* " + text;
			}
			return text;
		}

		[SerializeField]
		protected TMP_Text txtPriceSubtotal;

		[SerializeField]
		protected Button btnLess;

		[SerializeField]
		protected Button btnMore;

		[SerializeField]
		protected GameObject pnlNotWanted;

		protected CondOwner _co;

		protected GUITrade _guiTrade;

		protected GUITradeRowContainer _parentContainer;

		protected int _amountToTrade;

		protected readonly List<string> _coIDs = new List<string>();

		protected bool _wantedByTrader = true;

		[HideInInspector]
		public int containerDepth = 1;
	}
}
