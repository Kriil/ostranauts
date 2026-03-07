using System;
using System.Collections.Generic;
using Ostranauts.Events;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Ostranauts.ShipGUIs.Market
{
	public class GUIBulkTrader : GUIData
	{
		public override void Init(CondOwner coSelf, Dictionary<string, string> dict, string strCOKey)
		{
			base.Init(coSelf, dict, strCOKey);
			this._coUser = this.COSelf.GetInteractionCurrent().objThem;
			this._guiShipMarket.SetData(this._coUser, this.COSelf);
			this.SetUIVisaDiscount(this._coUser);
		}

		private void SetUIVisaDiscount(CondOwner coUser)
		{
			bool flag = GUIBulkTrader.IsVisaHolder(coUser);
			if (flag)
			{
				this.txtVisaActive.text = "ACTIVE";
				this.imgVisaActive.color = new Color(0f, 0.87109375f, 0.17578125f, 0.359375f);
			}
			else
			{
				this.txtVisaActive.text = "INACTIVE";
				this.imgVisaActive.color = new Color(0.45490196f, 0.03515625f, 0.03515625f, 0.70703125f);
			}
		}

		public static bool IsVisaHolder(CondOwner coUser)
		{
			if (coUser == null || coUser.ship == null)
			{
				return false;
			}
			string strRegID = coUser.ship.strRegID;
			CondTrigger condTrigger = DataHandler.GetCondTrigger("TIsVisaHolder" + strRegID);
			return condTrigger != null && !condTrigger.IsBlank() && condTrigger.Triggered(coUser, null, true);
		}

		private void OnDestroy()
		{
			GUIBulkTrader.OnMarketTransaction.RemoveAllListeners();
		}

		public static OnMarketTransactionEvent OnMarketTransaction = new OnMarketTransactionEvent();

		public static double VisaPercentBaseModifier = 0.05;

		[SerializeField]
		private GUISystemMarket _guiSystemMarket;

		[SerializeField]
		private GUIShipMarket _guiShipMarket;

		[SerializeField]
		private TMP_Text txtVisaActive;

		[SerializeField]
		private Image imgVisaActive;

		private CondOwner _coUser;
	}
}
