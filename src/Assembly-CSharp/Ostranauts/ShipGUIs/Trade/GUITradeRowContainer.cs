using System;
using System.Collections;
using System.Collections.Generic;
using Ostranauts.ShipGUIs.Trade.Models;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Ostranauts.ShipGUIs.Trade
{
	public class GUITradeRowContainer : GUITradeRowBase
	{
		public override float Subtotal
		{
			get
			{
				return this.Price * (float)this._amountToTrade + this._childrenSubTotal;
			}
		}

		public override float TransactionCost
		{
			get
			{
				return this.Price * (float)this._amountToTrade;
			}
		}

		private void Awake()
		{
			this.IsExpanded = false;
			this.tfContainer = this.tfSubItemContainer;
		}

		private void Start()
		{
			this.tfSubItemContainer.gameObject.SetActive(false);
			this.btnToggleExpand.onClick.AddListener(new UnityAction(this.OnToggle));
			this.btnMore.onClick.AddListener(delegate()
			{
				this.OnAmount(1);
			});
			this.btnLess.onClick.AddListener(delegate()
			{
				this.OnAmount(-1);
			});
			this.UpdateButtonvisibility(this._amountToTrade);
		}

		private void OnSelectAll(bool overwriteOn = false)
		{
			if (!this.IsExpanded)
			{
				this.OnToggle();
			}
			this.IsSelectAll = (overwriteOn || !this.IsSelectAll);
			if (this.IsSelectAll)
			{
				float num = 0f;
				foreach (GUITradeRowBase guitradeRowBase in this._dictTradeRowChildren.Values)
				{
					num += guitradeRowBase.SetToMax();
				}
				this._childrenSubTotal = num;
			}
			else
			{
				this.Reset();
			}
		}

		private void OnAmount(int change)
		{
			if (change > 0)
			{
				base.SetValue(this._amountToTrade = 1);
				this.OnSelectAll(true);
			}
			else
			{
				this.SetToMin();
			}
			this.UpdateButtonvisibility(this._amountToTrade);
			this.txtAmount.text = this._amountToTrade.ToString();
		}

		public void Expand()
		{
			this.IsExpanded = false;
			this.OnToggle();
		}

		public void Hide()
		{
			this.IsExpanded = true;
			this.OnToggle();
		}

		protected virtual void OnToggle()
		{
			this.IsExpanded = !this.IsExpanded;
			int num = (!this.IsExpanded) ? 90 : 0;
			this.tfbmpdropdown.rotation = Quaternion.Euler(0f, 0f, (float)num);
			int num2 = (!this.IsExpanded) ? 1 : (this.tfSubItemContainer.childCount + 1);
			RectTransform component = base.GetComponent<RectTransform>();
			Vector2 sizeDelta = new Vector2(component.sizeDelta.x, (float)(num2 * this.ROWHEIGHT));
			component.sizeDelta = sizeDelta;
			this.tfSubItemContainer.gameObject.SetActive(this.IsExpanded);
			if (this.IsExpanded)
			{
				foreach (GUITradeRowBase guitradeRowBase in this._dictTradeRowChildren.Values)
				{
					if (guitradeRowBase is GUITradeRowContainer && ((GUITradeRowContainer)guitradeRowBase).IsExpanded)
					{
						((GUITradeRowContainer)guitradeRowBase).OnToggle();
					}
				}
			}
			GUITradeRowContainer[] componentsInParent = base.transform.parent.GetComponentsInParent<GUITradeRowContainer>();
			if (componentsInParent != null)
			{
				foreach (GUITradeRowContainer guitradeRowContainer in componentsInParent)
				{
					if (guitradeRowContainer != this)
					{
						guitradeRowContainer.UpdateSize();
					}
				}
			}
			this.UpdateLayoutGroup(base.GetComponentInParent<VerticalLayoutGroup>());
		}

		private void UpdateSize()
		{
			int num = 1;
			IEnumerator enumerator = this.tfSubItemContainer.GetEnumerator();
			try
			{
				while (enumerator.MoveNext())
				{
					object obj = enumerator.Current;
					Transform transform = (Transform)obj;
					GUITradeRowContainer component = transform.GetComponent<GUITradeRowContainer>();
					if (component != null && component.IsExpanded)
					{
						RectTransform rectTransform = (RectTransform)component.tfSubItemContainer;
						int num2 = (!(rectTransform != null)) ? 0 : ((int)rectTransform.rect.size.y / this.ROWHEIGHT);
						num2 = ((num2 <= component.tfSubItemContainer.childCount) ? component.tfSubItemContainer.childCount : num2);
						num += num2;
					}
					num++;
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
			RectTransform component2 = base.GetComponent<RectTransform>();
			Vector2 sizeDelta = new Vector2(component2.sizeDelta.x, (float)(num * this.ROWHEIGHT));
			component2.sizeDelta = sizeDelta;
			this.UpdateLayoutGroup(base.GetComponentInParent<VerticalLayoutGroup>());
		}

		private void UpdateLayoutGroup(VerticalLayoutGroup layoutGroupComponent)
		{
			LayoutRebuilder.MarkLayoutForRebuild(layoutGroupComponent.transform as RectTransform);
		}

		protected virtual void SetHeader(TradeRowData trd, JsonZone jZone)
		{
			this.txtSubTotal.text = 0.ToString("n");
			if (trd != null && trd.CoItem != null)
			{
				this.Price = base.GetPrice(trd.CoItem, trd.CoThem, trd.CtVendor);
				this.txtPrice.text = this.Price.ToString("n");
				this.bmpImage.sprite = base.GetImage(trd.CoItem);
				this.txtCoName.text = base.GetItemName(trd.CoItem);
				base.gameObject.name = this.txtCoName.text;
				if (!trd.CtVendor.Triggered(trd.CoItem, null, true))
				{
					this.btnSelectAll.onClick.AddListener(new UnityAction(this.OnToggle));
					this.DisableElements();
					this.txtPrice.text = "Not Wanted";
					this.pnlNotWanted.SetActive(true);
					this._wantedByTrader = false;
				}
				else
				{
					this.btnSelectAll.onClick.AddListener(delegate()
					{
						this.OnSelectAll(false);
					});
					this._coIDs.Add(trd.CoItem.strID);
				}
				return;
			}
			if (jZone != null)
			{
				this.bmpImage.color = jZone.zoneColor;
				this.txtCoName.text = jZone.strName;
				this.txtPrice.gameObject.SetActive(false);
				this.DisableElements();
				base.gameObject.name = jZone.strName;
			}
		}

		private void DisableElements()
		{
			this.btnMore.gameObject.SetActive(false);
			this.btnLess.gameObject.SetActive(false);
			this.txtAmount.transform.parent.gameObject.SetActive(false);
		}

		public void UpdateSuptotal()
		{
			this._childrenSubTotal = 0f;
			foreach (GUITradeRowBase guitradeRowBase in this._dictTradeRowChildren.Values)
			{
				this._childrenSubTotal += guitradeRowBase.Subtotal;
			}
			if (this._parentContainer != null)
			{
				this._parentContainer.UpdateSuptotal();
			}
			this.txtSubTotal.text = this.Subtotal.ToString("n");
		}

		public override void Reset()
		{
			base.SetValue(this._amountToTrade = 0);
			foreach (GUITradeRowBase guitradeRowBase in this._dictTradeRowChildren.Values)
			{
				guitradeRowBase.Reset();
			}
			this._childrenSubTotal = 0f;
			this.txtSubTotal.text = this.Subtotal.ToString("n");
		}

		public override float SetToMax()
		{
			base.SetValue(1);
			this.txtAmount.text = this._amountToTrade.ToString();
			float num = 0f;
			foreach (GUITradeRowBase guitradeRowBase in this._dictTradeRowChildren.Values)
			{
				num += guitradeRowBase.SetToMax();
			}
			return num;
		}

		public override void SetToMin()
		{
			base.SetValue(this._amountToTrade = 0);
			this.txtAmount.text = this._amountToTrade.ToString();
			if (this._parentContainer != null)
			{
				this._parentContainer.SetToMin();
			}
		}

		public override void SetCOs(TradeRowData trd, GUITrade guiTradeReference)
		{
			this._guiTrade = guiTradeReference;
			this._parentContainer = base.transform.parent.GetComponentInParent<GUITradeRowContainer>();
			if (this._parentContainer)
			{
				this.containerDepth = this._parentContainer.containerDepth + 1;
			}
			this._co = trd.CoItem;
			this.SetHeader(trd, trd.JsonZone);
			if (trd.IsEmptyOrPoweredOffDataContainer)
			{
				return;
			}
			foreach (TradeRowData tradeRowData in trd.ContaineredCOs)
			{
				if (!tradeRowData.CoItem.bSlotLocked || tradeRowData.CoItem.slotNow == null)
				{
					if (tradeRowData.ContaineredCOs != null && tradeRowData.ContaineredCOs.Count > 0 && (tradeRowData.CoItem == null || tradeRowData.CoItem.aStack.Count == 0))
					{
						string strID = tradeRowData.CoItem.strID;
						if (!this._dictTradeRowChildren.ContainsKey(strID))
						{
							GUITradeRowBase guitradeRowBase = UnityEngine.Object.Instantiate<GUITradeRowContainer>(this._guiTrade.TradeRowContainerPrefab, this.tfSubItemContainer);
							guitradeRowBase.SetCOs(tradeRowData, this._guiTrade);
							this._dictTradeRowChildren.Add(strID, guitradeRowBase);
						}
						else
						{
							Debug.LogWarning("Skipping duplicate key: " + strID);
						}
					}
					else if (!(tradeRowData.CoItem != null) || this.IsSafeToSell(tradeRowData.CoItem, tradeRowData.CoItem.ship))
					{
						this._guiTrade.AddSingleRow(tradeRowData, ref this._dictTradeRowChildren, this.tfSubItemContainer);
					}
				}
			}
		}

		private bool IsSafeToSell(CondOwner coItem, Ship ship)
		{
			Vector2 tltileCoords = coItem.TLTileCoords;
			Item item = coItem.Item;
			for (int i = 0; i < item.nHeightInTiles; i++)
			{
				for (int j = 0; j < item.nWidthInTiles; j++)
				{
					int num = i * item.nWidthInTiles + j;
					bool activeInHierarchy = TileUtils.goPartTiles.activeInHierarchy;
					TileUtils.goPartTiles.SetActive(true);
					Ray ray = new Ray(new Vector3(tltileCoords.x + (float)j, tltileCoords.y - (float)i, -10f), Vector3.forward);
					RaycastHit[] array = Physics.RaycastAll(ray, 100f, 256);
					TileUtils.goPartTiles.SetActive(activeInHierarchy);
					RaycastHit[] array2 = array;
					int num2 = 0;
					if (num2 < array2.Length)
					{
						RaycastHit raycastHit = array2[num2];
						Tile component = raycastHit.transform.GetComponent<Tile>();
						if (!(component != null) || !(component.coProps != null) || component.coProps.ship != ship || !component.coProps.HasCond("IsFloor"))
						{
							return false;
						}
					}
				}
			}
			return true;
		}

		private readonly int ROWHEIGHT = 36;

		[Header("Buttons & Toggles")]
		[SerializeField]
		private Button btnToggleExpand;

		[SerializeField]
		private Button btnSelectAll;

		[Header("Text")]
		[SerializeField]
		private TMP_Text txtCoName;

		[SerializeField]
		private TMP_Text txtSubTotal;

		[SerializeField]
		private TMP_Text txtAmount;

		[SerializeField]
		private TMP_Text txtPrice;

		[Header("Others")]
		[SerializeField]
		private Transform tfSubItemContainer;

		[SerializeField]
		private RectTransform tfbmpdropdown;

		[SerializeField]
		private Image bmpImage;

		[HideInInspector]
		public Transform tfContainer;

		public bool IsExpanded;

		public bool IsSelectAll;

		private Dictionary<string, GUITradeRowBase> _dictTradeRowChildren = new Dictionary<string, GUITradeRowBase>();

		private float _childrenSubTotal;
	}
}
