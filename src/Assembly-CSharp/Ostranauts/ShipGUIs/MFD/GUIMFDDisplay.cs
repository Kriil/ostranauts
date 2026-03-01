using System;
using System.Collections.Generic;
using Ostranauts.Events;
using Ostranauts.Events.DTOs;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Ostranauts.ShipGUIs.MFD
{
	public class GUIMFDDisplay : MonoBehaviour
	{
		private void Awake()
		{
			if (GUIMFDDisplay.OnUpdateMFD == null)
			{
				GUIMFDDisplay.OnUpdateMFD = new OnUpdateMFDEvent();
			}
			GUIMFDDisplay.OnUpdateMFD.AddListener(new UnityAction<MFDDTO>(this.ShowMenu));
		}

		private void OnDestroy()
		{
			if (GUIMFDDisplay.OnUpdateMFD != null)
			{
				GUIMFDDisplay.OnUpdateMFD.RemoveListener(new UnityAction<MFDDTO>(this.ShowMenu));
			}
		}

		private string Format(List<string> aStrings, List<string> aColors)
		{
			if (aStrings == null)
			{
				aStrings = new List<string>();
			}
			while (aStrings.Count < 13)
			{
				aStrings.Add(string.Empty);
			}
			while (aColors.Count < 13)
			{
				aColors.Add("<color=#007FD8FF>");
			}
			return string.Concat(new string[]
			{
				" <size=30>",
				aColors[0],
				aStrings[0],
				"</color></size>\n",
				aColors[1],
				aStrings[1],
				"</color>\n <size=30>",
				aColors[2],
				aStrings[2],
				"</color></size>\n",
				aColors[3],
				aStrings[3],
				"</color>\n <size=30>",
				aColors[4],
				aStrings[4],
				"</color></size>\n",
				aColors[5],
				aStrings[5],
				"</color>\n <size=30>",
				aColors[6],
				aStrings[6],
				"</color></size>\n",
				aColors[7],
				aStrings[7],
				"</color>\n <size=30>",
				aColors[8],
				aStrings[8],
				"</color></size>\n",
				aColors[9],
				aStrings[9],
				"</color>\n <size=30>",
				aColors[10],
				aStrings[10],
				"</color></size>\n",
				aColors[11],
				aStrings[11],
				"</color>\n <size=30>",
				aColors[12],
				aStrings[12],
				"</color></size>\n"
			});
		}

		private void ShowMenu(MFDDTO mfdDto)
		{
			this.txtTitle.text = mfdDto.Title;
			string text = this.Format(mfdDto.Left, this.Colors);
			this.txtLeft.text = text;
			text = this.Format(mfdDto.Right, this.Colors);
			this.txtRight.text = text;
		}

		public static OnUpdateMFDEvent OnUpdateMFD;

		[SerializeField]
		private Text txtLeft;

		[SerializeField]
		private Text txtRight;

		[SerializeField]
		private Text txtTitle;

		private const string strClrW = "<color=#a0afe7ff>";

		private const string strClrB = "<color=#007FD8FF>";

		private const string strClrClose = "</color>";

		private const string strSmallOpen = " <size=30>";

		private const string strSmallClose = "</size>";

		private readonly List<string> Colors = new List<string>
		{
			"<color=#007FD8FF>",
			"<color=#a0afe7ff>",
			"<color=#007FD8FF>",
			"<color=#a0afe7ff>",
			"<color=#007FD8FF>",
			"<color=#a0afe7ff>",
			"<color=#007FD8FF>",
			"<color=#a0afe7ff>",
			"<color=#007FD8FF>",
			"<color=#a0afe7ff>",
			"<color=#007FD8FF>",
			"<color=#a0afe7ff>"
		};

		private static List<string> Blank = new List<string>
		{
			string.Empty,
			string.Empty,
			string.Empty,
			string.Empty,
			string.Empty,
			string.Empty,
			string.Empty,
			string.Empty,
			string.Empty,
			string.Empty,
			string.Empty,
			string.Empty
		};
	}
}
