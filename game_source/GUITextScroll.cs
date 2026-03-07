using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class GUITextScroll : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IEventSystemHandler
{
	private void Awake()
	{
		this.txt = base.GetComponent<TextMeshProUGUI>();
		if (this.txt.alignment == TextAlignmentOptions.Bottom || this.txt.alignment == TextAlignmentOptions.BottomFlush || this.txt.alignment == TextAlignmentOptions.BottomGeoAligned || this.txt.alignment == TextAlignmentOptions.BottomJustified || this.txt.alignment == TextAlignmentOptions.BottomLeft || this.txt.alignment == TextAlignmentOptions.BottomRight)
		{
			this.bTop = false;
		}
	}

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
		this.txt.margin = new Vector4(0f, this.txt.preferredHeight, 0f, 0f);
	}

	private void Update()
	{
		if (this.bMouseOver)
		{
			if (Input.mouseScrollDelta.y > 0f)
			{
				if (this.bTop)
				{
					this.txt.margin -= new Vector4(0f, -50f, 0f, 0f);
				}
				else
				{
					this.txt.margin -= new Vector4(0f, 0f, 0f, 50f);
				}
				Debug.Log(this.txt.margin + " vs " + this.txt.preferredHeight);
			}
			if (Input.mouseScrollDelta.y < 0f)
			{
				if (this.bTop)
				{
					this.txt.margin -= new Vector4(0f, 50f, 0f, 0f);
				}
				else
				{
					this.txt.margin -= new Vector4(0f, 0f, 0f, -50f);
				}
				Debug.Log(this.txt.margin + " vs " + this.txt.preferredHeight);
			}
		}
	}

	private TextMeshProUGUI txt;

	private bool bTop = true;

	private bool bMouseOver;
}
