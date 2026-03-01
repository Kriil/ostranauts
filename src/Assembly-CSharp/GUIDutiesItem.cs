using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class GUIDutiesItem : MonoBehaviour, IPointerEnterHandler, IPointerDownHandler, IEventSystemHandler
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
			this.Decrement();
		}
		else if (Input.GetMouseButton(1))
		{
			this.Increment();
		}
	}

	public void OnPointerDown(PointerEventData pointerEventData)
	{
		if (pointerEventData.button == PointerEventData.InputButton.Left)
		{
			this.Decrement();
		}
		else if (pointerEventData.button == PointerEventData.InputButton.Right)
		{
			this.Increment();
		}
	}

	public void Decrement()
	{
		this.nPriority--;
		this.SetPriority(this.nPriority);
		if (this.onChange != null)
		{
			this.onChange(this.nItemID, this.nPriority);
		}
	}

	public void Increment()
	{
		this.nPriority++;
		this.SetPriority(this.nPriority);
		if (this.onChange != null)
		{
			this.onChange(this.nItemID, this.nPriority);
		}
	}

	public void SetPriority(int nPriority)
	{
		if (nPriority > JsonCompanyRules.nPriorityMax)
		{
			nPriority = JsonCompanyRules.nPriorityMin;
		}
		if (nPriority < JsonCompanyRules.nPriorityMin)
		{
			nPriority = JsonCompanyRules.nPriorityMax;
		}
		this.nPriority = nPriority;
		float num = 1f - 1f * (float)nPriority / (float)(1 + JsonCompanyRules.nPriorityMax - JsonCompanyRules.nPriorityMin);
		this.pnlColor.color = new Color(JsonCompanyRules.clrDuty.r * num, JsonCompanyRules.clrDuty.g * num, JsonCompanyRules.clrDuty.b * num);
		this.txtLabel.text = string.Empty + nPriority;
	}

	public int nPriority = 3;

	public int nItemID;

	public TMP_Text txtLabel;

	private Image pnlColor;

	public Action<int, int> onChange;
}
