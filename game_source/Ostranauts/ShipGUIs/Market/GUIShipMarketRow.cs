using System;
using System.Collections;
using Ostranauts.ShipGUIs.Market.GUICargoPod;
using Ostranauts.Trading;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Ostranauts.ShipGUIs.Market
{
	public class GUIShipMarketRow : MonoBehaviour
	{
		private void Awake()
		{
			if (GUIShipMarketRow.OnMarketRowSelected == null)
			{
				GUIShipMarketRow.OnMarketRowSelected = new UnityEvent();
			}
			GUIShipMarketRow.OnMarketRowSelected.AddListener(new UnityAction(this.OnRowSelected));
			this.btnBuy.onClick.AddListener(new UnityAction(this.OnCategorySelected));
			this._guiShipMarket = base.GetComponentInParent<GUIShipMarket>();
			this.pnlSelectableBlock.SetActive(false);
			this.imgSelected.SetActive(false);
		}

		public void SetData(GUIShipMarketDTO marketDTO, bool isSelectable)
		{
			this._marketDto = marketDTO;
			this._isSelectable = isSelectable;
			this.pnlSelectableBlock.SetActive(!isSelectable);
			this.UpdateUI(marketDTO);
		}

		private void OnDestroy()
		{
			GUIShipMarketRow.OnMarketRowSelected.RemoveListener(new UnityAction(this.OnRowSelected));
		}

		private void OnRowSelected()
		{
			this.imgSelected.SetActive(false);
		}

		private void UpdateUI(GUIShipMarketDTO marketDTO)
		{
			string friendlyName = marketDTO.DataCoCollection.FriendlyName;
			this.txtCoCollName.text = friendlyName;
			this.txtPrice.text = "$" + marketDTO.TransactionPrice.ToString("F2");
			string text = "<color=#A9A9A9>";
			float num = marketDTO.PriceModifier - 1f;
			if (num != 0f)
			{
				text = ((num <= 0f) ? "<color=green>" : "<color=red>");
			}
			TMP_Text tmp_Text = this.txtPrice;
			string text2 = tmp_Text.text;
			tmp_Text.text = string.Concat(new object[]
			{
				text2,
				" (",
				text,
				(num <= 0f) ? string.Empty : "+",
				(int)(num * 100f),
				"%</color>)"
			});
			this.txtStock.text = marketDTO.Stock.ToString();
			TMP_Text tmp_Text2 = this.txtStock;
			tmp_Text2.text = tmp_Text2.text + "/" + marketDTO.MaxInventory;
			this.txtMass.text = marketDTO.AvgMass.ToString("F2");
			this.SetCargoReqIcons(marketDTO.DataCoCollection);
		}

		private void SetCargoReqIcons(DataCoCollection mConfig)
		{
			IEnumerator enumerator = this.tfSymbolHost.GetEnumerator();
			try
			{
				while (enumerator.MoveNext())
				{
					object obj = enumerator.Current;
					Transform transform = (Transform)obj;
					UnityEngine.Object.Destroy(transform.gameObject);
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
			if (mConfig == null || mConfig.CargoSpecs == null)
			{
				return;
			}
			foreach (JsonCargoSpec jsonCargoSpec in mConfig.CargoSpecs)
			{
				if (!string.IsNullOrEmpty(jsonCargoSpec.strImg))
				{
					GUICargoPodReqIcon guicargoPodReqIcon = UnityEngine.Object.Instantiate<GUICargoPodReqIcon>(this.prefabReqIcon, this.tfSymbolHost);
					guicargoPodReqIcon.SetData(jsonCargoSpec.strImg);
				}
			}
		}

		private void OnCategorySelected()
		{
			if (this._isSelectable && this._marketDto != null)
			{
				bool activeSelf = this.imgSelected.activeSelf;
				GUIShipMarketRow.OnMarketRowSelected.Invoke();
				if (!activeSelf)
				{
					this.imgSelected.SetActive(true);
				}
				this._guiShipMarket.OnMarketCategorySelected(this._marketDto.DataCoCollection);
			}
		}

		private static UnityEvent OnMarketRowSelected;

		[SerializeField]
		private TMP_Text txtCoCollName;

		[SerializeField]
		private TMP_Text txtPrice;

		[SerializeField]
		private TMP_Text txtStock;

		[SerializeField]
		private TMP_Text txtMass;

		[SerializeField]
		private GameObject pnlSelectableBlock;

		[SerializeField]
		private GameObject imgSelected;

		[SerializeField]
		private Transform tfSymbolHost;

		[SerializeField]
		private GUICargoPodReqIcon prefabReqIcon;

		[SerializeField]
		private Button btnBuy;

		private GUIShipMarketDTO _marketDto;

		private GUIShipMarket _guiShipMarket;

		private bool _isSelectable = true;
	}
}
