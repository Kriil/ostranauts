using System;
using TMPro;
using UnityEngine;

public class GUIPaperDollHitbox : MonoBehaviour
{
	public void UpdateStackText(GameObject go)
	{
		int num = 0;
		if (this.coUs != null)
		{
			num = this.coUs.StackCount;
		}
		float alpha = (num <= 1) ? 0f : 1f;
		for (int i = 0; i < go.transform.childCount; i++)
		{
			Transform child = go.transform.GetChild(i);
			CanvasGroup component = child.GetComponent<CanvasGroup>();
			if (component != null)
			{
				component.alpha = alpha;
			}
			TextMeshProUGUI component2 = child.GetComponent<TextMeshProUGUI>();
			if (component2 != null)
			{
				component2.text = "x" + num.ToString();
				component2.transform.rotation = Quaternion.identity;
			}
		}
	}

	public CondOwner coUs
	{
		get
		{
			if (this._coRef != null && this._coRef.strID == this.strCOID)
			{
				return this._coRef;
			}
			if (this.strCOID == null)
			{
				this._coRef = null;
				return null;
			}
			DataHandler.mapCOs.TryGetValue(this.strCOID, out this._coRef);
			return this._coRef;
		}
		set
		{
			if (value == null)
			{
				this.strCOID = null;
			}
			else
			{
				this.strCOID = value.strID;
			}
			this._coRef = null;
		}
	}

	public CanvasGroup cgPaperDoll;

	public CanvasGroup cgIcon;

	private const float fMinAlpha = 0.2f;

	public string strSlotName;

	private string strCOID;

	private CondOwner _coRef;
}
