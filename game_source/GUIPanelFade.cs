using System;
using System.Collections;
using UnityEngine;

public class GUIPanelFade : MonoBehaviour
{
	private void Awake()
	{
	}

	private void Update()
	{
		this.fDelay -= Time.deltaTime;
		if (this.fDelay <= 0f)
		{
			if (this.bAutoFadeIn)
			{
				base.StartCoroutine(this.FadeTextToFullAlpha(this.fDuration, base.GetComponent<CanvasGroup>()));
			}
			else if (this.bAutoFadeOut)
			{
				base.StartCoroutine(this.FadeTextToZeroAlpha(this.fDuration, base.GetComponent<CanvasGroup>()));
			}
			this.bAutoFadeIn = false;
			this.bAutoFadeOut = false;
		}
	}

	public void Reset(float fDur, float fDel, bool bFadeIn, bool bFadeOut)
	{
		this.bAutoFadeIn = bFadeIn;
		this.bAutoFadeOut = bFadeOut;
		this.fDuration = fDur;
		this.fDelay = fDel;
	}

	public IEnumerator FadeTextToFullAlpha(float t, CanvasGroup cg)
	{
		while (cg.alpha < 1f)
		{
			cg.alpha += Time.deltaTime / t;
			yield return null;
		}
		yield break;
	}

	public IEnumerator FadeTextToZeroAlpha(float t, CanvasGroup cg)
	{
		while (cg.alpha > 0f)
		{
			cg.alpha -= Time.deltaTime / t;
			yield return null;
		}
		yield break;
	}

	public bool bAutoFadeIn;

	public bool bAutoFadeOut;

	public float fDuration = 1f;

	public float fDelay;
}
