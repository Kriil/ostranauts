using System;
using Ostranauts.Racing;
using Ostranauts.Racing.Models;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Ostranauts.ShipGUIs.Racing
{
	public class GUIStandingsRow : MonoBehaviour
	{
		public void SetData(Racer racer, int position)
		{
			this.txtPosition.text = position.ToString();
			this.txtName.text = racer.Name;
			if (racer.Name == CrewSim.coPlayer.strName)
			{
				this.txtName.fontStyle = FontStyles.Underline;
			}
			this.txtPoints.text = racer.Points.ToString();
			switch (position)
			{
			case 1:
				this.imgHighlight.color = RacingLeagueManager.ColorGold;
				break;
			case 2:
				this.imgHighlight.color = RacingLeagueManager.ColorSilver;
				break;
			case 3:
				this.imgHighlight.color = RacingLeagueManager.ColorBronze;
				break;
			default:
				this.imgHighlight.gameObject.SetActive(false);
				break;
			}
		}

		[SerializeField]
		private TMP_Text txtPosition;

		[SerializeField]
		private TMP_Text txtName;

		[SerializeField]
		private TMP_Text txtPoints;

		[SerializeField]
		private Image imgHighlight;
	}
}
