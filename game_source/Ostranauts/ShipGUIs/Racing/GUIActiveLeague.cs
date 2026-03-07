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
	public class GUIActiveLeague : MonoBehaviour
	{
		private void Awake()
		{
			this.btnLeave.onClick.AddListener(new UnityAction(this.OnButtonLeave));
		}

		private void OnButtonLeave()
		{
			GUIRaceKiosk.OnLeaveLeague.Invoke();
		}

		public void SetData(LeagueData leagueData, CondOwner coUser)
		{
			this.txtLeagueName.text = leagueData.LeagueName;
			this.txtDescription.text = leagueData.JsonRacingLeague.strDescription;
			this.SetRewards(leagueData.JsonRacingLeague.GetPrizeStrings());
			this.SetStandings(leagueData.Participants);
			this.SetTrackResults(leagueData);
			this.SetImage(leagueData.JsonRacingLeague.strImgPath);
			CondTrigger condTrigger = DataHandler.GetCondTrigger("TIsRaceLeagueFinished");
			if (coUser != null && condTrigger.Triggered(coUser, null, true))
			{
				GUILeagueFinishedPopUp guileagueFinishedPopUp = UnityEngine.Object.Instantiate<GUILeagueFinishedPopUp>(this._guiLeagueFinishedPopup, base.transform);
				guileagueFinishedPopUp.SetData(leagueData);
			}
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

		private void SetRewards(List<string> prizes)
		{
			if (prizes == null || prizes.Count == 0)
			{
				foreach (TMP_Text tmp_Text in this.txtPrizes)
				{
					if (!(tmp_Text == null))
					{
						tmp_Text.text = "-";
					}
				}
				return;
			}
			for (int j = 0; j < prizes.Count; j++)
			{
				if (j < this.txtPrizes.Length)
				{
					this.txtPrizes[j].text = prizes[j];
				}
			}
		}

		private void SetTrackResults(LeagueData leagueData)
		{
			foreach (string text in leagueData.JsonRacingLeague.aTracks)
			{
				List<RaceResult> raceResults = leagueData.GetRaceResults(text);
				bool flag = raceResults == null || raceResults.Count == 0;
				if (flag)
				{
					GUIRaceTrack guiraceTrack = UnityEngine.Object.Instantiate<GUIRaceTrack>(this._guiRaceTrack, this.tfResultsScrollView);
					guiraceTrack.SetData(DataHandler.GetRaceTrack(text));
				}
				else
				{
					GUIRaceResult guiraceResult = UnityEngine.Object.Instantiate<GUIRaceResult>(this._guiRaceResult, this.tfResultsScrollView);
					guiraceResult.SetData(DataHandler.GetRaceTrack(text), raceResults);
				}
			}
		}

		private void SetStandings(List<Racer> racerData)
		{
			if (racerData == null)
			{
				return;
			}
			List<Racer> list = (from a in racerData
			orderby a.Points descending
			select a).ToList<Racer>();
			for (int i = 0; i < list.Count; i++)
			{
				Racer racer = list[i];
				GUIStandingsRow guistandingsRow = UnityEngine.Object.Instantiate<GUIStandingsRow>(this._guiStandingsRow, this.tfStandingsScrollView);
				guistandingsRow.SetData(racer, i + 1);
			}
		}

		[SerializeField]
		private Transform tfStandingsScrollView;

		[SerializeField]
		private Transform tfResultsScrollView;

		[SerializeField]
		private TMP_Text txtLeagueName;

		[SerializeField]
		private TMP_Text txtDescription;

		[SerializeField]
		private TMP_Text[] txtPrizes;

		[SerializeField]
		private RawImage imgLeague;

		[SerializeField]
		private GUIStandingsRow _guiStandingsRow;

		[SerializeField]
		private GUIRaceResult _guiRaceResult;

		[SerializeField]
		private GUIRaceTrack _guiRaceTrack;

		[SerializeField]
		private GUILeagueFinishedPopUp _guiLeagueFinishedPopup;

		[SerializeField]
		private Button btnLeave;
	}
}
