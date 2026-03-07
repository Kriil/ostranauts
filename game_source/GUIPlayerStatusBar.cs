using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class GUIPlayerStatusBar : MonoBehaviour
{
	private void Awake()
	{
		this.go = base.gameObject;
		this.txtLabel = base.transform.Find("lbl").GetComponent<TMP_Text>();
		this.lamp = base.GetComponent<GUILamp>();
	}

	public void Init(string strName, List<string> aCondsToTrack)
	{
		if (aCondsToTrack == null || strName == null)
		{
			return;
		}
		this.strName = strName;
		this.aConds = new List<Condition>();
		foreach (string text in aCondsToTrack)
		{
			Condition cond = DataHandler.GetCond(text);
			if (cond != null)
			{
				this.aConds.Add(cond);
			}
		}
		this.txtLabel.text = strName;
		this.lamp.SetValue(0);
	}

	public void UpdateStats(CondOwner co)
	{
		if (co != null)
		{
			foreach (Condition condition in this.aConds)
			{
				if (co.HasCond(condition.strName))
				{
					if (this.txtLabel.text != condition.strNameFriendly)
					{
						this.txtLabel.text = condition.strNameFriendly;
					}
					if (condition.strColor == "Bad")
					{
						this.lamp.SetValue(2);
					}
					else if (condition.strColor == "Neutral")
					{
						this.lamp.SetValue(1);
					}
					else
					{
						this.lamp.SetValue(0);
					}
					return;
				}
			}
		}
		if (this.txtLabel.text != this.strName)
		{
			this.txtLabel.text = this.strName;
		}
		this.lamp.SetValue(0);
	}

	public void UpdateManual(string strLabel, string strColor)
	{
		if (strLabel != null && strColor != null)
		{
			this.txtLabel.text = strLabel;
			if (strColor == "Bad")
			{
				this.lamp.SetValue(2);
			}
			else if (strColor == "Neutral")
			{
				this.lamp.SetValue(1);
			}
			else
			{
				this.lamp.SetValue(0);
			}
			return;
		}
		if (this.txtLabel.text != this.strName)
		{
			this.txtLabel.text = this.strName;
		}
		this.lamp.SetValue(0);
	}

	private GUILamp lamp;

	private TMP_Text txtLabel;

	private GameObject go;

	private string strName;

	private List<Condition> aConds;
}
