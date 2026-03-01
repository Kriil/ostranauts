using System;
using System.Collections.Generic;
using Ostranauts.Core;
using Ostranauts.Events;
using Ostranauts.Objectives;
using Ostranauts.ShipGUIs.Trade;
using Ostranauts.Ships;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Ostranauts.ShipGUIs.ShipBroker
{
	public class GUIXPDRBroker : GUITradeBase
	{
		protected override void Awake()
		{
			base.Awake();
			this.txtNoShipsInfo.gameObject.SetActive(false);
			this.ToggleVisibility(true, this.srSell);
			GUIXPDRBroker.OnPurchaseXPDR.AddListener(new UnityAction<string, double>(this.OnPurchaseConfirm));
		}

		private void OnDestroy()
		{
			GUIXPDRBroker.OnPurchaseXPDR.RemoveListener(new UnityAction<string, double>(this.OnPurchaseConfirm));
		}

		private void OnPurchaseConfirm(string strRegID, double fAmount)
		{
			this.txtError.gameObject.SetActive(false);
			CondOwner xpdr = this.GetXPDR();
			if (xpdr == null)
			{
				this.txtError.gameObject.SetActive(true);
				this.txtError.text = DataHandler.GetString("GUI_TRADE_ERROR_NO_XPDR", false);
				AudioManager.am.PlayAudioEmitter("ShipUIBtnSuppliesAcceptNeg", false, false);
				return;
			}
			if (fAmount > this._coUser.GetCondAmount(Ledger.CURRENCY))
			{
				this.txtError.gameObject.SetActive(true);
				this.txtError.text = DataHandler.GetString("GUI_TRADE_ERROR_NO_FUNDS", false);
				AudioManager.am.PlayAudioEmitter("ShipUIBtnSuppliesAcceptNeg", false, false);
				return;
			}
			xpdr.ApplyGPMChanges(new string[]
			{
				"Data,strRegID," + strRegID
			});
			xpdr.ZeroCondAmount("IsReadyTransponderReset");
			base.GetZones();
			List<string> list = base.BuyItems(new List<string>
			{
				xpdr.strID
			}, (float)fAmount, this._mapZonesUser);
			if (!this.bFits)
			{
				xpdr.Destroy();
			}
			if (this.bUpdateFunds)
			{
				this.UpdateCash(fAmount, strRegID);
				this._coUser.LogMessage(DataHandler.GetString("GUI_FINANCE_LOG_PAID", false) + this.COSelf.FriendlyName + ": " + fAmount.ToString("n"), "Bad", this._coUser.strName);
				if (CrewSim.coPlayer.HasCond("TutorialXPDRReplaceWaiting"))
				{
					CrewSim.coPlayer.ZeroCondAmount("TutorialXPDRReplaceWaiting");
					MonoSingleton<ObjectiveTracker>.Instance.CheckObjective(CrewSim.coPlayer.strID);
				}
			}
			if (this.bFits)
			{
				this.txtError.gameObject.SetActive(false);
				AudioManager.am.PlayAudioEmitter("ShipUIBtnSuppliesAcceptPos", false, false);
			}
			else
			{
				this.txtError.gameObject.SetActive(true);
			}
			foreach (string shipReg in list)
			{
				MonoSingleton<AsyncShipLoader>.Instance.SaveChangedCO(shipReg);
			}
		}

		protected override void RecordTransaction(float salesPrice, bool areWeSelling)
		{
			this.bUpdateFunds = true;
			this.bFits = true;
		}

		private void ToggleVisibility(bool isOn, ScrollRect sr)
		{
			sr.gameObject.SetActive(isOn);
		}

		public override void Init(CondOwner coSelf, Dictionary<string, string> dict, string strCOKey)
		{
			base.Init(coSelf, dict, strCOKey);
			this.txtTitle01.text = AIShipManager.strATCLast + DataHandler.GetString("GUI_TRADE_DEPARTMENT_LICENSING01", false);
			this.txtTitle02.text = DataHandler.GetString("GUI_TRADE_DEPARTMENT_LICENSING02", false);
			this.txtTitle03.text = DataHandler.GetString("GUI_TRADE_REPLACEMENT_XPDR", false);
			this.SetTrade();
		}

		private float GetVendorTransactionPriceModifier(bool buy)
		{
			string strName = (!buy) ? "DiscountSell" : "DiscountBuy";
			float num = (float)this.COSelf.GetCondAmount(strName);
			if (num == 0f)
			{
				num = 1f;
			}
			return num;
		}

		private void SetTrade()
		{
			this.SetupPlayerShips();
		}

		public override void UpdateUI()
		{
			base.UpdateUI();
			foreach (KeyValuePair<string, GameObject> keyValuePair in this._shipDict)
			{
				if (!(keyValuePair.Value == null))
				{
					UnityEngine.Object.Destroy(keyValuePair.Value);
				}
			}
			this._shipDict.Clear();
			this.SetupPlayerShips();
		}

		private void SetupPlayerShips()
		{
			this.txtError.gameObject.SetActive(false);
			CondOwner xpdr = this.GetXPDR();
			if (xpdr != null)
			{
				GUIXPDRBroker._fPriceXPDR = xpdr.GetCondAmount("StatBasePrice");
				xpdr.Destroy();
				float vendorTransactionPriceModifier = this.GetVendorTransactionPriceModifier(true);
				List<string> shipsForOwner = CrewSim.system.GetShipsForOwner(this._coUser.strID);
				this.txtNoShipsInfo.gameObject.SetActive(shipsForOwner.Count == 0);
				this.txtNoShipsInfo.text = "No legally owned ships could be found for Captain " + this._coUser.strName;
				foreach (string text in shipsForOwner)
				{
					Ship shipByRegID = CrewSim.system.GetShipByRegID(text);
					if (shipByRegID != null)
					{
						SellXPDREntry sellXPDREntry = UnityEngine.Object.Instantiate<SellXPDREntry>(this._sellXPDREntryPrefab, this.srSell.content);
						sellXPDREntry.SetData(shipByRegID, (float)GUIXPDRBroker._fPriceXPDR * vendorTransactionPriceModifier);
						this._shipDict[text] = sellXPDREntry.gameObject;
					}
				}
				LayoutRebuilder.ForceRebuildLayoutImmediate(this.srSell.GetComponent<RectTransform>());
				return;
			}
			this.txtError.gameObject.SetActive(true);
			this.txtError.text = DataHandler.GetString("GUI_TRADE_ERROR_NO_XPDR", false);
			AudioManager.am.PlayAudioEmitter("ShipUIBtnSuppliesAcceptNeg", false, false);
		}

		private CondOwner GetXPDR()
		{
			Loot loot = DataHandler.GetLoot("ItmTransponder01LooseNew");
			List<CondOwner> coloot = loot.GetCOLoot(null, false, null);
			for (int i = coloot.Count - 1; i >= 0; i--)
			{
				if (i == 0)
				{
					return coloot[i];
				}
				if (coloot[i] != null)
				{
					coloot[i].Destroy();
				}
			}
			return null;
		}

		private void UpdateCash(double fAmount, string strRegID)
		{
			this.COSelf.AddCondAmount(Ledger.CURRENCY, fAmount, 0.0, 0f);
			this._coUser.AddCondAmount(Ledger.CURRENCY, -fAmount, 0.0, 0f);
			Ledger.UpdateLedger(this.COSelf, this._coUser.FriendlyName, fAmount, "New Ship Transponder: " + strRegID);
		}

		public override void SaveAndClose()
		{
			base.StopAllCoroutines();
			GUIXPDRBroker.OnPurchaseXPDR.RemoveListener(new UnityAction<string, double>(this.OnPurchaseConfirm));
			base.SaveAndClose();
		}

		public static readonly OnPurchaseXPDREvent OnPurchaseXPDR = new OnPurchaseXPDREvent();

		[SerializeField]
		private ScrollRect srSell;

		[SerializeField]
		private TMP_Text txtTitle01;

		[SerializeField]
		private TMP_Text txtTitle02;

		[SerializeField]
		private TMP_Text txtTitle03;

		[SerializeField]
		private TMP_Text txtNoShipsInfo;

		[Header("Prefabs")]
		[SerializeField]
		private SellXPDREntry _sellXPDREntryPrefab;

		private readonly Dictionary<string, GameObject> _shipDict = new Dictionary<string, GameObject>();

		private static double _fPriceXPDR = 0.0;
	}
}
