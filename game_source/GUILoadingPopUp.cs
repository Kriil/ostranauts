using System;
using System.Collections;
using Ostranauts.Core;
using TMPro;
using UnityEngine;

public class GUILoadingPopUp : MonoSingleton<GUILoadingPopUp>
{
	private void Start()
	{
		LoadManager.OnAsyncSaveStarted.AddListener(delegate()
		{
			this.FadeOutToolTip(1.5f);
		});
		LoadManager.OnSaveFinished.AddListener(delegate()
		{
			this.FadeOutToolTip(1.5f);
		});
		LoadManager.OnSavingFailed.AddListener(delegate(Exception _)
		{
			this.FadeOutToolTip(1.5f);
		});
	}

	private void OnDestroy()
	{
		LoadManager.OnAsyncSaveStarted.RemoveListener(delegate()
		{
			this.FadeOutToolTip(1.5f);
		});
		LoadManager.OnSaveFinished.RemoveListener(delegate()
		{
			this.FadeOutToolTip(1.5f);
		});
		LoadManager.OnSavingFailed.RemoveListener(delegate(Exception _)
		{
			this.FadeOutToolTip(1.5f);
		});
	}

	private IEnumerator FadeToolTip(float wait)
	{
		float amt = wait;
		while (amt >= 0f)
		{
			amt -= Time.unscaledDeltaTime;
			this.cg.alpha = amt / wait;
			yield return null;
		}
		this.cg.alpha = 0f;
		yield return null;
		yield break;
	}

	public void ShowTooltip(string strTitle, string strBody)
	{
		this.txtTitle.text = strTitle;
		this.txtBody.text = strBody;
		this.cg.alpha = 1f;
	}

	public void FadeOutToolTip(float fadeOutDuration = 1.5f)
	{
		if (this.cg.alpha == 0f)
		{
			return;
		}
		base.StartCoroutine(this.FadeToolTip(fadeOutDuration));
	}

	[SerializeField]
	private TMP_Text txtTitle;

	[SerializeField]
	private TMP_Text txtBody;

	[SerializeField]
	private CanvasGroup cg;
}
