using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Ostranauts.ShipGUIs.Market
{
	public class GUIStationMarketMiddle : MonoBehaviour
	{
		private void Awake()
		{
			this.Hide();
		}

		public void Hide()
		{
			this.HideCargoPodImage();
			this.HideStationImage();
			this.SetGoodsName(string.Empty);
		}

		public void SetGoodsName(string name)
		{
			this.txtGoodsName.text = name;
			if (name == string.Empty)
			{
				this.HideStationImage();
			}
			else
			{
				this.ShowStationImage(null);
			}
		}

		public void HideCargoPodImage()
		{
			this._imgPod.gameObject.SetActive(false);
		}

		public void HideStationImage()
		{
			this._imgStation.gameObject.SetActive(false);
		}

		public void ShowCargoPodImage(string portraitImageName)
		{
			this._imgPod.gameObject.SetActive(true);
			this.SetImage(portraitImageName, this._imgPodAspectRatioFitter, this._imgPod);
		}

		public void ShowStationImage(string portraitImageName = null)
		{
			this._imgStation.gameObject.SetActive(true);
			if (portraitImageName != null)
			{
				this.SetImage(portraitImageName, this._imgStationAspectRatioFitter, this._imgStation);
			}
		}

		private void SetImage(string portraitName, AspectRatioFitter apec, RawImage rawI)
		{
			Texture2D texture2D = DataHandler.LoadPNG(portraitName + ".png", false, false);
			if (texture2D != null)
			{
				apec.aspectRatio = (float)texture2D.width / (float)texture2D.height;
				rawI.texture = texture2D;
			}
			else
			{
				rawI.gameObject.SetActive(false);
			}
		}

		[SerializeField]
		private AspectRatioFitter _imgPodAspectRatioFitter;

		[SerializeField]
		private RawImage _imgPod;

		[SerializeField]
		private AspectRatioFitter _imgStationAspectRatioFitter;

		[SerializeField]
		private RawImage _imgStation;

		[SerializeField]
		private TMP_Text txtGoodsName;

		private CondOwner _coTrader;
	}
}
