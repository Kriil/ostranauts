using System;
using System.Collections;
using Ostranauts.Trading;
using TMPro;
using UnityEngine;

namespace Ostranauts.ShipGUIs.Market.GUICargoPod
{
	public class GUITransferPanel : MonoBehaviour
	{
		private void Awake()
		{
			this.cgWarning.gameObject.SetActive(false);
			this.cgWarning.alpha = 1f;
			this.pnlLoading.alpha = 1f;
		}

		private void OnDestroy()
		{
			if (this._switchPanel != null)
			{
				base.StopCoroutine(this._switchPanel);
			}
		}

		public void UpdateTransferWindow(MarketActorConfig configPod, MarketActorConfig transferTargetPodConfig)
		{
			if (this._switchPanel != null)
			{
				base.StopCoroutine(this._switchPanel);
			}
			this.ResetWarningBlinker();
			this.cgWarning.gameObject.SetActive(transferTargetPodConfig != null && !transferTargetPodConfig.CanTakeCargoFrom(configPod.GetCargoPodData()));
			if (configPod.IsEmpty)
			{
				this.SetToEmpty();
				return;
			}
			this._switchPanel = base.StartCoroutine(this.SwitchPanels(transferTargetPodConfig));
		}

		private void SetToEmpty()
		{
			this.pnlLoading.alpha = 1f;
			this.txtloading.text = "No cargo to transfer";
		}

		private void Update()
		{
			if (this._blinkWarningTimeStamp <= 0f)
			{
				return;
			}
			float num = Time.unscaledTime - this._blinkWarningTimeStamp;
			if (num < 3f)
			{
				this.cgWarning.alpha = Mathf.PingPong(Time.unscaledTime * 1.3f, 1f);
			}
			else
			{
				this.ResetWarningBlinker();
			}
		}

		public void HighlightNotCompatible()
		{
			if (this.cgWarning.gameObject.activeSelf)
			{
				this._blinkWarningTimeStamp = Time.unscaledTime;
			}
		}

		private void ResetWarningBlinker()
		{
			this.cgWarning.alpha = 1f;
			this._blinkWarningTimeStamp = 0f;
		}

		private IEnumerator SwitchPanels(MarketActorConfig mconf)
		{
			this.pnlLoading.alpha = 1f;
			this.txtloading.text = "Fetching cargopod data";
			this._transferStatusPanel.SetData(mconf);
			yield return new WaitForSeconds(this._switchPanelDelay);
			if (mconf == null)
			{
				this.txtloading.text = "No valid cargopods found";
			}
			else
			{
				this.pnlLoading.alpha = 0f;
			}
			this._switchPanelDelay = 0.5f;
			this._switchPanel = null;
			yield break;
		}

		[SerializeField]
		private GUICargoPodStatusPanel _transferStatusPanel;

		[SerializeField]
		private CanvasGroup pnlLoading;

		[SerializeField]
		private TMP_Text txtloading;

		[SerializeField]
		private CanvasGroup cgWarning;

		private Coroutine _switchPanel;

		private float _switchPanelDelay;

		private float _blinkWarningTimeStamp;
	}
}
