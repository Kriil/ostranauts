using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Ostranauts.ShipGUIs.Market.GUICargoPod
{
	public class GUICargoPodEject : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IEventSystemHandler
	{
		private void Awake()
		{
			this.cgWarningLight.alpha = 0f;
		}

		public void SetData(Action ejectCallback)
		{
			this._ejectCallback = ejectCallback;
		}

		private void Update()
		{
			if (this._pointerDownTimeStamp == 0f)
			{
				return;
			}
			float num = Time.unscaledTime - this._pointerDownTimeStamp;
			if (num < 3f)
			{
				this.cgWarningLight.alpha = Mathf.PingPong(Time.unscaledTime * 1.3f, 1f);
				AudioManager.am.PlayAudioEmitter("ShipTrackAlarm", false, true);
			}
			else if (num < 5f)
			{
				this.cgWarningLight.alpha = 1f;
				AudioManager.am.PlayAudioEmitter("ShipUIBtnDCNoClearance", false, true);
			}
			else
			{
				this.ResetButton();
				this.Eject();
			}
		}

		private void Eject()
		{
			if (this._ejectCallback == null)
			{
				return;
			}
			this._ejectCallback();
			AudioManager.am.PlayAudioEmitter("ShipDockUnclamp", false, true);
			this._ejectCallback = null;
		}

		public void OnPointerDown(PointerEventData eventData)
		{
			if (this._ejectCallback == null)
			{
				return;
			}
			this._pointerDownTimeStamp = Time.unscaledTime;
		}

		private void ResetButton()
		{
			this._pointerDownTimeStamp = 0f;
			this.cgWarningLight.alpha = 0f;
			AudioManager.am.StopAudioEmitter("ShipTrackAlarm");
			AudioManager.am.StopAudioEmitter("ShipUIBtnDCNoClearance");
		}

		public void OnPointerUp(PointerEventData eventData)
		{
			this.ResetButton();
		}

		[SerializeField]
		private CanvasGroup cgWarningLight;

		private float _pointerDownTimeStamp;

		private Action _ejectCallback;
	}
}
