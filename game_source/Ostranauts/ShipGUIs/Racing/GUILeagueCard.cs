using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Ostranauts.ShipGUIs.Racing
{
	public class GUILeagueCard : MonoBehaviour
	{
		private void Awake()
		{
			this.btnJoin.onClick.AddListener(delegate()
			{
				GUIRaceKiosk.OnLeagueJoin.Invoke(this._league);
			});
		}

		public void SetData(JsonRacingLeague jLeague)
		{
			if (jLeague == null)
			{
				return;
			}
			this._league = jLeague;
			this.txtLeagueName.text = jLeague.strNameFriendly;
			this.txtDescription.text = jLeague.strDescription;
			this.SetRequirements(jLeague.ctEntryRequirements, jLeague.strEntryFeeLedgerDef);
			this.SetImage(jLeague.strImgPath);
			this.SetTrackList(jLeague.aTracks);
			this.SetPriceField(jLeague.GetPrizeStrings());
		}

		private void SetRequirements(string requirementsCT, string entryFeeLedger)
		{
			string text = "-";
			if (!string.IsNullOrEmpty(requirementsCT))
			{
				CondTrigger condTrigger = DataHandler.GetCondTrigger(requirementsCT);
				List<string> allReqNames = condTrigger.GetAllReqNames(false);
				for (int i = 0; i < allReqNames.Count; i++)
				{
					string strName = allReqNames[i];
					Condition cond = DataHandler.GetCond(strName);
					if (i != 0)
					{
						text += ", ";
					}
					text += cond.strNameFriendly;
				}
			}
			this.txtRequirementDescription.text = text;
			string text2 = "-";
			if (!string.IsNullOrEmpty(entryFeeLedger))
			{
				JsonLedgerDef ledgerDef = DataHandler.GetLedgerDef(entryFeeLedger);
				if (ledgerDef != null)
				{
					text2 = "$" + ledgerDef.fAmount.ToString("F1");
				}
			}
			this.txtEnrollmentFees.text = text2;
		}

		private void SetImage(string imagePath)
		{
			if (string.IsNullOrEmpty(imagePath))
			{
				this.imgLeague.gameObject.SetActive(false);
				RectTransform component = this.txtDescription.GetComponent<RectTransform>();
				component.anchorMin = new Vector2(0.01f, component.anchorMin.y);
				component.offsetMin = new Vector2(0f, 0f);
			}
			else
			{
				this.imgLeague.texture = DataHandler.LoadPNG(imagePath, false, false);
			}
		}

		private void SetTrackList(string[] tracks)
		{
			if (tracks == null)
			{
				return;
			}
			foreach (string strName in tracks)
			{
				JsonRaceTrack raceTrack = DataHandler.GetRaceTrack(strName);
				GUIRaceTrack guiraceTrack = UnityEngine.Object.Instantiate<GUIRaceTrack>(this._guiRaceTrack, this.tfTrackScrollView);
				guiraceTrack.SetData(raceTrack);
				this._tracks.Add(guiraceTrack);
			}
		}

		private void SetPriceField(List<string> prices)
		{
			if (prices == null || prices.Count == 0)
			{
				foreach (TMP_Text tmp_Text in this.txtPrices)
				{
					if (!(tmp_Text == null))
					{
						tmp_Text.text = "-";
					}
				}
				return;
			}
			for (int j = 0; j < prices.Count; j++)
			{
				if (j < this.txtPrices.Length)
				{
					this.txtPrices[j].text = prices[j];
				}
			}
		}

		[SerializeField]
		private TMP_Text txtLeagueName;

		[SerializeField]
		private TMP_Text txtDescription;

		[SerializeField]
		private RawImage imgLeague;

		[SerializeField]
		private TMP_Text txtRequirementDescription;

		[SerializeField]
		private TMP_Text txtEnrollmentFees;

		[SerializeField]
		private TMP_Text[] txtPrices;

		[SerializeField]
		private Transform tfTrackScrollView;

		[SerializeField]
		private GUIRaceTrack _guiRaceTrack;

		[SerializeField]
		private Button btnJoin;

		private List<GUIRaceTrack> _tracks = new List<GUIRaceTrack>();

		private JsonRacingLeague _league;
	}
}
