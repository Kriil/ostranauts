using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GUIModRow : MonoBehaviour
{
	private void Awake()
	{
		this.txtName = base.transform.Find("txtName").GetComponent<TMP_Text>();
		this.txtStatus = base.transform.Find("pnlStatus/txt").GetComponent<TMP_Text>();
		this.bmpStatus = base.transform.Find("pnlStatus/bmp").GetComponent<Image>();
		this.Status = GUIModRow.ModStatus.Loaded;
	}

	public GUIModRow.ModStatus Status
	{
		get
		{
			return this._status;
		}
		set
		{
			if (value != GUIModRow.ModStatus.Error)
			{
				if (value != GUIModRow.ModStatus.Loaded)
				{
					if (value == GUIModRow.ModStatus.Missing)
					{
						this.txtStatus.color = this.clrFail;
						this.bmpStatus.color = this.clrFail;
						this.txtStatus.text = "MISSING";
					}
				}
				else
				{
					this.txtStatus.color = this.clrPass;
					this.bmpStatus.color = this.clrPass;
					this.txtStatus.text = "LOADED";
				}
			}
			else
			{
				this.txtStatus.color = this.clrFail;
				this.bmpStatus.color = this.clrFail;
				this.txtStatus.text = "ERROR";
			}
		}
	}

	public Color clrFail = new Color(0.5019608f, 0.07058824f, 0.07058824f);

	public Color clrPass = new Color(0.05490196f, 0.7372549f, 0.07058824f);

	public TMP_Text txtName;

	private TMP_Text txtStatus;

	private Image bmpStatus;

	private GUIModRow.ModStatus _status;

	public enum ModStatus
	{
		Missing,
		Loaded,
		Error
	}
}
