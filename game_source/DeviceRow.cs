using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DeviceRow : MonoBehaviour
{
	private void Awake()
	{
	}

	public void Init(CondOwner coDevice, string strFilename, Action<Transform, CondOwner> act = null)
	{
		this.txt.text = strFilename;
		this.goPC.SetActive(false);
		this.goPhone.SetActive(false);
		this.goTablet.SetActive(false);
		if (this.CTNav.Triggered(coDevice, null, true))
		{
			this.goPC.SetActive(true);
		}
		else
		{
			this.goTablet.SetActive(true);
		}
		if (act != null)
		{
			Button component = base.GetComponent<Button>();
			component.onClick.AddListener(delegate()
			{
				act(this.transform.parent, coDevice);
			});
		}
	}

	public void Tint(Color color)
	{
		this.txt.color = color;
		this.goPC.GetComponent<Image>().color = color;
		this.goTablet.GetComponent<Image>().color = color;
		this.goPhone.GetComponent<Image>().color = color;
	}

	private CondTrigger CTNav
	{
		get
		{
			if (this._ctNav == null)
			{
				this._ctNav = DataHandler.GetCondTrigger("TIsNavStationNotOff");
			}
			return this._ctNav;
		}
	}

	[SerializeField]
	private TMP_Text txt;

	[SerializeField]
	private GameObject goPC;

	[SerializeField]
	private GameObject goTablet;

	[SerializeField]
	private GameObject goPhone;

	private CondTrigger _ctNav;
}
