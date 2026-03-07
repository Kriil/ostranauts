using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Ostranauts.Core.Models;
using Ostranauts.Trading;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace Ostranauts.ShipGUIs.Market
{
	public class GUIShipMarket : MonoBehaviour
	{
		private void Awake()
		{
			this.txtNoPods.gameObject.SetActive(false);
			this.txtNoStationMarket.gameObject.SetActive(false);
			GUIBulkTrader.OnMarketTransaction.AddListener(new UnityAction<string>(this.OnMarketTransaction));
			this._guiStationMarketLower.btnConfirmTransaction.onClick.AddListener(new UnityAction(this.OnConfirmTransaction));
		}

		private void Start()
		{
			this.txtNoPods.text = DataHandler.GetString("GUI_MARKET_NO_CARGOPODS", false);
			this.txtNoStationMarket.text = DataHandler.GetString("GUI_MARKET_NO_TRADEABLEITEMS", false);
		}

		private void OnDestroy()
		{
			GUIBulkTrader.OnMarketTransaction.RemoveListener(new UnityAction<string>(this.OnMarketTransaction));
			this._guiStationMarketLower.btnConfirmTransaction.onClick.RemoveListener(new UnityAction(this.OnConfirmTransaction));
		}

		private void OnConfirmTransaction()
		{
			int sliderValue = this._guiStationMarketLower.GetSliderValue();
			if (sliderValue == 0 || this._selectedPodCo == null || this._selectedCategory == null)
			{
				return;
			}
			double num = 1.0;
			if (GUIBulkTrader.IsVisaHolder(CrewSim.GetSelectedCrew()))
			{
				num = ((sliderValue >= 0) ? (1.0 + GUIBulkTrader.VisaPercentBaseModifier) : (1.0 - GUIBulkTrader.VisaPercentBaseModifier));
			}
			CondOwner selectedCrew = CrewSim.GetSelectedCrew();
			double condAmount = selectedCrew.GetCondAmount(Ledger.CURRENCY);
			int num2 = Mathf.Abs(sliderValue);
			double num3 = 0.0;
			GUIShipMarketDTO guishipMarketDTO;
			if (this._dictShipMarket.TryGetValue(this._selectedCategory, out guishipMarketDTO))
			{
				double transactionPrice = this.GetTransactionPrice((float)sliderValue, guishipMarketDTO);
				num3 = (double)num2 * transactionPrice * num;
			}
			if (guishipMarketDTO == null || guishipMarketDTO.DataCoCollection == null || num3 == 0.0)
			{
				return;
			}
			if (sliderValue < 0)
			{
				if (condAmount < num3)
				{
					this._guiStationMarketLower.ShowPaymentWarning();
					return;
				}
				this._coTrader.AddCondAmount(Ledger.CURRENCY, num3, 0.0, 0f);
				selectedCrew.AddCondAmount(Ledger.CURRENCY, -num3, 0.0, 0f);
				Ledger.UpdateLedger(selectedCrew, this._coTrader.FriendlyName, -num3, string.Concat(new object[]
				{
					"Bulk trade: ",
					num2,
					" x ",
					guishipMarketDTO.DataCoCollection.FriendlyName
				}));
				MarketManager.RegisterShipToShipTransaction(selectedCrew.ship.strRegID, this._selectedPodCo.ship.strRegID, this._selectedCategory, num2, this._selectedPodCo.strID);
			}
			else
			{
				this._coTrader.AddCondAmount(Ledger.CURRENCY, -num3, 0.0, 0f);
				selectedCrew.AddCondAmount(Ledger.CURRENCY, num3, 0.0, 0f);
				Ledger.UpdateLedger(selectedCrew, this._coTrader.FriendlyName, num3, string.Concat(new object[]
				{
					"Bulk trade: ",
					num2,
					" x ",
					guishipMarketDTO.DataCoCollection.FriendlyName
				}));
				MarketManager.RegisterShipToShipTransaction(this._selectedCategory, this._selectedPodCo.ship.strRegID, selectedCrew.ship.strRegID, this._selectedPodConfig, num2);
			}
			GUIBulkTrader.OnMarketTransaction.Invoke(string.Empty);
			this.UpdateCenterUI();
		}

		private void OnMarketTransaction(string cocoll)
		{
			for (int i = this.pnlListContent.childCount - 1; i >= 0; i--)
			{
				UnityEngine.Object.Destroy(this.pnlListContent.GetChild(i).gameObject);
			}
			this._dictShipMarket = this.BuildStationMarket(this._selectedPodConfig);
			if (this._selectedPodCo != null && this._selectedPodCo.ship != null)
			{
				this._selectedPodCo.ship.ResetMass();
			}
		}

		public GUIShipMarketDTO GetMarketDTOForCategory()
		{
			if (string.IsNullOrEmpty(this._selectedCategory) || !this._dictShipMarket.ContainsKey(this._selectedCategory))
			{
				return null;
			}
			return this._dictShipMarket[this._selectedCategory];
		}

		public double GetTransactionPrice(float change, GUIShipMarketDTO marketDto)
		{
			string strRegID = this._coTrader.ship.strRegID;
			ShipMarket shipMarket = MarketManager.GetShipMarket(strRegID);
			if (marketDto == null || marketDto.AvgPrice <= 0.0 || marketDto.DataCoCollection == null || shipMarket == null)
			{
				return 0.0;
			}
			double transactionPrice = marketDto.TransactionPrice;
			double num = (double)shipMarket.PredictPriceModifierForInventoryChange(marketDto.DataCoCollection.Name, change) * marketDto.AvgPrice;
			return (change != 1f) ? ((transactionPrice + num) / 2.0) : transactionPrice;
		}

		public void SetData(CondOwner coPlayer, CondOwner coTrader)
		{
			Ship dockedPlayerShip = this.GetDockedPlayerShip(coPlayer);
			this.txtPlayerShip.text = ((dockedPlayerShip != null) ? (dockedPlayerShip.strRegID + " - " + dockedPlayerShip.publicName) : string.Empty);
			this._coPods = this.GetCargoPods(dockedPlayerShip);
			this._coTrader = coTrader;
			this.BuildCargoPodList(null);
			this._dictShipMarket = this.BuildStationMarket(null);
			this.SetNoContentStrings();
			this._guiStationMarketMiddle.ShowStationImage(coTrader.strPortraitImg);
			this._guiStationMarketMiddle.Hide();
		}

		private Ship GetDockedPlayerShip(CondOwner coPlayer)
		{
			List<string> owned = coPlayer.GetShipsOwned();
			List<Ship> allDockedShips = coPlayer.ship.GetAllDockedShips();
			return allDockedShips.FirstOrDefault((Ship s) => owned != null && owned.Contains(s.strRegID));
		}

		private void SetNoContentStrings()
		{
			this.txtNoPods.gameObject.SetActive(this._coPods == null || this._coPods.Count == 0);
			this.txtNoStationMarket.gameObject.SetActive(this._dictShipMarket == null || this._dictShipMarket.Count == 0);
		}

		private Dictionary<string, GUIShipMarketDTO> BuildStationMarket(MarketActorConfig selectedCargoPod = null)
		{
			List<GUIShipMarketDTO> stationMarket = MarketManager.GetStationMarket(CrewSim.GetSelectedCrew().ship.strRegID);
			IEnumerator enumerator = this.pnlListContent.GetEnumerator();
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
			Dictionary<string, GUIShipMarketDTO> dictionary = new Dictionary<string, GUIShipMarketDTO>();
			foreach (GUIShipMarketDTO guishipMarketDTO in stationMarket)
			{
				bool flag = selectedCargoPod == null || guishipMarketDTO.DataCoCollection.CanFitIn(selectedCargoPod);
				if (flag && selectedCargoPod != null && !selectedCargoPod.IsEmpty)
				{
					flag = selectedCargoPod.MaxVirtualInventorySize.ContainsKey(guishipMarketDTO.DataCoCollection.Name);
				}
				GUIShipMarketRow guishipMarketRow = UnityEngine.Object.Instantiate<GUIShipMarketRow>(this.guiShipMarketRow, this.pnlListContent);
				guishipMarketRow.SetData(guishipMarketDTO, flag);
				dictionary[guishipMarketDTO.DataCoCollection.Name] = guishipMarketDTO;
			}
			return dictionary;
		}

		private void BuildCargoPodList(DataCoCollection selectedCategory = null)
		{
			IEnumerator enumerator = this.pnlCargoPods.GetEnumerator();
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
			foreach (Tuple<CondOwner, MarketActorConfig> tuple in this._coPods)
			{
				bool isSelectable = selectedCategory == null || selectedCategory.CanFitIn(tuple.Item2);
				GUICargoPodRow guicargoPodRow = UnityEngine.Object.Instantiate<GUICargoPodRow>(this.guiCargoPodRow, this.pnlCargoPods);
				guicargoPodRow.SetData(tuple.Item1, tuple.Item2, isSelectable);
			}
		}

		private List<Tuple<CondOwner, MarketActorConfig>> GetCargoPods(Ship playerShip)
		{
			List<Tuple<CondOwner, MarketActorConfig>> list = new List<Tuple<CondOwner, MarketActorConfig>>();
			if (playerShip == null)
			{
				return list;
			}
			List<MarketActorConfig> cargoPods = MarketManager.GetCargoPods(playerShip.strRegID);
			foreach (MarketActorConfig marketActorConfig in cargoPods)
			{
				CondOwner cobyID = playerShip.GetCOByID(marketActorConfig.COwnerId);
				if (cobyID == null)
				{
					Debug.LogWarning("Could not find CO for cargo pod");
				}
				else
				{
					list.Add(new Tuple<CondOwner, MarketActorConfig>(cobyID, marketActorConfig));
				}
			}
			return list;
		}

		public void OnPodSelected(CondOwner selectedPodCo, MarketActorConfig selectedPodConfig)
		{
			bool flag = this._selectedPodCo == null || selectedPodCo != this._selectedPodCo;
			if (this._selectedPodCo != null)
			{
				if (this._selectedPodConfig != null && !this._selectedPodConfig.IsEmpty)
				{
					this._selectedCategory = null;
				}
				this._selectedPodCo = null;
				this._selectedPodConfig = null;
			}
			if (flag && selectedPodConfig != null)
			{
				this._selectedPodCo = selectedPodCo;
				this._selectedPodConfig = selectedPodConfig;
				GUIShipMarketDTO cargoPodData = selectedPodConfig.GetCargoPodData();
				if (cargoPodData != null && cargoPodData.DataCoCollection != null)
				{
					this.OnMarketCategorySelected(cargoPodData.DataCoCollection);
				}
			}
			this.BuildStationMarket((!flag) ? null : selectedPodConfig);
			this.UpdateCenterUI();
		}

		private void UpdateCenterUI()
		{
			this._guiStationMarketTop.HideAll();
			if (this._selectedPodConfig == null || this._selectedPodCo == null)
			{
				this._guiStationMarketTop.HighlightSelectPod();
				this._guiStationMarketMiddle.HideCargoPodImage();
			}
			else
			{
				this._guiStationMarketMiddle.ShowCargoPodImage(this._selectedPodCo.strPortraitImg);
			}
			if (this._selectedCategory == null || !this._dictShipMarket.ContainsKey(this._selectedCategory))
			{
				this._guiStationMarketTop.HighlightSelectGood();
				this._guiStationMarketMiddle.HideStationImage();
				this._guiStationMarketMiddle.SetGoodsName(string.Empty);
			}
			else
			{
				this._guiStationMarketMiddle.SetGoodsName(this._dictShipMarket[this._selectedCategory].DataCoCollection.FriendlyName);
				this._guiStationMarketMiddle.ShowStationImage(null);
			}
			if (this._selectedPodConfig != null && this._selectedCategory != null)
			{
				this._guiStationMarketLower.Show();
				this.UpdateSliderValues();
			}
			else
			{
				this._guiStationMarketLower.Hide();
			}
		}

		public void OnMarketCategorySelected(DataCoCollection dataCoCollection)
		{
			bool flag = this._selectedCategory == null || this._selectedCategory != dataCoCollection.Name;
			this._selectedCategory = null;
			if (flag && dataCoCollection != null)
			{
				this._selectedCategory = dataCoCollection.Name;
			}
			this.BuildCargoPodList((!flag) ? null : dataCoCollection);
			this.UpdateCenterUI();
		}

		private void UpdateSliderValues()
		{
			if (this._selectedPodConfig == null)
			{
				return;
			}
			GUIShipMarketDTO guishipMarketDTO = null;
			if (!this._dictShipMarket.TryGetValue(this._selectedCategory, out guishipMarketDTO) || guishipMarketDTO == null)
			{
				return;
			}
			if (this._selectedPodConfig.IsEmpty)
			{
				int num = (int)((double)MarketManager.CARGOPOD_DEFAULTMASSCAPACITY / guishipMarketDTO.AvgMass);
				if (guishipMarketDTO.Stock < num)
				{
					num = guishipMarketDTO.Stock;
				}
				this._guiStationMarketLower.SetSlider(-num, 0, 0f);
			}
			else
			{
				int num2 = 0;
				this._selectedPodConfig.MaxVirtualInventorySize.TryGetValue(this._selectedCategory, out num2);
				List<MarketItem> list;
				this._selectedPodConfig.Stock.TryGetValue(this._selectedCategory, out list);
				int num3 = (list == null) ? 0 : list.Count;
				num2 -= num3;
				int num4 = (guishipMarketDTO.Stock >= num2) ? num2 : guishipMarketDTO.Stock;
				this._guiStationMarketLower.SetSlider(-num4, num3, 0f);
			}
		}

		[SerializeField]
		private Transform pnlListContent;

		[SerializeField]
		private Transform pnlCargoPods;

		[SerializeField]
		public GUIShipMarketRow guiShipMarketRow;

		[SerializeField]
		public GUICargoPodRow guiCargoPodRow;

		[SerializeField]
		private TMP_Text txtPlayerShip;

		[SerializeField]
		private TMP_Text txtNoPods;

		[SerializeField]
		private TMP_Text txtNoStationMarket;

		[SerializeField]
		private GUIStationMarketTop _guiStationMarketTop;

		[SerializeField]
		private GUIStationMarketMiddle _guiStationMarketMiddle;

		[SerializeField]
		private GUIStationMarketLower _guiStationMarketLower;

		private List<Tuple<CondOwner, MarketActorConfig>> _coPods;

		private Dictionary<string, GUIShipMarketDTO> _dictShipMarket;

		private CondOwner _coTrader;

		private CondOwner _selectedPodCo;

		private MarketActorConfig _selectedPodConfig;

		private string _selectedCategory;
	}
}
