using System;
using System.Collections.Generic;
using System.Linq;
using Ostranauts.Racing.Models;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Ostranauts.ShipGUIs.Racing
{
	public class GUILeagueFinishedPopUp : MonoBehaviour
	{
		private void Awake()
		{
			this.btnClose.onClick.AddListener(new UnityAction(this.OnClose));
		}

		public void SetData(LeagueData leagueData)
		{
			CondOwner coPlayer = CrewSim.coPlayer;
			if (coPlayer.GetCondRule("RacingLicense") == null)
			{
				coPlayer.AddCondRule("RacingLicense", true);
			}
			List<Racer> list = (from a in leagueData.Participants
			orderby a.Points descending
			select a).ToList<Racer>();
			int num = list.FindIndex((Racer x) => x.Name == coPlayer.strName);
			this.txtFinishingPosition.text = ((num < 0) ? "-" : (num + 1).ToString());
			RacePrize prizeForParticipant = this.GetPrizeForParticipant(leagueData, num);
			if (prizeForParticipant == null)
			{
				this.lblNothing.SetActive(true);
			}
			else
			{
				this.lblNothing.SetActive(false);
				if (!string.IsNullOrEmpty(prizeForParticipant.Loot))
				{
					this._lootPrize = DataHandler.GetLoot(prizeForParticipant.Loot);
					Dictionary<string, double> condLoot = this._lootPrize.GetCondLoot(1f, null, null);
					foreach (KeyValuePair<string, double> keyValuePair in condLoot)
					{
						string key = keyValuePair.Key;
						if (!key.StartsWith("-"))
						{
							Condition cond = DataHandler.GetCond(key);
							GUIPrize guiprize = UnityEngine.Object.Instantiate<GUIPrize>(this._pricePrefab, this.priceContainer);
							guiprize.SetData(keyValuePair.Value, cond.strNameFriendly);
						}
					}
				}
				if (!string.IsNullOrEmpty(prizeForParticipant.LedgerDef))
				{
					this._ledgerDef = DataHandler.GetLedgerDef(prizeForParticipant.LedgerDef);
					if (this._ledgerDef != null)
					{
						GUIPrize guiprize2 = UnityEngine.Object.Instantiate<GUIPrize>(this._pricePrefab, this.priceContainer);
						guiprize2.SetData(this._ledgerDef.fAmount);
					}
				}
			}
		}

		private void OnClose()
		{
			if (this._lootPrize != null)
			{
				this._lootPrize.ApplyCondLoot(CrewSim.coPlayer, 1f, null, 0f);
			}
			if (this._ledgerDef != null)
			{
				GUIRaceKiosk componentInParent = base.GetComponentInParent<GUIRaceKiosk>();
				CrewSim.coPlayer.AddCondAmount(Ledger.CURRENCY, (double)this._ledgerDef.fAmount, 0.0, 0f);
				Ledger.UpdateLedger(CrewSim.coPlayer, componentInParent.COSelf.FriendlyName, (double)this._ledgerDef.fAmount, this._ledgerDef.strDesc);
			}
			CrewSim.coPlayer.ZeroCondAmount("IsRaceLeagueFinished");
			GUIRaceKiosk componentInParent2 = base.GetComponentInParent<GUIRaceKiosk>();
			if (componentInParent2 != null)
			{
				componentInParent2.UpdateLicensePointsUI();
			}
			UnityEngine.Object.Destroy(base.gameObject);
		}

		private RacePrize GetPrizeForParticipant(LeagueData leagueData, int position)
		{
			if (leagueData.JsonRacingLeague == null || leagueData.JsonRacingLeague.aRacePrize == null || leagueData.Participants == null)
			{
				return null;
			}
			if (position >= 0 && position < leagueData.JsonRacingLeague.aRacePrize.Length)
			{
				return leagueData.JsonRacingLeague.aRacePrize[position];
			}
			return null;
		}

		[SerializeField]
		private TMP_Text txtFinishingPosition;

		[SerializeField]
		private Image imgTrophy;

		[SerializeField]
		private Transform priceContainer;

		[SerializeField]
		private GUIPrize _pricePrefab;

		[SerializeField]
		private Button btnClose;

		[SerializeField]
		private GameObject lblNothing;

		private Loot _lootPrize;

		private JsonLedgerDef _ledgerDef;
	}
}
