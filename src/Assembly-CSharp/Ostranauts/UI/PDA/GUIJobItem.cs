using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Ostranauts.UI.PDA
{
	public class GUIJobItem : MonoBehaviour
	{
		public void SetData(JsonInstallable ji)
		{
			string text = string.Empty;
			string cofriendlyName = DataHandler.GetCOFriendlyName(ji.strStartInstall);
			if (!string.IsNullOrEmpty(cofriendlyName))
			{
				string[] array = cofriendlyName.Split(new char[]
				{
					' '
				});
				for (int i = 0; i < array.Length; i++)
				{
					if (i != 0)
					{
						text += " ";
					}
					string text2 = array[i];
					if (text2.Length > GUIJobItem.MAX_WORD_LENGTH)
					{
						text2 = text2.Insert(GUIJobItem.MAX_WORD_LENGTH - 1, "-");
					}
					text += text2;
				}
			}
			this._title.text = text;
			string str = "blank";
			if (DataHandler.dictCOs.ContainsKey(ji.strStartInstall))
			{
				str = DataHandler.dictCOs[ji.strStartInstall].strPortraitImg;
			}
			else if (DataHandler.dictCOOverlays.ContainsKey(ji.strStartInstall))
			{
				str = DataHandler.dictCOOverlays[ji.strStartInstall].strPortraitImg;
			}
			Texture2D texture2D = DataHandler.LoadPNG(str + ".png", false, false);
			float num = (float)texture2D.width / (float)texture2D.height;
			this._aspectRationFitter.aspectRatio = num;
			if (num >= 3f)
			{
				this._aspectRationFitter.aspectMode = AspectRatioFitter.AspectMode.WidthControlsHeight;
			}
			this._texture.texture = texture2D;
			this._btn.onClick.AddListener(delegate()
			{
				CrewSim.objInstance.StartPaintingJob(ji);
			});
		}

		public void SetData(string title, string strImg, UnityAction callback)
		{
			this._title.text = title;
			this._texture.texture = DataHandler.LoadPNG(strImg + ".png", false, false);
			this._btn.onClick.AddListener(callback);
		}

		private static int MAX_WORD_LENGTH = 15;

		[SerializeField]
		private TMP_Text _title;

		[SerializeField]
		private AspectRatioFitter _aspectRationFitter;

		[SerializeField]
		private RawImage _texture;

		[SerializeField]
		private Button _btn;
	}
}
