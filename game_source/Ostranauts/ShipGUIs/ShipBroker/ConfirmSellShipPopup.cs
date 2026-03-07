using System;
using System.Collections.Generic;
using Ostranauts.Events.DTOs;
using TMPro;
using UnityEngine;

namespace Ostranauts.ShipGUIs.ShipBroker
{
	public class ConfirmSellShipPopup : ConfirmationPopupBase
	{
		protected override void OnBtnPurchase()
		{
			Ship shipByRegID = CrewSim.system.GetShipByRegID(this._shipDto.RegId);
			List<CondOwner> people = shipByRegID.GetPeople(false);
			if (people != null && people.Count > 0)
			{
				base.ShowError(DataHandler.GetString("GUI_TRADE_ERROR_PEOPLE_ON_SHIP", false));
				return;
			}
			base.OnBtnPurchase();
		}

		public override void ShowPanel(ShipPurchaseDTO shipDto, double availableFunds)
		{
			base.ShowPanel(shipDto, availableFunds);
			this._shipDto.TransactionPrice = shipDto.ShipValue;
			this.txtExistingMortgage.text = "none";
			this.txtPayout.text = "$" + shipDto.ShipValue.ToString("n");
			this.txtRemainingMortgage.text = "none";
			this.txtShipValue.text = "$" + this._shipDto.TransactionPrice.ToString("n");
			LedgerLI mortgageForShip = Ledger.GetMortgageForShip(this._shipDto.RegId);
			if (mortgageForShip != null)
			{
				this.txtExistingMortgage.text = "$" + mortgageForShip.fAmount.ToString("n");
				if ((double)mortgageForShip.fAmount - shipDto.ShipValue > 0.0)
				{
					this.txtPayout.text = "$0";
					this.txtRemainingMortgage.text = "$" + ((double)mortgageForShip.fAmount - shipDto.ShipValue).ToString("n");
				}
				else
				{
					this.txtPayout.text = "$" + (shipDto.ShipValue - (double)mortgageForShip.fAmount).ToString("n");
					this.txtRemainingMortgage.text = "none";
				}
			}
		}

		[Header("Main")]
		[SerializeField]
		private TMP_Text txtExistingMortgage;

		[SerializeField]
		private TMP_Text txtShipValue;

		[SerializeField]
		private TMP_Text txtPayout;

		[SerializeField]
		private TMP_Text txtRemainingMortgage;
	}
}
