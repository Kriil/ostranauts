using System;
using UnityEngine;

public class GUIHelmetBar : MonoBehaviour
{
	private void Awake()
	{
		this.tfBarDangerHigh = base.transform.Find("bmpDangerHigh").GetComponent<RectTransform>();
		this.tfBarDangerLow = base.transform.Find("bmpDangerLow").GetComponent<RectTransform>();
		this.tfBar = base.transform.Find("bmpBar").GetComponent<RectTransform>();
	}

	public double DangerLow
	{
		get
		{
			return this.fDangerLow;
		}
		set
		{
			this.fDangerLow = value;
			float num = (float)((this.fDangerLow - this.fMin) / (this.fMax - this.fMin));
			if (num > 1f)
			{
				num = 1f;
			}
			else if (num < 0f)
			{
				num = 0f;
			}
			this.tfBarDangerLow.localScale = new Vector3(1f, num, 1f);
		}
	}

	public double DangerHigh
	{
		get
		{
			return this.fDangerHigh;
		}
		set
		{
			this.fDangerHigh = value;
			float num = (float)((this.fMax - this.fDangerHigh) / (this.fMax - this.fMin));
			if (num > 1f)
			{
				num = 1f;
			}
			else if (num < 0f)
			{
				num = 0f;
			}
			this.tfBarDangerHigh.localScale = new Vector3(1f, num, 1f);
		}
	}

	public double Min
	{
		get
		{
			return this.fMin;
		}
		set
		{
			this.fMin = value;
			this.DangerHigh = this.DangerHigh;
			this.DangerLow = this.DangerLow;
		}
	}

	public double Max
	{
		get
		{
			return this.fMax;
		}
		set
		{
			this.fMax = value;
			this.DangerHigh = this.DangerHigh;
			this.DangerLow = this.DangerLow;
		}
	}

	public double Value
	{
		get
		{
			return this.fValue;
		}
		set
		{
			this.fValue = value;
			float num = (float)((this.fValue - this.fMin) / (this.fMax - this.fMin));
			if (num > 1f)
			{
				num = 1f;
			}
			else if (num < 0f)
			{
				num = 0f;
			}
			this.tfBar.localScale = new Vector3(1f, num, 1f);
		}
	}

	private RectTransform tfBar;

	private RectTransform tfBarDangerLow;

	private RectTransform tfBarDangerHigh;

	private double fMin;

	private double fMax = 1.0;

	private double fValue = 0.5;

	private double fDangerLow;

	private double fDangerHigh;
}
