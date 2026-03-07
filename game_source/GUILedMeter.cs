using System;
using UnityEngine;
using UnityEngine.UI;

public class GUILedMeter : MonoBehaviour
{
	private void Awake()
	{
		Transform transform = base.transform;
		this.aIMGs = new Image[transform.childCount];
		if (this.bReverse)
		{
			for (int i = 0; i < this.aIMGs.Length; i++)
			{
				this.aIMGs[i] = transform.GetChild(i).GetChild(0).GetComponent<Image>();
			}
		}
		else
		{
			for (int j = this.aIMGs.Length - 1; j >= 0; j--)
			{
				this.aIMGs[this.aIMGs.Length - 1 - j] = transform.GetChild(j).GetChild(0).GetComponent<Image>();
			}
		}
		this.bInitialized = true;
		this.SetState(0);
	}

	private void Update()
	{
		if (this.nState == 1)
		{
			this.fInitTime += Time.deltaTime;
			if (this.fInitTime > this.acurveInit.keys[this.acurveInit.length - 1].time)
			{
				this.SetState(2);
			}
			else
			{
				this.SetMeter(Convert.ToInt32(this.acurveInit.Evaluate(this.fInitTime) * (float)this.aIMGs.Length));
			}
		}
		if (this.nState == 2 && this.nLastDisplayed != this.nValue)
		{
			this.SetMeter(this.nValue);
		}
	}

	private void SetMeter(int nAmount)
	{
		if (!this.bInitialized || this.nLastDisplayed == nAmount)
		{
			return;
		}
		for (int i = 0; i < this.aIMGs.Length; i++)
		{
			if (i >= nAmount)
			{
				this.aIMGs[i].sprite = this.aLEDs[this.aOff[i]];
			}
			else
			{
				this.aIMGs[i].sprite = this.aLEDs[this.aOff[i] + 1];
			}
		}
		this.nLastDisplayed = nAmount;
	}

	public void SetValue(int nAmount)
	{
		this.nValue = nAmount;
	}

	public void SetValue(float fAmount)
	{
		this.nValue = Convert.ToInt32(fAmount * (float)this.aIMGs.Length);
	}

	public void SetState(int nState)
	{
		this.nState = nState;
		if (nState != 1)
		{
			if (nState != 2)
			{
				this.SetMeter(0);
			}
			else
			{
				this.SetMeter(this.nValue);
			}
		}
		else
		{
			this.fInitTime = 0f;
		}
	}

	public int State
	{
		get
		{
			return this.nState;
		}
	}

	public override string ToString()
	{
		return string.Concat(new object[]
		{
			"nValue: ",
			this.nValue,
			"; nLast",
			this.nLastDisplayed
		});
	}

	public Sprite[] aLEDs;

	public int[] aOff;

	public bool bReverse;

	private Image[] aIMGs;

	public AnimationCurve acurveInit;

	private float fInitTime;

	private int nState;

	private int nValue;

	private int nLastDisplayed = -1;

	private bool bInitialized;

	public const int STATE_OFF = 0;

	public const int STATE_INIT = 1;

	public const int STATE_ON = 2;
}
