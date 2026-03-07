using System;
using System.Collections.Generic;
using System.Linq;
using Ostranauts.Trading;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Ostranauts.ShipGUIs.Market.GUICargoPod
{
	public class GUICargoPod : GUIData
	{
		private GUIShipMarketDTO CargoPodUsDTO
		{
			get
			{
				return (this._cargoPodUsConfig == null) ? null : this._cargoPodUsConfig.GetCargoPodData();
			}
		}

		private MarketActorConfig SelectedTransferConfig
		{
			get
			{
				if (this._allCargoPodConfigs != null && this._allCargoPodConfigs.Count > 0 && this._transferIndex >= 0 && this._transferIndex < this._allCargoPodConfigs.Count)
				{
					return this._allCargoPodConfigs[this._transferIndex];
				}
				return null;
			}
		}

		private new void Awake()
		{
			base.Awake();
			this.btnTransferLeft.onClick.AddListener(new UnityAction(this.OnLeftTransferArrow));
			this.btnTransferRight.onClick.AddListener(new UnityAction(this.OnRightTransferArrow));
			this.btnTransfer.onClick.AddListener(new UnityAction(this.OnTransferClicked));
			this.transferSlider.minValue = 0f;
			this.transferSlider.maxValue = 100f;
			this.transferSlider.value = 0f;
			this.txtSliderCount.text = "0";
			this.transferSlider.onValueChanged.AddListener(new UnityAction<float>(this.OnSliderValueChanged));
		}

		public override void Init(CondOwner coSelf, Dictionary<string, string> dict, string strCOKey)
		{
			base.Init(coSelf, dict, strCOKey);
			this.SetData(coSelf);
		}

		private void SetData(CondOwner co)
		{
			if (co == null || co.ship == null)
			{
				return;
			}
			this._coSelf = co;
			this.txtCargoPodId.text = co.strID.Substring(co.strID.Length - 5, 4);
			this._allCargoPodConfigs = MarketManager.GetCargoPods(co.ship.strRegID);
			List<string> shipsForOwner = CrewSim.system.GetShipsForOwner(CrewSim.coPlayer.strID);
			foreach (Ship ship in co.ship.GetAllDockedShips())
			{
				if (shipsForOwner.Contains(ship.strRegID))
				{
					List<MarketActorConfig> cargoPods = MarketManager.GetCargoPods(ship.strRegID);
					if (cargoPods != null && cargoPods.Count > 0)
					{
						this._allCargoPodConfigs.AddRange(cargoPods);
					}
				}
			}
			if (this._allCargoPodConfigs.Count == 0)
			{
				return;
			}
			foreach (MarketActorConfig marketActorConfig in this._allCargoPodConfigs)
			{
				if (marketActorConfig != null && !(marketActorConfig.COwnerId != co.strID))
				{
					this._cargoPodUsConfig = marketActorConfig;
					this._allCargoPodConfigs.Remove(marketActorConfig);
					break;
				}
			}
			this.UpdateStatusWindow(this._cargoPodUsConfig);
			this.SetSliderMax();
			if (this.CargoPodUsDTO != null && !this.CargoPodUsDTO.IsEmpty)
			{
				this._guiCargoPodEject.SetData(new Action(this.Eject));
			}
			this._guiTransferPanel.UpdateTransferWindow(this._cargoPodUsConfig, this.SelectedTransferConfig);
			if (this._allCargoPodConfigs.Count <= 1)
			{
				this.btnTransferLeft.interactable = false;
				this.btnTransferRight.interactable = false;
			}
		}

		private void DebugAddTestCargo(MarketActorConfig mConfig)
		{
			KeyValuePair<string, DataCoCollection> keyValuePair = DataHandler.dictDataCoCollections.First<KeyValuePair<string, DataCoCollection>>();
			List<MarketItem> list = new List<MarketItem>();
			MarketItem item = new MarketItem(keyValuePair.Value);
			for (int i = 0; i < 20; i++)
			{
				list.Add(item);
			}
			mConfig.AddStockAndInventory(list);
		}

		private void OnSliderValueChanged(float value)
		{
			this.txtSliderCount.text = ((int)value).ToString();
		}

		private void SetSliderMax()
		{
			this.transferSlider.maxValue = 100f;
			if (this.CargoPodUsDTO == null)
			{
				return;
			}
			if (!this.CargoPodUsDTO.IsEmpty)
			{
				MarketActorConfig selectedTransferConfig = this.SelectedTransferConfig;
				GUIShipMarketDTO guishipMarketDTO = (selectedTransferConfig == null) ? null : selectedTransferConfig.GetCargoPodData();
				if (this.CargoPodUsDTO.DataCoCollection != null && guishipMarketDTO != null && guishipMarketDTO.DataCoCollection != null && guishipMarketDTO.DataCoCollection.Name == this.CargoPodUsDTO.DataCoCollection.Name)
				{
					int num = guishipMarketDTO.MaxInventory - guishipMarketDTO.Stock;
					this.transferSlider.maxValue = (float)((num <= this.CargoPodUsDTO.Stock) ? num : this.CargoPodUsDTO.Stock);
				}
				else
				{
					this.transferSlider.maxValue = (float)this.CargoPodUsDTO.Stock;
				}
			}
			this.transferSlider.value = 0f;
		}

		private void Eject()
		{
			MarketManager.ResetCargoPod(this._coSelf.ship.strRegID, this._cargoPodUsConfig);
			this._coSelf.ship.ResetMass();
			this.UpdateStatusWindow(this._cargoPodUsConfig);
		}

		private void OnLeftTransferArrow()
		{
			if (this._allCargoPodConfigs == null || this._allCargoPodConfigs.Count == 0)
			{
				return;
			}
			this._transferIndex--;
			if (this._transferIndex < 0)
			{
				this._transferIndex = this._allCargoPodConfigs.Count - 1;
			}
			this.SetSliderMax();
			this._guiTransferPanel.UpdateTransferWindow(this._cargoPodUsConfig, this.SelectedTransferConfig);
		}

		private void OnRightTransferArrow()
		{
			if (this._allCargoPodConfigs == null || this._allCargoPodConfigs.Count == 0)
			{
				return;
			}
			this._transferIndex++;
			if (this._transferIndex >= this._allCargoPodConfigs.Count)
			{
				this._transferIndex = 0;
			}
			this.SetSliderMax();
			this._guiTransferPanel.UpdateTransferWindow(this._cargoPodUsConfig, this.SelectedTransferConfig);
		}

		private void OnTransferClicked()
		{
			MarketActorConfig selectedTransferConfig = this.SelectedTransferConfig;
			if (selectedTransferConfig == null || this._cargoPodUsConfig == null || this.transferSlider.value == 0f || this._cargoPodUsConfig.IsEmpty)
			{
				return;
			}
			MarketActorConfig selectedTransferConfig2 = this.SelectedTransferConfig;
			if (!selectedTransferConfig2.CanTakeCargoFrom(this.CargoPodUsDTO))
			{
				this._guiTransferPanel.HighlightNotCompatible();
				return;
			}
			if (selectedTransferConfig2.IsEmpty && selectedTransferConfig2.IsCargoPod)
			{
				selectedTransferConfig2.SetMaxInventoryForCategory(this.CargoPodUsDTO.DataCoCollection, 0);
			}
			List<MarketItem> list = this._cargoPodUsConfig.TakeOutStock(this.CargoPodUsDTO.DataCoCollection.Name, (int)this.transferSlider.value);
			foreach (MarketItem marketItem in list)
			{
				int num = selectedTransferConfig2.TryAddStock(marketItem, 1);
				if (num > 0)
				{
					this._cargoPodUsConfig.TryAddStock(marketItem, 1);
					Debug.LogWarning("#Market# Transferred cargo did not fit into target cargopod");
				}
			}
			if (this._cargoPodUsConfig.GetFlatStockList().Count == 0)
			{
				MarketManager.ResetCargoPod(this._coSelf.ship.strRegID, this._cargoPodUsConfig);
			}
			this.UpdateStatusWindow(this._cargoPodUsConfig);
			this._guiTransferPanel.UpdateTransferWindow(this._cargoPodUsConfig, this.SelectedTransferConfig);
		}

		private void UpdateStatusWindow(MarketActorConfig podConfig)
		{
			this._mainStatusPanel.SetData(podConfig);
		}

		[SerializeField]
		private Button btnTransferLeft;

		[SerializeField]
		private Button btnTransferRight;

		[SerializeField]
		private Slider transferSlider;

		[SerializeField]
		private Button btnTransfer;

		[SerializeField]
		private GUICargoPodEject _guiCargoPodEject;

		[SerializeField]
		private GUICargoPodStatusPanel _mainStatusPanel;

		[SerializeField]
		private GUITransferPanel _guiTransferPanel;

		[SerializeField]
		private TMP_Text txtSliderCount;

		[SerializeField]
		private TMP_Text txtCargoPodId;

		private CondOwner _coSelf;

		private MarketActorConfig _cargoPodUsConfig;

		private List<MarketActorConfig> _allCargoPodConfigs;

		private int _transferIndex;
	}
}
