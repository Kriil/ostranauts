using System;
using Ostranauts.Core;
using Ostranauts.Racing;
using Ostranauts.Racing.Models;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Ostranauts.ShipGUIs.Racing
{
	public class GUIRaceTrack : MonoBehaviour
	{
		public void SetData(JsonRaceTrack jTrack)
		{
			if (jTrack == null || jTrack.aWaypoints == null)
			{
				return;
			}
			this.txtTrackName.text = jTrack.strNameFriendly;
			this.txtWaypoints.text = jTrack.aWaypoints.Length.ToString();
			int nLaps = jTrack.nLaps;
			this.txtLaps.text = ((nLaps != 0) ? nLaps.ToString() : " - ");
			this.txtType.text = jTrack.RaceTrackType.ToString();
			this.imgTrackPoints.texture = jTrack.GetTrackTexture();
			LapTime personalBestForTrack = MonoSingleton<RacingLeagueManager>.Instance.GetPersonalBestForTrack(jTrack.strName);
			this.txtLapRecord.text = ((personalBestForTrack == null) ? "-" : RacingLeagueManager.FormatTime(personalBestForTrack.TotalTime));
			if (this.txtDescription != null)
			{
				this.txtDescription.text = jTrack.strDescription;
			}
		}

		[SerializeField]
		private TMP_Text txtTrackName;

		[SerializeField]
		private TMP_Text txtType;

		[SerializeField]
		private TMP_Text txtWaypoints;

		[SerializeField]
		private TMP_Text txtLapRecord;

		[SerializeField]
		private TMP_Text txtLaps;

		[SerializeField]
		private RawImage imgTrackPoints;

		[SerializeField]
		private TMP_Text txtDescription;
	}
}
