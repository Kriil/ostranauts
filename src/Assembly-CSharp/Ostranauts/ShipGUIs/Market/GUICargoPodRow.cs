using System;
using System.Collections;
using Ostranauts.Core.Models;
using Ostranauts.ShipGUIs.Market.GUICargoPod;
using Ostranauts.Trading;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Ostranauts.ShipGUIs.Market
{
	public class GUICargoPodRow : MonoBehaviour
	{
		private void Awake()
		{
			if (GUICargoPodRow.OnCargopodSelected == null)
			{
				GUICargoPodRow.OnCargopodSelected = new UnityEvent();
			}
			GUICargoPodRow.OnCargopodSelected.AddListener(new UnityAction(this.OnRowSelected));
			this.btnSelect.onClick.AddListener(new UnityAction(this.OnSellClicked));
			GUIBulkTrader.OnMarketTransaction.AddListener(new UnityAction<string>(this.OnUIInteraction));
			this._guiShipMarket = base.GetComponentInParent<GUIShipMarket>();
			this.pnlSelectableBlock.SetActive(false);
			this.imgSelected.SetActive(false);
		}

		private void OnDestroy()
		{
			GUIBulkTrader.OnMarketTransaction.RemoveListener(new UnityAction<string>(this.OnUIInteraction));
			GUICargoPodRow.OnCargopodSelected.RemoveListener(new UnityAction(this.OnRowSelected));
		}

		private void OnRowSelected()
		{
			this.imgSelected.SetActive(false);
		}

		private void OnUIInteraction(string collection)
		{
			this.UpdateUI(this._co, this._marketConfig);
		}

		public void SetData(CondOwner co, MarketActorConfig marketConfig, bool isSelectable)
		{
			this._co = co;
			this._marketConfig = marketConfig;
			this._isSelectable = isSelectable;
			this.UpdateUI(co, marketConfig);
		}

		private void UpdateUI(CondOwner co, MarketActorConfig marketConfig)
		{
			Texture2D texture2D = DataHandler.LoadPNG(co.strPortraitImg + ".png", false, false);
			if (texture2D != null)
			{
				this._imgAspectRatioFitter.aspectRatio = (float)texture2D.width / (float)texture2D.height;
				this._imgCO.texture = texture2D;
			}
			Tuple<string, int> tuple = marketConfig.GetStockSingle();
			if (tuple == null)
			{
				tuple = new Tuple<string, int>("Empty", 0);
			}
			string item = tuple.Item1;
			int item2 = tuple.Item2;
			int num = 0;
			marketConfig.MaxVirtualInventorySize.TryGetValue(item, out num);
			DataCoCollection dataCoCollection = DataHandler.GetDataCoCollection(item);
			if (dataCoCollection != null)
			{
				this.txtPodContentName.text = dataCoCollection.FriendlyName;
				double averageMass = dataCoCollection.GetAverageMass();
				this.txtMass.text = string.Concat(new object[]
				{
					(int)(averageMass * (double)item2),
					"/",
					MarketManager.CARGOPOD_DEFAULTMASSCAPACITY,
					"kg"
				});
				this.txtStock.text = item2.ToString();
				TMP_Text tmp_Text = this.txtStock;
				tmp_Text.text = tmp_Text.text + "/" + num;
			}
			else
			{
				this.txtPodContentName.text = item;
				this.txtMass.text = "0/" + MarketManager.CARGOPOD_DEFAULTMASSCAPACITY + "kg";
				this.txtStock.text = "-/-";
			}
			this.pnlSelectableBlock.SetActive(!this._isSelectable);
			this.SetCargoReqIcons(marketConfig);
		}

		private void OnSellClicked()
		{
			if (this._isSelectable && this._co != null && this._marketConfig != null)
			{
				bool activeSelf = this.imgSelected.activeSelf;
				GUICargoPodRow.OnCargopodSelected.Invoke();
				if (!activeSelf)
				{
					this.imgSelected.SetActive(true);
				}
				this._guiShipMarket.OnPodSelected(this._co, this._marketConfig);
			}
		}

		private void SetCargoReqIcons(MarketActorConfig mConfig)
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

		private static UnityEvent OnCargopodSelected;

		[SerializeField]
		private RawImage _imgCO;

		[SerializeField]
		private AspectRatioFitter _imgAspectRatioFitter;

		[SerializeField]
		private TMP_Text txtPodContentName;

		[SerializeField]
		private TMP_Text txtStock;

		[SerializeField]
		private TMP_Text txtMass;

		[SerializeField]
		private Transform tfSymbolHost;

		[SerializeField]
		private GUICargoPodReqIcon prefabReqIcon;

		[SerializeField]
		private GameObject pnlSelectableBlock;

		[SerializeField]
		private GameObject imgSelected;

		[SerializeField]
		private Button btnSelect;

		private CondOwner _co;

		private MarketActorConfig _marketConfig;

		private GUIShipMarket _guiShipMarket;

		private bool _isSelectable = true;
	}
}
