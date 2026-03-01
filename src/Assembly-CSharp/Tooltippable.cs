using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class Tooltippable : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IEventSystemHandler
{
	private void Start()
	{
		this.gii = base.gameObject.GetComponent<GUIInventoryItem>();
	}

	public void OnPointerEnter(PointerEventData eventData)
	{
		CrewSim.inventoryGUI.coTooltip = this.CO;
	}

	public void OnPointerExit(PointerEventData eventData)
	{
		if (CrewSim.inventoryGUI.coTooltip == this.CO)
		{
			CrewSim.inventoryGUI.coTooltip = null;
		}
	}

	public CondOwner CO
	{
		get
		{
			if (this.gii != null)
			{
				return this.gii.CO;
			}
			this.gii = base.gameObject.GetComponent<GUIInventoryItem>();
			if (this.gii != null)
			{
				return this.gii.CO;
			}
			return null;
		}
	}

	private GUIInventoryItem gii;
}
