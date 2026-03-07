using System;
using System.Collections.Generic;
using Ostranauts.ShipGUIs.Transit.Interfaces;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Ostranauts.ShipGUIs.Transit
{
	public class GUITransitOKLG : MonoBehaviour, ITransitUI
	{
		protected void Awake()
		{
			this.lblTitle = base.transform.Find("Mask").Find("lblTitle").GetComponent<TextMeshProUGUI>();
			this.lamp04 = base.transform.Find("btnStop04").Find("bmpStop04").GetComponent<GUILamp>();
			this.lamp06 = base.transform.Find("btnStop06").Find("bmpStop06").GetComponent<GUILamp>();
			this.lamp07 = base.transform.Find("btnStop07").Find("bmpStop07").GetComponent<GUILamp>();
			this.lamp08 = base.transform.Find("btnStop08").Find("bmpStop08").GetComponent<GUILamp>();
			this.cgBoarding = base.transform.Find("pnlBoarding").GetComponent<CanvasGroup>();
			CanvasManager.HideCanvasGroup(this.cgBoarding);
			if (GUITransit.OnButtonPressed != null)
			{
				GUITransit.OnButtonPressed.AddListener(new UnityAction<JsonTransitConnection>(this.OnUserPressedButton));
			}
		}

		public void SetData(List<JsonTransitConnection> connections, CondOwner coKiosk)
		{
			this.lamp04.State = 0;
			this.lamp06.State = 0;
			this.lamp07.State = 0;
			this.lamp08.State = 0;
			if (coKiosk.ship.strRegID == "OKLG_BIZ")
			{
				this.lamp04.State = 3;
				this.lblTitle.text = "Azikiwe Commercial\nStation";
			}
			else if (coKiosk.ship.strRegID == "OKLG_RES")
			{
				this.lamp06.State = 3;
				this.lblTitle.text = "Azikiwe Estates\nStation";
			}
			else if (coKiosk.ship.strRegID == "OKLG")
			{
				this.lamp07.State = 3;
				this.lblTitle.text = "Port Azikiwe\nStation";
			}
			else if (coKiosk.ship.strRegID == "OKLG_MES")
			{
				this.lamp08.State = 3;
				this.lblTitle.text = "Old Emporium\nStation";
			}
			for (int i = 0; i < connections.Count; i++)
			{
				JsonTransitConnection connection = connections[i];
				if (connection.strTargetRegID == "OKLG_BIZ" && this._btnBiz == null)
				{
					this._btnBiz = base.transform.Find("btnStop04").GetComponent<Button>();
					this._btnBiz.onClick.AddListener(delegate()
					{
						GUITransit.OnButtonPressed.Invoke(connection);
					});
					AudioManager.AddBtnAudio(this._btnBiz.gameObject, "ShipUIBtnReactorCoilRearIn", "ShipUIBtnReactorCoilRearOut");
				}
				else if (connection.strTargetRegID == "OKLG_RES" && this._btnRes == null)
				{
					this._btnRes = base.transform.Find("btnStop06").GetComponent<Button>();
					this._btnRes.onClick.AddListener(delegate()
					{
						GUITransit.OnButtonPressed.Invoke(connection);
					});
					AudioManager.AddBtnAudio(this._btnRes.gameObject, "ShipUIBtnReactorCoilFwdIn", "ShipUIBtnReactorCoilFwdOut");
				}
				else if (connection.strTargetRegID == "OKLG" && this._btnOKLG == null)
				{
					this._btnOKLG = base.transform.Find("btnStop07").GetComponent<Button>();
					this._btnOKLG.onClick.AddListener(delegate()
					{
						GUITransit.OnButtonPressed.Invoke(connection);
					});
					AudioManager.AddBtnAudio(this._btnOKLG.gameObject, "ShipUIBtnReactorCoilFwdIn", "ShipUIBtnReactorCoilFwdOut");
				}
				else if (connection.strTargetRegID == "OKLG_MES" && this._btnMesca == null)
				{
					this._btnMesca = base.transform.Find("btnStop08").GetComponent<Button>();
					this._btnMesca.onClick.AddListener(delegate()
					{
						GUITransit.OnButtonPressed.Invoke(connection);
					});
					AudioManager.AddBtnAudio(this._btnMesca.gameObject, "ShipUIBtnReactorCoilFwdIn", "ShipUIBtnReactorCoilFwdOut");
				}
			}
		}

		private void OnUserPressedButton(JsonTransitConnection jTransit)
		{
			if (jTransit == null)
			{
				return;
			}
			if (jTransit.strTargetRegID == "OKLG_BIZ")
			{
				this.lamp04.State = 3;
				this.lamp06.State = 0;
				this.lamp07.State = 0;
				this.lamp08.State = 0;
			}
			else if (jTransit.strTargetRegID == "OKLG_RES")
			{
				this.lamp04.State = 0;
				this.lamp06.State = 3;
				this.lamp07.State = 0;
				this.lamp08.State = 0;
			}
			else if (jTransit.strTargetRegID == "OKLG")
			{
				this.lamp04.State = 0;
				this.lamp06.State = 0;
				this.lamp07.State = 3;
				this.lamp08.State = 0;
			}
			else if (jTransit.strTargetRegID == "OKLG_MES")
			{
				this.lamp04.State = 0;
				this.lamp06.State = 0;
				this.lamp07.State = 0;
				this.lamp08.State = 3;
			}
		}

		private TMP_Text lblTitle;

		private GUILamp lamp04;

		private GUILamp lamp06;

		private GUILamp lamp07;

		private GUILamp lamp08;

		private CanvasGroup cgBoarding;

		private Button _btnOKLG;

		private Button _btnBiz;

		private Button _btnMesca;

		private Button _btnRes;
	}
}
