using System;
using System.Collections;
using Ostranauts.Core;
using UnityEngine;
using UnityEngine.Events;

public class GUIAutosavePopUp : MonoBehaviour
{
	private void Start()
	{
		LoadManager.OnAsyncSaveStarted.AddListener(new UnityAction(this.ShowTooltip));
		LoadManager.OnSaveFinished.AddListener(new UnityAction(this.FadeOutTooltip));
		LoadManager.OnSavingFailed.AddListener(delegate(Exception _)
		{
			this.FadeOutTooltip();
		});
	}

	private void OnDestroy()
	{
		LoadManager.OnAsyncSaveStarted.RemoveListener(new UnityAction(this.ShowTooltip));
		LoadManager.OnSaveFinished.RemoveListener(new UnityAction(this.FadeOutTooltip));
		LoadManager.OnSavingFailed.RemoveListener(delegate(Exception _)
		{
			this.FadeOutTooltip();
		});
	}

	private void FadeOutTooltip()
	{
		if (this.cg.alpha == 0f)
		{
			return;
		}
		base.StartCoroutine(this.FadeToolTip(1f));
	}

	private IEnumerator FadeToolTip(float wait = 1f)
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

	private void ShowTooltip()
	{
		this.cg.alpha = 1f;
	}

	[SerializeField]
	private CanvasGroup cg;
}
