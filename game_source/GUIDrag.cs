using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class GUIDrag : MonoBehaviour, IPointerDownHandler, IDragHandler, IEventSystemHandler
{
	private void Awake()
	{
		if (this.bDragParent)
		{
			this.tfDragging = (base.transform.parent as RectTransform);
		}
		else
		{
			this.tfDragging = (base.transform as RectTransform);
		}
		this.tfDraggingParent = (this.tfDragging.parent as RectTransform);
	}

	public void OnPointerDown(PointerEventData data)
	{
		this.ptPositionLocalOld = this.tfDragging.localPosition;
		RectTransformUtility.ScreenPointToLocalPointInRectangle(this.tfDraggingParent, data.position, data.pressEventCamera, out this.ptMouseParentOld);
	}

	public void OnDrag(PointerEventData data)
	{
		if (this.tfDragging == null || this.tfDraggingParent == null)
		{
			return;
		}
		Vector2 a;
		if (RectTransformUtility.ScreenPointToLocalPointInRectangle(this.tfDraggingParent, data.position, data.pressEventCamera, out a))
		{
			Vector3 b = a - this.ptMouseParentOld;
			this.tfDragging.localPosition = this.ptPositionLocalOld + b;
		}
		this.ClampToWindow();
	}

	private void ClampToWindow()
	{
		Vector3 localPosition = this.tfDragging.localPosition;
		Vector3 vector = this.tfDraggingParent.rect.min - this.tfDragging.rect.min;
		Vector3 vector2 = this.tfDraggingParent.rect.max - this.tfDragging.rect.max;
		localPosition.x = Mathf.Clamp(this.tfDragging.localPosition.x, vector.x, vector2.x);
		localPosition.y = Mathf.Clamp(this.tfDragging.localPosition.y, vector.y, vector2.y);
		this.tfDragging.localPosition = localPosition;
	}

	private Vector2 ptMouseParentOld;

	private Vector3 ptPositionLocalOld;

	private RectTransform tfDragging;

	private RectTransform tfDraggingParent;

	public bool bDragParent = true;
}
