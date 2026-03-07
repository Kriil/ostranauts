using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class GUICrew : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IEventSystemHandler
{
	public void OnPointerEnter(PointerEventData eventData)
	{
		this.bMouseOver = true;
	}

	public void OnPointerExit(PointerEventData eventData)
	{
		this.bMouseOver = false;
	}

	public void ResetScrollPosition()
	{
		this.messageText.margin = new Vector4(0f, this.messageText.preferredHeight, 0f, 0f);
	}

	private void Update()
	{
		if (this.bMouseOver)
		{
			if (Input.mouseScrollDelta.y > 0f)
			{
				this.messageText.margin -= new Vector4(0f, 0f, 0f, 50f);
			}
			if (Input.mouseScrollDelta.y < 0f)
			{
				this.messageText.margin += new Vector4(0f, 0f, 0f, 50f);
			}
		}
	}

	public TextMeshProUGUI nameText;

	public TextMeshProUGUI messageText;

	public TextMeshProUGUI conditionText;

	public GameObject UIBackgroundLower;

	public bool bMouseOver;
}
