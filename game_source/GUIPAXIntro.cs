using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(CanvasGroup))]
public class GUIPAXIntro : MonoBehaviour, IPointerClickHandler, IEventSystemHandler
{
	private void Awake()
	{
		this.cg = base.GetComponent<CanvasGroup>();
		Transform transform = base.transform.Find("pnlFadeOut");
		if (transform != null)
		{
			this.pnlFadeOut = transform.GetComponent<CanvasGroup>();
		}
	}

	private void Update()
	{
		if (!this.bActive)
		{
			return;
		}
		if (Input.anyKeyDown)
		{
			this.Dismiss();
		}
	}

	public void OnPointerClick(PointerEventData eventData)
	{
		if (!this.bActive)
		{
			return;
		}
		this.Dismiss();
	}

	public void Show(Action callback)
	{
		this._callback = callback;
		this.bActive = true;
		this.cg.alpha = 1f;
		this.cg.interactable = true;
		this.cg.blocksRaycasts = true;
		if (this.pnlFadeOut != null)
		{
			this.pnlFadeOut.alpha = 0f;
		}
	}

	private void Hide()
	{
		this.bActive = false;
		this.cg.alpha = 0f;
		this.cg.interactable = false;
		this.cg.blocksRaycasts = false;
	}

	private void Dismiss()
	{
		base.StartCoroutine(this.FadeOut(0.25f));
		this.bActive = false;
	}

	private IEnumerator FadeOut(float t)
	{
		if (this.pnlFadeOut != null)
		{
			this.pnlFadeOut.GetComponent<GUIPanelFade>().Reset(t, 0f, true, false);
		}
		float fTimer = 0f;
		while (fTimer < t)
		{
			fTimer += Time.deltaTime;
			yield return null;
		}
		this.bActive = false;
		this.Hide();
		if (this._callback != null)
		{
			this._callback();
			this._callback = null;
		}
		yield break;
	}

	private CanvasGroup pnlFadeOut;

	private CanvasGroup cg;

	private bool bActive;

	private Action _callback;
}
