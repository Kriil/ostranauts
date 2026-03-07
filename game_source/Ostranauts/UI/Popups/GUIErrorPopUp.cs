using System;
using System.Reflection;
using Ostranauts.Core;
using Ostranauts.Events;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Ostranauts.UI.Popups
{
	public class GUIErrorPopUp : MonoSingleton<GUIErrorPopUp>
	{
		private void Start()
		{
			if (LoadManager.OnSavingFailed == null)
			{
				LoadManager.OnSavingFailed = new SavingFailedEvent();
			}
			LoadManager.OnSavingFailed.AddListener(new UnityAction<Exception>(this.OnSavingFailed));
			this.btnConfirm.onClick.AddListener(new UnityAction(this.CloseTooltip));
			this.cg.ignoreParentGroups = true;
			this.CloseTooltip();
		}

		private void OnDestroy()
		{
			LoadManager.OnSavingFailed.RemoveListener(new UnityAction<Exception>(this.OnSavingFailed));
		}

		public void ShowTooltip(string strTitle, string strBody)
		{
			this.txtTitle.text = strTitle;
			this.txtBody.text = strBody;
			CanvasManager.ShowCanvasGroup(this.cg);
			CrewSim.Paused = true;
		}

		public void CloseTooltip()
		{
			CanvasManager.HideCanvasGroup(this.cg);
		}

		private void OnSavingFailed(Exception e)
		{
			string text = "Error Saving Game";
			string strBody = "No save file was created";
			try
			{
				int num = 0;
				PropertyInfo property = typeof(Exception).GetProperty("HResult", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
				if (property != null)
				{
					object value = property.GetValue(e, null);
					num = (Convert.ToInt32(value) & 65535);
				}
				string text2 = DataHandler.GetString("ERROR_CODE_" + num, true);
				if (string.IsNullOrEmpty(text2))
				{
					text2 = e.Message;
				}
				strBody = text2;
				Debug.LogWarning(text + " " + text2);
			}
			catch (Exception ex)
			{
				Debug.LogWarning("Can't display exception: " + ex.Message);
			}
			this.ShowTooltip(text, strBody);
		}

		[SerializeField]
		private TMP_Text txtTitle;

		[SerializeField]
		private TMP_Text txtBody;

		[SerializeField]
		private CanvasGroup cg;

		[SerializeField]
		private Button btnConfirm;
	}
}
