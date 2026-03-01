using System;
using UnityEngine;
using UnityEngine.UI;

public class GUILamp : MonoBehaviour
{
	private void Awake()
	{
		this.State = 0;
		this.SetImage(this.nValue);
	}

	private void Update()
	{
		if (this.nState == 1)
		{
			this.fInitTime += Time.deltaTime;
			if (this.fInitTime > this.acurveInit.keys[this.acurveInit.length - 1].time)
			{
				this.State = 2;
			}
			else
			{
				this.SetImage(Convert.ToInt32(this.acurveInit.Evaluate(this.fInitTime) * (float)(this.aSprites.Length - 1)));
			}
		}
		else if (this.nState == 2)
		{
			this.fInitTime += Time.deltaTime;
			this.SetImage(Convert.ToInt32(this.acurveWait.Evaluate(this.fInitTime) * (float)(this.aSprites.Length - 1)));
		}
	}

	private void SetImage(int nIndex)
	{
		if (this.bmp == null)
		{
			this.bmp = base.GetComponent<Image>();
		}
		if (nIndex < 0)
		{
			nIndex = 0;
		}
		if (nIndex >= this.aSprites.Length)
		{
			nIndex = this.aSprites.Length - 1;
		}
		if (nIndex >= this.aSprites.Length)
		{
			return;
		}
		if (this.bmp.sprite != this.aSprites[nIndex])
		{
			this.bmp.sprite = this.aSprites[nIndex];
			this.nCurrIndex = nIndex;
		}
	}

	public void SetValue(int nAmount)
	{
		this.nValue = nAmount;
		this.SetImage(this.nValue);
	}

	public int State
	{
		get
		{
			return this.nState;
		}
		set
		{
			if (value == this.nState)
			{
				return;
			}
			this.nState = value;
			switch (this.nState)
			{
			case 1:
				this.fInitTime = 0f;
				break;
			case 2:
				this.fInitTime = 0f;
				this.SetImage(this.nValue);
				break;
			case 3:
				this.nValue = 1;
				this.SetImage(this.nValue);
				break;
			default:
				this.nValue = 0;
				this.SetImage(this.nValue);
				break;
			}
		}
	}

	public int ImageIndex
	{
		get
		{
			return this.nCurrIndex;
		}
	}

	public AnimationCurve acurveInit;

	public AnimationCurve acurveWait;

	public Sprite[] aSprites;

	private float fInitTime;

	private int nState;

	private Image bmp;

	private int nValue;

	private int nCurrIndex;

	public const int STATE_OFF = 0;

	public const int STATE_INIT = 1;

	public const int STATE_WAIT = 2;

	public const int STATE_ON = 3;
}
