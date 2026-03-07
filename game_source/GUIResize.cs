using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class GUIResize : MonoBehaviour, IPointerDownHandler, IDragHandler, IEventSystemHandler
{
	private void Awake()
	{
		this.panelRectTransform = base.transform.parent.GetComponent<RectTransform>();
	}

	public void OnPointerDown(PointerEventData data)
	{
		this.originalSizeDelta = this.panelRectTransform.sizeDelta;
		RectTransformUtility.ScreenPointToLocalPointInRectangle(this.panelRectTransform, data.position, data.pressEventCamera, out this.originalLocalPointerPosition);
	}

	public void OnDrag(PointerEventData data)
	{
		if (this.panelRectTransform == null)
		{
			return;
		}
		Vector2 a;
		RectTransformUtility.ScreenPointToLocalPointInRectangle(this.panelRectTransform, data.position, data.pressEventCamera, out a);
		Vector3 vector = a - this.originalLocalPointerPosition;
		Vector2 sizeDelta = this.originalSizeDelta + new Vector2(vector.x, -vector.y);
		sizeDelta = new Vector2(Mathf.Clamp(sizeDelta.x, this.minSize.x, this.maxSize.x), Mathf.Clamp(sizeDelta.y, this.minSize.y, this.maxSize.y));
		this.panelRectTransform.sizeDelta = sizeDelta;
	}

	public Vector2 minSize = new Vector2(200f, 125f);

	public Vector2 maxSize = new Vector2(800f, 600f);

	private RectTransform panelRectTransform;

	private Vector2 originalLocalPointerPosition;

	private Vector2 originalSizeDelta;
}
