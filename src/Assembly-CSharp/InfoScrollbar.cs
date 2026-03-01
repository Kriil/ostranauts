using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InfoScrollbar : MonoBehaviour, IPointerEnterHandler, IPointerDownHandler, IPointerExitHandler, IEventSystemHandler
{
	public RectTransform rect
	{
		get
		{
			if (!this._rect)
			{
				this._rect = (base.transform as RectTransform);
			}
			return this._rect;
		}
	}

	private void Awake()
	{
		this.start = this.ScrollBarImage.color;
		this.text = this.scrollChildContent.GetComponent<TextMeshProUGUI>();
	}

	public bool ContentSmallerThanViewport()
	{
		float y = this.MaskParent.rectTransform.rect.size.y;
		return Info.instance.lastBodyTextPreferredHeight < y - 10f;
	}

	public void ResetScrollBarAfterTextDraw()
	{
		float y = this.MaskParent.rectTransform.rect.size.y;
		float num = Mathf.Lerp(100f, 50f, Info.instance.lastBodyTextPreferredHeight / (y * 3f));
		this.scrollBarMaxYPos = y / 2f - num / 2f;
		this.scrollBarMinYPos = -this.scrollBarMaxYPos;
		this.ScrollBarImage.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, num);
		this.ScrollBarImage.rectTransform.localPosition = new Vector3(this.ScrollBarImage.rectTransform.localPosition.x, this.scrollBarMaxYPos);
	}

	public void RememberCurrentScrollVal()
	{
		this.oldScrollValBeforeResize = this.scrollVal;
	}

	public void AfterResize()
	{
		if (this.ContentSmallerThanViewport())
		{
			this.HideScrollBar();
			this.scrollChildContent.offsetMax = Vector2.zero;
			this.scrollChildContent.offsetMin = Vector2.zero;
			this.scrollVal = 0f;
			this.oldScrollValBeforeResize = 0f;
			return;
		}
		float y = this.MaskParent.rectTransform.rect.size.y;
		float x = this.MaskParent.rectTransform.rect.size.x;
		float num = Mathf.Lerp(y / 2.5f, 50f, Info.instance.lastBodyTextPreferredHeight / (y * 3f));
		this.scrollBarMaxYPos = y / 2f - num / 2f;
		this.scrollBarMinYPos = -this.scrollBarMaxYPos;
		this.ScrollBarImage.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, num);
		this.scrollChildContent.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, x - this.ScrollBarImage.rectTransform.rect.width);
		if (this.text)
		{
			this.text.ForceMeshUpdate();
			Info.instance.lastBodyTextPreferredHeight = this.text.preferredHeight;
			this.scrollChildContent.offsetMax = new Vector2(-this.ScrollBarImage.rectTransform.rect.width, this.scrollVal * (Info.instance.lastBodyTextPreferredHeight - this.MaskParent.rectTransform.rect.height));
			this.scrollChildContent.offsetMin = new Vector2(0f, (1f - this.scrollVal) * -(Info.instance.lastBodyTextPreferredHeight - this.MaskParent.rectTransform.rect.height));
			this.scrollContentMaxY = this.scrollChildContent.rect.size.y / 2f - y / 2f;
			this.scrollContentMinY = -this.scrollChildContent.rect.size.y / 2f + y / 2f;
			this.ScrollBarImage.rectTransform.localPosition = new Vector3(this.ScrollBarImage.rectTransform.localPosition.x, (this.oldScrollValBeforeResize - 0.5f) * this.scrollContentMaxY * 2f);
			this.UnhideScrollBar();
		}
	}

	public void AfterNewTextDraw()
	{
		if (this.ContentSmallerThanViewport())
		{
			this.HideScrollBar();
			this.scrollChildContent.offsetMax = Vector2.zero;
			this.scrollChildContent.offsetMin = Vector2.zero;
			if (this.text)
			{
				Info.instance.lastBodyTextPreferredHeight = this.text.preferredHeight;
				this.scrollVal = 0f;
				this.oldScrollValBeforeResize = 0f;
			}
			return;
		}
		this.scrollVal = 0f;
		this.oldScrollValBeforeResize = 0f;
		this.ResetScrollBarAfterTextDraw();
		this.scrollChildContent.offsetMax = new Vector2(-this.ScrollBarImage.rectTransform.rect.width, 0f);
		this.scrollChildContent.offsetMin = new Vector2(0f, 0f);
		this.scrollContentMaxY = Info.instance.lastBodyTextPreferredHeight - this.MaskParent.rectTransform.rect.height;
		this.scrollContentMinY = -(Info.instance.lastBodyTextPreferredHeight - this.MaskParent.rectTransform.rect.height);
		this.scrollChildContent.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, this.MaskParent.rectTransform.rect.size.x - this.ScrollBarImage.rectTransform.rect.width);
		if (this.text)
		{
			this.text.ForceMeshUpdate();
			Info.instance.lastBodyTextPreferredHeight = this.text.preferredHeight;
		}
		this.UnhideScrollBar();
		this.PositionBar(0f);
	}

	public void HideScrollBar()
	{
		if (this.ScrollBarImage.enabled)
		{
			this.ScrollBarImage.raycastTarget = false;
			this.ScrollBarImage.enabled = false;
		}
	}

	private void UnhideScrollBar()
	{
		if (!this.ScrollBarImage.enabled)
		{
			this.ScrollBarImage.raycastTarget = true;
			this.ScrollBarImage.enabled = true;
		}
	}

	public void OnPointerDown(PointerEventData eventData)
	{
		this.ScrollBarImage.color = this.mouseHold;
		this.holdingMouse = true;
		this.lastMouseY = Input.mousePosition.y;
	}

	public void OnPointerEnter(PointerEventData eventData)
	{
		if (!this.holdingMouse)
		{
			this.ScrollBarImage.color = this.mouseOver;
		}
	}

	public void OnPointerExit(PointerEventData eventData)
	{
		if (!this.holdingMouse)
		{
			this.ScrollBarImage.color = this.start;
		}
	}

	public void PositionBar(float val)
	{
		if (!this.holdingMouse)
		{
			this.scrollChildContent.offsetMax = new Vector2(-this.ScrollBarImage.rectTransform.rect.width, val * (Info.instance.lastBodyTextPreferredHeight - this.MaskParent.rectTransform.rect.height));
			this.scrollChildContent.offsetMin = new Vector2(0f, (1f - val) * -(Info.instance.lastBodyTextPreferredHeight - this.MaskParent.rectTransform.rect.height));
			this.ScrollBarImage.rectTransform.localPosition = new Vector3(this.ScrollBarImage.rectTransform.localPosition.x, Mathf.Lerp(this.scrollBarMaxYPos, this.scrollBarMinYPos, val));
			if (this.ContentSmallerThanViewport())
			{
				RectTransform rectTransform = this.scrollChildContent;
				Vector2 zero = Vector2.zero;
				this.scrollChildContent.offsetMin = zero;
				rectTransform.offsetMax = zero;
			}
		}
	}

	private void Update()
	{
		if (this.holdingMouse)
		{
			this.ScrollBarImage.rectTransform.localPosition += new Vector3(0f, Input.mousePosition.y - this.lastMouseY);
			if (this.ScrollBarImage.rectTransform.localPosition.y >= this.scrollBarMaxYPos)
			{
				this.ScrollBarImage.rectTransform.localPosition = new Vector3(this.ScrollBarImage.rectTransform.localPosition.x, this.scrollBarMaxYPos);
			}
			else if (this.ScrollBarImage.rectTransform.localPosition.y <= this.scrollBarMinYPos)
			{
				this.ScrollBarImage.rectTransform.localPosition = new Vector3(this.ScrollBarImage.rectTransform.localPosition.x, this.scrollBarMinYPos);
			}
			else
			{
				this.lastMouseY = Input.mousePosition.y;
			}
			this.scrollVal = (this.ScrollBarImage.rectTransform.localPosition.y / (this.scrollBarMaxYPos * 2f) - 0.5f) * -1f;
			this.scrollVal = Mathf.Clamp01(this.scrollVal);
			this.scrollChildContent.offsetMax = new Vector2(-this.ScrollBarImage.rectTransform.sizeDelta.x, this.scrollVal * (Info.instance.lastBodyTextPreferredHeight - this.MaskParent.rectTransform.rect.height));
			this.scrollChildContent.offsetMin = new Vector2(0f, (1f - this.scrollVal) * -(Info.instance.lastBodyTextPreferredHeight - this.MaskParent.rectTransform.rect.height));
			if (Input.GetMouseButtonUp(0))
			{
				this.holdingMouse = false;
				if (RectTransformUtility.RectangleContainsScreenPoint(this.rect, Input.mousePosition, this.ScrollBarImage.canvas.worldCamera))
				{
					this.ScrollBarImage.color = this.mouseOver;
				}
				else
				{
					this.ScrollBarImage.color = this.start;
				}
			}
		}
	}

	public RectMask2D MaskParent;

	public Canvas parent;

	public Image ScrollBarImage;

	public Color start;

	public Color mouseOver;

	public Color mouseHold;

	public bool holdingMouse;

	private RectTransform _rect;

	public float scrollContentMaxY;

	public float scrollContentMinY;

	public float scrollBarMaxYPos;

	public float scrollBarMinYPos;

	public float lastMouseY;

	public float oldScrollValBeforeResize;

	public float scrollVal;

	public RectTransform scrollChildContent;

	[HideInInspector]
	public TextMeshProUGUI text;
}
