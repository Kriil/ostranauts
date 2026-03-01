using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class GUIInventoryTab : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IEventSystemHandler
{
	private void Awake()
	{
		this.inventoryWindow = base.GetComponentInParent<GUIInventoryWindow>();
	}

	public void OnPointerDown(PointerEventData eventData)
	{
		if (this.rectParent == null)
		{
			this.rectParent = base.transform.parent.GetComponent<RectTransform>();
		}
		GUIInventory.instance.selectedTab = base.gameObject;
		Vector2 v;
		RectTransformUtility.ScreenPointToLocalPointInRectangle(this.rectParent, Input.mousePosition, CrewSim.objInstance.UICamera, out v);
		GUIInventory.instance.mouseOffset = v;
		if (this.inventoryWindow != null)
		{
			this.inventoryWindow.SurfaceWindow();
		}
	}

	public void OnPointerUp(PointerEventData eventData)
	{
		if (this.inventoryWindow != null)
		{
			this.inventoryWindow.Pin(true, false);
		}
		GUIInventory.instance.selectedTab = null;
	}

	private GUIInventoryWindow inventoryWindow;

	private RectTransform rectParent;
}
