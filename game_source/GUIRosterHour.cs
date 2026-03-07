using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class GUIRosterHour : MonoBehaviour, IPointerEnterHandler, IPointerDownHandler, IEventSystemHandler
{
	private void Awake()
	{
		this.pnlColor = base.transform.Find("pnlColor").GetComponent<Image>();
		this.txtLabel = base.transform.Find("Text").GetComponent<TMP_Text>();
	}

	public void OnPointerEnter(PointerEventData pointerEventData)
	{
		if (Input.GetMouseButton(0))
		{
			this.ToggleShift();
		}
	}

	public void OnPointerDown(PointerEventData pointerEventData)
	{
		this.ToggleShift();
	}

	public void ToggleShift()
	{
		this.nShift++;
		if (this.nShift > JsonCompanyRules.Shifts().Keys.Count - 1)
		{
			this.nShift = 0;
		}
		this.pnlColor.color = JsonCompanyRules.Shifts()[this.nShift].clr;
		if (this.onChange != null)
		{
			this.onChange(this.nHour, this.nShift);
		}
	}

	public void SetShift(int nShift)
	{
		this.nShift = nShift;
		if (nShift > JsonCompanyRules.Shifts().Keys.Count - 1)
		{
			nShift = 0;
		}
		if (nShift < 0)
		{
			nShift = 0;
		}
		this.pnlColor.color = JsonCompanyRules.Shifts()[nShift].clr;
	}

	public int nShift;

	public TMP_Text txtLabel;

	private Image pnlColor;

	public int nHour = -1;

	public Action<int, int> onChange;
}
