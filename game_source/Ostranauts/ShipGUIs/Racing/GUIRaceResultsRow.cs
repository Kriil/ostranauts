using System;
using System.Linq;
using Ostranauts.Racing;
using Ostranauts.Racing.Models;
using TMPro;
using UnityEngine;

namespace Ostranauts.ShipGUIs.Racing
{
	public class GUIRaceResultsRow : MonoBehaviour
	{
		public void SetData(RaceResult result, RaceResult first)
		{
			this.txtPosition.text = result.FinishingPosition.ToString();
			this.txtName.text = result.PilotName;
			if (result.PilotName == CrewSim.coPlayer.strName)
			{
				this.txtName.fontStyle = FontStyles.Underline;
			}
			this.txtPoints.text = result.PointsEarned.ToString();
			double num = result.LapTimes.Sum((LapTime x) => x.TotalTime);
			bool flag = num > 10000000.0;
			this.txtTotalTime.text = ((!flag) ? RacingLeagueManager.FormatTime(num) : "DNF");
			this._fastestLap = result.LapTimes.Min((LapTime x) => x.TotalTime);
			this.txtBestLap.text = ((!flag) ? RacingLeagueManager.FormatTime(this._fastestLap) : "DNF");
			TMP_Text tmp_Text = this.txtGap;
			string text;
			if (result == first || flag)
			{
				text = string.Empty;
			}
			else
			{
				text = "+" + (num - first.LapTimes.Sum((LapTime x) => x.TotalTime)).ToString("F2");
			}
			tmp_Text.text = text;
		}

		public void SetBestLap(double fastestRaceLap)
		{
			if (Math.Abs(this._fastestLap - fastestRaceLap) < 1E-05)
			{
				this.txtBestLap.color = RacingLeagueManager.ColorFastestLap;
			}
		}

		[SerializeField]
		private TMP_Text txtPosition;

		[SerializeField]
		private TMP_Text txtName;

		[SerializeField]
		private TMP_Text txtPoints;

		[SerializeField]
		private TMP_Text txtTotalTime;

		[SerializeField]
		private TMP_Text txtBestLap;

		[SerializeField]
		private TMP_Text txtGap;

		private double _fastestLap;
	}
}
