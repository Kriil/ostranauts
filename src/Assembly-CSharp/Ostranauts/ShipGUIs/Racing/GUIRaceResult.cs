using System;
using System.Collections.Generic;
using System.Linq;
using Ostranauts.Core;
using Ostranauts.Racing;
using Ostranauts.Racing.Models;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Ostranauts.ShipGUIs.Racing
{
	public class GUIRaceResult : MonoBehaviour
	{
		public void SetData(JsonRaceTrack jTrack, List<RaceResult> raceResults)
		{
			this.txtTrackName.text = jTrack.strNameFriendly;
			this.imgTrack.texture = jTrack.GetTrackTexture();
			LapTime personalBestForTrack = MonoSingleton<RacingLeagueManager>.Instance.GetPersonalBestForTrack(jTrack.strName);
			this.txtPBLapTime.text = ((personalBestForTrack == null) ? "-" : RacingLeagueManager.FormatTime(personalBestForTrack.TotalTime));
			this.txtTrackType.text = jTrack.RaceTrackType.ToString();
			this.txtLaps.text = jTrack.nLaps.ToString();
			List<RaceResult> list = (from x in raceResults
			orderby x.FinishingPosition
			select x).ToList<RaceResult>();
			double num = double.PositiveInfinity;
			List<GUIRaceResultsRow> list2 = new List<GUIRaceResultsRow>();
			foreach (RaceResult raceResult in list)
			{
				if (raceResult.LapTimes.Min((LapTime x) => x.TotalTime) < num)
				{
					num = raceResult.LapTimes.Min((LapTime x) => x.TotalTime);
				}
				GUIRaceResultsRow guiraceResultsRow = UnityEngine.Object.Instantiate<GUIRaceResultsRow>(this._guiResultsRow, this.tfResultsScrollView);
				guiraceResultsRow.SetData(raceResult, list[0]);
				list2.Add(guiraceResultsRow);
			}
			foreach (GUIRaceResultsRow guiraceResultsRow2 in list2)
			{
				guiraceResultsRow2.SetBestLap(num);
			}
			this.txtBestLapTime.text = RacingLeagueManager.FormatTime(num);
		}

		[SerializeField]
		private RawImage imgTrack;

		[SerializeField]
		private Transform tfResultsScrollView;

		[SerializeField]
		private TMP_Text txtTrackName;

		[SerializeField]
		private TMP_Text txtBestLapTime;

		[SerializeField]
		private TMP_Text txtPBLapTime;

		[SerializeField]
		private TMP_Text txtTrackType;

		[SerializeField]
		private TMP_Text txtLaps;

		[SerializeField]
		private GUIRaceResultsRow _guiResultsRow;
	}
}
