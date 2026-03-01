using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Ostranauts.ShipGUIs.NavStation
{
	public class NavModDiagnostics : NavModBase
	{
		protected override void Awake()
		{
			base.Awake();
			this.chkDiag.onValueChanged.AddListener(new UnityAction<bool>(this.ToggleStatusUI));
			AudioManager.AddBtnAudio(this.chkDiag.gameObject, "ShipUIBtnNSMinusIn", "ShipUIBtnNSMinusOut");
			this.chkLogs.onValueChanged.AddListener(new UnityAction<bool>(this.ToggleLogsUI));
			AudioManager.AddBtnAudio(this.chkLogs.gameObject, "ShipUIBtnNSMinusIn", "ShipUIBtnNSMinusOut");
			this.cgStatus = GUIRenderTargets.goLines.transform.Find("pnlStatus").GetComponent<CanvasGroup>();
			CanvasManager.HideCanvasGroup(this.cgStatus);
			this.txtStatusTitle = this.cgStatus.transform.Find("txtTitle").GetComponent<TMP_Text>();
			this.txtStatusNames = this.cgStatus.transform.Find("txtNames").GetComponent<TMP_Text>();
			this.txtStatusValues = this.cgStatus.transform.Find("txtValues").GetComponent<TMP_Text>();
			this.txtStatusLog = this.cgStatus.transform.Find("pnlLog/Viewport/txt").GetComponent<TMP_Text>();
		}

		protected override void OnNavModMessage(NavModMessageType messageType, object arg)
		{
			if (messageType == NavModMessageType.UpdateUI)
			{
				if (this.COSelf.HasCond("IsDamagedSoftware"))
				{
					this.PrintErrors();
				}
			}
		}

		private void ToggleStatusUI(bool isOn)
		{
			if (isOn)
			{
				CanvasManager.ShowCanvasGroup(this.cgStatus);
				base.StartCoroutine(this.PrintStatus());
				return;
			}
			CanvasManager.HideCanvasGroup(this.cgStatus);
		}

		private IEnumerator PrintStatus()
		{
			if (this.bDiagStarted)
			{
				yield break;
			}
			this.bLogsStarted = false;
			this.bDiagStarted = true;
			this.txtStatusTitle.text = DataHandler.GetString("GUI_ORBIT_STATUS_REPORT_TITLE", false);
			this.txtStatusNames.text = string.Empty;
			this.txtStatusValues.text = string.Empty;
			this.txtStatusLog.text = string.Empty;
			string[] aValues = new string[ShipStatus.aNames.Length];
			ShipStatus.PrintStatus(this.COSelf, ref aValues);
			for (int j = 0; j < aValues.Length; j++)
			{
				TMP_Text tmp_Text = this.txtStatusNames;
				tmp_Text.text = tmp_Text.text + ShipStatus.aNames[j] + "\n";
			}
			yield return null;
			for (int i = 0; i < aValues.Length; i++)
			{
				if (!this.bDiagStarted)
				{
					break;
				}
				TMP_Text tmp_Text2 = this.txtStatusValues;
				tmp_Text2.text = tmp_Text2.text + aValues[i] + "\n";
				AudioManager.am.PlayAudioEmitter("ShipUINSMapPan02", false, false);
				yield return new WaitForSeconds(MathUtils.Rand(0.24f, 1.65f, MathUtils.RandType.Flat, null));
			}
			this.bDiagStarted = false;
			yield return null;
			yield break;
		}

		private void ToggleLogsUI(bool isOn)
		{
			if (isOn)
			{
				CanvasManager.ShowCanvasGroup(this.cgStatus);
				base.StartCoroutine(this.PrintLogs());
				return;
			}
			CanvasManager.HideCanvasGroup(this.cgStatus);
		}

		private IEnumerator PrintLogs()
		{
			if (this.bLogsStarted)
			{
				yield break;
			}
			this.bDiagStarted = false;
			this.bLogsStarted = true;
			this.txtStatusTitle.text = DataHandler.GetString("GUI_ORBIT_STATUS_LOG_TITLE", false);
			this.txtStatusNames.text = string.Empty;
			this.txtStatusValues.text = string.Empty;
			this.txtStatusLog.text = string.Empty;
			if (this.dictPropMap.ContainsKey("strDiagLog"))
			{
				this.dictPropMap.Remove("strDiagLog");
			}
			List<JsonShipLog> aValues = new List<JsonShipLog>();
			aValues.AddRange(this.COSelf.ship.LogGet());
			yield return null;
			aValues.Sort((JsonShipLog itm1, JsonShipLog itm2) => itm2.fEpoch.CompareTo(itm1.fEpoch));
			aValues.InsertRange(0, this.COSelf.ship.LogGetHeader());
			yield return null;
			for (int i = 0; i < aValues.Count; i++)
			{
				if (!this.bLogsStarted)
				{
					break;
				}
				if (aValues[i].bShowEpoch)
				{
					TMP_Text tmp_Text = this.txtStatusLog;
					string text = tmp_Text.text;
					tmp_Text.text = string.Concat(new string[]
					{
						text,
						MathUtils.GetUTCFromS(aValues[i].fEpoch),
						": ",
						aValues[i].strEntry,
						"\n"
					});
				}
				else
				{
					TMP_Text tmp_Text2 = this.txtStatusLog;
					tmp_Text2.text = tmp_Text2.text + aValues[i].strEntry + "\n";
				}
				AudioManager.am.PlayAudioEmitter("ShipUINSMapPan02", false, false);
				yield return new WaitForSeconds(0.24f);
			}
			this.bLogsStarted = false;
			yield return null;
			yield break;
		}

		private void PrintErrors()
		{
			if (CrewSim.TimeElapsedScaled() == 0f)
			{
				return;
			}
			if (this.cgStatus.alpha == 0f)
			{
				CanvasManager.ShowCanvasGroup(this.cgStatus);
				this.txtStatusTitle.text = string.Empty;
				this.txtStatusNames.text = string.Empty;
				this.txtStatusValues.text = string.Empty;
				this.txtStatusLog.text = string.Empty;
			}
			string @string = DataHandler.GetString("GUI_ORBIT_ERROR", false);
			TMP_Text tmp_Text = this.txtStatusLog;
			tmp_Text.text += @string;
		}

		[SerializeField]
		private Toggle chkDiag;

		[SerializeField]
		private Toggle chkLogs;

		private CanvasGroup cgStatus;

		private TMP_Text txtStatusTitle;

		private TMP_Text txtStatusNames;

		private TMP_Text txtStatusValues;

		private TMP_Text txtStatusLog;

		private bool bLogsStarted;

		private bool bDiagStarted;
	}
}
