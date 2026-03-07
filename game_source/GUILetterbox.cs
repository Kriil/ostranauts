using System;
using System.Collections;
using UnityEngine;

public class GUILetterbox : MonoBehaviour
{
	private void Awake()
	{
		this.rect = base.GetComponent<RectTransform>();
		this.parentCanvas = this.rect.parent.GetComponent<RectTransform>();
		this.cg = base.GetComponent<CanvasGroup>();
		this.bTrueIfBottom = true;
		if (this.rect.anchorMax.y == 1f)
		{
			this.bTrueIfBottom = false;
			GUILetterbox.letterboxTop = base.gameObject;
		}
		else
		{
			GUILetterbox.letterboxBottom = base.gameObject;
		}
	}

	private void Start()
	{
		if (this.bTrueIfBottom)
		{
			CrewSim.objInstance.LetterboxBottom = this;
		}
		else
		{
			CrewSim.objInstance.LetterboxTop = this;
		}
		if (!CanvasManager.instance.canvasStack.Contains(base.transform.parent.gameObject))
		{
			CanvasManager.instance.goCanvasLetterboxes = base.transform.parent.gameObject;
			CanvasManager.instance.canvasStack.Add(GUILetterbox.letterboxBottom.transform.parent.gameObject);
			base.transform.parent.SetParent(CanvasManager.instance.canvasStackHolder.transform);
			CanvasManager.instance.StartCoroutine("FadeUpAllCanvases", 0.1f);
		}
	}

	public void AnimateIn()
	{
		base.StopAllCoroutines();
		base.StartCoroutine("AnimateLetterboxIn", 2f);
	}

	public void AnimateOut()
	{
		base.StopAllCoroutines();
		base.StartCoroutine("AnimateLetterboxOut", 2f);
	}

	public IEnumerator AnimateLetterboxIn(float duration)
	{
		bool bTrueIfBottomLetterbox = true;
		float goal = 0.2f;
		float currentAnchor = this.rect.anchorMax.y;
		float halfwayPoint = (goal - currentAnchor) / 2f + currentAnchor;
		if (this.rect.anchorMax.y == 1f)
		{
			bTrueIfBottomLetterbox = false;
			currentAnchor = this.rect.anchorMin.y;
			goal = 0.8f;
			halfwayPoint = (currentAnchor - goal) / 2f + goal;
		}
		float timePassed = 0f;
		if (bTrueIfBottomLetterbox)
		{
			while (timePassed < duration)
			{
				timePassed += Time.deltaTime;
				float blend = Mathf.Clamp01(timePassed / duration);
				this.cg.alpha = Mathf.Lerp(0f, 3f, blend);
				this.rect.anchorMax = new Vector2(this.rect.anchorMax.x, Mathf.Lerp(this.rect.anchorMax.y, goal, 0.05f));
				this.SetCorners();
				yield return null;
			}
			this.rect.anchorMax = new Vector2(this.rect.anchorMax.x, goal);
			this.SetCorners();
			timePassed = 0f;
		}
		else
		{
			while (timePassed < duration)
			{
				timePassed += Time.deltaTime;
				float blend2 = Mathf.Clamp01(timePassed / duration);
				this.cg.alpha = Mathf.Lerp(0f, 3f, blend2);
				this.rect.anchorMin = new Vector2(this.rect.anchorMin.x, Mathf.Lerp(this.rect.anchorMin.y, goal, 0.05f));
				this.SetCorners();
				yield return null;
			}
			this.rect.anchorMin = new Vector2(this.rect.anchorMin.x, goal);
			this.SetCorners();
			timePassed = 0f;
		}
		yield return null;
		yield break;
	}

	public IEnumerator AnimateLetterboxOut(float duration)
	{
		bool bTrueIfBottomLetterbox = true;
		float goal = 0.02f;
		float currentAnchor = this.rect.anchorMax.y;
		if (this.rect.anchorMax.y == 1f)
		{
			bTrueIfBottomLetterbox = false;
			currentAnchor = this.rect.anchorMin.y;
			goal = 0.98f;
		}
		float timePassed = 0f;
		if (bTrueIfBottomLetterbox)
		{
			while (timePassed < duration)
			{
				timePassed += Time.deltaTime;
				float blend = Mathf.Clamp01(timePassed / duration);
				this.cg.alpha = Mathf.Lerp(1f, -2f, blend);
				this.rect.anchorMax = new Vector2(this.rect.anchorMax.x, Mathf.Lerp(this.rect.anchorMax.y, goal, 0.05f));
				this.SetCorners();
				yield return null;
			}
			this.rect.anchorMax = new Vector2(this.rect.anchorMax.x, goal);
			this.SetCorners();
		}
		else
		{
			while (timePassed < duration)
			{
				timePassed += Time.deltaTime;
				float blend2 = Mathf.Clamp01(timePassed / duration);
				this.cg.alpha = Mathf.Lerp(1f, -2f, blend2);
				this.rect.anchorMin = new Vector2(this.rect.anchorMin.x, Mathf.Lerp(this.rect.anchorMin.y, goal, 0.05f));
				this.SetCorners();
				yield return null;
			}
			this.rect.anchorMin = new Vector2(this.rect.anchorMin.x, goal);
			this.SetCorners();
		}
		yield return null;
		yield break;
	}

	public void SetCorners()
	{
		RectTransform rectTransform = this.rect;
		Vector2 vector = new Vector2(0f, 0f);
		this.rect.offsetMax = vector;
		rectTransform.offsetMin = vector;
	}

	public void SetAnchors()
	{
		RectTransform rectTransform = this.rect;
		RectTransform rectTransform2 = this.parentCanvas;
		if (rectTransform == null || rectTransform2 == null)
		{
			return;
		}
		Vector2 anchorMin = new Vector2(rectTransform.anchorMin.x + rectTransform.offsetMin.x / rectTransform2.rect.width, rectTransform.anchorMin.y + rectTransform.offsetMin.y / rectTransform2.rect.height);
		Vector2 anchorMax = new Vector2(rectTransform.anchorMax.x + rectTransform.offsetMax.x / rectTransform2.rect.width, rectTransform.anchorMax.y + rectTransform.offsetMax.y / rectTransform2.rect.height);
		rectTransform.anchorMin = anchorMin;
		rectTransform.anchorMax = anchorMax;
		RectTransform rectTransform3 = rectTransform;
		Vector2 vector = new Vector2(0f, 0f);
		rectTransform.offsetMax = vector;
		rectTransform3.offsetMin = vector;
	}

	public RectTransform rect;

	private RectTransform parentCanvas;

	private CanvasGroup cg;

	public bool bTrueIfBottom;

	public static GameObject letterboxTop;

	public static GameObject letterboxBottom;
}
