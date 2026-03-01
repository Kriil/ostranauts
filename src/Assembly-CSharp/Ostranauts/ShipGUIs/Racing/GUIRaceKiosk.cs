using System;
using System.Collections;
using System.Collections.Generic;
using Ostranauts.Core;
using Ostranauts.Events;
using Ostranauts.Racing;
using Ostranauts.Racing.Models;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Ostranauts.ShipGUIs.Racing
{
	public class GUIRaceKiosk : GUIData
	{
		private new void Awake()
		{
			base.Awake();
			if (GUIRaceKiosk.OnLeagueJoin == null)
			{
				GUIRaceKiosk.OnLeagueJoin = new OnLeagueJoinRequestEvent();
			}
			if (GUIRaceKiosk.OnLeaveLeague == null)
			{
				GUIRaceKiosk.OnLeaveLeague = new UnityEvent();
			}
			GUIRaceKiosk.OnLeaveLeague.AddListener(new UnityAction(this.OnLeagueLeavePressed));
			GUIRaceKiosk.OnLeagueJoin.AddListener(new UnityAction<JsonRacingLeague>(this.OnLeagueJoinRequestPressed));
			this.btnClose.onClick.AddListener(new UnityAction(this.OnBtnCloseDown));
			this.txtError.gameObject.SetActive(false);
		}

		private void OnDestroy()
		{
			if (GUIRaceKiosk.OnLeagueJoin != null)
			{
				GUIRaceKiosk.OnLeagueJoin.RemoveAllListeners();
			}
			if (GUIRaceKiosk.OnLeaveLeague != null)
			{
				GUIRaceKiosk.OnLeaveLeague.RemoveAllListeners();
			}
		}

		private void OnBtnCloseDown()
		{
			CrewSim.LowerUI(false);
		}

		private void OnLeagueJoinRequestPressed(JsonRacingLeague jLeague)
		{
			if (jLeague == null)
			{
				Debug.LogWarning("League obj was null");
				return;
			}
			bool flag = true;
			if (!string.IsNullOrEmpty(jLeague.strEntryFeeLedgerDef))
			{
				JsonLedgerDef ledgerDef = DataHandler.GetLedgerDef(jLeague.strEntryFeeLedgerDef);
				if (ledgerDef != null)
				{
					double condAmount = this._coUser.GetCondAmount(Ledger.CURRENCY);
					if (condAmount < (double)ledgerDef.fAmount)
					{
						flag = false;
					}
				}
			}
			CondTrigger condTrigger = DataHandler.GetCondTrigger(jLeague.ctEntryRequirements);
			if (condTrigger != null && condTrigger.Triggered(this._coUser, null, true) && flag)
			{
				Ledger.AddLI(jLeague.strEntryFeeLedgerDef, this._coUser, this.COSelf);
				if (!string.IsNullOrEmpty(jLeague.strStartingLoot))
				{
					Loot loot = DataHandler.GetLoot(jLeague.strStartingLoot);
					loot.ApplyCondLoot(this._coUser, 1f, null, 0f);
				}
				this.txtError.gameObject.SetActive(false);
				this._coUser.AddCondAmount("IsRaceLeagueRegistered", 1.0, 0.0, 0f);
				MonoSingleton<RacingLeagueManager>.Instance.StartNewLeague(jLeague, this._coUser);
				this.ShowActiveLeague(true);
			}
			else
			{
				this.txtError.text = (flag ? DataHandler.GetString("GUI_RACEKIOSK_ERROR_REQUIREMENTS", false) : DataHandler.GetString("GUI_TRADE_ERROR_NO_FUNDS", false));
				AudioManager.am.PlayAudioEmitter("ShipUIBtnSuppliesAcceptNeg", false, false);
				this.txtError.gameObject.SetActive(true);
			}
		}

		private void OnLeagueLeavePressed()
		{
			this._coUser.ZeroCondAmount("IsRaceLeagueRegistered");
			MonoSingleton<RacingLeagueManager>.Instance.LeaveLeague();
			this.SetData();
			IEnumerator enumerator = this.pnlActiveLeague.GetEnumerator();
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
		}

		public override void Init(CondOwner coSelf, Dictionary<string, string> dict, string strCOKey)
		{
			base.Init(coSelf, dict, strCOKey);
			if (this.COSelf.GetInteractionCurrent() != null && this.COSelf.GetInteractionCurrent().objThem != null)
			{
				this._coUser = CrewSim.coPlayer;
			}
			this.SetData();
		}

		private void SetData()
		{
			CondTrigger condTrigger = DataHandler.GetCondTrigger("TIsRaceLeagueRegistered");
			this.UpdateLicensePointsUI();
			if (condTrigger != null && condTrigger.Triggered(this._coUser, null, true))
			{
				this.ShowActiveLeague(false);
			}
			else
			{
				this.ShowAllAvailableLeagues();
			}
		}

		private void OnCheat()
		{
		}

		public void UpdateLicensePointsUI()
		{
			float num = (float)CrewSim.coPlayer.GetCondAmount("IsRacingLicensePoint");
			CondRule condRule = DataHandler.GetCondRule("RacingLicense");
			CondRuleThresh condRuleThresh = null;
			CondRuleThresh condRuleThresh2 = null;
			for (int i = 0; i < condRule.aThresholds.Length; i++)
			{
				CondRuleThresh condRuleThresh3 = condRule.aThresholds[i];
				if (condRuleThresh3.fMax >= num)
				{
					condRuleThresh = condRuleThresh3;
					if (i < condRule.aThresholds.Length - 1)
					{
						condRuleThresh2 = condRule.aThresholds[i + 1];
					}
					break;
				}
			}
			if (condRuleThresh == null)
			{
				return;
			}
			float fillAmount = Mathf.InverseLerp(condRuleThresh.fMin, condRuleThresh.fMax, num);
			this.imgFill.fillAmount = fillAmount;
			Loot loot = DataHandler.GetLoot(condRuleThresh.strLootNew);
			this.txtLicense.text = "NO LICENSE";
			if (loot != null)
			{
				foreach (KeyValuePair<string, double> keyValuePair in loot.GetCondLoot(1f, null, null))
				{
					if (keyValuePair.Value >= 0.0)
					{
						this.txtLicense.text = DataHandler.GetCond(keyValuePair.Key).strNameFriendly;
						break;
					}
				}
			}
			if (condRuleThresh2 == null)
			{
				this.txtPoints.text = "MAX";
				this.txtNextLicense.text = "-";
			}
			else
			{
				this.txtPoints.text = (condRuleThresh.fMax - num).ToString("N0") + " POINTS TO";
				Loot loot2 = DataHandler.GetLoot(condRuleThresh2.strLootNew);
				if (loot2 == null)
				{
					this.txtNextLicense.text = "-";
				}
				else
				{
					foreach (KeyValuePair<string, double> keyValuePair2 in loot2.GetCondLoot(1f, null, null))
					{
						if (keyValuePair2.Value >= 0.0)
						{
							this.txtNextLicense.text = DataHandler.GetCond(keyValuePair2.Key).strNameFriendly;
							break;
						}
					}
				}
			}
		}

		private void DestroyLeagueCards()
		{
			IEnumerator enumerator = this.pnlListContent.transform.GetEnumerator();
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
		}

		private void ShowActiveLeague(bool cleanUp)
		{
			if (cleanUp)
			{
				this.DestroyLeagueCards();
			}
			LeagueData activeLeagueForUser = MonoSingleton<RacingLeagueManager>.Instance.GetActiveLeagueForUser();
			if (activeLeagueForUser == null)
			{
				return;
			}
			GUIActiveLeague guiactiveLeague = UnityEngine.Object.Instantiate<GUIActiveLeague>(this._guiActiveLeague, this.pnlActiveLeague);
			guiactiveLeague.SetData(activeLeagueForUser, this._coUser);
		}

		private void ShowAllAvailableLeagues()
		{
			foreach (KeyValuePair<string, JsonRacingLeague> keyValuePair in DataHandler.dictRacingLeagues)
			{
				JsonRacingLeague value = keyValuePair.Value;
				if (value != null && this.IsValidHost(this.COSelf, value.ctLeagueHost))
				{
					GUILeagueCard guileagueCard = UnityEngine.Object.Instantiate<GUILeagueCard>(this._guiLeagueCard, this.pnlListContent);
					guileagueCard.SetData(value);
					this._leagues.Add(guileagueCard);
				}
			}
		}

		private bool IsValidHost(CondOwner coPlayer, string ct)
		{
			if (coPlayer == null || string.IsNullOrEmpty(ct))
			{
				return true;
			}
			CondTrigger condTrigger = DataHandler.GetCondTrigger(ct);
			return condTrigger.Triggered(coPlayer, null, true);
		}

		public override void SaveAndClose()
		{
			base.StopAllCoroutines();
			base.SaveAndClose();
		}

		public static UnityEvent OnLeaveLeague = new UnityEvent();

		public static OnLeagueJoinRequestEvent OnLeagueJoin = new OnLeagueJoinRequestEvent();

		[SerializeField]
		private Transform pnlListContent;

		[SerializeField]
		private Transform pnlActiveLeague;

		[SerializeField]
		private GUILeagueCard _guiLeagueCard;

		[SerializeField]
		private GUIActiveLeague _guiActiveLeague;

		[SerializeField]
		private TMP_Text txtError;

		[SerializeField]
		private Button btnClose;

		[SerializeField]
		private Image imgFill;

		[SerializeField]
		private TMP_Text txtLicense;

		[SerializeField]
		private TMP_Text txtPoints;

		[SerializeField]
		private TMP_Text txtNextLicense;

		private List<GUILeagueCard> _leagues = new List<GUILeagueCard>();

		private CondOwner _coUser;

		[SerializeField]
		private Button cheat;
	}
}
