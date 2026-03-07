using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GUIBtnLitRim : MonoBehaviour
{
	private void Awake()
	{
		this.chk = base.GetComponent<Toggle>();
		Transform transform = base.transform.Find("Background/Checkmark/lblOn");
		if (transform != null)
		{
			this.goLbl = transform.gameObject;
			this.txtOn = transform.GetComponent<TMP_Text>();
		}
		transform = base.transform.Find("Background/lblOff");
		if (transform != null)
		{
			this.txtOff = transform.GetComponent<TMP_Text>();
		}
		this.chk.onValueChanged.AddListener(delegate(bool A_1)
		{
			this.OnChange();
		});
		this.OnChange();
	}

	private void OnChange()
	{
		if (this.goLbl == null)
		{
			return;
		}
		this.goLbl.SetActive(this.chk.isOn);
	}

	public void SetText(string str)
	{
		if (this.txtOff != null && this.txtOn != null)
		{
			if (this.txtOn.text != str)
			{
				this.txtOn.text = str;
			}
			if (this.txtOff.text != str)
			{
				this.txtOff.text = str;
			}
		}
	}

	public void Tint(Color color, bool bTXTOn = false)
	{
		if (bTXTOn)
		{
			this.txtOn.color = color;
		}
		this.txtOff.color = color;
		if (this.chk.targetGraphic != null)
		{
			this.chk.targetGraphic.color = color;
		}
		if (this.chk.graphic != null)
		{
			this.chk.graphic.color = color;
		}
	}

	private GameObject goLbl;

	private Toggle chk;

	private TMP_Text txtOn;

	private TMP_Text txtOff;
}
